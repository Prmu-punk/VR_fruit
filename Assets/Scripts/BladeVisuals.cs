using UnityEngine;

public class BladeVisuals : MonoBehaviour
{
    [SerializeField] private Transform rightBlade;
    [SerializeField] private Transform leftBlade;
    [SerializeField] private Vector3 rightViewportIdle = new Vector3(0.62f, 0.08f, 1.05f);
    [SerializeField] private Vector3 leftViewportIdle = new Vector3(0.38f, 0.08f, 1.05f);

    private Material rightMaterial;
    private Material leftMaterial;
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
        rightMaterial = CreateMaterial("Right Saber Material", new Color(0.02f, 0.05f, 0.12f));
        leftMaterial = CreateMaterial("Left Saber Material", new Color(0.02f, 0.13f, 0.28f));

        rightBlade = CreateBlade("Right VR Saber", rightMaterial, ViewportToWorld(rightViewportIdle));
        leftBlade = CreateBlade("Left VR Saber", leftMaterial, ViewportToWorld(leftViewportIdle));
    }

    public void SetRightBladePose(Vector3 bladeTip, bool active)
    {
        SetRightBladePose(bladeTip, active, true);
    }

    public void SetBladePoses(Vector3 rightBladeTip, bool rightActive, Vector3 leftBladeTip, bool leftActive)
    {
        SetRightBladePose(rightBladeTip, rightActive, false);
        SetLeftBladePose(leftBladeTip, leftActive);
    }

    private void SetRightBladePose(Vector3 bladeTip, bool active, bool mirrorLeft)
    {
        if (rightBlade == null)
            return;

        Vector3 idle = ViewportToWorld(rightViewportIdle);
        Vector3 handle = Vector3.Lerp(idle, bladeTip + new Vector3(0.18f, -0.62f, -0.45f), active ? 0.42f : 0.08f);
        Vector3 direction = bladeTip - handle;
        if (direction.sqrMagnitude < 0.001f) {
            direction = Vector3.up;
        }

        rightBlade.position = handle + direction.normalized * 0.62f;
        rightBlade.rotation = Quaternion.LookRotation(Vector3.forward, direction.normalized);

        if (mirrorLeft) {
            Vector3 mirroredTip = new Vector3(-bladeTip.x, bladeTip.y * 0.65f - 0.5f, bladeTip.z - 0.25f);
            SetLeftBladePose(mirroredTip, active);
        }
    }

    private void SetLeftBladePose(Vector3 bladeTip, bool active)
    {
        if (leftBlade == null)
            return;

        Vector3 idle = ViewportToWorld(leftViewportIdle);
        Vector3 handle = Vector3.Lerp(idle, bladeTip + new Vector3(-0.18f, -0.62f, -0.45f), active ? 0.42f : 0.08f);
        Vector3 direction = bladeTip - handle;
        if (direction.sqrMagnitude < 0.001f) {
            direction = Vector3.up;
        }

        leftBlade.position = handle + direction.normalized * 0.62f;
        leftBlade.rotation = Quaternion.LookRotation(Vector3.forward, direction.normalized);
    }

    private Transform CreateBlade(string name, Material material, Vector3 idlePosition)
    {
        GameObject root = new GameObject(name);
        root.transform.position = idlePosition;

        GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shaft.name = "Saber Shaft";
        shaft.transform.SetParent(root.transform, false);
        shaft.transform.localScale = new Vector3(0.028f, 0.68f, 0.028f);
        shaft.transform.localPosition = Vector3.zero;
        shaft.GetComponent<Renderer>().material = material;
        Destroy(shaft.GetComponent<Collider>());

        GameObject guard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        guard.name = "Saber Guard";
        guard.transform.SetParent(root.transform, false);
        guard.transform.localPosition = new Vector3(0f, -0.58f, 0f);
        guard.transform.localScale = new Vector3(0.28f, 0.045f, 0.075f);
        guard.GetComponent<Renderer>().material = material;
        Destroy(guard.GetComponent<Collider>());

        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        handle.name = "Saber Handle";
        handle.transform.SetParent(root.transform, false);
        handle.transform.localPosition = new Vector3(0f, -0.78f, 0f);
        handle.transform.localScale = new Vector3(0.055f, 0.2f, 0.055f);
        handle.GetComponent<Renderer>().material = material;
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

    private static Material CreateMaterial(string name, Color color)
    {
        Material material = new Material(Shader.Find("Standard"));
        material.name = name;
        material.color = color;
        return material;
    }
}
