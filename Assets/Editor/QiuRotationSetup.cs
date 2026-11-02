using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class QiuRotationSetup
{
    const string Audit = "D:/-11-transfer-audit/rotation-20261018/";
    static QiuRotationSetup()
    {
        EditorApplication.delayCall += Apply;
        EditorApplication.playModeStateChanged += state => {
            if(state==PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += Apply;
        };
    }
    [MenuItem("Tools/QIU/Apply Continuous Rotation")]
    public static void Apply()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall+=Apply; return; }
        if(EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying) return;
        try {
            var scene=SceneManager.GetActiveScene();
            if(!scene.IsValid() || !scene.isLoaded) return;
            var targets=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true))
                .Where(t=>IsTarget(t.name)).ToArray();
            if(targets.Length==0) return;
            Directory.CreateDirectory(Audit);
            bool playing=EditorApplication.isPlaying;
            bool changed=targets.Any(t=>!t.GetComponent<ContinuousSelfRotation>() || !t.GetComponent<ContinuousSelfRotation>().enabled);
            if(changed && !playing && !File.Exists(Audit+"scene.before.unity"))
                EditorSceneManager.SaveScene(scene,Audit+"scene.before.unity",true);
            foreach(var target in targets) {
                var rotation=target.GetComponent<ContinuousSelfRotation>();
                if(!rotation) rotation=target.gameObject.AddComponent<ContinuousSelfRotation>();
                rotation.enabled=true;
                EditorUtility.SetDirty(rotation);
            }
            if(changed && !playing) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
            File.WriteAllText(Audit+"live-editor.json",JsonUtility.ToJson(new Report {
                scene=scene.path, names=targets.Select(t=>t.name).ToArray(), count=targets.Length,
                playing=playing, saved=!playing, time=DateTime.Now.ToString("s") },true));
            Debug.Log("[QIU Rotation] "+targets.Length+" objects rotate around their own local Z axis at 45 degrees/second; saved="+!playing);
        } catch(Exception e) { Directory.CreateDirectory(Audit); File.WriteAllText(Audit+"error.txt",e.ToString()); Debug.LogException(e); }
    }
    public static bool IsTarget(string name)
    {
        string n=name.Trim().ToLowerInvariant();
        return n=="qiue" || (n.Length==4 && n.StartsWith("qiu") && n[3]>='1' && n[3]<='7');
    }
    [Serializable] class Report { public string scene,time; public string[] names; public int count; public bool playing,saved; }
}
