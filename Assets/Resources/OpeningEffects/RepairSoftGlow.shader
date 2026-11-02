Shader "Hidden/Scene2/Repair Soft Glow"{
Properties{_MainTex("Image",2D)="white"{}}
SubShader{Cull Off ZWrite Off ZTest Always
CGINCLUDE
#include "UnityCG.cginc"
sampler2D _MainTex;float4 _Step;
fixed4 blur(v2f_img i):SV_Target{fixed4 c=tex2D(_MainTex,i.uv)*.227027;for(int n=1;n<=4;n++){float w=n==1?.194595:n==2?.121622:n==3?.054054:.016216;c+=(tex2D(_MainTex,i.uv+_Step.xy*n)+tex2D(_MainTex,i.uv-_Step.xy*n))*w;}return c;}
fixed4 mask(v2f_img i):SV_Target{return fixed4(.72,.66,1,tex2D(_MainTex,i.uv).a*.8);}
ENDCG
Pass{CGPROGRAM
#pragma vertex vert_img
#pragma fragment blur
ENDCG}
Pass{CGPROGRAM
#pragma vertex vert_img
#pragma fragment mask
ENDCG}
}Fallback Off}
