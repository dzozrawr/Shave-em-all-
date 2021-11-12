using UnityEngine;

namespace HairStudio
{
    public struct SegmentDTO
    {
        public const int SIZE = sizeof(int) +
            sizeof(float) +
            sizeof(float) +
            sizeof(float) +
            sizeof(float) * 3 +
            sizeof(float) * 3 +
            sizeof(float) * 3 +
            sizeof(float) * 3 +
            sizeof(float) * 4 +
            sizeof(float) * 4;

        public readonly int canMove;
        public readonly float rate;
        public readonly float previousDeltaTime;
        public readonly float length;
        public readonly Vector3 pos;
        public readonly Vector3 previousPos;
        public readonly Vector3 initialLocalPos;
        public readonly Vector3 arbitraryUp;
        public readonly Vector4 frame;
        public readonly Vector4 localRestRotation;

        public SegmentDTO(StrandSegment seg) {
            canMove = seg.canMove;
            rate = seg.rate;
            previousDeltaTime = 1;
            length = seg.length;
            initialLocalPos = seg.initialLocalPos;
            arbitraryUp = seg.arbitraryUp;
            pos = seg.pos;
            previousPos = seg.pos;
            frame = QuaternionUtility.ToVector4(seg.frame);
            localRestRotation = QuaternionUtility.ToVector4(seg.localRestRotation);
        }
    }
}
