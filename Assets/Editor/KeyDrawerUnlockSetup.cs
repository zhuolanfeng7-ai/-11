using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
[InitializeOnLoad]public static class KeyDrawerUnlockSetup{
 static KeyDrawerUnlockSetup(){EditorApplication.delayCall+=Apply;EditorSceneManager.sceneSaved+=scene=>EditorApplication.delayCall+=Apply;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode)EditorApplication.delayCall+=Apply;};}
 [MenuItem("Tools/JIQI/Setup Key Drawer Unlock")]
 public static void Apply(){
 if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Apply;return;}if(EditorApplication.isPlayingOrWillChangePlaymode)return;
 var scene=SceneManager.GetActiveScene();if(!string.Equals(scene.name.Replace(" ",""),"scene2",StringComparison.OrdinalIgnoreCase))return;
 var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();var drawer=all.SingleOrDefault(t=>t.name=="drawers");var key=all.SingleOrDefault(t=>string.Equals(t.name,"key",StringComparison.OrdinalIgnoreCase));if(!key)return;
 var shell=all.SingleOrDefault(t=>t.name=="Drawer Complete Shell");if(!shell && drawer){DrawerShellRepairSetup.Apply();shell=drawer.Find("Drawer Complete Shell");}if(!shell)return;
 var button=all.SingleOrDefault(t=>string.Equals(t.name,"bottona",StringComparison.OrdinalIgnoreCase));
 bool changed=false;
 var previous=drawer?drawer.GetComponent<KeyDrawerUnlock>():null;var controller=shell.GetComponent<KeyDrawerUnlock>();
 if(!controller){controller=shell.gameObject.AddComponent<KeyDrawerUnlock>();if(previous)EditorUtility.CopySerialized(previous,controller);changed=true;}
 if(previous){UnityEngine.Object.DestroyImmediate(previous);changed=true;}
 var outward=Vector3.ProjectOnPlane(-shell.up,Vector3.up).normalized;
 if(controller.KeySource!=key || Vector3.Dot(controller.OutwardWorld,outward)<.999f){var center=shell.GetComponent<Renderer>().bounds.center;controller.Configure(key,outward,center+outward*24f,22f);changed=true;}
 if(controller.MotionRoot!=shell || controller.ExtraClickTarget!=button){controller.ConfigureTargets(shell,button);changed=true;}
 if(button)foreach(var mesh in button.GetComponentsInChildren<MeshFilter>(true)){if(!mesh.sharedMesh || mesh.GetComponent<Collider>())continue;mesh.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh.sharedMesh;changed=true;}
 var housing=KeyDrawerUnlock.FindPianoHousing(scene);if(controller.Housing!=housing){controller.ConfigureHousing(housing);changed=true;}
 var item=all.SingleOrDefault(t=>string.Equals(t.name,"nail",StringComparison.OrdinalIgnoreCase));if(controller.SlidingItem!=item){controller.ConfigureSlidingItem(item);changed=true;}
 EditorUtility.SetDirty(controller);
 if(changed){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
 Directory.CreateDirectory("D:/-11-transfer-audit/drawer-shell-button-20261020");File.WriteAllText("D:/-11-transfer-audit/drawer-shell-button-20261020/live-editor.txt","animation owner=Drawer Complete Shell; bottona found="+(bool)button+"; time="+DateTime.Now.ToString("s"));
 }
}
