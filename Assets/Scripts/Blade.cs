using UnityEngine;
using UnityEngine.XR;
using WebcamXR;

public class Blade : MonoBehaviour, IBladeSliceSource
{
    public float sliceForce = 5f;
    public float minSliceVelocity = 0.01f;

    [Header("3D Play Volume")]
    [SerializeField] private Vector3 playVolumeCenter = new Vector3(0f, 1.25f, -3.5f);
    [SerializeField] private Vector3 playVolumeSize = new Vector3(1.25f, 1.15f, 0.7f);

    [Header("Webcam XR")]
    [SerializeField] private bool useWebcamTracking = false;
    [SerializeField] private Vector3 webcamRightNeutral = new Vector3(0.34f, 1.25f, 0.72f);
    [SerializeField] private Vector3 webcamLeftNeutral = new Vector3(-0.34f, 1.25f, 0.72f);
    [SerializeField] private float webcamHorizontalScale = 32.0f;
    [SerializeField] private float webcamVerticalScale = 22.0f;
    [SerializeField] private float webcamDepthScale = 3.1f;
    [SerializeField] private float webcamPositionSmoothing = 8f;
    [SerializeField] private float webcamDepthSmoothing = 4.5f;
    [SerializeField] private float webcamDeadZone = 0.0f;
    [SerializeField] private float webcamDepthDeadZone = 0.04f;
    [SerializeField] private float webcamMaxSpeed = 1000f;
    [SerializeField] private float webcamMaxDepthSpeed = 2.2f;

    [Header("XR Controller")]
    [SerializeField] private bool useXRControllers = true;
    [SerializeField] private bool requireXRTriggerToSlice = false;
    [SerializeField] private Vector3 xrBladeTipOffset = new Vector3(0f, 0f, 0.32f);
    [SerializeField] private Vector3 xrBladeLocalAxis = Vector3.forward;
    [SerializeField] private Vector3 xrBladeRotationOffsetEuler = new Vector3(-6f, 0f, 0f);
    [SerializeField] private float xrBladeHandleRollOffsetDegrees = 90f;
    [SerializeField] private bool clampXRToPlayVolume = false;
    [SerializeField] private bool showTrailInXR = false;

    [Header("Desktop")]
    [SerializeField] private float desktopSlicePlaneZ = -3.35f;

    [Header("Collision")]
    [SerializeField] private Vector3 sliceCollider3DSize = new Vector3(0.026f, 0.29f, 0.022f);

    private Camera mainCamera;
    private BoxCollider sliceCollider;
    private BoxCollider leftSliceCollider;
    private GameObject leftSliceObject;
    private TrailRenderer sliceTrail;
    private WebcamTrackingReceiver webcamReceiver;
    private BladeVisuals bladeVisuals;
    private XRRuntimeRigDriver xrRuntimeRigDriver;
    private InputDevice rightXRDevice;
    private InputDevice leftXRDevice;
    private bool webcamSliceActive;
    private bool xrSliceActive;
    private bool bladeVisualsDrivenThisFrame;
    private bool leftSlicing;
    private Vector3 smoothedWebcamRightPosition;
    private Vector3 smoothedWebcamLeftPosition;
    private bool hasSmoothedWebcamRightPosition;
    private bool hasSmoothedWebcamLeftPosition;
    private Vector3 previousLeftBladePosition;
    private Quaternion leftBladeRotation = Quaternion.identity;
    private Vector3 leftBladeDirection;
    private bool hasPreviousLeftBladePosition;
    private Vector3 lastRightBladePosition;
    private Vector3 lastLeftBladePosition;
    private Quaternion lastRightBladeRotation = Quaternion.identity;
    private Quaternion lastLeftBladeRotation = Quaternion.identity;
    private bool hasLastRightBladePosition;
    private bool hasLastLeftBladePosition;

    public Vector3 direction { get; private set; }
    public bool slicing { get; private set; }
    public Vector3 Direction => direction;
    public float SliceForce => sliceForce;
    public Vector3 Position => transform.position;
    public Quaternion Rotation => transform.rotation;

