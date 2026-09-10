Shader "Custom/HoverLavenderRim"
{
    Properties
    {
        _RimColor ("Lavender Rim", Color) = (0.55, 0.28, 0.9, 0.34)
        _RimPower ("Rim Width", Range(0.5, 8)) = 2.8
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
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 vertex : SV_POSITION; float3 worldNormal : TEXCOORD0; float3 worldPosition : TEXCOORD1; };
            fixed4 _RimColor;
            float _RimPower;
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
                return fixed4(_RimColor.rgb, _RimColor.a * edge);
            }
            ENDCG
        }
    }
}
