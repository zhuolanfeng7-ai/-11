Shader "Scene2/Pickup Lavender Rim"
{
 Properties{_Tint("Glow",Color)=(.5,.3,1,1) _Strength("Strength",Float)=1}
 SubShader{
  Tags{"Queue"="Transparent+10" "RenderType"="Transparent"}
  Blend SrcAlpha One ZWrite Off Cull Back
  Pass{
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   float4 _Tint;float _Strength;
   struct v2f{float4 vertex:SV_POSITION;float3 normal:TEXCOORD0;float3 view:TEXCOORD1;};
   v2f vert(appdata_base v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);float3 p=mul(unity_ObjectToWorld,v.vertex).xyz;o.normal=UnityObjectToWorldNormal(v.normal);o.view=_WorldSpaceCameraPos.xyz-p;return o;}
   fixed4 frag(v2f i):SV_Target{float rim=pow(1-saturate(abs(dot(normalize(i.normal),normalize(i.view)))),1.8);return fixed4(_Tint.rgb*_Strength,rim*.8);}
   ENDCG
  }
 }
}
