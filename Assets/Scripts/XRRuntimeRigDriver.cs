using UnityEngine;
using UnityEngine.XR;

public class XRRuntimeRigDriver : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Vector3 rigOrigin = new Vector3(0f, 0f, -8.2f);
    [SerializeField] private bool logXRState = true;

    private InputDevice hmdDevice;
    private bool wasTracking;

    public bool IsTracking { get; private set; }

    private void Awake()
    {
        if (targetCamera == null) {
            targetCamera = Camera.main;
        }

        if (targetCamera != null) {
            targetCamera.stereoTargetEye = StereoTargetEyeMask.Both;
        }
    }

    private void Update()
    {
        IsTracking = TryUpdateHeadPose();

        if (logXRState && IsTracking != wasTracking)
        {
            Debug.Log(IsTracking
                ? "XRRuntimeRigDriver: HMD tracking active. Camera is driven by XR head pose."
                : "XRRuntimeRigDriver: HMD tracking inactive. Falling back to desktop camera pose.",
                this);
        }

        wasTracking = IsTracking;
    }

    private bool TryUpdateHeadPose()
    {
        if (targetCamera == null)
            return false;

        if (!hmdDevice.isValid) {
            hmdDevice = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
        }

        if (!hmdDevice.isValid)
            return false;

        if (!hmdDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 localPosition))
            localPosition = new Vector3(0f, 1.6f, 0f);

        if (!hmdDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion localRotation))
            localRotation = Quaternion.identity;

        targetCamera.transform.SetPositionAndRotation(rigOrigin + localPosition, localRotation);
        return true;
    }
}
