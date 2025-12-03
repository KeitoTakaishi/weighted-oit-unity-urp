using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class RenderTargetBuffer
{
    public static class ShaderPropertyId
    {
        public static readonly int ColorTex = Shader.PropertyToID("_ColorTex");
        public static readonly int NormalTex = Shader.PropertyToID("_NormalTex");
    }

    static RTHandle accumulationRT;
    static RTHandle revealageRT;

    static RTHandle depthTexture;

    public static RTHandle AccumulationRT => accumulationRT;
    public static RTHandle RevealageRT => revealageRT;

    public static RTHandle DepthTexture => depthTexture;

    public static RTHandle[] OITColorAttachments { get; private set; }
    public static RTHandle DepthAttachment { get; private set; }

    public static void Setup(RenderTextureDescriptor desc)
    {
        var accumDesc = new RenderTextureDescriptor(desc.width, desc.height, RenderTextureFormat.ARGBHalf, 0);
        accumDesc.useMipMap = false;
        accumDesc.autoGenerateMips = false;

        var revealDesc = new RenderTextureDescriptor(desc.width, desc.height, RenderTextureFormat.ARGBHalf, 0);
        revealDesc.useMipMap = false;
        revealDesc.autoGenerateMips = false;

        var depthDesc = new RenderTextureDescriptor(desc.width, desc.height, RenderTextureFormat.Depth, 24);
        depthDesc.useMipMap = false;
        depthDesc.autoGenerateMips = false;



        RenderingUtils.ReAllocateIfNeeded(ref accumulationRT, accumDesc, FilterMode.Point, TextureWrapMode.Clamp, name: "AcumulationTexture");
        RenderingUtils.ReAllocateIfNeeded(ref revealageRT, revealDesc, FilterMode.Point, TextureWrapMode.Clamp, name: "RevealTexture");

        RenderingUtils.ReAllocateIfNeeded(ref depthTexture, depthDesc, FilterMode.Point, TextureWrapMode.Clamp, name: "DepthTexture");

        OITColorAttachments = new[] { accumulationRT, revealageRT };
        DepthAttachment = depthTexture;
    }

    public static void Dispose()
    {
        DepthAttachment = null;
        OITColorAttachments = null;

        RTHandles.Release(accumulationRT);
        accumulationRT = null;


        RTHandles.Release(revealageRT);
        revealageRT = null;

        RTHandles.Release(DepthTexture);
        depthTexture = null;


    }
}