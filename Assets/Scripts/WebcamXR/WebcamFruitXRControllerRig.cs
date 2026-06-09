using UnityEngine;

namespace WebcamXR
{
    public class WebcamFruitXRControllerRig : MonoBehaviour
    {
        [SerializeField] private WebcamTrackingReceiver receiver;
        [SerializeField] private Vector3 rightNeutral = new Vector3(0.34f, 1.25f, 0.72f);
        [SerializeField] private Vector3 leftNeutral = new Vector3(-0.34f, 1.25f, 0.72f);
        [SerializeField] private Vector3 rightRestWorld = new Vector3(0.42f, 0.7f, -4.85f);
        [SerializeField] private Vector3 leftRestWorld = new Vector3(-0.42f, 0.7f, -4.85f);
        [SerializeField] private Vector3 playCenter = new Vector3(0f, 1.55f, -4.85f);
        [SerializeField] private float horizontalScale = 18f;
        [SerializeField] private float verticalScale = 13f;
        [SerializeField] private float positionSmoothing = 10f;
        [SerializeField] private float sliceForce = 5f;
        [SerializeField] private Vector3 sliceColliderSize = new Vector3(0.72f, 0.72f, 3.2f);
        [SerializeField] private bool mouseControlsRightControllerWhenUntracked = true;
        [SerializeField] private float minSliceSpeed = 0.15f;

        private HandControllerRuntime leftController;
        private HandControllerRuntime rightController;
        private Camera mainCamera;

        private void Awake()
        {
            if (receiver == null) {
                receiver = FindObjectOfType<WebcamTrackingReceiver>();
            }

            if (receiver == null)
            {
                GameObject receiverObject = new GameObject("Webcam Tracking Receiver");
                receiver = receiverObject.AddComponent<WebcamTrackingReceiver>();
            }

            mainCamera = Camera.main;
            leftController = CreateController("Left XR Controller", leftRestWorld, new Color(0.02f, 0.13f, 0.28f));
            rightController = CreateController("Right XR Controller", rightRestWorld, new Color(0.02f, 0.05f, 0.12f));
        }

        private void Update()
        {
            if (receiver == null || !receiver.HasFreshFrame)
            {
                SetControllerAtRest(leftController, leftRestWorld);
                UpdateRightControllerFromMouse();
                return;
            }

            TrackingFrameV1 frame = receiver.LatestFrame;
            if (frame == null || !frame.calibrated)
            {
                SetControllerAtRest(leftController, leftRestWorld);
                UpdateRightControllerFromMouse();
                return;
            }

            UpdateController(leftController, frame.left, leftNeutral, leftRestWorld);
            UpdateController(rightController, frame.right, rightNeutral, rightRestWorld);
        }

        private void UpdateRightControllerFromMouse()
        {
            if (!mouseControlsRightControllerWhenUntracked || rightController == null)
            {
                SetControllerAtRest(rightController, rightRestWorld);
                return;
            }

            Vector3 targetWorld = MouseToWorld(rightRestWorld.z);
            Vector3 direction = targetWorld - rightController.previousWorldPosition;
            rightController.root.transform.position = targetWorld;
            rightController.root.transform.rotation = Quaternion.LookRotation(Vector3.forward, direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.up);
            rightController.controller.ApplyPose(true, rightController.root.transform.localPosition, rightController.root.transform.localRotation);
            rightController.controller.ApplySelect(Input.GetMouseButton(0), Input.GetMouseButton(0) ? 1f : 0f);
            rightController.sliceSource.SetDirection(direction);
            rightController.collider.enabled = Input.GetMouseButton(0);
            rightController.worldPosition = targetWorld;
            rightController.previousWorldPosition = targetWorld;
            rightController.hasPose = true;
        }

        private Vector3 MouseToWorld(float planeZ)
        {
            if (mainCamera == null) {
                mainCamera = Camera.main;
            }

            if (mainCamera == null)
                return rightRestWorld;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.back, new Vector3(0f, 0f, planeZ));
            if (plane.Raycast(ray, out float enter)) {
                return ray.GetPoint(enter);
            }

            return rightRestWorld;
        }

