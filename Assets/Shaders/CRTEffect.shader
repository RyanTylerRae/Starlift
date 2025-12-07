Shader "Custom/CRTEffect"
{
    Properties
    {
        _BlitTexture ("Texture", 2D) = "white" {}
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.5
        _ScanlineCount ("Scanline Count", Float) = 500
        _Vignette ("Vignette", Range(0, 1)) = 0.3
        _ChromaticAberration ("Chromatic Aberration", Range(0, 0.01)) = 0.002
        _Brightness ("Brightness", Range(0.5, 1.5)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}

        Pass
        {
            Name "CRTEffect"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _ScanlineIntensity;
            float _ScanlineCount;
            float _Vignette;
            float _ChromaticAberration;
            float _Brightness;

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                // Chromatic aberration (color separation like old CRTs)
                half r = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2(_ChromaticAberration, 0)).r;
                half4 centerSample = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
                half g = centerSample.g;
                half b = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv - float2(_ChromaticAberration, 0)).b;
                half4 color = half4(r, g, b, centerSample.a);

                // Scanlines (horizontal lines)
                float scanline = sin(uv.y * _ScanlineCount * 3.14159) * 0.5 + 0.5;
                scanline = lerp(1.0, scanline, _ScanlineIntensity);
                color.rgba *= scanline;

                // Interlacing flicker effect (every other line slightly dimmer)
                float interlace = step(0.5, frac(uv.y * _ScanlineCount * 0.5));
                color.rgba *= lerp(0.95, 1.0, interlace);

                // Vignette (darkening at edges)
                float2 vignetteUV = uv * (1.0 - uv.yx);
                float vignette = vignetteUV.x * vignetteUV.y * 15.0;
                vignette = pow(vignette, _Vignette);
                color.rgb *= vignette;

                // Brightness adjustment
                color.rgb *= _Brightness;

                return color;
            }
            ENDHLSL
        }
    }
}
