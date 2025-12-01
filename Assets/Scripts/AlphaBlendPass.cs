using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class AlphaBlendPass : ScriptableRenderPass
{
    private const string profilerTag = "AlphaBlendPass";

    private Material material;
    private Mesh srcMesh;
    private Matrix4x4[] matrices;
    private int instanceCount;
    private GraphicsBuffer paramsBuffer;

    public AlphaBlendPass(Material mat, Mesh mesh, Matrix4x4[] matrices, GraphicsBuffer buffer)
    {
        this.material = mat;
        this.srcMesh = mesh;
        this.matrices = matrices;
        this.instanceCount = matrices.Length;
        this.paramsBuffer = buffer;
        renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (srcMesh == null)
        {
            Debug.LogWarning("Source Mesh is null");
            return;
        }

        CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

        RTHandle cameraColorRT = renderingData.cameraData.renderer.cameraColorTargetHandle;
        cmd.SetRenderTarget(cameraColorRT);
        if (paramsBuffer != null)
        {
            cmd.SetGlobalBuffer("paramsBuffer", paramsBuffer);
        }

        cmd.DrawMeshInstanced(
            srcMesh,
            0,
            material,
            0,
            matrices,
            instanceCount
        );

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void Cleanup()
    {
        // 今回は何もなし
    }
}
