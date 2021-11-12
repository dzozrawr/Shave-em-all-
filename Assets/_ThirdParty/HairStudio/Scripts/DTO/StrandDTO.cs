using UnityEngine;

namespace HairStudio
{
    public struct StrandDTO
    {
        public const int SIZE = sizeof(int) +
            sizeof(int) +
            sizeof(float) * 4;

        public readonly int firstSegmentIndex;
        public readonly int nbSegments;
        public readonly Vector4 localRotation;

        public StrandDTO(Strand strand) {
            firstSegmentIndex = strand.firstSegmentIndex;
            nbSegments = strand.segmentCount;
            localRotation = QuaternionUtility.ToVector4(strand.localRotation);
        }
    }
}