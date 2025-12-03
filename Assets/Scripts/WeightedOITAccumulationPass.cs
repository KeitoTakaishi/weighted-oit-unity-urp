using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class WeightedOITAccumulationPass : ScriptableRenderPass
{
    private string profilerTag = "WeightedOITRenderPass";
    private Material material;
    private Mesh srcMesh;
    private Matrix4x4[] matrices;
    private int instanceCount;
    private GraphicsBuffer paramsBuffer;


    public WeightedOITAccumulationPass(Material mat, Mesh mesh, Matrix4x4[] matrices, GraphicsBuffer paramsBuffer)
    {
        this.material = mat;
        this.srcMesh = mesh;
        this.matrices = matrices;
        this.instanceCount = matrices.Length;
        this.paramsBuffer = paramsBuffer;
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

        cmd.SetRenderTarget(RenderTargetBuffer.AccumulationRT, RenderTargetBuffer.DepthAttachment);
        cmd.ClearRenderTarget(true, true, Color.clear);
        cmd.SetRenderTarget(RenderTargetBuffer.RevealageRT, RenderTargetBuffer.DepthAttachment);
        cmd.ClearRenderTarget(false, true, Color.white);


        RenderTargetIdentifier[] mrt = new RenderTargetIdentifier[2];
        mrt[0] = RenderTargetBuffer.AccumulationRT;
        mrt[1] = RenderTargetBuffer.RevealageRT;
        cmd.SetRenderTarget(mrt, RenderTargetBuffer.DepthAttachment);
        if (paramsBuffer != null)
        {
            cmd.SetGlobalBuffer("paramsBuffer", paramsBuffer);
        }



        cmd.DrawMeshInstanced(srcMesh, 0, material, 0, matrices, instanceCount);
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void Cleanup()
    {
    }
}
