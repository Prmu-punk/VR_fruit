using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class StartMenuView : MonoBehaviour
{
    [SerializeField] private GameObject startFruitPrefab;
    [SerializeField] private GameObject[] modeFruitPrefabs;
    [SerializeField] private float startFruitScale = 0.38f;

    private Canvas canvas;
    private readonly List<GameObject> startFruits = new List<GameObject>();

    public void SetStartFruitPrefab(GameObject prefab)
    {
        startFruitPrefab = prefab;
    }

    public void SetModeFruitPrefabs(GameObject[] prefabs)
    {
        modeFruitPrefabs = prefabs;
    }

    public void Show()
    {
        if (canvas == null) {
            BuildCanvas();
        }

        canvas.gameObject.SetActive(true);
        CreateStartFruits();
    }

    public void Hide()
    {
        if (canvas != null) {
            canvas.gameObject.SetActive(false);
        }

        foreach (GameObject fruit in startFruits) {
            if (fruit != null) {
                Destroy(fruit);
            }
        }

        startFruits.Clear();
    }

    private void BuildCanvas()
    {
        GameObject canvasObject = new GameObject("Start Menu Canvas");
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObject.AddComponent<GraphicRaycaster>();

        CreateTitle(canvasObject.transform);
        CreateHint(canvasObject.transform);
        CreateModeLabel(canvasObject.transform);
    }

    private void CreateTitle(Transform parent)
    {
        GameObject titleObject = new GameObject("Title");
        titleObject.transform.SetParent(parent, false);

        Text title = titleObject.AddComponent<Text>();
        title.text = "FRUIT NINJA VR";
        title.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        title.fontSize = 68;
        title.fontStyle = FontStyle.Bold;
        title.alignment = TextAnchor.UpperCenter;
        title.color = new Color(1f, 0.85f, 0.18f);

        RectTransform rect = title.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -32f);
        rect.sizeDelta = new Vector2(860f, 110f);
    }

    private void CreateHint(Transform parent)
    {
        GameObject hintObject = new GameObject("Hint");
        hintObject.transform.SetParent(parent, false);

        Text hint = hintObject.AddComponent<Text>();
        hint.text = "Slice a fruit to choose your mode";
        hint.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        hint.fontSize = 34;
        hint.fontStyle = FontStyle.Bold;
        hint.alignment = TextAnchor.MiddleCenter;
        hint.color = new Color(1f, 0.95f, 0.78f);

        RectTransform rect = hint.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -275f);
        rect.sizeDelta = new Vector2(760f, 90f);
    }

    private void CreateModeLabel(Transform parent)
    {
        GameObject labelObject = new GameObject("Mode Label");
        labelObject.transform.SetParent(parent, false);

        Text label = labelObject.AddComponent<Text>();
        label.text = "CLASSIC  |  ARCADE  |  ZEN  |  BATTLE";
        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        label.fontSize = 30;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(0.3f, 0.9f, 1f);

        RectTransform rect = label.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 260f);
        rect.sizeDelta = new Vector2(820f, 70f);
    }

    private void CreateStartFruits()
    {
        if (startFruits.Count > 0 || startFruitPrefab == null)
            return;

        CreateModeFruit(FruitNinjaMode.Classic, new Vector3(-2.15f, 1.45f, -3.35f), "CLASSIC\n3 MISSES");
        CreateModeFruit(FruitNinjaMode.Arcade, new Vector3(-0.72f, 1.95f, -3.35f), "ARCADE\n60 SEC");
        CreateModeFruit(FruitNinjaMode.Zen, new Vector3(0.72f, 1.95f, -3.35f), "ZEN\nNO BOMBS");
        CreateModeFruit(FruitNinjaMode.Battle, new Vector3(2.15f, 1.45f, -3.35f), "BATTLE\nSCORE");
    }

    private void CreateModeFruit(FruitNinjaMode mode, Vector3 position, string label)
    {
        GameObject prefab = GetModePrefab(mode);
        GameObject fruit = Instantiate(prefab, position, Quaternion.identity);
        fruit.name = $"Slice To Start {mode}";
        fruit.transform.localScale *= startFruitScale;

        Rigidbody body = fruit.GetComponent<Rigidbody>();
        if (body != null) {
            body.isKinematic = true;
            body.useGravity = false;
        }

        Collider collider = fruit.GetComponent<Collider>();
        if (collider != null) {
            collider.isTrigger = true;
        }

        Fruit fruitScript = fruit.GetComponent<Fruit>();
        if (fruitScript != null) {
            fruitScript.enabled = false;
        }

        StartFruit trigger = fruit.AddComponent<StartFruit>();
        trigger.Init(mode);
        fruit.AddComponent<FloatingMenuFruit>();

        AddWorldLabel(fruit.transform, label);
        startFruits.Add(fruit);
    }

    private GameObject GetModePrefab(FruitNinjaMode mode)
    {
        if (modeFruitPrefabs == null || modeFruitPrefabs.Length == 0)
            return startFruitPrefab;

        int index = Mathf.Clamp((int)mode, 0, modeFruitPrefabs.Length - 1);
        return modeFruitPrefabs[index] != null ? modeFruitPrefabs[index] : startFruitPrefab;
    }

    private void AddWorldLabel(Transform parent, string text)
    {
        GameObject labelObject = new GameObject("Mode World Label");
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.localPosition = new Vector3(0f, -0.85f, 0f);
        labelObject.transform.localScale = Vector3.one / startFruitScale;

        TextMesh label = labelObject.AddComponent<TextMesh>();
        label.text = text;
        label.fontSize = 40;
        label.characterSize = 0.042f;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.color = new Color(1f, 0.92f, 0.55f);
    }
}
