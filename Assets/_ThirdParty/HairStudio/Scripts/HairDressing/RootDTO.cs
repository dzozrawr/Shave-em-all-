using UnityEngine;
using UnityEditor;

namespace HairStudio
{
    public struct RootDTO
    {
        public int zone;
        public Vector3 localPos;

        public RootDTO(Root root) {
            zone = root.Zone;
            localPos = root.LocalPos;
        }

    }
}