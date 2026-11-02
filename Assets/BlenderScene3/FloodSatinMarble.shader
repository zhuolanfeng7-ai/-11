Shader "BlenderScene3/FloodSatinMarble" {
 Properties {
 _Color("Base color",Color)=(1,1,1,1)
 _MainTex("Base texture",2D)="white"{}
 _MetalTex("Metallic texture",2D)="white"{}
 _RoughTex("Roughness texture",2D)="white"{}
 _BumpMap("Normal texture",2D)="bump"{}
 _EmissionTex("Emission texture",2D)="white"{}
 _Metallic("Metallic",Range(0,1))=0
 _Roughness("Roughness",Range(0,1))=0.5
 _EmissionColor("Emission",Color)=(0,0,0,1)
 _NormalStrength("Normal strength",Float)=1
 _UseMetal("Use metallic texture",Float)=0
 _UseRough("Use roughness texture",Float)=0
 _UseNormal("Use normal texture",Float)=0
 _BaseUV("Base UV scale offset",Vector)=(1,1,0,0)
 _MetalUV("Metal UV scale offset",Vector)=(1,1,0,0)
 _RoughUV("Rough UV scale offset",Vector)=(1,1,0,0)
 _NormalUV("Normal UV scale offset",Vector)=(1,1,0,0)
 _EmitUV("Emission UV scale offset",Vector)=(1,1,0,0)
 _Angles("Base metal rough normal rotation",Vector)=(0,0,0,0)
 _EmitAngle("Emission rotation",Float)=0
_UVChannels("Base metal rough normal UV channel",Vector)=(0,0,0,0)
_EmitUVChannel("Emission UV channel",Float)=0
_AlphaTex("Opacity texture",2D)="white"{}
_AlphaUV("Opacity UV",Vector)=(1,1,0,0)
_AlphaAngle("Opacity UV rotation",Float)=0
_AlphaUVChannel("Opacity UV channel",Float)=0
 }
 SubShader {
 Cull Off
 Tags {"RenderType"="Opaque"}
 LOD 300
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows
 #pragma target 3.0
 #include "UnityCG.cginc"
 sampler2D _MainTex,_MetalTex,_RoughTex,_BumpMap,_EmissionTex,_AlphaTex;
 float4 _Color,_EmissionColor,_BaseUV,_MetalUV,_RoughUV,_NormalUV,_EmitUV,_Angles,_UVChannels,_AlphaUV;
 float _Metallic,_Roughness,_NormalStrength,_UseMetal,_UseRough,_UseNormal,_EmitAngle,_EmitUVChannel,_AlphaAngle,_AlphaUVChannel;
 struct Input { float2 uv_MainTex; float2 uv2_MainTex; };
 float2 mapped(float2 uv,float4 st,float angle){float2 p=uv*st.xy;float c=cos(angle),s=sin(angle);return float2(c*p.x-s*p.y,s*p.x+c*p.y)+st.zw;}
 void surf(Input IN,inout SurfaceOutputStandard o){
 float4 c=tex2D(_MainTex,mapped(lerp(IN.uv_MainTex,IN.uv2_MainTex,_UVChannels.x),_BaseUV,_Angles.x))*_Color;
 o.Albedo=c.rgb;o.Alpha=c.a;
 o.Metallic=lerp(_Metallic,tex2D(_MetalTex,mapped(lerp(IN.uv_MainTex,IN.uv2_MainTex,_UVChannels.y),_MetalUV,_Angles.y)).r,_UseMetal);
 o.Smoothness=1-max(0.55,lerp(_Roughness,tex2D(_RoughTex,mapped(lerp(IN.uv_MainTex,IN.uv2_MainTex,_UVChannels.z),_RoughUV,_Angles.z)).r,_UseRough)*0.65+0.25);
 if(_UseNormal>0.5){float3 n=UnpackNormal(tex2D(_BumpMap,mapped(lerp(IN.uv_MainTex,IN.uv2_MainTex,_UVChannels.w),_NormalUV,_Angles.w)));n.xy*=_NormalStrength;o.Normal=normalize(n);}
 o.Emission=tex2D(_EmissionTex,mapped(lerp(IN.uv_MainTex,IN.uv2_MainTex,_EmitUVChannel),_EmitUV,_EmitAngle)).rgb*_EmissionColor.rgb;
 }
 ENDCG
 }
 Fallback "Standard"
}





