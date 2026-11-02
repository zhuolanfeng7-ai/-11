Shader "Scene2/Silver Four Point Glint"
{
 Properties{_Tint("Silver white",Color)=(.96,.98,1,1) _Strength("Twinkle",Float)=1}
 SubShader{
  Tags{"Queue"="Transparent+20" "RenderType"="Transparent" "IgnoreProjector"="True"}
  Blend One One ZWrite Off ZTest LEqual Cull Off
  Pass{
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   float4 _Tint;float _Strength;
   struct v2f{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;};
   v2f vert(appdata_img v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord;return o;}
   fixed4 frag(v2f i):SV_Target{
    float2 p=(i.uv-.5)*2;
    float core=exp(-dot(p,p)*95)*1.2;
    float vertical=pow(saturate(1-abs(p.y)),1.65)*exp(-abs(p.x)*52);
    float horizontal=pow(saturate(1-abs(p.x)),1.85)*exp(-abs(p.y)*52)*.85;
    float halo=exp(-dot(p,p)*11)*.055;
    float shape=core+vertical+horizontal+halo;
    return float4(_Tint.rgb*shape*_Strength,0);
   }
   ENDCG
  }
 }
 Fallback Off
}
