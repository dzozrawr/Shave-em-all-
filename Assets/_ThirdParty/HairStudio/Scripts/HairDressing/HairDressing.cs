using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace HairStudio
{
    [RequireComponent(typeof(HairSimulation))]
    public class HairDressing : MonoBehaviour
    {
        [NonSerialized]
        public float scaleFactor;

        [Tooltip("The mesh collider used to place the roots to demarcate the scalp. This collider doesn't affect the guides. Use Colliders property instead.")]
        public MeshCollider scalpCollider;
        public List<Guide> guides = new List<Guide>();

        [Range(0.001f, 0.1f), Tooltip("The length of each segment. This length is always in world unit, whatever the current scalp's scale.")]
        public float segmentLength = 0.01f;

        [Range(1, 30), Tooltip("The number of segments on each guide.")]
        public int segmentCountPerGuide = 16;

        [Range(0.001f, 0.1f), Tooltip("The minimum distance the start of a guide can approach the colliders.")]
        public float scalpSpacing = 0.005f;
        
        [Range(0.001f, 0.1f), Tooltip("The minimum distance the end of a guide can approach the colliders. Set a greater value here to ensure that tips will be on the outside, and to create volume.")]
        public float scalpSpacingAtTip = 0.005f;

        public RootProvider roots = new RootProvider();

        [Tooltip("The maximum distance at which a hair strand can appear around a root.")]
        public float rootRadius = 0.0025f;

        [Tooltip("The minimum length a guide can be trimmed.")]
        public float minimumHairLength = 0.005f;

        [Tooltip("The number of hair to generate per root.")]
        public float hairDensity = 1.0f;

        [Range(0, 1), Tooltip("The minimum clumping effect. For each strand, the clumping force is choosen randomly between min and max.")]
        public float minClumping;

        [Range(0, 1), Tooltip("The maximum clumping effect. For each strand, the clumping force is choosen randomly between min and max.")]
        public float maxClumping;

        [Tooltip("The scale of the clumping effect along the strand, from the root to the tip. Negative values are allowed to create volume.")]
        public AnimationCurve clumpingAlongStrand;

        [Range(0, 0.03f), Tooltip("The amplitude of the displacement.")]
        public float waviness;

        [Range(0, 1000), Tooltip("The frequency of the waves along the strand. 10 leads to long waves, 100 leads to short waves, 1000 leads to bed-head effect.")]
        public float wavinessFrequency;

        [Tooltip("The scale of the amplitude along the strand, from the root to the tip.")]
        public AnimationCurve wavinessAlongStrand;

        [Tooltip("The scale of the frequency along the strand, from the root to the tip.")]
        public AnimationCurve wavinessFrequencyAlongStrand;

        [Tooltip("The maximum number of segment per hair strand. More segments means more detailled fiber and reliable collisions with small colliders, but may lead to unstable stiffness. Leave to 0 tu use the guide segment count instead.")]
        public int maxSegmentCount = 16;

        [Tooltip("The random seed insures that the generated strands will be the same between generations. Change for any random number to avoid artifacts that can occur in some rare situations.")]
        public int randomSeed;

        [Tooltip("The colliders used while combing the hair guides. These colliders won't be used during hair simulation in play mode (see HairSimulation component).")]
        public List<SphereCollider> colliders = new List<SphereCollider>();

        [NonSerialized] public List<ColliderInfo> colliderInfos = new List<ColliderInfo>();
        [NonSerialized] public List<Guide> dirtyGuides = new List<Guide>();

        private void Awake() {
            if (!roots.Get().Any()) {
                Debug.LogWarning("HairDressing does not contain any root. Hair generation aborted. You must paint roots on the scalp using the root tool.", this);
                return;
            }
            if (!guides.Any()) {
                Debug.LogWarning("HairDressing does not contain any guide. Hair generation aborted. You must generate guides using the guide generator in styling tab.", this);
                return;
            }
            UnityEngine.Random.InitState(randomSeed);
            var simulation = GetComponent<HairSimulation>();
            // we assign root zone to guides
            var zonedGuides = new List<Guide>();
            foreach (var guide in guides) {
                var zonedGuide = guide;
                zonedGuide.zone = roots.Get().MinBy(root => (root.LocalPos - guide.segments[0].localPosition).sqrMagnitude).Zone;
                zonedGuides.Add(zonedGuide);
            }
            guides = zonedGuides;

            // generating strands
            var shuffledLocalRoots = roots.Get().Shuffle().ToList();
            int strandCount = (int)(shuffledLocalRoots.Count * hairDensity);

            var strandRoots = new List<RootDTO>(strandCount);
            int rootIndex = 0;
            for (int i = 0; i < strandCount; i++) {
                strandRoots.Add(new RootDTO(shuffledLocalRoots[rootIndex]));
                if (++rootIndex >= shuffledLocalRoots.Count) {
                    rootIndex = 0;
                }
            }

            var job = new CreateStrandJob();
            job.rootRadius = rootRadius;

            job.scalpTransform = transform.localToWorldMatrix;
            job.scalpRotation = transform.rotation;

            job.clumpingMin = minClumping;
            job.clumpingMax = maxClumping;
            job.clumpingKeyFrames = new NativeArray<Keyframe>(clumpingAlongStrand.keys, Allocator.TempJob);

            job.waviness = waviness;
            job.wavinessKeyFrames = new NativeArray<Keyframe>(wavinessAlongStrand.keys, Allocator.TempJob);
            job.wavinessFrequency = wavinessFrequency;
            job.wavinessFrequencyKeyFrames = new NativeArray<Keyframe>(wavinessFrequencyAlongStrand.keys, Allocator.TempJob);

            job.roots = new NativeArray<RootDTO>(strandRoots.ToArray(), Allocator.TempJob);

            var guideDTOs = new List<GuideDTO>();
            var guideSegmentDTOs = new List<GuideSegmentDTO>();
            int guideSegmentIndex = 0;
            foreach(var guide in guides) {
                guideSegmentDTOs.AddRange(guide.segments.Select(gs => new GuideSegmentDTO(gs)));
                guideDTOs.Add(new GuideDTO(guide, guideSegmentIndex, guideSegmentDTOs));
                guideSegmentIndex += guide.segments.Count;
            }
            job.guidesA = new NativeArray<GuideDTO>(guideDTOs.Where(g => g.zone == Root.A || g.zone == Root.AB || g.zone == Root.CA).ToArray(), Allocator.TempJob);
            job.guidesB = new NativeArray<GuideDTO>(guideDTOs.Where(g => g.zone == Root.B || g.zone == Root.AB || g.zone == Root.BC).ToArray(), Allocator.TempJob);
            job.guidesC = new NativeArray<GuideDTO>(guideDTOs.Where(g => g.zone == Root.C || g.zone == Root.BC || g.zone == Root.CA).ToArray(), Allocator.TempJob);
            job.guidesAB = new NativeArray<GuideDTO>(guideDTOs.Where(g => g.zone == Root.AB || g.zone == Root.A || g.zone == Root.B).ToArray(), Allocator.TempJob);
            job.guidesBC = new NativeArray<GuideDTO>(guideDTOs.Where(g => g.zone == Root.BC || g.zone == Root.B || g.zone == Root.C).ToArray(), Allocator.TempJob);
            job.guidesCA = new NativeArray<GuideDTO>(guideDTOs.Where(g => g.zone == Root.CA || g.zone == Root.C || g.zone == Root.A).ToArray(), Allocator.TempJob);
            job.guideSegments = new NativeArray<GuideSegmentDTO>(guideSegmentDTOs.ToArray(), Allocator.TempJob);

            job.randomSeeds = new NativeArray<int>(strandCount, Allocator.TempJob);
            for (int i = 0; i < strandCount; i++) {
                job.randomSeeds[i] = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
                var rand = new RandomUtility.Random((uint)job.randomSeeds[i]);
            }

            var test = new NativeArray<Strand>(10, Allocator.Temp);

            job.maxSegmentcount = maxSegmentCount != 0? maxSegmentCount : guides.Select(g => g.segments.Count).Max();
            job.strands = new NativeArray<Strand>(strandCount, Allocator.TempJob);
            job.segments = new NativeArray<StrandSegment>(job.maxSegmentcount * strandCount, Allocator.TempJob);

            job.Schedule(strandCount, 4).Complete();

            int index = 0;
            foreach (var strand in job.strands) {
                var str = strand;
                str.firstSegmentIndex = index * job.maxSegmentcount;
                index++;
                simulation.strands.Add(str);
            }
            simulation.segments = job.segments.ToList();

            job.clumpingKeyFrames.Dispose();
            job.wavinessKeyFrames.Dispose();
            job.wavinessFrequencyKeyFrames.Dispose();
            job.guidesA.Dispose();
            job.guidesB.Dispose();
            job.guidesC.Dispose();
            job.guidesAB.Dispose();
            job.guidesBC.Dispose();
            job.guidesCA.Dispose();
            job.guideSegments.Dispose();
            job.roots.Dispose();
            job.strands.Dispose();
            job.segments.Dispose();
            job.randomSeeds.Dispose();
        }

        private void OnValidate() {
            scaleFactor = transform.lossyScale.x + transform.lossyScale.x + transform.lossyScale.z;
            scaleFactor /= 3;
            ComputeScaledColliders();
        }

        private void ComputeScaledColliders() {
            colliderInfos.Clear();
            foreach (var sc in colliders) {
                colliderInfos.Add(new ColliderInfo() {
                    collider = sc,
                    radius = sc.radius * Mathf.Max(
                        sc.transform.lossyScale.x,
                        sc.transform.lossyScale.y,
                        sc.transform.lossyScale.z),
                });
            }
        }
    }
}
