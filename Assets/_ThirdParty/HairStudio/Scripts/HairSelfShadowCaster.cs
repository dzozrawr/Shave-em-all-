using UnityEngine;
using UnityEngine.Rendering;

namespace HairStudio
{
    public class HairSelfShadowCaster : MonoBehaviour
    {
        private Camera cam;
        public RenderTexture map;

        public new Light light;
        public int mapSize = 256;
        public float fiberSpacing = 0.005f;
        public float focusDistance = 2;
        public float sceneCaptureDistance = 0.5f;
        public HairRenderer hairRenderer;

        public GameObject tester;

        private void Awake() {
            var camGO = new GameObject("SelfShadowingCamera");
            camGO.hideFlags = HideFlags.DontSave;// | HideFlags.NotEditable | HideFlags.HideInHierarchy;
            cam = camGO.AddComponent<Camera>();
            cam.renderingPath = RenderingPath.Forward;
            cam.clearFlags = CameraClearFlags.Nothing;
            cam.depthTextureMode = DepthTextureMode.None;
            cam.useOcclusionCulling = false;
            cam.orthographic = true;
            cam.depth = -100;
            cam.aspect = 1f;
            cam.enabled = false;

            if (map == null) {
                map = new RenderTexture(mapSize, mapSize, 16, RenderTextureFormat.Shadowmap, RenderTextureReadWrite.Linear);
                map.filterMode = FilterMode.Bilinear;
                map.useMipMap = false;
                map.autoGenerateMips = false;
                map.Create();
            }


            GetComponent<HairRenderer>().material.SetTexture("_SelfShadowMap", map);
            GetComponent<HairRenderer>().material.SetMatrix("_SelfShadowMatrix", GetShadowMatrix());
        }

        private void Update() {
            cam.transform.rotation = light.transform.rotation;
            cam.transform.position = hairRenderer.transform.position - light.transform.forward * focusDistance; // TODO: Correct focus distance!

            cam.nearClipPlane = -sceneCaptureDistance;
            cam.farClipPlane = focusDistance * 2f;
            cam.orthographicSize = focusDistance;
            Render();
        }

        public void Render() {
            GetComponent<HairRenderer>().material.SetTexture("_SelfShadowMap", map);
            GetComponent<HairRenderer>().material.SetMatrix("_SelfShadowMatrix", GetShadowMatrix());
            GetComponent<HairRenderer>().material.SetFloat("_SelfShadowFiberSpacing", fiberSpacing); // TODO


            Graphics.SetRenderTarget(map);
            GL.Clear(true, true, Color.black);

            // Prepare material
            //Material mat = depthPassMaterial;
            //renderer.SetShaderParams(mat);

            // Create command buffer for self shadows
            CommandBuffer selfShadowCommandBuffer = new CommandBuffer();
            selfShadowCommandBuffer.name = "SelfShadows";
            selfShadowCommandBuffer.SetRenderTarget(new RenderTargetIdentifier(map));
            //foreach (var m in GetComponent<HairRenderer>().meshes) {
            //    selfShadowCommandBuffer.DrawMesh(m, Matrix4x4.identity, GetComponent<HairRenderer>().material);
            //}

            // Prepare cam & render
            var savedShadowDistance = QualitySettings.shadowDistance;
            QualitySettings.shadowDistance = 0f;

            cam.AddCommandBuffer(CameraEvent.AfterEverything, selfShadowCommandBuffer);
            cam.targetTexture = map;
            cam.cullingMask = 0;

            // Render shadows
            cam.Render();
            cam.RemoveAllCommandBuffers();

            QualitySettings.shadowDistance = savedShadowDistance;

        }

        public Matrix4x4 GetShadowMatrix() {
            var m_shadowSpaceMatrix = new Matrix4x4();
            var isD3D9 = false; //SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Direct3D9;
            var isD3D = isD3D9 || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Direct3D11;
            float to = isD3D9 ? 0.5f / (float)mapSize : 0f;
            float zs = isD3D ? 1f : 0.5f, zo = isD3D ? 0f : 0.5f;
            float db = -0.01f; // TODO: Real bias
            m_shadowSpaceMatrix.SetRow(0, new Vector4(0.5f, 0.0f, 0.0f, 0.5f + to));
            m_shadowSpaceMatrix.SetRow(1, new Vector4(0.0f, 0.5f, 0.0f, 0.5f + to));
            m_shadowSpaceMatrix.SetRow(2, new Vector4(0.0f, 0.0f, zs, zo + db));
            m_shadowSpaceMatrix.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

            var shadowViewMat = cam.worldToCameraMatrix;
            var shadowProjMat = GL.GetGPUProjectionMatrix(cam.projectionMatrix, false);
            return m_shadowSpaceMatrix * shadowProjMat * shadowViewMat;
        }
    }
}


