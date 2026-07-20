Shader "Custom/URP/CliffGrassBlend"
{
    Properties
    {
        _CliffTex ("崖 テクスチャ", 2D) = "white" {}
        _CliffColor ("崖 色", Color) = (1, 1, 1, 1)
        _CliffTiling ("崖 タイリング", Float) = 1

        _GrassTex ("草 テクスチャ", 2D) = "white" {}
        _GrassColor ("草 色", Color) = (1, 1, 1, 1)
        _GrassTiling ("草 タイリング", Float) = 1

        _BlendStart ("ブレンド開始", Range(0, 1)) = 0.25
        _BlendEnd ("ブレンド終了", Range(0, 1)) = 0.85
        _NoiseScale ("境目ノイズの細かさ", Range(0.1, 20)) = 4
        _NoiseStrength ("境目ノイズの強さ", Range(0, 0.5)) = 0.08
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            // フォワードレンダリング用のパス
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            // URPのシェーダーライブラリをインクルード
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // 頂点属性
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            // 変換行列
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };

            TEXTURE2D(_CliffTex);
            SAMPLER(sampler_CliffTex);
            TEXTURE2D(_GrassTex);
            SAMPLER(sampler_GrassTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _CliffTex_ST;
                float4 _GrassTex_ST;
                float4 _CliffColor;
                float4 _GrassColor;
                float _CliffTiling;
                float _GrassTiling;
                float _BlendStart;
                float _BlendEnd;
                float _NoiseScale;
                float _NoiseStrength;
            CBUFFER_END


            // 頂点シェーダー
            float Hash21(float2 p)
            {
                p = frac(p * float2(234.34, 435.345));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }


            // 2D値ノイズ関数
            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                // 4点のランダム値を作成し、補間してノイズを生成
                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));

                float x1 = lerp(a, b, f.x);
                float x2 = lerp(c, d, f.x);

                return lerp(x1, x2, f.y);
            }


            // 頂点シェーダー
            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs n = GetVertexNormalInputs(IN.normalOS);

                // ワールド空間での位置と法線を計算
                OUT.positionHCS = p.positionCS;
                OUT.positionWS = p.positionWS;
                OUT.normalWS = n.normalWS;
                OUT.uv = IN.uv;
                OUT.fogFactor = ComputeFogFactor(p.positionCS.z);

                return OUT;
            }


            // フラグメントシェーダー
            half4 frag(Varyings IN) : SV_Target
            {
                // 崖と草のUV座標を計算
                float2 cliffUV = IN.uv * (_CliffTex_ST.xy * _CliffTiling) + _CliffTex_ST.zw;
                float2 grassUV = IN.uv * (_GrassTex_ST.xy * _GrassTiling) + _GrassTex_ST.zw;

                // 崖と草のテクスチャをサンプリングし、色を取得
                half3 cliff = SAMPLE_TEXTURE2D(_CliffTex, sampler_CliffTex, cliffUV).rgb * _CliffColor.rgb;
                half3 grass = SAMPLE_TEXTURE2D(_GrassTex, sampler_GrassTex, grassUV).rgb * _GrassColor.rgb;

                // ノイズを使用してブレンドの境界を決定
                float noise = ValueNoise(IN.positionWS.xz * _NoiseScale);
                float blendSource = IN.uv.y + (noise - 0.5) * _NoiseStrength;
                half blend = smoothstep(_BlendStart, _BlendEnd, blendSource);

                half3 albedo = lerp(cliff, grass, blend);

                // ライティング計算
                float3 normalWS = normalize(IN.normalWS);
                Light mainLight = GetMainLight();
                half lightAmount = saturate(dot(normalWS, mainLight.direction));
                half3 ambient = SampleSH(normalWS);
                half3 color = albedo * (ambient + mainLight.color * lightAmount);

                color = MixFog(color, IN.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
