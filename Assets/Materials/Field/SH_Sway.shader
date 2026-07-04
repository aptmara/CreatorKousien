Shader "Custom/SH_Sway"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        
    }

    SubShader
    {
        Tags {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"="Transparent"
        }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                //OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                float4 localPos = IN.positionOS;

                float t = smoothstep(0.0f,1.0f,localPos.y);
                float wave = sin(_Time.y * 5.0f - t * 0.6f);

                float center = 1.0f - abs(wave);

                float scaleY = lerp(1.0f,0.85f,center);
                float scaleX = lerp(1.0f,1.10f,center);

                localPos.x *= scaleX;
                localPos.y *= scaleY;

                localPos.x += wave * 0.1f * t;
                
                OUT.positionHCS = TransformObjectToHClip(localPos.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);

                clip(tex.a - 0.01f);

                return tex * _BaseColor;
            }
            ENDHLSL
        }
    }
}