    private void Awake()
    {
        mainCamera = Camera.main;
        ConfigureSliceCollider();
        ConfigureLeftSliceCollider();
        sliceTrail = GetComponentInChildren<TrailRenderer>();
        bladeVisuals = FindObjectOfType<BladeVisuals>();
        if (bladeVisuals == null)
        {
            GameObject visualsObject = new GameObject("Runtime Blade Visuals");
            bladeVisuals = visualsObject.AddComponent<BladeVisuals>();
        }

        xrRuntimeRigDriver = FindObjectOfType<XRRuntimeRigDriver>();
        webcamReceiver = FindObjectOfType<WebcamTrackingReceiver>();

        if (webcamReceiver == null)
        {
            GameObject receiverObject = new GameObject("Webcam Tracking Receiver");
            webcamReceiver = receiverObject.AddComponent<WebcamTrackingReceiver>();
        }
    }

    private void OnEnable()
    {
        StopAllSlices();
    }

    private void OnDisable()
    {
        StopAllSlices();
    }

    private void Update()
    {
        bladeVisualsDrivenThisFrame = false;

        if (TryUpdateXRControllerSlices())
            return;

        if (xrSliceActive)
        {
            StopAllSlices();
            xrSliceActive = false;
        }

        if (TryUpdateWebcamSlices())
            return;

        if (webcamSliceActive)
        {
            StopAllSlices();
            webcamSliceActive = false;
        }

        UpdateMouseSlice();
    }

    private void UpdateMouseSlice()
    {
        if (Input.GetMouseButtonDown(0))
        {
            BladePose3D pose = CreateMousePose(Vector3.zero, true);
            ApplyRightPose(pose);
            slicing = true;
            sliceTrail.enabled = true;
            sliceTrail.Clear();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            StopRightSlice();
        }
        else if (slicing)
        {
            BladePose3D pose = CreateMousePose(transform.position, true);
            ApplyRightPose(pose);
        }
    }

    private BladePose3D CreateMousePose(Vector3 previousPosition, bool active)
    {
        Vector3 position = PointerToBladeWorldPosition(Input.mousePosition);
        Vector3 movement = previousPosition == Vector3.zero ? Vector3.zero : position - previousPosition;
        Quaternion rotation = MovementToBladeRotation(movement, transform.rotation);
        return new BladePose3D(BladeHand.Right, position, rotation, movement, active);
    }

    private void StopAllSlices()
    {
        StopRightSlice();
        StopLeftSlice();
    }

    private void StopRightSlice()
    {
        slicing = false;

        if (sliceCollider != null)
            sliceCollider.enabled = false;

        if (sliceTrail != null)
            sliceTrail.enabled = false;
    }

    private void StopLeftSlice()
    {
        leftSlicing = false;

        if (leftSliceCollider != null)
            leftSliceCollider.enabled = false;

        hasPreviousLeftBladePosition = false;
    }

    private bool TryUpdateWebcamSlices()
    {
        if (!useWebcamTracking || webcamReceiver == null || !webcamReceiver.HasFreshFrame)
            return false;

        TrackingFrameV1 frame = webcamReceiver.LatestFrame;
        if (frame == null || !frame.calibrated)
            return false;

        bool rightTracked = frame.right != null && frame.right.tracked;
        bool leftTracked = frame.left != null && frame.left.tracked;

        if (!rightTracked && !leftTracked)
            return false;

        webcamSliceActive = true;

        if (rightTracked)
        {
            BladePose3D rightPose = CreateWebcamPose(BladeHand.Right, frame.right, webcamRightNeutral);
            ApplyRightPose(rightPose);
        }
        else
        {
            StopRightSlice();
        }

        if (leftTracked)
        {
            BladePose3D leftPose = CreateWebcamPose(BladeHand.Left, frame.left, webcamLeftNeutral);
            ApplyLeftPose(leftPose);
        }
        else
        {
            StopLeftSlice();
        }

        UpdateBladeVisualsFromLastPoses(rightTracked, leftTracked);
        return true;
    }

