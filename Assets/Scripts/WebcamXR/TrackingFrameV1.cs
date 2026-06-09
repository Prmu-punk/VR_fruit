using System;
using UnityEngine;

namespace WebcamXR
{
    [Serializable]
    public class TrackingFrameV1
    {
        public int version = 1;
        public long timestamp_ms;
        public bool calibrated;
        public HandTrackingData left = new HandTrackingData();
        public HandTrackingData right = new HandTrackingData();
    }

    [Serializable]
    public class HandTrackingData
    {
        public bool tracked;
        public bool pinch;
        public float pinch_strength;
        public float[] local_position = new float[0];
        public float[] forward = new float[0];

        public Vector3 PositionVector(Vector3 fallback)
        {
            if (local_position == null || local_position.Length < 3)
                return fallback;

            return new Vector3(local_position[0], local_position[1], local_position[2]);
        }
    }
}
