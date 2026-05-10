Shader "Custom/IsoWallTopCapSeamFill"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)

        [Header(Occupancy)]
        _PivotUV ("Pivot UV (sprite-local 0..1)", Vector) = (0,0,0,0)
        [Enum(R_Outer,0, G_Inner,1, B,2, A,3)]
        _TargetChannel ("Target Channel", Float) = 0

        [Header(Render Bounds)]
        _RenderBoundsExpandPx ("Render Bounds Expand Px (L,T,R,B)", Vector) = (4,8,4,0)
        _PixelsPerUnit ("Pixels Per Unit", Float) = 512

        _CapColor ("Cap Color", Color) = (0.79,0.63,0.54,1)
        _CapTopHighlightColor ("Cap Top Highlight", Color) = (0.92,0.82,0.75,0.85)
        _CapBottomShadowColor ("Cap Bottom Shadow", Color) = (0.48,0.36,0.31,0.55)
        _FaceCreaseColor ("Face Crease Color", Color) = (0.43,0.31,0.27,0.45)

        _CapHeight ("Cap Height (Pixels)", Range(1,32)) = 10
        _CapTopHighlightWidth ("Cap Top Highlight Width", Range(1,8)) = 2
        _CapBottomShadowWidth ("Cap Bottom Shadow Width", Range(1,8)) = 2
        _FaceCreaseWidth ("Face Crease Width", Range(1,8)) = 2

        _SeamFillWidth ("Seam Fill Width (Pixels)", Range(0,8)) = 4

        _VerticalDirection ("Vertical Direction (+1 or -1)", Float) = 1
        _TopEdgeBias ("Top Edge Bias", Range(0,2)) = 0.65
        _TopEdgeSharpness ("Top Edge Sharpness", Range(1,8)) = 4
        _AlphaCutoff ("Alpha Cutoff", Range(0,1)) = 0.1

        [Header(Outline)]
        _OutlineColor ("Outline Color", Color) = (0.36,0.25,0.21,1)
        _OutlineWidthPx ("Outline Width Px", Range(0,16)) = 2
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
            float4 _PivotUV;
            float4 _RenderBoundsExpandPx;

            float4 _CapColor;
            float4 _CapTopHighlightColor;
            float4 _CapBottomShadowColor;
            float4 _FaceCreaseColor;
            float4 _OutlineColor;

            float _TargetChannel;
            float _PixelsPerUnit;
            float _CapHeight;
            float _CapTopHighlightWidth;
            float _CapBottomShadowWidth;
            float _FaceCreaseWidth;
            float _SeamFillWidth;

            float _VerticalDirection;
            float _TopEdgeBias;
            float _TopEdgeSharpness;
            float _AlphaCutoff;
            float _OutlineWidthPx;
            CBUFFER_END

            TEXTURE2D(_WallOccupancyMap);
            SAMPLER(sampler_WallOccupancyMap);

            float4 _WallOccupancyOrigin;
            float4 _WallOccupancyBasisX;
            float4 _WallOccupancyBasisY;
            float4 _WallOccupancyMapSize;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 sourceUV    : TEXCOORD0;
                float2 baseUV      : TEXCOORD1;
                float3 positionWSOriginal : TEXCOORD2;
                float4 color       : COLOR;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.baseUV = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.positionWSOriginal = TransformObjectToWorld(IN.positionOS.xyz);

                float2 texSizePx = _MainTex_TexelSize.zw;
                float leftUv   = _RenderBoundsExpandPx.x / max(texSizePx.x, 1.0);
                float topUv    = _RenderBoundsExpandPx.y / max(texSizePx.y, 1.0);
                float rightUv  = _RenderBoundsExpandPx.z / max(texSizePx.x, 1.0);
                float bottomUv = _RenderBoundsExpandPx.w / max(texSizePx.y, 1.0);

                OUT.sourceUV.x = lerp(-leftUv, 1.0 + rightUv, IN.uv.x);
                OUT.sourceUV.y = lerp(-bottomUv, 1.0 + topUv, IN.uv.y);

                float2 expandOffsetPx;
                expandOffsetPx.x = lerp(-_RenderBoundsExpandPx.x, _RenderBoundsExpandPx.z, IN.uv.x);
                expandOffsetPx.y = lerp(-_RenderBoundsExpandPx.w, _RenderBoundsExpandPx.y, IN.uv.y);

                float4 positionOS = IN.positionOS;
                positionOS.xy += expandOffsetPx / max(_PixelsPerUnit, 1.0);

                OUT.positionHCS = TransformObjectToHClip(positionOS.xyz);
                OUT.color = IN.color;
                return OUT;
            }

            float InsideRect01(float2 uv)
            {
                return step(0.0, uv.x) * step(uv.x, 1.0) * step(0.0, uv.y) * step(uv.y, 1.0);
            }

            half SampleAlpha(float2 uv)
            {
                float inside = InsideRect01(uv);
                float2 sampleUv = clamp(uv, 0.0, 1.0);
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleUv).a * inside;
            }

            float4 SampleBase(float2 uv)
            {
                float inside = InsideRect01(uv);
                float2 sampleUv = clamp(uv, 0.0, 1.0);
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleUv) * inside;
            }

            float2 WorldToPixelCell(float2 worldXY)
            {
                float2 rel = worldXY - _WallOccupancyOrigin.xy;
                float2 bX  = _WallOccupancyBasisX.xy;
                float2 bY  = _WallOccupancyBasisY.xy;
                float det = bX.x * bY.y - bY.x * bX.y;
                float invDet = 1.0 / max(abs(det), 1e-6);
                invDet *= (det < 0.0) ? -1.0 : 1.0;

                float cx = (bY.y * rel.x - bY.x * rel.y) * invDet;
                float cy = (-bX.y * rel.x + bX.x * rel.y) * invDet;
                return float2(cx, cy);
            }

            float4 SampleOccupancyCell(float2 pixelCell)
            {
                if (pixelCell.x < 0.0 || pixelCell.y < 0.0 ||
                    pixelCell.x >= _WallOccupancyMapSize.x || pixelCell.y >= _WallOccupancyMapSize.y)
                {
                    return 0.0;
                }

                float2 uv = (pixelCell + 0.5) * _WallOccupancyMapSize.zw;
                return SAMPLE_TEXTURE2D_LOD(_WallOccupancyMap, sampler_WallOccupancyMap, uv, 0);
            }

            float ChannelOf(float4 occ, float channel)
            {
                if (channel < 0.5) return occ.r;
                if (channel < 1.5) return occ.g;
                if (channel < 2.5) return occ.b;
                return occ.a;
            }

            float SampleChannel(float2 pixelCell, float channel)
            {
                return ChannelOf(SampleOccupancyCell(pixelCell), channel);
            }

            float2 ComputePivotWS(float2 positionWSxy, float2 uv)
            {
                float dux = ddx(uv.x);
                float duy = ddy(uv.y);
                if (abs(dux) < 1e-6) dux = (dux >= 0.0) ? 1e-6 : -1e-6;
                if (abs(duy) < 1e-6) duy = (duy >= 0.0) ? 1e-6 : -1e-6;

                float worldPerUVx = ddx(positionWSxy.x) / dux;
                float worldPerUVy = ddy(positionWSxy.y) / duy;

                return float2(
                    positionWSxy.x + (_PivotUV.x - uv.x) * worldPerUVx,
                    positionWSxy.y + (_PivotUV.y - uv.y) * worldPerUVy
                );
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

            void TryCapCandidate(
                float2 candidateUV,
                float2 upStep,
                float2 rightStep,
                int capHeight,
                float horizontalWeight,
                inout float bestScore,
                inout float bestDist)
            {
                float dist = FindOpaqueBelowDistance(candidateUV, upStep, capHeight);
                if (dist < 0.0)
                    return;

                float2 edgeUV = candidateUV - upStep * (dist + 1.0);
                float edgeMask = ComputeTopEdgeMask(edgeUV, upStep, rightStep);

                float score = edgeMask * horizontalWeight;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestDist = dist;
                }
            }

            float GetDilatedTopEdgeMask(float2 edgeUV, float2 upStep, float2 rightStep)
            {
                float center = ComputeTopEdgeMask(edgeUV, upStep, rightStep);
                float left1  = ComputeTopEdgeMask(edgeUV - rightStep, upStep, rightStep) * 0.75;
                float right1 = ComputeTopEdgeMask(edgeUV + rightStep, upStep, rightStep) * 0.75;
                float left2  = ComputeTopEdgeMask(edgeUV - rightStep * 2.0, upStep, rightStep) * 0.5;
                float right2 = ComputeTopEdgeMask(edgeUV + rightStep * 2.0, upStep, rightStep) * 0.5;

                return max(center, max(max(left1, right1), max(left2, right2)));
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 tex = SampleBase(IN.sourceUV);
                float4 baseCol = tex * _BaseColor * IN.color;

                float2 texel = _MainTex_TexelSize.xy;
                float2 upStep = float2(0, texel.y * _VerticalDirection);
                float2 rightStep = float2(texel.x, 0);

                float2 pivotWS = ComputePivotWS(IN.positionWSOriginal.xy, IN.baseUV);
                float2 cellFloat = WorldToPixelCell(pivotWS);
                float2 cellInt = floor(cellFloat);
                float occHere = SampleChannel(cellInt, _TargetChannel);
                float occLeft = SampleChannel(cellInt + float2(-1.0, 0.0), _TargetChannel);
                float occRight = SampleChannel(cellInt + float2(1.0, 0.0), _TargetChannel);
                float occUp = SampleChannel(cellInt + float2(0.0, 1.0), _TargetChannel);
                float occDown = SampleChannel(cellInt + float2(0.0, -1.0), _TargetChannel);

                int capHeight = (int)round(_CapHeight);
                int seamFillWidth = max(
                    (int)round(_SeamFillWidth),
                    (int)ceil(max(_RenderBoundsExpandPx.x, _RenderBoundsExpandPx.z))
                );

                // --------------------------------
                // 1) 벽 내부: 접히는 선
                // --------------------------------
                if (tex.a > _AlphaCutoff)
                {
                    float distInside = FindTransparentAboveDistance(IN.sourceUV, upStep, capHeight);

                    if (distInside >= 0.0)
                    {
                        float2 topEdgeUV = IN.sourceUV + upStep * distInside;
                        float topEdgeMask = GetDilatedTopEdgeMask(topEdgeUV, upStep, rightStep);

                        float faceCreaseMask =
                            (1.0 - smoothstep(0.0, _FaceCreaseWidth, distInside)) *
                            topEdgeMask;

                        baseCol.rgb = lerp(baseCol.rgb, _FaceCreaseColor.rgb, faceCreaseMask * _FaceCreaseColor.a);
                    }

                    return baseCol;
                }

                // --------------------------------
                // 2) 투명 영역: 윗면 생성
                // 현재 위치 + 좌우 근처까지 탐색
                // --------------------------------
                float bestScore = 0.0;
                float bestDist = -1.0;

                if (occHere > 0.001)
                {
                    TryCapCandidate(IN.sourceUV, upStep, rightStep, capHeight, 1.0, bestScore, bestDist);

                    [loop]
                    for (int i = 1; i <= 8; i++)
                    {
                        if (i > seamFillWidth)
                            break;

                        float weight = 1.0 - ((float)i / max(_SeamFillWidth + 1.0, 1.0));

                        TryCapCandidate(IN.sourceUV - rightStep * i, upStep, rightStep, capHeight, weight, bestScore, bestDist);
                        TryCapCandidate(IN.sourceUV + rightStep * i, upStep, rightStep, capHeight, weight, bestScore, bestDist);
                    }
                }

                float leftContinuation = max(occLeft, occUp);
                float rightContinuation = max(occRight, occDown);
                float leftTerminal = occHere * (1.0 - saturate(leftContinuation));
                float rightTerminal = occHere * (1.0 - saturate(rightContinuation));

                float leftBand = 0.0;
                float rightBand = 0.0;

                if (_OutlineWidthPx > 0.0)
                {
                    float leftUvWidth = _OutlineWidthPx * _MainTex_TexelSize.x;
                    float rightUvWidth = _OutlineWidthPx * _MainTex_TexelSize.x;
                    leftBand = leftTerminal * (1.0 - smoothstep(0.0, leftUvWidth, -IN.sourceUV.x));
                    rightBand = rightTerminal * (1.0 - smoothstep(0.0, rightUvWidth, IN.sourceUV.x - 1.0));
                }

                float outlineMask = max(leftBand, rightBand) * step(0.0, IN.sourceUV.y) * step(IN.sourceUV.y, 1.0);

                if (bestDist < 0.0 || bestScore <= 0.001)
                {
                    if (outlineMask <= 0.001)
                        return float4(0, 0, 0, 0);

                    return float4(_OutlineColor.rgb, _OutlineColor.a * IN.color.a * outlineMask);
                }

                float depth01 = saturate(bestDist / max(_CapHeight - 1.0, 1.0));

                float3 capRgb = _CapColor.rgb;

                float bottomShadowMask = 1.0 - smoothstep(0.0, _CapBottomShadowWidth, bestDist);
                capRgb = lerp(capRgb, _CapBottomShadowColor.rgb, bottomShadowMask * _CapBottomShadowColor.a);

                float topStart = max(_CapHeight - _CapTopHighlightWidth - 1.0, 0.0);
                float topHighlightMask = smoothstep(topStart, _CapHeight - 1.0, bestDist);
                capRgb = lerp(capRgb, _CapTopHighlightColor.rgb, topHighlightMask * _CapTopHighlightColor.a);

                capRgb = lerp(capRgb, _CapTopHighlightColor.rgb, depth01 * 0.18);

                float4 capCol = float4(capRgb, _CapColor.a * IN.color.a * bestScore);

                if (outlineMask > 0.001)
                {
                    float outlineAlpha = _OutlineColor.a * IN.color.a * outlineMask;
                    capCol.rgb = lerp(capCol.rgb, _OutlineColor.rgb, outlineAlpha);
                    capCol.a = max(capCol.a, outlineAlpha);
                }

                return capCol;
            }
            ENDHLSL
        }
    }
}
