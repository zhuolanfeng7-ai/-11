Shader "Hidden/Scene2/PlatePreview" {
 Properties{_Color("Tint",Color)=(.35,1,.75,.32)}
 SubShader{Tags{"Queue"="Transparent" "RenderType"="Transparent"}Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 Pass{CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct v2f{float4 pos:SV_POSITION;};float4 _Color;v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);return o;}fixed4 frag(v2f i):SV_Target{return _Color;}
 ENDCG}}
}
