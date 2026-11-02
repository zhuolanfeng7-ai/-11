Shader "Scene2/P5 Repair Note"
{
 Properties{_MainTex("Repair drawing",2D)="white"{} _BoundsMin("Mesh minimum",Vector)=(-.01,-.01,-.01,0) _BoundsSize("Mesh size",Vector)=(.02,.02,.02,0) _FitV("Vertical fit",Float)=1.2}
 SubShader{Tags{"RenderType"="Opaque"}Cull Off
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows vertex:vert
 #pragma target 3.0
 #include "UnityCG.cginc"
 sampler2D _MainTex;float4 _BoundsMin,_BoundsSize;float _FitV;
 struct Input{float2 noteUV;float noteFace;};
 void vert(inout appdata_full v,out Input o){UNITY_INITIALIZE_OUTPUT(Input,o);o.noteUV=(v.vertex.zy-_BoundsMin.zy)/max(_BoundsSize.zy,float2(.00001,.00001));o.noteFace=abs(v.normal.x);}
 void surf(Input i,inout SurfaceOutputStandard o){float2 uv=i.noteUV;uv.x=1-uv.x;uv.y=(uv.y-.5)*_FitV+.5;fixed3 paper=1;if(i.noteFace>.5&&uv.y>=0&&uv.y<=1)paper=tex2D(_MainTex,saturate(uv)).rgb;o.Albedo=paper;o.Metallic=0;o.Smoothness=.08;o.Alpha=1;}
 ENDCG
 }Fallback "Diffuse"
}

