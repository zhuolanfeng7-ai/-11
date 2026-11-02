using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class TelescopingDeviceSetup
{
    const string Audit="D:/-11-transfer-audit/device-20261018/";
    static TelescopingDeviceSetup()
    {
        EditorApplication.delayCall+=Apply;
        EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode)EditorApplication.delayCall+=Apply;};
    }
    [MenuItem("Tools/JIQI/Setup Telescoping Device")]
    public static void Apply()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating){EditorApplication.delayCall+=Apply;return;}
        if(EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)return;
        var scene=SceneManager.GetActiveScene();
        if(!scene.IsValid() || !string.Equals(scene.name.Replace(" ",""),"scene2",StringComparison.OrdinalIgnoreCase))return;
        try {
            var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
            Func<string,Transform> find=name=>all.SingleOrDefault(t=>string.Equals(t.name,name,StringComparison.OrdinalIgnoreCase));
            var one=find("zhuzi1");var two=find("zhuzi2");var a=find("zhuziA");var b=find("zhuziB");
            // The authored upper cap has not been renamed yet in this scene.
            if(!b)b=find("柱体.026");
            if(!one||!two||!a||!b)throw new Exception("Required zhuzi1/zhuzi2/zhuziA/zhuziB upper cap not found");
            Directory.CreateDirectory(Audit);
            bool playing=EditorApplication.isPlaying;
            var eight=find("zhuzi8");var microphone=find("maikefeng");
            if(!microphone)microphone=find("tripo_node_faf6a10c");
            var followers=new[]{eight,microphone}.Where(t=>t).ToArray();
            var existing=two.GetComponent<TelescopingDevice>();
            bool followerChange=(existing && !existing.FollowingParts.SequenceEqual(followers)) || (microphone && microphone.name!="maikefeng");
            var board=find("dangban");var alloy=find("alloy1");
            var revealExisting=two.GetComponent<TubeRewardReveal>();
            bool revealChanged=board && alloy && (!revealExisting || revealExisting.Board!=board || revealExisting.Alloy!=alloy);
            bool changed=(alloy && (!alloy.GetComponent<HoverScanHighlight>() || !alloy.GetComponent<Collider>() || !alloy.GetComponent<AlloyPickup>())) || revealChanged || !two.GetComponent<HoverScanHighlight>() || followerChange || !two.GetComponent<TelescopingDevice>() || !two.GetComponent<Collider>() || b.name!="zhuziB";
            if(changed&&!playing&&!File.Exists(Audit+"scene.before.unity"))EditorSceneManager.SaveScene(scene,Audit+"scene.before.unity",true);
            var collider=two.GetComponent<Collider>();
            if(!collider){var box=two.gameObject.AddComponent<BoxCollider>();var bounds=two.GetComponent<MeshFilter>().sharedMesh.bounds;box.center=bounds.center;box.size=bounds.size;collider=box;}
            var device=two.GetComponent<TelescopingDevice>();if(!device)device=two.gameObject.AddComponent<TelescopingDevice>();
            b.name="zhuziB";if(microphone)microphone.name="maikefeng";device.Configure(one,a,b,collider);device.ConfigureFollowingParts(followers);EditorUtility.SetDirty(device);
            var highlight=two.GetComponent<HoverScanHighlight>();
            if(!highlight)highlight=two.gameObject.AddComponent<HoverScanHighlight>();
            highlight.Configure(6f,100f);EditorUtility.SetDirty(highlight);
            if(board && alloy){
                var reveal=two.GetComponent<TubeRewardReveal>();if(!reveal)reveal=two.gameObject.AddComponent<TubeRewardReveal>();
                reveal.Configure(board,alloy,AssetDatabase.LoadAssetAtPath<Shader>("Assets/BlenderScene2/TubeReveal.shader"));EditorUtility.SetDirty(reveal);
                var alloyCollider=alloy.GetComponent<Collider>();
                if(!alloyCollider){var box=alloy.gameObject.AddComponent<BoxCollider>();var meshBounds=alloy.GetComponent<MeshFilter>().sharedMesh.bounds;box.center=meshBounds.center;box.size=meshBounds.size;}
                var alloyHighlight=alloy.GetComponent<HoverScanHighlight>();if(!alloyHighlight)alloyHighlight=alloy.gameObject.AddComponent<HoverScanHighlight>();
                alloyHighlight.Configure(6f,100f);alloyHighlight.ConfigureReward(reveal);EditorUtility.SetDirty(alloyHighlight);
                var pickup=alloy.GetComponent<AlloyPickup>();if(!pickup)pickup=alloy.gameObject.AddComponent<AlloyPickup>();
                pickup.Configure(reveal);EditorUtility.SetDirty(pickup);
            }
            if(changed&&!playing){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
            File.WriteAllText(Audit+"live-editor.json",JsonUtility.ToJson(new Report{scene=scene.path,target=two.name,upperCap=b.name,followers=followers.Select(t=>t.name).ToArray(),missingFollowers=new[]{eight?null:"zhuzi8",microphone?null:"maikefeng"}.Where(n=>n!=null).ToArray(),saved=!playing,playing=playing,time=DateTime.Now.ToString("s")},true));
            Debug.Log("[Telescoping Device] Single-click retract configured on zhuzi2; upper cap="+b.name+"; saved="+!playing);
        }catch(Exception e){Directory.CreateDirectory(Audit);File.WriteAllText(Audit+"error.txt",e.ToString());Debug.LogException(e);}
    }
    [Serializable] class Report{public string scene,target,upperCap,time;public string[] followers,missingFollowers;public bool saved,playing;}
}
