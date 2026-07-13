Shader "Custom/URP/FieldMaskBlend"
{
    Properties
    {
        [Header(Grass)]
        _GrassTex ("草 テクスチャ", 2D) = "white" {}
        _GrassColor ("草 色", Color) = (0.70, 0.60, 0.28, 1)

        [Header(Dirt Path)]
        _PathTex ("土道 テクスチャ", 2D) = "white" {}
        _PathColor ("土道 色", Color) = (0.90, 0.86, 0.74, 1)

        [Header(Stone Path)]
        _StoneTex ("石道 テクスチャ", 2D) = "white" {}
        _StoneColor ("石道 色", Color) = (1, 1, 1, 1)

        [Header(Terrain Mask)]
        _MaskTex ("地形マスク 黒=草 R=土道 G=石道", 2D) = "black" {}
        _FieldSize ("フィールドのサイズ XZ", Vector) = (112, 50, 0, 0)

        [Header(Blend Settings)]
        _Blur ("境目のぼかし幅", Range(0, 0.03)) = 0.008
        _Edge ("境目のなじみ幅", Range(0.001, 1)) = 0.35
        _PathSpread ("道の広がり 草側へ浸食", Range(0, 0.4)) = 0.15

        [Header(Noise Settings)]
        _NoiseScale ("浸食ノイズの細かさ", Range(0.2, 10)) = 2.0
        _NoiseStrength ("浸食ノイズの強さ", Range(0, 0.5)) = 0.25
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float2 maskUV      : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float3 positionWS  : TEXCOORD3;
                float  fogFactor   : TEXCOORD4;
            };

            // ----- プロパティ -----
            // 草テクスチャ
            TEXTURE2D(_GrassTex); SAMPLER(sampler_GrassTex);

            // 土道テクスチャ
            TEXTURE2D(_PathTex);  SAMPLER(sampler_PathTex);

            // 石道テクスチャ
            TEXTURE2D(_StoneTex);  SAMPLER(sampler_StoneTex);

            // RGB地形マスク
            TEXTURE2D(_MaskTex);  SAMPLER(sampler_MaskTex);


            CBUFFER_START(UnityPerMaterial)

                float4 _GrassTex_ST;
                float4 _PathTex_ST;
                float4 _StoneTex_ST;

                float4 _GrassColor;
                float4 _PathColor;
                float4 _StoneColor;

                float4 _FieldSize;

                float _Blur;
                float _Edge;
                float _PathSpread;

                float _NoiseScale;
                float _NoiseStrength;

            CBUFFER_END


            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs n = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = p.positionCS;
                OUT.positionWS  = p.positionWS;
                OUT.normalWS    = n.normalWS;

                OUT.uv          = IN.uv;

                // オブジェクトのXZ座標を0-1のマスクUVへ変換する
                OUT.maskUV      = IN.positionOS.xz / _FieldSize.xy + 0.5;
                OUT.fogFactor   = ComputeFogFactor(p.positionCS.z);
                return OUT;
            }


            // ----- ノイズ関数 -----

            // 2Dの座標から0～1の擬似乱数を生成する
            float Hash21(float2 p)
            {
                p = frac(p * float2(234.34, 435.345));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }


            // ワールドXZ用のバリューノイズ
            float ValueNoise(float2 p)
            {
                float2 cell = floor(p);

                float2 cellPosition = frac(p);

                cellPosition = cellPosition * cellPosition * (3.0 - 2.0 * cellPosition);

                float bottomLeft  = Hash21(cell);
                float bottomRight = Hash21(cell + float2(1.0, 0.0));
                float topLeft     = Hash21(cell + float2(0.0, 1.0));
                float topRight    = Hash21(cell + float2(1.0, 1.0));

                float bottom      = lerp(bottomLeft, bottomRight, cellPosition.x);
                float top         = lerp(topLeft, topRight, cellPosition.x);
                return lerp(bottom, top, cellPosition.y);
            }


            // マスク
            // ------------------------------------------------------------

            /// <summary>
            /// R=土道、G=石道のマスクをぼかして取得する。
            /// </summary>
            float2 SampleMaskBlurredRG(float2 uv, float blur)
            {
                float2 sum = float2(0.0, 0.0);

                // 5x5のBox blur
                [unroll]
                for (int x = -2; x <= 2; x++)
                {
                    [unroll]
                    for (int y = -2; y <= 2; y++)
                    {
                        float2 offset = float2(x, y) * blur;
                        float2 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv + offset).rg;

                        sum += mask;
                    }
                }

                return sum / 25.0;
            }

            /// <summary>
            /// マスクとノイズから、草と道のブレンド値を計算する。
            /// 土道と石道の両方で使用する。
            /// </summary>
            half CalculateTerrainBlend(half maskValue, float noise)
            {
                // マスクの境界部分だけを抽出する
                half boundaryBand = saturate(maskValue * (1.0 - maskValue) * 4.0);

                // 境界部分にノイズを加える
                half noisyMask = maskValue + (noise - 0.5) * _NoiseStrength * boundaryBand;

                // しきい値を下げると、道が草側へ浸食する
                half threshold = 0.5 - _PathSpread;

                half blendWidth = _Edge * 0.5;

                return smoothstep(threshold - blendWidth, threshold + blendWidth, noisyMask);
            }


            // フラグメント
            // ------------------------------------------------------------

            half4 frag (Varyings IN) : SV_Target
            {
                // --- 各テクスチャのUV ---
                float2 grassUV = IN.uv * _GrassTex_ST.xy + _GrassTex_ST.zw;
                float2 pathUV  = IN.uv * _PathTex_ST.xy  + _PathTex_ST.zw;
                float2 stoneUV = IN.uv * _StoneTex_ST.xy + _StoneTex_ST.zw;

                // --- 各テクスチャの色 ---
                half3 grassColor = SAMPLE_TEXTURE2D(_GrassTex, sampler_GrassTex, grassUV).rgb * _GrassColor.rgb;
                half3 pathColor  = SAMPLE_TEXTURE2D(_PathTex,  sampler_PathTex,  pathUV).rgb  * _PathColor.rgb;
                half3 stoneColor = SAMPLE_TEXTURE2D(_StoneTex, sampler_StoneTex, stoneUV).rgb * _StoneColor.rgb;

                // --- RGBマスク ---
                float2 terrainMask = SampleMaskBlurredRG(IN.maskUV, _Blur);

                // 赤チャンネル
                half dirtMask = terrainMask.r;

                // 緑チャンネル
                half stoneMask = terrainMask.g;

                // --- 境界用ノイズ ---
                // ワールド座標ベースのノイズ(2オクターブ)で境目を揺らす
                float noise = ValueNoise(IN.positionWS.xz * _NoiseScale) * 0.7
                            + ValueNoise(IN.positionWS.xz * _NoiseScale * 3.7) * 0.3;


                // --- 土道・石道のブレンド値 ---
                half dirtBlend  = CalculateTerrainBlend(dirtMask, noise);
                half stoneBlend = CalculateTerrainBlend(stoneMask, noise);


                // --- 草・土・石の合成 ---
                half grassWeight = saturate(1.0 - dirtBlend - stoneBlend);

                half dirtWeight = dirtBlend;
                half stoneWeight = stoneBlend;

                // 土と石の境界では合計が1を超えることがあるため、重みを正規化
                half totalWeight = max(grassWeight + dirtWeight + stoneWeight, 0.0001);

                grassWeight /= totalWeight;
                dirtWeight  /= totalWeight;
                stoneWeight /= totalWeight;

                half3 albedo = grassColor * grassWeight + pathColor * dirtWeight + stoneColor * stoneWeight;


                // --- URPライティング ---
                float3 normalWS = normalize(IN.normalWS);
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);

                Light mainLight = GetMainLight(shadowCoord);

                half normalLight = saturate(dot(normalWS, mainLight.direction));
                half3 directLight = mainLight.color * normalLight * mainLight.shadowAttenuation;
                half3 ambientLight = SampleSH(normalWS);

                half3 finalColor = albedo * (directLight + ambientLight);


                // --- フォグ ---
                finalColor = MixFog(finalColor, IN.fogFactor);
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }


        // ShadowCaster
        // ------------------------------------------------------------

        Pass
        {
            Name "ShadowCaster"

            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ColorMask 0


            HLSLPROGRAM

            #pragma vertex shadowVert
            #pragma fragment shadowFrag


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;


            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };


            struct ShadowVaryings
            {
                float4 positionHCS : SV_POSITION;
            };


            ShadowVaryings shadowVert(ShadowAttributes IN)
            {
                ShadowVaryings OUT;

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);

                float3 normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                   positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                   positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                OUT.positionHCS = positionCS;
                return OUT;
            }

            half4 shadowFrag (ShadowVaryings IN) : SV_Target
            {
                return 0;
            }

            ENDHLSL
        }
    }
}
