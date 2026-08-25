Shader "Custom/SH_Sway_Lit"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        _Saturation ("Saturation",Range(0,2)) = 1.2

        [Header(Sway)]
        _Beat("Beat",Float) = 1.0

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

        [KeywordEnum(Lit,Toon,Smooth)] _LightingMode("Lighting Mode",Float) = 0

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
        ZWrite On
        ZTest LEqual
        Cull Back

        Pass
        {
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _ALPHATEST_ON
            #pragma multi_compile _LIGHTINGMODE_LIT _LIGHTINGMODE_TOON _LIGHTINGMODE_SMOOTH

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES

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
                float3 positionWS : TEXCOORD4;
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

            float _Beat;
            #define TWO_PI 6.28318530718

            float3 JellyVertex(float3 pos)
            {
                float t = smoothstep(0.0f,1.0f,pos.y);
                float wave = sin(_Beat * TWO_PI - t * 0.6f);
                //float wave = sin(_Time.y * 0.6f);


                float center = 1.0f - abs(wave);

                float scaleY = lerp(1.0f,0.85f,center);
                float scaleX = lerp(1.0f,1.10f,center);

                float3 result = pos;

                result.x *= scaleX;
                result.y *= scaleY;

                result.x += wave * 0.1f * t;

                return result;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float4 localPos = float4(JellyVertex(IN.positionOS.xyz),1.0f);

                OUT.positionHCS = TransformObjectToHClip(localPos.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);

                float3 positionWS = TransformObjectToWorld(localPos.xyz);
                OUT.viewDirWS = GetWorldSpaceViewDir(positionWS);
                OUT.positionWS = positionWS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.height = smoothstep(0.0f,1.0f,localPos.y);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                Light light = GetMainLight();
                half3 normal = normalize(IN.normalWS);
                half3 lightDir = normalize(light.direction);
                half3 viewDir = normalize(IN.viewDirWS);
                half ndl = saturate(dot(normal,lightDir));
                half3 diffuse;

            #ifdef _ALPHATEST_ON
                clip(tex.a - _Cutoff);
            #endif

            #ifdef _LIGHTINGMODE_TOON
               half shade = step(_ShadeThreshold, ndl);

               diffuse = lerp(
                   _ShadowColor.rgb,
                   tex,
                   shade
                   );
            #endif

            #ifdef _LIGHTINGMODE_LIT
                half3 baseColor = tex.rgb * _BaseColor.rgb;

                // Environment Lighting や Light Probe から取得する環境光
                half3 ambient = SampleSH(normal);

                // Forward+ のライトループが inputData を参照するので用意する
                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = normal;
                inputData.viewDirectionWS = viewDir;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionHCS);

                // メインライト
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half3 direct = mainLight.color
                             * saturate(dot(normal, mainLight.direction))
                             * mainLight.distanceAttenuation
                             * mainLight.shadowAttenuation;

                // 追加ライト（Spot / Point）を加算
            #if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light addLight = GetAdditionalLight(lightIndex, IN.positionWS, half4(1, 1, 1, 1));
                    direct += addLight.color
                            * saturate(dot(normal, addLight.direction))
                            * addLight.distanceAttenuation
                            * addLight.shadowAttenuation;
                LIGHT_LOOP_END
            #endif

                diffuse = baseColor * (ambient + direct);
            #endif

            #ifdef _LIGHTINGMODE_SMOOTH
                half shade = smoothstep(
                    _ShadeThreshold - 0.05,
                    _ShadeThreshold + 0.05,
                    ndl
                    );

                    diffuse = lerp(
                        _ShadowColor.rgb,
                        tex,
                        shade
                        );
            #endif
                return half4(diffuse,tex.a * _BaseColor.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"

            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                float4 shadowCoords : TEXCOORD3;
            };

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)

            CBUFFER_END

            float _Beat;
            #define TWO_PI 6.28318530718

            float3 JellyVertex(float3 pos)
            {
                float t = smoothstep(0.0f,1.0f,pos.y);
                float wave = sin(_Beat * TWO_PI - t * 0.6f);

                float center = 1.0f - abs(wave);

                float scaleY = lerp(1.0f,0.85f,center);
                float scaleX = lerp(1.0f,1.10f,center);

                float3 result = pos;

                result.x *= scaleX;
                result.y *= scaleY;

                result.x += wave * 0.1f * t;

                return result;
            }

            ShadowVaryings ShadowVertex(Attributes IN)
            {
                ShadowVaryings OUT;

                float3 pos = JellyVertex(IN.positionOS.xyz);

                OUT.positionCS = TransformObjectToHClip(pos);

                // Get the VertexPositionInputs for the vertex position
                VertexPositionInputs positions = GetVertexPositionInputs(pos);

                // Convert the vertex position to a position on the shadow map
                float4 shadowCoordinates = GetShadowCoord(positions);

                // Pass the shadow coordinates to the fragment shader
                OUT.shadowCoords = shadowCoordinates;

                return OUT;
            }

            half4 ShadowFragment(ShadowVaryings IN) : SV_Target
            {
                half shadowAmount = MainLightRealtimeShadow(IN.shadowCoords);

                // Set the fragment color to the shadow value
                return shadowAmount;
            }

            ENDHLSL
        }
    }
}
