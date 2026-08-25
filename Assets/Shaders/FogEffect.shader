Shader "Custom/FogEffect"
{
    Properties
    {
        _BlitTexture ("Texture", 2D) = "white" {}
        _FogColor ("Fog Color", Color) = (0, 0, 0, 1)
        _FogStartDistance ("Fog Start Distance", Float) = 0
        _FogPower ("Fog Curve Power", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}

        Pass
        {
            Name "FogEffect"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            half4 _FogColor;
            float _FogStartDistance;
            float _FogPower;

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half4 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);

                float rawDepth = SampleSceneDepth(uv);

                // Skybox pixels never write depth - leave unobstructed sky alone
                if (rawDepth == UNITY_RAW_FAR_CLIP_VALUE)
                {
                    return sceneColor;
                }

                // Reconstruct this pixel's world-space position from depth
                float3 worldPos = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);

                // Distance from camera to that world position, normalized against the far clip plane
                float distanceToCamera = length(worldPos - _WorldSpaceCameraPos);
                float farClip = _ProjectionParams.z;

                float fogFactor = saturate((distanceToCamera - _FogStartDistance) / max(farClip - _FogStartDistance, 0.0001));
                fogFactor = pow(fogFactor, max(_FogPower, 0.0001));

                half3 finalColor = lerp(sceneColor.rgb, _FogColor.rgb, fogFactor);
                return half4(finalColor, sceneColor.a);
            }
            ENDHLSL
        }
    }
}