    private BladePose3D CreateWebcamPose(BladeHand hand, HandTrackingData handData, Vector3 neutral)
    {
        Vector3 targetPosition = WebcamHandToWorld(handData.PositionVector(neutral), neutral);
        Vector3 smoothedPosition = SmoothWebcamTarget(targetPosition, hand);
        Vector3 previousPosition = hand == BladeHand.Right && hasLastRightBladePosition
            ? lastRightBladePosition
            : hand == BladeHand.Left && hasLastLeftBladePosition
                ? lastLeftBladePosition
                : smoothedPosition;
        Vector3 movement = smoothedPosition - previousPosition;
        Quaternion previousRotation = hand == BladeHand.Right ? lastRightBladeRotation : lastLeftBladeRotation;
        Quaternion rotation = MovementToBladeRotation(movement, previousRotation);
        return new BladePose3D(hand, smoothedPosition, rotation, movement, true);
    }

    private Vector3 SmoothWebcamTarget(Vector3 targetPosition, BladeHand hand)
    {
        bool rightHand = hand == BladeHand.Right;
        Vector3 smoothedPosition = rightHand ? smoothedWebcamRightPosition : smoothedWebcamLeftPosition;
        bool hasSmoothedPosition = rightHand ? hasSmoothedWebcamRightPosition : hasSmoothedWebcamLeftPosition;

        if (!hasSmoothedPosition)
        {
            SaveSmoothedWebcamTarget(hand, targetPosition, true);
            return targetPosition;
        }

        Vector3 delta = targetPosition - smoothedPosition;
        Vector3 speedLimitedTarget = smoothedPosition;
        Vector3 planarDelta = new Vector3(delta.x, delta.y, 0f);

        if (planarDelta.magnitude >= webcamDeadZone)
        {
            if (webcamMaxSpeed > 0f && webcamMaxSpeed < 999f)
                planarDelta = Vector3.ClampMagnitude(planarDelta, webcamMaxSpeed * Time.deltaTime);

            speedLimitedTarget += planarDelta;
        }

        if (Mathf.Abs(delta.z) >= webcamDepthDeadZone)
        {
            float depthStep = delta.z;
            if (webcamMaxDepthSpeed > 0f)
                depthStep = Mathf.Clamp(depthStep, -webcamMaxDepthSpeed * Time.deltaTime, webcamMaxDepthSpeed * Time.deltaTime);

            speedLimitedTarget.z += depthStep;
        }

        float planarSmoothing = 1f - Mathf.Exp(-webcamPositionSmoothing * Time.deltaTime);
        float depthSmoothing = 1f - Mathf.Exp(-webcamDepthSmoothing * Time.deltaTime);
        smoothedPosition = new Vector3(
            Mathf.Lerp(smoothedPosition.x, speedLimitedTarget.x, planarSmoothing),
            Mathf.Lerp(smoothedPosition.y, speedLimitedTarget.y, planarSmoothing),
            Mathf.Lerp(smoothedPosition.z, speedLimitedTarget.z, depthSmoothing));
        smoothedPosition = ClampToPlayVolume(smoothedPosition);
        SaveSmoothedWebcamTarget(hand, smoothedPosition, true);
        return smoothedPosition;
    }

    private void SaveSmoothedWebcamTarget(BladeHand hand, Vector3 position, bool hasPosition)
    {
        if (hand == BladeHand.Right)
        {
            smoothedWebcamRightPosition = position;
            hasSmoothedWebcamRightPosition = hasPosition;
        }
        else
        {
            smoothedWebcamLeftPosition = position;
            hasSmoothedWebcamLeftPosition = hasPosition;
        }
    }

