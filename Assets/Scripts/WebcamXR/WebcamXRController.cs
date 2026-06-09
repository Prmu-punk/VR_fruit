using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace WebcamXR
{
    [AddComponentMenu("XR/Webcam XR Controller")]
    public class WebcamXRController : XRBaseController
    {
        private bool isTracked;
        private Vector3 localPosition;
        private Quaternion localRotation = Quaternion.identity;
        private bool selectActive;
        private float selectValue;

        public void ApplyPose(bool tracked, Vector3 position, Quaternion rotation)
        {
            isTracked = tracked;
            localPosition = position;
            localRotation = rotation;
        }

        public void ApplySelect(bool active, float value)
        {
            selectActive = active;
            selectValue = Mathf.Clamp01(value);
        }

        public void ClearInput()
        {
            ApplySelect(false, 0f);
        }

        protected override void UpdateTrackingInput(XRControllerState controllerState)
        {
            controllerState.time = Time.timeAsDouble;

            if (!isTracked)
            {
                controllerState.inputTrackingState = InputTrackingState.None;
                return;
            }

            controllerState.inputTrackingState = InputTrackingState.Position | InputTrackingState.Rotation;
            controllerState.position = localPosition;
            controllerState.rotation = localRotation;
        }

        protected override void UpdateInput(XRControllerState controllerState)
        {
            controllerState.selectInteractionState.SetFrameState(selectActive, selectValue);
        }
    }
}
