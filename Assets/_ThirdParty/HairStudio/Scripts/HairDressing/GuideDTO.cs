using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;

namespace HairStudio
{
    public struct GuideDTO {
        public int segmentCount;
        public int firstSegmentIndex;
        public Quaternion localRotation;
        public int mixedLock;
        public float lastSegmentLength;
        public float segmentLength;
        public int zone;

        public GuideDTO(Guide guide, int firstSegmentIndex, List<GuideSegmentDTO> segments) {
            segmentCount = guide.segments.Count;
            this.firstSegmentIndex = firstSegmentIndex;
            localRotation = guide.localRotation;
            mixedLock = guide.mixedLock? 1 : 0;
            //lastSegmentLength = guide.lastSegmentLength;
            //segmentLength = guide.segmentLength;
            lastSegmentLength = Vector3.Distance(segments[firstSegmentIndex + segmentCount - 2].localPosition, segments[firstSegmentIndex + segmentCount - 1].localPosition);
            segmentLength = Vector3.Distance(segments[firstSegmentIndex].localPosition, segments[firstSegmentIndex + 1].localPosition);
            zone = guide.zone;
        }

        public float Length { get => (segmentCount - 2) * segmentLength + lastSegmentLength; }

        public Vector3 GetLocalPosition(GuideSegmentDTO[] segments, float rate) {
            if (rate == 0) return segments[firstSegmentIndex].localPosition;
            if (rate == 1) return segments[firstSegmentIndex + segmentCount - 1].localPosition;
            if (rate < 0 || rate > 1) throw new Exception("Rate must be in range [0, 1], but was " + rate);

            var lengthAtRate = Length * rate;
            var index = Mathf.Floor(lengthAtRate / segmentLength);
            var remains = lengthAtRate - index * segmentLength;

            var localSegmentLength = (int)index == segmentCount - 2 ? lastSegmentLength : segmentLength;
            return Vector3.Lerp(segments[firstSegmentIndex + (int)index].localPosition, segments[firstSegmentIndex + (int)index + 1].localPosition, remains / localSegmentLength);
        }
    }
}