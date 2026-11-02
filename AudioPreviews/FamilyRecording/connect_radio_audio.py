from pathlib import Path
import uuid
import shutil

root=Path('D:/-11')
changes={
'ClickablePanelFall.cs': [('musicPlayer.Play();','Scene2AudioPlayback.Play(musicPlayer);'),('impactPlayer.PlayOneShot(impactClip,1f);','Scene2AudioPlayback.PlayOneShot(impactPlayer,impactClip,1f);')],
'BellMicroSwing.cs': [('source.PlayOneShot(sound,.36f*Mathf.Pow(.59f,chime));','Scene2AudioPlayback.PlayOneShot(source,sound,.36f*Mathf.Pow(.59f,chime));')],
'DrumSetSupplement.cs': [('source.PlayOneShot(strike,.65f);','Scene2AudioPlayback.PlayOneShot(source,strike,.65f);')],
'DrumBellSequence.cs': [('source.PlayOneShot(strike,.55f);','Scene2AudioPlayback.PlayOneShot(source,strike,.55f);')],
'RadioQuarterTurn.cs': [('turnAudio.PlayOneShot(turnSound);','Scene2AudioPlayback.PlayOneShot(turnAudio,turnSound);')],
'MusicBoxCrank.cs': [('audioOutput.Play();','Scene2AudioPlayback.Play(audioOutput);')],
'Scene2MicrophoneDialogue.cs': [('voice.Play();','Scene2AudioPlayback.Play(voice);')],
'Scene2OpeningAudio.cs': [('Output.Play();','Scene2AudioPlayback.Play(Output);')],
'Scene2PlateInstallation.cs': [('AudioSource.PlayClipAtPoint(sound,slot.position,.2f);','Scene2AudioPlayback.PlayAtPoint(sound,slot.position,.2f);')],
}
for name,pairs in changes.items():
    path=root/'Assets/Scenes'/name
    text=path.read_text(encoding='utf-8-sig')
    for old,new in pairs:
        assert old in text, (name,old)
        text=text.replace(old,new)
    path.write_text(text,encoding='utf-8-sig')
folder=root/'Assets/Resources/RadioAudio'
folder.mkdir(exist_ok=True)
shutil.copyfile(root/'AudioPreviews/FamilyRecording/Mother_Then_Company_AI_EN.wav',folder/'FamilyRecording.wav')
meta=(root/'Assets/Resources/PropFalls/P1Thud.wav.meta').read_text(encoding='utf-8-sig')
meta=meta.replace('ced31cb4ef622e44cb7688dc9528be21',uuid.uuid4().hex).replace('preloadAudioData: 0','preloadAudioData: 1')
(folder/'FamilyRecording.wav.meta').write_text(meta,encoding='utf-8')
(root/'Assets/Scenes/Scene2AudioPlayback.cs.meta').write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n',encoding='utf-8')
print('Connected all Scene2 audio triggers and imported combined recording.')
