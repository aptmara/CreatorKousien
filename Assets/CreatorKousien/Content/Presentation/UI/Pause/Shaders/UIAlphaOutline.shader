Shader "CreatorKousien/UI/Alpha Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineSize ("Outline Size", Range(0, 12)) = 4
        _OutlineEnabled ("Outline Enabled", Float) = 0
        [HideInInspector] _SpriteUVRect ("Sprite UV Rect", Vector) = (0,0,1,1)
        [HideInInspector] _OriginalSize ("Original Size", Vector) = (1,1,0,0)
        [HideInInspector] _OutlinePadding ("Outline Padding", Vector) = (0,0,0,0)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            fixed4 _OutlineColor;
            float _OutlineSize;
            float _OutlineEnabled;
            float4 _SpriteUVRect;
            float2 _OriginalSize;
            float2 _OutlinePadding;
            float4 _ClipRect;

            v2f vert(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color;
                return output;
            }

            fixed SampleAlpha(float2 sourceUV)
            {
                float2 insideMinimum = step(0.0, sourceUV);
                float2 insideMaximum = step(sourceUV, 1.0);
                float inside = insideMinimum.x * insideMinimum.y * insideMaximum.x * insideMaximum.y;
                float2 atlasUV = lerp(_SpriteUVRect.xy, _SpriteUVRect.zw, sourceUV);
                return saturate(tex2D(_MainTex, atlasUV).a + _TextureSampleAdd.a) * inside;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                if (_OutlineEnabled < 0.5 || _OutlineSize <= 0.0)
                {
                    return 0;
                }

                float2 spriteUVSize = max(_SpriteUVRect.zw - _SpriteUVRect.xy, 0.00001);
                float2 expandedUV = (input.texcoord - _SpriteUVRect.xy) / spriteUVSize;
                float2 originalSize = max(_OriginalSize, 0.00001);
                float2 expandedSize = originalSize + _OutlinePadding * 2.0;
                float2 sourceUV = (expandedUV * expandedSize - _OutlinePadding) / originalSize;
                float2 offset = fwidth(sourceUV) * _OutlineSize;
                float2 diagonal = offset * 0.70710678;
                float2 shallow = offset * float2(0.92387953, 0.38268343);
                float2 steep = offset * float2(0.38268343, 0.92387953);
                float2 halfOffset = offset * 0.5;
                float2 halfDiagonal = diagonal * 0.5;

                fixed surroundingAlpha = 0;
                surroundingAlpha = max(surroundingAlpha, SampleAlpha(sourceUV + float2(offset.x, 0)));
                surroundingAlpha = max(surroundingAlpha, SampleAlpha(sourceUV - float2(offset.x, 0)));
                surroundingAlpha = max(surroundingAlpha, SampleAlpha(sourceUV + float2(0, offset.y)));
                surroundingAlpha = max(surroundingAlpha, SampleAlpha(sourceUV - float2(0, offset.y)));
                surroundingAlpha = max(surroundingAlpha, SampleAlpha(sourceUV + float2(diagonal.x, diagonal.y)));
                surroundingAlpha = max(surroundingAlpha, SampleAlpha(sourceUV + float2(diagonal.x, -diagonal.y)));
                surroundingAlpha = max(surroundingAlpha, SampleAlpha(sourceUV + float2(-diagonal.x, diagonal.y)));
                surroundingAlpha = max(surroundingAlpha, SampleAlpha(sourceUV - float2(diagonal.x, diagonal.y)));
                surroundingAlpha = max(surroundingAlpha, SampleAlpha(sourceUV + float2(shallow.x, shallow.y)));
                surroundingAlpha = max(surroundingAlpha, SampleAlpha(sourceUV + float2(shallow.x, -shallow.y)));
                surroundingAlpha = max(surroundingAlpha, SampleAlpha(sourceUV + float2(-shallow.x, shallow.y)));
                surroundingAlpha = max(surroundingAlpha, SampleAlpha(sourceUV - float2(shallow.x, shallow.y)));
                surroundingAlpha = max(surroundingAlpha, SampleAlpha(sourceUV + float2(steep.x, steep.y)));
                surroundingAlpha = max(surroundingAlpha, SampleAlpha(sourceUV + float2(steep.x, -steep.y)));
                surroundingAlpha = max(surroundingAlpha, SampleAlpha(sourceUV + float2(-steep.x, steep.y)));
                surroundingAlpha = max(surroundingAlpha, SampleAlpha(sourceUV - float2(steep.x, steep.y)));
                surroundingAlpha = max(surroundingAlpha, SampleAlpha(sourceUV + float2(halfOffset.x, 0)));
                surroundingAlpha = max(surroundingAlpha, SampleAlpha(sourceUV - float2(halfOffset.x, 0)));
                surroundingAlpha = max(surroundingAlpha, SampleAlpha(sourceUV + float2(0, halfOffset.y)));
                surroundingAlpha = max(surroundingAlpha, SampleAlpha(sourceUV - float2(0, halfOffset.y)));
                surroundingAlpha = max(surroundingAlpha, SampleAlpha(sourceUV + float2(halfDiagonal.x, halfDiagonal.y)));
                surroundingAlpha = max(surroundingAlpha, SampleAlpha(sourceUV + float2(halfDiagonal.x, -halfDiagonal.y)));
                surroundingAlpha = max(surroundingAlpha, SampleAlpha(sourceUV + float2(-halfDiagonal.x, halfDiagonal.y)));
                surroundingAlpha = max(surroundingAlpha, SampleAlpha(sourceUV - float2(halfDiagonal.x, halfDiagonal.y)));

                fixed sourceAlpha = SampleAlpha(sourceUV);
                fixed outlineAlpha = saturate(surroundingAlpha - sourceAlpha) * _OutlineColor.a * input.color.a;
                fixed4 result = fixed4(_OutlineColor.rgb * input.color.rgb, outlineAlpha);

                #ifdef UNITY_UI_CLIP_RECT
                result.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a - 0.001);
                #endif

                return result;
            }
            ENDCG
        }
    }
}
