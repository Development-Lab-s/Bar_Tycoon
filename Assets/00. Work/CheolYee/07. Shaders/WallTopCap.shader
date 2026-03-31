Shader "Custom/IsoWallTopCapConnected"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)

        _CapColor ("Cap Color", Color) = (0.79,0.63,0.54,1)
        _CapTopHighlightColor ("Cap Top Highlight", Color) = (0.92,0.82,0.75,0.85)
        _CapBottomShadowColor ("Cap Bottom Shadow", Color) = (0.48,0.36,0.31,0.55)
        _FaceCreaseColor ("Face Crease Color", Color) = (0.43,0.31,0.27,0.45)

        _CapHeight ("Cap Height (Pixels)", Range(1,32)) = 10
        _CapTopHighlightWidth ("Cap Top Highlight Width", Range(1,8)) = 2
        _CapBottomShadowWidth ("Cap Bottom Shadow Width", Range(1,8)) = 2
        _FaceCreaseWidth ("Face Crease Width", Range(1,8)) = 2

        _VerticalDirection ("Vertical Direction (+1 or -1)", Float) = 1
        _TopEdgeBias ("Top Edge Bias", Range(0,2)) = 0.65
        _TopEdgeSharpness ("Top Edge Sharpness", Range(1,8)) = 4
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

            float4 _CapColor;
            float4 _CapTopHighlightColor;
            float4 _CapBottomShadowColor;
            float4 _FaceCreaseColor;

            float _CapHeight;
            float _CapTopHighlightWidth;
            float _CapBottomShadowWidth;
            float _FaceCreaseWidth;
            float _VerticalDirection;
            float _TopEdgeBias;
            float _TopEdgeSharpness;
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

            float FindTransparentAboveDistance(float2 uv, float2 upStep, int maxSteps)
            {
                [loop]
                for (int i = 1; i <= 32; i++)
                {
                    if (i > maxSteps)
                        break;

                    float a = SampleAlpha(uv + upStep * i);
                    if (a <= _AlphaCutoff)
                        return (float)(i - 1);
                }

                return -1.0;
            }

            float FindOpaqueBelowDistance(float2 uv, float2 upStep, int maxSteps)
            {
                [loop]
                for (int i = 1; i <= 32; i++)
                {
                    if (i > maxSteps)
                        break;

                    float a = SampleAlpha(uv - upStep * i);
                    if (a > _AlphaCutoff)
                        return (float)(i - 1);
                }

                return -1.0;
            }

            float ComputeTopEdgeMask(float2 edgeUV, float2 upStep, float2 rightStep)
            {
                float aL = SampleAlpha(edgeUV - rightStep);
                float aR = SampleAlpha(edgeUV + rightStep);
                float aU = SampleAlpha(edgeUV + upStep);
                float aD = SampleAlpha(edgeUV - upStep);

                float gx = abs(aR - aL);
                float gy = abs(aU - aD);

                float score = gy - gx * _TopEdgeBias;
                return saturate(score * _TopEdgeSharpness);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float4 baseCol = tex * _BaseColor * IN.color;

                float2 texel = _MainTex_TexelSize.xy;
                float2 upStep = float2(0, texel.y * _VerticalDirection);
                float2 rightStep = float2(texel.x, 0);

                int capHeight = (int)round(_CapHeight);

                // 1) 벽 내부: 접히는 선
                if (tex.a > _AlphaCutoff)
                {
                    float distInside = FindTransparentAboveDistance(IN.uv, upStep, capHeight);

                    if (distInside >= 0.0)
                    {
                        float2 topEdgeUV = IN.uv + upStep * distInside;
                        float topEdgeMask = ComputeTopEdgeMask(topEdgeUV, upStep, rightStep);

                        float faceCreaseMask =
                            (1.0 - smoothstep(0.0, _FaceCreaseWidth, distInside)) *
                            topEdgeMask;

                        baseCol.rgb = lerp(baseCol.rgb, _FaceCreaseColor.rgb, faceCreaseMask * _FaceCreaseColor.a);
                    }

                    return baseCol;
                }

                // 2) 투명 영역: 윗면 생성
                float distAboveTop = FindOpaqueBelowDistance(IN.uv, upStep, capHeight);

                if (distAboveTop < 0.0)
                    return float4(0, 0, 0, 0);

                float2 topEdgeUV = IN.uv - upStep * (distAboveTop + 1.0);
                float topEdgeMask = ComputeTopEdgeMask(topEdgeUV, upStep, rightStep);

                // 옆 단면이면 cap 생성 금지
                if (topEdgeMask <= 0.001)
                    return float4(0, 0, 0, 0);

                float heightDenom = max(_CapHeight - 1.0, 1.0);
                float depth01 = saturate(distAboveTop / heightDenom);

                float3 capRgb = _CapColor.rgb;

                float bottomShadowMask = 1.0 - smoothstep(0.0, _CapBottomShadowWidth, distAboveTop);
                capRgb = lerp(capRgb, _CapBottomShadowColor.rgb, bottomShadowMask * _CapBottomShadowColor.a);

                float topStart = max(_CapHeight - _CapTopHighlightWidth - 1.0, 0.0);
                float topHighlightMask = smoothstep(topStart, _CapHeight - 1.0, distAboveTop);
                capRgb = lerp(capRgb, _CapTopHighlightColor.rgb, topHighlightMask * _CapTopHighlightColor.a);

                capRgb = lerp(capRgb, _CapTopHighlightColor.rgb, depth01 * 0.18);

                return float4(capRgb, _CapColor.a * IN.color.a * topEdgeMask);
            }
            ENDHLSL
        }
    }
}