Shader "Unlit/Curvature"
{
    Properties
    {
        [HideInInspector]_MainTex ("Base (RGB)", 2D) = "white" {}
        _Scale ("Scale", Float) = 1
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 200

        Pass
        {
            Name "Edge Detection"
            
            HLSLPROGRAM
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float _Scale;
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.vertex = vertexInput.positionCS;
                output.uv = input.uv;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 d = _Scale * float3(1.0 / _ScreenParams.xy, 0.0);

                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                float3 normalU = SampleSceneNormals(input.uv + d.zy);
                float3 normalD = SampleSceneNormals(input.uv - d.zy);
                float3 normalR = SampleSceneNormals(input.uv + d.xz);
                float3 normalL = SampleSceneNormals(input.uv - d.xz);
                
                float edge = normalL.r - normalR.r + (normalU.g - normalD.g);
                return col * (0.5 + lerp(0.5, 1, 0.75 * edge));
            }

            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
    FallBack "Diffuse"
}