using System;
using UnityEngine;

// Notify before playback so an interrupted recording pauses in the same frame.
public static class Scene2AudioPlayback
{
    public static event Action<AudioSource> SoundStarting;

    public static void Play(AudioSource source)
    {
        if (!source || !source.isActiveAndEnabled || !source.clip) return;
        SoundStarting?.Invoke(source);
        source.Play();
    }

    public static void PlayOneShot(AudioSource source, AudioClip clip, float volume = 1f)
    {
        if (!source || !source.isActiveAndEnabled || !clip || volume <= 0) return;
        SoundStarting?.Invoke(source);
        source.PlayOneShot(clip, volume);
    }

    public static void Resume(AudioSource source)
    {
        if (!source || !source.isActiveAndEnabled || !source.clip) return;
        SoundStarting?.Invoke(source);
        source.UnPause();
    }

    public static void PlayAtPoint(AudioClip clip, Vector3 position, float volume)
    {
        if (!clip || volume <= 0) return;
        SoundStarting?.Invoke(null);
        AudioSource.PlayClipAtPoint(clip, position, volume);
    }
}
