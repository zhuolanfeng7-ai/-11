using UnityEngine;

[DefaultExecutionOrder(-200)]
[DisallowMultipleComponent]
public sealed class EvidenceInspect : MonoBehaviour
{
    [SerializeField] private Texture evidenceTexture;
    [SerializeField, Min(.1f)] private float displaySeconds=3f;
    [SerializeField] private bool useHighlight=true;
    [SerializeField] private float clickDistance=160f;
    private static EvidenceInspect active;
    private static int consumedFrame=-1;
    private float openedAt;
    private int openedFrame;
    private FirstPersonPlayer player;
    private bool playerWasEnabled;
    private CursorLockMode previousCursor;
    public bool IsOpen => active==this;
    public static bool BlocksWorldInput => Scene2RepairCelebration.IsPlaying || active!=null || consumedFrame==Time.frameCount || Scene2PlateInstallation.BlocksWorldInput || Scene2WorldIntroduction.BlocksWorldInput || Scene2PickupPresentation.IsPlaying;
    public Texture DisplayTexture => evidenceTexture;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState(){active=null;consumedFrame=-1;}
    public void Configure(Texture texture){evidenceTexture=texture;}
    public void ConfigureWithoutHighlight(Texture texture,float range,float seconds=3f){evidenceTexture=texture;useHighlight=false;clickDistance=range;displaySeconds=Mathf.Max(.1f,seconds);}
    private void Awake(){if(!evidenceTexture){var renderer=GetComponent<Renderer>();if(renderer&&renderer.sharedMaterial)evidenceTexture=renderer.sharedMaterial.mainTexture;}}
    private void Update()
    {
        if(IsOpen){
            if(Time.unscaledTime-openedAt>=displaySeconds || (Time.frameCount>openedFrame&&Input.GetMouseButtonDown(0)))CloseInspection();
            return;
        }
        if(BlocksWorldInput||!Application.isFocused||!Input.GetMouseButtonDown(0))return;
        var camera=Camera.main;if(!camera)return;
        Vector2 pointer=Cursor.lockState==CursorLockMode.Locked?camera.pixelRect.center:(Vector2)Input.mousePosition;
        TryOpenAtScreenPosition(camera,pointer);
    }
    public bool TryOpenAtScreenPosition(Camera camera,Vector2 pointer)
    {
        if(BlocksWorldInput||!evidenceTexture)return false;
        if(useHighlight){var hover=GetComponent<HoverScanHighlight>();if(!hover||!hover.RefreshDeviceHover(camera,pointer))return false;}
        else{if(!camera||!camera.pixelRect.Contains(pointer))return false;Physics.SyncTransforms();var hits=Physics.RaycastAll(camera.ScreenPointToRay(pointer),clickDistance,~((1<<30)|(1<<31)),QueryTriggerInteraction.Ignore);System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));bool found=false;foreach(var hit in hits){if(hit.collider.GetComponentInParent<FirstPersonPlayer>())continue;if(hit.transform!=transform&&!hit.transform.IsChildOf(transform))return false;found=true;break;}if(!found)return false;}
        active=this;consumedFrame=Time.frameCount;openedFrame=Time.frameCount;openedAt=Time.unscaledTime;
        player=FindObjectOfType<FirstPersonPlayer>();playerWasEnabled=player&&player.enabled;
        if(playerWasEnabled)player.enabled=false;
        previousCursor=Cursor.lockState;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        return true;
    }
    public void CloseInspection()
    {
        if(!IsOpen)return;
        active=null;consumedFrame=Time.frameCount;
        if(player&&playerWasEnabled)player.enabled=true;
        Cursor.lockState=previousCursor;Cursor.visible=true;
    }
    private void OnGUI()
    {
        if(!IsOpen||!evidenceTexture)return;
        Color previous=GUI.color;GUI.color=new Color(0,0,0,.65f);
        GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);
        GUI.color=Color.white;
        float ratio=evidenceTexture.width/(float)evidenceTexture.height;
        float width=Mathf.Min(Screen.width*.75f,900f);float height=width/ratio;
        if(height>Screen.height*.8f){height=Screen.height*.8f;width=height*ratio;}
        GUI.DrawTexture(new Rect((Screen.width-width)*.5f,(Screen.height-height)*.5f,width,height),evidenceTexture,ScaleMode.ScaleToFit,true);
        GUI.color=previous;
        if(Event.current.type==EventType.MouseDown&&Event.current.button==0&&Time.frameCount>openedFrame){CloseInspection();Event.current.Use();}
    }
    private void OnDisable(){CloseInspection();}
}
