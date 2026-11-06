using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Same fixed-pixel layout as Scene1; a separate camera shows the actual robot.
[DefaultExecutionOrder(10500)]
public sealed class Scene2SelfMonitor : MonoBehaviour
{
    public Camera MonitorCamera{get;private set;}
    public RectTransform Frame{get;private set;}
    public Transform Target{get;private set;}
    public RenderTexture Feed{get;private set;}
    private Canvas canvas;
    private Renderer[] body;
    private bool[] hidden;
    private bool rendering;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize(){SceneManager.sceneLoaded-=Bind;SceneManager.sceneLoaded+=Bind;Bind(SceneManager.GetActiveScene(),LoadSceneMode.Single);}
    private static void Bind(Scene scene,LoadSceneMode mode)
    {
        if(!scene.IsValid()||!scene.name.Replace(" ","").Equals("scene2",StringComparison.OrdinalIgnoreCase))return;
        foreach(var root in scene.GetRootGameObjects())if(root.GetComponentInChildren<Scene2SelfMonitor>(true))return;
        new GameObject("Scene2 Robot Self Monitor").AddComponent<Scene2SelfMonitor>();
    }
    private IEnumerator Start()
    {
        // Eye-camera binding runs during scene initialization; resolve after it.
        yield return null;
        var eye=FindObjectOfType<Scene2RobotCameraFollow>();
        Target=eye?eye.Target:null;
        if(!Target){var player=FindObjectOfType<FirstPersonPlayer>();if(player)Target=player.transform;}
        if(!Target){enabled=false;yield break;}
        body=Target.GetComponentsInChildren<Renderer>(true);hidden=new bool[body.Length];
        var ui=new GameObject("SurveillanceMonitorUI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));ui.transform.SetParent(transform,false);
        canvas=ui.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=50;
        var scaler=ui.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;scaler.scaleFactor=1;
        var frame=new GameObject("MonitorFrame",typeof(RectTransform),typeof(Image));frame.transform.SetParent(ui.transform,false);
        Frame=frame.GetComponent<RectTransform>();Frame.anchorMin=Frame.anchorMax=Frame.pivot=Vector2.one;
        Frame.anchoredPosition=new Vector2(-1.2000122f,-.7000122f);Frame.sizeDelta=new Vector2(195,120);
        var border=frame.GetComponent<Image>();border.color=new Color(1,1,1,.392f);border.raycastTarget=false;
        border.type=Image.Type.Simple;
        var screen=new GameObject("MonitorImage",typeof(RectTransform),typeof(RawImage));screen.transform.SetParent(Frame,false);
        var rect=screen.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=new Vector2(-12,-12);rect.anchoredPosition=Vector2.zero;
        Feed=new RenderTexture(732,432,24,RenderTextureFormat.ARGB32){name="Scene2 Live Robot Monitor",antiAliasing=2};Feed.Create();
        var image=screen.GetComponent<RawImage>();image.texture=Feed;image.raycastTarget=false;
        // This object only holds the monitor camera, never an AudioListener.
        MonitorCamera=gameObject.AddComponent<Camera>();MonitorCamera.targetTexture=Feed;MonitorCamera.aspect=183f/108;
        MonitorCamera.fieldOfView=55;MonitorCamera.nearClipPlane=.3f;MonitorCamera.farClipPlane=1500;
        MonitorCamera.cullingMask=~((1<<5)|(1<<30)|(1<<31));MonitorCamera.depth=10;
        MonitorCamera.clearFlags=CameraClearFlags.Skybox;MonitorCamera.allowHDR=true;
        Follow();
    }
    private Bounds BodyBounds()
    {
        var bounds=new Bounds(Target.position+Vector3.up*35,new Vector3(25,70,25));bool found=false;
        foreach(var renderer in body)if(renderer&&renderer.enabled&&(renderer is MeshRenderer||renderer is SkinnedMeshRenderer))
        {if(!found){bounds=renderer.bounds;found=true;}else bounds.Encapsulate(renderer.bounds);}
        return bounds;
    }
    private void Follow()
    {
        if(!Target||!MonitorCamera)return;
        var bounds=BodyBounds();float height=Mathf.Max(1,bounds.size.y);
        var rotation=Quaternion.Euler(0,Target.eulerAngles.y,0);
        Vector3 aim=bounds.center+Vector3.up*height*.05f;
        Vector3 offset=rotation*new Vector3(height*.6f,height*.65f,-height*1.65f);
        // Stop before room walls, while ignoring the robot's own colliders.
        float distance=offset.magnitude;
        foreach(var hit in Physics.SphereCastAll(aim,Mathf.Max(.1f,height*.025f),offset.normalized,distance,~((1<<5)|(1<<30)|(1<<31)),QueryTriggerInteraction.Ignore))
            if(hit.transform!=Target&&!hit.transform.IsChildOf(Target))distance=Mathf.Min(distance,Mathf.Max(height*.45f,hit.distance-height*.035f));
        transform.SetPositionAndRotation(aim+offset.normalized*distance,Quaternion.LookRotation(-offset,Vector3.up));
    }
    private void LateUpdate(){Follow();if(Input.GetKeyDown(KeyCode.Tab)&&canvas)canvas.enabled=!canvas.enabled;if(MonitorCamera&&canvas)MonitorCamera.enabled=canvas.enabled;}
    private void OnPreCull()
    {
        if(body==null)return;
        for(int i=0;i<body.Length;i++)if(body[i]){hidden[i]=body[i].forceRenderingOff;body[i].forceRenderingOff=false;}
        rendering=true;
    }
    private void Restore(){if(!rendering)return;for(int i=0;i<body.Length;i++)if(body[i])body[i].forceRenderingOff=hidden[i];rendering=false;}
    private void OnPostRender(){Restore();}
    private void OnDisable(){Restore();}
    private void OnDestroy(){Restore();if(MonitorCamera)MonitorCamera.targetTexture=null;if(Feed){Feed.Release();Destroy(Feed);}}
}
