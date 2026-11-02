from pathlib import Path
import wave
import numpy as np

ROOT = Path(__file__).resolve().parent
SR = 44100
rng = np.random.default_rng(27027)

def load(name):
    with wave.open(str(ROOT / name), 'rb') as w:
        rate, channels, width = w.getframerate(), w.getnchannels(), w.getsampwidth()
        assert width == 2
        a = np.frombuffer(w.readframes(w.getnframes()), dtype='<i2').astype(float) / 32768
        a = a.reshape(-1, channels).mean(axis=1)
    a = np.interp(np.arange(round(len(a)*SR/rate))*rate/SR, np.arange(len(a)), a)
    active = np.flatnonzero(np.abs(a) > .003)
    if len(active):
        a = a[max(0, active[0]-int(.06*SR)):min(len(a), active[-1]+int(.15*SR))]
    return a / max(np.max(np.abs(a)), .01) * .75

def band(a, low, high):
    f = np.fft.rfftfreq(len(a), 1/SR)
    h = (1 / (1+(low/np.maximum(f, 1))**6)) / (1+(f/high)**8)
    return np.fft.irfft(np.fft.rfft(a)*h, n=len(a))

def fade(a, seconds=.018):
    n = min(int(seconds*SR), len(a)//2)
    ramp = np.linspace(0,1,n)
    if a.ndim == 2: ramp = ramp[:,None]
    a[:n] *= ramp
    a[-n:] *= ramp[::-1]
    return a

mother = load('mother_v2_clean.wav')
mother = band(mother, 210, 3700)
mother = np.tanh(mother*1.5)*.64
t = np.arange(len(mother))/SR
mother *= 1 + .025*np.sin(2*np.pi*1.7*t)
# Brief partial signal losses retain the intelligibility of the message.
for fraction, duration in [(.17,.075),(.36,.10),(.58,.065),(.78,.085),(.93,.07)]:
    start = int(len(mother)*fraction)
    n = int(duration*SR)
    mother[start:start+n] *= 1-.38*np.sin(np.linspace(0,np.pi,n))**2
noise = band(rng.normal(0,1,len(mother)), 380, 4800)
noise /= max(np.std(noise), .01)
rustle = np.zeros(len(mother))
for fraction, duration in [(.10,.25),(.32,.32),(.55,.20),(.74,.28),(.96,.20)]:
    start = int(len(mother)*fraction)
    n = min(int(duration*SR),len(mother)-start)
    rustle[start:start+n] += np.sin(np.linspace(0,np.pi,n))**2*.025
mother += noise*(.004+rustle*.65)
fade(mother)
mother_stereo = np.stack([mother, mother], axis=1)

# A short disconnect crackle and two quiet line tones separate the voices.
gap = np.zeros(int(1.8*SR))
n = int(.32*SR)
gap[:n] = band(rng.normal(0,1,n),600,5500)*np.exp(-np.arange(n)/(SR*.075))*.085
for start in [.40,.70]:
    k = int(start*SR); n = int(.085*SR); tt = np.arange(n)/SR
    gap[k:k+n] += fade((np.sin(2*np.pi*480*tt)+np.sin(2*np.pi*620*tt))*.018,.008)

system = load('system_clean.wav')
tt = np.arange(len(system))/SR
# A restrained metallic texture and short, even echoes suggest an automated service.
system = band(system,130,7000)*( .87 + .13*np.cos(2*np.pi*43*tt))
system = np.pad(system,(0,int(.65*SR)))
left, right = system.copy(), system.copy()
for delay, gain in [(.105,.09),(.21,.045),(.34,.025)]:
    d = int(delay*SR)
    left[d:] += system[:-d]*gain
    d = int((delay+.012)*SR)
    right[d:] += system[:-d]*gain
system_stereo = np.stack([left,right],axis=1)
system_stereo *= .70/max(np.max(np.abs(system_stereo)),.01)
fade(system_stereo,.025)
result = np.concatenate([np.zeros((int(.22*SR),2)),mother_stereo,
                         np.stack([gap,gap],axis=1),system_stereo,
                         np.zeros((int(.20*SR),2))])
peak = np.max(np.abs(result))
if peak > .92: result *= .92/peak
out = ROOT/'Family_Recording_Preview_EN_V2.wav'
with wave.open(str(out),'wb') as w:
    w.setnchannels(2); w.setsampwidth(2); w.setframerate(SR)
    w.writeframes(np.round(result*32767).astype('<i2').tobytes())
print(f'{out}\nDuration: {len(result)/SR:.2f}s; peak: {np.max(np.abs(result)):.3f}; stereo 44.1 kHz PCM')
