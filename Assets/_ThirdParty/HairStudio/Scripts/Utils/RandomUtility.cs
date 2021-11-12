using System;
using UnityEngine;

namespace HairStudio
{
    public static class RandomUtility
    {
        public static Vector2 InsideUnitCircleUniform(Random random = null) {
            float t = 2 * Mathf.PI * (random != null ? random.Float() : UnityEngine.Random.value);
            float u = random != null ? random.Float() + random.Float() : UnityEngine.Random.value + UnityEngine.Random.value;
            float r = u > 1 ? 2 - u : u;
            return new Vector2(r * Mathf.Cos(t), r * Mathf.Sin(t));
        }

        public static Vector3 OnUnitSphere(Random rand) {
            var x = rand.Float(-1f, 1f);
            var y = rand.Float(-1f, 1f);
            var z = rand.Float(-1f, 1f);
            return new Vector3(x, y, z).normalized;
        }

        public class Random
        {
            uint state;
            public Random(uint seed = 0x6E624EB7u) {
                if (seed == 0) seed = 0x6E624EB7u;
                state = seed;
            }

            public bool Bool() {
                return (SetNextState() & 1) == 1;
            }

            public int Int() {
                return (int)SetNextState() ^ -2147483648;
            }

            public int Int(int max) {
                CheckMinMax(0, max);
                return (int)(SetNextState() * (ulong)max >> 32);
            }

            public int Int(int min, int max) {
                CheckMinMax(min, max);
                uint range = (uint)(max - min);
                return (int)(SetNextState() * (ulong)range >> 32) + min;
            }

            public float Float() {
                float f = BitConverter.ToSingle(BitConverter.GetBytes(0x3f800000 | (SetNextState() >> 9)), 0);
                return f - 1.0f;
            }

            public float Float(float max) {
                return Float() * max;
            }

            public float Float(float min, float max) {
                return Float() * (max - min) + min;
            }

            private uint SetNextState() {
                uint s = state;
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                return s;
            }

            private void CheckMinMax(int min, int max) {
                if (min > max)
                    throw new System.ArgumentException("min must be less than or equal to max");
            }
        }
    }
}
