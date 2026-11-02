using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[DefaultExecutionOrder(-80)]
[DisallowMultipleComponent]
public sealed class MusicBoxCrank : MonoBehaviour
{
    [SerializeField] private Transform connection;
    [SerializeField,Min(.1f)] private float turnDuration=2.5f;
    [SerializeField] private AudioSource audioOutput;
    [SerializeField] private AudioClip[] turnSounds=new AudioClip[4];
    [SerializeField] private AudioClip completeMelody;
    [SerializeField] private UnityEvent onFourthTurnCompleted=new UnityEvent();
    [SerializeField] private MusicBoxRewardLift unlockReward;
    public MusicBoxRewardLift UnlockReward => unlockReward;
    public void ConfigureReward(MusicBoxRewardLift reward){unlockReward=reward;}
    [SerializeField] private Transform linkedDrum;
    [SerializeField] private Vector3 drumAxisLocal=Vector3.forward;
    [SerializeField] private Vector3 drumCenterLocal;
    private bool drumInitialized;
    private Quaternion drumRest;
    private Vector3 drumCenter,drumOffset;
    public Transform LinkedDrum => linkedDrum;
    public Vector3 DrumAxisWorld => drumRest*drumAxisLocal;
    public Vector3 DrumCenter => drumCenter;
    public void ConfigureDrum(Transform drum,Vector3 axis){
        if(Application.isPlaying&&drumInitialized&&linkedDrum==drum)return;
        linkedDrum=drum;drumAxisLocal=drum.InverseTransformDirection(axis.normalized);
        drumCenterLocal=drum.InverseTransformPoint(drum.GetComponent<Renderer>().bounds.center);
        if(Application.isPlaying)InitializeDrum();
    }
    private void InitializeDrum(){
        if(drumInitialized||!linkedDrum)return;
        drumRest=linkedDrum.rotation;drumCenter=linkedDrum.TransformPoint(drumCenterLocal);drumOffset=linkedDrum.position-drumCenter;drumInitialized=true;
    }
    [System.Serializable] private sealed class GearLink
    {
        public Transform part;public Vector3 axisLocal,centerLocal;public float ratio=1;
        [System.NonSerialized] public bool initialized;
        [System.NonSerialized] public Quaternion rest;
        [System.NonSerialized] public Vector3 center,offset;
    }
    [SerializeField] private GearLink[] gears=new GearLink[0];
    public Transform Gear => gears!=null&&gears.Length>0?gears[0].part:null;
    public Transform Gear1 => gears!=null&&gears.Length>1?gears[1].part:null;
    public void ConfigureGears(Transform gear,Transform gear1)
    {
        if(Application.isPlaying&&Gear==gear&&Gear1==gear1)return;
        Transform[] parts={gear,gear1};Vector3[] axes={new Vector3(.59128212f,.03966188f,-.80548892f),new Vector3(.5938878f,-.02221773f,-.80424104f)};
        gears=new GearLink[2];for(int i=0;i<2;i++)gears[i]=new GearLink{part=parts[i],axisLocal=parts[i].InverseTransformDirection(axes[i].normalized),centerLocal=parts[i].InverseTransformPoint(parts[i].GetComponent<Renderer>().bounds.center),ratio=i==0?1f:-2f};
    }
    private void ApplyGears(float angle)
    {
        if(gears==null)return;
        foreach(var gear in gears){
            if(gear==null||!gear.part)continue;
            if(!gear.initialized){gear.rest=gear.part.rotation;gear.center=gear.part.TransformPoint(gear.centerLocal);gear.offset=gear.part.position-gear.center;gear.initialized=true;}
            var delta=Quaternion.AngleAxis(angle*gear.ratio,gear.rest*gear.axisLocal);
            gear.part.SetPositionAndRotation(gear.center+delta*gear.offset,delta*gear.rest);
        }
    }
    private int completedTurns;
    public int CompletedTurns => completedTurns;
    public bool IsUnlocked => completedTurns>=4;
    public AudioSource AudioOutput => audioOutput;
    public bool IsBusy => turning || (audioOutput && audioOutput.isPlaying);
    public void ConfigureAudio(AudioSource output,AudioClip[] sounds,AudioClip complete){audioOutput=output;turnSounds=sounds;completeMelody=complete;}
    private Quaternion connectionRest,handleRest;
    private Vector3 handleOffset;
    private bool initialized,turning;
    public Transform Connection => connection;
    public bool IsTurning => turning;
    public void Configure(Transform pivot){connection=pivot;if(Application.isPlaying&&!initialized)Initialize();}
    private void Awake(){Initialize();}
    private void Initialize(){if(initialized||!connection)return;connectionRest=connection.localRotation;handleOffset=transform.position-connection.position;handleRest=transform.rotation;initialized=true;InitializeDrum();}
    private void Update(){
        if(IsBusy||EvidenceInspect.BlocksWorldInput||!Application.isFocused||!Input.GetMouseButtonDown(0))return;
        var camera=Camera.main;if(!camera)return;
        Vector2 pointer=Cursor.lockState==CursorLockMode.Locked?camera.pixelRect.center:(Vector2)Input.mousePosition;
        var hover=GetComponent<HoverScanHighlight>();
        if(hover&&hover.RefreshDeviceHover(camera,pointer))TryTurn();
    }
    public bool TryTurn(){
        Initialize();if(!initialized||IsBusy)return false;
        turning=true;
        if(audioOutput){
            AudioClip clip=completedTurns<4 && turnSounds!=null && completedTurns<turnSounds.Length?turnSounds[completedTurns]:completeMelody;
            if(clip){audioOutput.clip=clip;Scene2AudioPlayback.Play(audioOutput);}
        }
        StartCoroutine(Turn());return true;
    }
    public void SetTurnAngle(float degrees){
        Initialize();if(!initialized)return;
        connection.localRotation=connectionRest*Quaternion.AngleAxis(degrees,Vector3.forward);
        var restWorld=connection.parent?connection.parent.rotation*connectionRest:connectionRest;
        var delta=Quaternion.AngleAxis(degrees,restWorld*Vector3.forward);
        transform.SetPositionAndRotation(connection.position+delta*handleOffset,delta*handleRest);
        ApplyGears(degrees);
        if(linkedDrum){
            InitializeDrum();
            var drumDelta=Quaternion.AngleAxis(degrees,drumRest*drumAxisLocal);
            linkedDrum.SetPositionAndRotation(drumCenter+drumDelta*drumOffset,drumDelta*drumRest);
        }
    }
    private IEnumerator Turn(){float time=0;while(time<turnDuration){time+=Time.deltaTime;SetTurnAngle(Mathf.SmoothStep(0,360,Mathf.Clamp01(time/turnDuration)));yield return null;}SetTurnAngle(0);completedTurns++;turning=false;if(completedTurns==4){if(unlockReward)unlockReward.Raise();onFourthTurnCompleted.Invoke();}}
    private void OnDisable(){StopAllCoroutines();if(audioOutput)audioOutput.Stop();if(initialized)SetTurnAngle(0);turning=false;}
}
