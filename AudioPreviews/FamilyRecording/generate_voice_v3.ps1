Add-Type -AssemblyName System.Speech
$taskFolder='D:\-11\AudioPreviews\FamilyRecording'
$taskVoice=New-Object System.Speech.Synthesis.SpeechSynthesizer
$taskVoice.SelectVoice('Microsoft Zira Desktop')
$taskVoice.Rate=-1
$taskVoice.SetOutputToWaveFile((Join-Path $taskFolder 'mother_v3_clean.wav'))
$taskVoice.Speak("Why aren't you home yet? I've saved you some dinner. You have time to make money. Don't forget to take care of yourself. Call out when you get home, so I know you're back.")
$taskVoice.SetOutputToNull()
$taskVoice.Rate=0
$taskVoice.SetOutputToWaveFile((Join-Path $taskFolder 'system_v3_clean.wav'))
$taskVoice.Speak('Your family recording preview has ended. You have sufficient funds. However, you no longer own the right to hear.')
$taskVoice.Dispose()
