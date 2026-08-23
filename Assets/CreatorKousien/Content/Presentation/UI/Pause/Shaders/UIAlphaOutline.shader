Shader "CreatorKousien/UI/Alpha Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineSize ("Outline Size", Range(0, 12)) = 4
        _OutlineEnabled ("Outline Enabled", Float) = 0

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

            fixed SampleAlpha(float2 uv)
            {
                return saturate(tex2D(_MainTex, uv).a + _TextureSampleAdd.a);
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 source = (tex2D(_MainTex, input.texcoord) + _TextureSampleAdd) * input.color;
                float2 offset = fwidth(input.texcoord) * max(0.0, _OutlineSize);
                float2 diagonal = offset * 0.70710678;

                fixed surroundingAlpha = 0;
                surroundingAlpha = max(surroundingAlpha, SampleAlpha(input.texcoord + float2(offset.x, 0)));
                surroundingAlpha = max(surroundingAlpha, SampleAlpha(input.texcoord - float2(offset.x, 0)));
                surroundingAlpha = max(surroundingAlpha, SampleAlpha(input.texcoord + float2(0, offset.y)));
                surroundingAlpha = max(surroundingAlpha, SampleAlpha(input.texcoord - float2(0, offset.y)));
                surroundingAlpha = max(surroundingAlpha, SampleAlpha(input.texcoord + float2(diagonal.x, diagonal.y)));
                surroundingAlpha = max(surroundingAlpha, SampleAlpha(input.texcoord + float2(diagonal.x, -diagonal.y)));
                surroundingAlpha = max(surroundingAlpha, SampleAlpha(input.texcoord + float2(-diagonal.x, diagonal.y)));
                surroundingAlpha = max(surroundingAlpha, SampleAlpha(input.texcoord - float2(diagonal.x, diagonal.y)));

                fixed outlineAlpha = saturate(surroundingAlpha - source.a) * saturate(_OutlineEnabled) * _OutlineColor.a * input.color.a;
                fixed4 result;
                result.rgb = lerp(_OutlineColor.rgb * input.color.rgb, source.rgb, source.a);
                result.a = max(source.a, outlineAlpha);

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
