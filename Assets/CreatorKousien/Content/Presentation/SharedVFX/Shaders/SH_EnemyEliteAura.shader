// ------------------------------------------------------------
// File		: SH_EnemyEliteAura.shader
// Summary	: 強化敵のオーラを表現するシェーダー
//
// Author	: [浅野 勇生]
// Created	: 2026-09-14
//
// Notes	:
// - 強化敵であることを示す赤いオーラつくってみるお
// ------------------------------------------------------------
Shader "Custom/Enemy/SH_EnemyEliteAura"
{
    // 強化敵であることを示す赤いオーラ
    Properties
    {
        [Header(Aura)]
        [HDR] _AuraColor ("Aura Color", Color) = (1.0, 0.15, 0.1, 1.0)
        _AuraStrength ("Aura Strength", Range(0, 5)) = 1.2
        _BodyTint ("Body Tint (全体のうすうすうす)", Range(0, 1)) = 0.12
        _Inflation ("Mesh Inflation (Thickness)", Range(0, 0.1)) = 0.012

        [Header(Rim)]
        _RimPower ("Rim Power", Range(0.5, 8.0)) = 2.5
        _RimIntensity ("Rim Intensity", Range(0, 5)) = 1.6

        [Header(Pulse)]
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 1.5
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+5"
        }
        LOD 100

        // 加算合成。元のモデルを暗くせず、光をまとっているように見せる！
        Blend One One
        ZWrite Off
        Cull Back

        Pass
        {
            Name "EliteAuraOverlay"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 viewDirWS   : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _AuraColor;
                float  _AuraStrength;
                float  _BodyTint;
                float  _Inflation;
                float  _RimPower;
                float  _RimIntensity;
                float  _PulseSpeed;
                float  _PulseAmount;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // 法線方向に少し押し出してZファイティングを防ぎ、オーラに厚みを持たせるらしい
                float3 inflatedPosOS = IN.positionOS.xyz + IN.normalOS * _Inflation;

                VertexPositionInputs posInputs = GetVertexPositionInputs(inflatedPosOS);
                OUT.positionHCS = posInputs.positionCS;
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS   = GetWorldSpaceViewDir(posInputs.positionWS);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(IN.viewDirWS);

                // 輪郭ほど強く光らせる
                float NdotV = saturate(dot(N, V));
                float fresnel = pow(1.0 - NdotV, _RimPower);

                // ゆっくりした明滅。PulseAmount=0で明滅なし
                float pulse01 = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                float pulse = lerp(1.0 - _PulseAmount, 1.0, pulse01);

                // 全体のうっすら + 輪郭の強い光
                float aura = (_BodyTint + fresnel * _RimIntensity) * pulse;
                aura = max(aura, 0.0);

                float3 finalColor = _AuraColor.rgb * _AuraStrength * aura;

                // 加算合成なのでアルファは使わない
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
