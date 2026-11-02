Shader "Scene3/GPChangeOverlay" {
 Properties { _MainTex("Source",2D)="white"{} _Values("Values 1-4",Vector)=(12.8,7.4,18.6,-2.1) _Value5("Value 5",Float)=1.3 }
 SubShader { Cull Off ZWrite Off ZTest Always Pass { CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment frag
 #pragma target 3.0
 #include "UnityCG.cginc"
 sampler2D _MainTex; float4 _Values; float _Value5;
 float box(float2 p,float2 c,float2 size){float2 a=abs(p-c);return step(a.x,size.x)*step(a.y,size.y);}
 float digit(float2 p,int n){
  if(n<0)return 0;
  float a=box(p,float2(.5,.91),float2(.29,.065));float b=box(p,float2(.84,.72),float2(.065,.19));float c=box(p,float2(.84,.28),float2(.065,.19));float d=box(p,float2(.5,.09),float2(.29,.065));float e=box(p,float2(.16,.28),float2(.065,.19));float f=box(p,float2(.16,.72),float2(.065,.19));float g=box(p,float2(.5,.5),float2(.29,.065));
  return saturate(a*(n!=1&&n!=4)+b*(n!=5&&n!=6)+c*(n!=2)+d*(n!=1&&n!=4&&n!=7)+e*(n==0||n==2||n==6||n==8)+f*(n!=1&&n!=2&&n!=3&&n!=7)+g*(n!=0&&n!=1&&n!=7));
 }
 float percent(float2 p){float2 a=p-float2(.25,.77);float2 b=p-float2(.75,.23);float circles=(step(.11,length(a))*step(length(a),.22)+step(.11,length(b))*step(length(b),.22));float diag=step(abs(p.y-(.94-.88*p.x)),.085)*step(.1,p.x)*step(p.x,.9);return saturate(circles+diag);}
 fixed4 frag(v2f_img i):SV_Target {
  float2 uv=i.uv; fixed4 original=tex2D(_MainTex,uv); int row=-1;float cy=0;float val=0;
  if(abs(uv.y-.632)<.048){row=0;cy=.632;val=_Values.x;}
  else if(abs(uv.y-.511)<.048){row=1;cy=.511;val=_Values.y;}
  else if(abs(uv.y-.390)<.048){row=2;cy=.390;val=_Values.z;}
  else if(abs(uv.y-.269)<.048){row=3;cy=.269;val=_Values.w;}
  else if(abs(uv.y-.148)<.048){row=4;cy=.148;val=_Value5;}
  if(row<0||uv.x<.804||uv.x>.956)return original;
  fixed4 bg=tex2D(_MainTex,float2(.793,uv.y));
  float2 a=float2((uv.x-.814)/.026,(uv.y-(cy-.020))/.040);if(val<0)a.y=1-a.y;
  float arrow=step(0,a.x)*step(a.x,1)*step(0,a.y)*step(a.y,1)*step(abs(a.x-.5),.49*(1-a.y));
  float number=abs(val);int ten=(int)floor(number/10);int one=(int)fmod(floor(number),10);int tenth=(int)fmod(floor(number*10+.5),10);
  float2 p=float2((uv.x-.850)/.021,(uv.y-(cy-.025))/.050);int index=(int)floor(p.x);p.x=frac(p.x);float ink=0;
  if(index==0)ink=digit(p,ten==0?-1:ten);else if(index==1)ink=digit(p,one);else if(index==2)ink=box(p,float2(.5,.10),float2(.12,.10));else if(index==3)ink=digit(p,tenth);else if(index==4)ink=percent(p);
  ink*=step(0,p.y)*step(p.y,1)*step(.850,uv.x)*step(uv.x,.955);
  float3 color=val>=0?float3(.96,.25,.92):float3(.58,.88,1.0);return fixed4(lerp(bg.rgb,color,saturate(arrow+ink)),1);
 }
 ENDCG } }
 Fallback Off
}
