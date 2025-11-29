//Shader "Hidden/BlitMRT"
Shader "Custom/OITComposition"
{
    Properties
    {
        //_MainTex ("Texture", 2D) = "white" {}
        //[Toggle] _ShowNormal ("Show Normal", Float) = 0
        //_Intensity ("Intensity", Float) = 1.0
    }

    SubShader
    {
        ZTest Always
        ZWrite Off
        Cull Off
        //Blend One One

        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"


            TEXTURE2D(_MyAccumulationTexture);
            SAMPLER(sampler_MyAccumulationTexture);

            TEXTURE2D(_MyRevealageTexture);
            SAMPLER(sampler_MyRevealageTexture);

            TEXTURE2D(_CameraColorTexture);
            SAMPLER(sampler_CameraColorTexture);

            float _UseBlurPass;

            half4 frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord.xy;

                // Accumulation textureから色とアルファを取得
                half4 accum = SAMPLE_TEXTURE2D(_MyAccumulationTexture, sampler_MyAccumulationTexture, uv);

                // Revealage textureから透過率の積を取得
                float revealage = SAMPLE_TEXTURE2D(_MyRevealageTexture, sampler_MyRevealageTexture, uv).r;

                // 背景色（不透明オブジェクト）を取得
                half4 background = SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, uv);

                // ========================================
                // 最終的な合成
                // ========================================

                if (accum.a < 1e-6)
                {
                    discard;
                }

                // 累積された色を正規化
                half3 averageColor = accum.rgb / max(accum.a, 1e-6);

                // ========================================
                // 重要な修正: 正しいWeighted OIT合成式
                // ========================================
                // revealage = 背景の可視率（0=見えない, 1=完全に見える）
                // (1 - revealage) = 透明オブジェクトの不透明度

                // 方法1: 完全上書き方式（Blend One Zero使用）
                half3 finalColor = half3(0.0, 0.0, 0.0);

                if(_UseBlurPass > 0.5){
                    finalColor = averageColor;
                }else{
                    finalColor = averageColor * (1.0 - revealage) + background.rgb * revealage;
                }


                return half4(finalColor , 1.0);
                //return half4(accum.xyz, 1.0);

                // 方法2: アルファブレンド方式（Blend SrcAlpha OneMinusSrcAlpha使用）
                //half3 compositeColor = averageColor - background.rgb * revealage;
                //return half4(compositeColor, 1.0 - revealage);
            }
            ENDHLSL
        }
    }
}