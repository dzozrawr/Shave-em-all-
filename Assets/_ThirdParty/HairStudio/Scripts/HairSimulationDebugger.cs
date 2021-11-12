using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HairStudio
{
    [RequireComponent(typeof(HairSimulation))]
    public class HairSimulationDebugger : MonoBehaviour
    {
        private HairSimulation sim;
        private Material upMat, restMat;

        public bool drawVelocities, drawStrands = true;

        private void Awake() {
            sim = GetComponent<HairSimulation>();
#if UNITY_EDITOR
            upMat = GetMaterial("HairStudio_DebugRed");
            restMat = GetMaterial("HairStudio_DebugBlue");
#endif
        }

#if UNITY_EDITOR
        private Material GetMaterial(string name) {
            var guids = AssetDatabase.FindAssets(name);
            if (!guids.Any()) {
                Debug.LogWarning("Cannot find a ressource for HairStudio : " + name + ". Try reinstalling the asset or contact support.");
                return default;
            } else if (guids.Select(guid => AssetDatabase.GUIDToAssetPath(guid)).Distinct().Count() > 1) {
                Debug.LogWarning("An asset in your project uses the same name as a ressource for HairStudio (or this one is duplicated). Please fix the name collision. Name was " + name);
                foreach (var guid in guids) {
                    Debug.LogWarning("    " + AssetDatabase.GUIDToAssetPath(guid));
                }
                return default;
            }
            return AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guids.First()), typeof(Material)) as Material;
        }
#endif

        void OnDrawGizmos() {
            if (!Application.isPlaying) return;
            if (drawStrands) DrawStrands();
            if (drawVelocities) DrawVelocities();
        }

        private void DrawVelocities() {
            var reso = sim.gridResolution;
            var vels = new int[(int)Mathf.Pow(reso, 3) * 4];
            sim.velocityGridBuffer.GetData(vels);
            for (int x = 0, i = 0; x < reso; x++)
                for (int y = 0; y < reso; y++)
                    for (int z = 0; z < reso; z++, i += 4) {
                        Vector4 vel = new Vector4(vels[i], vels[i + 1], vels[i + 2], vels[i + 3]);
                        if (vel.w == 0) continue;

                        var voxel = new Vector3(
                            x - reso / 2,
                            y - reso / 2,
                            z - reso / 2) * sim.voxelSize + transform.position;
                        var oppositeVoxel = new Vector3(
                            voxel.x + sim.voxelSize,
                            voxel.y + sim.voxelSize,
                            voxel.z + sim.voxelSize);
                        var voxelCenter = (voxel + oppositeVoxel) / 2;

                        Color color = Color.red;
                        color.a = vel.w / 10000.0f;
                        Debug.DrawLine(voxelCenter, voxelCenter + (new Vector3(vel.x, vel.y, vel.z) / vel.w) / 1000, color);

                        Color gridColor = Color.yellow;
                        gridColor.a = vel.w / 100000.0f;

                        Debug.DrawLine(voxel, voxel + Vector3.right * sim.voxelSize, gridColor);
                        Debug.DrawLine(voxel, voxel + Vector3.up * sim.voxelSize, gridColor);
                        Debug.DrawLine(voxel, voxel + Vector3.forward * sim.voxelSize, gridColor);

                        Debug.DrawLine(oppositeVoxel, oppositeVoxel - Vector3.right * sim.voxelSize, gridColor);
                        Debug.DrawLine(oppositeVoxel, oppositeVoxel - Vector3.up * sim.voxelSize, gridColor);
                        Debug.DrawLine(oppositeVoxel, oppositeVoxel - Vector3.forward * sim.voxelSize, gridColor);
                    }
        }

        public struct SegmentForShading
        {
            public Vector3 pos;
            public Vector3 tangent;
            public Vector3 up;
        };

        private void DrawStrands() {
        //    var segments = new SegmentDTO[sim.segmentBuffer.count];
        //    sim.segmentBuffer.GetData(segments);

        //    var segmentsForShading = new SegmentForShading[sim.segmentForShadingBuffer.count];
        //    sim.segmentForShadingBuffer.GetData(segmentsForShading);

        //    var strands = new StrandDTO[sim.strandBuffer.count];
        //    sim.strandBuffer.GetData(strands);
        //    int strandIndex = 0;
        //    foreach (var strandDTO in strands) {
        //        // up
        //        //GL.Begin(GL.LINES);
        //        //restMat.SetPass(0);
        //        //GL.Vertex3(pos.x, pos.y, pos.z);
        //        //GL.Vertex3(up.x, up.y, up.z);
        //        //GL.End();
        //        strandIndex++;
        //        for (int i = strandDTO.firstSegmentIndex; i < strandDTO.firstSegmentIndex + strandDTO.nbSegments; i++) {
        //            var segDTO = segments[i];
        //            var segForShading = segmentsForShading[i];
        //            //if (segDTO.canMove != 0) continue;
        //            var frame = QuaternionUtility.FromVector4(segDTO.frame);
        //            var restRotation = QuaternionUtility.FromVector4(segDTO.localRestRotation);
        //            var pos = segForShading.pos;
        //            var frameForward = pos + frame * Vector3.forward * 0.003f;
        //            var frameUp = pos + frame * Vector3.up * 0.001f;
        //            var rest = pos + frame * restRotation * Vector3.forward * 0.004f;

        //            // up
        //            GL.Begin(GL.LINES);
        //            upMat.SetPass(0);
        //            GL.Vertex3(pos.x, pos.y, pos.z);
        //            GL.Vertex3(frameForward.x, frameForward.y, frameForward.z);
        //            GL.End();

        //            GL.Begin(GL.LINES);
        //            upMat.SetPass(0);
        //            GL.Vertex3(pos.x, pos.y, pos.z);
        //            GL.Vertex3(frameUp.x, frameUp.y, frameUp.z);
        //            //var initial = transform.TransformPoint(segDTO.initialLocalPos);
        //            //var initialUp = initial + Vector3.up * 0.02f;
        //            //GL.Vertex3(initial.x, initial.y, initial.z);
        //            //GL.Vertex3(initialUp.x, initialUp.y, initialUp.z);
        //            GL.End();

        //            // rest
        //            if (i == strandDTO.firstSegmentIndex + strandDTO.nbSegments - 1) break;
        //            GL.Begin(GL.LINES);
        //            restMat.SetPass(0);
        //            GL.Vertex3(pos.x, pos.y, pos.z);
        //            GL.Vertex3(rest.x, rest.y, rest.z);
        //            GL.End();
        //        }
        //    }
        }
    }
}
