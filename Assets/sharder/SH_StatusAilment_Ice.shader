Shader "Custom/Enemy/SH_StatusAilment_Ice"
{
    Properties
    {
        [Header(Ice Base)]
        [MainColor] _IceColor ("Ice Bright Color", Color) = (0.7, 0.95, 1.0, 1.0)
        _IceColorDeep ("Ice Deep Color", Color) = (0.1, 0.4, 0.8, 1.0)
        _IceOpacity ("Ice Base Opacity", Range(0, 1)) = 0.5
        _EmissionStrength ("Emission Strength", Range(0, 5)) = 2.0
        _Inflation ("Mesh Inflation (Thickness)", Range(0, 0.1)) = 0.02

        [Header(Crystalline Structure)]
        _CrystalScale ("Crystal Scale", Range(1, 20)) = 10.0
        _ParallaxDepth ("Parallax Depth", Range(0, 0.2)) = 0.05
        _CrackSharpness ("Crack Sharpness", Range(0.1, 10)) = 5.0

        [Header(Frost Glitter)]
        _GlitterScale ("Glitter Scale", Range(10, 100)) = 60.0
        _GlitterSpeed ("Glitter Speed", Range(0, 5)) = 0.5
        _GlitterThreshold ("Glitter Threshold", Range(0, 1)) = 0.85
        _GlitterColor ("Glitter Color", Color) = (1.0, 1.0, 1.0, 1.0)

        [Header(Rim Light)]
        _RimPower ("Rim Power", Range(0.5, 8.0)) = 4.0
        _RimIntensity ("Rim Intensity", Range(0, 5)) = 3.0
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
            Name "IceOverlay"

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
                float4 _IceColor;
                float4 _IceColorDeep;
                float  _IceOpacity;
                float  _EmissionStrength;
                float  _Inflation;
                float  _CrystalScale;
                float  _ParallaxDepth;
                float  _CrackSharpness;
                float  _GlitterScale;
                float  _GlitterSpeed;
                float  _GlitterThreshold;
                float4 _GlitterColor;
                float  _RimPower;
                float  _RimIntensity;
            CBUFFER_END

            // 2D Hash
            float2 hash22(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            float hash12(float2 p)
            {
                float3 p3  = frac(float3(p.xyx) * .1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            // Worley/Cellular Noise (Returns F1 and F2 for crack generation)
            float2 worley(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float minDist1 = 1.0;
                float minDist2 = 1.0;

                for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                {
                    float2 neighbor = float2(x, y);
                    float2 pt = hash22(i + neighbor);
                    // ゆっくり動く結晶
                    pt = 0.5 + 0.5 * sin(_Time.y * 0.1 + 6.2831 * pt);
                    float2 diff = neighbor + pt - f;
                    float dist = length(diff);

                    if (dist < minDist1)
                    {
                        minDist2 = minDist1;
                        minDist1 = dist;
                    }
                    else if (dist < minDist2)
                    {
                        minDist2 = dist;
                    }
                }
                return float2(minDist1, minDist2);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                // 法線方向に少し膨らませてZファイティングを防止
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
                float NdotV = saturate(dot(N, V));

                // --- 1. Parallax/Depth UV Calculation ---
                // 視差効果で氷の厚みをシミュレート
                float2 viewOffset = V.xy * _ParallaxDepth;
                float2 uvDeep = IN.uv + viewOffset;

                // --- 2. Crystalline Cracks (Worley Noise) ---
                // 表面の結晶
                float2 wSurface = worley(IN.uv * _CrystalScale);
                float crackSurface = saturate((wSurface.y - wSurface.x) * _CrackSharpness);
                // 内部の結晶（視差UVを使用）
                float2 wDeep = worley(uvDeep * _CrystalScale * 1.5);
                float crackDeep = saturate((wDeep.y - wDeep.x) * _CrackSharpness);

                // --- 3. Base Color Blending ---
                // 内部（Deep）から表面（Surface）へのグラデーション
                float3 iceColDeep = lerp(_IceColorDeep.rgb, _IceColor.rgb, crackDeep * 0.5);
                float3 iceColSurface = lerp(_IceColorDeep.rgb, _IceColor.rgb, crackSurface);
                // 視線に対してエッジほど内部が見えにくく表面が白く反射する
                float3 finalColor = lerp(iceColDeep, iceColSurface, NdotV);

                // 氷のひび割れを白く強調
                float cracks = saturate((1.0 - crackSurface) + (1.0 - crackDeep) * 0.5);
                finalColor += _IceColor.rgb * cracks * 0.8;

                // --- 4. Frost Glitter / Sparkles ---
                // 視線と時間に依存する高周波数ノイズ
                float2 glitterUV = IN.uv * _GlitterScale;
                float glitterNoise = hash12(floor(glitterUV) + floor(_Time.y * _GlitterSpeed * 10.0));
                // 視線角度によってきらめきが明滅する
                float viewSparkle = sin(dot(V.xy, float2(12.9898, 78.233)) * 10.0 + _Time.y * _GlitterSpeed);
                float glitter = saturate(glitterNoise * viewSparkle);
                glitter = smoothstep(_GlitterThreshold, 1.0, glitter);
                finalColor += _GlitterColor.rgb * glitter * 2.0;

                // --- 5. Rim Light & Fresnel ---
                float fresnel = pow(1.0 - NdotV, _RimPower);
                float rim = fresnel * _RimIntensity;
                finalColor += _IceColor.rgb * rim;

                finalColor *= _EmissionStrength;

                // --- 6. Alpha Calculation ---
                // 氷は中央が透けてエッジが濃い、かつヒビ割れ部分は不透明度が高い
                float alpha = _IceOpacity + fresnel * 0.5 + cracks * 0.3 + glitter * 0.5;
                alpha = saturate(alpha);

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
