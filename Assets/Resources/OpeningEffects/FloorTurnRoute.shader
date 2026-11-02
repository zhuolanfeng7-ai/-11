Shader "Scene2/Floor Turn Route"
{
 Properties{_Opacity("Opacity",Float)=1 _Strength("Strength",Float)=1}
 SubShader{Tags{"Queue"="Transparent+10" "RenderType"="Transparent"}Cull Off ZWrite Off ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha
 Pass{CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct v2f{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;};float _Opacity,_Strength;
 v2f vert(appdata_base v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord.xy;return o;}
 fixed4 frag(v2f i):SV_Target{
 float edge=pow(abs(i.uv.x*2-1),10);float white=step(.5,i.uv.y);
 float3 cyan=lerp(float3(.17,.72,1),float3(.82,.97,1),edge);
 float alpha=lerp(.66,1,edge);
 return float4(lerp(cyan,float3(.92,1,1),white),lerp(alpha,1,white)*_Opacity*_Strength);
 }
 ENDCG}
 }Fallback Off
}
