using UnityEngine;

[DisallowMultipleComponent]
public sealed class BellMicroSwing : MonoBehaviour
{
    private MeshFilter filter;private Mesh original,animated;private Renderer originalRenderer;private GameObject dynamicVisual;
    public Mesh AnimatedMesh => animated;private Vector3[] restLocal,restWorld,restNormals,current,normals;
    private Bounds restBounds;private float sideLeft=1.35f,sideRight=1.35f;
    public Bounds RestBounds => restBounds;
    private float[] shellWeight,clapperWeight;private Vector3 anchor,shellPivot,clapperPivot,axis;
    private float elapsed;private float swingFrequency=1.65f;private float activeFrequency=1.65f;private int chime;private AudioSource source;private AudioClip sound;
    public bool IsSwinging {get;private set;}
    public int TriggerCount {get;private set;}
    public int ClapperVertexCount {get;private set;}
    public int ChainVertexCount {get;private set;}
    public float MaximumDisplacement {get;private set;}
    public void Configure(int number)
    {
        if(animated)return;swingFrequency=1.56f+number*.045f;var originalFilter=GetComponent<MeshFilter>();original=originalFilter.sharedMesh;originalRenderer=GetComponent<Renderer>();
        var template=Resources.Load<Mesh>("BellMeshes/bling"+number+"Swing");
        if(!template){Debug.LogError("Bell swing mesh missing: "+name,this);return;}
        animated=Instantiate(template);animated.name=name+" Runtime Swing";animated.MarkDynamic();
        dynamicVisual=new GameObject("Bell Dynamic Visual");dynamicVisual.transform.SetParent(transform,false);dynamicVisual.isStatic=false;
        filter=dynamicVisual.AddComponent<MeshFilter>();filter.sharedMesh=animated;var renderer=dynamicVisual.AddComponent<MeshRenderer>();renderer.sharedMaterials=originalRenderer.sharedMaterials;renderer.shadowCastingMode=originalRenderer.shadowCastingMode;renderer.receiveShadows=originalRenderer.receiveShadows;originalRenderer.enabled=false;
        restLocal=animated.vertices;restNormals=animated.normals;current=new Vector3[restLocal.Length];normals=new Vector3[restLocal.Length];restWorld=new Vector3[restLocal.Length];
        for(int i=0;i<restLocal.Length;i++)restWorld[i]=transform.TransformPoint(restLocal[i]);
        var bounds=new Bounds(restWorld[0],Vector3.zero);foreach(var p in restWorld)bounds.Encapsulate(p);
        restBounds=bounds;
        float height=bounds.size.y;float radius=Mathf.Max(bounds.extents.x,bounds.extents.z);
        anchor=restWorld[0];foreach(var p in restWorld)if(p.y>anchor.y)anchor=p;
        shellPivot=anchor-Vector3.up*(height*.40f);clapperPivot=anchor-Vector3.up*(height*.52f);
        axis=Vector3.forward; // The authored support bar runs along world Z; swing forward/back in world X.
        shellWeight=new float[restLocal.Length];clapperWeight=new float[restLocal.Length];
        for(int i=0;i<restLocal.Length;i++){
            var p=restWorld[i];float y=(p.y-bounds.min.y)/height;float radial=Vector2.Distance(new Vector2(p.x,p.z),new Vector2(bounds.center.x,bounds.center.z));
            shellWeight[i]=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1,.60f,y));
            clapperWeight[i]=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(radius*.15f,radius*.25f,radial)))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.38f,.51f,y)));
            if(clapperWeight[i]>.5f)ClapperVertexCount++;if(y>.60f)ChainVertexCount++;
        }
        sound=Resources.Load<AudioClip>("DrumBells/Bell"+number);source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=.7f;source.minDistance=35;source.maxDistance=400;source.rolloffMode=AudioRolloffMode.Linear;
    }
    public void ConfigureSideClearance(BellMicroSwing[] neighbors)
    {
        sideLeft=sideRight=1.35f;
        foreach(var other in neighbors){
            if(!other||other==this)continue;
            if(other.restBounds.center.z<restBounds.center.z){float gap=restBounds.min.z-other.restBounds.max.z;sideLeft=Mathf.Min(sideLeft,Mathf.Max(0,(gap-.08f)*.49f));}
            else {float gap=other.restBounds.min.z-restBounds.max.z;sideRight=Mathf.Min(sideRight,Mathf.Max(0,(gap-.08f)*.49f));}
        }
    }
    public void Excite(float delay=0f){if(!animated)return;StopSwing();activeFrequency=swingFrequency;IsSwinging=true;elapsed=-Mathf.Max(0,delay);chime=0;TriggerCount++;MaximumDisplacement=0;}
    public void ExciteSynchronized(){Excite();activeFrequency=1.65f;}
    public void StopSwing(){IsSwinging=false;if(source)source.Stop();if(animated&&restLocal!=null){animated.vertices=restLocal;if(restNormals.Length==restLocal.Length)animated.normals=restNormals;animated.RecalculateBounds();}}
    private void Update()
    {
        if(!IsSwinging)return;elapsed+=Time.deltaTime;if(elapsed<0)return;const float duration=2.8f;if(elapsed>=duration){StopSwing();return;}
        float fade=Mathf.Exp(-elapsed*1.15f)*Mathf.SmoothStep(0,1,elapsed/.06f)*(1-Mathf.SmoothStep(0,1,(elapsed-2.25f)/.55f));float wave=elapsed*2*Mathf.PI*activeFrequency;
        float a=4f*Mathf.Sin(wave)*fade,b=1.4f*Mathf.Sin(wave-.15f)*fade,c=5f*Mathf.Sin(wave*1.12f-.45f)*fade;
        // Forward rotation is around world Z, so it preserves the spacing along
        // the support bar. Add a bounded side motion only within each bell's gap.
        float sideWave=Mathf.Sin(wave*.73f)*fade;
        float side=sideWave*(sideWave<0?sideLeft:sideRight);
        var root=Quaternion.AngleAxis(a,axis);
        for(int i=0;i<restLocal.Length;i++){
            var shell=Quaternion.AngleAxis(b*shellWeight[i],axis);var clapper=Quaternion.AngleAxis(c*clapperWeight[i],axis);
            Vector3 p=clapperPivot+clapper*(restWorld[i]-clapperPivot);p=shellPivot+shell*(p-shellPivot);p=anchor+root*(p-anchor);
            float sideWeight=Mathf.SmoothStep(0,1,(anchor.y-restWorld[i].y)/Mathf.Max(.001f,restBounds.size.y*.4f));
            p.z+=side*sideWeight;
            current[i]=transform.InverseTransformPoint(p);MaximumDisplacement=Mathf.Max(MaximumDisplacement,Vector3.Distance(p,restWorld[i]));
            if(restNormals.Length==restLocal.Length)normals[i]=transform.InverseTransformDirection(root*shell*clapper*transform.TransformDirection(restNormals[i])).normalized;
        }
        animated.vertices=current;if(restNormals.Length==restLocal.Length)animated.normals=normals;animated.RecalculateBounds();
        float[] times={.08f,.39f,.72f,1.08f,1.48f};while(chime<times.Length&&elapsed>=times[chime]){if(sound)Scene2AudioPlayback.PlayOneShot(source,sound,.36f*Mathf.Pow(.59f,chime));chime++;}
    }
    private void OnDisable(){StopSwing();}
    private void OnDestroy(){if(originalRenderer)originalRenderer.enabled=true;if(dynamicVisual)Destroy(dynamicVisual);if(animated)Destroy(animated);}
}
