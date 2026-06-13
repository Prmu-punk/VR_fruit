using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DojoSceneBuilder : MonoBehaviour
{
    [SerializeField] private Spawner spawner;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Vector3 desktopCameraPosition = new Vector3(0f, 0f, -20f);
    [SerializeField] private Quaternion desktopCameraRotation = Quaternion.identity;
    [SerializeField] private float desktopOrthographicSize = 10f;

    [Header("Legacy Background Board")]
    [SerializeField] private string backgroundName = "Background";
    [SerializeField] private bool hideLegacyBackground = true;

    [Header("Complete Environment Scene")]
    [SerializeField] private bool loadCompleteEnvironmentScene = true;
    [SerializeField] private string environmentScenePath = "Assets/Stylized Asia RG/Scenes/Demo Stylized Asia.unity";
    [SerializeField] private Vector3 environmentViewPosition = new Vector3(0f, 1.6f, 4.85f);
    [SerializeField] private Vector3 environmentViewEulerAngles = new Vector3(0f, -60f, 0f);
    [SerializeField] private float environmentPlayerShiftDistance = 46f;
    [SerializeField] private float environmentReferenceGroundY = 9.9f;
    [SerializeField] private float environmentTargetGroundY = -3.5f;
    [SerializeField] private float environmentScale = 1f;
    [SerializeField] private float environmentCullRadius = 42f;
    [SerializeField] private float vrNearClipPlane = 0.01f;

    private bool built;
    private static readonly string[] GeneratedDojoObjectNames =
    {
        "Dojo Stone Floor",
        "Arcade Dojo Root",
        "Dojo Back Gate",
        "Dojo Left Screen",
        "Dojo Right Screen",
        "Player Rail",
        "Slice Zone Top",
        "Slice Zone Bottom",
        "Slice Zone Left",
        "Slice Zone Right",
        "Soft Dojo Fill Light"
    };

    private void Awake()
    {
        BuildNow();
    }

    public void BuildNow()
    {
        if (built)
            return;

        built = true;

        RemoveGeneratedDojo();
        RestoreLegacyCamera();
        ConfigureStaticBackground();
        LoadEnvironmentScene();
        PositionSpawner();
    }

    private void RestoreLegacyCamera()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return;

        targetCamera.clearFlags = CameraClearFlags.SolidColor;
        targetCamera.backgroundColor = new Color(0.56f, 0.72f, 0.9f, 0f);

        if (!UnityEngine.XR.XRSettings.enabled)
        {
            targetCamera.orthographic = true;
            targetCamera.orthographicSize = desktopOrthographicSize;
            targetCamera.transform.SetPositionAndRotation(desktopCameraPosition, desktopCameraRotation);
        }
        else
        {
            targetCamera.orthographic = false;
            targetCamera.nearClipPlane = vrNearClipPlane;
        }
    }

    private void ConfigureStaticBackground()
    {
        GameObject background = GameObject.Find(backgroundName);
        if (background == null)
            return;

        if (hideLegacyBackground)
            background.SetActive(false);

        MeshCollider meshCollider = background.GetComponent<MeshCollider>();
        if (meshCollider != null)
            meshCollider.enabled = false;
    }

    private void PositionSpawner()
    {
        if (spawner == null)
            spawner = FindObjectOfType<Spawner>();

        if (spawner == null)
            return;

        spawner.transform.position = new Vector3(0f, 0.6f, -3.25f);
        spawner.transform.localScale = Vector3.one;

        BoxCollider box = spawner.GetComponent<BoxCollider>();
        if (box != null)
        {
            box.size = new Vector3(0.95f, 0.08f, 0.25f);
            box.center = Vector3.zero;
        }
    }

    private void LoadEnvironmentScene()
    {
        if (!loadCompleteEnvironmentScene || string.IsNullOrEmpty(environmentScenePath))
            return;

        Scene existingScene = SceneManager.GetSceneByPath(environmentScenePath);
        if (existingScene.isLoaded)
            return;

        StartCoroutine(LoadEnvironmentSceneRoutine());
    }

    private IEnumerator LoadEnvironmentSceneRoutine()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(environmentScenePath, LoadSceneMode.Additive);
        if (operation == null)
        {
            Debug.LogWarning($"Could not load environment scene: {environmentScenePath}", this);
            yield break;
        }

        while (!operation.isDone)
            yield return null;

        Scene environmentScene = SceneManager.GetSceneByPath(environmentScenePath);
        if (!environmentScene.IsValid())
            yield break;

        Camera referenceCamera = null;
        foreach (GameObject rootObject in environmentScene.GetRootGameObjects())
        {
            Camera[] cameras = rootObject.GetComponentsInChildren<Camera>(true);
            if (cameras.Length > 0 && referenceCamera == null)
                referenceCamera = cameras[0];
        }

        GameObject environmentRoot = new GameObject("Complete Environment Root");
        Transform environmentRootTransform = environmentRoot.transform;

        Quaternion targetRotation = Quaternion.Euler(0f, environmentViewEulerAngles.y, 0f);
        if (referenceCamera != null)
        {
            Quaternion referenceYaw = Quaternion.Euler(0f, referenceCamera.transform.eulerAngles.y, 0f);
            environmentRootTransform.rotation = targetRotation * Quaternion.Inverse(referenceYaw);
            Vector3 referenceGroundPoint = new Vector3(referenceCamera.transform.position.x, environmentReferenceGroundY, referenceCamera.transform.position.z);
            Vector3 targetGroundPoint = GetShiftedEnvironmentGroundPoint();
            environmentRootTransform.position = targetGroundPoint - environmentRootTransform.rotation * (referenceGroundPoint * environmentScale);
        }
        else
        {
            environmentRootTransform.position = GetShiftedEnvironmentGroundPoint();
            environmentRootTransform.rotation = targetRotation;
        }

        environmentRootTransform.localScale = Vector3.one * environmentScale;

        foreach (GameObject rootObject in environmentScene.GetRootGameObjects())
        {
            if (rootObject == environmentRoot)
                continue;

            rootObject.transform.SetParent(environmentRootTransform, false);

            foreach (Camera camera in rootObject.GetComponentsInChildren<Camera>(true))
                camera.enabled = false;

            foreach (AudioListener listener in rootObject.GetComponentsInChildren<AudioListener>(true))
                listener.enabled = false;
        }

        CullEnvironment(environmentRootTransform);
    }

    private Vector3 GetShiftedEnvironmentGroundPoint()
    {
        Vector3 shiftDirection = Vector3.left;
        Vector3 shiftedPosition = environmentViewPosition - shiftDirection * environmentPlayerShiftDistance;
        return new Vector3(shiftedPosition.x, environmentTargetGroundY, shiftedPosition.z);
    }

    private void CullEnvironment(Transform environmentRoot)
    {
        if (environmentRoot == null)
            return;

        DisableEnvironmentParticles(environmentRoot);
        Vector3 playerGround = new Vector3(0f, environmentTargetGroundY, 0f);
        for (int i = environmentRoot.childCount - 1; i >= 0; i--)
            CullEnvironmentBranch(environmentRoot.GetChild(i), playerGround);
    }

    private void DisableEnvironmentParticles(Transform environmentRoot)
    {
        ParticleSystem[] particles = environmentRoot.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem particle in particles)
        {
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.EmissionModule emission = particle.emission;
            emission.enabled = false;
            Destroy(particle.gameObject);
        }
    }

    private bool CullEnvironmentBranch(Transform node, Vector3 playerGround)
    {
        if (node == null)
            return false;

        if (IsUtilityEnvironmentObject(node.gameObject))
            return true;

        bool hasKeptChild = false;
        for (int i = node.childCount - 1; i >= 0; i--)
        {
            if (CullEnvironmentBranch(node.GetChild(i), playerGround))
                hasKeptChild = true;
        }

        bool hasNearRenderer = HasDirectRendererInCullRadius(node, playerGround);
        if (hasKeptChild || hasNearRenderer)
            return true;

        Destroy(node.gameObject);
        return false;
    }

    private bool HasDirectRendererInCullRadius(Transform node, Vector3 playerGround)
    {
        Renderer[] renderers = node.GetComponents<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled)
                continue;

            if (HorizontalDistanceToBounds(renderer.bounds, playerGround) <= environmentCullRadius)
                return true;
        }

        return false;
    }

    private static float HorizontalDistanceToBounds(Bounds bounds, Vector3 point)
    {
        float dx = Mathf.Max(Mathf.Abs(point.x - bounds.center.x) - bounds.extents.x, 0f);
        float dz = Mathf.Max(Mathf.Abs(point.z - bounds.center.z) - bounds.extents.z, 0f);
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    private static bool IsUtilityEnvironmentObject(GameObject gameObject)
    {
        return gameObject.GetComponent<Light>() != null
            || gameObject.GetComponent<ReflectionProbe>() != null;
    }

    private static void RemoveGeneratedDojo()
    {
        foreach (string objectName in GeneratedDojoObjectNames)
        {
            GameObject generatedObject = GameObject.Find(objectName);
            if (generatedObject != null)
                Destroy(generatedObject);
        }
    }
}
