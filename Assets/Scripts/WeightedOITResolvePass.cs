using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class WeightedOITResolvePass : ScriptableRenderPass
{
    private Material material;
    private Shader shader;
    private RTHandle blurRenderTarget;

    public WeightedOITResolvePass(Shader shader)
    {
        this.shader = shader;
        this.material = new Material(this.shader);
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public void Setup()
    {
        if (material == null)
        {
            this.material = new Material(this.shader);
        }
    }


    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        //ConfigureInput(ScriptableRenderPassInput.Color);
        ConfigureTarget(renderingData.cameraData.renderer.cameraColorTargetHandle, renderingData.cameraData.renderer.cameraDepthTargetHandle);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {

        if (material == null) return;
        CommandBuffer cmd = CommandBufferPool.Get("WeightedOITResolvePass");
        //cmd.SetGlobalTexture("_CameraColorTexture", RenderTargetBuffer.CameraColorCopy);
        cmd.SetGlobalTexture("_MyAccumulationTexture", RenderTargetBuffer.MyAccumTexture);
        cmd.SetGlobalTexture("_MyRevealageTexture", RenderTargetBuffer.MyRevealTexture);

        //default Color Render TargetÇ…çáê¨åãâ ÇèoóÕ
        RTHandle cameraTargetHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;
        Blitter.BlitCameraTexture(cmd, RenderTargetBuffer.MyAccumTexture, cameraTargetHandle, material, 0);
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void Cleanup()
    {
        if (material != null)
        {
            CoreUtils.Destroy(material);
            material = null;
        }
    }
}
