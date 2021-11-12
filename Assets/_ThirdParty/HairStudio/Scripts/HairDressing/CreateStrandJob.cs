using UnityEngine;
using UnityEditor;
using Unity.Jobs;
using Unity.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

namespace HairStudio
{
    public struct CreateStrandJob : IJobParallelFor
    {
        [ReadOnly] public float rootRadius, clumpingMin, clumpingMax, waviness, wavinessFrequency;
        [ReadOnly] public int maxSegmentcount;
        [ReadOnly] public Matrix4x4 scalpTransform;
        [ReadOnly] public Quaternion scalpRotation;
        [ReadOnly] public NativeArray<GuideDTO> guidesA;
        [ReadOnly] public NativeArray<GuideDTO> guidesB;
        [ReadOnly] public NativeArray<GuideDTO> guidesC;
        [ReadOnly] public NativeArray<GuideDTO> guidesAB;
        [ReadOnly] public NativeArray<GuideDTO> guidesBC;
        [ReadOnly] public NativeArray<GuideDTO> guidesCA;
        [ReadOnly] public NativeArray<GuideSegmentDTO> guideSegments;
        [ReadOnly] public NativeArray<Keyframe> clumpingKeyFrames;
        [ReadOnly] public NativeArray<Keyframe> wavinessKeyFrames;
        [ReadOnly] public NativeArray<Keyframe> wavinessFrequencyKeyFrames;
        [ReadOnly] public NativeArray<int> randomSeeds;
        [ReadOnly] public NativeArray<RootDTO> roots;

        public NativeArray<Strand> strands;
        
        [NativeDisableParallelForRestriction]
        public NativeArray<StrandSegment> segments;

