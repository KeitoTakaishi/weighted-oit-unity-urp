Shader "Custom/OITComposition"
{
    Properties
    {
    }
    SubShader
    {
        ZTest Always
        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            TEXTURE2D(_AccumTexture);
            SAMPLER(sampler_AccumTexture);
            TEXTURE2D(_RevealTexture);
            SAMPLER(sampler_RevealTexture);
            float _UseBlurPass;
            half4 frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord.xy;
                // Accumulation textureから色とアルファを取得
                half4 accum = SAMPLE_TEXTURE2D(_AccumTexture, sampler_AccumTexture, uv);
                // Revealage textureから透過率の積を取得
                float revealage = SAMPLE_TEXTURE2D(_RevealTexture, sampler_RevealTexture, uv).r;
                // ========================================
                // 最終的な合成
                // ========================================
                if (accum.a < 1e-6)
                {
                    discard;
                }


                half3 averageColor = accum.rgb / max(accum.a, 1e-5);
                half alpha = 1.0 - revealage;
                return half4(averageColor, alpha);
                /*
                // 累積された色を正規化
                half3 averageColor = accum.rgb / max(accum.a, 1e-6);
                // ========================================
                // 重要な修正: 正しいWeighted OIT合成式
                // ========================================
                // revealage = 背景の可視率（0=見えない, 1=完全に見える）
                // (1 - revealage) = 透明オブジェクトの不透明度
                // 方法1: 完全上書き方式（Blend One Zero使用）
                half3 finalColor = half3(0.0, 0.0, 0.0);
                finalColor = averageColor * (1.0 - revealage) ;
                return half4(finalColor , 1.0);
                */
            }
            ENDHLSL
        }
    }
}