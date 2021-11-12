using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HairStudio
{
    public static class MathUtility
    {
        public static IEnumerable<Vector3> GetSmoothPath(List<Vector3> points, int intermediate, bool smoothCurvature = false) {
            var localPoints = points.ToList();
            Vector3 first = points.First(),
                second = points[1],
                beforeLast = points[points.Count - 2],
                last = points.Last();
            localPoints.Insert(0, first * 2 - second);
            localPoints.Add(last * 2 - beforeLast);
            float tStep = 1.0f / intermediate;
            for (int i = 1; i < localPoints.Count - 2; i++) {
                var p0 = localPoints[i - 1];
                var p1 = localPoints[i];
                var p2 = localPoints[i + 1];
                var p3 = localPoints[i + 2];
                if (smoothCurvature) {
                    var length = (p1 - p2).magnitude;
                    p0 = p1 + (p0 - p1).normalized * length;
                    p3 = p2 + (p3 - p2).normalized * length;
                }
                float t = 0;
                var localIntermediate = i == localPoints.Count - 3 ? intermediate + 1 : intermediate;
                for (int j = 0; j < localIntermediate; j++) {
                    yield return GetCatmullRomPosition(t, p0, p1, p2, p3);
                    t += tStep;
                }
            }
        }

        public static float Square(float value) {
            return value * value;
        }

        public static Vector3 GetCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3) {
            Vector3 a = 2f * p1;
            Vector3 b = p2 - p0;
            Vector3 c = 2f * p0 - 5f * p1 + 4f * p2 - p3;
            Vector3 d = -p0 + 3f * p1 - 3f * p2 + p3;
            //The cubic polynomial: a + b * t + c * t^2 + d * t^3
            return 0.5f * (a + (b * t) + (c * t * t) + (d * t * t * t));
        }
    }
}
