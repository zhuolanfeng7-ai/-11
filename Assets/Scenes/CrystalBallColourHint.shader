Shader "Custom/CrystalBallColourHint"
{
    Properties { _MainTex ("Texture", 2D) = "white" {} _Tint ("Tint", Color) = (1,1,1,1) _HintAlpha ("Hint Alpha", Range(0,1)) = .22 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_ST; fixed4 _Tint; float _HintAlpha;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = TRANSFORM_TEX(v.uv, _MainTex); return o; }
            fixed4 frag(v2f i) : SV_Target { fixed4 c = tex2D(_MainTex, i.uv) * _Tint; c.a = _HintAlpha; return c; }
            ENDCG
        }
    }
}
