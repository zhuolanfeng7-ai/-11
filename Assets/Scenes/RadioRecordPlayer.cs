using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-70)]
[DisallowMultipleComponent]
public sealed class RadioRecordPlayer : MonoBehaviour
{
    [SerializeField] private Transform recordSource;
    [SerializeField] private AudioClip music;
    [SerializeField] private Vector3 platterCenter=new Vector3(19.1f,272.2f,-186.1f);
    [SerializeField] private float interactionDistance=100;
    [SerializeField,Min(.1f)] private float placementDuration=1.8f;
    private bool placing;
    private Vector3 settledPosition;
    private Quaternion settledRotation;
    private AudioSource output;
    private Transform placedRecord;
    private bool recordingPaused;
    private Transform missingRecordHint;
    private Material hintMaterial,lineMaterial;
    private Coroutine hintRoutine;
    private bool missingRecordHintStarted;
    public bool IsMissingRecordHintVisible=>missingRecordHint&&missingRecordHint.gameObject.activeSelf;
    public bool IsRecordingPaused => recordingPaused;
    public const string RecordingResource = "RadioAudio/FamilyRecording";
    public bool HasPlacedRecord => placedRecord && !placing;
    public bool IsPlacing => placing;
    public AudioSource Output => output;
    public Transform PlacedRecord => placedRecord;
    public Transform RecordSource => recordSource;
    public AudioClip Music => music;
    public void ShowMissingRecordHint()
    {
        if(placedRecord||placing||missingRecordHintStarted||!recordSource)return;
        if(!missingRecordHint)BuildMissingRecordHint();
        if(missingRecordHint)
        {
            missingRecordHintStarted=true;
            hintRoutine=StartCoroutine(MissingRecordReminder());
        }
    }
    private IEnumerator MissingRecordReminder()
    {
        float repeatAt=Time.time+5f;
        yield return FlashMissingRecord();
        while(Time.time<repeatAt&&!placing&&!placedRecord)yield return null;
        if(!placing&&!placedRecord)yield return FlashMissingRecord();
        hintRoutine=null;
    }
    private void BuildMissingRecordHint()
    {
        var source=recordSource.GetComponent<MeshFilter>();if(!source||!source.sharedMesh)return;
        var shader=Resources.Load<Shader>("OpeningEffects/RecordPlacementHint");if(!shader)return;
        hintMaterial=new Material(shader);hintMaterial.SetColor("_Tint",new Color(.7f,.55f,1,0));
        var go=new GameObject("Missing Record Single Fade Hint");missingRecordHint=go.transform;
        go.transform.rotation=Quaternion.FromToRotation(new Vector3(-.0001865f,.9999994f,-.001033f),Vector3.up)*recordSource.rotation;
        go.transform.localScale=recordSource.lossyScale;
        go.AddComponent<MeshFilter>().sharedMesh=source.sharedMesh;
        var renderer=go.AddComponent<MeshRenderer>();var mats=new Material[source.sharedMesh.subMeshCount];for(int i=0;i<mats.Length;i++)mats[i]=hintMaterial;renderer.sharedMaterials=mats;
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        var b=renderer.bounds;go.transform.position=new Vector3(platterCenter.x-b.center.x,platterCenter.y+.35f-b.min.y,platterCenter.z-b.center.z);
        b=renderer.bounds;lineMaterial=new Material(Shader.Find("Sprites/Default"));
        for(int ring=0;ring<2;ring++)
        {
            var circle=new GameObject("Record Outline");circle.transform.SetParent(go.transform,false);var line=circle.AddComponent<LineRenderer>();
            line.useWorldSpace=true;line.loop=true;line.positionCount=96;line.sharedMaterial=lineMaterial;
            line.startWidth=line.endWidth=Mathf.Max(.12f,b.size.x*.008f);
            float radius=Mathf.Max(b.extents.x,b.extents.z)*(ring==0?.97f:.13f);
            for(int i=0;i<96;i++){float a=i*Mathf.PI*2/96;line.SetPosition(i,new Vector3(platterCenter.x+Mathf.Cos(a)*radius,b.max.y+.05f,platterCenter.z+Mathf.Sin(a)*radius));}
        }
        go.SetActive(false);
    }
    private IEnumerator FlashMissingRecord()
    {
        missingRecordHint.gameObject.SetActive(true);
        float elapsed=0;while(elapsed<.5f&&!placing&&!placedRecord)
        {
            float pulse=Mathf.Pow(Mathf.Sin(elapsed/.5f*Mathf.PI),2);
            hintMaterial.SetColor("_Tint",new Color(.7f,.55f,1,pulse*.16f));
            foreach(var line in missingRecordHint.GetComponentsInChildren<LineRenderer>())line.startColor=line.endColor=new Color(.8f,.7f,1,pulse*.85f);
            elapsed+=Time.deltaTime;yield return null;
        }
        missingRecordHint.gameObject.SetActive(false);
    }
    private IEnumerator LightKnobsInOrder()
    {
        foreach(string name in new[]{"botton1","botton2","botton3"})
        {
            var go=GameObject.Find(name);var knob=go?go.GetComponent<RadioQuarterTurn>():null;if(knob)knob.ShowRecordReady();
            yield return new WaitForSeconds(.3f);
        }
    }
    public void Configure(Transform record,AudioClip clip){recordSource=record;music=Resources.Load<AudioClip>(RecordingResource);}
    private void Awake()
    {
        music=Resources.Load<AudioClip>(RecordingResource);
        output=gameObject.AddComponent<AudioSource>();output.playOnAwake=false;output.loop=false;output.spatialBlend=0;output.dopplerLevel=0;output.volume=.85f;output.clip=music;
    }
    private void OnEnable(){Scene2AudioPlayback.SoundStarting-=PauseForOtherSound;Scene2AudioPlayback.SoundStarting+=PauseForOtherSound;}
    private void PauseForOtherSound(AudioSource source)
    {
        if(source==output || !output || !output.isPlaying)return;
        // The three tuning knobs belong to this playback puzzle. Their chimes
        // accompany the record; other object sounds still interrupt it.
        var knob=source?source.GetComponent<RadioQuarterTurn>():null;
        if(knob&&knob.TurnAudio==source)return;
        output.Pause();recordingPaused=true;
    }
    private void Update()
    {
        if(!Application.isFocused || EvidenceInspect.BlocksWorldInput || !Input.GetMouseButtonDown(0))return;
        var camera=Camera.main;if(!camera)return;
        TryInteract(camera,Cursor.lockState==CursorLockMode.Locked?camera.pixelRect.center:(Vector2)Input.mousePosition);
    }
    public bool TryInteract(Camera camera,Vector2 point)
    {
        if(!camera || !camera.pixelRect.Contains(point) || EvidenceInspect.BlocksWorldInput)return false;
        var target=GetComponent<Collider>();if(!target || !target.enabled || Vector3.Distance(camera.transform.position,target.bounds.ClosestPoint(camera.transform.position))>interactionDistance)return false;
        var ray=camera.ScreenPointToRay(point);ray.origin=camera.transform.position;Physics.SyncTransforms();
        var hits=Physics.RaycastAll(ray,camera.farClipPlane,~0,QueryTriggerInteraction.Ignore);System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
        foreach(var hit in hits){if(hit.transform.GetComponentInParent<FirstPersonPlayer>())continue;return hit.transform==transform && TryPlaceRecord();}return false;
    }
    public bool TryPlaceRecord()
    {
        if(placing)return false;
        if(placedRecord)
        {
            if(!recordingPaused || !output)return false;
            Scene2AudioPlayback.Resume(output);recordingPaused=false;return true;
        }
        var inventory=FindObjectOfType<ImportantItemCollectionUI>();
        if(!inventory || !inventory.HasCp1 || !recordSource)return false;
        var source=recordSource.GetComponent<MeshFilter>();var original=recordSource.GetComponent<MeshRenderer>();
        if(!source || !source.sharedMesh || !original)return false;
        var record=new GameObject("cp1 On Turntable");
        record.transform.SetPositionAndRotation(Vector3.zero,Quaternion.FromToRotation(new Vector3(-.0001865f,.9999994f,-.001033f),Vector3.up)*recordSource.rotation);
        record.transform.localScale=recordSource.lossyScale;
        record.AddComponent<MeshFilter>().sharedMesh=source.sharedMesh;
        var renderer=record.AddComponent<MeshRenderer>();renderer.sharedMaterials=original.sharedMaterials;
        var b=renderer.bounds;
        record.transform.position=new Vector3(platterCenter.x-b.center.x,platterCenter.y+.04f-b.min.y,platterCenter.z-b.center.z);
        if(!inventory.ConsumeCp1()){Destroy(record);return false;}
        placedRecord=record.transform;settledPosition=placedRecord.position;settledRotation=placedRecord.rotation;
        if(hintRoutine!=null){StopCoroutine(hintRoutine);hintRoutine=null;}
        if(missingRecordHint)missingRecordHint.gameObject.SetActive(false);
        placing=true;StartCoroutine(PlaceRecord());return true;
    }
    private IEnumerator PlaceRecord()
    {
        Vector3 start=settledPosition+Vector3.up*8f+Vector3.forward*14f;
        Vector3 above=settledPosition+Vector3.up*4f;
        Quaternion tilted=Quaternion.AngleAxis(-12f,Vector3.right)*settledRotation;
        float elapsed=0;
        placedRecord.SetPositionAndRotation(start,tilted);
        while(elapsed<placementDuration)
        {
            elapsed+=Time.deltaTime;
            float t=Mathf.Clamp01(elapsed/placementDuration);
            if(t<.65f)
            {
                float approach=Mathf.SmoothStep(0,1,t/.65f);
                placedRecord.SetPositionAndRotation(Vector3.Lerp(start,above,approach),Quaternion.Slerp(tilted,settledRotation,approach));
            }
            else
            {
                float lower=Mathf.SmoothStep(0,1,(t-.65f)/.35f);
                placedRecord.SetPositionAndRotation(Vector3.Lerp(above,settledPosition,lower),settledRotation);
            }
            yield return null;
        }
        placedRecord.SetPositionAndRotation(settledPosition,settledRotation);
        placing=false;recordingPaused=false;output.clip=music;Scene2AudioPlayback.Play(output);
        StartCoroutine(LightKnobsInOrder());
    }
    private void LateUpdate()
    {
        if(HasPlacedRecord&&!recordingPaused&&output&&output.isPlaying)
            placedRecord.RotateAround(platterCenter,Vector3.up,45f*Time.deltaTime);
    }
    private void OnDisable()
    {
        Scene2AudioPlayback.SoundStarting-=PauseForOtherSound;
        StopAllCoroutines();
        hintRoutine=null;if(missingRecordHint)missingRecordHint.gameObject.SetActive(false);
        if(placing && placedRecord)placedRecord.SetPositionAndRotation(settledPosition,settledRotation);
        placing=false;recordingPaused=false;if(output)output.Stop();
    }
    private void OnDestroy(){if(placedRecord)Destroy(placedRecord.gameObject);if(missingRecordHint)Destroy(missingRecordHint.gameObject);if(hintMaterial)Destroy(hintMaterial);if(lineMaterial)Destroy(lineMaterial);}
}
