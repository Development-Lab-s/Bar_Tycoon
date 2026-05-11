Shader "Custom/2D/TilemapSeamFix"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _AlphaCutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
        // 알파가 이 값보다 낮으면 픽셀을 버림 (초록 가장자리 제거)

        [Toggle] _UsePointSampling ("Force Point Sampling", Float) = 1
        // Bilinear가 알파를 섞지 못하게 강제 Point 샘플링

        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType"     = "Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma shader_feature_local _USEPOINTSAMPLING_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;  // (1/w, 1/h, w, h)
                float4 _Color;
                float4 _RendererColor;
                float  _AlphaCutoff;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color      = IN.color * _Color * _RendererColor;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

            #ifdef _USEPOINTSAMPLING_ON
                // 강제 Point 샘플링: UV를 텍셀 중심으로 스냅
                // Bilinear 샘플러여도 텍셀 중심에서는 정확히 한 픽셀만 가져옴
                float2 texSize = _MainTex_TexelSize.zw; // (width, height)
                uv = (floor(uv * texSize) + 0.5) / texSize;
            #endif

                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                // 알파 컷오프: 반투명 가장자리 제거 (초록 픽셀이 알파 0.5 미만이면 사라짐)
                clip(col.a - _AlphaCutoff);

                col      *= IN.color;
                col.rgb  *= col.a; // Premultiplied alpha
                return col;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}