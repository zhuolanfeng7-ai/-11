Shader "Hidden/Scene2/XFIndicatorHalo"
{
 Properties{_Tint("Tint",Color)=(.58,.68,1,1) _Strength("Strength",Float)=0}
 SubShader{Tags{"Queue"="Transparent+10" "RenderType"="Transparent"} Blend One One ZWrite Off ZTest LEqual Cull Off
 Pass{CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment frag
 #include "UnityCG.cginc"
 float4 _Tint;float _Strength;
 fixed4 frag(v2f_img i):SV_Target{float r=length((i.uv-.5)*2);float halo=pow(saturate(1-r),3);return float4(_Tint.rgb*halo*_Strength*.85,1);}
 ENDCG}
 }Fallback Off
}

