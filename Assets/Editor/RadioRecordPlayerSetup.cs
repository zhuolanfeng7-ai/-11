using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
[InitializeOnLoad]public static class RadioRecordPlayerSetup{
 static RadioRecordPlayerSetup(){EditorApplication.delayCall+=Apply;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode)EditorApplication.delayCall+=Apply;};}
 [MenuItem("Tools/JIQI/Setup Radio Record Player")]
 public static void Apply(){
 if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Apply;return;}if(EditorApplication.isPlayingOrWillChangePlaymode)return;
 var scene=SceneManager.GetActiveScene();if(!string.Equals(scene.name.Replace(" ",""),"scene2",StringComparison.OrdinalIgnoreCase))return;
 var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
 var radio=all.SingleOrDefault(t=>t.name=="radio")??all.SingleOrDefault(t=>t.name=="tripo_node_142c5262");var record=all.SingleOrDefault(t=>t.name=="cp1");if(!radio||!record)return;
 var clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/RadioAudio/FamilyRecording.wav");if(!clip)return;
 bool changed=false;if(radio.name!="radio"){radio.name="radio";changed=true;}
 if(!radio.GetComponent<Collider>()){var c=radio.gameObject.AddComponent<MeshCollider>();c.sharedMesh=radio.GetComponent<MeshFilter>().sharedMesh;changed=true;}
 var player=radio.GetComponent<RadioRecordPlayer>();if(!player){player=radio.gameObject.AddComponent<RadioRecordPlayer>();changed=true;}
 if(player.RecordSource!=record || player.Music!=clip){player.Configure(record,clip);EditorUtility.SetDirty(player);changed=true;}
 if(changed){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
 Directory.CreateDirectory("D:/-11-transfer-audit/record-playback-20261019");File.WriteAllText("D:/-11-transfer-audit/record-playback-20261019/live-editor.txt","radio + cp1 configured "+DateTime.Now.ToString("s"));
 }
}
