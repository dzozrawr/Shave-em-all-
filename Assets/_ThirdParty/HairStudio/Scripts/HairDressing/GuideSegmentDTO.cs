using UnityEngine;
using UnityEditor;

namespace HairStudio
{
    public struct GuideSegmentDTO
    {
        public int canMove;
        public Vector3 localPosition;

        public GuideSegmentDTO(GuideSegment seg) {
            canMove = seg.canMove? 1 : 0;
            localPosition = seg.localPosition;
        }

    }
}
