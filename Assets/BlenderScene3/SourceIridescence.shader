Shader "BlenderScene3/SourceIridescence" {
Properties {_FacingLUT("Blender facing colors",2D)="white"{} _Metallic("Metallic",Range(0,1))=.18674034 _Roughness("Roughness",Range(0,1))=0}
SubShader {Tags {"RenderType"="Opaque"} Cull Off
CGPROGRAM
#pragma surface surf Standard fullforwardshadows addshadow
#pragma target 3.0
sampler2D _FacingLUT;float _Metallic,_Roughness;
struct Input {float3 viewDir;};
void surf(Input IN,inout SurfaceOutputStandard o){float facing=saturate(abs(normalize(IN.viewDir).z));float4 col=tex2D(_FacingLUT,float2(facing,.5));o.Albedo=col.rgb;o.Metallic=_Metallic;o.Smoothness=1-_Roughness;o.Emission=col.rgb*col.a;o.Alpha=1;}
ENDCG
}Fallback "Standard"}
