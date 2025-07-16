using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HeightmapRenderer : ScriptableRendererFeature
{
    class HeightmapRenderPass : ScriptableRenderPass
    {
        private Material heightMaterial;
        private RenderTargetIdentifier target;
        private Mesh fullScreenQuad;
        private RenderTexture outputTexture;
        public HeightmapRenderPass(Material mat, RenderTexture rt)
        {
            this.heightMaterial = mat;
            this.outputTexture = rt;
            this.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
            this.fullScreenQuad = GenerateFullscreenQuad();
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Render Heightmap");

            cmd.SetRenderTarget(outputTexture);
            cmd.ClearRenderTarget(true, true, Color.black);
            cmd.DrawMesh(fullScreenQuad, Matrix4x4.identity, heightMaterial);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private Mesh GenerateFullscreenQuad()
        {
            Mesh mesh = new Mesh();
            mesh.vertices = new Vector3[]
            {
            new Vector3(-1, -1, 0),
            new Vector3( 1, -1, 0),
            new Vector3( 1,  1, 0),
            new Vector3(-1,  1, 0),
            };
            mesh.uv = new Vector2[]
            {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1),
            };
            mesh.triangles = new int[]
            {
            0, 2, 1,
            0, 3, 2
            };
            return mesh;
        }
    }


    //----------------------------------------------------------------------------------------------------------------------------------------
    //HeightmapRenderPass
    //----------------------------------------------------------------------------------------------------------------------------------------

    public Material heightmapMaterial;
    public int textureWidth = 512;
    public int textureHeight = 512;

    private HeightmapRenderPass pass;
    private RenderTexture heightRT;

    HeightmapRenderPass m_ScriptablePass;

    public override void Create()
    {
        // Configures where the render pass should be injected.
        //m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        heightRT = new RenderTexture(textureWidth, textureHeight, 0, RenderTextureFormat.RFloat);
        heightRT.enableRandomWrite = true;
        heightRT.Create();

        pass = new HeightmapRenderPass(heightmapMaterial, heightRT);
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(pass);
    }
    public RenderTexture GetHeightTexture()
    {
        return heightRT;
    }
}


