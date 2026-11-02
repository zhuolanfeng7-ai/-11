Shader "BlenderScene3/ColorView" {
Properties {_MainTex("Source",2D)="white"{}}
SubShader {Cull Off ZWrite Off ZTest Always Pass {
CGPROGRAM
#pragma vertex vert_img
#pragma fragment frag
#include "UnityCG.cginc"
sampler2D _MainTex;
float4 frag(v2f_img i):SV_Target {
float3 c=tex2D(_MainTex,i.uv).rgb*1.41421356;
float3x3 inset=float3x3(0.84247906,0.0784336,0.07922375,0.04232824,0.87846864,0.07916613,0.04237565,0.0784336,0.87914297);
c=mul(inset,max(c,0.000001));c=saturate((log2(c)+12.47393)/16.5);
float3 x2=c*c,x4=x2*x2;
c=15.5*x4*x2-40.14*x4*c+31.96*x4-6.868*x2*c+0.4298*x2+0.1191*c-0.00232;
float3x3 outset=float3x3(1.19687901,-0.09802088,-0.09902974,-0.05289685,1.15190313,-0.09896118,-0.05297164,-0.09804345,1.15107367);
c=mul(outset,c);return float4(pow(max(c,0),2.2),1);
}
ENDCG
}}}


