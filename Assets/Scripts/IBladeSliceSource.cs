using UnityEngine;

public interface IBladeSliceSource
{
    Vector3 Direction { get; }
    float SliceForce { get; }
    Vector3 Position { get; }
}
