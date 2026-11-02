Shader "Custom/GlassesGlowBackdrop"
{
    Properties
    {
        _GlowColor ("Glow Color", Color) = (1, 1, 1, 0.28)
        _Intensity ("Glow Intensity", Range(0, 2)) = 0.35
        _InnerRadius ("Soft Centre", Range(0, 0.5)) = 0.08
        _OuterRadius ("Fade Radius", Range(0.1, 0.75)) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent+20" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            fixed4 _GlowColor;
            float _Intensity;
            float _InnerRadius;
            float _OuterRadius;
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float distanceFromCentre = length(i.uv - 0.5);
                float halo = 1.0 - smoothstep(_InnerRadius, _OuterRadius, distanceFromCentre);
                halo = halo * halo * (3.0 - 2.0 * halo);
                return fixed4(_GlowColor.rgb * _Intensity, _GlowColor.a * halo * _Intensity);
            }
            ENDCG
        }
    }
}
