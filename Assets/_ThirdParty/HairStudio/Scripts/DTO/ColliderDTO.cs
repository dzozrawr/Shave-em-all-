using UnityEngine;

namespace HairStudio
{
    struct ColliderDTO
    {
        public const int SIZE = sizeof(float) * 3 + sizeof(float);

        public readonly Vector3 pos;
        public readonly float radius;

        public ColliderDTO(ColliderInfo ci) {
            pos = ci.collider.transform.position;
            radius = ci.radius;
        }
    }
}