    private bool TryUpdateXRControllerSlices()
    {
        if (!useXRControllers)
            return false;

        bool hasRight = TryGetXRBladePose(BladeHand.Right, out BladePose3D rightPose, out bool rightPressed);
        bool hasLeft = TryGetXRBladePose(BladeHand.Left, out BladePose3D leftPose, out bool leftPressed);

        if (!hasRight && !hasLeft)
            return false;

        xrSliceActive = true;

        bool rightActive = hasRight && (!requireXRTriggerToSlice || rightPressed);
        bool leftActive = hasLeft && (!requireXRTriggerToSlice || leftPressed);

        if (hasRight)
        {
            rightPose = new BladePose3D(BladeHand.Right, rightPose.Position, rightPose.Rotation, rightPose.Direction, rightActive);
            ApplyRightPose(rightPose);
        }
        else
        {
            StopRightSlice();
        }

        if (hasLeft)
        {
            leftPose = new BladePose3D(BladeHand.Left, leftPose.Position, leftPose.Rotation, leftPose.Direction, leftActive);
            ApplyLeftPose(leftPose);
        }
        else
        {
            StopLeftSlice();
        }

        UpdateBladeVisualsFromLastPoses(hasRight, hasLeft);
        return true;
    }

    private bool TryGetXRBladePose(BladeHand hand, out BladePose3D pose, out bool triggerPressed)
    {
        XRNode node = hand == BladeHand.Right ? XRNode.RightHand : XRNode.LeftHand;
        InputDevice device = hand == BladeHand.Right ? rightXRDevice : leftXRDevice;
        if (!device.isValid)
        {
            device = InputDevices.GetDeviceAtXRNode(node);
            if (hand == BladeHand.Right)
                rightXRDevice = device;
            else
                leftXRDevice = device;
        }

        pose = new BladePose3D(hand, Vector3.zero, Quaternion.identity, Vector3.zero, false);
        triggerPressed = false;

        if (!device.isValid)
            return false;

        if (!device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 localPosition))
            return false;

