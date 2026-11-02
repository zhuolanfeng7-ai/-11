Shader "Scene3/Video2GlassScreen" {
 Properties {
  _MainTex("Video image", 2D) = "black" {}
  _Color("Tint", Color) = (1,1,1,1)
  _ScreenBrightness("Screen brightness", Range(0,2)) = 0.85
  _GlassReflectivity("Glass reflectivity", Range(0,0.3)) = 0.09
  _GlassSmoothness("Glass smoothness", Range(0,1)) = 0.90
  _RimColor("Violet edge reflection", Color) = (0.22,0.12,0.40,1)
  _RimStrength("Edge reflection strength", Range(0,0.15)) = 0.035
 }
 SubShader {
  Tags { "RenderType"="Opaque" }
  LOD 250
  Cull Off
  CGPROGRAM
  #pragma surface surf StandardSpecular fullforwardshadows
  #pragma target 3.0
  sampler2D _MainTex;
  fixed4 _Color, _RimColor;
  half _ScreenBrightness, _GlassReflectivity, _GlassSmoothness, _RimStrength;
  struct Input { float2 uv_MainTex; float3 viewDir; };
  void surf(Input IN, inout SurfaceOutputStandardSpecular o) {
   fixed4 image = tex2D(_MainTex, IN.uv_MainTex) * _Color;
   half rim = pow(1.0 - saturate(dot(normalize(IN.viewDir), float3(0,0,1))), 4.0);
   o.Albedo = image.rgb * 0.15;
   o.Specular = half3(_GlassReflectivity, _GlassReflectivity, _GlassReflectivity);
   o.Smoothness = _GlassSmoothness;
   o.Emission = image.rgb * _ScreenBrightness + _RimColor.rgb * rim * _RimStrength;
   o.Alpha = 1;
  }
  ENDCG
 }
 FallBack "Standard"
}
