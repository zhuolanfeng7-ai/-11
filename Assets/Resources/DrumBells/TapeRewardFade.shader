Shader "JIQI/TapeRewardFade" {
 Properties { _MainTex("Texture",2D)="white"{} _Color("Color",Color)=(1,1,1,1) _Metallic("Metallic",Range(0,1))=0 _Glossiness("Smoothness",Range(0,1))=.4 _RevealAlpha("Reveal Alpha",Range(0,1))=0 }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent"} LOD 200
 CGPROGRAM
 #pragma surface surf Standard alpha:fade
 #pragma target 3.0
 sampler2D _MainTex;fixed4 _Color;half _Metallic,_Glossiness,_RevealAlpha;
 struct Input {float2 uv_MainTex;};
 void surf(Input IN,inout SurfaceOutputStandard o){fixed4 c=tex2D(_MainTex,IN.uv_MainTex)*_Color;o.Albedo=c.rgb;o.Metallic=_Metallic;o.Smoothness=_Glossiness;o.Alpha=c.a*_RevealAlpha;}
 ENDCG
 }Fallback "Transparent/Diffuse"
}
