Add-Type -AssemblyName System.Speech
$taskFolder='D:\-11\AudioPreviews\FamilyRecording'
$taskVoice=New-Object System.Speech.Synthesis.SpeechSynthesizer
$taskVoice.SelectVoice('Microsoft Zira Desktop')
$taskVoice.SetOutputToWaveFile((Join-Path $taskFolder 'mother_clean.wav'))
$taskVoice.SpeakSsml('<speak version="1.0" xmlns="http://www.w3.org/2001/10/synthesis" xml:lang="en-US"><prosody rate="-4%" pitch="+3%" volume="soft">Why aren''t you home yet?<break time="230ms"/> I''ve saved you some dinner.<break time="420ms"/>You have time to make money.<break time="190ms"/> Don''t forget to take care of yourself.<break time="380ms"/>Call out when you get home,<break time="170ms"/> so I know you''re back.</prosody></speak>')
$taskVoice.SetOutputToNull()
$taskVoice.SetOutputToWaveFile((Join-Path $taskFolder 'system_clean.wav'))
$taskVoice.SpeakSsml('<speak version="1.0" xmlns="http://www.w3.org/2001/10/synthesis" xml:lang="en-US"><prosody rate="-2%" pitch="-13%">Your family recording preview has ended.<break time="400ms"/>You have sufficient funds.<break time="400ms"/>However,<break time="120ms"/> you no longer own the right to hear.</prosody></speak>')
$taskVoice.Dispose()
