Shader "Custom/AlphaBlendInstanced_Lit"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,0.5)
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "AlphaBlend_Lit"

            ZWrite Off
            ZTest LEqual
            Cull Back

            // ---- 通常のアルファブレンド ----
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityIndirect.cginc"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float alpha : TEXCOORD2;
                float2 uv : TEXCOORD3;


                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Smoothness;
                float _Metallic;
            CBUFFER_END
            StructuredBuffer<float4> paramsBuffer;
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;


            // --------------------
            // Vertex
            // --------------------
            Varyings vert(Attributes input, uint svInstanceID : SV_InstanceID)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                uint instanceID = GetIndirectInstanceID(svInstanceID);

                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs   nor = GetVertexNormalInputs(input.normalOS);

                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS   = nor.normalWS;
                float normalizedDistance = paramsBuffer[instanceID].x;
                output.alpha = normalizedDistance;
                float2 uv = paramsBuffer[instanceID].yz;
                output.uv = uv;

                return output;
            }

            // --------------------
            // Fragment
            // --------------------
            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);


                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS   = normalize(input.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);

                SurfaceData surfaceData = (SurfaceData)0;
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                surfaceData.albedo = texColor.rgb;
                surfaceData.alpha     = _BaseColor.a;
                surfaceData.metallic  = _Metallic;
                surfaceData.smoothness= _Smoothness;
                surfaceData.normalTS  = float3(0,0,1);
                surfaceData.emission  = 0;
                surfaceData.occlusion = 1;

                Light mainLight = GetMainLight();

                float3 normal   = inputData.normalWS;
                float3 lightDir = mainLight.direction;

                float NdotL = saturate(dot(normal, lightDir));

                float3 ambient = SampleSH(normal);
                float3 diffuse = mainLight.color * NdotL;

                float3 viewDir = inputData.viewDirectionWS;
                float3 halfDir = normalize(lightDir + viewDir);

                float NdotH = saturate(dot(normal, halfDir));
                float specularPower = exp2(10 * _Smoothness + 1);

                float3 specular =
                    mainLight.color *
                    pow(NdotH, specularPower) *
                    _Smoothness;

                #ifdef _ADDITIONAL_LIGHTS
                uint lightCount = GetAdditionalLightsCount();
                for (uint i = 0; i < lightCount; ++i)
                {
                    Light light = GetAdditionalLight(i, input.positionWS);

                    float3 lc = light.color * light.distanceAttenuation;
                    float lNdotL = saturate(dot(normal, light.direction));
                    diffuse += lc * lNdotL;

                    float3 h = normalize(light.direction + viewDir);
                    float lNdotH = saturate(dot(normal, h));
                    specular += lc * pow(lNdotH, specularPower) * _Smoothness;
                }
                #endif

                float3 finalColor =
                    surfaceData.albedo * (ambient + diffuse)
                    + specular * (1.0 - _Metallic);

                float alpha = surfaceData.alpha;
                alpha = input.alpha;
                //return float4(finalColor, surfaceData.alpha);
                return float4(finalColor, alpha);
            }

            ENDHLSL
        }
    }
}
