Shader "Custom/WeightedOIT_Lit"
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
            Name "WeightedOIT_Lit"

            ZWrite Off
            ZTest LEqual
            Cull Back

            // MRT用のブレンド設定
            Blend 0 One One          // Accum: 加算
            Blend 1 Zero OneMinusSrcColor  // Reveal: 乗算

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
             #pragma instancing_options

            // ライティング用のキーワード
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
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float depth : TEXCOORD2;
                float alpha : TEXCOORD3;
                float2 uv: TEXCOORD4;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct FragOutput
            {
                float4 accum : SV_Target0;
                float4 reveal : SV_Target1;
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

            Varyings vert(Attributes input, uint svInstanceID : SV_InstanceID)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                uint instanceID = GetIndirectInstanceID(svInstanceID);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.depth = positionInputs.positionCS.z;


                float normalizedDistance = paramsBuffer[instanceID].x;
                output.alpha = normalizedDistance;
                float2 uv = paramsBuffer[instanceID].yz;
                output.uv = uv;

                return output;
            }

            FragOutput frag(Varyings input)
            {
                UNITY_SETUP_INSTANCE_ID(input);

                FragOutput output;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalize(input.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);

                SurfaceData surfaceData = (SurfaceData)0;
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                surfaceData.albedo = texColor.rgb;
                surfaceData.alpha = texColor.a;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = float3(0, 0, 1);
                surfaceData.emission = 0;
                surfaceData.occlusion = 1;

                Light mainLight = GetMainLight();

                float3 normal = inputData.normalWS;
                float3 lightDir = mainLight.direction;
                float NdotL = saturate(dot(normal, lightDir));

                float3 ambient = SampleSH(normal);
                float3 diffuse = mainLight.color * NdotL;

                float3 viewDir = inputData.viewDirectionWS;
                float3 halfDir = normalize(lightDir + viewDir);
                float NdotH = saturate(dot(normal, halfDir));
                float specularPower = exp2(10 * _Smoothness + 1);
                float3 specular = mainLight.color * pow(NdotH, specularPower) * _Smoothness;

                #ifdef _ADDITIONAL_LIGHTS
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
                {
                    Light light = GetAdditionalLight(lightIndex, input.positionWS);
                    float3 attenuatedLightColor = light.color * light.distanceAttenuation;
                    float additionalNdotL = saturate(dot(normal, light.direction));
                    diffuse += attenuatedLightColor * additionalNdotL;

                    float3 additionalHalfDir = normalize(light.direction + viewDir);
                    float additionalNdotH = saturate(dot(normal, additionalHalfDir));
                    specular += attenuatedLightColor * pow(additionalNdotH, specularPower) * _Smoothness;
                }
                #endif

                float3 finalColor = surfaceData.albedo * (ambient + diffuse) + specular * (1.0 - _Metallic);
                float alpha = _BaseColor.a;
                alpha = input.alpha;




                float z = input.depth;
                float weight = alpha * clamp(10.0 / (1e-5 + abs(z)/5.0 + pow(abs(z), 2.0)/200.0), 1e-2, 3e3);
                output.accum = float4(finalColor * alpha, alpha) * weight;
                output.reveal = float4(alpha, alpha, alpha, alpha);

                return output;
            }
            ENDHLSL
        }
    }
}