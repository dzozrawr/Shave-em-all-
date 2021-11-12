using UnityEngine;

namespace HairStudio
{
    public static class Vector3Utils
    {
        public static Vector3 Clamp(Vector3 value, Vector3 min, Vector3 max) {
            return new Vector3(
                    Mathf.Clamp(value.x, min.x, max.x),
                    Mathf.Clamp(value.y, min.y, max.y),
                    Mathf.Clamp(value.z, min.z, max.z));
        }

        public static float InverseLerp(Vector3 a, Vector3 b, Vector3 value) {
            Vector3 AB = b - a;
            Vector3 AV = value - a;
            return Vector3.Dot(AV, AB) / Vector3.Dot(AB, AB);
        }
    }
}
