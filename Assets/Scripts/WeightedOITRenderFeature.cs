using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Linq;
using Unity.Mathematics;
using System.Collections.Generic;
using System;


public class WeightedOITRenderFeature : ScriptableRendererFeature
{
    //GPU Instanced
    [Header("OIT Instanced Pass")]
    public Material weightedOITAccumulationMaterial;
    public Shader weightedOITResolveShader;
    public Mesh Mesh;

    private int instanceCount;

    private Matrix4x4[] matrices;

    #region RenderPass
    private WeightedOITAccumulationPass weightedOITAccumulationPass;
    private BlitPass blitPass;
    private WeightedOITResolvePass weightedOITResolvePass;
    #endregion



    public override void Create()
    {
        if (Mesh == null || weightedOITAccumulationMaterial == null)
        {
            return;
        }


        CleanupResources();

        int gridX = 25;
        int gridY = 25;
        float spacing = 0.5f;
        instanceCount = gridX * gridY;


        matrices = new Matrix4x4[instanceCount];


        for (int i = 0; i < instanceCount; i++)
        {
            int x = i % gridX - gridX/2;
            int y = i / gridX - gridY / 2;

            Vector3 p = new Vector3(
                x * spacing,
                0.0f,
                y * spacing
            );

            matrices[i] = Matrix4x4.TRS(p, Quaternion.identity, Vector3.one * 0.1f);
        }

        weightedOITAccumulationPass = new WeightedOITAccumulationPass(weightedOITAccumulationMaterial, Mesh, matrices);
        blitPass = new BlitPass();
        weightedOITResolvePass = new WeightedOITResolvePass(weightedOITResolveShader);
    }

    //RenderTargetの取得・セットアップ
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        RenderTargetBuffer.Setup(renderingData.cameraData.cameraTargetDescriptor);

        if (RenderTargetBuffer.OITColorAttachments == null || RenderTargetBuffer.OITColorAttachments.Length == 0)
        {
            return;
        }


        if(weightedOITAccumulationPass != null){
            //todo reveal
            weightedOITAccumulationPass.ConfigureTarget(RenderTargetBuffer.OITColorAttachments, RenderTargetBuffer.DepthAttachment);
            weightedOITAccumulationPass.ConfigureClear(ClearFlag.All, Color.clear);
        }

        if (weightedOITResolveShader != null)
        {
            //weightedOITResolvePass.Setup(MyRenderTargetBuffer.OITRenderTexture);
            weightedOITResolvePass.ConfigureTarget(RenderTargetBuffer.OITRenderTexture, RenderTargetBuffer.DepthAttachment);
            weightedOITResolvePass.ConfigureClear(ClearFlag.All, Color.clear);
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {

        if (weightedOITAccumulationPass == null　|| blitPass == null)
        {
            return;
        }
        renderer.EnqueuePass(weightedOITAccumulationPass);
        //renderer.EnqueuePass(blitPass);
        renderer.EnqueuePass(weightedOITResolvePass);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            CleanupResources();
        }
        RenderTargetBuffer.Dispose();
        base.Dispose(disposing);
    }

    private void CleanupResources()
    {
        weightedOITAccumulationPass?.Cleanup();
        weightedOITAccumulationPass = null;
        blitPass?.Cleanup();
        blitPass = null;
        weightedOITResolvePass?.Cleanup();
        weightedOITResolvePass = null;
    }
}
