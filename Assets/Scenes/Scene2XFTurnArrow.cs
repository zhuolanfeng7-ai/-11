using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(11230)]
public sealed class Scene2XFTurnArrow : MonoBehaviour
{
    private Transform target;private Renderer[] surfaces;
    private Transform repairNote,microphone;private Vector3 approachDirection;
    private bool passedMicrophone;
    private Material material;private MaterialPropertyBlock properties;
    private readonly List<Mesh> meshes=new List<Mesh>();
    private readonly List<Renderer> markers=new List<Renderer>();
    private readonly List<Light> pools=new List<Light>();
    private bool built,arrived;private float opacity=1;
    public bool IsBuilt=>built;public bool HasArrived=>arrived;public float Opacity=>opacity;
    public Vector3 StartPoint{get;private set;}public Vector3 TipPoint{get;private set;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize(){SceneManager.sceneLoaded-=Bind;SceneManager.sceneLoaded+=Bind;Bind(SceneManager.GetActiveScene(),LoadSceneMode.Single);}
    static void Bind(Scene scene,LoadSceneMode mode)
    {
        if(!scene.IsValid()||!string.Equals(scene.name.Replace(" ",""),"scene2",StringComparison.OrdinalIgnoreCase))return;
        if(scene.GetRootGameObjects().Any(g=>g.GetComponent<Scene2XFTurnArrow>()))return;
        var owner=new GameObject("XF Floating Forward Then Left Route");SceneManager.MoveGameObjectToScene(owner,scene);owner.AddComponent<Scene2XFTurnArrow>();
    }
    private void Build()
    {
        var lighting=FindObjectOfType<Scene2MicrophoneLighting>();
        if(!lighting||lighting.XFGuideRoute==null||lighting.XFGuideRoute.Length<2)return;
        target=gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t=>t.name.Equals("xf",StringComparison.OrdinalIgnoreCase));
        if(!target)return;
        var objects=gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        repairNote=objects.FirstOrDefault(t=>t.name.Equals("p5",StringComparison.OrdinalIgnoreCase));
        microphone=objects.FirstOrDefault(t=>t.name.Equals("maikefeng",StringComparison.OrdinalIgnoreCase));
        if(microphone&&Camera.main)approachDirection=Vector3.ProjectOnPlane(microphone.position-Camera.main.transform.position,Vector3.up).normalized;
        surfaces=target.GetComponentsInChildren<Renderer>(true).Where(r=>r.GetComponent<MeshFilter>()).ToArray();if(surfaces.Length==0)return;
        var shader=Resources.Load<Shader>("OpeningEffects/FloorTurnRoute");if(!shader)return;
        material=new Material(shader){hideFlags=HideFlags.HideAndDontSave};properties=new MaterialPropertyBlock();
        var route=lighting.XFGuideRoute;var forward=Vector3.right;
        // Start at the microphone approach from image one. End is the player's
        // captured image-two location, projected to the same floating height.
        var left=Vector3.Cross(forward,Vector3.up).normalized;
        var start=new Vector3(-165.94f,route[0].y+24,257.46f);StartPoint=start;
        TipPoint=new Vector3(8.20f,start.y,274.56f);
        const float radius=7,headLength=7;
        float stem=Vector3.Dot(TipPoint-start,forward)-radius;
        var points=new List<Vector3>{start,start+forward*stem};var center=start+forward*stem+left*radius;
        for(int i=1;i<=20;i++){float a=i*Mathf.PI/40;points.Add(center+forward*(Mathf.Sin(a)*radius)-left*(Mathf.Cos(a)*radius));}
        var end=TipPoint-left*headLength;points.Add(end);
        AddRibbon(points,4.3f,.25f,"Soft Cyan Halo");AddRibbon(points,3.2f,1,"Cyan Forward Left Ribbon");
        AddPolygon(new[]{TipPoint,end-left*1.5f+forward*7.5f,end-left*1.5f-forward*7.5f},false,"Left Arrowhead");
        for(int i=0;i<3;i++)
        {
            var c=start+forward*(stem*(.2f+i*.3f))+Vector3.up*.06f;var right=-left;
            AddPolygon(new[]{c+forward*2.6f,c-forward*.6f-right*2.5f,c-forward*2.3f-right*2.5f,c+forward*.8f,c-forward*2.3f+right*2.5f,c-forward*.6f+right*2.5f},true,"Forward Chevron "+(i+1));
        }
        for(int i=0;i<3;i++)
        {
            var owner=new GameObject("Route Cyan Floor Spill");owner.transform.SetParent(transform,false);owner.transform.position=start+forward*(stem*(.2f+i*.3f))+Vector3.down*17;
            var light=owner.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(.34f,.75f,1);light.intensity=.8f;light.range=20;light.shadows=LightShadows.None;light.cullingMask=~((1<<30)|(1<<31));pools.Add(light);
        }
        built=true;
    }
    private void AddRibbon(List<Vector3> points,float width,float strength,string label)
    {
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
        for(int i=0;i<points.Count;i++)
        {
            var direction=(points[Mathf.Min(i+1,points.Count-1)]-points[Mathf.Max(0,i-1)]).normalized;var side=Vector3.Cross(Vector3.up,direction)*width;
            vertices.Add(points[i]+side);vertices.Add(points[i]-side);uv.Add(new Vector2(0,0));uv.Add(new Vector2(1,0));
            if(i==0)continue;int n=i*2;triangles.AddRange(new[]{n-2,n-1,n,n,n-1,n+1});
        }
        AddMesh(vertices,triangles,uv,label,strength);
    }
    private void AddPolygon(Vector3[] points,bool white,string label)
    {
        var triangles=new List<int>();for(int i=1;i<points.Length-1;i++)triangles.AddRange(new[]{0,i,i+1});
        AddMesh(points.ToList(),triangles,points.Select(p=>new Vector2(.95f,white?1:0)).ToList(),label,1);
    }
    private void AddMesh(List<Vector3> vertices,List<int> triangles,List<Vector2> uv,string label,float strength)
    {
        var mesh=new Mesh{name=label};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,uv);mesh.RecalculateBounds();meshes.Add(mesh);
        var owner=new GameObject(label);owner.transform.SetParent(transform,false);owner.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=owner.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
        var values=new MaterialPropertyBlock();values.SetFloat("_Strength",strength);renderer.SetPropertyBlock(values);markers.Add(renderer);
    }
    void LateUpdate()
    {
        if(!built){Build();return;}
        var camera=Camera.main;if(!camera||!target)return;
        EvaluateArrival(camera.transform.position);
        opacity=Mathf.MoveTowards(opacity,arrived?0:1,Time.unscaledDeltaTime/.8f);float breath=.9f+.1f*Mathf.Sin(Time.unscaledTime*1.8f);
        foreach(var marker in markers){marker.GetPropertyBlock(properties);properties.SetFloat("_Opacity",opacity*breath);marker.SetPropertyBlock(properties);marker.enabled=opacity>.001f;}
        foreach(var pool in pools)if(pool)pool.intensity=.8f*opacity*breath;
    }
    public void EvaluateArrival(Vector3 playerPosition)
    {
        if(arrived)return;
        if(microphone&&approachDirection.sqrMagnitude>.5f&&Vector3.Dot(Vector3.ProjectOnPlane(playerPosition-microphone.position,Vector3.up),approachDirection)>3)passedMicrophone=true;
        if(!passedMicrophone||!repairNote)return;
        var renderer=repairNote.GetComponentInChildren<Renderer>();
        var nearest=renderer?renderer.bounds.ClosestPoint(playerPosition):repairNote.position;
        if(Vector3.ProjectOnPlane(playerPosition-nearest,Vector3.up).magnitude<70)arrived=true;
    }
    void OnDestroy(){foreach(var mesh in meshes)if(mesh)Destroy(mesh);if(material)Destroy(material);}
}
