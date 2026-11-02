Shader "Scene2/Presentation Restored Grille"{
Properties{_MainTex("Grid",2D)="white"{}}
SubShader{Tags{"Queue"="Transparent+30" "RenderType"="Transparent"}Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha
Pass{CGPROGRAM
#pragma vertex vert_img
#pragma fragment frag
#include "UnityCG.cginc"
sampler2D _MainTex;
fixed4 frag(v2f_img i):SV_Target{return tex2D(_MainTex,i.uv);}
ENDCG}
}Fallback Off}
