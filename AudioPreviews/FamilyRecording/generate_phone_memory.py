import asyncio
import sys
from pathlib import Path
import wave
import numpy as np

root=Path(__file__).resolve().parent
sys.path.insert(0,str(root/'.voice_tools'))
import edge_tts
import miniaudio

message=("Hey, future me. Are we rich yet? "
         "I thought selling our hearing would buy us a better life. "
         "I hope you still remember what our laugh sounded like.")
notice="Transaction complete. Regret is not grounds for a refund."

async def main():
    await edge_tts.Communicate(message,'en-GB-RyanNeural',rate='-8%',
        connect_timeout=15,receive_timeout=30).save(str(root/'Phone_Past_Self.mp3'))
    await edge_tts.Communicate(notice,'en-GB-SoniaNeural',rate='-5%',
        connect_timeout=15,receive_timeout=30).save(str(root/'Phone_Company_Notice.mp3'))
    def read(name):
        decoded=miniaudio.decode_file(str(root/name),output_format=miniaudio.SampleFormat.SIGNED16,nchannels=1,sample_rate=44100)
        a=np.array(decoded.samples,dtype=float)/32768
        return a*.7/max(np.max(abs(a)),.01)
    a=read('Phone_Past_Self.mp3')
    f=np.fft.rfftfreq(len(a),1/44100)
    response=(1/(1+(180/np.maximum(f,1))**6))/(1+(f/4100)**8)
    a=np.fft.irfft(np.fft.rfft(a)*response,n=len(a))
    b=read('Phone_Company_Notice.mp3')
    b*=.85+.15*np.cos(2*np.pi*43*np.arange(len(b))/44100)
    gap=np.zeros(int(.65*44100))
    n=int(.11*44100); t=np.arange(n)/44100
    gap[int(.12*44100):int(.12*44100)+n]=np.sin(2*np.pi*880*t)*np.sin(np.linspace(0,np.pi,n))**2*.09
    full=np.concatenate([a,gap,b,np.zeros(int(.15*44100))])
    out=root/'Phone_Past_Self_Memory_EN.wav'
    with wave.open(str(out),'wb') as w:
        w.setnchannels(1);w.setsampwidth(2);w.setframerate(44100)
        w.writeframes(np.round(full*32767).astype('<i2').tobytes())
    print(f'{out}\nDuration: {len(full)/44100:.2f}s, peak: {np.max(abs(full)):.3f}')

asyncio.run(main())
