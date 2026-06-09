using UnityEngine;
using WebcamXR;
using UnityEngine.XR;

public class Blade : MonoBehaviour, IBladeSliceSource
{
    public float sliceForce = 5f;
    public float minSliceVelocity = 0.01f;
    [Header("Webcam XR")]
    [SerializeField] private bool useWebcamTracking = false;
    [SerializeField] private bool preferRightHand = true;
    [SerializeField] private Vector3 webcamSlicePlaneCenter = new Vector3(0f, 1.55f, -4.85f);
    [SerializeField] private Vector3 webcamRightNeutral = new Vector3(0.34f, 1.25f, 0.72f);
    [SerializeField] private Vector3 webcamLeftNeutral = new Vector3(-0.34f, 1.25f, 0.72f);
    [SerializeField] private float webcamHorizontalScale = 32.0f;
    [SerializeField] private float webcamVerticalScale = 22.0f;
    [SerializeField] private float webcamPositionSmoothing = 8f;
    [SerializeField] private float webcamDeadZone = 0.0f;
    [SerializeField] private float webcamMaxSpeed = 1000f;
    [Header("XR Controller")]
    [SerializeField] private bool useXRControllers = true;
    [SerializeField] private bool requireXRTriggerToSlice = false;
    [SerializeField] private Vector3 xrSlicePlaneCenter = new Vector3(0f, 1.55f, -4.85f);
    [SerializeField] private Vector3 xrRightNeutral = new Vector3(0.28f, 1.1f, 0.55f);
    [SerializeField] private Vector3 xrLeftNeutral = new Vector3(-0.28f, 1.1f, 0.55f);
    [SerializeField] private float xrHorizontalScale = 3.2f;
    [SerializeField] private float xrVerticalScale = 3.0f;
    [SerializeField] private float desktopSlicePlaneZ = -4.85f;
    [SerializeField] private Vector3 sliceCollider3DSize = new Vector3(0.9f, 0.9f, 3.6f);

    private Camera mainCamera;
    private BoxCollider sliceCollider;
    private BoxCollider leftSliceCollider;
    private GameObject leftSliceObject;
    private TrailRenderer sliceTrail;
    private WebcamTrackingReceiver webcamReceiver;
    private BladeVisuals bladeVisuals;
    private bool webcamSliceActive;
    private bool xrSliceActive;
    private Vector3 smoothedWebcamBladePosition;
    private Vector3 smoothedWebcamLeftBladePosition;
    private bool hasSmoothedWebcamBladePosition;
    private bool hasSmoothedWebcamLeftBladePosition;
    private Vector3 leftBladeDirection;
    private Vector3 previousLeftBladePosition;
    private bool hasPreviousLeftBladePosition;
    private InputDevice rightXRDevice;
    private InputDevice leftXRDevice;
    private Vector3 lastXRLeftBladePosition;
    private bool hasXRLeftBladePosition;
    private bool bladeVisualsDrivenThisFrame;

    public Vector3 direction { get; private set; }
    public bool slicing { get; private set; }
    public Vector3 Direction => direction;
    public float SliceForce => sliceForce;
    public Vector3 Position => transform.position;

    private void Awake()
    {
        mainCamera = Camera.main;
        ConfigureSliceCollider();
        ConfigureLeftSliceCollider();
        sliceTrail = GetComponentInChildren<TrailRenderer>();
        bladeVisuals = FindObjectOfType<BladeVisuals>();

        webcamReceiver = FindObjectOfType<WebcamTrackingReceiver>();

        if (webcamReceiver == null)
        {
            GameObject receiverObject = new GameObject("Webcam Tracking Receiver");
            webcamReceiver = receiverObject.AddComponent<WebcamTrackingReceiver>();
        }
    }

    private void OnEnable()
    {
        StopSlice();
    }

    private void OnDisable()
    {
        StopSlice();
    }

