using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class DrawerUpShellRepairSetup
{
    static DrawerUpShellRepairSetup()
    {
        EditorApplication.delayCall+=Apply;
        EditorSceneManager.sceneOpened+=(scene,mode)=>Apply();
        EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredEditMode)Apply();};
    }
    [MenuItem("Tools/JIQI/Repair Scene1 Upper Drawer")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Apply;return;}
        var scene=SceneManager.GetActiveScene();
        if(!scene.IsValid()||!scene.name.Replace(" ","").Equals("scene1",System.StringComparison.OrdinalIgnoreCase))return;
        foreach(var root in scene.GetRootGameObjects())foreach(var target in root.GetComponentsInChildren<DrawerUpController>(true))
        {
            var existing=target.transform.Find(DrawerUpShellRepair.ShellName);
            if(existing)
            {
                var source=target.GetComponent<MeshRenderer>();Material expected=null;
                if(source)foreach(var candidate in source.sharedMaterials)if(candidate){expected=candidate;break;}
                bool changed=false;foreach(var panel in existing.GetComponentsInChildren<MeshRenderer>())if(panel.sharedMaterial!=expected)changed=true;
                DrawerUpShellRepair.Ensure(target.transform);
                if(changed)EditorSceneManager.MarkSceneDirty(scene);
                continue;
            }
            var shell=DrawerUpShellRepair.Ensure(target.transform);
            if(shell){Undo.RegisterCreatedObjectUndo(shell.gameObject,"Repair upper drawer panels");EditorSceneManager.MarkSceneDirty(scene);}
        }
    }
}
