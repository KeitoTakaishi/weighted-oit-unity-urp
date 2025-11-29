Shader "Unlit/WeightedOITGPUInstancing"
{
    Properties
    {
        [HideInInspector]
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {

        Tags {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }



        //Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}
        LOD 100

        HLSLINCLUDE

        StructuredBuffer<float4x4> _TransformMatrixArray;
        void transform_vertex(inout float3 position, uint instanceId)
        {
            float4x4 mat = _TransformMatrixArray[instanceId];
            position = mul(mat, float4(position, 1.0)).xyz;
        }

        ENDHLSL
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Zwrite Off
            Cull Back


            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

             // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitForwardPass.hlsl"
            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityIndirect.cginc"



            //sampler2D _MainTex;
            //float4 _MainTex_ST;
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;

            struct MyVaryings
            {
                float fogCoord : TEXCOORD1;
                float4 positionCS : SV_POSITION;
                float4 color :COLOR0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            MyVaryings vert (Attributes input, uint svInstanceID : SV_InstanceID)
            {
               //Varyings OUT;

               InitIndirectDrawArgs(0);
               MyVaryings output = (MyVaryings)0;

               UNITY_SETUP_INSTANCE_ID(input);
               UNITY_TRANSFER_INSTANCE_ID(input, output);
               UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
               uint instanceID = GetIndirectInstanceID(svInstanceID);

               float4x4 mat = _TransformMatrixArray[instanceID];
               float3 transformedPosition = mul(mat, float4(input.positionOS.xyz, 1.0)).xyz;
               float4 positionWS = float4(transformedPosition.xyz, 1.0);
               output.positionCS = mul(UNITY_MATRIX_VP, positionWS);

               return output;
            }


            void frag(MyVaryings input, out half4 outColor : SV_Target0)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);


                half4 c = half4(1.0, 1.0, 1.0, 1.0);
                //half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
               //half4 c = half4(texColor.x, texColor.y, texColor.z, input.alpha);

               outColor = c;

            }
            ENDHLSL
        }
    }
}
