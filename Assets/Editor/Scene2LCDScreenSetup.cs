using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class Scene2LCDScreenSetup
{
    static Scene2LCDScreenSetup(){EditorApplication.delayCall+=Apply;}
    [MenuItem("Tools/JIQI/Refresh LCD Video Screen")]
    public static void Apply()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Apply;return;}
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var scene=SceneManager.GetActiveScene();
        if(!scene.IsValid()||scene.name.Replace(" ","").ToLowerInvariant()!="scene2")return;
        var screen=Scene2SilentVideoLoop.FindScreen(scene);
        if(Scene2SilentVideoLoop.ApplyScreenMaterial(screen))
        {
            EditorUtility.SetDirty(screen);
            PrefabUtility.RecordPrefabInstancePropertyModifications(screen);
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
