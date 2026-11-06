using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-300)]
public sealed class Scene3HumanDialogue : MonoBehaviour
{
    public const string Dialogue = "Welcome back, Founder. Your sensory restoration procedure is complete.\n\nHere is your Restoration Asset Confirmation Form. Please sign to acknowledge receipt.\n\nThe assets have passed compatibility testing and fully meet your restoration requirements.\n\nYou may consult the company archives for information about their origins.\n\nFeel free to explore the company first. You can return to sign the form later.";
    public static bool BlocksWorldInput => current != null && current.open || consumedFrame == Time.frameCount;
    static Scene3HumanDialogue current;
    static int consumedFrame = -1;
    Transform human;
    bool open, dismissedNearby;
    int openedFrame;
    CursorLockMode oldLock;
    bool oldVisible;
    FirstPersonPlayer player;
    bool playerWasEnabled;
    GUIStyle bodyStyle, labelStyle, hintStyle;
    Texture2D panelTexture;
    const float TriggerDistance = 1.3f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { current = null; consumedFrame = -1; SceneManager.sceneLoaded -= SceneLoaded; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize() { SceneManager.sceneLoaded -= SceneLoaded; SceneManager.sceneLoaded += SceneLoaded; }
    static void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.name.Replace(" ", "").Equals("scene3", System.StringComparison.OrdinalIgnoreCase)) return;
        foreach (var root in scene.GetRootGameObjects())
            if (root.GetComponentInChildren<Scene3HumanDialogue>(true)) return;
        var host = new GameObject("Human Reception Dialogue");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<Scene3HumanDialogue>();
    }
    void Update()
    {
        if (!human)
        {
            foreach (var root in gameObject.scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if ((t.name.Equals("human", System.StringComparison.OrdinalIgnoreCase) || t.name.Equals("Human Animated Rig", System.StringComparison.OrdinalIgnoreCase)) && t.gameObject.activeInHierarchy) { human = t; break; }
            if (!human) return;
        }
        var cam = Camera.main;
        if (!cam || !Application.isFocused) return;
        Vector3 delta = cam.transform.position - human.position; delta.y = 0;
        float distance = delta.magnitude;
        if (distance > TriggerDistance + .4f) dismissedNearby = false;
        if (!open && !dismissedNearby && distance <= TriggerDistance && !EvidenceInspect.BlocksWorldInput) Open();
        if (open) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }
    void Open()
    {
        current = this; open = true; openedFrame = Time.frameCount;
        oldLock = Cursor.lockState; oldVisible = Cursor.visible;
        player = FindObjectOfType<FirstPersonPlayer>(); playerWasEnabled = player && player.enabled;
        if (playerWasEnabled) player.enabled = false;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }
    void Close()
    {
        if (!open) return;
        open = false; dismissedNearby = true; consumedFrame = Time.frameCount;
        if (player && playerWasEnabled) player.enabled = true;
        Cursor.lockState = oldLock; Cursor.visible = oldVisible;
        if (current == this) current = null;
    }
    void OnGUI()
    {
        if (!open) return;
        if (bodyStyle == null)
        {
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, wordWrap = true, richText = false, padding = new RectOffset(0,0,0,0) };
            bodyStyle.normal.textColor = new Color(.96f,.94f,1);
            labelStyle = new GUIStyle(bodyStyle) { fontSize = 17, fontStyle = FontStyle.Bold }; labelStyle.normal.textColor = new Color(.77f,.57f,1);
            hintStyle = new GUIStyle(bodyStyle) { fontSize = 16, alignment = TextAnchor.MiddleRight };
            panelTexture = new Texture2D(1,1); panelTexture.SetPixel(0,0,new Color(.07f,.035f,.105f,.94f)); panelTexture.Apply();
        }
        var matrix = GUI.matrix; var color = GUI.color; int depth = GUI.depth;
        float scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
        GUI.matrix = Matrix4x4.TRS(Vector3.zero,Quaternion.identity,Vector3.one*scale); GUI.depth = -1000;
        float width = Screen.width / scale, height = Screen.height / scale;
        float panelWidth = Mathf.Min(width-100,1560), textWidth = panelWidth-80;
        float contentHeight = bodyStyle.CalcHeight(new GUIContent(Dialogue),textWidth);
        var panel = new Rect((width-panelWidth)/2,height-contentHeight-175,panelWidth,contentHeight+135);
        GUI.color = new Color(.68f,.46f,.85f,.9f); GUI.DrawTexture(new Rect(panel.x-1,panel.y-1,panel.width+2,panel.height+2),Texture2D.whiteTexture);
        GUI.color = Color.white; GUI.DrawTexture(panel,panelTexture);
        GUI.Label(new Rect(panel.x+40,panel.y+16,textWidth,25),"S E N S O R Y   L I Q U I D I T Y",labelStyle);
        GUI.Label(new Rect(panel.x+40,panel.y+49,textWidth,25),"RECEPTIONIST",labelStyle);
        GUI.Label(new Rect(panel.x+40,panel.y+83,textWidth,contentHeight),Dialogue,bodyStyle);
        GUI.Label(new Rect(panel.x+40,panel.yMax-36,textWidth,25),"Click to close",hintStyle);
        GUI.matrix = matrix; GUI.color = color; GUI.depth = depth;
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && Time.frameCount > openedFrame) { Close(); Scene3SignatureForm.Show(gameObject.scene); Event.current.Use(); }
    }
    void OnDisable() { Close(); }
    void OnDestroy() { Close(); if (panelTexture) Destroy(panelTexture); }
}


