from pathlib import Path
import numpy as np,wave,json
out=Path('D:/-11/AudioPreviews/Opening');sr=44100;dur=10;n=sr*dur;rng=np.random.default_rng(423)
t=np.arange(n)/sr
music=np.zeros((n,2));steps=np.zeros_like(music);fault=np.zeros_like(music)
def place(buf,x,start,gain=1,pan=0):
 i=int(start*sr);count=min(len(x),n-i)
 if count<=0:return
 buf[i:i+count,0]+=x[:count]*gain*np.sqrt((1-pan)/2)
 buf[i:i+count,1]+=x[:count]*gain*np.sqrt((1+pan)/2)
def softnoise(count,width=15):
 x=rng.normal(0,1,count+width-1);return np.convolve(x,np.ones(width)/width,'valid')
# Quiet, warm electric-piano figures with rounded attacks.
for k,pitch in enumerate([62,69,65,72,64,69,62,65,61,68,64,71,62,69,65,72]):
 start=.15+k*.5;tt=np.arange(int(1.7*sr))/sr;f=440*2**((pitch-69)/12)
 x=(np.sin(2*np.pi*f*tt)+.22*np.sin(4*np.pi*f*tt))*np.exp(-tt*2.8)*(1-np.exp(-tt*75))*np.minimum(1,(1.7-tt)/.12)
 place(music,x,start,.085,(-1)**k*.25)
# Increasing loss of the musical signal, with smooth edges to avoid sharp clicks.
level=np.ones(n)
level*=np.where(t<3,1,np.clip(1-(t-3)/5,.04,1))
for start,length,depth in [(4.15,.09,.35),(4.48,.14,.6),(5.3,.18,.8),(6.1,.28,.95),(6.65,.19,.95),(7.4,.35,1),(8.1,.5,1)]:
 mask=(t>=start)&(t<start+length);u=(t[mask]-start)/length;level[mask]*=1-depth*np.sin(np.pi*u)**2
music*=level[:,None]
for k,start in enumerate(np.arange(.65,9.4,.72)):
 tt=np.arange(int(.22*sr))/sr
 x=(.55*np.sin(2*np.pi*(105*tt+1.2*(1-np.exp(-tt*30))))*np.exp(-tt*34)+softnoise(len(tt),22)*np.exp(-tt*24))*(1-np.exp(-tt*500))
 place(steps,x,float(start),.15 if start<6 else .11,(-1)**k*.12)
 if start>=3:
  length=.12 if start<6 else .19;st=np.arange(int(length*sr))/sr
  scratch=softnoise(len(st),12)*(np.sin(2*np.pi*38*st)**2)*np.sin(np.pi*st/length)**2
  place(fault,scratch,float(start)+.045,.15 if start<6 else .23,0)
# Short low-mid crackling bursts, separated by silence rather than a sustained harsh noise.
for start,length in [(6.32,.14),(7.12,.22),(7.91,.13),(8.48,.26),(9.05,.17)]:
 tt=np.arange(int(length*sr))/sr
 pulse=(.45+.55*np.sin(2*np.pi*31*tt)**2)
 x=(softnoise(len(tt),18)*.8+.12*np.sin(2*np.pi*155*tt))*pulse*np.sin(np.pi*tt/length)**2
 place(fault,x,start,.32)
# Small room reflections, kept quiet.
for buf in [music,steps,fault]:
 dry=buf.copy();shift=int(.09*sr);buf[shift:]+=dry[:-shift,::-1]*.12
mix=music+steps+fault
fade=np.minimum(1,t/.08)*np.minimum(1,(dur-t)/.45);mix*=fade[:,None]
# Preserve dynamics; only attenuate if necessary.
gain=min(1,.72/max(np.max(np.abs(mix)),1e-9));mix*=gain
for name,buf in [('Opening_Hearing_Failure_10s',mix),('Footsteps_10s',steps*fade[:,None]*gain),('Intermittent_Fault_10s',fault*fade[:,None]*gain)]:
 with wave.open(str(out/(name+'.wav')),'wb') as w:w.setnchannels(2);w.setsampwidth(2);w.setframerate(sr);w.writeframes(np.clip(buf*32767,-32767,32767).astype('<i2').tobytes())
print(json.dumps({'duration':dur,'peak':float(abs(mix).max()),'rms':float(np.sqrt(np.mean(mix**2))),'files':3}))
