Shader "Custom/HoverLavenderRim"
{
    Properties
    {
        _RimColor ("Lavender Rim", Color) = (0.55, 0.28, 0.9, 0.34)
        _RimPower ("Rim Width", Range(0.5, 8)) = 2.8
        [HideInInspector] _GlowIntensity ("Glow Intensity", Float) = 1
        [HideInInspector] _Additive ("Additive Mode", Float) = 0
        [HideInInspector] _FullSurfaceGlow ("Full Surface Glow", Float) = 0
        [HideInInspector] _SrcBlend ("Source Blend", Float) = 5
        [HideInInspector] _DstBlend ("Destination Blend", Float) = 10
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend [_SrcBlend] [_DstBlend]
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 vertex : SV_POSITION; float3 worldNormal : TEXCOORD0; float3 worldPosition : TEXCOORD1; };
            fixed4 _RimColor;
            float _RimPower;
            float _GlowIntensity;
            float _Additive;
            float _FullSurfaceGlow;
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPosition = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                float3 viewDirection = normalize(_WorldSpaceCameraPos - i.worldPosition);
                float edge = pow(1 - saturate(abs(dot(normalize(i.worldNormal), viewDirection))), _RimPower);
                float glowMask = lerp(edge, 1.0, saturate(_FullSurfaceGlow));
                float3 additiveGlow = _RimColor.rgb * glowMask * _RimColor.a * _GlowIntensity;
                float3 outputColor = lerp(_RimColor.rgb, additiveGlow, saturate(_Additive));
                return fixed4(outputColor, _RimColor.a * glowMask);
            }
            ENDCG
        }
    }
}
