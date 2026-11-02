using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
[InitializeOnLoad]public static class RadioQuarterTurnSetup
{
 static RadioQuarterTurnSetup(){EditorApplication.delayCall+=Apply;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode)EditorApplication.delayCall+=Apply;};}
 [MenuItem("Tools/JIQI/Setup Radio Quarter Turn")]
 public static void Apply(){
 if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Apply;return;}
 if(EditorApplication.isPlayingOrWillChangePlaymode)return;
 var scene=SceneManager.GetActiveScene();if(!string.Equals(scene.name.Replace(" ",""),"scene2",StringComparison.OrdinalIgnoreCase))return;
 var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
 bool changed=false;foreach(var name in new[]{"botton1","botton2","botton3"}){
 var knob=all.SingleOrDefault(t=>t.name==name);if(!knob)continue;if(!knob.GetComponent<Collider>()){var c=knob.gameObject.AddComponent<MeshCollider>();c.sharedMesh=knob.GetComponent<MeshFilter>().sharedMesh;changed=true;}
 var dial=knob.GetComponent<RadioQuarterTurn>();if(!dial){dial=knob.gameObject.AddComponent<RadioQuarterTurn>();changed=true;}
 int index=name=="botton1"?1:name=="botton2"?2:3;int target=index==1?2:index==2?4:3;
 var clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Radio/Knob0"+index+".wav");
 if(dial.TargetStep!=target || dial.TurnSound!=clip){dial.ConfigureTuning(target,clip);EditorUtility.SetDirty(dial);changed=true;}
 }
 if(changed){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
 Directory.CreateDirectory("D:/-11-transfer-audit/radio-scale-20261019");File.WriteAllText("D:/-11-transfer-audit/radio-scale-20261019/live-editor.txt","botton1, botton2, botton3 configured "+DateTime.Now.ToString("s"));
 }
}
