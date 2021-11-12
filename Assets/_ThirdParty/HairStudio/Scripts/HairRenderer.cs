using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HairStudio
{
    [RequireComponent(typeof(HairSimulation))]
    public class HairRenderer : MonoBehaviour
    {
        private const int LOD_COUNT = 30;

        private ComputeBuffer segmentDefBuffer;
        private HairSimulation simulation;
        private readonly int firstSegmentIndexPropertyID = Shader.PropertyToID("_FirstSegmentIndex");
        private readonly int offsetPropertyID = Shader.PropertyToID("_Offset");
        private readonly int segmentDefinitionsPropertyID = Shader.PropertyToID("_SegmentDefinitions");
        private readonly int segmentsPropertyID = Shader.PropertyToID("_SegmentsForShading");
        private readonly int HairThicknessPropertyID = Shader.PropertyToID("_ThicknessRoot");
        private Material localMaterial;

        private Dictionary<int, List<Mesh>> meshesByLOD = new Dictionary<int, List<Mesh>>();
        private List<Mesh> allMeshes = new List<Mesh>();
        private float distanceForMinDetailSqr, distanceForMaxDetailSqr;
        private float materialThickness;

        public Material material;

        [Tooltip("For performance purpose, the material is cloned and any change in the original material won't affect this renderer. Check this to force update every frame (works only in editor).")]
        public bool updateMaterial = false;

        [Tooltip("Tesselation allows to add intermediate vertices in the hair strands.\n 1 will not produce any intermediate vertex.\n Higher values will produce smoother curves at the cost of performances.")]
        [Range(1, 10)] public int tesselation = 1;

        [Tooltip("The distance for maximum detail. At that distance and closer, all strands are drawn with the minimum thickness.")]
        public float distanceForMaxDetail = 0.2f;
        [Tooltip("The distance for minimum detail. At that distance and farer, only a part of the strands are drawn and the thickness is increased.")]
        public float distanceForMinDetail = 10;

        private void Awake() {
            if (material == null) {
                Debug.LogError("The hair renderer does not have a material. The renderer is deactivated.", this);
                enabled = false;
                return;
            }
            simulation = GetComponent<HairSimulation>();
            for (int i = 0; i < LOD_COUNT; i++) {
                meshesByLOD[i] = new List<Mesh>();
            }

            distanceForMinDetailSqr = Mathf.Pow(distanceForMinDetail, 2);
            distanceForMaxDetailSqr = Mathf.Pow(distanceForMaxDetail, 2);
        }

        private void OnValidate() {
            distanceForMinDetailSqr = Mathf.Pow(distanceForMinDetail, 2);
            distanceForMaxDetailSqr = Mathf.Pow(distanceForMaxDetail, 2);
            localMaterial = null;
        }

        private void OnEnable() {
            Camera.onPreCull -= OnPreCullCamera;
            Camera.onPreCull += OnPreCullCamera;
        }
        private void OnDisable() {
            Camera.onPreCull -= OnPreCullCamera;
        }

        private void Start() {
            if (!simulation.strands.Any()) {
                Debug.LogWarning("The simulation does not contain any strand to render.", this);
                return;
            }

            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> indices = new List<int>();

            var strandsByLOD = (float)simulation.strands.Count / LOD_COUNT;
            var currentLOD = 0;
            var strandInLOD = 0;

            int strandIndex = 0;
            foreach (var strand in simulation.strands) {
                if (++strandInLOD > strandsByLOD) {
                    // time to switch to next LOD
                    var mesh = BuildMesh(verts, normals, uvs, indices);
                    meshesByLOD[currentLOD].Add(mesh);
                    allMeshes.Add(mesh);
                    verts.Clear();
                    normals.Clear();
                    uvs.Clear();
                    indices.Clear();

                    currentLOD++;
                    strandInLOD = 0;
                }
                if (verts.Count > 65000) {
                    var mesh = BuildMesh(verts, normals, uvs, indices);
                    meshesByLOD[currentLOD].Add(mesh);
                    allMeshes.Add(mesh);
                    verts.Clear();
                    normals.Clear();
                    uvs.Clear();
                    indices.Clear();
                }
                float rate = 0;
                float rateStep = 1.0f / (strand.segmentCount - 1);
                for (int segIndex = strand.firstSegmentIndex; segIndex < strand.firstSegmentIndex + strand.segmentCount - 1; segIndex++) {
                    // last segment is ignored
                    /////////////
                    // 0--1  4 //
                    // | /  /| //
                    // |/  / | //
                    // 2  3--5 //
                    /////////////
                    // tesselation
                    float time = 0;
                    float timeStep = 1.0f / (tesselation + 1);
                    for(int tess = 0; tess <= tesselation; tess++) {
                        for (int j = 0; j < 6; j++) {
                            int localSegIndex;
                            float localRate;
                            float localTime;
                            int side = j == 0 || j == 1 || j == 4 ? 1 : -1;
                            if (j == 0 || j == 2 || j == 3) {
                                localSegIndex = segIndex;
                                localRate = rate + rateStep * time;
                                localTime = time;
                            } else {
                                if(tess == tesselation) {
                                    // the vertex belong to the next segment
                                    localSegIndex = segIndex + 1;
                                    localRate = rate + rateStep;
                                    localTime = 0;
                                } else {
                                    localSegIndex = segIndex;
                                    localRate = rate + rateStep * (time + timeStep);
                                    localTime = time + timeStep;
                                }
                            }
                            verts.Add(new Vector3((float)strandIndex / simulation.strands.Count, localTime, 0));
                            normals.Add(new Vector3(localSegIndex, localRate, side));
                            uvs.Add(Vector2.zero);
                            indices.Add(verts.Count - 1);
                        }
                        time += timeStep;
                    }
                    rate += rateStep;
                }
                strandIndex++;
            }
            meshesByLOD[currentLOD].Add(BuildMesh(verts, normals, uvs, indices));

            var defs = new List<SegmentDef>();
            int counter = 0;
            foreach (var strand in simulation.strands) {
                var roughnessRate = UnityEngine.Random.value;
                var eccentricityRate = UnityEngine.Random.value;
                for(int i = strand.firstSegmentIndex; i < strand.firstSegmentIndex + strand.segmentCount - 1; i++) {
                    if(i < 0 || i > simulation.segments.Count - 1) {
                        Debug.LogError("index out of bound " + i + " count = " + simulation.segments.Count + " strand " + counter);
                        Debug.LogError("strand.firstSegmentIndex " + strand.firstSegmentIndex);
                        Debug.LogError("strand.segmentCount" + strand.segmentCount);
                    }
                    var seg = simulation.segments[i];
                    var sd = new SegmentDef();
                    sd.initialLocalPos = seg.initialLocalPos;
                    sd.roughnessRate = roughnessRate;
                    sd.eccentricityRate = eccentricityRate;
                    defs.Add(sd);
                }
                counter++;
            }

            segmentDefBuffer = new ComputeBuffer(defs.Count, sizeof(float) * 3 + sizeof(float) + sizeof(float));
            segmentDefBuffer.SetData(defs.ToArray());
        }

        private void OnPreCullCamera(Camera cam) {
            if (localMaterial == null) return;
            var sqrDistance = (cam.transform.position - transform.position).sqrMagnitude;
            var distanceRateForLOD = Mathf.InverseLerp(distanceForMaxDetailSqr, distanceForMinDetailSqr, sqrDistance);
            var LOD = Mathf.Lerp(1, LOD_COUNT - 1, 1 - distanceRateForLOD);
            var meshes = new List<Mesh>();
            for (int i = 0; i <= Mathf.FloorToInt(LOD); i++) {
                meshes.AddRange(meshesByLOD[i]);
            }

            localMaterial.SetFloat(HairThicknessPropertyID, materialThickness * (LOD_COUNT - LOD));
            localMaterial.SetVector(offsetPropertyID, transform.position);
            foreach (var mesh in meshes) {
                Graphics.DrawMesh(mesh, transform.position, Quaternion.identity, localMaterial, gameObject.layer, cam, 0, null, true, true);
            }
        }

        private void LateUpdate() {
            // we copy the material properties each frames to take material changes into account.
            if (localMaterial == null) {
                UpdateMaterial();
            }
#if UNITY_EDITOR
            if (updateMaterial) {
                UpdateMaterial();
            }
#endif
        }

        public void UpdateMaterial() {
            localMaterial = null;
            if (material == null) return;
            if (material.shader.name != "HairStudio") {
                Debug.LogError("The material " + material.name + " does not use the HairStudio shader. Material is discarded.", this);
                material = null;
                return;
            }
            localMaterial = new Material(material);
            //localMaterial.CopyPropertiesFromMaterial(material);
            materialThickness = material.GetFloat(HairThicknessPropertyID);
            localMaterial.SetBuffer(segmentDefinitionsPropertyID, segmentDefBuffer);
            localMaterial.SetBuffer(segmentsPropertyID, simulation.segmentForShadingBuffer);
        }

        private Mesh BuildMesh(List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs, List<int> indices) {
            var res = new Mesh();
            res.vertices = verts.ToArray();
            res.normals = normals.ToArray();
            res.uv = uvs.ToArray();
            res.triangles = indices.ToArray();
            res.bounds = new Bounds(Vector3.zero, Vector3.one);
            return res;
        }

        private struct SegmentDef
        {
            public Vector3 initialLocalPos;
            public float roughnessRate;
            public float eccentricityRate;
        }

        private void OnDestroy() {
            segmentDefBuffer?.Release();
        }
    }
}