    private void Update()
    {
        bladeVisualsDrivenThisFrame = false;

        if (TryUpdateXRControllerSlice()) {
            return;
        }

        if (xrSliceActive) {
            StopSlice();
            xrSliceActive = false;
        }

        if (TryUpdateWebcamSlice()) {
            return;
        }

        if (webcamSliceActive) {
            StopSlice();
            webcamSliceActive = false;
        }

        if (Input.GetMouseButtonDown(0)) {
            StartSlice();
        } else if (Input.GetMouseButtonUp(0)) {
            StopSlice();
        } else if (slicing) {
            ContinueSlice();
        }
    }

    private void StartSlice()
    {
        Vector3 position = PointerToBladeWorldPosition(Input.mousePosition);
        transform.position = position;

        slicing = true;
        sliceCollider.enabled = true;
        sliceTrail.enabled = true;
        sliceTrail.Clear();
    }

    private void StopSlice()
    {
        slicing = false;
        sliceCollider.enabled = false;
        sliceTrail.enabled = false;
    }

    private void ContinueSlice()
    {
        Vector3 newPosition = PointerToBladeWorldPosition(Input.mousePosition);
        direction = newPosition - transform.position;

        float velocity = direction.magnitude / Time.deltaTime;
        sliceCollider.enabled = velocity > minSliceVelocity;

        transform.position = newPosition;
        UpdateBladeVisuals(true);
    }

    private bool TryUpdateWebcamSlice()
    {
        if (!useWebcamTracking || webcamReceiver == null || !webcamReceiver.HasFreshFrame)
            return false;

        TrackingFrameV1 frame = webcamReceiver.LatestFrame;
        if (frame == null || !frame.calibrated)
            return false;

        HandTrackingData rightHand = frame.right;
        HandTrackingData leftHand = frame.left;
        bool hasRight = rightHand != null && rightHand.tracked;
        bool hasLeft = leftHand != null && leftHand.tracked;

        if (!hasRight && !hasLeft)
            return false;

        Vector3 targetPosition = transform.position;
        if (hasRight) {
            Vector3 handPosition = rightHand.PositionVector(webcamRightNeutral);
            targetPosition = SmoothWebcamTarget(WebcamHandToWorld(handPosition, webcamRightNeutral), true);
        } else if (hasLeft) {
            Vector3 handPosition = leftHand.PositionVector(webcamLeftNeutral);
            targetPosition = SmoothWebcamTarget(WebcamHandToWorld(handPosition, webcamLeftNeutral), false);
        }

        if (!slicing)
        {
            transform.position = targetPosition;
            slicing = true;
            webcamSliceActive = true;
            sliceTrail.enabled = true;
            sliceTrail.Clear();
        }

        Vector3 newPosition = targetPosition;
        direction = newPosition - transform.position;

        float velocity = Time.deltaTime > 0f ? direction.magnitude / Time.deltaTime : 0f;
        sliceCollider.enabled = velocity > minSliceVelocity;
        transform.position = newPosition;

        bool leftActive = false;
        Vector3 leftPosition = new Vector3(-newPosition.x, newPosition.y - 0.5f, newPosition.z - 0.25f);
        if (hasLeft) {
            Vector3 leftHandPosition = leftHand.PositionVector(webcamLeftNeutral);
            leftPosition = SmoothWebcamTarget(WebcamHandToWorld(leftHandPosition, webcamLeftNeutral), false);
            UpdateLeftSliceCollider(leftPosition);
            leftActive = leftSliceCollider != null && leftSliceCollider.enabled;
        } else if (leftSliceCollider != null) {
            leftSliceCollider.enabled = false;
        }

        if (bladeVisuals != null) {
            bladeVisuals.SetBladePoses(newPosition, true, leftPosition, hasLeft);
            bladeVisualsDrivenThisFrame = true;
        }
        return true;
    }

