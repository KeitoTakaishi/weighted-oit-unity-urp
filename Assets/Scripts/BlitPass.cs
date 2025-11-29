using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;


class BlitPass : ScriptableRenderPass
{
    private RTHandle source;
    private RTHandle destination;
    public BlitPass()
    {
        renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }


    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get("Custom Blit Color Texture Pass");

        source = renderingData.cameraData.renderer.cameraColorTargetHandle;
        destination = RenderTargetBuffer.CameraColorCopy;


        if (source == null)
        {
            Debug.LogWarning("CopyColorPass: source is null");
        }


        if (destination == null)
        {
            Debug.LogWarning("CopyColorPass: destination is null");
            //return;
        }

        if (source.rt == null)
        {
            Debug.LogWarning("CopyColorPass: source.rt is null");
            //return;
        }

        if (destination.rt == null)
        {
            Debug.LogWarning("CopyColorPass: destination.rt is null");
            //return;
        }

        // Step 7 の結果を、Step 8 のクリアが発生する前に自前 RT にコピー
        Blitter.BlitCameraTexture(cmd, source, destination);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void Cleanup()
    {
        if (source != null)
        {
            source.Release();
            source = null;
        }

        if (destination != null)
        {
            destination.Release();
            destination = null;
        }
    }
}