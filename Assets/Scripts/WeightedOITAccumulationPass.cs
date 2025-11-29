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


    public WeightedOITAccumulationPass(Material mat, Mesh mesh, Matrix4x4[] matrices)
    {
        this.material = mat;
        this.srcMesh = mesh;
        this.matrices = matrices;
        this.instanceCount = matrices.Length;
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



        //clear accum
        cmd.SetRenderTarget(RenderTargetBuffer.AcumulationRT, RenderTargetBuffer.DepthAttachment);
        cmd.ClearRenderTarget(true, true, Color.clear);
        //clear revealage
        cmd.SetRenderTarget(RenderTargetBuffer.RevealageRT, RenderTargetBuffer.DepthAttachment);
        cmd.ClearRenderTarget(false, true, Color.white);


        RenderTargetIdentifier[] mrt = new RenderTargetIdentifier[2];
        mrt[0] = RenderTargetBuffer.AcumulationRT;
        mrt[1] = RenderTargetBuffer.RevealageRT;
        cmd.SetRenderTarget(mrt, RenderTargetBuffer.DepthAttachment);


        //material.SetVector("_CaemraPosition", Camera.main.transform.position);

        cmd.DrawMeshInstanced(srcMesh, 0, material, 0, matrices, instanceCount);
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void Cleanup()
    {
    }
}
