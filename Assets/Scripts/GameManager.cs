using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using WebcamXR;

[DefaultExecutionOrder(-1)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private Blade blade;
    [SerializeField] private Spawner spawner;
    [SerializeField] private Text scoreText;
    [SerializeField] private Image fadeImage;
    [SerializeField] private GameObject startFruitPrefab;
    [SerializeField] private Text statusText;
    [SerializeField] private DojoSceneBuilder dojoSceneBuilder;
    [SerializeField] private XRRuntimeRigDriver xrRuntimeRigDriver;
    [SerializeField] private WebcamFruitXRControllerRig webcamXRControllerRig;

    public int score { get; private set; } = 0;
    private StartMenuView startMenu;
    private FruitNinjaMode currentMode;
    private float timeRemaining;
    private int missedFruit;
    private bool gameRunning;
    private Coroutine timerRoutine;

    private const int BombPenalty = 10;

    private void Awake()
    {
        if (Instance != null) {
            DestroyImmediate(gameObject);
        } else {
            Instance = this;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) {
            Instance = null;
        }
    }

    private void Start()
    {
        if (dojoSceneBuilder == null) {
            dojoSceneBuilder = gameObject.AddComponent<DojoSceneBuilder>();
        }

        dojoSceneBuilder.BuildNow();

        if (xrRuntimeRigDriver == null) {
            xrRuntimeRigDriver = gameObject.AddComponent<XRRuntimeRigDriver>();
        }

        if (webcamXRControllerRig == null) {
            GameObject rigObject = new GameObject("Webcam XR Controller Rig");
            webcamXRControllerRig = rigObject.AddComponent<WebcamFruitXRControllerRig>();
        }

        ShowStartMenu();
    }

    public void StartGameFromMenu(FruitNinjaMode mode)
    {
        if (startMenu != null) {
            startMenu.Hide();
        }

        NewGame(mode);
    }

    private void ShowStartMenu()
    {
        Time.timeScale = 1f;
        ClearScene();

        blade.enabled = true;
        spawner.enabled = false;
        gameRunning = false;

        score = 0;
        UpdateHud();
        SetHudVisible(false);

        if (startMenu == null) {
            startMenu = gameObject.AddComponent<StartMenuView>();
        }

        if (startFruitPrefab == null && spawner.fruitPrefabs.Length > 0) {
            startFruitPrefab = spawner.fruitPrefabs[0];
        }

        startMenu.SetStartFruitPrefab(startFruitPrefab);
        startMenu.SetModeFruitPrefabs(spawner.fruitPrefabs);
        startMenu.Show();
    }

    private void NewGame(FruitNinjaMode mode)
    {
        Time.timeScale = 1f;
        currentMode = mode;
        gameRunning = true;

        ClearScene();

        blade.enabled = true;
        spawner.enabled = true;
        ConfigureSpawnerForMode(mode);

        score = 0;
        missedFruit = 0;
        timeRemaining = GetModeDuration(mode);
        SetHudVisible(true);
        UpdateHud();

        if (timerRoutine != null) {
            StopCoroutine(timerRoutine);
        }

        timerRoutine = StartCoroutine(TimerRoutine());
    }

    private void ClearScene()
    {
        Fruit[] fruits = FindObjectsOfType<Fruit>();

        foreach (Fruit fruit in fruits) {
            Destroy(fruit.gameObject);
        }

        Bomb[] bombs = FindObjectsOfType<Bomb>();

        foreach (Bomb bomb in bombs) {
            Destroy(bomb.gameObject);
        }
    }

    public void IncreaseScore(int points)
    {
        score += points;
        UpdateHud();

        float hiscore = PlayerPrefs.GetFloat("hiscore", 0);

        if (score > hiscore)
        {
            hiscore = score;
            PlayerPrefs.SetFloat("hiscore", hiscore);
        }
    }

    public void Explode()
    {
        HandleBombHit(Vector3.zero);
    }

    public void HandleBombHit(Vector3 position)
    {
        if (!gameRunning)
            return;

        score = Mathf.Max(0, score - BombPenalty);
        UpdateHud();
    }

    public void ReportMissedFruit()
    {
        if (!gameRunning)
            return;

        if (currentMode == FruitNinjaMode.Zen)
            return;

        missedFruit++;
        UpdateHud();

        if (currentMode == FruitNinjaMode.Classic && missedFruit >= 3) {
            EndGame();
        }
    }

    private IEnumerator TimerRoutine()
    {
        while (gameRunning)
        {
            if (timeRemaining > 0f) {
                timeRemaining -= Time.deltaTime;
                if (timeRemaining <= 0f) {
                    timeRemaining = 0f;
                    UpdateHud();
                    EndGame();
                    yield break;
                }
            }

            UpdateHud();
            yield return null;
        }
    }

    private void EndGame()
    {
        if (!gameRunning)
            return;

        gameRunning = false;
        blade.enabled = false;
        spawner.enabled = false;

        if (timerRoutine != null) {
            StopCoroutine(timerRoutine);
            timerRoutine = null;
        }

        StartCoroutine(EndGameSequence());
    }

    private IEnumerator EndGameSequence()
    {
        float elapsed = 0f;
        float duration = 0.5f;

        // Fade to white
        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            fadeImage.color = Color.Lerp(Color.clear, Color.white, t);

            Time.timeScale = 1f - t;
            elapsed += Time.unscaledDeltaTime;

            yield return null;
        }

        yield return new WaitForSecondsRealtime(1f);

        ShowStartMenu();

        elapsed = 0f;

        // Fade back in
        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            fadeImage.color = Color.Lerp(Color.white, Color.clear, t);

            elapsed += Time.unscaledDeltaTime;

            yield return null;
        }
    }

    private void ConfigureSpawnerForMode(FruitNinjaMode mode)
    {
        spawner.spawnedObjectScale = 0.44f;
        spawner.minSideForce = -0.55f;
        spawner.maxSideForce = 0.55f;
        spawner.minDepthForce = 0.18f;
        spawner.maxDepthForce = 0.55f;
        spawner.maxLifetime = 6.2f;
        spawner.bombChance = mode == FruitNinjaMode.Zen ? 0f : mode == FruitNinjaMode.Arcade ? 0.08f : 0.05f;

        if (mode == FruitNinjaMode.Zen) {
            spawner.minSpawnDelay = 0.55f;
            spawner.maxSpawnDelay = 1.15f;
            spawner.minForce = 5.8f;
            spawner.maxForce = 8.2f;
        } else if (mode == FruitNinjaMode.Arcade) {
            spawner.minSpawnDelay = 0.18f;
            spawner.maxSpawnDelay = 0.55f;
            spawner.minForce = 6.4f;
            spawner.maxForce = 9.2f;
        } else if (mode == FruitNinjaMode.Battle) {
            spawner.minSpawnDelay = 0.16f;
            spawner.maxSpawnDelay = 0.45f;
            spawner.minForce = 6.7f;
            spawner.maxForce = 9.6f;
        } else {
            spawner.minSpawnDelay = 0.25f;
            spawner.maxSpawnDelay = 1f;
            spawner.minForce = 6f;
            spawner.maxForce = 8.8f;
        }
    }

    private float GetModeDuration(FruitNinjaMode mode)
    {
        switch (mode)
        {
            case FruitNinjaMode.Arcade:
                return 60f;
            case FruitNinjaMode.Zen:
                return 90f;
            case FruitNinjaMode.Battle:
                return 45f;
            default:
                return 120f;
        }
    }

    private void SetHudVisible(bool visible)
    {
        if (scoreText != null) {
            scoreText.gameObject.SetActive(visible);
        }

        if (statusText != null) {
            statusText.gameObject.SetActive(visible);
        }
    }

    private void UpdateHud()
    {
        if (scoreText != null) {
            scoreText.text = score.ToString();
        }

        if (statusText == null) {
            statusText = CreateStatusText();
        }

        if (statusText == null)
            return;

        statusText.text = $"{currentMode}   TIME {Mathf.CeilToInt(timeRemaining):00}s   MISS {missedFruit}/3   BOMB -{BombPenalty}";
    }

    private Text CreateStatusText()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
            return null;

        GameObject statusObject = new GameObject("Status Text");
        statusObject.transform.SetParent(canvas.transform, false);

        Text text = statusObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 34;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.UpperCenter;
        text.color = new Color(1f, 0.9f, 0.2f);

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -18f);
        rect.sizeDelta = new Vector2(900f, 70f);

        return text;
    }

}
