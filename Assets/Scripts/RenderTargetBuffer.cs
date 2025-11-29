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

    static RTHandle acumulationRT;
    static RTHandle revealageRT;

    static RTHandle m_MyDepthTexture;
    static RTHandle m_CameraColorCopy;

    //PureなOITRenderingの出力と2回1Blurされた結果を書き込む
    static RTHandle m_OITRenderTexture;



    public static RTHandle AcumulationRT => acumulationRT;
    public static RTHandle RevealageRT => revealageRT;

    public static RTHandle MyDepthTexture => m_MyDepthTexture;
    public static RTHandle CameraColorCopy => m_CameraColorCopy;
    public static RTHandle OITRenderTexture => m_OITRenderTexture;


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

        var colorDesc = new RenderTextureDescriptor(desc.width, desc.height, RenderTextureFormat.ARGBHalf, 0);
        colorDesc.useMipMap = false;
        colorDesc.autoGenerateMips = false;


        var oitRenderDesc = new RenderTextureDescriptor(desc.width, desc.height, RenderTextureFormat.ARGBHalf, 0);
        oitRenderDesc.useMipMap = false;
        oitRenderDesc.autoGenerateMips = false;



        // このメソッド自体が「サイズが違う場合だけ再生成」してくれる
        RenderingUtils.ReAllocateIfNeeded(ref acumulationRT, accumDesc, FilterMode.Point, TextureWrapMode.Clamp, name: "AcumulationTexture");
        RenderingUtils.ReAllocateIfNeeded(ref revealageRT, revealDesc, FilterMode.Point, TextureWrapMode.Clamp, name: "RevealTexture");

        RenderingUtils.ReAllocateIfNeeded(ref m_MyDepthTexture, depthDesc, FilterMode.Point, TextureWrapMode.Clamp, name: "_MyDepthTexture");
        RenderingUtils.ReAllocateIfNeeded(ref m_CameraColorCopy, colorDesc, FilterMode.Point, TextureWrapMode.Clamp, name: "_CameraColorCopyTexture");
        RenderingUtils.ReAllocateIfNeeded(ref m_OITRenderTexture, oitRenderDesc, FilterMode.Point, TextureWrapMode.Clamp, name: "_OITRenderTexture");

        OITColorAttachments = new[] { acumulationRT, revealageRT };
        DepthAttachment = m_MyDepthTexture;
    }

    public static void Dispose()
    {
        DepthAttachment = null;
        OITColorAttachments = null;

        RTHandles.Release(acumulationRT);
        acumulationRT = null;


        RTHandles.Release(revealageRT);
        revealageRT = null;

        RTHandles.Release(MyDepthTexture);
        m_MyDepthTexture = null;


        RTHandles.Release(CameraColorCopy);
        m_CameraColorCopy = null;

        RTHandles.Release(OITRenderTexture);
        m_OITRenderTexture = null;


    }
}