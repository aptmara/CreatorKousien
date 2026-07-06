Shader "Custom/URP/FieldMaskBlend"
{
    Properties
    {
        [Header(Grass  base)]
        _GrassTex ("草 テクスチャ", 2D) = "white" {}
        _GrassColor ("草 色", Color) = (0.70, 0.60, 0.28, 1)

        [Header(Path  white in mask)]
        _PathTex ("道 テクスチャ", 2D) = "white" {}
        _PathColor ("道 色", Color) = (0.90, 0.86, 0.74, 1)

        [Header(Mask  white is path)]
        _MaskTex ("道マスク (白=道)", 2D) = "black" {}
        _FieldSize ("フィールドのサイズ XZ", Vector) = (112, 50, 0, 0)
        _Blur ("境目のぼかし幅", Range(0, 0.03)) = 0.008
        _Edge ("境目のなじみ幅", Range(0.001, 1)) = 0.35
        _PathSpread ("道の広がり (草側へ浸食)", Range(0, 0.4)) = 0.15
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
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
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

            TEXTURE2D(_GrassTex); SAMPLER(sampler_GrassTex);
            TEXTURE2D(_PathTex);  SAMPLER(sampler_PathTex);
            TEXTURE2D(_MaskTex);  SAMPLER(sampler_MaskTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _GrassTex_ST;
                float4 _PathTex_ST;
                float4 _GrassColor;
                float4 _PathColor;
                float4 _FieldSize;
                float  _Blur;
                float  _Edge;
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
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }




            // マスクをぼかしてグレーの中間帯を作る(5x5 box blur)
            float SampleMaskBlurred(float2 uv, float blur)
            {
                float sum = 0.0;
                [unroll] for (int x = -2; x <= 2; x++)
                {
                    [unroll] for (int y = -2; y <= 2; y++)
                    {
                        float2 o = float2(x, y) * blur * 0.5;
                        sum += SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv + o).r;
                    }
                }
                return sum / 25.0;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float2 gUV = IN.uv * _GrassTex_ST.xy + _GrassTex_ST.zw;
                float2 pUV = IN.uv * _PathTex_ST.xy  + _PathTex_ST.zw;

                half3 grass = SAMPLE_TEXTURE2D(_GrassTex, sampler_GrassTex, gUV).rgb * _GrassColor.rgb;
                half3 path  = SAMPLE_TEXTURE2D(_PathTex,  sampler_PathTex,  pUV).rgb * _PathColor.rgb;

                half m = SampleMaskBlurred(IN.maskUV, _Blur);

                // ワールド座標ベースのノイズ(2オクターブ)で境目を揺らす
                float noise = ValueNoise(IN.positionWS.xz * _NoiseScale) * 0.7
                            + ValueNoise(IN.positionWS.xz * _NoiseScale * 3.7) * 0.3;

                // 境目の帯(0<m<1)の中だけノイズを効かせる
                half band = saturate(m * (1.0 - m) * 4.0);
                half mNoisy = m + (noise - 0.5) * _NoiseStrength * band;

                // しきい値を0.5より下げる → 細い道が消えず、道が草側へ浸食する
                half th = 0.5 - _PathSpread;
                half w  = _Edge * 0.5;
                half blend = smoothstep(th - w, th + w, mNoisy);
                half3 albedo = lerp(grass, path, blend);

                float3 N = normalize(IN.normalWS);
                float4 sc = TransformWorldToShadowCoord(IN.positionWS);
                Light main = GetMainLight(sc);
                half3 lit = main.color * (saturate(dot(N, main.direction)) * main.shadowAttenuation);
                half3 ambient = SampleSH(N);
                half3 col = albedo * (lit + ambient);

                col = MixFog(col, IN.fogFactor);
                return half4(col, 1.0);
            }
            ENDHLSL
        }

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
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct V { float4 positionHCS : SV_POSITION; };

            V shadowVert (A IN)
            {
                V OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 nWS   = TransformObjectToWorldNormal(IN.normalOS);
                float4 cs = TransformWorldToHClip(ApplyShadowBias(posWS, nWS, _LightDirection));
                #if UNITY_REVERSED_Z
                    cs.z = min(cs.z, cs.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    cs.z = max(cs.z, cs.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                OUT.positionHCS = cs;
                return OUT;
            }
            half4 shadowFrag (V IN) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