    private Vector3 SmoothWebcamTarget(Vector3 targetPosition, bool rightHand)
    {
        Vector3 smoothedPosition = rightHand ? smoothedWebcamBladePosition : smoothedWebcamLeftBladePosition;
        bool hasSmoothedPosition = rightHand ? hasSmoothedWebcamBladePosition : hasSmoothedWebcamLeftBladePosition;

        if (!hasSmoothedPosition)
        {
            smoothedPosition = targetPosition;
            hasSmoothedPosition = true;
            SaveSmoothedWebcamTarget(rightHand, smoothedPosition, hasSmoothedPosition);
            return targetPosition;
        }

        Vector3 delta = targetPosition - smoothedPosition;
        if (delta.magnitude < webcamDeadZone)
            return targetPosition;

        Vector3 speedLimitedTarget = targetPosition;
        if (webcamMaxSpeed > 0f && webcamMaxSpeed < 999f) {
            float maxStep = webcamMaxSpeed * Time.deltaTime;
            speedLimitedTarget = smoothedPosition + Vector3.ClampMagnitude(delta, maxStep);
        }
        float smoothing = 1f - Mathf.Exp(-webcamPositionSmoothing * Time.deltaTime);
        smoothedPosition = Vector3.Lerp(smoothedPosition, speedLimitedTarget, smoothing);
        SaveSmoothedWebcamTarget(rightHand, smoothedPosition, hasSmoothedPosition);
        return smoothedPosition;
    }

    private void SaveSmoothedWebcamTarget(bool rightHand, Vector3 position, bool hasPosition)
    {
        if (rightHand)
        {
            smoothedWebcamBladePosition = position;
            hasSmoothedWebcamBladePosition = hasPosition;
        }
        else
        {
            smoothedWebcamLeftBladePosition = position;
            hasSmoothedWebcamLeftBladePosition = hasPosition;
        }
    }

    private bool TryUpdateXRControllerSlice()
    {
        if (!useXRControllers)
            return false;

        if (!TryGetXRBladePosition(XRNode.RightHand, xrRightNeutral, out Vector3 rightPosition, out bool rightPressed))
            return false;

        bool shouldSlice = !requireXRTriggerToSlice || rightPressed;
        if (!shouldSlice)
            return false;

        if (!slicing)
        {
            transform.position = rightPosition;
            slicing = true;
            xrSliceActive = true;
            sliceTrail.enabled = true;
            sliceTrail.Clear();
        }

        direction = rightPosition - transform.position;
        float velocity = Time.deltaTime > 0f ? direction.magnitude / Time.deltaTime : 0f;
        sliceCollider.enabled = velocity > minSliceVelocity;
        transform.position = rightPosition;

        if (TryGetXRBladePosition(XRNode.LeftHand, xrLeftNeutral, out Vector3 leftPosition, out bool leftPressed))
        {
            lastXRLeftBladePosition = leftPosition;
            hasXRLeftBladePosition = true;
            if (bladeVisuals != null) {
                bladeVisuals.SetBladePoses(rightPosition, true, leftPosition, !requireXRTriggerToSlice || leftPressed);
                bladeVisualsDrivenThisFrame = true;
            }
        }
        else if (bladeVisuals != null)
        {
            Vector3 fallbackLeft = hasXRLeftBladePosition ? lastXRLeftBladePosition : new Vector3(-rightPosition.x, rightPosition.y - 0.5f, rightPosition.z - 0.25f);
            bladeVisuals.SetBladePoses(rightPosition, true, fallbackLeft, false);
            bladeVisualsDrivenThisFrame = true;
        }

        return true;
    }

    private bool TryGetXRBladePosition(XRNode node, Vector3 neutral, out Vector3 bladePosition, out bool triggerPressed)
    {
        InputDevice device = node == XRNode.RightHand ? rightXRDevice : leftXRDevice;
        if (!device.isValid) {
            device = InputDevices.GetDeviceAtXRNode(node);
            if (node == XRNode.RightHand) {
                rightXRDevice = device;
            } else {
                leftXRDevice = device;
            }
        }

        triggerPressed = false;
        bladePosition = Vector3.zero;

        if (!device.isValid)
            return false;

        if (!device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 localPosition))
            return false;

