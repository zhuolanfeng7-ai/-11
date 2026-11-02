import asyncio
from pathlib import Path
import sys
import wave
import numpy as np

root=Path(__file__).resolve().parent
sys.path.insert(0,str(root/'.voice_tools'))
import edge_tts
import miniaudio

TEXT=("Hey, future me. Has the money come through? "
      "They said ears don't earn interest, so I signed. "
      "Remember to buy the most expensive concert tickets. "
      "We used to listen from outside. "
      "Now we can finally afford to sit inside.")
SR=44100

def band(a, low, high):
    f=np.fft.rfftfreq(len(a),1/SR)
    response=1/(1+(low/np.maximum(f,1))**6)/(1+(f/high)**8)
    return np.fft.irfft(np.fft.rfft(a)*response,n=len(a))

async def main():
    clean=root/'Future_Self_Concert_Clean_EN.mp3'
    await edge_tts.Communicate(TEXT,'en-GB-RyanNeural',rate='-12%',pitch='-2Hz',
        connect_timeout=15,receive_timeout=30).save(str(clean))
    decoded=miniaudio.decode_file(str(clean),output_format=miniaudio.SampleFormat.SIGNED16,
                                  nchannels=1,sample_rate=SR)
    voice=np.array(decoded.samples,dtype=float)/32768
    voice=band(voice,240,3500)
    voice*=.70/max(np.max(abs(voice)),.001)
    # Signal fluctuation changes level only: never repeat, delay or cut words.
    for fraction,duration in [(.18,.075),(.42,.09),(.68,.065),(.84,.08)]:
        start=int(len(voice)*fraction); n=int(duration*SR)
        voice[start:start+n]*=1-.24*np.sin(np.linspace(0,np.pi,n))**2
    rng=np.random.default_rng(271027)
    hiss=band(rng.normal(size=len(voice)),400,6500)
    hiss/=max(np.std(hiss),.001)
    envelope=np.full(len(voice),.003)
    for fraction,duration in [(.08,.23),(.35,.18),(.59,.25),(.90,.16)]:
        start=int(len(voice)*fraction);n=min(int(duration*SR),len(voice)-start)
        envelope[start:start+n]+=.012*np.sin(np.linspace(0,np.pi,n))**2
    audio=voice+hiss*envelope
    n=int(.035*SR)
    audio[:n]*=np.linspace(0,1,n);audio[-n:]*=np.linspace(1,0,n)
    assert np.isfinite(audio).all() and np.max(abs(audio))<.95
    out=root/'Future_Self_Concert_Phone_Preview_EN.wav'
    with wave.open(str(out),'wb') as w:
        w.setnchannels(1);w.setsampwidth(2);w.setframerate(SR)
        w.writeframes(np.round(audio*32767).astype('<i2').tobytes())
    print(f'{out}\nDuration: {len(audio)/SR:.2f}s; peak {np.max(abs(audio)):.3f}')

asyncio.run(main())
