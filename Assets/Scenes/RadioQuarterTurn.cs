using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RadioQuarterTurn : MonoBehaviour
{
    [SerializeField] private Vector3 outwardAxis = Vector3.forward;
    [SerializeField] private float turnDuration = .6f;
    [SerializeField] private float interactionDistance = 100f;
    [SerializeField,Range(1,4)] private int targetStep=2;
    [SerializeField] private AudioClip turnSound;
    private AudioSource turnAudio;
    private int completedTurns;
    private bool turning;
    private Quaternion restRotation;
    private Vector3 restPosition, center;
    private Transform decoration, pointer;
    private Renderer[] lamps;
    private Material brass, dark, lit;
    private RadioRecordPlayer recordPlayer;
    private bool readyLamp;
    public int CompletedTurns => completedTurns;
    public int CurrentStep => completedTurns==0?0:((completedTurns-1)%4)+1;
    public int TargetStep => targetStep;
    public bool IsAtTarget => !turning && CurrentStep==targetStep;
    public AudioClip TurnSound => turnSound;
    public AudioSource TurnAudio => turnAudio;
    public void ConfigureTuning(int step,AudioClip sound){targetStep=Mathf.Clamp(step,1,4);turnSound=sound;}
    public bool IsTurning => turning;
    public Transform Decoration => decoration;
    public bool TryTurn()
    {
        if(turning)return false;
        if(!recordPlayer)recordPlayer=FindObjectOfType<RadioRecordPlayer>();
        if(recordPlayer&&!recordPlayer.HasPlacedRecord)recordPlayer.ShowMissingRecordHint();
        turning=true;if(turnAudio && turnSound)Scene2AudioPlayback.PlayOneShot(turnAudio,turnSound);StartCoroutine(Turn());return true;
    }
    private void Awake()
    {
        restRotation=transform.rotation;restPosition=transform.position;
        center=GetComponent<Renderer>().bounds.center;
        brass=MakeMaterial(new Color(.78f,.57f,.23f));
        dark=MakeMaterial(new Color(.18f,.12f,.25f));
        lit=MakeMaterial(new Color(.25f,1f,.55f));
        BuildScale();
        turnAudio=gameObject.AddComponent<AudioSource>();turnAudio.playOnAwake=false;turnAudio.loop=false;turnAudio.spatialBlend=.35f;turnAudio.minDistance=10;turnAudio.maxDistance=150;turnAudio.dopplerLevel=0;turnAudio.volume=.85f;
    }
    private Material MakeMaterial(Color color)
    {
        var material=new Material(Shader.Find("Unlit/Color"));material.color=color;return material;
    }
    private void BuildScale()
    {
        var axis=outwardAxis.normalized;
        var up=Vector3.ProjectOnPlane(Vector3.up,axis).normalized;
        var right=Vector3.Cross(axis,up).normalized;
        var bounds=GetComponent<Renderer>().bounds;
        float radius=bounds.extents.x;
        decoration=new GameObject(name+" Fixed Scale").transform;
        decoration.SetPositionAndRotation(center,Quaternion.identity);
        float front=bounds.extents.z+.08f;
        for(int i=0;i<16;i++)
        {
            float a=i*Mathf.PI*2/16;
            Vector3 direction=up*Mathf.Cos(a)+right*Mathf.Sin(a);
            Line("Tick "+i,center+axis*front+direction*radius*1.17f,center+axis*front+direction*radius*(i%4==0?1.48f:1.33f),radius*.045f,brass,decoration);
        }
        string[] labels={"0/4","1","2","3"};
        for(int i=0;i<4;i++)
        {
            float a=i*Mathf.PI*.5f;Vector3 direction=up*Mathf.Cos(a)+right*Mathf.Sin(a);
            var label=new GameObject("Scale "+labels[i]);label.transform.SetParent(decoration,true);
            label.transform.SetPositionAndRotation(center+axis*front+direction*radius*1.78f,Quaternion.LookRotation(-axis,up));
            var text=label.AddComponent<TextMesh>();text.text=labels[i];text.fontSize=48;text.characterSize=radius*.16f;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=brass.color;
        }
        pointer=new GameObject(name+" Rotating Pointer").transform;pointer.SetPositionAndRotation(center,Quaternion.identity);
        Line("Pointer",center+axis*front+up*radius*.25f,center+axis*front+up*radius*.83f,radius*.13f,dark,pointer);
        lamps=new Renderer[4];
        for(int i=0;i<4;i++)
        {
            var lamp=GameObject.CreatePrimitive(PrimitiveType.Sphere);lamp.name=name+" Progress "+(i+1);lamp.transform.SetParent(decoration,true);
            lamp.transform.position=center+right*((i-1.5f)*radius*.55f)+up*radius*4.1f+axis*(front+.06f);
            lamp.transform.localScale=Vector3.one*radius*.4f;
            Destroy(lamp.GetComponent<Collider>());lamps[i]=lamp.GetComponent<Renderer>();lamps[i].sharedMaterial=dark;
        }
    }
    private static void Line(string name,Vector3 a,Vector3 b,float width,Material material,Transform parent)
    {
        var go=new GameObject(name);go.transform.SetParent(parent,true);var line=go.AddComponent<LineRenderer>();line.useWorldSpace=false;
        line.sharedMaterial=material;line.positionCount=2;line.SetPosition(0,go.transform.InverseTransformPoint(a));line.SetPosition(1,go.transform.InverseTransformPoint(b));line.startWidth=line.endWidth=width;
    }
    private void Update()
    {
        if(!Application.isFocused || EvidenceInspect.BlocksWorldInput || !Input.GetMouseButtonDown(0))return;
        var camera=Camera.main;if(!camera)return;
        Vector2 point=Cursor.lockState==CursorLockMode.Locked?camera.pixelRect.center:(Vector2)Input.mousePosition;
        TryInteract(camera,point);
    }
    public bool TryInteract(Camera camera,Vector2 point)
    {
        if(!camera || !camera.pixelRect.Contains(point) || EvidenceInspect.BlocksWorldInput)return false;
        var target=GetComponent<Collider>();if(!target || !target.enabled)return false;
        if(Vector3.Distance(camera.transform.position,target.bounds.ClosestPoint(camera.transform.position))>interactionDistance)return false;
        var ray=camera.ScreenPointToRay(point);ray.origin=camera.transform.position;Physics.SyncTransforms();
        var hits=Physics.RaycastAll(ray,camera.farClipPlane,~0,QueryTriggerInteraction.Ignore);System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
        foreach(var hit in hits)
        {
            if(hit.transform.GetComponentInParent<FirstPersonPlayer>())continue;
            return hit.transform==transform && TryTurn();
        }
        return false;
    }
    private IEnumerator Turn()
    {
        float elapsed=0;float start=(completedTurns%4)*90f;
        while(elapsed<turnDuration){elapsed+=Time.deltaTime;ApplyAngle(start+90*Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/turnDuration)));yield return null;}
        completedTurns++;ApplyAngle((completedTurns%4)*90f);
        RefreshLamps();
        turning=false;
    }
    public void ShowRecordReady(){readyLamp=true;RefreshLamps();}
    private void RefreshLamps()
    {
        if(!recordPlayer)recordPlayer=FindObjectOfType<RadioRecordPlayer>();
        int count=readyLamp&&recordPlayer&&recordPlayer.HasPlacedRecord?Mathf.Max(1,CurrentStep):CurrentStep;
        for(int i=0;i<lamps.Length;i++)lamps[i].sharedMaterial=i<count?lit:dark;
    }
    private void ApplyAngle(float degrees)
    {
        // The front-facing scale and knob share the same clockwise direction.
        Quaternion delta=Quaternion.AngleAxis(degrees,outwardAxis.normalized);
        transform.SetPositionAndRotation(center+delta*(restPosition-center),delta*restRotation);
        pointer.rotation=delta;
    }
    private void OnDisable()
    {
        StopAllCoroutines();turning=false;
        if(pointer)ApplyAngle((completedTurns%4)*90f);
        if(decoration)decoration.gameObject.SetActive(false);if(pointer)pointer.gameObject.SetActive(false);
    }
    private void OnEnable(){if(decoration)decoration.gameObject.SetActive(true);if(pointer)pointer.gameObject.SetActive(true);}
    private void OnDestroy()
    {
        if(decoration)Destroy(decoration.gameObject);if(pointer)Destroy(pointer.gameObject);
        if(brass)Destroy(brass);if(dark)Destroy(dark);if(lit)Destroy(lit);
    }
}
