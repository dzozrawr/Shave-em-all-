using UnityEngine;
using UnityEditor;
using HairStudio;
using System.Collections.Generic;
using System.Linq;

namespace HairStudio
{
    public class GuideSet
    {
        private IEnumerable<GuideDTO> guides;
        private GuideSegmentDTO[] segments;
        private List<float> influences = new List<float>();

        public GuideSet(Vector3 rootLocalPos, IEnumerable<GuideDTO> guides, GuideSegmentDTO[] segments) {
            this.guides = guides;
            this.segments = segments;
            List<float> distances = guides.Select(guide => {
                var distance = (rootLocalPos - segments[guide.firstSegmentIndex].localPosition).sqrMagnitude;
                return distance == 0 ? 1 : 1 / distance;
                }).ToList();
            var sumDist = distances.Sum();
            int index = 0;
            foreach (var guide in guides) {
                influences.Add(distances[index] / sumDist);
                index++;
            }
        }

        public Vector3 GetLerpLocalRotation() {
            Vector3 res = Vector3.zero;
            int index = 0;
            foreach (var guide in guides) {
                //Debug.Log("guide local rotation " + guide.localRotation.eulerAngles);
                res += guide.localRotation * Vector3.forward * influences[index];
                index++;
            }
            return res;
        }

        public float GetLerpTotalLength() {
            float res = 0;
            float max = guides.Max(g => g.Length);
            float min = guides.Min(g => g.Length);
            int index = 0;
            foreach (var guide in guides) {
                res += guide.Length * influences[index];
                index++;
            }
            return Mathf.Clamp(res, guides.Min(g => g.Length), guides.Max(g => g.Length));
        }

        public Vector3 GetLerpPosition(float rate, Vector3 offset, float clumping) {
            if (rate < 0 || rate > 1) throw new System.Exception("can't get Lerp position outside guide bounds. Was " + rate);
            Vector3 res = Vector3.zero;
            int guideIndex = 0;

            foreach (var guide in guides) {
                var posOnSeg = guide.GetLocalPosition(segments, rate);
                res += posOnSeg * influences[guideIndex];
                guideIndex++;
            }
            res += offset;
            if (clumping != 0) {
                Vector3 closest = guides.First().GetLocalPosition(segments, rate);
                res = Vector3.LerpUnclamped(res, closest, clumping);
            }
            return res;
        }


    }
}
