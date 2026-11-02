using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public sealed class Scene2WorldIntroduction : MonoBehaviour
{
    public const string Title="UNIT-001 · BODY UPGRADE COMPLETE";
    public const string Body="Vision restored.\nCleaning unit upgraded to an upright operational unit.\n\nYou can finally stand and see this room clearly.\nEverything here was bought with a part of yourself.";
    public const string IdentityTitle="IDENTITY FILE RESTORED";
    public const string IdentityBody="You sold or leased your five senses to make your first fortune.\nThen you founded a company that bought other people's senses.\n\nThe company grew.\nYou became known as “the person who helped ordinary people turn themselves into wealth.”";
    public const string InquiryTitle="SYSTEM INQUIRY";
    public const string InquiryBody="Last time, the system asked:\nWould you give up your wealth and status to become ordinary?\n\nYou did not answer.\nYou simply began searching for your eyes.\nNow, you want to hear.";
    public const string HearingTitle="HEARING RESTORATION PROTOCOL";
    public const string HearingBody="The broken tape recorder holds a recording connected to you.\n\n<b>Find the four parts, repair the tape recorder, and play the full recording.</b>\n\nOnce, every word you spoke became an order.\nSomewhere in the dark, a pale light still waits for a voice.";
    public const string VoiceTraceTitle="VOICE TRACE";
    public const string VoiceTraceBody="Chairman, beneath the spotlight still stands the thing that once carried your every word into everyone's ears.\n\nSomething is hidden inside.";
    public const string ClockworkTitle="CLOCKWORK TRACE";
    public const string ClockworkBody="Chairman, your income no longer depends on the labour of your hands.\nBut the sound within that clockwork mechanism still demands your own effort.\n\nHidden inside is a part needed for the repair.";
    public const string CollectionTitle="COLLECTION TRACE";
    public const string CollectionBody="Chairman, the open horn is still there, but its centre is empty.\n\nYour collection has always been kept in cabinets, each piece accompanied by instructions—so its buyer would know how to use it.\n\nThis time, the instructions may be worth reading yourself.";
    public const string PianoTitle="LOCKED ARCHIVE";
    public const string PianoBody="Chairman, the lock beneath the piano keys is still closed. There may be something you need inside.";
    public const string BellsTitle="MISSING RESPONSE";
    public const string BellsBody="Chairman, you once counted one less sense as one more source of income.\nStrike the drum and see how the bells respond.\n\nEach time, one is left out. You once profited from such losses. Now you must make up for one to claim the final part.";
    private readonly List<Transform> bellTargets=new List<Transform>();
    private bool bellsShown;
    private Transform pianoTarget;
    private bool pianoShown;
    private Transform radioTarget;
    private bool collectionShown;
    [SerializeField,Min(1f)] private float voiceTraceDistance=90f;
    private Transform voiceTraceTarget;
    private bool voiceTraceShown;
    private readonly List<Transform> clockworkTargets=new List<Transform>();
    private bool clockworkShown;
    private static Scene2WorldIntroduction current;
    private static int consumedFrame=-1;
    private bool open=true,closing;
    private bool openingFinished,hearingShown;
    private float alpha=1;
    private int page;
    private GUIStyle titleStyle,bodyStyle,promptStyle;
    public bool IsOpen=>open;
    public int Page=>page;
    public string CurrentTitle=>page==0?Title:page==1?IdentityTitle:page==2?InquiryTitle:page==3?HearingTitle:page==4?VoiceTraceTitle:page==5?ClockworkTitle:page==6?CollectionTitle:page==7?PianoTitle:BellsTitle;
    public string CurrentBody=>page==0?Body:page==1?IdentityBody:page==2?InquiryBody:page==3?HearingBody:page==4?VoiceTraceBody:page==5?ClockworkBody:page==6?CollectionBody:page==7?PianoBody:BellsBody;
    public bool LayoutFits{get;private set;}
    public static bool BlocksWorldInput=>(current&&current.open&&current.page<3)||consumedFrame==Time.frameCount;

    public static bool NotifyPlayerWalking()
    {
        if(!current||!current.isActiveAndEnabled||!current.openingFinished||current.hearingShown)return false;
        if(EvidenceInspect.BlocksWorldInput)return false;
        current.hearingShown=true;current.page=3;current.open=true;current.closing=false;current.alpha=1;
        return true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState(){current=null;consumedFrame=-1;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded-=Bind;SceneManager.sceneLoaded+=Bind;
        Bind(SceneManager.GetActiveScene(),LoadSceneMode.Single);
    }
    private static void Bind(Scene scene,LoadSceneMode mode)
    {
        if(!scene.IsValid()||!string.Equals(scene.name.Replace(" ",""),"scene2",StringComparison.OrdinalIgnoreCase))return;
        foreach(var root in scene.GetRootGameObjects())if(root.GetComponentInChildren<Scene2WorldIntroduction>(true))return;
        var owner=new GameObject("Scene2 World Introduction");SceneManager.MoveGameObjectToScene(owner,scene);
        owner.AddComponent<Scene2WorldIntroduction>();
    }
    private void Awake(){current=this;}
    private void Start()
    {
        foreach(var root in gameObject.scene.GetRootGameObjects())
        foreach(var target in root.GetComponentsInChildren<Transform>(true))
        {
            string name=target.name.Trim();
            if(string.Equals(name,"zhuzi2",StringComparison.OrdinalIgnoreCase))voiceTraceTarget=target;
            if(string.Equals(name,"radio",StringComparison.OrdinalIgnoreCase))radioTarget=target;
            if(string.Equals(name,"piano",StringComparison.OrdinalIgnoreCase)
                ||string.Equals(name,"pinao",StringComparison.OrdinalIgnoreCase))pianoTarget=target;
            if(string.Equals(name,"hezi",StringComparison.OrdinalIgnoreCase)
                ||string.Equals(name,"shoubing",StringComparison.OrdinalIgnoreCase)
                ||string.Equals(name,"zhuantong",StringComparison.OrdinalIgnoreCase))clockworkTargets.Add(target);
            if(string.Equals(name,"drum",StringComparison.OrdinalIgnoreCase)
                ||string.Equals(name,"drump",StringComparison.OrdinalIgnoreCase)
                ||string.Equals(name,"bling1",StringComparison.OrdinalIgnoreCase)
                ||string.Equals(name,"bling2",StringComparison.OrdinalIgnoreCase)
                ||string.Equals(name,"bling3",StringComparison.OrdinalIgnoreCase))bellTargets.Add(target);
        }
    }
    private void Update()
    {
        if(!Application.isFocused)return;
        if(open&&!closing&&Input.GetMouseButtonDown(0)){Advance();return;}
        if(!openingFinished||!hearingShown||closing||EvidenceInspect.BlocksWorldInput||(open&&page>=4))return;
        var camera=Camera.main;if(!camera)return;
        if(!voiceTraceShown&&voiceTraceTarget)
        {
            var device=voiceTraceTarget.GetComponent<TelescopingDevice>();
            if(!(device&&(device.IsRetracting||device.IsRetracted))&&IsNear(voiceTraceTarget,camera.transform.position))
            {voiceTraceShown=true;page=4;open=true;alpha=1;return;}
        }
        if(!clockworkShown)
            foreach(var target in clockworkTargets)
                if(IsNear(target,camera.transform.position))
                {clockworkShown=true;page=5;open=true;alpha=1;return;}
        if(!collectionShown&&IsNear(radioTarget,camera.transform.position))
        {collectionShown=true;page=6;open=true;alpha=1;return;}
        if(!pianoShown&&IsNear(pianoTarget,camera.transform.position))
        {
            var inventory=FindObjectOfType<ImportantItemCollectionUI>();
            if(inventory&&inventory.HasScene2Key)
            {pianoShown=true;page=7;open=true;alpha=1;return;}
        }
        if(!bellsShown)
            foreach(var target in bellTargets)
                if(IsNear(target,camera.transform.position))
                {bellsShown=true;page=8;open=true;alpha=1;return;}
    }
    private bool IsNear(Transform target,Vector3 position)
    {
        if(!target||!target.gameObject.activeInHierarchy)return false;
        var collider=target.GetComponent<Collider>();var renderer=target.GetComponent<Renderer>();
        Vector3 nearest=collider&&collider.enabled?collider.bounds.ClosestPoint(position)
            :renderer?renderer.bounds.ClosestPoint(position):target.position;
        return Vector3.Distance(position,nearest)<=voiceTraceDistance;
    }
    public void Advance()
    {
        if(!open||closing)return;
        if(page<3)consumedFrame=Time.frameCount;
        if(page<2){page++;return;}
        Dismiss();
    }
    public void Dismiss()
    {
        if(!open||closing)return;
        closing=true;
        // Gameplay lore is nonmodal: clicking a world object also closes the
        // card, but must still reach that object's interaction in this frame.
        Vector2 pointer=new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y);
        if(page<3||WorldLoreTerminalLayout.Frame(Screen.height).Contains(pointer))consumedFrame=Time.frameCount;
        StartCoroutine(FadeOut());
    }
    private IEnumerator FadeOut()
    {
        float began=Time.unscaledTime;
        while(Time.unscaledTime-began<.42f){alpha=1-Mathf.Clamp01((Time.unscaledTime-began)/.42f);yield return null;}
        alpha=0;open=false;closing=false;openingFinished=true;if(page<3)consumedFrame=Time.frameCount;
    }
    private void EnsureStyles()
    {
        if(titleStyle!=null)return;
        var label=Event.current!=null?GUI.skin.label:new GUIStyle{font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")};
        titleStyle=new GUIStyle(label){fontSize=19,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleLeft};
        titleStyle.normal.textColor=new Color(.66f,.78f,1);
        bodyStyle=new GUIStyle(label){fontSize=14,wordWrap=true,richText=true,alignment=TextAnchor.UpperLeft};
        bodyStyle.normal.textColor=new Color(.9f,.87f,.96f);
        promptStyle=new GUIStyle(label){fontSize=12,fontStyle=FontStyle.Italic,alignment=TextAnchor.MiddleLeft};
        promptStyle.normal.textColor=new Color(.36f,1,.68f);
    }
    public bool TextFitsAtHeight(int screenHeight)
    {
        EnsureStyles();
        float scale=WorldLoreTerminalLayout.Scale(screenHeight);
        float width=(WorldLoreTerminalLayout.Width-40)*scale;
        titleStyle.fontSize=19;
        while(titleStyle.fontSize>11&&titleStyle.CalcSize(new GUIContent(CurrentTitle)).x>width)titleStyle.fontSize--;
        bodyStyle.fontSize=14;
        float height=(WorldLoreTerminalLayout.Height-109)*scale;
        while(bodyStyle.fontSize>10&&bodyStyle.CalcHeight(new GUIContent(CurrentBody),width)>height)bodyStyle.fontSize--;
        return titleStyle.CalcSize(new GUIContent(CurrentTitle)).x<=width
            &&bodyStyle.CalcHeight(new GUIContent(CurrentBody),width)<=height;
    }
    private void OnGUI()
    {
        if(!open||alpha<=0)return;
        EnsureStyles();LayoutFits=TextFitsAtHeight(Screen.height);
        float scale=WorldLoreTerminalLayout.Scale(Screen.height);
        Rect box=WorldLoreTerminalLayout.Frame(Screen.height);
        Color old=GUI.color;int depth=GUI.depth;GUI.depth=-200;
        GUI.color=new Color(.15f,.075f,.25f,.93f*alpha);GUI.DrawTexture(box,Texture2D.whiteTexture);
        GUI.color=new Color(.08f,.82f,.42f,alpha);GUI.DrawTexture(new Rect(box.x,box.y,box.width,3*scale),Texture2D.whiteTexture);
        GUI.color=new Color(.25f,.62f,1,alpha);GUI.DrawTexture(new Rect(box.x+7*scale,box.y+8*scale,box.width-14*scale,2*scale),Texture2D.whiteTexture);
        GUI.color=new Color(1,1,1,alpha);
        GUI.Label(new Rect(box.x+20*scale,box.y+25*scale,box.width-40*scale,28*scale),CurrentTitle,titleStyle);
        GUI.Label(new Rect(box.x+20*scale,box.y+61*scale,box.width-40*scale,box.height-109*scale),CurrentBody,bodyStyle);
        GUI.Label(new Rect(box.x+20*scale,box.yMax-31*scale,box.width-40*scale,18*scale),page<3?"CLICK ANYWHERE TO CONTINUE":"CLICK ANYWHERE TO DISMISS",promptStyle);
        GUI.color=old;GUI.depth=depth;
    }
    private void OnDisable(){StopAllCoroutines();open=false;if(current==this)current=null;}
}