        public void Execute(int strandIndex) {
            int segmentIndex = strandIndex * maxSegmentcount;
            var clumpingCurve = new AnimationCurve(clumpingKeyFrames.ToArray());
            var wavinessCurve = new AnimationCurve(wavinessKeyFrames.ToArray());
            var wavinessFrequencyCurve = new AnimationCurve(wavinessFrequencyKeyFrames.ToArray());
            var rand = new RandomUtility.Random((uint)randomSeeds[strandIndex]);

            // find root
            var root = roots[strandIndex];
            var rootPos = root.localPos + Quaternion.LookRotation(root.localPos) * RandomUtility.InsideUnitCircleUniform(rand) * rootRadius;
            GuideDTO[] localGuides;
            switch (root.zone) {
                case Root.A: localGuides = guidesA.ToArray(); break;
                case Root.B: localGuides = guidesB.ToArray(); break;
                case Root.C: localGuides = guidesC.ToArray(); break;
                case Root.AB: localGuides = guidesAB.ToArray(); break;
                case Root.BC: localGuides = guidesBC.ToArray(); break;
                case Root.CA: localGuides = guidesCA.ToArray(); break;
                default: throw new Exception("Internal error, unknown zone " + root.zone);
            };

            // compute influencers
            var influencers = new List<GuideDTO>();
            var localGuideSegments = guideSegments.ToArray();
            GuideDTO closest = default;
            float closestDist = float.MaxValue;
            foreach(var guide in localGuides) {
                var dist = (localGuideSegments[guide.firstSegmentIndex].localPosition - rootPos).sqrMagnitude;
                if(dist < closestDist) {
                    closestDist = dist;
                    closest = guide;
                }
            }
            influencers.Add(closest);
            if (closest.mixedLock != 0) {
                float closestDist2 = float.MaxValue, closestDist3 = float.MaxValue;
                GuideDTO closest2 = default, closest3 = default;
                foreach(var guide in localGuides) {
                    if (guide.mixedLock == 0) continue;
                    var dist = (localGuideSegments[guide.firstSegmentIndex].localPosition - rootPos).sqrMagnitude;
                    if(dist <= closestDist) {
                        continue;
                    } else if (dist < closestDist2) {
                        if(!closest2.Equals(default)) {
                            closest3 = closest2;
                            closestDist3 = closestDist2;
                        }
                        closest2 = guide;
                        closestDist2 = dist;
                    } else if (dist < closestDist3) {
                        closest3 = guide;
                        closestDist3 = dist;
                    }
                }
                influencers.Add(closest2);
                influencers.Add(closest3);
            }

            var guideSet = new GuideSet(rootPos, influencers, localGuideSegments);

            var strand = new Strand();
            strand.localRotation = Quaternion.LookRotation(guideSet.GetLerpLocalRotation(), Vector3.up);

            float clumping = rand.Float(clumpingMin, clumpingMax);

            Vector3 rootOffset = rootPos - guideSet.GetLerpPosition(0, Vector3.zero, clumping * clumpingCurve.Evaluate(0));
            var strandLength = guideSet.GetLerpTotalLength();

            var segmentCount = Mathf.FloorToInt(strandLength / influencers.Max(g => g.segmentLength)) + 1;
            segmentCount = Mathf.Clamp(segmentCount, 2, maxSegmentcount);
            if (segmentCount > influencers.Max(g => g.segmentCount)) {
                Debug.Log($"segmentcount ({segmentCount}) superieur au max des guides ({influencers.Max(g => g.segmentCount)})");
            }
            if (segmentCount < 2) {
                throw new Exception("Abnormal segment count " + segmentCount + " in strand " + strandIndex + ". strand length is " + strandLength + " and max influencer's segment length is " + influencers.Min(guide => guide.segmentLength));
            }
            strand.segmentCount = segmentCount;

            var rateStep = 1.0f / (segmentCount - 1);

            for (int i = 0; i < segmentCount; i++) {
                var seg = new StrandSegment();
                if(i == 0) {
                    seg.frame = scalpRotation * strand.localRotation;
                }

                // rate
                float rate;
                if (i == 0) rate = 0;
                else if (i == segmentCount - 1) rate = 1;
                else rate = i * rateStep;
                seg.rate = rate;

                // can move if all guide can
                seg.canMove = 1;
                foreach(var guide in influencers) {
                    if (guide.segmentCount <= i) continue;
                    if(localGuideSegments[guide.firstSegmentIndex + i].canMove == 0) {
                        seg.canMove = 0;
                        break;
                    }
                }

                // position
                var guidePos = guideSet.GetLerpPosition(seg.rate, rootOffset, clumping * clumpingCurve.Evaluate(seg.rate));
                seg.initialLocalPos = guidePos;
                seg.pos = seg.previousPos = scalpTransform.MultiplyPoint(guidePos);


                seg.arbitraryUp = seg.frame * Vector3.up;
                segments[segmentIndex + i] = seg;
            }

            for (int i = 0; i < segmentCount; i++) {
                if (i == segmentCount - 1) break;
                var seg = segments[segmentIndex + i];
                var next = segments[segmentIndex + i + 1];
                var toNext = next.pos - seg.pos;
                // we look for a up vector that is not colinear with the segment.
                while (Vector3.Angle(toNext, seg.arbitraryUp) < 20 ||
                    Vector3.Angle(-toNext, seg.arbitraryUp) < 20) {
                    seg.arbitraryUp = RandomUtility.OnUnitSphere(rand);
                }

                // we apply the waviness
                if (waviness != 0) {
                    var noise = Perlin.Noise(seg.pos * wavinessFrequency * wavinessFrequencyCurve.Evaluate(next.rate));
                    next.pos += seg.arbitraryUp * noise * waviness * wavinessCurve.Evaluate(next.rate);
                }
                next.previousPos = next.pos;
                next.initialLocalPos = scalpTransform.inverse.MultiplyPoint(next.pos);

                toNext = next.pos - seg.pos;
                Quaternion rotation = Quaternion.LookRotation(toNext, seg.arbitraryUp);
                seg.localRestRotation = Quaternion.Inverse(seg.frame) * rotation;
                next.frame = rotation;
                seg.length = toNext.magnitude;
                segments[segmentIndex + i] = seg;
                segments[segmentIndex + i + 1] = next;
            }
            strands[strandIndex] = strand;
        }
    }
}