Shader "Custom/Collectible/SH_GummyObject"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        _SpecularColor("Specular Color", Color) = (1, 1, 1, 1)
        _Smoothness("Smoothness", Range(0,1)) = 0.8
        _SubsurfaceColor("Subsurface Color", Color) = (1, 0.5, 0.6, 1)
        _SubsurfaceStrength("Subsurface Strength", Range(0,2)) = 0.6
        _FresnelF0("Fresnel F0 (color)", Color) = (0.02, 0.02, 0.02, 1)
        _Clearcoat("Clearcoat Intensity", Range(0,1)) = 0.0
        _ClearcoatSmoothness("Clearcoat Smoothness", Range(0,1)) = 0.98
    }

    SubShader
    {

        Pass
        {

            Tags
            {
                "RenderType" = "Transparent"

                "Queue" = "Transparent"

                "RenderPipeline" = "UniversalPipeline"

            }


            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };


            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 posWS : TEXCOORD2; 
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                half4 _SpecularColor;
                half _Smoothness;
                half4 _SubsurfaceColor;
                half _SubsurfaceStrength;
                half _Clearcoat;
                half _ClearcoatSmoothness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                // 位置とUV
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                // 法線
                OUT.normalWS = normalize(mul((float3x3)unity_ObjectToWorld, IN.normalOS));
                // ワールド位置
                OUT.posWS = mul(unity_ObjectToWorld, IN.positionOS).xyz;
                return OUT;
            }

            static inline float SmoothnessToExponent(float s)
            {
                return lerp(8.0, 512.0, saturate(s));
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                 // ベース色取得
                half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                half3 baseColor = baseTex.rgb * _BaseColor.rgb;
                half alpha = baseTex.a * _BaseColor.a;

                // 正規化
                float3 N = normalize(IN.normalWS);                         // ワールド空間法線（頂点で計算して渡す）
                float3 V = normalize(_WorldSpaceCameraPos - IN.posWS);     // カメラ方向（ビュー方向）

                // cosθを取得
                float cosTheta = saturate(dot(N, V));

                // --- Schlick の近似（色付き F0）---
                float3 F0 = float3(0.7f, 0.7f, 0.7f);                   // 非金属の目安。色付きにする場合は RGB を変える
                float3 fresnelSchlick = F0 + (1.0 - F0) * pow(1.0 - cosTheta, 5.0);

                // 光源方向（可能ならシーンの主光源を使う、なければ上向き）
                float3 L;
                #if defined(_WorldSpaceLightPos0)
                    if (_WorldSpaceLightPos0.w == 0.0)
                    {
                        // directional: _WorldSpaceLightPos0.xyz はライト方向（Unity の定義に依存）
                        L = normalize(_WorldSpaceLightPos0.xyz);
                    }
                    else
                    {
                        // point light: 方向は光からサーフェスへのベクトル
                        L = normalize(_WorldSpaceLightPos0.xyz - IN.posWS);
                    }
                #else
                    L = normalize(float3(0.0, 1.0, 0.0));
                #endif

                // 簡易 Blinn-Phong スペキュラ
                float3 H = normalize(L + V);
                float NdotH = saturate(dot(N, H));
                float specExp = SmoothnessToExponent(_Smoothness);
                float specIntensity = pow(NdotH, specExp);

                // スペキュラ色にフレネルを乗算して角度依存にする
                float3 specular = _SpecularColor.rgb * specIntensity * fresnelSchlick;

                // クリアコートを作成
                float ccExp = SmoothnessToExponent(_ClearcoatSmoothness);
                float ccSpec = pow(NdotH, ccExp);
                float3 F0_cc = (0.8f, 0.8f, 0.8f);
                float ccF = F0_cc + (1 - F0_cc) * pow((1 - dot(N, V)), 5.0f);
                float clearcoatTerm = _Clearcoat * ccSpec * ccF;

                // 擬似サブサーフェス（リム的に加える）
                float rim = pow(saturate(1.0 - cosTheta), 3.0) * _SubsurfaceStrength;
                float3 subsurfaceAdd = _SubsurfaceColor.rgb * rim;

                // 単純合成：拡散 + スペキュラ + サブサーフェス
                float3 outColor = baseColor + specular + subsurfaceAdd + clearcoatTerm;

                // オーバーブライト防止
                outColor = saturate(outColor);

                return half4(outColor, alpha);
            }
            ENDHLSL
        }
    }
}
