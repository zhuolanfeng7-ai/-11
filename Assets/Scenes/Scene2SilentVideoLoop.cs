using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

// Enforce the screen's silent looping playback when Scene2 starts, including
// when the editor still has a previously loaded copy of the scene in memory.
[DisallowMultipleComponent]
[RequireComponent(typeof(VideoPlayer))]
public sealed class Scene2SilentVideoLoop : MonoBehaviour
{
    private VideoPlayer player;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= Bind;
        SceneManager.sceneLoaded += Bind;
        Bind(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }
    private static void Bind(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() || !string.Equals(scene.name.Replace(" ", ""), "scene2", StringComparison.OrdinalIgnoreCase)) return;
        foreach (var root in scene.GetRootGameObjects())
        foreach (var video in root.GetComponentsInChildren<VideoPlayer>(true))
        {
            bool matches = string.Equals(video.name.Trim(), "vedio", StringComparison.OrdinalIgnoreCase)
                || string.Equals(video.name.Trim(), "video", StringComparison.OrdinalIgnoreCase)
                || (video.clip && video.clip.name == "\u8bbd\u523a\u672a\u6765\u542c\u89c9\u79df\u8d41\u5e7f\u544a\u77ed\u7247\u751f\u6210");
            if (matches && !video.GetComponent<Scene2SilentVideoLoop>()) video.gameObject.AddComponent<Scene2SilentVideoLoop>();
        }
    }
    public static Renderer FindScreen(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (string.Equals(t.name.Trim(), "vedio", StringComparison.OrdinalIgnoreCase))
                return t.GetComponent<Renderer>() ? t.GetComponent<Renderer>() : t.GetComponentInChildren<Renderer>(true);
        return null;
    }
    private void OnEnable()
    {
        player = GetComponent<VideoPlayer>();
        var screen = FindScreen(gameObject.scene);
        if (screen)
        {
            ApplyScreenMaterial(screen);
            player.renderMode = VideoRenderMode.MaterialOverride;
            player.targetMaterialRenderer = screen;
            player.targetMaterialProperty = "_MainTex";
            player.targetTexture = null;
        }
        if (!screen) { Debug.LogError("Scene2 video: renderer named vedio was not found.", this); return; }
        player.playOnAwake = true;
        player.isLooping = true;
        player.audioOutputMode = VideoAudioOutputMode.None;
        player.enabled = true;
        if (!player.isPlaying) player.Play();
    }

    public static bool ApplyScreenMaterial(Renderer screen)
    {
        var material=Resources.Load<Material>("ScreenEffects/VideoLCD");
        if(!screen||!material||screen.sharedMaterial==material)return false;
        var materials=screen.sharedMaterials;
        if(materials.Length==0)screen.sharedMaterial=material;
        else{materials[0]=material;screen.sharedMaterials=materials;}
        return true;
    }
}
