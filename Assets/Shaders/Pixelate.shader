Shader "Custom/Pixelate"
{
    Properties
    {
        _BlitTexture ("Texture", 2D) = "white" {}
        _PixelsPerScreenHeight ("Pixels Per Screen Height", Float) = 240
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}

        Pass
        {
            Name "Pixelate"
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

            half4 Frag(Varyings input) : SV_Target
            {
                // Square blocks sized in device pixels, so the grid stays
                // consistent regardless of aspect ratio or output resolution.
                float2 screenSize = _ScreenParams.xy;
                float blockSize = max(screenSize.y / _PixelsPerScreenHeight, 1.0);
                float2 gridCount = screenSize / blockSize;

                float2 snappedUV = (floor(input.texcoord * gridCount) + 0.5) / gridCount;

                // Skybox pixels are never written by opaque geometry, so they
                // sit at the far plane - leave them at full resolution instead
                // of snapping to the pixel grid.
                float rawDepth = SampleSceneDepth(input.texcoord);
                float depth01 = Linear01Depth(rawDepth, _ZBufferParams);
                float2 uv = depth01 > 0.999 ? input.texcoord : snappedUV;

                return SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
            }
            ENDHLSL
        }
    }
}
