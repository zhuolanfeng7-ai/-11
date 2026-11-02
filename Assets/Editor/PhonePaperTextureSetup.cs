using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PhonePaperTextureSetup
{
    static PhonePaperTextureSetup(){EditorApplication.delayCall+=Apply;}
    [MenuItem("Tools/JIQI/Refresh Phone Paper Receipt")]
    public static void Apply()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Apply;return;}
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var scene=SceneManager.GetActiveScene();
        PhonePaperTexture.ApplyScene(scene,LoadSceneMode.Single);
        var material=Resources.Load<Material>("PhonePaper/PhonePaperReceipt");
        if(material){EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);}
        if(scene.IsValid()&&scene.name.Replace(" ","").ToLowerInvariant()=="scene2")EditorSceneManager.MarkSceneDirty(scene);
    }
}
