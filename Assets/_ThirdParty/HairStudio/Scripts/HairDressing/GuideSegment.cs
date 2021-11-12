using System;
using UnityEngine;

namespace HairStudio
{
    [Serializable]
    public class GuideSegment
    {
        public bool canMove;
        public Vector3 localPosition;

        [NonSerialized]
        public GuideSegment next;
        [NonSerialized]
        public GuideSegment previous;
    }
}
