Shader "Custom/FisheyeEffect"
{
    Properties
    {
        _BlitTexture ("Texture", 2D) = "white" {}
        _Strength ("Distortion Strength", Float) = 0.3
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}

        Pass
        {
            Name "FisheyeEffect"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Strength;

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                // Center coordinates
                float2 center = float2(0.5, 0.5);
                float2 offset = uv - center;

                // Calculate distance from center
                float dist = length(offset);

                // Apply barrel distortion
                float distortion = 1.0 + _Strength * (dist * dist);
                float2 distortedUV = center + offset * distortion;

                // Sample texture with distorted UVs (using Blit.hlsl's texture)
                half4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, distortedUV);

                return color;
            }
            ENDHLSL
        }
    }
}
