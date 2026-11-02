from pathlib import Path
import sys
import wave

root = Path(__file__).resolve().parent
sys.path.insert(0,str(root/'.voice_tools'))
import miniaudio

mother = miniaudio.decode_file(str(root/'Mother_Message_Gentle_EN.mp3'),
    output_format=miniaudio.SampleFormat.SIGNED16, nchannels=2, sample_rate=44100)
with wave.open(str(root/'Company_AI_Disconnect_2s_EN.wav'),'rb') as source:
    assert (source.getnchannels(),source.getsampwidth(),source.getframerate()) == (2,2,44100)
    ai = source.readframes(source.getnframes())
audio = mother.samples.tobytes()+ai
output = root/'Mother_Then_Company_AI_EN.wav'
with wave.open(str(output),'wb') as w:
    w.setnchannels(2)
    w.setsampwidth(2)
    w.setframerate(44100)
    w.writeframes(audio)
with wave.open(str(output),'rb') as w:
    assert w.getnframes()*4==len(audio)
    print(f'{output}\nTotal: {w.getnframes()/44100:.2f}s; mother: {len(mother.samples)/2/44100:.2f}s; then original disconnect + AI.')
