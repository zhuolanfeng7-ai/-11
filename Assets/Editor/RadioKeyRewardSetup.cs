using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
[InitializeOnLoad]public static class RadioKeyRewardSetup{
 static RadioKeyRewardSetup(){EditorApplication.delayCall+=Apply;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode)EditorApplication.delayCall+=Apply;};}
 [MenuItem("Tools/JIQI/Setup Radio Key Reward")]
 public static void Apply(){
 if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Apply;return;}if(EditorApplication.isPlayingOrWillChangePlaymode)return;
 var scene=SceneManager.GetActiveScene();if(!string.Equals(scene.name.Replace(" ",""),"scene2",StringComparison.OrdinalIgnoreCase))return;
 var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
 var radio=all.SingleOrDefault(t=>t.name=="radio");var key=all.SingleOrDefault(t=>string.Equals(t.name,"KEY",StringComparison.OrdinalIgnoreCase));if(!radio||!key)return;
 bool changed=false;
 if(!key.GetComponent<Collider>()){var c=key.gameObject.AddComponent<MeshCollider>();c.sharedMesh=key.GetComponent<MeshFilter>().sharedMesh;changed=true;}
 var end=all.SingleOrDefault(t=>t.name=="KEY Raised Position");if(!end){end=new GameObject("KEY Raised Position").transform;end.position=new Vector3(key.position.x,275f,key.position.z);changed=true;}
 var desiredEnd=new Vector3(key.position.x,275f,key.position.z);if(Vector3.Distance(end.position,desiredEnd)>.001f){end.position=desiredEnd;EditorUtility.SetDirty(end);changed=true;}
 var lift=key.GetComponent<MusicBoxRewardLift>();if(!lift){lift=key.gameObject.AddComponent<MusicBoxRewardLift>();changed=true;}
 var liftData=new SerializedObject(lift);
 if(lift.RaisedPosition!=end || !liftData.FindProperty("worldYOnly").boolValue){lift.ConfigureVerticalLift(end);changed=true;}
 var settings=new SerializedObject(lift);if(Mathf.Abs(settings.FindProperty("riseDuration").floatValue-2.3f)>.001f){settings.FindProperty("riseDuration").floatValue=2.3f;settings.ApplyModifiedPropertiesWithoutUndo();changed=true;}
 var hover=key.GetComponent<HoverScanHighlight>();if(!hover){hover=key.gameObject.AddComponent<HoverScanHighlight>();changed=true;}hover.ConfigureCrankReward(lift);hover.Configure(6,100);
 var pickup=key.GetComponent<AlloyPickup>();if(!pickup){pickup=key.gameObject.AddComponent<AlloyPickup>();changed=true;}pickup.ConfigureKey(lift);
 var reward=radio.GetComponent<RadioKeyReward>();if(!reward){reward=radio.gameObject.AddComponent<RadioKeyReward>();changed=true;}if(reward.KeyReward!=lift){reward.Configure(lift);changed=true;}
 EditorUtility.SetDirty(lift);EditorUtility.SetDirty(hover);EditorUtility.SetDirty(pickup);EditorUtility.SetDirty(reward);
 if(changed){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
 Directory.CreateDirectory("D:/-11-transfer-audit/radio-key-20261019");File.WriteAllText("D:/-11-transfer-audit/radio-key-20261019/live-editor.txt","KEY configured "+DateTime.Now.ToString("s"));
 }
}
