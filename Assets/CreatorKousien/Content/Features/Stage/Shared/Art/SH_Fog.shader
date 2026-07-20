Shader "Custom/SH_Fog"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (0.045, 0.065, 0.06, 0.62)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        _Alpha("Alpha", Range(0, 1)) = 0.58
        _NoiseStrength("Noise Strength", Range(0, 1)) = 0.82
        _NoiseScale("Noise Scale", Float) = 0.35
        _NoiseSpeed("Noise Speed", Vector) = (0.32, 0.11, 0, 0)
        _WindDirection("Wind Direction", Vector) = (0.85, 0.35, 0, 0)
        _WarpStrength("Warp Strength", Range(0, 2)) = 1.25
        _Turbulence("Turbulence", Range(0, 1)) = 0.72
        _Puffiness("Puffiness", Range(0, 1)) = 0.86
        _BreathStrength("Breath Strength", Range(0, 0.4)) = 0.18
        _DepthFadeStrength("Depth Fade Strength", Range(0, 1)) = 1
        _DepthFadeDistance("Depth Fade Distance", Float) = 6
        _EdgeFade("Edge Fade", Range(0.001, 0.95)) = 0.72
        _EdgeNoiseStrength("Edge Noise Strength", Range(0, 0.9)) = 0.55
        _Dissolve("Dissolve", Range(0, 1)) = 0.38
        _DissolveSoftness("Dissolve Softness", Range(0.01, 0.7)) = 0.38
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float4 screenPosition : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                half _Alpha;
                half _NoiseStrength;
                float _NoiseScale;
                float2 _NoiseSpeed;
                float2 _WindDirection;
                half _WarpStrength;
                half _Turbulence;
                half _Puffiness;
                half _BreathStrength;
                half _DepthFadeStrength;
                float _DepthFadeDistance;
                float _EdgeFade;
                half _EdgeNoiseStrength;
                half _Dissolve;
                half _DissolveSoftness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.screenPosition = ComputeScreenPos(OUT.positionHCS);
                return OUT;
            }

            /// <summary>
            /// テクスチャ未設定でも霧が白い板にならないよう、手続き型ノイズを生成する。
            /// </summary>
            float Hash21(float2 value)
            {
                value = frac(value * float2(123.34, 345.45));
                value += dot(value, value + 34.345);
                return frac(value.x * value.y);
            }

            /// <summary>
            /// なめらかな値ノイズを返す。
            /// </summary>
            float ValueNoise(float2 value)
            {
                float2 cell = floor(value);
                float2 local = frac(value);
                float2 curve = local * local * (3.0 - 2.0 * local);

                float bottomLeft = Hash21(cell);
                float bottomRight = Hash21(cell + float2(1.0, 0.0));
                float topLeft = Hash21(cell + float2(0.0, 1.0));
                float topRight = Hash21(cell + float2(1.0, 1.0));

                float bottom = lerp(bottomLeft, bottomRight, curve.x);
                float top = lerp(topLeft, topRight, curve.x);
                return lerp(bottom, top, curve.y);
            }

            /// <summary>
            /// 複数スケールのノイズを重ね、自然な霧の塊を作る。
            /// </summary>
            float FractionalBrownianMotion(float2 value)
            {
                float sum = 0.0;
                float amplitude = 0.5;

                [unroll]
                for (int index = 0; index < 4; index++)
                {
                    sum += ValueNoise(value) * amplitude;
                    value = value * 2.03 + float2(19.19, 7.31);
                    amplitude *= 0.52;
                }

                return saturate(sum);
            }

            /// <summary>
            /// ノイズ座標をゆっくり歪ませ、直線的に流れるだけの動きを避ける。
            /// </summary>
            float2 CalculateFlowUv(float2 worldUv, float time)
            {
                float2 wind = normalize(_WindDirection + 0.0001);
                float2 crossWind = float2(-wind.y, wind.x);
                float2 mainDrift = wind * time * _NoiseSpeed.x;
                float2 crossDrift = crossWind * sin(time * 0.47) * _NoiseSpeed.y;

                float warpA = FractionalBrownianMotion(worldUv * 0.7 + mainDrift * 0.9 + time * 0.09);
                float warpB = FractionalBrownianMotion(worldUv * 0.7 - crossDrift * 1.1 - time * 0.07);
                float2 warp = (float2(warpA, warpB) - 0.5) * _WarpStrength;

                return worldUv + mainDrift + crossDrift + warp;
            }

            /// <summary>
            /// 地形との交差部分を柔らかくして、板ポリ感を抑える。
            /// </summary>
            half CalculateDepthFade(float4 screenPosition, float3 positionWS)
            {
                float2 screenUv = screenPosition.xy / screenPosition.w;
                float sceneRawDepth = SampleSceneDepth(screenUv);
                float sceneEyeDepth = LinearEyeDepth(sceneRawDepth, _ZBufferParams);
                float surfaceEyeDepth = -TransformWorldToView(positionWS).z;
                half depthFade = saturate((sceneEyeDepth - surfaceEyeDepth) / max(_DepthFadeDistance, 0.001));
                return lerp(1.0, depthFade, _DepthFadeStrength);
            }

            /// <summary>
            /// メッシュ外周の直線的な境界を薄くして、霧の切れ目を隠す。
            /// </summary>
            half CalculateEdgeFade(float2 uv, half edgeNoise)
            {
                float2 edgeDistance = min(uv, 1.0 - uv);
                float noisyEdge = min(edgeDistance.x, edgeDistance.y);
                noisyEdge += (edgeNoise - 0.5) * _EdgeNoiseStrength;
                return smoothstep(0.0, _EdgeFade, noisyEdge);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 worldUv = IN.positionWS.xz * max(_NoiseScale, 0.001);
                float time = _Time.y;
                float2 flowUv = CalculateFlowUv(worldUv, time);
                float2 detailUv = CalculateFlowUv(worldUv * 2.4 + 17.0, time * 0.73);
                float2 edgeUv = IN.uv * 2.6 + CalculateFlowUv(worldUv * 0.55 + 5.0, time * 0.38);

                float slowNoise = FractionalBrownianMotion(flowUv);
                float detailNoise = FractionalBrownianMotion(detailUv);
                float edgeNoise = FractionalBrownianMotion(edgeUv);
                float turbulentNoise = FractionalBrownianMotion(flowUv * 5.0 + float2(time * -0.28, time * 0.41));

                half baseAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a;
                half noise = saturate(slowNoise * 0.68 + detailNoise * 0.22 + turbulentNoise * _Turbulence * 0.18);
                half puffMask = lerp(noise, smoothstep(_Dissolve, saturate(_Dissolve + _DissolveSoftness), noise), _Puffiness);
                half breathNoise = FractionalBrownianMotion(worldUv * 1.15 + time * 0.24);
                half breath = 1.0 + (breathNoise - 0.5) * _BreathStrength;
                half depthFade = CalculateDepthFade(IN.screenPosition, IN.positionWS);
                half edgeFade = CalculateEdgeFade(IN.uv, edgeNoise);

                half fogShape = lerp(1.0, puffMask, _NoiseStrength);
                half alpha = baseAlpha * fogShape * breath * depthFade * edgeFade * _BaseColor.a * _Alpha;
                half4 color = half4(_BaseColor.rgb, alpha);
                return color;
            }
            ENDHLSL
        }
    }
}
