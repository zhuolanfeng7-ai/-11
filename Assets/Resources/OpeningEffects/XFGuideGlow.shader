Shader "Hidden/Scene2/XFGuideGlow"
{
 Properties{_Tint("Tint",Color)=(.55,.65,1,1) _Strength("Strength",Float)=0}
 SubShader{Tags{"Queue"="Transparent+5" "RenderType"="Transparent"} Blend One One ZWrite Off ZTest LEqual Offset -1,-1
 Pass{CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata{float4 vertex:POSITION;float3 normal:NORMAL;};struct v2f{float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 view:TEXCOORD1;};
 float4 _Tint;float _Strength;
 v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.view=UnityWorldSpaceViewDir(mul(unity_ObjectToWorld,v.vertex).xyz);return o;}
 fixed4 frag(v2f i):SV_Target{float rim=pow(1-saturate(abs(dot(normalize(i.normal),normalize(i.view)))),2);return float4(_Tint.rgb*_Strength*(.12+.38*rim),1);}
 ENDCG}
 }Fallback Off
}
