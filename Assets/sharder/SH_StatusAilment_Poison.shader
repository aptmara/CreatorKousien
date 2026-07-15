Shader "Custom/Enemy/SH_StatusAilment_Poison"
{
    Properties
    {
        [Header(Poison Slime)]
        [MainColor] _PoisonColor ("Poison Bright Color", Color) = (0.2, 0.9, 0.1, 1.0)
        _PoisonColorDark ("Poison Dark Color", Color) = (0.05, 0.4, 0.05, 1.0)
        _SlimeOpacity ("Slime Base Opacity", Range(0, 1)) = 0.6
        _EmissionStrength ("Emission Strength", Range(0, 5)) = 2.0
        _Inflation ("Mesh Inflation (Thickness)", Range(0, 0.1)) = 0.02

        [Header(Organic Flow)]
        _FlowSpeed ("Flow Speed (X, Y)", Vector) = (0.1, -0.3, 0, 0)
        _DistortionScale ("Distortion Scale", Range(1, 20)) = 8.0
        _DistortionStrength ("Distortion Strength", Range(0, 0.5)) = 0.1

        [Header(Toxic Bubbles)]
        _BubbleScale ("Bubble Scale", Range(2, 30)) = 15.0
        _BubbleSpeed ("Bubble Speed", Range(0, 5)) = 1.5
        _BubbleColor ("Bubble Highlight Color", Color) = (0.6, 1.0, 0.2, 1.0)

        [Header(Rim Light)]
        _RimPower ("Rim Power", Range(0.5, 8.0)) = 3.0
        _RimIntensity ("Rim Intensity", Range(0, 5)) = 2.5
        _RimPulseSpeed ("Rim Pulse Speed", Range(0, 10)) = 3.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+10"
        }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "PoisonOverlay"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
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
                float3 normalWS    : TEXCOORD0;
                float3 viewDirWS   : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;
                float2 uv          : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _PoisonColor;
                float4 _PoisonColorDark;
                float  _SlimeOpacity;
                float  _EmissionStrength;
                float  _Inflation;
                float4 _FlowSpeed;
                float  _DistortionScale;
                float  _DistortionStrength;
                float  _BubbleScale;
                float  _BubbleSpeed;
                float4 _BubbleColor;
                float  _RimPower;
                float  _RimIntensity;
                float  _RimPulseSpeed;
            CBUFFER_END

            // 2D Hash
            float2 hash22(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            // 2D Value Noise
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(dot(hash22(i + float2(0.0,0.0)), f - float2(0.0,0.0)),
                                 dot(hash22(i + float2(1.0,0.0)), f - float2(1.0,0.0)), u.x),
                            lerp(dot(hash22(i + float2(0.0,1.0)), f - float2(0.0,1.0)),
                                 dot(hash22(i + float2(1.0,1.0)), f - float2(1.0,1.0)), u.x), u.y) + 0.5;
            }

            // Fractal Brownian Motion for organic slime texture
            float fbm(float2 uv)
            {
                float f = 0.0;
                float w = 0.5;
                for (int i = 0; i < 3; i++)
                {
                    f += w * noise(uv);
                    uv *= 2.0;
                    w *= 0.5;
                }
                return f;
            }

            // Voronoi for bubbles
            float voronoi_bubbles(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float minDist = 1.0;
                for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                {
                    float2 neighbor = float2(x, y);
                    float2 pt = hash22(i + neighbor);
                    // 泡が湧き上がるような動き
                    pt = 0.5 + 0.5 * sin(_Time.y * _BubbleSpeed + 6.2831 * pt);
                    float2 diff = neighbor + pt - f;
                    float dist = length(diff);
                    minDist = min(minDist, dist);
                }
                // 泡の形（中心が白くエッジが暗い）
                return saturate(1.0 - minDist * 1.5);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                // 法線方向に頂点を少し押し出してZファイティングを防ぎ、厚みを持たせる
                float3 inflatedPosOS = IN.positionOS.xyz + IN.normalOS * _Inflation;
                
                VertexPositionInputs posInputs = GetVertexPositionInputs(inflatedPosOS);
                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS   = GetWorldSpaceViewDir(posInputs.positionWS);
                OUT.uv          = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(IN.viewDirWS);

                // --- 1. Distortion & Flow ---
                float2 flowUV = IN.uv + _FlowSpeed.xy * _Time.y;
                float distNoise = fbm(flowUV * _DistortionScale);
                float2 distortedUV = IN.uv + (distNoise - 0.5) * _DistortionStrength;
                
                // --- 2. Slime Base (FBM) ---
                float slimeNoise = fbm(distortedUV * 4.0 - _FlowSpeed.xy * _Time.y * 0.5);
                float3 slimeColor = lerp(_PoisonColorDark.rgb, _PoisonColor.rgb, slimeNoise);

                // --- 3. Toxic Bubbles (Voronoi) ---
                float bubbles = voronoi_bubbles(distortedUV * _BubbleScale);
                // 泡がはじけるような鋭いハイライト
                float bubbleHighlight = pow(bubbles, 4.0);
                
                // --- 4. Rim Light & Fresnel ---
                float NdotV = saturate(dot(N, V));
                float fresnel = pow(1.0 - NdotV, _RimPower);
                float rimPulse = sin(_Time.y * _RimPulseSpeed - distortedUV.y * 10.0) * 0.5 + 0.5;
                float rim = fresnel * _RimIntensity * (0.5 + 0.5 * rimPulse);

                // --- 5. Compositing ---
                float3 finalColor = slimeColor;
                finalColor += _BubbleColor.rgb * bubbleHighlight * 2.0; // 泡のハイライト
                finalColor += _PoisonColor.rgb * rim; // 脈動するリムライト

                finalColor *= _EmissionStrength;

                // アルファ計算（ベース不透明度 + 泡の明るさ + リムライト）
                float alpha = _SlimeOpacity + (bubbleHighlight * 0.5) + (rim * 0.5);
                alpha *= saturate(slimeNoise + 0.2); // 毒の濃淡で透け感を出す
                alpha = saturate(alpha);

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
