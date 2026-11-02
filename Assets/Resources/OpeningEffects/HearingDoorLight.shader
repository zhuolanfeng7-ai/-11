Shader "Scene2/Hearing Door White Space"
{
 Properties{_Opacity("Opacity",Float)=0 _Soft("Haze",Float)=0}
 SubShader{Tags{"Queue"="Transparent" "RenderType"="Transparent"}Cull Off ZWrite Off ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha
 Pass{CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct v2f{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;};float _Opacity,_Soft;
 v2f vert(appdata_base v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord.xy;return o;}
 fixed4 frag(v2f i):SV_Target{
 float mist=.8+.12*sin(i.uv.y*17+_Time.y*.4)*sin(i.uv.x*23-_Time.y*.3);
 float edge=saturate(1-length((i.uv-.5)*1.8));
 return float4(.96,.98,1,_Opacity*lerp(1,edge*mist,_Soft));
 }
 ENDCG}
 }Fallback Off
}
