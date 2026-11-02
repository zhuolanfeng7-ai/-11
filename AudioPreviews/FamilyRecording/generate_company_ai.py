from pathlib import Path
import wave
import numpy as np

root = Path(__file__).resolve().parent
sr = 44100
with wave.open(str(root/'Company_AI_Clean.wav'), 'rb') as w:
    rate, channels = w.getframerate(), w.getnchannels()
    voice = np.frombuffer(w.readframes(w.getnframes()), dtype='<i2').astype(float)/32768
    voice = voice.reshape(-1,channels).mean(axis=1)
active = np.flatnonzero(abs(voice)>.002)
voice = voice[max(0,active[0]-int(.025*rate)):min(len(voice),active[-1]+int(.12*rate))]
# Lower the register slightly; retain a single intelligible spoken performance.
voice = np.interp(np.arange(round(len(voice)*sr/rate/.94))*rate*.94/sr,
                  np.arange(len(voice)),voice)
t = np.arange(len(voice))/sr
voice /= max(np.max(abs(voice)),.01)
f = np.fft.rfftfreq(len(voice),1/sr)
response = (1/(1+(115/np.maximum(f,1))**6))/(1+(f/6500)**8)
voice = np.fft.irfft(np.fft.rfft(voice)*response,n=len(voice))
# Amplitude modulation and a small bright harmonic component create an AI timbre.
voice = .82*voice*(.73+.27*np.cos(2*np.pi*47*t)) + .045*np.tanh(voice*4)
voice *= .76/max(np.max(abs(voice)),.01)
edge = min(int(.012*sr),len(voice)//2)
voice[:edge] *= np.linspace(0,1,edge)
voice[-edge:] *= np.linspace(1,0,edge)

intro = np.zeros(2*sr)
# Four descending electronic notes, followed by a short disconnected silence.
for start,duration,hz,level in [(.06,.18,1046.5,.19),(.38,.18,880,.18),
                                (.66,.20,659.25,.18),(1.02,.43,261.63,.22)]:
    n = int(duration*sr); tt=np.arange(n)/sr
    attack=np.minimum(tt/.009,1)
    envelope=attack*np.exp(-tt/(duration*.38))
    tone=(np.sin(2*np.pi*hz*tt)+.25*np.sin(2*np.pi*hz*2*tt))*envelope*level
    tone[-min(n,441):] *= np.linspace(1,0,min(n,441))
    k=int(start*sr); intro[k:k+n] += tone
signal = np.concatenate([intro,voice,np.zeros(int(.20*sr))])
stereo=np.column_stack([signal,signal])
out=root/'Company_AI_Disconnect_2s_EN.wav'
with wave.open(str(out),'wb') as w:
    w.setnchannels(2); w.setsampwidth(2); w.setframerate(sr)
    w.writeframes(np.round(stereo*32767).astype('<i2').tobytes())
assert np.isfinite(stereo).all() and np.max(abs(stereo))<1
print(f'{out}\nDuration {len(signal)/sr:.2f}s; AI speech starts at 2.00s; peak {np.max(abs(stereo)):.3f}')