        if (device.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerButton)) {
            triggerPressed = triggerButton;
        } else if (device.TryGetFeatureValue(CommonUsages.gripButton, out bool gripButton)) {
            triggerPressed = gripButton;
        } else {
            triggerPressed = true;
        }

        Vector3 offset = localPosition - neutral;
        bladePosition = new Vector3(
            xrSlicePlaneCenter.x + offset.x * xrHorizontalScale,
            xrSlicePlaneCenter.y + offset.y * xrVerticalScale,
            xrSlicePlaneCenter.z);
        return true;
    }

    private Vector3 WebcamHandToWorld(Vector3 handPosition, Vector3 neutralPosition)
    {
        Vector3 offset = handPosition - neutralPosition;
        return new Vector3(
            webcamSlicePlaneCenter.x + offset.x * webcamHorizontalScale,
            webcamSlicePlaneCenter.y + offset.y * webcamVerticalScale,
            webcamSlicePlaneCenter.z);
    }

    private Vector3 PointerToBladeWorldPosition(Vector3 screenPosition)
    {
        if (mainCamera.orthographic) {
            Vector3 position = mainCamera.ScreenToWorldPoint(screenPosition);
            position.z = desktopSlicePlaneZ;
            return position;
        }

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        Plane slicePlane = new Plane(Vector3.back, new Vector3(0f, 0f, desktopSlicePlaneZ));
        if (slicePlane.Raycast(ray, out float enter)) {
            return ray.GetPoint(enter);
        }

        Vector3 fallback = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, Mathf.Abs(mainCamera.transform.position.z)));
        fallback.z = desktopSlicePlaneZ;
        return fallback;
    }

    private void ConfigureSliceCollider()
    {
        Collider existingCollider = GetComponent<Collider>();
        if (existingCollider != null) {
            Destroy(existingCollider);
        }

        sliceCollider = gameObject.AddComponent<BoxCollider>();
        sliceCollider.isTrigger = true;
        sliceCollider.size = sliceCollider3DSize;
        sliceCollider.center = Vector3.zero;
    }

    private void ConfigureLeftSliceCollider()
    {
        leftSliceObject = new GameObject("Left Webcam Slice Collider");
        leftSliceObject.transform.SetParent(transform.parent, false);
        leftSliceObject.tag = gameObject.tag;

        LeftBladeProxy proxy = leftSliceObject.AddComponent<LeftBladeProxy>();
        proxy.Init(this);

        leftSliceCollider = leftSliceObject.AddComponent<BoxCollider>();
        leftSliceCollider.isTrigger = true;
        leftSliceCollider.size = sliceCollider3DSize;
        leftSliceCollider.enabled = false;
    }

    private void UpdateLeftSliceCollider(Vector3 leftPosition)
    {
        if (leftSliceObject == null || leftSliceCollider == null)
            return;

        if (!hasPreviousLeftBladePosition)
        {
            previousLeftBladePosition = leftPosition;
            hasPreviousLeftBladePosition = true;
        }

        leftBladeDirection = leftPosition - previousLeftBladePosition;
        float velocity = Time.deltaTime > 0f ? leftBladeDirection.magnitude / Time.deltaTime : 0f;
        leftSliceObject.transform.position = leftPosition;
        leftSliceCollider.enabled = velocity > minSliceVelocity;
        previousLeftBladePosition = leftPosition;
    }

    public Vector3 GetLeftBladeDirection()
    {
        return leftBladeDirection;
    }

    private class LeftBladeProxy : MonoBehaviour, IBladeSliceSource
    {
        private Blade source;

        public Vector3 Direction => source != null ? source.GetLeftBladeDirection() : Vector3.zero;
        public float SliceForce => source != null ? source.sliceForce : 5f;
        public Vector3 Position => transform.position;

        public void Init(Blade blade)
        {
            source = blade;
        }
    }

    private void LateUpdate()
    {
        if (!bladeVisualsDrivenThisFrame) {
            UpdateBladeVisuals(slicing);
        }
    }

    private void UpdateBladeVisuals(bool active)
    {
        if (bladeVisuals != null) {
            bladeVisuals.SetRightBladePose(transform.position, active);
            bladeVisualsDrivenThisFrame = true;
        }
    }

}
