using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HairStudio
{
    [Serializable]
    public class Guide : ISerializationCallbackReceiver
    {
        public List<GuideSegment> segments = new List<GuideSegment>();
        public Quaternion localRotation;
        public bool mixedLock;
        public float lastSegmentLength;
        public float segmentLength;
        public int zone;

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize() {
            GuideSegment previous = null;
            foreach (var seg in segments) {
                if (previous != null) {
                    seg.previous = previous;
                    previous.next = seg;
                }
                previous = seg;
            }
        }
    }
}


