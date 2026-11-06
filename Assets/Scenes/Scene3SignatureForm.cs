using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-310)]
public sealed class Scene3SignatureForm : MonoBehaviour
{
    public const string SignerName="UNIT-001";
    static Scene3SignatureForm active;
    static int consumedFrame=-1;
    public static bool BlocksWorldInput=>active!=null||consumedFrame==Time.frameCount;
    // Each new game run gets a fresh decision, regardless of a previous saved signature.
    static bool signedThisRun;
    public static bool IsSigned=>signedThisRun;
    Texture document;
    Texture2D signature;
    bool signing,playerWasEnabled,previouslySigned;
    float signedAt, signElapsed; bool signatureRendered;
    string error;
    FirstPersonPlayer player;
    CursorLockMode oldLock;
    bool oldVisible;
    GUIStyle heading,button,hint; int openedFrame;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState(){active=null;consumedFrame=-1;signedThisRun=false;}
    public static void Show(Scene scene, bool preview = false)
    {
        if(active)return;
        Texture texture=null;
        foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))
            if(t.name.Equals("qianzi",StringComparison.OrdinalIgnoreCase)){var r=t.GetComponent<Renderer>();if(r&&r.sharedMaterial)texture=r.sharedMaterial.mainTexture;}
        if(!texture){Debug.LogWarning("qianzi document texture missing.");return;}
        var host=new GameObject("Qianzi Signature Form");SceneManager.MoveGameObjectToScene(host,scene);
        var form=host.AddComponent<Scene3SignatureForm>();form.document=texture;form.Begin();
    }
    void Begin()
    {
        active=this;openedFrame=Time.frameCount;oldLock=Cursor.lockState;oldVisible=Cursor.visible;
        player=FindObjectOfType<FirstPersonPlayer>();playerWasEnabled=player&&player.enabled;if(playerWasEnabled)player.enabled=false;
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        signature=Resources.Load<Texture2D>("Signature/UNIT001Signature");
        previouslySigned=IsSigned;
    }
    void Update()
    {
        if(active!=this)return;
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        if(signing&&signatureRendered){signElapsed+=Mathf.Min(Time.unscaledDeltaTime,.05f);if(signElapsed>=1.65f)SaveAndClose();}
        if(!signing&&Time.frameCount>openedFrame&&Input.GetMouseButtonDown(0)){Vector2 pointer=new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y);HandlePointer(pointer);}
        if(active==this&&!signing&&Input.GetKeyDown(KeyCode.Escape))Dismiss();
    }
    Rect PaperRect()
    {
        float h=Mathf.Min(Screen.height*.80f,Screen.height-130),w=h*document.width/document.height;
        if(w>Screen.width*.85f){w=Screen.width*.85f;h=w*document.height/document.width;}
        return new Rect((Screen.width-w)/2,45,w,h);
    }
    Rect ConfirmRect(){var paper=PaperRect();float bw=Mathf.Min(230,Screen.width*.36f);return new Rect((Screen.width-bw*2-18)/2,paper.yMax+38,bw,38);}
    Rect LaterRect(){var r=ConfirmRect();r.x+=r.width+18;return r;}
    // Read the click before other IMGUI panels can consume MouseUp.
    void HandlePointer(Vector2 position)
    {
        if(signing||!document)return;
        if(!previouslySigned&&ConfirmRect().Contains(position)){consumedFrame=Time.frameCount;Confirm();}
        else if(LaterRect().Contains(position)){consumedFrame=Time.frameCount;Dismiss();}
    }
    void OnGUI()
    {
        if(active!=this||!document)return;
        if(heading==null){heading=new GUIStyle(GUI.skin.label){fontSize=Mathf.Clamp(Screen.height/52,14,24),alignment=TextAnchor.MiddleCenter};heading.normal.textColor=new Color(.95f,.91f,1);button=new GUIStyle(GUI.skin.button){fontSize=Mathf.Clamp(Screen.height/60,13,20)};hint=new GUIStyle(heading){fontSize=Mathf.Clamp(Screen.height/75,11,16)};}
        var old=GUI.color;int depth=GUI.depth;bool enabled=GUI.enabled;GUI.depth=-1100;
        GUI.color=new Color(.025f,.01f,.045f,.82f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=Color.white;
        float h=Mathf.Min(Screen.height*.80f,Screen.height-130),w=h*document.width/document.height;
        if(w>Screen.width*.85f){w=Screen.width*.85f;h=w*document.height/document.width;}
        var paper=new Rect((Screen.width-w)/2,45,w,h);
        GUI.Label(new Rect(0,5,Screen.width,32),"RESTORATION ASSET CONFIRMATION",heading);
        GUI.DrawTexture(paper,document,ScaleMode.StretchToFill,true);
        var sign=new Rect(paper.x+paper.width*.66f,paper.y+paper.height*.695f,paper.width*.255f,paper.height*.135f);
        GUI.color=new Color(.40f,.24f,.57f,.45f);
        GUI.DrawTexture(new Rect(sign.x-1,sign.y-1,sign.width+2,1),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(sign.x-1,sign.yMax,sign.width+2,1),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(sign.x-1,sign.y,1,sign.height),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(sign.xMax,sign.y,1,sign.height),Texture2D.whiteTexture);GUI.color=Color.white;
        if((signing||previouslySigned)&&signature){float progress=previouslySigned?1f:Mathf.Clamp01(signElapsed/.95f);GUI.BeginClip(new Rect(sign.x,sign.y,sign.width*progress,sign.height));GUI.DrawTexture(new Rect(0,0,sign.width,sign.height),signature,ScaleMode.StretchToFill,true);GUI.EndClip();if(Event.current.type==EventType.Repaint)signatureRendered=true;}
        float y=paper.yMax+8;
        string message=error??(previouslySigned?"Signed by UNIT-001. Your confirmation has been saved.":signing?(signElapsed<.95f?"Signing...":"Signature confirmed."):"Choose Sign & Confirm to sign as UNIT-001, or Review Later to return.");
        GUI.Label(new Rect(0,y,Screen.width,24),message,hint);
        float bw=Mathf.Min(230,Screen.width*.36f),gap=18,x=(Screen.width-bw*2-gap)/2;
        GUI.enabled=!signing&&!previouslySigned;
        if(GUI.Button(ConfirmRect(),previouslySigned?"Signed: UNIT-001":"Sign & Confirm",button))Confirm();
        GUI.enabled=!signing;
        if(GUI.Button(LaterRect(),previouslySigned?"Return to Game":"Review Later",button))Dismiss();
        GUI.enabled=enabled;GUI.color=old;GUI.depth=depth;
    }
    void Confirm(){if(signing||previouslySigned)return;if(!signature){error="Signature could not be loaded. Please try again.";return;}error=null;signing=true;signedAt=Time.unscaledTime;signElapsed=0;signatureRendered=false;Debug.Log("[Scene3 Signature] Sign & Confirm clicked: signing UNIT-001.");}
    void SaveAndClose()
    {
        try{var data=Resources.Load<TextAsset>("Signature/UNIT001SignatureData");if(!data)throw new IOException("Signature data missing");File.WriteAllBytes(Path.Combine(Application.persistentDataPath,"Scene3RestorationSignature.png"),data.bytes);PlayerPrefs.SetString("Scene3.RestorationSignerName",SignerName);PlayerPrefs.SetInt("Scene3.RestorationAssetSigned",1);PlayerPrefs.Save();signedThisRun=true;Dismiss();}
        catch(Exception ex){signing=false;error="Signature could not be saved. Please try again.";Debug.LogError(ex.Message);}
    }
    void Dismiss(){Release();Destroy(gameObject);}
    void Release(){if(active!=this)return;active=null;consumedFrame=Time.frameCount;if(player&&playerWasEnabled)player.enabled=true;Cursor.lockState=oldLock;Cursor.visible=oldVisible;}
    void OnDisable(){Release();}
    void OnDestroy(){Release();}
}





