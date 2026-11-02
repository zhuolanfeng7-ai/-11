import numpy as np, wave, json
from pathlib import Path
out=Path('D:/-11/AudioPreviews/FourPaintings')
sr=44100; duration=32; rng=np.random.default_rng(20261023)
def note(buf,beat,midi,length,amp,kind='keys',pan=0,bpm=96):
 start=int(beat*60/bpm*sr); n=int(length*60/bpm*sr)
 if start>=len(buf):return
 n=min(n,len(buf)-start);t=np.arange(n)/sr;f=440*2**((midi-69)/12)
 if kind=='keys':
  x=sum(a*np.sin(2*np.pi*f*k*t)*np.exp(-t*(1.5+k*.5)) for k,a in [(1,1),(2,.34),(3,.15),(4,.07)]);env=(1-np.exp(-t*180))*np.minimum(1,(n/sr-t)/.09)
 elif kind=='bell':
  x=sum(a*np.sin(2*np.pi*f*k*t)*np.exp(-t*d) for k,a,d in [(1,1,1.8),(2.76,.35,3),(4.07,.15,5)]);env=(1-np.exp(-t*300))*np.minimum(1,(n/sr-t)/.07)
 elif kind=='strings':
  vibr=t+.0007*np.sin(2*np.pi*5*t);x=sum(np.sin(2*np.pi*f*k*vibr)/k**1.5 for k in range(1,7));env=np.minimum(1,t/.35)*np.minimum(1,(n/sr-t)/.5)*.6
 elif kind=='bass':
  x=np.sin(2*np.pi*f*t)+.25*np.sin(4*np.pi*f*t);env=(1-np.exp(-t*60))*np.exp(-t*2)*np.minimum(1,(n/sr-t)/.08)
 elif kind=='flute':
  x=np.sin(2*np.pi*f*t+.045*np.sin(2*np.pi*5*t))+.13*np.sin(4*np.pi*f*t);env=np.minimum(1,t/.12)*np.minimum(1,(n/sr-t)/.2)
 elif kind=='pluck':
  x=sum(np.sin(2*np.pi*f*k*t)*np.exp(-t*k*2)/k for k in range(1,8));env=(1-np.exp(-t*300))*np.minimum(1,(n/sr-t)/.05)
 x=x*env*amp
 buf[start:start+n,0]+=x*np.sqrt((1-pan)/2);buf[start:start+n,1]+=x*np.sqrt((1+pan)/2)
def hit(buf,beat,kind,amp,bpm):
 start=int(beat*60/bpm*sr); n=min(int(.35*sr),len(buf)-start)
 if n<=0:return
 t=np.arange(n)/sr;noise=rng.normal(0,1,n)
 if kind=='kick': x=np.sin(2*np.pi*(48*t+3*(1-np.exp(-t*35))))*np.exp(-t*22)
 elif kind=='snare':x=(noise*.65+np.sin(2*np.pi*180*t)*.25)*np.exp(-t*35)
 else:x=np.concatenate(([0],np.diff(noise)))*np.exp(-t*100)*.4
 buf[start:start+n]+=x[:,None]*amp
motif=[(0,74,1),(1.5,77,.5),(2,76,1),(3.5,69,.5),(4,72,1.5),(6,73,.75),(7,69,.7)]
def make(idx,title,bpm):
 b=np.zeros((sr*duration,2),dtype=np.float64); total=(duration-3)*bpm/60
 progressions={1:[[50,57,62,65],[46,53,58,62],[48,55,60,64],[45,52,57,61]],2:[[50,57,60,65],[55,59,62,65],[48,55,58,64],[45,52,55,61]],3:[[50,57,62,65],[46,53,57,62],[53,60,64,69],[45,52,57,61]],4:[[50,57,60,64],[55,59,62,66],[48,55,59,62],[45,52,55,61]]}
 chords=progressions[idx]
 for bar in range(int(total/4)):
  pos=bar*4; ch=chords[(bar//2)%4]
  if idx==1:
   for step in range(8):note(b,pos+step*.5,ch[step%4]+12,.32,.12,'pluck',(-1)**step*.4,bpm)
   for q in range(4):hit(b,pos+q,'snare' if q%2 else 'kick',.085,bpm)
   for q in range(8):hit(b,pos+q*.5,'hat',.04,bpm)
   note(b,pos,ch[0]-12,1.5,.19,'bass',0,bpm)
  elif idx==2:
   for q in [0,2]:note(b,pos+q,ch[0]-12,.8,.18,'bass',-.25,bpm)
   for q in [1,3]:
    for n in ch[1:]:note(b,pos+q,n,.65,.075,'keys',.3,bpm)
    hit(b,pos+q,'snare',.045,bpm)
   for q in range(8):hit(b,pos+q*.5,'hat',.022,bpm)
   if bar%2==1:
    for k,n in enumerate([88,92,95]):note(b,pos+3+k*.13,n,.6,.07,'bell',.65,bpm)
  elif idx==3:
   for n in ch:note(b,pos,n,4.5,.045,'strings',-.3,bpm)
   for k in range(8):note(b,pos+k*.5,ch[k%4]+(12 if k>3 else 0),1.3,.085,'keys',.3,bpm)
   note(b,pos,ch[0]-12,3,.13,'bass',0,bpm)
  else:
   for n in ch:note(b,pos,n,4.3,.035,'strings',-.4,bpm)
   for k in range(8):note(b,pos+k*.5,ch[k%4]+12,1.1,.065,'pluck',.4,bpm)
   note(b,pos,ch[0]-12,2,.13,'bass',-.2,bpm)
   for q in [1,3]:hit(b,pos+q,'hat',.017,bpm)
 for phrase in range(int(total/8)):
  for t,n,l in motif:
   if idx==1:kind='bell'; pitch=n+12; a=.115; l=min(l,.55)
   elif idx==2:kind='flute';pitch=n;a=.15
   elif idx==3:kind='keys';pitch=n;a=.23
   else:kind='flute';pitch=n;a=.12
   # The locked note is heard high and distant; the final suite makes it effortless.
   if idx==3 and t==6:kind='bell';pitch=n+24;a=.065
   note(b,phrase*8+t,pitch,l,a,kind,-.05,bpm)
 if idx==1:
  for t in np.arange(4,total,8):note(b,t,49,.22,.065,'bell',.65,bpm)
 # Short stereo room and two quiet echoes preserve clarity.
 dry=b.copy()
 for delay,gain in [(.071,.12),(.113,.09),(.229,.08),(.347,.05)]:
  shift=int(delay*sr);b[shift:]+=dry[:-shift,::-1]*gain
 fade=np.minimum(1,np.arange(len(b))/sr/.25)*np.minimum(1,(len(b)-1-np.arange(len(b)))/sr/2.7)
 b*=fade[:,None];b=np.tanh(b*1.25)
 peak=np.max(np.abs(b));b*=.87/max(peak,1e-9)
 pcm=(b*32767).astype('<i2');p=out/f'{idx:02}_{title}.wav'
 with wave.open(str(p),'wb') as w:w.setnchannels(2);w.setsampwidth(2);w.setframerate(sr);w.writeframes(pcm.tobytes())
 return {'file':str(p),'seconds':duration,'peak':float(np.max(np.abs(b))),'rms':float(np.sqrt(np.mean(b*b)))}
results=[make(1,'声音流水线',112),make(2,'耳朵交易所',104),make(3,'金币赎不回的旋律',82),make(4,'听觉贵宾室',88)]
(out/'audio-check.json').write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(results,ensure_ascii=False))
