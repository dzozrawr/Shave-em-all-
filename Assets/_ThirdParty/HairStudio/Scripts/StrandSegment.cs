using System;
using UnityEngine;

namespace HairStudio
{
    [Serializable]
    public struct StrandSegment
    {
        public int canMove;
        public float rate;
        public float length;
        public Vector3 pos;
        public Vector3 previousPos;
        public Vector3 initialLocalPos;
        public Vector3 arbitraryUp;
        public Quaternion frame;
        public Quaternion rotation;
        public Quaternion localRestRotation;
        public Quaternion idealPos;
    }
}
