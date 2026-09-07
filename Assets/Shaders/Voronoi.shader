Shader "Custom/Voronoi"
{
    Properties
    {
        _BlitTexture ("Texture", 2D) = "white" {}
        _PixelsPerScreenHeight ("Cells Per Screen Height", Float) = 240
        _Jitter ("Jitter", Range(0, 1)) = 0.8
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}

        Pass
        {
            Name "Voronoi"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float _PixelsPerScreenHeight;
            float _Jitter;

            float2 CellHash(float2 cell)
            {
                float2 p = float2(dot(cell, float2(127.1, 311.7)), dot(cell, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453123);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Same square-cell grid as the pixelate pass, but instead of
                // snapping to the block center we find the nearest jittered
                // seed point (Worley/Voronoi) and sample there, so every
                // fragment inside a cell shares one flat-shaded color and
                // the cell boundaries are organic instead of blocky.
                float2 screenSize = _ScreenParams.xy;
                float cellSize = max(screenSize.y / _PixelsPerScreenHeight, 1.0);
                float2 gridCount = screenSize / cellSize;

                float2 cellUV = input.texcoord * gridCount;
                float2 baseCell = floor(cellUV);

                float2 nearestSeedCell = baseCell;
                float minDistSq = 1e10;

                [unroll]
                for (int y = -1; y <= 1; y++)
                {
                    [unroll]
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 neighbor = baseCell + float2(x, y);
                        float2 jitter = (CellHash(neighbor) - 0.5) * _Jitter;
                        float2 seedCell = neighbor + 0.5 + jitter;

                        float2 diff = cellUV - seedCell;
                        float distSq = dot(diff, diff);

                        if (distSq < minDistSq)
                        {
                            minDistSq = distSq;
                            nearestSeedCell = seedCell;
                        }
                    }
                }

                float2 voronoiUV = saturate(nearestSeedCell / gridCount);

                // Skybox pixels are never written by opaque geometry, so they
                // sit at the far plane - leave them at full resolution instead
                // of snapping to a Voronoi cell.
                float rawDepth = SampleSceneDepth(input.texcoord);
                float depth01 = Linear01Depth(rawDepth, _ZBufferParams);
                float2 uv = depth01 > 0.999 ? input.texcoord : voronoiUV;

                return SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
            }
            ENDHLSL
        }
    }
}
