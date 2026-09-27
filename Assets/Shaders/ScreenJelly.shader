Shader "UI/ScreenJelly"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        // band thickness as a fraction of the screen's shorter side
        _EdgeWidth ("Edge Width", Range(0.001, 0.5)) = 0.08
        // > 1 keeps the band tighter against the edge, < 1 pushes it further in
        _Falloff ("Falloff", Range(0.1, 8)) = 2
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "ScreenJelly"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            fixed4 _Color;
            float _EdgeWidth;
            float _Falloff;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.texcoord;

                // derive the image's aspect from the uv derivatives so the band has the same
                // thickness on every edge regardless of resolution or canvas render mode
                float aspect = abs(ddy(uv.y)) / max(abs(ddx(uv.x)), 1e-6);
                float2 size = aspect >= 1.0 ? float2(aspect, 1.0) : float2(1.0, 1.0 / aspect);

                // distance outside an inset rectangle: 0 in the clear middle, _EdgeWidth at the
                // screen edge, with rounded inner corners
                float2 p = abs(uv - 0.5) * size;
                float2 q = p - (size * 0.5 - _EdgeWidth);
                float dist = length(max(q, 0.0));

                float band = pow(saturate(dist / _EdgeWidth), _Falloff);

                fixed4 color = i.color;
                color.a *= band;
                return color;
            }
            ENDCG
        }
    }
}
