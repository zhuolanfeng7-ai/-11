using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class JiqiControlsSetup
{
    const string Report = "D:/-11-transfer-audit/controls-20261018/live-editor.json";
    static JiqiControlsSetup() { EditorApplication.delayCall += Apply; EditorApplication.playModeStateChanged += OnPlayMode; }
    static void OnPlayMode(PlayModeStateChange state) { if(state == PlayModeStateChange.EnteredEditMode || state == PlayModeStateChange.EnteredPlayMode) EditorApplication.delayCall += Apply; }
    [MenuItem("Tools/JIQI/Apply Scene2 Controls")]
    public static void Apply()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += Apply; return; }
        try
        {
            var scene = SceneManager.GetActiveScene();
            if(!string.Equals(scene.name.Replace(" ", ""), "scene2", StringComparison.OrdinalIgnoreCase)) return;
            var robot = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t=>string.Equals(t.name,"jiqiren1",StringComparison.OrdinalIgnoreCase)||string.Equals(t.name,"JIQI",StringComparison.OrdinalIgnoreCase));
            if(!robot) throw new Exception("JIQI not found in current scene");
            var camera = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).FirstOrDefault(c=>c.CompareTag("MainCamera"));
            if(!camera) throw new Exception("Main Camera missing");
            bool playing = EditorApplication.isPlaying;
            bool missingPlayer = !robot.GetComponent<FirstPersonPlayer>();
            bool missingBody = !robot.GetComponent<Rigidbody>();
            if(!playing && (missingPlayer || missingBody)) EditorSceneManager.SaveScene(scene,"D:/-11-transfer-audit/controls-20261018/live-scene.before.unity",true);
            var body=robot.GetComponent<Rigidbody>();
            if(!body) body=robot.gameObject.AddComponent<Rigidbody>();
            body.useGravity=false;
            body.constraints=RigidbodyConstraints.FreezeRotation|RigidbodyConstraints.FreezePositionY;
            body.interpolation=RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            var controller=robot.GetComponent<FirstPersonPlayer>();
            if(!controller) controller=robot.gameObject.AddComponent<FirstPersonPlayer>();
            var data=new SerializedObject(controller);
            data.FindProperty("playerCamera").objectReferenceValue=camera;
            var renderer=robot.GetComponentInChildren<Renderer>();
            data.FindProperty("moveSpeed").floatValue=renderer ? renderer.bounds.size.y*.8f : 4.5f;
            data.ApplyModifiedPropertiesWithoutUndo();
            controller.enabled=true;
            var follow=camera.GetComponent<Scene2RobotCameraFollow>();
            if(!follow) follow=camera.gameObject.AddComponent<Scene2RobotCameraFollow>();
            follow.Bind(robot);
            if(!playing) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
            File.WriteAllText(Report,JsonUtility.ToJson(new Status { scene=scene.path, playing=playing, addedPlayer=missingPlayer, addedBody=missingBody, enabled=controller.enabled, player=robot.name, cameraParent=camera.transform.parent.name, speed=data.FindProperty("moveSpeed").floatValue, saved=!playing, time=DateTime.Now.ToString("s") },true));
            Debug.Log("[JIQI Controls] Current Scene2 robot has WASD/mouse controller and eye camera; saved="+!playing,robot);
        }
        catch(Exception e) { File.WriteAllText(Report,e.ToString()); Debug.LogException(e); }
    }
    [Serializable] class Status { public string scene,player,cameraParent,time; public bool playing,addedPlayer,addedBody,enabled,saved; public float speed; }
}
