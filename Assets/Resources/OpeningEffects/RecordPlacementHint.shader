Shader "Scene2/Record Placement Hint"
{
 Properties{_Tint("Tint",Color)=(.7,.55,1,.15)}
 SubShader{
 Tags{"Queue"="Transparent" "RenderType"="Transparent"}
 Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 Pass{
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 float4 _Tint;
 float4 vert(float4 vertex:POSITION):SV_POSITION{return UnityObjectToClipPos(vertex);}
 fixed4 frag():SV_Target{return _Tint;}
 ENDCG
 }
 }
}
