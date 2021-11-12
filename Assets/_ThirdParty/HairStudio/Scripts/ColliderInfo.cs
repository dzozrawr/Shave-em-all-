using System;
using UnityEngine;

namespace HairStudio
{
    [Serializable]
    public struct ColliderInfo
    {
        public SphereCollider collider;
        public float radius;

        public Vector3 GetDepenetration(Vector3 position, float otherRadius) {
            var center = collider.transform.TransformPoint(collider.center);
            var v = position - center;

            var bothRadii = radius + otherRadius;

            if (v.sqrMagnitude < bothRadii * bothRadii) {
                // collision detected
                var penetration = bothRadii - v.magnitude;
                if (penetration > 0) {
                    return v.normalized * penetration;
                }
            }
            return Vector3.zero;
        }
    }
}
