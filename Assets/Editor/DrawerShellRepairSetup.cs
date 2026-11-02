using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
[InitializeOnLoad]public static class DrawerShellRepairSetup{
 static DrawerShellRepairSetup(){EditorApplication.delayCall+=Apply;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode)EditorApplication.delayCall+=Apply;};}
 [MenuItem("Tools/JIQI/Repair Drawer Shell")]
 public static void Apply(){
 if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Apply;return;}if(EditorApplication.isPlayingOrWillChangePlaymode)return;
 var scene=SceneManager.GetActiveScene();if(!string.Equals(scene.name.Replace(" ",""),"scene2",StringComparison.OrdinalIgnoreCase))return;
 var drawer=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).SingleOrDefault(t=>t.name=="drawers");var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Repairs/DrawerClosedShell.asset");if(!drawer || !mesh)return;
 var shell=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).SingleOrDefault(t=>t.name=="Drawer Complete Shell");if(shell)return;
 var go=new GameObject("Drawer Complete Shell");go.transform.SetParent(drawer,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=drawer.GetComponent<MeshRenderer>().sharedMaterials;go.AddComponent<MeshCollider>().sharedMesh=mesh;
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 Directory.CreateDirectory("D:/-11-transfer-audit/drawer-shell-20261020");File.WriteAllText("D:/-11-transfer-audit/drawer-shell-20261020/live-editor.txt","Drawer side walls and bottom saved "+DateTime.Now.ToString("s"));
 }
}
