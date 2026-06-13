using UnityEngine;

public class BladeVisuals : MonoBehaviour
{
    [SerializeField] private Transform rightBlade;
    [SerializeField] private Transform leftBlade;
    [SerializeField] private Vector3 rightViewportIdle = new Vector3(0.62f, 0.08f, 1.05f);
    [SerializeField] private Vector3 leftViewportIdle = new Vector3(0.38f, 0.08f, 1.05f);
    [SerializeField] private Vector3 bladeRootOffset = new Vector3(0f, -0.12f, 0f);

    private Material bladeMaterial;
    private Material handleMaterial;
    private Material guardMaterial;
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
        bladeMaterial = CreateBladeMaterial();
        handleMaterial = CreateSimpleMaterial("Fruit Knife Handle Material", new Color(0.055f, 0.05f, 0.045f), 0.28f, 0.25f);
        guardMaterial = CreateSimpleMaterial("Fruit Knife Guard Material", new Color(0.45f, 0.42f, 0.36f), 0.72f, 0.46f);

        rightBlade = CreateBlade("Right VR Fruit Knife", ViewportToWorld(rightViewportIdle));
        leftBlade = CreateBlade("Left VR Fruit Knife", ViewportToWorld(leftViewportIdle));
    }

    public void SetRightBladePose(Vector3 bladeTip, bool active)
    {
        SetRightBladePose(bladeTip, Quaternion.identity, active);
    }

    public void SetRightBladePose(Vector3 bladeTip, Quaternion rotation, bool active)
    {
        SetRightBladePose(bladeTip, rotation, active, true);
    }

    public void SetBladePoses(Vector3 rightBladeTip, bool rightActive, Vector3 leftBladeTip, bool leftActive)
    {
        SetBladePoses(rightBladeTip, Quaternion.identity, rightActive, leftBladeTip, Quaternion.identity, leftActive);
    }

    public void SetBladePoses(Vector3 rightBladeTip, Quaternion rightRotation, bool rightActive, Vector3 leftBladeTip, Quaternion leftRotation, bool leftActive)
    {
        SetRightBladePose(rightBladeTip, rightRotation, rightActive, false);
        SetLeftBladePose(leftBladeTip, leftRotation, leftActive);
    }

    private void SetRightBladePose(Vector3 bladeTip, Quaternion rotation, bool active, bool mirrorLeft)
    {
        if (rightBlade == null)
            return;

        if (active) {
            rightBlade.position = bladeTip + rotation * bladeRootOffset;
            rightBlade.rotation = rotation == Quaternion.identity ? rightBlade.rotation : rotation;
        } else {
            Vector3 idle = ViewportToWorld(rightViewportIdle);
            rightBlade.position = Vector3.Lerp(rightBlade.position, idle, 0.08f);
        }

        if (mirrorLeft) {
            Vector3 mirroredTip = new Vector3(-bladeTip.x, bladeTip.y * 0.65f - 0.5f, bladeTip.z - 0.25f);
            SetLeftBladePose(mirroredTip, rotation, active);
        }
    }

    private void SetLeftBladePose(Vector3 bladeTip, bool active)
    {
        SetLeftBladePose(bladeTip, Quaternion.identity, active);
    }

    private void SetLeftBladePose(Vector3 bladeTip, Quaternion rotation, bool active)
    {
        if (leftBlade == null)
            return;

        if (active) {
            leftBlade.position = bladeTip + rotation * bladeRootOffset;
            leftBlade.rotation = rotation == Quaternion.identity ? leftBlade.rotation : rotation;
        } else {
            Vector3 idle = ViewportToWorld(leftViewportIdle);
            leftBlade.position = Vector3.Lerp(leftBlade.position, idle, 0.08f);
        }
    }

    private Transform CreateBlade(string name, Vector3 idlePosition)
    {
        GameObject root = new GameObject(name);
        root.transform.position = idlePosition;

        GameObject blade = new GameObject("Flat Fruit Knife Blade");
        blade.transform.SetParent(root.transform, false);
        blade.transform.localPosition = new Vector3(0f, 0.045f, 0f);
        MeshFilter meshFilter = blade.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = CreateFlatBladeMesh();
        MeshRenderer meshRenderer = blade.AddComponent<MeshRenderer>();
        meshRenderer.material = bladeMaterial;

        GameObject spine = GameObject.CreatePrimitive(PrimitiveType.Cube);
        spine.name = "Fruit Knife Dull Spine";
        spine.transform.SetParent(root.transform, false);
        spine.transform.localPosition = new Vector3(-0.022f, 0.09f, 0f);
        spine.transform.localScale = new Vector3(0.01f, 0.25f, 0.01f);
        spine.GetComponent<Renderer>().material = guardMaterial;
        Destroy(spine.GetComponent<Collider>());

        GameObject guard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        guard.name = "Fruit Knife Guard";
        guard.transform.SetParent(root.transform, false);
        guard.transform.localPosition = new Vector3(0f, -0.105f, 0f);
        guard.transform.localScale = new Vector3(0.095f, 0.016f, 0.032f);
        guard.GetComponent<Renderer>().material = guardMaterial;
        Destroy(guard.GetComponent<Collider>());

        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        handle.name = "Fruit Knife Handle";
        handle.transform.SetParent(root.transform, false);
        handle.transform.localPosition = new Vector3(0f, -0.175f, 0f);
        handle.transform.localScale = new Vector3(0.044f, 0.115f, 0.034f);
        handle.GetComponent<Renderer>().material = handleMaterial;
        Destroy(handle.GetComponent<Collider>());

        return root.transform;
    }

    private Vector3 ViewportToWorld(Vector3 viewportPosition)
    {
        if (mainCamera == null) {
            mainCamera = Camera.main;
        }

        return mainCamera != null ? mainCamera.ViewportToWorldPoint(viewportPosition) : Vector3.zero;
    }

    private static Mesh CreateFlatBladeMesh()
    {
        float baseY = -0.06f;
        float tipY = 0.25f;
        float baseHalfWidth = 0.029f;
        float tipHalfWidth = 0.006f;
        float halfThickness = 0.0038f;

        Vector3[] vertices =
        {
            new Vector3(-baseHalfWidth, baseY, -halfThickness),
            new Vector3(baseHalfWidth, baseY, -halfThickness),
            new Vector3(tipHalfWidth, tipY, -halfThickness),
            new Vector3(-tipHalfWidth, tipY, -halfThickness),
            new Vector3(-baseHalfWidth, baseY, halfThickness),
            new Vector3(baseHalfWidth, baseY, halfThickness),
            new Vector3(tipHalfWidth, tipY, halfThickness),
            new Vector3(-tipHalfWidth, tipY, halfThickness),
        };

        int[] triangles =
        {
            0, 2, 1, 0, 3, 2,
            4, 5, 6, 4, 6, 7,
            0, 4, 7, 0, 7, 3,
            1, 2, 6, 1, 6, 5,
            3, 7, 6, 3, 6, 2,
            0, 1, 5, 0, 5, 4,
        };

        Mesh mesh = new Mesh();
        mesh.name = "Runtime Flat Fruit Knife Blade Mesh";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Material CreateBladeMaterial()
    {
        Material material = CreateSimpleMaterial("Fruit Knife Blade Material", new Color(0.82f, 0.9f, 0.94f), 0.88f, 0.58f);
        return material;
    }

    private static Material CreateSimpleMaterial(string name, Color color, float metallic, float smoothness)
    {
        Material material = new Material(Shader.Find("Standard"));
        material.name = name;
        material.color = color;
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Glossiness", smoothness);
        return material;
    }
}
