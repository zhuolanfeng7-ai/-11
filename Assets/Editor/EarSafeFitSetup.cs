using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class EarSafeFitSetup
{
    static EarSafeFitSetup(){EditorApplication.delayCall+=ApplyOnce;}
    static void ApplyOnce()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=ApplyOnce;return;}
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var scene=SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/scene 2.unity")return;
        string key=Application.dataPath+"/EarSafeFit-v2-click-animation";
        if(EditorPrefs.GetBool(key,false))return;
        var ear=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t=>t.name=="ear");
        if(!ear)return;
        Undo.RecordObject(ear,"Fit ear into safe recess");
        var original=PrefabUtility.GetCorrespondingObjectFromSource(ear); if(!original)return; ear.localPosition=new Vector3(344f,128.8f,original.localPosition.z);
        ear.localRotation=original.localRotation;
        ear.localScale=original.localScale;
        PrefabUtility.RecordPrefabInstancePropertyModifications(ear);
        EditorUtility.SetDirty(ear);EditorSceneManager.MarkSceneDirty(scene);
        EditorPrefs.SetBool(key,true);
    }
}
