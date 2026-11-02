Shader "Scene2/LCD Television"
{
 Properties {
  _MainTex("Video",2D)="black"{}
  _Color("Tint",Color)=(1,1,1,1)
  _BaseUV("UV scale and offset",Vector)=(1,1,0,0)
  _Angles("UV rotation",Vector)=(0,0,0,0)
  _UVChannels("UV channels",Vector)=(0,0,0,0)
  _Brightness("Backlight",Range(.5,2))=1.12
  _Contrast("Contrast",Range(.8,1.3))=1.08
  _PanelResolution("LCD cells",Vector)=(384,216,0,0)
  _PixelStrength("Pixel structure",Range(0,.6))=.12
  _GlassStrength("Glass reflection",Range(0,.3))=.09
  _Iridescence("Optical film rainbow reflection",Range(0,.6))=.32
  _MicroShimmer("Fine glass highlights",Range(0,.3))=.13
  _BezelWidth("Dark screen surround",Range(0,.05))=.018
 }
 SubShader {
  Tags {"RenderType"="Opaque" "Queue"="Geometry"}
  Cull Off
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #include "UnityCG.cginc"
   sampler2D _MainTex;
   float4 _MainTex_ST,_Color,_BaseUV,_Angles,_UVChannels,_PanelResolution;
   float _Brightness,_Contrast,_PixelStrength,_GlassStrength,_BezelWidth,_Iridescence,_MicroShimmer;
   float hash21(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float3 spectrum(float phase){return .5+.5*cos(6.2831853*(phase+float3(0,.333333,.666667)));}
   struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;float2 uv2:TEXCOORD1;};
   struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float3 worldPos:TEXCOORD1;float3 normal:TEXCOORD2;};
   v2f vert(appdata v) {
    v2f o;o.vertex=UnityObjectToClipPos(v.vertex);
    float2 p=TRANSFORM_TEX(lerp(v.uv,v.uv2,saturate(_UVChannels.x)),_MainTex)*_BaseUV.xy;
    float c=cos(_Angles.x),s=sin(_Angles.x);
    o.uv=float2(c*p.x-s*p.y,s*p.x+c*p.y)+_BaseUV.zw;
    o.worldPos=mul(unity_ObjectToWorld,v.vertex).xyz;o.normal=UnityObjectToWorldNormal(v.normal);return o;
   }
   fixed4 frag(v2f i):SV_Target {
    float2 screenUV=i.uv;
    float edge=min(min(screenUV.x,1-screenUV.x),min(screenUV.y,1-screenUV.y));
    float edgeAA=max(fwidth(edge),.0001);
    float panelMask=smoothstep(_BezelWidth-edgeAA,_BezelWidth+edgeAA,edge);
    // Fit the entire video inside the glass instead of cropping its edges.
    float2 uv=saturate((screenUV-_BezelWidth)/(1-2*_BezelWidth));
    float3 color=tex2D(_MainTex,uv).rgb*_Color.rgb;
    color=max(0,(color-.5)*_Contrast+.5)*_Brightness;
    // Retain the pixel structure at ordinary viewing distance; filter only
    // after the cells genuinely become smaller than a raster pixel.
    float2 cell=uv*_PanelResolution.xy;
    float2 footprint=fwidth(cell);
    float cellVisibility=1-smoothstep(.75,1.75,max(footprint.x,footprint.y));
    float3 stripes=.5+.5*cos(6.2831853*(cell.x+float3(0,.333333,.666667)));
    float row=.5+.5*cos(6.2831853*cell.y);
    color*=1-_PixelStrength*cellVisibility*(.65*(1-stripes)+.35*(1-row));
    // Subtle edge falloff and a stationary glass sheen, with stable LCD backlight.
    float2 centered=uv-.5;
    color*=1-.20*saturate(dot(centered,centered)*2);
    float3 view=normalize(_WorldSpaceCameraPos.xyz-i.worldPos);
    float facing=abs(dot(normalize(i.normal),view));
    float diagonal=uv.x*.7+uv.y-.96;
    float reflection=exp(-pow(diagonal*5,2));
    float highlight=exp(-pow((diagonal-.12)*45,2));
    color+=float3(.72,.83,1)*_GlassStrength*(reflection*.65+highlight*.35+pow(1-facing,4)*.5);
    // Localised interference colours on the optical film. The video itself
    // remains untinted; only a few reflective patches carry the rainbow.
    float3 localView=normalize(mul((float3x3)unity_WorldToObject,view));
    float viewPhase=localView.x*.9+localView.y*.7+(1-facing)*1.1;
    float2 filmUV=uv+localView.xy*.07;
    float2 patchA=(filmUV-float2(.24,.74))/float2(.22,.085);
    float2 patchB=(filmUV-float2(.77,.29))/float2(.19,.065);
    float2 patchC=(filmUV-float2(.83,.84))/float2(.12,.055);
    float patches=exp(-dot(patchA,patchA)*1.5)+.8*exp(-dot(patchB,patchB)*1.5)+.6*exp(-dot(patchC,patchC)*1.5);
    float phase=filmUV.x*2.4+filmUV.y*3.1+viewPhase;
    float3 rainbow=spectrum(phase);
    color+=rainbow*patches*_Iridescence*(.75+.6*(1-facing));
    // Sparse, sub-pixel highlights and a fine prismatic weave give the glass
    // texture without another layer of heavy television scanlines.
    float2 microUV=uv*float2(96,54);
    float2 microCell=floor(microUV);
    float seed=hash21(microCell);
    float2 local=frac(microUV)-.5;
    float antialias=max(.055,max(fwidth(microUV.x),fwidth(microUV.y))*.32);
    float glintSpot=exp(-dot(local,local)/(antialias*antialias));
    float sparkle=glintSpot*step(.84,seed)*pow(.5+.5*cos(seed*45+viewPhase*8),6);
    float fineVisibility=1-smoothstep(.9,1.8,max(fwidth(microUV.x),fwidth(microUV.y)));
    float weave=(.5+.5*cos(6.2831853*(microUV.x+microUV.y)))*fineVisibility;
    color+=spectrum(seed+viewPhase)*sparkle*_MicroShimmer;
    color+=spectrum(phase+.15)*weave*patches*_MicroShimmer*.13;
    // A narrow graphite surround and lit inner glass edge separate the display
    // from the wall without adding geometry or altering the video player.
    float innerRim=exp(-abs(edge-_BezelWidth)/max(edgeAA,.0012));
    float3 bezel=float3(.009,.012,.022)+float3(.08,.11,.18)*innerRim;
    color=lerp(bezel,color,panelMask);
    return fixed4(color,1);
   }
   ENDCG
  }
 }
 Fallback "Unlit/Texture"
}
