using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
[InitializeOnLoad]public static class Alloy2RewardSetup{
 static Alloy2RewardSetup(){EditorApplication.delayCall+=Apply;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode)EditorApplication.delayCall+=Apply;};}
 [MenuItem("Tools/JIQI/Setup Alloy2 Fourth Turn Reward")]
 public static void Apply(){
 if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Apply;return;}
 if(EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isPlaying)return;
 var scene=SceneManager.GetActiveScene();if(!string.Equals(scene.name.Replace(" ",""),"scene2",StringComparison.OrdinalIgnoreCase))return;
 try{
 var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();var reward=all.SingleOrDefault(t=>t.name=="alloy2");if(!reward)return;
 var box=all.Single(t=>t.name=="hezi");var crank=all.Single(t=>t.name=="shoubing").GetComponent<MusicBoxCrank>();var target=box.Find("Alloy2RaisedPosition");
 bool changed=!target||!reward.GetComponent<MusicBoxRewardLift>()||!reward.GetComponent<HoverScanHighlight>()||!reward.GetComponent<Collider>()||!crank.UnlockReward||!reward.GetComponent<AlloyPickup>();
 if(!target){var bb=box.GetComponent<Renderer>().bounds;var rb=reward.GetComponent<Renderer>().bounds;target=new GameObject("Alloy2RaisedPosition").transform;target.SetParent(box,true);target.position=reward.position+Vector3.up*(bb.center.y+bb.size.y*.25f-rb.center.y);target.rotation=reward.rotation;}
 if(!reward.GetComponent<Collider>()){var collider=reward.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=reward.GetComponent<MeshFilter>().sharedMesh;}
 var lift=reward.GetComponent<MusicBoxRewardLift>();if(!lift)lift=reward.gameObject.AddComponent<MusicBoxRewardLift>();lift.Configure(target);EditorUtility.SetDirty(lift);
 var highlight=reward.GetComponent<HoverScanHighlight>();if(!highlight)highlight=reward.gameObject.AddComponent<HoverScanHighlight>();highlight.ConfigureCrankReward(lift);highlight.Configure(6,100);EditorUtility.SetDirty(highlight);
 var pickup=reward.GetComponent<AlloyPickup>();if(!pickup)pickup=reward.gameObject.AddComponent<AlloyPickup>();pickup.ConfigureMusicBox(lift);EditorUtility.SetDirty(pickup);
 crank.ConfigureReward(lift);EditorUtility.SetDirty(crank);
 if(changed&&!EditorApplication.isPlaying){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
 Directory.CreateDirectory("D:/-11-transfer-audit/alloy2-lift-20261019");File.WriteAllText("D:/-11-transfer-audit/alloy2-lift-20261019/live-editor.txt","alloy2 linked to fourth completed turn; start="+reward.position+" target="+target.position+"; playing="+EditorApplication.isPlaying+" time="+DateTime.Now.ToString("s"));
 }catch(Exception e){Debug.LogException(e);}}
}
