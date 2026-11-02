using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(11220)]
[DisallowMultipleComponent]
public sealed class DiscoBallSilverGlints : MonoBehaviour
{
    [SerializeField,Range(8,32)] private int glintCount=20;
    private sealed class Glint
    {
        public Vector3 point,normal;
        public Transform transform;
        public Renderer renderer;
        public float period,phase,size;
    }
    private readonly List<Glint> glints=new List<Glint>();
    private MeshFilter source;
    private Renderer surface;
    private GameObject glintRoot;
    private Material material;
    private MaterialPropertyBlock properties;
    private float emphasis=1f;
    public int GlintCount=>glints.Count;
    public Transform GlintRoot=>glintRoot?glintRoot.transform:null;
    public Renderer Surface=>surface;
    public int ActiveGlintCount{get;private set;}
    public float TotalBrightness{get;private set;}

    public static bool IsBallName(string name)
    {
        string n=name.Trim().ToLowerInvariant().Replace(" ","");
        int suffix=n.IndexOf('(');
        if(suffix>=0)
        {
            int duplicate;
            if(!n.EndsWith(")")||!int.TryParse(n.Substring(suffix+1,n.Length-suffix-2),out duplicate))return false;
            n=n.Substring(0,suffix);
        }
        return n=="qiue"||(n.Length==4&&n.StartsWith("qiu")&&n[3]>='1'&&n[3]<='7');
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded-=Bind;SceneManager.sceneLoaded+=Bind;
        Bind(SceneManager.GetActiveScene(),LoadSceneMode.Single);
    }
    private static void Bind(Scene scene,LoadSceneMode mode)
    {
        if(!scene.IsValid()||!string.Equals(scene.name.Replace(" ",""),"scene2",StringComparison.OrdinalIgnoreCase))return;
        foreach(var root in scene.GetRootGameObjects())
        foreach(var target in root.GetComponentsInChildren<Transform>(true))
            if(IsBallName(target.name)&&!target.GetComponent<DiscoBallSilverGlints>())target.gameObject.AddComponent<DiscoBallSilverGlints>();
    }
    private static int bindingScene=-1;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetBinding(){bindingScene=-1;}
    private void Start()
    {
        Build();
        if(bindingScene!=gameObject.scene.handle)
        {bindingScene=gameObject.scene.handle;StartCoroutine(FinishBinding());}
    }
    private System.Collections.IEnumerator FinishBinding()
    {
        // Other scene initializers can restore object names after sceneLoaded.
        yield return null;
        Bind(gameObject.scene,LoadSceneMode.Single);
        yield return null;
        Bind(gameObject.scene,LoadSceneMode.Single);
    }
    private void Build()
    {
        source=GetComponent<MeshFilter>();surface=GetComponent<Renderer>();
        if(!source||!source.sharedMesh||!surface)return;
        float largest=BallRadius(source);
        foreach(var root in gameObject.scene.GetRootGameObjects())
        foreach(var candidate in root.GetComponentsInChildren<MeshFilter>(true))
            if(IsBallName(candidate.name)&&candidate.sharedMesh)
                largest=Mathf.Max(largest,BallRadius(candidate));
        float relativeSize=BallRadius(source)/Mathf.Max(.0001f,largest);
        bool small=relativeSize<.55f;
        bool large=relativeSize>=.8f;
        int count=small?2:large?glintCount:12;
        emphasis=small?1f:large?1.6f:1.35f;
        var shader=Resources.Load<Shader>("OpeningEffects/SilverFourPointGlint");if(!shader)return;
        if(glintRoot){glintRoot.SetActive(false);Destroy(glintRoot);}glints.Clear();
        if(material)Destroy(material);
        material=new Material(shader){name=name+" Silver Glints",hideFlags=HideFlags.HideAndDontSave};
        glintRoot=new GameObject(name+" Silver Four Point Glints");SceneManager.MoveGameObjectToScene(glintRoot,gameObject.scene);
        properties=new MaterialPropertyBlock();
        var bounds=source.sharedMesh.bounds;
        float normalization=1/Mathf.Max(.0001f,bounds.extents.magnitude);
        var probe=new GameObject("Temporary Glint Surface Probe"){hideFlags=HideFlags.HideAndDontSave};
        probe.layer=30;probe.transform.position=Vector3.one*18000;probe.transform.localScale=Vector3.one*normalization;
        var collider=probe.AddComponent<MeshCollider>();collider.sharedMesh=source.sharedMesh;Physics.SyncTransforms();
        int seed=17;foreach(char c in name)seed=unchecked(seed*31+c);
        var random=new System.Random(seed&int.MaxValue);
        float offset=(float)random.NextDouble()*Mathf.PI*2;
        for(int i=0;i<count;i++)
        {
            float y=1-2*(i+.5f)/count;float angle=i*2.399963f+offset;
            var direction=new Vector3(Mathf.Cos(angle)*Mathf.Sqrt(1-y*y),y,Mathf.Sin(angle)*Mathf.Sqrt(1-y*y));
            var point=bounds.center+Vector3.Scale(bounds.extents,direction);var normal=direction;
            var from=probe.transform.TransformPoint(bounds.center+direction*bounds.extents.magnitude*2.5f);
            RaycastHit hit;
            if(collider.Raycast(new Ray(from,-direction),out hit,5))
            {point=probe.transform.InverseTransformPoint(hit.point);normal=hit.normal;}
            // A simple quad mesh avoids adding even a temporary gameplay collider.
            var star=new GameObject("Silver Four Point "+(i+1));star.transform.SetParent(glintRoot.transform,false);
            star.AddComponent<MeshFilter>().sharedMesh=QuadMesh();var renderer=star.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            renderer.lightProbeUsage=UnityEngine.Rendering.LightProbeUsage.Off;renderer.reflectionProbeUsage=UnityEngine.Rendering.ReflectionProbeUsage.Off;
            glints.Add(new Glint{point=point,normal=normal,transform=star.transform,renderer=renderer,
                period=3.2f+(float)random.NextDouble()*2.4f,phase=offset+i*2.399963f,
                size=!small&&i%4==0?2f+(float)random.NextDouble()*.6f:.85f+(float)random.NextDouble()*.3f});
        }
        probe.SetActive(false);Destroy(probe);
    }
    private static float BallRadius(MeshFilter mesh)
    {
        var e=mesh.sharedMesh.bounds.extents;var s=mesh.transform.lossyScale;
        return Mathf.Max(Mathf.Abs(e.x*s.x),Mathf.Abs(e.y*s.y),Mathf.Abs(e.z*s.z));
    }
    private static Mesh quad;
    private static Mesh QuadMesh()
    {
        if(quad)return quad;
        quad=new Mesh{name="Silver Glint Quad",hideFlags=HideFlags.HideAndDontSave};
        quad.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)};
        quad.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};quad.triangles=new[]{0,2,1,0,3,2};quad.RecalculateBounds();return quad;
    }
    private void LateUpdate()
    {
        if(glints.Count==0){Build();if(glints.Count==0)return;}
        EvaluateForCamera(Camera.main,Time.time);
    }
    public void EvaluateForCamera(Camera camera,float time)
    {
        ActiveGlintCount=0;TotalBrightness=0;
        if(properties==null)properties=new MaterialPropertyBlock();
        bool visible=camera&&surface&&surface.enabled&&surface.gameObject.activeInHierarchy;
        float radius=surface?Mathf.Max(surface.bounds.extents.x,surface.bounds.extents.y,surface.bounds.extents.z):1;
        var normalMatrix=transform.localToWorldMatrix.inverse.transpose;
        foreach(var glint in glints)
        {
            if(!visible){glint.renderer.enabled=false;continue;}
            Vector3 point=transform.TransformPoint(glint.point),normal=normalMatrix.MultiplyVector(glint.normal).normalized;
            Vector3 toward=(camera.transform.position-point).normalized;
            float facing=Vector3.Dot(normal,toward);
            float depth=Vector3.Dot(point-camera.transform.position,camera.transform.forward);
            if(facing<=.08f||depth<=camera.nearClipPlane||depth>=camera.farClipPlane){glint.renderer.enabled=false;continue;}
            float pulse=.5f+.5f*Mathf.Sin(time*Mathf.PI*2/glint.period+glint.phase);
            float sparkle=pulse*pulse*(3-2*pulse);
            float strength=(.025f+1.1f*sparkle)*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.08f,.42f,facing));
            float pixelSize=camera.orthographic?2*camera.orthographicSize/Mathf.Max(1,camera.pixelHeight)
                :2*depth*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f)/Mathf.Max(1,camera.pixelHeight);
            float size=Mathf.Clamp(radius*.13f*glint.size,pixelSize*4*glint.size,pixelSize*20*glint.size)*emphasis*(.86f+sparkle*.24f);
            glint.transform.SetPositionAndRotation(point+normal*radius*.004f+toward*radius*.001f,
                Quaternion.LookRotation(-toward,camera.transform.up));
            glint.transform.localScale=Vector3.one*size;
            properties.SetFloat("_Strength",strength);glint.renderer.SetPropertyBlock(properties);glint.renderer.enabled=true;
            ActiveGlintCount++;TotalBrightness+=strength;
        }
    }
    private void OnEnable(){if(glintRoot)glintRoot.SetActive(true);}
    private void OnDisable(){if(glintRoot)glintRoot.SetActive(false);}
    private void OnDestroy(){if(glintRoot)Destroy(glintRoot);if(material)Destroy(material);}
}
