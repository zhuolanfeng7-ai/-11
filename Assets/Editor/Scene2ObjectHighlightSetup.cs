using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
[InitializeOnLoad]
public static class Scene2ObjectHighlightSetup
{
    static Scene2ObjectHighlightSetup(){EditorApplication.delayCall+=Apply;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode)EditorApplication.delayCall+=Apply;};}
    [MenuItem("Tools/JIQI/Setup Scene2 Object Highlights")]
    public static void Apply()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Apply;return;}
        if(EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isPlaying)return;
        var scene=SceneManager.GetActiveScene();if(!string.Equals(scene.name.Replace(" ",""),"scene2",StringComparison.OrdinalIgnoreCase))return;
        try{
            var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();bool changed=Scene2HighlightExclusions.Apply(scene);
            foreach(string name in new[]{"shoubing","paper11","cp1","nail","wood","bottona"}){
                var t=all.SingleOrDefault(x=>string.Equals(x.name,name,StringComparison.OrdinalIgnoreCase));
                if(!t)continue;
                var pickupRoot=t;
                if(name=="wood"&&!t.GetComponent<MeshFilter>()){var mesh=t.GetComponentInChildren<MeshFilter>(true);if(!mesh)continue;t=mesh.transform;}
                if(!t.GetComponent<Collider>()){var col=t.gameObject.AddComponent<MeshCollider>();col.sharedMesh=t.GetComponent<MeshFilter>().sharedMesh;changed=true;}
                var h=t.GetComponent<HoverScanHighlight>();if(!h){h=t.gameObject.AddComponent<HoverScanHighlight>();changed=true;}
                var data=new SerializedObject(h);if(!data.FindProperty("scene2Object").boolValue)changed=true;
                h.ConfigureScene2();h.Configure(6,100);if(name=="bottona"||name=="Drawer Complete Shell")h.ConfigureOcclusionHousing(KeyDrawerUnlock.FindPianoHousing(scene));EditorUtility.SetDirty(h);
                if(name=="cp1" || name=="nail" || name=="wood"){
                    var pickup=t.GetComponent<AlloyPickup>();if(!pickup){pickup=t.gameObject.AddComponent<AlloyPickup>();changed=true;}
                    if(name=="wood")pickup.ConfigureWood(pickupRoot);else if(name=="nail")pickup.ConfigureNail();else pickup.ConfigureCp1();EditorUtility.SetDirty(pickup);
                }
                if(name=="zhengju1" || name=="paper11"){
                    var inspect=t.GetComponent<EvidenceInspect>();if(!inspect){inspect=t.gameObject.AddComponent<EvidenceInspect>();changed=true;}
                    inspect.Configure(t.GetComponent<Renderer>().sharedMaterial.mainTexture);EditorUtility.SetDirty(inspect);
                }
            }
            if(changed&&!EditorApplication.isPlaying){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
            Directory.CreateDirectory("D:/-11-transfer-audit/three-highlights-20261019");
            File.WriteAllText("D:/-11-transfer-audit/three-highlights-20261019/live-editor.txt","paper11 found="+all.Any(x=>string.Equals(x.name,"paper11",StringComparison.OrdinalIgnoreCase))+"; hezi, shoubing, zhengju1 configured; playing="+EditorApplication.isPlaying+"; time="+DateTime.Now.ToString("s"));
        }catch(Exception e){Debug.LogException(e);}
    }
}
