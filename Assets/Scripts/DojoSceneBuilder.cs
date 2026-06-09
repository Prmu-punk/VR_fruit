using UnityEngine;

public class DojoSceneBuilder : MonoBehaviour
{
    [SerializeField] private Spawner spawner;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Vector3 cameraPosition = new Vector3(0f, 1.55f, -6.2f);
    [SerializeField] private Vector3 cameraEulerAngles = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 playPlaneCenter = new Vector3(0f, 1.85f, -2.85f);
    private bool built;

    private void Awake()
    {
        BuildNow();
    }

    public void BuildNow()
    {
        if (built)
            return;

        built = true;

        if (targetCamera == null) {
            targetCamera = Camera.main;
        }

        ConfigureCamera();
        ConfigureLighting();
        HideLegacyBackground();
        BuildDojo();
        PositionSpawner();
    }

    private void ConfigureCamera()
    {
        if (targetCamera == null)
            return;

        targetCamera.orthographic = false;
        targetCamera.fieldOfView = 72f;
        targetCamera.transform.position = cameraPosition;
        targetCamera.transform.rotation = Quaternion.Euler(cameraEulerAngles);
        targetCamera.backgroundColor = new Color(0.08f, 0.055f, 0.035f);
    }

    private void ConfigureLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.68f, 0.62f, 0.56f);

        Light[] lights = FindObjectsOfType<Light>();
        foreach (Light light in lights)
        {
            if (light.type == LightType.Directional) {
                light.enabled = false;
            }
        }
    }

    private void BuildDojo()
    {
        Material floorMaterial = CreateMaterial("Dojo Floor Material", new Color(0.36f, 0.32f, 0.27f));
        Material wallMaterial = CreateMaterial("Dojo Wall Material", new Color(0.45f, 0.28f, 0.16f));
        Material trimMaterial = CreateMaterial("Dojo Trim Material", new Color(0.11f, 0.065f, 0.035f));
        Material pipeMaterial = CreateMaterial("Fruit Pipe Material", new Color(0.34f, 0.31f, 0.28f));
        Material ringMaterial = CreateMaterial("Slice Ring Material", new Color(0.42f, 0.25f, 0.08f));

        CreateCube("Dojo Stone Floor", new Vector3(0f, -0.65f, -3.0f), new Vector3(14f, 0.22f, 10f), floorMaterial);
        CreateCube("Dojo Back Gate", new Vector3(0f, 2.65f, -2.25f), new Vector3(7.2f, 5.6f, 0.28f), wallMaterial);
        CreateCube("Dojo Left Screen", new Vector3(-4.8f, 2.25f, -3.7f), new Vector3(0.24f, 5.2f, 4.8f), wallMaterial);
        CreateCube("Dojo Right Screen", new Vector3(4.8f, 2.25f, -3.7f), new Vector3(0.24f, 5.2f, 4.8f), wallMaterial);
        CreateCube("Player Rail", new Vector3(0f, -0.24f, -5.2f), new Vector3(5.8f, 0.25f, 0.35f), trimMaterial);

        BuildPipeBank(pipeMaterial, ringMaterial);

        CreateCube("Slice Zone Top", playPlaneCenter + new Vector3(0f, 2.05f, 0.18f), new Vector3(5.8f, 0.06f, 0.06f), ringMaterial);
        CreateCube("Slice Zone Bottom", playPlaneCenter + new Vector3(0f, -1.15f, 0.18f), new Vector3(5.8f, 0.06f, 0.06f), ringMaterial);
        CreateCube("Slice Zone Left", playPlaneCenter + new Vector3(-2.9f, 0.45f, 0.18f), new Vector3(0.06f, 3.2f, 0.06f), ringMaterial);
        CreateCube("Slice Zone Right", playPlaneCenter + new Vector3(2.9f, 0.45f, 0.18f), new Vector3(0.06f, 3.2f, 0.06f), ringMaterial);

        CreatePointLight("Soft Dojo Fill Light", new Vector3(0f, 2.7f, -5.0f), 0.85f, 9f, new Color(1f, 0.86f, 0.68f));
    }

    private void HideLegacyBackground()
    {
        GameObject legacyBackground = GameObject.Find("Background");
        if (legacyBackground != null) {
            legacyBackground.SetActive(false);
        }
    }

    private void BuildPipeBank(Material pipeMaterial, Material rimMaterial)
    {
        const int pipeCount = 9;
        const float spacing = 0.78f;
        float startX = -(pipeCount - 1) * spacing * 0.5f;

        for (int i = 0; i < pipeCount; i++)
        {
            float x = startX + spacing * i;
            float curve = Mathf.Abs(i - (pipeCount - 1) * 0.5f) * 0.04f;
            Vector3 pipePosition = new Vector3(x * 0.78f, -0.32f - curve, -4.85f + curve);
            CreateCylinder($"Fruit Pipe {i + 1}", pipePosition, Quaternion.Euler(90f, 0f, 0f), new Vector3(0.2f, 0.22f, 0.2f), pipeMaterial);
            CreateCylinder($"Fruit Pipe Rim {i + 1}", pipePosition + new Vector3(0f, 0.12f, -0.02f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.25f, 0.03f, 0.25f), rimMaterial);
        }
    }

    private void PositionSpawner()
    {
        if (spawner == null) {
            spawner = FindObjectOfType<Spawner>();
        }

        if (spawner == null)
            return;

        spawner.transform.position = new Vector3(0f, -0.28f, -2.85f);
        spawner.transform.localScale = Vector3.one;

        BoxCollider box = spawner.GetComponent<BoxCollider>();
        if (box != null) {
            box.size = new Vector3(4.8f, 0.12f, 0.55f);
            box.center = Vector3.zero;
        }
    }

    private static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.position = position;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().material = material;
        return obj;
    }

    private static GameObject CreateCylinder(string name, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        obj.name = name;
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().material = material;
        return obj;
    }

    private static void CreateSpotLight(string name, Vector3 position, Quaternion rotation, float intensity, float angle)
    {
        GameObject obj = new GameObject(name);
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        Light light = obj.AddComponent<Light>();
        light.type = LightType.Spot;
        light.intensity = intensity;
        light.spotAngle = angle;
        light.range = 18f;
        light.color = new Color(1f, 0.74f, 0.46f);
    }

    private static void CreatePointLight(string name, Vector3 position, float intensity, float range, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.position = position;
        Light light = obj.AddComponent<Light>();
        light.type = LightType.Point;
        light.intensity = intensity;
        light.range = range;
        light.color = color;
    }

    private static Material CreateMaterial(string name, Color color)
    {
        Material material = new Material(Shader.Find("Standard"));
        material.name = name;
        material.color = color;
        return material;
    }
}
