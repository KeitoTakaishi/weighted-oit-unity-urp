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
    public Material alphaBlendMaterial;

    public Shader weightedOITResolveShader;
    public Mesh Mesh;
    public bool UseOit = true;
    private int instanceCount;

    private Matrix4x4[] matrices;

    #region RenderPass
    private WeightedOITAccumulationPass weightedOITAccumulationPass;
    private WeightedOITResolvePass weightedOITResolvePass;
    private AlphaBlendPass alphaBlendPass;
    #endregion


    //[alpha, uv.x, uv.y, buffer]
    private GraphicsBuffer paramsBuffer;
    private Vector4[] paramsData;


    public override void Create()
    {
        if (Mesh == null || weightedOITAccumulationMaterial == null)
        {
            return;
        }


        CleanupResources();

        int gridX = 7;
        int gridY = 7;
        int gridZ = 7;
        float spacing = 1.0f;

        instanceCount = gridX * gridY * gridZ;
        matrices = new Matrix4x4[instanceCount];

        float offsetX = (gridX - 1) * 0.5f;
        float offsetY = (gridY - 1) * 0.5f;
        float offsetZ = (gridZ - 1) * 0.5f;

        int index = 0;

        for (int z = 0; z < gridZ; z++)
        {
            for (int y = 0; y < gridY; y++)
            {
                for (int x = 0; x < gridX; x++)
                {
                    Vector3 p = new Vector3(
                        (x - offsetX) * spacing,
                        (y - offsetY) * spacing,
                        (z - offsetZ) * spacing
                    );

                    matrices[index++] =
                        Matrix4x4.TRS(
                            p,
                            Quaternion.identity,
                            Vector3.one * 0.5f
                        );
                }
            }
        }

        paramsData = new Vector4[instanceCount];

        float maxDistance = Mathf.Sqrt(
            Mathf.Pow((gridX - 1) * 0.5f * spacing, 2) +
            Mathf.Pow((gridY - 1) * 0.5f * spacing, 2) +
            Mathf.Pow((gridZ - 1) * 0.5f * spacing, 2)
        );

        for (int i = 0; i < instanceCount; i++)
        {
            Vector3 pos = matrices[i].GetColumn(3);
            float distance = pos.magnitude;
            float normalizedDistance = distance / maxDistance;
            normalizedDistance = 1.0f - normalizedDistance;
            Vector2 uv = new Vector2(normalizedDistance, 0.5f);
            paramsData[i] = new Vector4(normalizedDistance, uv.x, uv.y, 1f);
        }

        paramsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, instanceCount, sizeof(float) * 4);
        paramsBuffer.SetData(paramsData);

        if (UseOit)
        {
            weightedOITAccumulationPass = new WeightedOITAccumulationPass(weightedOITAccumulationMaterial, Mesh, matrices, paramsBuffer);
            weightedOITResolvePass = new WeightedOITResolvePass(weightedOITResolveShader);
        }
        else
        {
            alphaBlendPass = new AlphaBlendPass(alphaBlendMaterial, Mesh, matrices, paramsBuffer);
        }

    }

    //RenderTargetの取得・セットアップ
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        RenderTargetBuffer.Setup(renderingData.cameraData.cameraTargetDescriptor);

        if (RenderTargetBuffer.OITColorAttachments == null || RenderTargetBuffer.OITColorAttachments.Length == 0)
        {
            return;
        }

        if (UseOit)
        {
            if(weightedOITAccumulationPass != null){
                //todo reveal
                //weightedOITAccumulationPass.ConfigureTarget(RenderTargetBuffer.OITColorAttachments, RenderTargetBuffer.DepthAttachment);
                //weightedOITAccumulationPass.ConfigureClear(ClearFlag.All, Color.clear);
            }

            if (weightedOITResolveShader != null)
            {
                weightedOITResolvePass.ConfigureTarget(renderingData.cameraData.renderer.cameraColorTargetHandle, renderingData.cameraData.renderer.cameraDepthTargetHandle);
            }
        }
        else
        {
            //enable alpha blend pass
        }


    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (UseOit)
        {
            if (weightedOITAccumulationPass == null || weightedOITResolvePass == null)
            {
                return;
            }
            renderer.EnqueuePass(weightedOITAccumulationPass);
            renderer.EnqueuePass(weightedOITResolvePass);
        }
        else
        {
            if (alphaBlendPass == null) return;
            renderer.EnqueuePass(alphaBlendPass);
        }
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
        weightedOITResolvePass?.Cleanup();
        weightedOITResolvePass = null;
        alphaBlendPass?.Cleanup();
        alphaBlendPass = null;

        paramsBuffer?.Release();
        paramsBuffer = null;

        matrices = null;
        paramsData = null;
    }
}
