using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
[InitializeOnLoad]public static class MusicBoxCrankSetup{
 static MusicBoxCrankSetup(){EditorApplication.delayCall+=Apply;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode)EditorApplication.delayCall+=Apply;};}
 [MenuItem("Tools/JIQI/Setup Music Box Crank")]
 public static void Apply(){
 if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Apply;return;}
 if(EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isPlaying)return;
 var scene=SceneManager.GetActiveScene();if(!string.Equals(scene.name.Replace(" ",""),"scene2",StringComparison.OrdinalIgnoreCase))return;
 try{
 var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();var box=all.Single(t=>t.name=="hezi");var handle=all.Single(t=>t.name=="shoubing");
 var pivot=box.Find("CrankConnection");bool changed=!pivot||!handle.GetComponent<MusicBoxCrank>() || !handle.GetComponent<MusicBoxCrank>().AudioOutput;
 if(!pivot){pivot=new GameObject("CrankConnection").transform;pivot.SetParent(box,true);pivot.SetPositionAndRotation(new Vector3(-394.24142f,262.49428f,33.49296f),Quaternion.LookRotation(new Vector3(.54859224f,.07436362f,-.83277645f),Vector3.up));}
 var drum=all.SingleOrDefault(t=>t.name=="zhuantong");
 var crank=handle.GetComponent<MusicBoxCrank>();if(!crank)crank=handle.gameObject.AddComponent<MusicBoxCrank>();crank.Configure(pivot);
 if(drum){if(crank.LinkedDrum!=drum)changed=true;crank.ConfigureDrum(drum,new Vector3(.55222445f,.0096688f,-.83363941f));}
 var gear=all.SingleOrDefault(t=>t.name=="gear");var gear1=all.SingleOrDefault(t=>t.name=="gear1");
 if(!gear1)gear1=all.SingleOrDefault(t=>t.name=="tripo_node_cbd8ac82.002");
 if(gear&&gear1){if(crank.Gear!=gear||crank.Gear1!=gear1||gear1.name!="gear1")changed=true;gear1.name="gear1";crank.ConfigureGears(gear,gear1);}
 var output=crank.AudioOutput;
 if(!output){var audioNode=box.Find("MusicBoxAudio");if(!audioNode){audioNode=new GameObject("MusicBoxAudio").transform;audioNode.SetParent(box,true);audioNode.position=pivot.position;}output=audioNode.GetComponent<AudioSource>();if(!output)output=audioNode.gameObject.AddComponent<AudioSource>();}
 output.playOnAwake=false;output.loop=false;output.spatialBlend=.3f;output.minDistance=15f;output.maxDistance=180f;output.dopplerLevel=0;output.volume=.8f;
 var clips=Enumerable.Range(1,4).Select(i=>AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/MusicBox/Turn"+i.ToString("00")+".wav")).ToArray();
 crank.ConfigureAudio(output,clips,AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/MusicBox/CompleteMelody.wav"));EditorUtility.SetDirty(crank);EditorUtility.SetDirty(output);
 if(changed&&!EditorApplication.isPlaying){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
 Directory.CreateDirectory("D:/-11-transfer-audit/crank-axis-20261019");File.WriteAllText("D:/-11-transfer-audit/crank-axis-20261019/live-editor.txt","CrankConnection on hezi; shoubing configured; playing="+EditorApplication.isPlaying+"; time="+DateTime.Now.ToString("s"));
 }catch(Exception e){Debug.LogException(e);}}
}
