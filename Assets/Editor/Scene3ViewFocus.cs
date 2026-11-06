using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Keep the Scene view centered on the company whenever scene3 is opened.
[InitializeOnLoad]
internal static class Scene3ViewFocus
{
    private const string ScenePath = "Assets/Scenes/scene3.unity";
    static Scene3ViewFocus()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.delayCall += FocusIfScene3;
    }
    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (scene.path == ScenePath) EditorApplication.delayCall += FocusIfScene3;
    }
    private static void FocusIfScene3()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath || !scene.isLoaded) return;
        var floor = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true))
            .FirstOrDefault(r => r.name.Equals("flood", StringComparison.OrdinalIgnoreCase) && r.gameObject.activeInHierarchy && r.enabled);
        if (!floor) return;
        var view = SceneView.lastActiveSceneView;
        if (!view) view = EditorWindow.GetWindow<SceneView>();
        if (!view) return;
        view.in2DMode = false;
        view.LookAt(floor.bounds.center + Vector3.up * .35f, Quaternion.Euler(38f, 35f, 0f), 5f, false, true);
        view.Repaint();
    }
}
