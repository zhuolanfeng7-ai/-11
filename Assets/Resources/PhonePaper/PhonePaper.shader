Shader "Scene2/Phone Paper Receipt"
{
 Properties {
  _MainTex("Receipt",2D)="white"{}
  _BoundsMin("Bounds minimum",Vector)=(0,0,0,0)
  _BoundsSize("Bounds size",Vector)=(1,1,1,0)
  _UAxis("Horizontal axis",Vector)=(1,0,0,0)
  _VAxis("Vertical axis",Vector)=(0,1,0,0)
  _Fit("Image fit",Vector)=(1,1,0,0)
 }
 SubShader {
  Tags {"RenderType"="Opaque"} Cull Off
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows vertex:vert
  #pragma target 3.0
  #include "UnityCG.cginc"
  sampler2D _MainTex;
  float4 _BoundsMin,_BoundsSize,_UAxis,_VAxis,_Fit;
  struct Input {float2 receiptUV;};
  void vert(inout appdata_full v,out Input o) {
   UNITY_INITIALIZE_OUTPUT(Input,o);
   float3 p=(v.vertex.xyz-_BoundsMin.xyz)/max(_BoundsSize.xyz,float3(.00001,.00001,.00001))-.5;
   o.receiptUV=float2(dot(p,_UAxis.xyz),dot(p,_VAxis.xyz))+.5;
  }
  void surf(Input i,inout SurfaceOutputStandard o) {
   float2 uv=(i.receiptUV-.5)*_Fit.xy+.5;
   fixed3 paper=fixed3(1,.98,.93);
   if(all(uv>=0)&&all(uv<=1))paper=tex2D(_MainTex,uv).rgb;
   o.Albedo=paper;o.Smoothness=.04;o.Metallic=0;o.Alpha=1;
  }
  ENDCG
 }
 Fallback "Diffuse"
}
