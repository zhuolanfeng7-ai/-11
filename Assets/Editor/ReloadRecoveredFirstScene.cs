#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
[InitializeOnLoad]
public static class ReloadRecoveredFirstScene
{
    const string Marker = "C:/Users/13560/Documents/ChatGPT/项目4/unity-fix/scene-reloaded.txt";
    static ReloadRecoveredFirstScene() { EditorApplication.delayCall += Repair; }
    static void Repair()
    {
        if (System.IO.File.Exists(Marker)) return;
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/scene 1.unity") return;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.playModeStateChanged -= AfterStop;
            EditorApplication.playModeStateChanged += AfterStop;
            EditorApplication.isPlaying = false;
            return;
        }
        Reload();
    }
    static void AfterStop(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode) return;
        EditorApplication.playModeStateChanged -= AfterStop;
        EditorApplication.delayCall += Reload;
    }
    static void Reload()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/scene 1.unity", OpenSceneMode.Single);
        var cameras = Object.FindObjectsOfType<Camera>();
        int count = 0;
        foreach (var camera in cameras)
            if (camera.enabled && camera.gameObject.activeInHierarchy && camera.targetTexture == null && camera.targetDisplay == 0) count++;
        System.IO.File.WriteAllText(Marker, "Scene reopened; active Display 1 cameras: " + count);
        Debug.Log("Recovered first scene reopened. Active Display 1 cameras: " + count);
    }
}
#endif