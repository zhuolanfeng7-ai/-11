Shader "Hidden/Scene2/RecoveryGlitch"
{
 Properties{_MainTex("Source",2D)="white"{}}
 SubShader{Cull Off ZWrite Off ZTest Always
 Pass{CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;float4 _MainTex_TexelSize;float _Strength,_Phase;
 fixed4 frag(v2f_img i):SV_Target{
  float band=floor(i.uv.y*32);float tick=floor(_Phase*19);
  float random=frac(sin(band*17.17+tick*47.31)*13758.5453);
  float shift=(random-.5)*.022*_Strength*step(.72,random);
  float2 uv=float2(saturate(i.uv.x+shift),i.uv.y);
  float2 offset=_MainTex_TexelSize.xy*(2.8*_Strength);
  fixed4 color=tex2D(_MainTex,uv)*.4;
  color+=tex2D(_MainTex,uv+float2(offset.x,0))*.15;
  color+=tex2D(_MainTex,uv-float2(offset.x,0))*.15;
  color+=tex2D(_MainTex,uv+float2(0,offset.y))*.15;
  color+=tex2D(_MainTex,uv-float2(0,offset.y))*.15;
  color.r=lerp(color.r,tex2D(_MainTex,saturate(uv+float2(.0025*_Strength,0))).r,.35*_Strength);
  color.b=lerp(color.b,tex2D(_MainTex,saturate(uv-float2(.0025*_Strength,0))).b,.35*_Strength);
  float scan=.5+.5*sin(i.uv.y*_ScreenParams.y*.9);
  color.rgb*=1-_Strength*(.30+.10*scan);return color;
 }
 ENDCG}
 }
 Fallback Off
}