        if (!device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion localRotation))
            localRotation = Quaternion.identity;

        if (device.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerButton))
            triggerPressed = triggerButton;
        else if (device.TryGetFeatureValue(CommonUsages.gripButton, out bool gripButton))
            triggerPressed = gripButton;
        else
            triggerPressed = true;

        if (xrRuntimeRigDriver == null)
            xrRuntimeRigDriver = FindObjectOfType<XRRuntimeRigDriver>();

        Vector3 controllerWorldPosition = xrRuntimeRigDriver != null
            ? xrRuntimeRigDriver.TrackingToWorldPosition(localPosition)
            : localPosition;
        Quaternion controllerWorldRotation = xrRuntimeRigDriver != null
            ? xrRuntimeRigDriver.TrackingToWorldRotation(localRotation)
            : localRotation;

        Quaternion correctedControllerRotation = controllerWorldRotation * Quaternion.Euler(xrBladeRotationOffsetEuler);
        Vector3 bladeAxis = correctedControllerRotation * SafeAxis(xrBladeLocalAxis);
        Vector3 bladePosition = controllerWorldPosition + correctedControllerRotation * xrBladeTipOffset;
        if (clampXRToPlayVolume)
            bladePosition = ClampToPlayVolume(bladePosition);

        Vector3 previousPosition = hand == BladeHand.Right && hasLastRightBladePosition
            ? lastRightBladePosition
            : hand == BladeHand.Left && hasLastLeftBladePosition
                ? lastLeftBladePosition
                : bladePosition;
        Vector3 movement = bladePosition - previousPosition;
        Quaternion bladeRotation = Quaternion.AngleAxis(xrBladeHandleRollOffsetDegrees, bladeAxis)
            * BladeAxisToRotation(bladeAxis, correctedControllerRotation * Vector3.up);
        pose = new BladePose3D(hand, bladePosition, bladeRotation, movement, true);
        return true;
    }

    private void ApplyRightPose(BladePose3D pose)
    {
        direction = pose.Direction;
        transform.SetPositionAndRotation(pose.Position, pose.Rotation);
        slicing = pose.Active;

        float velocity = Time.deltaTime > 0f ? pose.Direction.magnitude / Time.deltaTime : 0f;
        sliceCollider.enabled = pose.Active && velocity > minSliceVelocity;
        if (sliceCollider.enabled)
            SweepBombs(sliceCollider, this);

        bool showTrail = pose.Active && (!xrSliceActive || showTrailInXR);
        if (showTrail && sliceTrail != null && !sliceTrail.enabled)
        {
            sliceTrail.enabled = true;
            sliceTrail.Clear();
        }
        else if (!showTrail && sliceTrail != null)
        {
            sliceTrail.enabled = false;
        }

        lastRightBladePosition = pose.Position;
        lastRightBladeRotation = pose.Rotation;
        hasLastRightBladePosition = true;
    }

    private void ApplyLeftPose(BladePose3D pose)
    {
        if (leftSliceObject == null || leftSliceCollider == null)
            return;

        if (!hasPreviousLeftBladePosition)
        {
            previousLeftBladePosition = pose.Position;
            hasPreviousLeftBladePosition = true;
        }

        leftBladeDirection = pose.Position - previousLeftBladePosition;
        leftBladeRotation = pose.Rotation;
        leftSlicing = pose.Active;
        float velocity = Time.deltaTime > 0f ? leftBladeDirection.magnitude / Time.deltaTime : 0f;
        leftSliceObject.transform.SetPositionAndRotation(pose.Position, pose.Rotation);
        leftSliceCollider.enabled = pose.Active && velocity > minSliceVelocity;
        if (leftSliceCollider.enabled)
            SweepBombs(leftSliceCollider, leftSliceObject.GetComponent<LeftBladeProxy>());
        previousLeftBladePosition = pose.Position;
        lastLeftBladePosition = pose.Position;
        lastLeftBladeRotation = pose.Rotation;
        hasLastLeftBladePosition = true;
    }

    private void SweepBombs(BoxCollider bladeCollider, IBladeSliceSource sliceSource)
    {
        if (bladeCollider == null || sliceSource == null)
            return;

        Vector3 center = bladeCollider.transform.TransformPoint(bladeCollider.center);
        Vector3 halfExtents = Vector3.Scale(bladeCollider.size, bladeCollider.transform.lossyScale) * 0.5f;
        Collider[] hits = Physics.OverlapBox(center, halfExtents, bladeCollider.transform.rotation, ~0, QueryTriggerInteraction.Collide);
        foreach (Collider hit in hits)
        {
            Bomb bomb = hit.GetComponentInParent<Bomb>();
            if (bomb != null)
                bomb.TrySlice(sliceSource);
        }
    }

    private Vector3 WebcamHandToWorld(Vector3 handPosition, Vector3 neutralPosition)
    {
        Vector3 offset = handPosition - neutralPosition;
        return ClampToPlayVolume(new Vector3(
            playVolumeCenter.x + offset.x * webcamHorizontalScale,
            playVolumeCenter.y + offset.y * webcamVerticalScale,
            playVolumeCenter.z + offset.z * webcamDepthScale));
    }

    private Vector3 PointerToBladeWorldPosition(Vector3 screenPosition)
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return ClampToPlayVolume(new Vector3(0f, playVolumeCenter.y, desktopSlicePlaneZ));

        if (mainCamera.orthographic)
        {
            Vector3 position = mainCamera.ScreenToWorldPoint(screenPosition);
            position.z = desktopSlicePlaneZ;
            return ClampToPlayVolume(position);
        }

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        Plane slicePlane = new Plane(Vector3.back, new Vector3(0f, 0f, desktopSlicePlaneZ));
        if (slicePlane.Raycast(ray, out float enter))
            return ClampToPlayVolume(ray.GetPoint(enter));

        Vector3 fallback = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, Mathf.Abs(mainCamera.transform.position.z)));
        fallback.z = desktopSlicePlaneZ;
        return ClampToPlayVolume(fallback);
    }

    private void ConfigureSliceCollider()
    {
        Collider existingCollider = GetComponent<Collider>();
        if (existingCollider != null)
            Destroy(existingCollider);

        sliceCollider = gameObject.AddComponent<BoxCollider>();
        sliceCollider.isTrigger = true;
        sliceCollider.size = sliceCollider3DSize;
        sliceCollider.center = Vector3.zero;
    }

    private void ConfigureLeftSliceCollider()
    {
        leftSliceObject = new GameObject("Left Blade Slice Collider");
        leftSliceObject.transform.SetParent(transform.parent, false);
        leftSliceObject.tag = gameObject.tag;

        LeftBladeProxy proxy = leftSliceObject.AddComponent<LeftBladeProxy>();
        proxy.Init(this);

        leftSliceCollider = leftSliceObject.AddComponent<BoxCollider>();
        leftSliceCollider.isTrigger = true;
        leftSliceCollider.size = sliceCollider3DSize;
        leftSliceCollider.center = Vector3.zero;
        leftSliceCollider.enabled = false;
    }

    public Vector3 GetLeftBladeDirection()
    {
        return leftBladeDirection;
    }

    public Quaternion GetLeftBladeRotation()
    {
        return leftBladeRotation;
    }

    private void LateUpdate()
    {
        if (!bladeVisualsDrivenThisFrame)
            UpdateBladeVisualsFromLastPoses(hasLastRightBladePosition, hasLastLeftBladePosition);
    }

    private void UpdateBladeVisualsFromLastPoses(bool hasRightPose, bool hasLeftPose)
    {
        if (bladeVisuals == null)
            return;

        Vector3 rightPosition = hasRightPose ? lastRightBladePosition : transform.position;
        Quaternion rightRotation = hasRightPose ? lastRightBladeRotation : transform.rotation;
        bool rightActive = slicing;

        if (hasLeftPose)
        {
            bladeVisuals.SetBladePoses(rightPosition, rightRotation, rightActive, lastLeftBladePosition, lastLeftBladeRotation, leftSlicing);
        }
        else
        {
            bladeVisuals.SetRightBladePose(rightPosition, rightRotation, rightActive);
        }

        bladeVisualsDrivenThisFrame = true;
    }

    private Vector3 ClampToPlayVolume(Vector3 position)
    {
        Vector3 halfSize = playVolumeSize * 0.5f;
        return new Vector3(
            Mathf.Clamp(position.x, playVolumeCenter.x - halfSize.x, playVolumeCenter.x + halfSize.x),
            Mathf.Clamp(position.y, playVolumeCenter.y - halfSize.y, playVolumeCenter.y + halfSize.y),
            Mathf.Clamp(position.z, playVolumeCenter.z - halfSize.z, playVolumeCenter.z + halfSize.z));
    }

    private static Quaternion MovementToBladeRotation(Vector3 movementDirection, Quaternion fallback)
    {
        if (movementDirection.sqrMagnitude < 0.0001f)
            return fallback == Quaternion.identity ? Quaternion.LookRotation(Vector3.forward, Vector3.up) : fallback;

        return Quaternion.LookRotation(Vector3.forward, movementDirection.normalized);
    }

    private static Vector3 SafeAxis(Vector3 axis)
    {
        return axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.forward;
    }

    private static Quaternion BladeAxisToRotation(Vector3 bladeAxis, Vector3 forwardHint)
    {
        Vector3 up = bladeAxis.sqrMagnitude > 0.0001f ? bladeAxis.normalized : Vector3.up;
        Vector3 forward = Vector3.ProjectOnPlane(forwardHint, up);
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.ProjectOnPlane(Vector3.forward, up);

        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.right;

        return Quaternion.LookRotation(forward.normalized, up);
    }

    private class LeftBladeProxy : MonoBehaviour, IBladeSliceSource
    {
        private Blade source;

        public Vector3 Direction => source != null ? source.GetLeftBladeDirection() : Vector3.zero;
        public float SliceForce => source != null ? source.sliceForce : 5f;
        public Vector3 Position => transform.position;
        public Quaternion Rotation => source != null ? source.GetLeftBladeRotation() : transform.rotation;

        public void Init(Blade blade)
        {
            source = blade;
        }
    }
}
