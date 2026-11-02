Shader "BlenderScene3/SensoryLiquidityGlass" {
Properties {_FacingLUT("Source Blender Color Ramp",2D)="white"{} _Transmission("Source transmission",Range(0,1))=.7 _IOR("Source IOR",Float)=1.45}
SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent"} Cull Off
CGPROGRAM
#pragma surface surf Standard fullforwardshadows alpha:premul
#pragma target 3.0
sampler2D _FacingLUT;float _Transmission,_IOR;
struct Input {float3 viewDir;};
void surf(Input IN,inout SurfaceOutputStandard o){float d=saturate(abs(normalize(IN.viewDir).z));float3 tint=tex2D(_FacingLUT,float2(d,.5)).rgb;float f0=pow((_IOR-1)/(_IOR+1),2);float fresnel=f0+(1-f0)*pow(1-d,5);o.Albedo=tint;o.Metallic=0;o.Smoothness=1;o.Alpha=1-_Transmission+_Transmission*fresnel;o.Emission=0;}
ENDCG
}Fallback "Transparent/Diffuse"}
