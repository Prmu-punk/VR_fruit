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
    [Header("VR HUD")]
    [SerializeField] private Vector3 hudWorldPosition = new Vector3(0f, 1.7f, -3.35f);
    [SerializeField] private Vector2 hudCanvasSize = new Vector2(1.2f, 0.28f);
    [SerializeField] private float hudCanvasScale = 0.0015f;

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

        ConfigureWorldSpaceHud();
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
        SetScoreVisible(true);
        SetStatusVisible(false);

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
        spawner.minSpawnedObjectScale = 0.22f;
        spawner.maxSpawnedObjectScale = 0.3f;
        spawner.minSideForce = -0.18f;
        spawner.maxSideForce = 0.18f;
        spawner.minDepthForce = 0.04f;
        spawner.maxDepthForce = 0.14f;
        spawner.maxLifetime = 6.2f;
        spawner.bombChance = mode == FruitNinjaMode.Zen ? 0f : mode == FruitNinjaMode.Arcade ? 0.08f : 0.05f;

        if (mode == FruitNinjaMode.Zen) {
            spawner.minSpawnDelay = 0.85f;
            spawner.maxSpawnDelay = 1.55f;
            spawner.minForce = 4.25f;
            spawner.maxForce = 5.75f;
        } else if (mode == FruitNinjaMode.Arcade) {
            spawner.minSpawnDelay = 0.32f;
            spawner.maxSpawnDelay = 0.85f;
            spawner.minForce = 4.75f;
            spawner.maxForce = 6.5f;
        } else if (mode == FruitNinjaMode.Battle) {
            spawner.minSpawnDelay = 0.3f;
            spawner.maxSpawnDelay = 0.75f;
            spawner.minForce = 5f;
            spawner.maxForce = 6.75f;
        } else {
            spawner.minSpawnDelay = 0.45f;
            spawner.maxSpawnDelay = 1.25f;
            spawner.minForce = 4.5f;
            spawner.maxForce = 6.25f;
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
        SetScoreVisible(visible);
        SetStatusVisible(visible);
    }

    private void SetScoreVisible(bool visible)
    {
        if (scoreText != null) {
            scoreText.gameObject.SetActive(visible);
        }
    }

    private void SetStatusVisible(bool visible)
    {
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
        Canvas canvas = GetOrCreateHudCanvas();
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

    private void ConfigureWorldSpaceHud()
    {
        Canvas canvas = GetOrCreateHudCanvas();
        if (canvas == null)
            return;

        if (scoreText == null) {
            scoreText = CreateHudText("Score Text", canvas.transform, new Vector2(-330f, 48f), new Vector2(240f, 90f), 56, TextAnchor.MiddleLeft);
        } else {
            MoveTextToHud(scoreText, canvas.transform, new Vector2(-330f, 48f), new Vector2(240f, 90f), 56, TextAnchor.MiddleLeft);
        }

        if (statusText == null) {
            statusText = CreateHudText("Status Text", canvas.transform, new Vector2(120f, 48f), new Vector2(620f, 90f), 30, TextAnchor.MiddleRight);
        } else {
            MoveTextToHud(statusText, canvas.transform, new Vector2(120f, 48f), new Vector2(620f, 90f), 30, TextAnchor.MiddleRight);
        }
    }

    private Canvas GetOrCreateHudCanvas()
    {
        GameObject canvasObject = GameObject.Find("VR HUD Canvas");
        Canvas canvas = canvasObject != null ? canvasObject.GetComponent<Canvas>() : null;

        if (canvas == null)
        {
            canvasObject = new GameObject("VR HUD Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 20;
        canvas.worldCamera = Camera.main;

        RectTransform rect = canvas.GetComponent<RectTransform>();
        rect.sizeDelta = hudCanvasSize / hudCanvasScale;
        rect.position = hudWorldPosition;
        rect.rotation = Quaternion.identity;
        rect.localScale = Vector3.one * hudCanvasScale;

        return canvas;
    }

    private Text CreateHudText(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontStyle = FontStyle.Bold;
        text.color = new Color(1f, 0.9f, 0.2f);

        MoveTextToHud(text, parent, anchoredPosition, size, fontSize, alignment);
        return text;
    }

    private void MoveTextToHud(Text text, Transform parent, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment)
    {
        text.transform.SetParent(parent, false);
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.raycastTarget = false;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

}
