using UnityEngine;

namespace HairStudio
{
    public static class QuaternionUtility
    {
        public static Vector4 ToVector4(Quaternion q) {
            return new Vector4(q.x, q.y, q.z, q.w);
        }
        public static Quaternion FromVector4(Vector4 v) {
            return new Quaternion(v.x, v.y, v.z, v.w);
        }
    }
}
