using UnityEngine;

namespace WebcamXR
{
    public class FruitSaberSliceSource : MonoBehaviour, IBladeSliceSource
    {
        public float sliceForce = 5f;

        public Vector3 Direction { get; private set; } = Vector3.up;
        public float SliceForce => sliceForce;
        public Vector3 Position => transform.position;

        public void SetDirection(Vector3 direction)
        {
            Direction = direction.sqrMagnitude > 0.0001f ? direction : Vector3.up;
        }
    }
}
