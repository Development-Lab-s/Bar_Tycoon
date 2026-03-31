Shader "Custom/IsoWallTopBand"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _TopBandColor ("Top Band Color", Color) = (0.62,0.45,0.34,1)
        _TopBandThickness ("Top Band Thickness (Pixels)", Range(1,8)) = 2
        _AlphaCutoff ("Alpha Cutoff", Range(0,1)) = 0.1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _BaseColor;
            float4 _TopBandColor;
            float _TopBandThickness;
            float _AlphaCutoff;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                return OUT;
            }

            half SampleAlpha(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float4 col = tex * _BaseColor * IN.color;

                float currentAlpha = tex.a;

                float2 texel = _MainTex_TexelSize.xy;

                // 현재 픽셀보다 "위쪽"의 알파를 샘플링
                float upperAlpha = SampleAlpha(IN.uv + float2(0, texel.y * _TopBandThickness));

                // 현재는 불투명, 위쪽은 투명이면 상단 경계
                float topMask =
                    step(_AlphaCutoff, currentAlpha) *
                    (1.0 - step(_AlphaCutoff, upperAlpha));

                col.rgb = lerp(col.rgb, _TopBandColor.rgb, topMask * _TopBandColor.a);

                return col;
            }
            ENDHLSL
        }
    }
}