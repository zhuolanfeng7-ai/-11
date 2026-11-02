Shader "Hidden/Scene2/RouteMarker"
{
 Properties{_Color("Color",Color)=(.55,.7,1,1)}
 SubShader{Tags{"Queue"="Transparent" "RenderType"="Transparent"} Cull Off ZWrite Off ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha
 Pass{CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct v2f{float4 vertex:SV_POSITION;};float4 _Color;
 v2f vert(appdata_base v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);return o;}
 fixed4 frag(v2f i):SV_Target{return _Color;}
 ENDCG}
 }Fallback Off
}
