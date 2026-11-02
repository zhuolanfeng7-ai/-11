Shader "Scene2/ClearGlass" {
 Properties {
  _Color("Glass tint",Color)=(0.98,0.99,1,1)
  _Opacity("Opacity (0 clear, 1 opaque)",Range(0,1))=0.12
  _Smoothness("Smoothness",Range(0,1))=0.96
  _RoughnessInfluence("Surface texture strength",Range(0,1))=0.04
  _RoughnessTex("Original glass roughness",2D)="white"{}
  [Normal] _BumpMap("Original glass normal",2D)="bump"{}
  _NormalStrength("Normal strength",Range(0,1))=0.025
  _UVChannel("UV channel",Float)=0
 }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
  LOD 300
  ZWrite Off
  Cull Back
  CGPROGRAM
  #pragma surface surf Standard alpha:fade fullforwardshadows
  #pragma target 3.0
  #include "UnityCG.cginc"
  sampler2D _RoughnessTex,_BumpMap;
  float4 _Color;
  float _Opacity,_Smoothness,_RoughnessInfluence,_NormalStrength,_UVChannel;
  struct Input { float2 uv_BumpMap; float2 uv2_BumpMap; };
  void surf(Input IN,inout SurfaceOutputStandard o) {
   float2 uv=lerp(IN.uv_BumpMap,IN.uv2_BumpMap,_UVChannel);
   o.Albedo=_Color.rgb;
   o.Metallic=0;
   o.Smoothness=saturate(_Smoothness-tex2D(_RoughnessTex,uv).r*_RoughnessInfluence);
   float3 n=UnpackNormal(tex2D(_BumpMap,uv));n.xy*=_NormalStrength;o.Normal=normalize(n);
   o.Alpha=_Opacity;
  }
  ENDCG
 }
 Fallback Off
}
