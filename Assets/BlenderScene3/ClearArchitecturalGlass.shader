Shader "BlenderScene3/ClearArchitecturalGlass" {
Properties {_Color("Glass tint",Color)=(.92,.97,1,.05) _Smoothness("Reflection smoothness",Range(0,1))=.70 _Frost("Frost blur in pixels",Range(0,10))=2.5}
SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"} GrabPass {"_Scene3GlassBackground"}
Pass {Cull Off ZWrite Off ZTest LEqual
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma target 3.0
#include "UnityCG.cginc"
#include "UnityStandardUtils.cginc"
sampler2D _Scene3GlassBackground;float4 _Scene3GlassBackground_TexelSize;float4 _Color;float _Smoothness,_Frost;
struct app {float4 vertex:POSITION;float3 normal:NORMAL;};
struct vary {float4 pos:SV_POSITION;float4 grab:TEXCOORD0;float3 world:TEXCOORD1;float3 normal:TEXCOORD2;};
vary vert(app v){vary o;o.pos=UnityObjectToClipPos(v.vertex);o.grab=ComputeGrabScreenPos(o.pos);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.normal=UnityObjectToWorldNormal(v.normal);return o;}
float4 frag(vary i):SV_Target{float2 uv=i.grab.xy/i.grab.w;float2 d=_Scene3GlassBackground_TexelSize.xy*_Frost;float3 bg=tex2D(_Scene3GlassBackground,uv).rgb*.25;
bg+=(tex2D(_Scene3GlassBackground,uv+float2(d.x,0)).rgb+tex2D(_Scene3GlassBackground,uv-float2(d.x,0)).rgb+tex2D(_Scene3GlassBackground,uv+float2(0,d.y)).rgb+tex2D(_Scene3GlassBackground,uv-float2(0,d.y)).rgb)*.125;
bg+=(tex2D(_Scene3GlassBackground,uv+d).rgb+tex2D(_Scene3GlassBackground,uv-d).rgb+tex2D(_Scene3GlassBackground,uv+float2(d.x,-d.y)).rgb+tex2D(_Scene3GlassBackground,uv+float2(-d.x,d.y)).rgb)*.0625;
float3 V=normalize(_WorldSpaceCameraPos-i.world),N=normalize(i.normal);float face=abs(dot(N,V));float fresnel=.04+.96*pow(1-face,5);float3 R=reflect(-V,N);half4 probe=UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0,R,(1-_Smoothness)*6);float3 reflection=DecodeHDR(probe,unity_SpecCube0_HDR);float3 transmission=lerp(bg,bg*_Color.rgb,saturate(_Color.a));return float4(lerp(transmission,reflection,fresnel),1);}
ENDCG
}}Fallback "Transparent/VertexLit"}
