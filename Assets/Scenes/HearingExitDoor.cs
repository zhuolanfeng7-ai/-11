using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class HearingExitDoor : MonoBehaviour
{
    private EarSafeInstallation installation;
    private Scene2RepairCelebration repairState;
    private Transform leaf,seal,hinge;
    private Bounds closedBounds;
    private Renderer backdrop,haze;
    private Material whiteMaterial,mistMaterial;
    private Mesh portalMesh;
    private Light beam,fill;
    private AudioSource mechanism;
    private AudioClip click,motor,unlock;
    private float initialListenerVolume,whiteout,originalSide=-1;
    private FirstPersonPlayer player;
    public bool SequenceStarted{get;private set;}
    public bool IsOpen{get;private set;}
    public float Opening{get;private set;}
    public float Whiteout=>whiteout;
    public Transform DoorLeaf=>leaf;
    public Transform MovingSeal=>seal;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize(){SceneManager.sceneLoaded-=Bind;SceneManager.sceneLoaded+=Bind;Bind(SceneManager.GetActiveScene(),LoadSceneMode.Single);}
    static void Bind(Scene scene,LoadSceneMode mode)
    {
        if(!scene.IsValid()||!string.Equals(scene.name.Replace(" ",""),"scene2",StringComparison.OrdinalIgnoreCase))return;
        if(scene.GetRootGameObjects().Any(r=>r.GetComponent<HearingExitDoor>()))return;
        var owner=new GameObject("Hearing Restored Exit Door");SceneManager.MoveGameObjectToScene(owner,scene);owner.AddComponent<HearingExitDoor>();
    }
    void Start()
    {
        var all=gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
        leaf=all.FirstOrDefault(t=>t.name.Equals("door",StringComparison.OrdinalIgnoreCase));
        installation=FindObjectOfType<EarSafeInstallation>();player=FindObjectOfType<FirstPersonPlayer>();
        if(!leaf||!leaf.GetComponent<Renderer>()){enabled=false;return;}
        closedBounds=leaf.GetComponent<Renderer>().bounds;initialListenerVolume=AudioListener.volume;
        if(!leaf.GetComponents<Collider>().Any(c=>c.enabled&&!c.isTrigger))
        {var mesh=leaf.GetComponent<MeshFilter>();if(mesh&&mesh.sharedMesh){var box=leaf.gameObject.AddComponent<BoxCollider>();box.center=mesh.sharedMesh.bounds.center;box.size=mesh.sharedMesh.bounds.size;}}
        var camera=Camera.main;if(camera)originalSide=Mathf.Sign(camera.transform.position.x-closedBounds.center.x);
        seal=all.Where(t=>t!=leaf&&t.GetComponent<Renderer>()).FirstOrDefault(t=>
        {
            var b=t.GetComponent<Renderer>().bounds;
            return Mathf.Abs(b.center.x-closedBounds.center.x)<4&&Mathf.Abs(b.center.z-closedBounds.center.z)<6&&b.extents.x<5&&b.size.y>40&&b.size.y<90;
        });
        hinge=new GameObject("Door Right Hinge").transform;hinge.SetParent(transform,false);
        hinge.position=new Vector3(closedBounds.center.x,closedBounds.min.y,closedBounds.min.z);
        leaf.SetParent(hinge,true);if(seal)seal.SetParent(hinge,true);
        // The frame and bell rack remain under their original parents.
        MakeWallAperture(all);
        BuildLightSpace();
        var audio=new GameObject("Exit Mechanical Sounds");audio.transform.SetParent(transform,false);
        mechanism=audio.AddComponent<AudioSource>();mechanism.playOnAwake=false;mechanism.spatialBlend=.35f;mechanism.minDistance=90;mechanism.maxDistance=500;mechanism.volume=.8f;
        click=MechanicalClip("Ear metal seat",.2f,0);motor=MechanicalClip("Safe mechanism",.85f,1);unlock=MechanicalClip("Door latch release",.17f,2);
        ApplyOpening(0);
    }
    private struct WallVertex
    {
        public Vector3 p;public Vector2 uv;
        public static WallVertex Lerp(WallVertex a,WallVertex b,float t){return new WallVertex{p=Vector3.Lerp(a.p,b.p,t),uv=Vector2.Lerp(a.uv,b.uv,t)};}
    }
    private static float Side(WallVertex v,Vector2 a,Vector2 b){return (b.x-a.x)*(v.p.y-a.y)-(b.y-a.y)*(v.p.z-a.x);}
    private static System.Collections.Generic.List<WallVertex> Clip(System.Collections.Generic.List<WallVertex> input,Vector2 a,Vector2 b,bool inside)
    {
        var result=new System.Collections.Generic.List<WallVertex>();if(input.Count==0)return result;
        var previous=input[input.Count-1];float pd=Side(previous,a,b)*(inside?1:-1);
        foreach(var current in input)
        {
            float cd=Side(current,a,b)*(inside?1:-1);
            if((cd>=0)!=(pd>=0))result.Add(WallVertex.Lerp(previous,current,pd/(pd-cd)));
            if(cd>=0)result.Add(current);previous=current;pd=cd;
        }
        return result;
    }
    private void MakeWallAperture(Transform[] all)
    {
        var source=Resources.Load<Mesh>("SafeInstallation/Cube62Mesh");if(!source)return;
        float half=closedBounds.extents.z+1;float bottom=closedBounds.min.y-1;float top=closedBounds.max.y+1;float center=top-half;
        var outline=new System.Collections.Generic.List<Vector2>{new Vector2(closedBounds.center.z-half,bottom),new Vector2(closedBounds.center.z+half,bottom),new Vector2(closedBounds.center.z+half,center)};
        for(int i=1;i<=32;i++){float a=i*Mathf.PI/32;outline.Add(new Vector2(closedBounds.center.z+Mathf.Cos(a)*half,center+Mathf.Sin(a)*half));}
        var originalVertices=source.vertices;var originalUV=source.uv;var originalTriangles=source.triangles;
        foreach(var wall in all.Where(t=>t.name.Replace(" ","").Equals("cube62",StringComparison.OrdinalIgnoreCase)||t.name.Replace(" ","").StartsWith("cube62(",StringComparison.OrdinalIgnoreCase)))
        {
            var renderer=wall.GetComponent<MeshRenderer>();if(!renderer)continue;
            var vertices=new System.Collections.Generic.List<Vector3>();var uv=new System.Collections.Generic.List<Vector2>();var triangles=new System.Collections.Generic.List<int>();
            Action<System.Collections.Generic.List<WallVertex>> append=polygon=>
            {
                if(polygon.Count<3)return;int first=vertices.Count;
                foreach(var v in polygon){vertices.Add(v.p-wall.position);uv.Add(v.uv);}
                for(int i=1;i<polygon.Count-1;i++)
                    if(wall.localToWorldMatrix.determinant<0)triangles.AddRange(new[]{first,first+i+1,first+i});else triangles.AddRange(new[]{first,first+i,first+i+1});
            };
            for(int i=0;i<originalTriangles.Length;i+=3)
            {
                var remaining=new System.Collections.Generic.List<WallVertex>();
                for(int j=0;j<3;j++){int index=originalTriangles[i+j];remaining.Add(new WallVertex{p=wall.TransformPoint(originalVertices[index]),uv=index<originalUV.Length?originalUV[index]:Vector2.zero});}
                for(int edge=0;edge<outline.Count&&remaining.Count>0;edge++)
                {var a=outline[edge];var b=outline[(edge+1)%outline.Count];append(Clip(remaining,a,b,false));remaining=Clip(remaining,a,b,true);}
            }
            var mesh=new Mesh{name="Wall with arched hearing exit"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();mesh.RecalculateNormals();apertureMeshes.Add(mesh);
            var owner=new GameObject(wall.name+" Exit Aperture");owner.transform.SetParent(transform,false);owner.transform.position=wall.position;owner.AddComponent<MeshFilter>().sharedMesh=mesh;
            var replacement=owner.AddComponent<MeshRenderer>();replacement.sharedMaterials=renderer.sharedMaterials;
            owner.AddComponent<MeshCollider>().sharedMesh=mesh;
            wallRenderers.Add(renderer);renderer.enabled=false;
            foreach(var collider in wall.GetComponents<Collider>())if(collider.enabled){wallColliders.Add(collider);collider.enabled=false;}
        }
    }
    private readonly System.Collections.Generic.List<Mesh> apertureMeshes=new System.Collections.Generic.List<Mesh>();
    private readonly System.Collections.Generic.List<Renderer> wallRenderers=new System.Collections.Generic.List<Renderer>();
    private readonly System.Collections.Generic.List<Collider> wallColliders=new System.Collections.Generic.List<Collider>();
    void BuildLightSpace()
    {
        var shader=Resources.Load<Shader>("OpeningEffects/HearingDoorLight");if(!shader)return;
        whiteMaterial=new Material(shader){hideFlags=HideFlags.HideAndDontSave};whiteMaterial.SetFloat("_Soft",0);
        mistMaterial=new Material(shader){hideFlags=HideFlags.HideAndDontSave};mistMaterial.SetFloat("_Soft",1);
        float half=closedBounds.extents.z;float bottom=closedBounds.min.y;float top=closedBounds.max.y;float center=top-half;
        var contour=new System.Collections.Generic.List<Vector3>{new Vector3(0,bottom,-half),new Vector3(0,bottom,half),new Vector3(0,center,half)};
        for(int i=1;i<=32;i++){float a=i*Mathf.PI/32;contour.Add(new Vector3(0,center+Mathf.Sin(a)*half,Mathf.Cos(a)*half));}
        var vertices=new System.Collections.Generic.List<Vector3>();var uv=new System.Collections.Generic.List<Vector2>();var triangles=new System.Collections.Generic.List<int>();
        float depth=closedBounds.size.z+25;
        foreach(var p in contour){vertices.Add(new Vector3(closedBounds.max.x+depth,p.y,closedBounds.center.z+p.z));uv.Add(new Vector2((p.z/half+1)*.5f,(p.y-bottom)/(top-bottom)));}
        for(int i=1;i<contour.Count-1;i++)triangles.AddRange(new[]{0,i,i+1});
        for(int i=0;i<contour.Count;i++)
        {
            int next=(i+1)%contour.Count;int n=vertices.Count;
            vertices.AddRange(new[]{new Vector3(closedBounds.max.x+2,contour[i].y,closedBounds.center.z+contour[i].z),new Vector3(closedBounds.max.x+2,contour[next].y,closedBounds.center.z+contour[next].z),vertices[next],vertices[i]});
            uv.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
        }
        portalMesh=new Mesh{name="White arched space behind hearing door"};portalMesh.SetVertices(vertices);portalMesh.SetTriangles(triangles,0);portalMesh.SetUVs(0,uv);portalMesh.RecalculateBounds();
        var owner=new GameObject("White Space Beyond Door");owner.transform.SetParent(transform,false);owner.AddComponent<MeshFilter>().sharedMesh=portalMesh;backdrop=owner.AddComponent<MeshRenderer>();backdrop.sharedMaterial=whiteMaterial;backdrop.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        var fog=GameObject.CreatePrimitive(PrimitiveType.Quad);fog.name="Door Light Haze";Destroy(fog.GetComponent<Collider>());fog.transform.SetParent(transform,false);
        fog.transform.position=closedBounds.center+Vector3.left*1.1f;fog.transform.rotation=Quaternion.Euler(0,90,0);fog.transform.localScale=new Vector3(closedBounds.size.z,closedBounds.size.y,1);haze=fog.GetComponent<Renderer>();haze.sharedMaterial=mistMaterial;haze.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        beam=MakeLight("White light through door crack",LightType.Spot,new Vector3(closedBounds.center.x+12,closedBounds.center.y+35,closedBounds.center.z));
        beam.transform.LookAt(new Vector3(closedBounds.center.x-95,closedBounds.min.y+10,closedBounds.center.z));beam.range=290;beam.spotAngle=85;beam.innerSpotAngle=40;beam.shadows=LightShadows.Soft;
        fill=MakeLight("Door frame white bounce",LightType.Point,closedBounds.center+Vector3.left*12);fill.range=145;
    }
    Light MakeLight(string label,LightType type,Vector3 position)
    {
        var owner=new GameObject(label);owner.transform.SetParent(transform,false);owner.transform.position=position;
        var light=owner.AddComponent<Light>();light.type=type;light.color=new Color(.95f,.97f,1);light.renderMode=LightRenderMode.ForcePixel;light.cullingMask=~((1<<30)|(1<<31));light.intensity=0;return light;
    }
    void Update()
    {
        if(!installation)installation=FindObjectOfType<EarSafeInstallation>();
        if(!SequenceStarted)TryBegin();
        if(!IsOpen||!Camera.main)return;
        var position=player?player.transform.position:Camera.main.transform.position;
        float side=(position.x-closedBounds.center.x)*originalSide;
        bool within=Mathf.Abs(position.z-closedBounds.center.z)<closedBounds.extents.z-5;
        float approach=within?1-Mathf.Clamp01(Mathf.Max(0,side)/65):0;
        // No scene load: the player remains in control and can back out of the light.
        AudioListener.volume=initialListenerVolume*Mathf.Lerp(1,.12f,approach);
        float fade=within?Mathf.Clamp01((-side+1)/18):0;
        whiteout=Mathf.MoveTowards(whiteout,fade,Time.unscaledDeltaTime*.8f);
    }
    public bool TryBegin()
    {
        if(SequenceStarted||!leaf||!RequirementsMet())return false;
        SequenceStarted=true;StartCoroutine(OpenSequence());return true;
    }
    public bool RequirementsMet()
    {
        if(!installation)installation=FindObjectOfType<EarSafeInstallation>();
        if(!installation||!installation.Installed)return false;
        if(!repairState)repairState=FindObjectOfType<Scene2RepairCelebration>();
        return repairState&&repairState.AllPartsInstalled();
    }
    IEnumerator OpenSequence()
    {
        if(installation)mechanism.transform.position=installation.transform.position;
        Scene2AudioPlayback.PlayOneShot(mechanism,click,.6f);
        yield return new WaitForSecondsRealtime(.5f);
        Scene2AudioPlayback.PlayOneShot(mechanism,motor,.7f);
        yield return new WaitForSecondsRealtime(.85f);
        mechanism.transform.position=closedBounds.center;Scene2AudioPlayback.PlayOneShot(mechanism,unlock,.8f);
        yield return new WaitForSecondsRealtime(.18f);
        yield return AnimateDoor(0,.085f,.65f);
        yield return new WaitForSecondsRealtime(.3f);
        yield return AnimateDoor(.085f,1,2.4f);
        IsOpen=true;
    }
    IEnumerator AnimateDoor(float from,float to,float duration)
    {
        for(float elapsed=0;elapsed<duration;elapsed+=Time.unscaledDeltaTime){ApplyOpening(Mathf.Lerp(from,to,Mathf.SmoothStep(0,1,elapsed/duration)));yield return null;}
        ApplyOpening(to);
    }
    void ApplyOpening(float value)
    {
        Opening=value;if(hinge)hinge.localRotation=Quaternion.AngleAxis(value*65,Vector3.up);
        float light=Mathf.SmoothStep(0,1,Mathf.Clamp01(value*2));
        if(backdrop)backdrop.enabled=value>.002f;if(haze)haze.enabled=value>.002f;
        if(whiteMaterial)whiteMaterial.SetFloat("_Opacity",light);
        if(mistMaterial)mistMaterial.SetFloat("_Opacity",light*.12f);
        if(beam){beam.intensity=light*11;beam.spotAngle=Mathf.Lerp(14,85,Mathf.Sqrt(value));}
        if(fill)fill.intensity=light*3.2f;
    }
    void OnGUI()
    {
        if(whiteout<=.001f)return;int depth=GUI.depth;var color=GUI.color;GUI.depth=-1000;GUI.color=new Color(1,1,1,whiteout);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=color;GUI.depth=depth;
    }
    static AudioClip MechanicalClip(string name,float duration,int kind)
    {
        const int rate=44100;var samples=new float[Mathf.CeilToInt(duration*rate)];var random=new System.Random(404+kind);float smooth=0;
        for(int i=0;i<samples.Length;i++)
        {
            float t=i/(float)rate;float noise=(float)(random.NextDouble()*2-1);smooth=Mathf.Lerp(smooth,noise,.18f);
            float envelope=kind==1?Mathf.Sin(Mathf.PI*t/duration):Mathf.Exp(-t*32);
            float tone=kind==1?(Mathf.Sin(t*2*Mathf.PI*92)+.35f*Mathf.Sin(t*2*Mathf.PI*184))*.17f:Mathf.Sin(t*2*Mathf.PI*(kind==0?1350:720))*.35f;
            samples[i]=(tone+smooth*(kind==1?.22f:.7f))*envelope;
        }
        var clip=AudioClip.Create(name,samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
    }
    void OnDestroy()
    {
        if(IsOpen)AudioListener.volume=initialListenerVolume;
        if(whiteMaterial)Destroy(whiteMaterial);if(mistMaterial)Destroy(mistMaterial);if(portalMesh)Destroy(portalMesh);
        if(click)Destroy(click);if(motor)Destroy(motor);if(unlock)Destroy(unlock);
        foreach(var renderer in wallRenderers)if(renderer)renderer.enabled=true;
        foreach(var collider in wallColliders)if(collider)collider.enabled=true;
        foreach(var mesh in apertureMeshes)if(mesh)Destroy(mesh);
    }
}
