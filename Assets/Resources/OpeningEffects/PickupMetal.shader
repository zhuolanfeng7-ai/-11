Shader "Scene2/Pickup Brushed Metal"
{
 Properties{_MainTex("Surface",2D)="white"{} _Color("Silver",Color)=(.72,.76,.86,1) _Pulse("Pickup light",Float)=0 _TextureWeight("Original surface colour",Range(0,1))=.16 _Gold("Gold frame",Float)=0}
 SubShader{
  Tags{"RenderType"="Opaque"}
  Pass{
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #include "UnityCG.cginc"
   sampler2D _MainTex;float4 _MainTex_ST,_Color;float _Pulse,_TextureWeight,_Gold;
   struct v2f{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float3 normal:TEXCOORD1;float3 view:TEXCOORD2;};
   v2f vert(appdata_base v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.texcoord,_MainTex);o.normal=UnityObjectToWorldNormal(v.normal);o.view=_WorldSpaceCameraPos.xyz-mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
   fixed4 frag(v2f i):SV_Target{
    float3 n=normalize(i.normal),v=normalize(i.view),l=normalize(float3(-.4,.7,-1));
    float diffuse=saturate(dot(n,l));float specular=pow(saturate(dot(n,normalize(l+v))),48);
    float3 surface=lerp(float3(1,1,1),tex2D(_MainTex,i.uv).rgb,_TextureWeight)*_Color.rgb;
    if(_Gold>.5)return fixed4(surface*(.8+.3*abs(dot(n,l)))+float3(1,.94,.7)*specular*.45,1);
    float grain=sin(i.uv.y*1100+sin(i.uv.x*80))*.018;
    float grainVisibility=1-smoothstep(.7,1.7,fwidth(i.uv.y)*175);
    float3 color=surface*(.28+.72*diffuse)*(1+grain*grainVisibility);
    color+=float3(.73,.85,1)*specular*.38;
    color+=float3(.24,.16,.45)*pow(1-saturate(abs(dot(n,v))),2)*.22;
    color+=float3(.12,.1,.22)*_Pulse*.22;
    return fixed4(color/(1+color*.3),1);
   }
   ENDCG
  }
 }
 Fallback "Unlit/Texture"
}
