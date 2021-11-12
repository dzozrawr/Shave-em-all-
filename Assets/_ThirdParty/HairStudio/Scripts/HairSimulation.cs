using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HairStudio
{
    public class HairSimulation : MonoBehaviour
    {
        private int physicStepKernelID, renderingStepKernelID;
        private readonly List<SegmentDTO> segmentDTOs = new List<SegmentDTO>();
        private readonly List<StrandDTO> strandDTOs = new List<StrandDTO>();
        private readonly List<ColliderInfo> colliderInfos = new List<ColliderInfo>();
        private float hairRange;

        public int debugColliderInRangeCount = 0;

        [NonSerialized] public ComputeBuffer strandBuffer;
        [NonSerialized] public ComputeBuffer segmentBuffer;
        [NonSerialized] public ComputeBuffer segmentForShadingBuffer;
        [NonSerialized] public ComputeBuffer velocityGridBuffer;
        [NonSerialized] public ComputeBuffer densityGridBuffer;
        [NonSerialized] public ComputeBuffer colliderBuffer;

        [NonSerialized] public List<Strand> strands = new List<Strand>();
        [NonSerialized] public List<StrandSegment> segments = new List<StrandSegment>();

        [HideInInspector] public ComputeShader computeShader;

        [Header("Physics")]
        [Tooltip("The minimum distance between strands and colliders.")]
        [Range(0.001f, 0.05f)] public float collisionDistance = 0.01f;

        [Tooltip("The weight of the strands.")]
        [Range(0, 0.01f)] public float weight = 0.001f;

        [Tooltip("The drag force (damping) that will slow the strands, representing the friction of the air with the medium. Greater values can be used to simulate hair in water.")]
        [Range(0, 10)] public float drag = 0.1f;

        [Tooltip("The stiffness of the hair strand, from a segment to the next. Great values may make the simulation unstable. Try to increase the number of iterations in that case.")]
        [Range(0, 0.8f)] public float localStiffness = 0.1f;

        [Tooltip("Global stiffness brings the strands back in its position relatively to the scalp. This is the distance from the root at which the global stiffness starts to decrease.")]
        [Range(0, 1)] public float globalStiffnessStart = 0f;
        [Tooltip("Global stiffness brings the strands back in its position relatively to the scalp. This is the distance from the root at which there is no more global stiffness.")]
        [Range(0, 2)] public float globalStiffnessEnd = 0.3f;

        [Header("Simulation")]
        [Tooltip("The number of iterations made to preserve strand segment correct length. 1 or 2 iterations are generally enough.")]
        [Range(1, 20)] public int lengthIterations = 2;

        [Tooltip("The number of iterations made to apply local stiffness. The more segments per strand, the more iterations are required to get stable simulation at the cost of performances.")]
        [Range(1, 20)] public int stiffnessIterations = 5;

        [Tooltip("The sphere colliders for hair strands. For the moment, only spheres are supported.")]
        [SerializeField] private List<SphereCollider> colliders = new List<SphereCollider>();
        [ReadOnlyProperty] public Vector3 externalForce;

        [Header("Hair/hair")]
        [Delayed, HideInInspector] public float voxelSize = 0.05f;
        [Delayed, HideInInspector] public int gridResolution = 32;

        [Tooltip("The hair/hair friction, used to make the strands move together for more realism.")]
        [Range(0, 0.5f)] public float friction = 0.05f;

        [Tooltip("The hair/hair repulsion, used to temporarily add artifical volume (electrocution, fear, hair dryer...)")]
        [Range(0, 400)] public float repulsion = 0f;

        private void Awake() {
            SetShaderConstants();
            ComputeScaledColliders();
        }

        private void OnValidate() {
            globalStiffnessStart = Mathf.Clamp(globalStiffnessStart, 0, globalStiffnessEnd);
            globalStiffnessEnd = Mathf.Clamp(globalStiffnessEnd, globalStiffnessStart, 2);

            SetShaderConstants();
            ComputeScaledColliders();
        }

        private void Start() {
            if (!strands.Any()) {
                Debug.LogWarning("The simulation does not contain any strand to simulate. Simulation is deactivated.", this);
                enabled = false;
                return;
            }
            if (!segments.Any()) throw new System.Exception("The simulation contains strands, but no segments, which is abnormal.");

            // we compute the max hair range
            // note that the last segment may have a smallest length, so the range is not exact.
            var scaleFactor = transform.lossyScale.x + transform.lossyScale.x + transform.lossyScale.z;
            scaleFactor /= 3;
            hairRange = strands.Max(strand => {
                var firstSegment = segments[strand.firstSegmentIndex];
                var length = firstSegment.length * strand.segmentCount / scaleFactor;
                var distanceToScalp = firstSegment.initialLocalPos.magnitude;
                return length + distanceToScalp;
            });

            // strand buffer
            strandDTOs.AddRange(strands.Select(strand => new StrandDTO(strand)));
            strandBuffer = new ComputeBuffer(strands.Count, StrandDTO.SIZE);
            strandBuffer.SetData(strandDTOs);

            // segment buffer
            segmentDTOs.AddRange(segments.Select(seg => new SegmentDTO(seg)));
            segmentBuffer = new ComputeBuffer(segmentDTOs.Count, SegmentDTO.SIZE);
            segmentBuffer.SetData(segmentDTOs);

            // segment for shading buffer
            segmentForShadingBuffer = new ComputeBuffer(segmentDTOs.Count,
                sizeof(float) * 3 +
                sizeof(float) * 3 +
                sizeof(float) * 3 +
                sizeof(float) * 3);

            SetShaderConstants();

            // velocity grid buffer
            velocityGridBuffer = new ComputeBuffer(gridResolution * gridResolution * gridResolution, sizeof(int) * 4);
            velocityGridBuffer.SetData(new List<Vector4>());

            //density grid buffer
            densityGridBuffer = new ComputeBuffer(gridResolution * gridResolution * gridResolution, sizeof(int));
            densityGridBuffer.SetData(new List<int>());

            physicStepKernelID = computeShader.FindKernel("PhysicStep");
            renderingStepKernelID = computeShader.FindKernel("RenderingStep");
        }

        private void SetShaderConstants() {
            if (computeShader == null) return;
            computeShader.SetFloat("_Gravity", Physics.gravity.y * weight);
            computeShader.SetFloat("_Drag", drag);
            computeShader.SetFloat("_Radius", collisionDistance);
            computeShader.SetFloat("_LocalStiffness", localStiffness);
            computeShader.SetFloat("_GlobalStiffnessStart", globalStiffnessStart);
            computeShader.SetFloat("_GlobalStiffnessEnd", globalStiffnessEnd);
            computeShader.SetInt("_LengthIterationCount", lengthIterations);
            computeShader.SetInt("_StiffnessIterationCount", stiffnessIterations);
            computeShader.SetFloat("_VoxelSize", voxelSize);
            computeShader.SetInt("_GridResolution", gridResolution);
            computeShader.SetFloat("_Friction", friction);
            computeShader.SetFloat("_Repulsion", repulsion);

            computeShader.SetInt("_StrandCount", strands.Count);
        }

        private void LateUpdate() {
            SetShaderConstants();

            computeShader.SetVector("_ScalpPosition", transform.position);
            computeShader.SetVector("_ScalpScale", transform.lossyScale);
            computeShader.SetVector("_ScalpRotation", QuaternionUtility.ToVector4(transform.rotation));

            computeShader.SetBuffer(renderingStepKernelID, "_Strands", strandBuffer);
            computeShader.SetBuffer(renderingStepKernelID, "_Segments", segmentBuffer);
            computeShader.SetBuffer(renderingStepKernelID, "_SegmentsForShading", segmentForShadingBuffer);

            if (colliderInfos.Count != 0) {
                colliderBuffer = new ComputeBuffer(colliderInfos.Count, ColliderDTO.SIZE);
                colliderBuffer.SetData(colliderInfos.Select(ci => new ColliderDTO(ci)).ToList());
            } else {
                colliderBuffer = new ComputeBuffer(1, ColliderDTO.SIZE);
            }
            computeShader.SetBuffer(renderingStepKernelID, "_ColliderInfos", colliderBuffer);

            computeShader.Dispatch(renderingStepKernelID, (int)Mathf.Ceil((float)strands.Count / 64), 1, 1);

            colliderBuffer?.Dispose();
        }

        private void FixedUpdate() {
            SetShaderConstants();

            computeShader.SetVector("_Force", externalForce);
            externalForce = Vector3.zero;
            // setting changing values
            computeShader.SetVector("_ScalpPosition", transform.position);
            computeShader.SetVector("_ScalpScale", transform.lossyScale);
            computeShader.SetVector("_ScalpRotation", QuaternionUtility.ToVector4(transform.rotation));
            computeShader.SetFloat("_DeltaTime", Time.fixedDeltaTime);
            computeShader.SetVector("_Center", transform.position);

            computeShader.SetBuffer(physicStepKernelID, "_Strands", strandBuffer);
            computeShader.SetBuffer(physicStepKernelID, "_Segments", segmentBuffer);

            //velocityGridBuffer.SetData(new int[(int)Mathf.Pow(gridResolution, 3) * 4]);
            //densityGridBuffer.SetData(new int[(int)Mathf.Pow(gridResolution, 3)]);
            computeShader.SetBuffer(physicStepKernelID, "_VelocityGrid", velocityGridBuffer);
            computeShader.SetBuffer(physicStepKernelID, "_DensityGrid", densityGridBuffer);

            // colliders
            var scaleFactor = transform.lossyScale.x + transform.lossyScale.x + transform.lossyScale.z;
            scaleFactor /= 3;
            var collidersInRange = colliderInfos.Where(ci => {
                return (ci.collider.transform.position - transform.position).sqrMagnitude < MathUtility.Square(hairRange * scaleFactor + ci.radius);
            });
            debugColliderInRangeCount = collidersInRange.Count();
            if (collidersInRange.Count() != 0) {
                colliderBuffer = new ComputeBuffer(collidersInRange.Count(), ColliderDTO.SIZE);
                colliderBuffer.SetData(collidersInRange.Select(ci => new ColliderDTO(ci)).ToList());
            } else {
                colliderBuffer = new ComputeBuffer(1, ColliderDTO.SIZE);
            }
            computeShader.SetBuffer(physicStepKernelID, "_ColliderInfos", colliderBuffer);

            computeShader.Dispatch(physicStepKernelID, (int)Mathf.Ceil((float)strands.Count / 64), 1, 1);

            colliderBuffer?.Dispose();
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

        private void OnDestroy() {
            strandBuffer?.Release();
            segmentBuffer?.Release();
            segmentForShadingBuffer?.Release();
            velocityGridBuffer?.Release();
            densityGridBuffer?.Release();
            colliderBuffer?.Release();
        }

        public void AddCollider(SphereCollider sc) {
            if (colliders.Contains(sc)) return;
            colliders.Add(sc);
            ComputeScaledColliders();
        }

        public void RemoveCollider(SphereCollider sc) {
            colliders.Remove(sc);
            ComputeScaledColliders();
        }
    }
}
