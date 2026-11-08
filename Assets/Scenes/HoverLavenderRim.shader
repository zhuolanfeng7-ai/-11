Shader "Custom/HoverLavenderRim"
{
    Properties
    {
        _RimColor ("Lavender Rim", Color) = (0.55, 0.28, 0.9, 0.34)
        [HideInInspector] _AccentColor ("Accent Color", Color) = (0.15, 1, 0.42, 0.9)
        [HideInInspector] _AccentStrength ("Accent Strength", Float) = 0
        _RimPower ("Rim Width", Range(0.5, 8)) = 2.8
<<<<<<< Updated upstream
=======
        [HideInInspector] _GlowIntensity ("Glow Intensity", Float) = 1
        [HideInInspector] _Additive ("Additive Mode", Float) = 0
        [HideInInspector] _FullSurfaceGlow ("Full Surface Glow", Float) = 0
        [HideInInspector] _SrcBlend ("Source Blend", Float) = 5
        [HideInInspector] _DstBlend ("Destination Blend", Float) = 10
        [HideInInspector] _Cull ("Cull", Float) = 0
        [HideInInspector] _BackGlow ("Back Glow", Float) = 0
        [HideInInspector] _OutlineWidth ("Outline Width", Float) = 0
>>>>>>> Stashed changes
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull [_Cull]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 vertex : SV_POSITION; float3 worldNormal : TEXCOORD0; float3 worldPosition : TEXCOORD1; };
            fixed4 _RimColor;
            fixed4 _AccentColor;
            float _RimPower;
<<<<<<< Updated upstream
=======
            float _AccentStrength;
            float _GlowIntensity;
            float _Additive;
            float _FullSurfaceGlow;
            float _BackGlow;
            float _OutlineWidth;
>>>>>>> Stashed changes
            v2f vert (appdata v)
            {
                v2f o;
                float3 expanded = v.vertex.xyz + v.normal * (_OutlineWidth * saturate(_BackGlow));
                o.vertex = UnityObjectToClipPos(float4(expanded, 1));
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPosition = mul(unity_ObjectToWorld, float4(expanded, 1)).xyz;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                if (_BackGlow > .5)
                    return fixed4(_RimColor.rgb * _RimColor.a * _GlowIntensity, _RimColor.a);
                float3 viewDirection = normalize(_WorldSpaceCameraPos - i.worldPosition);
<<<<<<< Updated upstream
                float edge = pow(1 - saturate(abs(dot(normalize(i.worldNormal), viewDirection))), _RimPower);
                return fixed4(_RimColor.rgb, _RimColor.a * edge);
=======
                float3 worldNormal = normalize(i.worldNormal);
                float edge = pow(1 - saturate(abs(dot(worldNormal, viewDirection))), _RimPower);
                float glowMask = lerp(edge, 1.0, saturate(_FullSurfaceGlow));
                float accentMask = pow(saturate(abs(worldNormal.y)), 4) * saturate(_AccentStrength);
                float3 rimColor = lerp(_RimColor.rgb, _AccentColor.rgb, accentMask);
                float3 additiveGlow = rimColor * glowMask * _RimColor.a * _GlowIntensity;
                float3 outputColor = lerp(rimColor, additiveGlow, saturate(_Additive));
                return fixed4(outputColor, _RimColor.a * glowMask);
>>>>>>> Stashed changes
            }
            ENDCG
        }
    }
}
