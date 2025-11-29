Shader "Custom/WeightedOIT"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,0.5)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }

        Pass
        {
            // 深度書き込みOFF、深度テストON
            ZWrite Off
            ZTest LEqual
            Cull Back

            // MRT用のブレンド設定
            // ColorAttachment0 (Accum): 加算ブレンド
            Blend 0 One One
            //Blend SrcAlpha OneMinusSrcAlpha

            // ColorAttachment1 (Reveal): 乗算ブレンド
            Blend 1 Zero OneMinusSrcColor

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float depth : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct FragOutput
            {
                float4 accum : SV_Target0;
                float4 reveal : SV_Target1;
            };

            float4 _Color;

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.depth = output.positionCS.z;

                return output;
            }

            FragOutput frag(Varyings input)
            {
                UNITY_SETUP_INSTANCE_ID(input);

                FragOutput output;

                // Weighted OITの重み計算
                float weight = _Color.a * clamp(0.03 / (1e-5 + pow(input.depth / 200.0, 4.0)), 1e-2, 3e3);

                // Accum (rgb * alpha * weight, alpha * weight)
                output.accum = float4(_Color.rgb * _Color.a, _Color.a);
                //output.accum = float4(_Color.rgb, _Color.a);
                //output.accum = float4(1.0, 0.0, 0.0, 0.1f);

                // Reveal (1 - alpha)
                output.reveal = float4(_Color.a, _Color.a, _Color.a, _Color.a);

                return output;
            }
            ENDHLSL
        }
    }
}