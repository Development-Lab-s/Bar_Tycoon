Shader "Custom/IsoWallTopBevel"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)

        _TopFaceColor ("Top Face Color", Color) = (0.95,0.90,0.86,0.65)
        _TopRimColor ("Top Rim Color", Color) = (1.0,0.98,0.95,0.85)
        _BottomShadowColor ("Bottom Shadow Color", Color) = (0.55,0.43,0.38,0.55)

        _BevelHeight ("Bevel Height (Pixels)", Range(1,24)) = 8
        _TopRimWidth ("Top Rim Width (Pixels)", Range(1,6)) = 2
        _BottomShadowWidth ("Bottom Shadow Width (Pixels)", Range(1,8)) = 2

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

            float4 _TopFaceColor;
            float4 _TopRimColor;
            float4 _BottomShadowColor;

            float _BevelHeight;
            float _TopRimWidth;
            float _BottomShadowWidth;
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

            float FindDistanceFromTopEdge(float2 uv, float2 texel, int bevelHeight)
            {
                // 0 = 가장 윗줄, 1 = 그 아래 한 줄, ...
                // bevelHeight 범위 안에서 위쪽 투명 구간을 찾지 못하면 -1 반환
                [unroll(24)]
                for (int i = 1; i <= 24; i++)
                {
                    if (i > bevelHeight)
                        break;

                    float a = SampleAlpha(uv + float2(0, texel.y * i));

                    if (a <= _AlphaCutoff)
                    {
                        return (float)(i - 1);
                    }
                }

                return -1.0;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float4 col = tex * _BaseColor * IN.color;

                if (tex.a <= _AlphaCutoff)
                    return col;

                float2 texel = _MainTex_TexelSize.xy;
                int bevelHeight = (int)round(_BevelHeight);

                float distFromTop = FindDistanceFromTopEdge(IN.uv, texel, bevelHeight);

                // 베벨 구간 바깥이면 원본 유지
                if (distFromTop < 0.0)
                    return col;

                float heightDenom = max(_BevelHeight - 1.0, 1.0);
                float depth01 = saturate(distFromTop / heightDenom);

                // 전체 윗면 톤: 위로 갈수록 더 강하게
                float topFaceWeight = _TopFaceColor.a * (1.0 - depth01 * 0.35);

                // 맨 윗줄 하이라이트
                float topRimMask = 1.0 - smoothstep(0.0, _TopRimWidth, distFromTop);

                // 베벨 맨 아래쪽 얇은 그림자
                float shadowStart = max(_BevelHeight - _BottomShadowWidth, 0.0);
                float bottomShadowMask = smoothstep(shadowStart - 1.0, shadowStart + 0.5, distFromTop);

                float3 result = col.rgb;

                // 윗면 밝은 톤
                result = lerp(result, _TopFaceColor.rgb, topFaceWeight);

                // 맨 윗선 강조
                result = lerp(result, _TopRimColor.rgb, topRimMask * _TopRimColor.a);

                // 베벨 아래쪽 접히는 그림자
                result = lerp(result, _BottomShadowColor.rgb, bottomShadowMask * _BottomShadowColor.a);

                col.rgb = result;
                return col;
            }
            ENDHLSL
        }
    }
}