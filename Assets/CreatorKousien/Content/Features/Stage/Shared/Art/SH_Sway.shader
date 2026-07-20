Shader "Custom/SH_Sway"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        _Saturation ("Saturation",Range(0,2)) = 1.2

        [Header(Shadow)]
        _ShadowColor("Shadow Color",Color) = (0.4,0.2,0.6,1.0)
        _ShadeThreshold ("Shade Threshold" , Range(0,1)) = 0.5

        [Header(Rim Light)]
        _RimColor ("Rim Color",Color) = (1,1,1,1)
        _RimPower ("Rim Power",Range(0.1,8)) = 3

        [Header(Specular)]
        _SpecColor ("Specular Color",Color) = (1,1,1,1)
        _SpecPower ("Specular Power",Range(1,256)) = 64
        _SpecIntensity ("Specular Intensity" , Range(0,2)) = 1.2

        [Header(Emission)]
        _EmissionColor ("Emission Color",Color) = (1,0.6,1,1)
        _EmissionStrength ("Emission Strength",Range(0,2)) = 0.3

        [Header(Alpha)]
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clip",Float) = 0
        _Cutoff ("Cutoff",Range(0,1)) = 0.5

    }

    SubShader
    {
        Tags {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"="Transparent"
        }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _ALPHATEST_ON
            

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float height : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                float _Saturation;
                float4 _RimColor; float _RimPower;
                float4 _ShadowColor; float _ShadeThreshold;
                float4 _SpecColor;float _SpecPower;float _SpecIntensity;
                float4 _EmissionColor;float _EmissionStrength;
                float _Cutoff;
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
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = GetWorldSpaceViewDir(IN.positionOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.height = t;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);

            #ifdef _ALPHATEST_ON
                clip(tex.a - _Cutoff);
            #endif

                half3 col = tex.rgb * _BaseColor;
                half  lumi = dot(col,half3(0.2126,0.7152,0.722));
                col = lerp(lumi.xxx,col, _Saturation);

                Light light = GetMainLight();

                // トゥーン陰
                half3 normal = normalize(IN.normalWS);
                half3 lightDir = normalize(light.direction);
                half3 viewDir = normalize(IN.viewDirWS);


                half ndl = saturate(dot(normal,lightDir));

                half shade = smoothstep(
                    _ShadeThreshold - 0.05,
                    _ShadeThreshold + 0.05,
                    ndl);

                half3 diffuse =
                    lerp(_ShadowColor.rgb,
                        col,
                        shade
                        );

                half3 halfDir = normalize(lightDir + viewDir);

                half spec = pow(saturate(dot(normal,halfDir)),
                        _SpecPower);

                spec *= _SpecIntensity;

                half3 specular =
                    spec * _SpecColor.rgb;

                half rim =
                    1.0 - saturate(dot(normal,viewDir));

                rim = pow(rim,_RimPower);

                half3 rimLight =
                    rim * _RimColor.rgb;

                half3 emission =
                    _EmissionColor.rgb *
                    _EmissionStrength;

                half3 color =
                    diffuse +
                    specular +
                    rimLight +
                    emission;

                return half4(color,tex.a * _BaseColor.a);
            }
            ENDHLSL
        }
    }
}
