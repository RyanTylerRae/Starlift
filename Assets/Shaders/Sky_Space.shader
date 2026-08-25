Shader "Skybox/Sky_Space"
{
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Skybox" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 dir        : TEXCOORD0;
            };

            // 3D value noise — uniform across the sphere, no seams or blend zones
            float _Hash3(float3 p)
            {
                p = frac(p * float3(443.8975, 397.2973, 491.1871));
                p += dot(p, p.yxz + 19.19);
                return frac((p.x + p.y) * p.z);
            }

            float _ValueNoise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(lerp(_Hash3(i + float3(0,0,0)), _Hash3(i + float3(1,0,0)), f.x),
                         lerp(_Hash3(i + float3(0,1,0)), _Hash3(i + float3(1,1,0)), f.x), f.y),
                    lerp(lerp(_Hash3(i + float3(0,0,1)), _Hash3(i + float3(1,0,1)), f.x),
                         lerp(_Hash3(i + float3(0,1,1)), _Hash3(i + float3(1,1,1)), f.x), f.y),
                    f.z);
            }

            // 3-octave FBM matching Shader Graph SimpleNoise octave weights
            float SpaceNoise(float3 p, float scale)
            {
                return _ValueNoise3D(p * scale)        * 0.125
                     + _ValueNoise3D(p * scale * 0.5)  * 0.25
                     + _ValueNoise3D(p * scale * 0.25) * 0.5;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.dir = normalize(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 dir = normalize(IN.dir);
                float3 animated_dir = dir + _Time.y * 0.003;

                float noise_coarse = SpaceNoise(dir, 1000.0);
                float noise_fine   = SpaceNoise(animated_dir, 300.0);

                float star_mask = ceil(noise_coarse - 0.75);
                float stars = star_mask * smoothstep(0.3, 1.0, noise_fine);

                return half4(stars, stars, stars, 1.0);
            }
            ENDHLSL
        }
    }
}
