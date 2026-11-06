Shader "Custom/CeilingMemoryProjection"
{
    Properties
    {
        _MainTex ("Memory Image", 2D) = "white" {}
        _Tint ("Pale Warm Tint", Color) = (1, 0.96, 0.78, 1)
        _Opacity ("Projection Opacity", Range(0,1)) = 0
        _Lighten ("Paper Lighten", Range(0,1)) = 0.28
        _Feather ("Edge Feather", Range(0.001,0.3)) = 0.18
        _CornerRadius ("Corner Radius", Range(0.01,0.45)) = 0.22
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
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
            sampler2D _MainTex;
            float4 _Tint;
            float _Opacity, _Lighten, _Feather, _CornerRadius;
            v2f vert (appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }
            fixed4 frag (v2f i) : SV_Target
            {
                float2 p = abs(i.uv - .5) - (.5 - _CornerRadius);
                float distanceToRoundedEdge = length(max(p, 0)) + min(max(p.x, p.y), 0) - _CornerRadius;
                float roundedMask = 1 - smoothstep(-_Feather, 0, distanceToRoundedEdge);
                float vignette = 1 - smoothstep(.34, .72, length((i.uv - .5) * float2(1.15, 1)));
                fixed4 image = tex2D(_MainTex, i.uv);
                float3 paleImage = lerp(image.rgb, 1, _Lighten) * _Tint.rgb;
                // Fade coverage instead of darkening the paper into a visible border.
                float edgeCoverage = smoothstep(0, 1, roundedMask) * lerp(.75, 1, vignette);
                return fixed4(paleImage, image.a * edgeCoverage * _Opacity);
            }
            ENDCG
        }
    }
}