        private void UpdateController(HandControllerRuntime runtime, HandTrackingData hand, Vector3 neutral, Vector3 restWorld)
        {
            bool tracked = hand != null && hand.tracked;
            if (!tracked)
            {
                SetTracked(runtime, false);
                return;
            }

            Vector3 targetWorld = HandToWorld(hand.PositionVector(neutral), neutral, restWorld);
            Vector3 direction = targetWorld - runtime.previousWorldPosition;

            if (!runtime.hasPose)
            {
                runtime.worldPosition = targetWorld;
                runtime.previousWorldPosition = targetWorld;
                runtime.hasPose = true;
            }

            float factor = 1f - Mathf.Exp(-positionSmoothing * Time.deltaTime);
            runtime.worldPosition = Vector3.Lerp(runtime.worldPosition, targetWorld, factor);
            direction = runtime.worldPosition - runtime.previousWorldPosition;

            Vector3 bladeDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.up;
            Quaternion rotation = Quaternion.LookRotation(Vector3.forward, bladeDirection);

            runtime.root.transform.position = runtime.worldPosition;
            runtime.root.transform.rotation = rotation;
            runtime.controller.ApplyPose(true, runtime.root.transform.localPosition, runtime.root.transform.localRotation);
            runtime.controller.ApplySelect(hand.pinch, hand.pinch_strength);
            runtime.sliceSource.SetDirection(direction);
            float speed = Time.deltaTime > 0f ? direction.magnitude / Time.deltaTime : 0f;
            runtime.collider.enabled = speed >= minSliceSpeed;
            runtime.previousWorldPosition = runtime.worldPosition;
        }

        private Vector3 HandToWorld(Vector3 handPosition, Vector3 neutral, Vector3 restWorld)
        {
            Vector3 offset = handPosition - neutral;
            return new Vector3(
                playCenter.x + offset.x * horizontalScale,
                playCenter.y + offset.y * verticalScale,
                restWorld.z);
        }

        private void SetTracked(HandControllerRuntime runtime, bool tracked)
        {
            if (runtime == null)
                return;

            runtime.controller.ApplyPose(tracked, runtime.root.transform.localPosition, runtime.root.transform.localRotation);
            runtime.controller.ClearInput();
            runtime.collider.enabled = tracked;
            runtime.hasPose = tracked && runtime.hasPose;
        }

        private void SetControllerAtRest(HandControllerRuntime runtime, Vector3 restWorld)
        {
            if (runtime == null)
                return;

            runtime.root.transform.position = restWorld;
            runtime.root.transform.rotation = Quaternion.identity;
            runtime.controller.ApplyPose(true, runtime.root.transform.localPosition, runtime.root.transform.localRotation);
            runtime.controller.ClearInput();
            runtime.collider.enabled = false;
            runtime.worldPosition = restWorld;
            runtime.previousWorldPosition = restWorld;
            runtime.hasPose = false;
            runtime.sliceSource.SetDirection(Vector3.up);
        }

        private HandControllerRuntime CreateController(string name, Vector3 restWorld, Color color)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(transform, false);
            root.transform.position = restWorld;

            WebcamXRController controller = root.AddComponent<WebcamXRController>();
            FruitSaberSliceSource sliceSource = root.AddComponent<FruitSaberSliceSource>();
            sliceSource.sliceForce = sliceForce;

            BoxCollider sliceCollider = root.AddComponent<BoxCollider>();
            sliceCollider.isTrigger = true;
            sliceCollider.size = sliceColliderSize;
            sliceCollider.center = new Vector3(0f, 0.52f, 0.85f);
            sliceCollider.enabled = false;
            root.tag = "Player";

            CreateControllerModel(root.transform, color);

            return new HandControllerRuntime
            {
                root = root,
                controller = controller,
                sliceSource = sliceSource,
                collider = sliceCollider,
                worldPosition = restWorld,
                previousWorldPosition = restWorld
            };
        }

        private static void CreateControllerModel(Transform parent, Color color)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.name = parent.name + " Saber Material";
            material.color = color;

            GameObject modelRoot = new GameObject("Controller Model - Saber");
            modelRoot.transform.SetParent(parent, false);
            modelRoot.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);

            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handle.name = "XR Controller Handle";
            handle.transform.SetParent(modelRoot.transform, false);
            handle.transform.localPosition = new Vector3(0f, -0.22f, -0.05f);
            handle.transform.localScale = new Vector3(0.065f, 0.24f, 0.065f);
            handle.GetComponent<Renderer>().material = material;
            Destroy(handle.GetComponent<Collider>());

            GameObject guard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            guard.name = "Saber Guard";
            guard.transform.SetParent(modelRoot.transform, false);
            guard.transform.localPosition = new Vector3(0f, 0.04f, -0.02f);
            guard.transform.localScale = new Vector3(0.3f, 0.045f, 0.075f);
            guard.GetComponent<Renderer>().material = material;
            Destroy(guard.GetComponent<Collider>());

            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            blade.name = "Saber Blade";
            blade.transform.SetParent(modelRoot.transform, false);
            blade.transform.localPosition = new Vector3(0f, 0.58f, 0.18f);
            blade.transform.localScale = new Vector3(0.023f, 0.68f, 0.023f);
            blade.GetComponent<Renderer>().material = material;
            Destroy(blade.GetComponent<Collider>());
        }

        private class HandControllerRuntime
        {
            public GameObject root;
            public WebcamXRController controller;
            public FruitSaberSliceSource sliceSource;
            public BoxCollider collider;
            public Vector3 worldPosition;
            public Vector3 previousWorldPosition;
            public bool hasPose;
        }
    }
}
