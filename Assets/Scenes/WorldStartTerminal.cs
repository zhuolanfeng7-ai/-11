using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Opens the game with a small in-world system terminal.  There is no button:
/// one left click anywhere acknowledges the boot message and enables play.
/// </summary>
public sealed class WorldStartTerminal : MonoBehaviour
{
    // One fixed terminal footprint for every present and future world-lore page.
    // One larger fixed footprint shared by every opening and gameplay lore card.
    private const float TerminalWidth = WorldLoreTerminalLayout.Width;
    private const float TerminalHeight = WorldLoreTerminalLayout.Height;
    private const float TerminalLeft = WorldLoreTerminalLayout.Left;
    private const float TerminalMargin = WorldLoreTerminalLayout.Bottom;
    [SerializeField, Min(0f)] private float fadeSeconds = .42f;

    private readonly List<Behaviour> pausedGameplay = new List<Behaviour>();
    private bool active = true;
    private bool starting;
    private int page;
    private float alpha = 1f;
    private bool postStartMessageVisible;
    private float postStartMessageAlpha;
    private bool postMessageCanBeDismissed;
    private string postStartTitle = "SENSORY DATA TRACE";
    private string postStartBody = "LOST VISUAL INTERFACE DETECTED. IT AWAITS THE RETURN OF ITS ORIGINAL OWNER.";
    private readonly Queue<LorePage> queuedLore = new Queue<LorePage>();
    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle promptStyle;

    private struct LorePage
    {
        public string Title;
        public string Body;
        public LorePage(string title, string body) { Title = title; Body = body; }
    }

    /// <summary>True once the player has passed the two opening terminal pages.</summary>
    public bool IsGameStarted => !active;

    private void Awake()
    {
        AttachKeyHoverHighlight();
        AttachPurpleBoxHoverHighlight();
        PauseGameplay();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private static void AttachKeyHoverHighlight()
    {
        GameObject keyObject = GameObject.Find("key");
        if (keyObject == null) return;
        if (keyObject.GetComponentInChildren<Collider>(true) == null)
            AddFittedCollider(keyObject);

        HoverScanHighlight highlight = keyObject.GetComponent<HoverScanHighlight>();
        if (highlight == null) highlight = keyObject.AddComponent<HoverScanHighlight>();
        highlight.Configure(20f, 5f);
        if (keyObject.GetComponent<KeyPickupHandler>() == null)
            keyObject.AddComponent<KeyPickupHandler>();
    }

    private static void AttachPurpleBoxHoverHighlight()
    {
        // The placed prefab was saved as "purplr box" in the scene.  Accept the
        // intended spelling too, so renaming it later does not break the effect.
        GameObject purpleBox = null;
        foreach (Transform candidate in FindObjectsOfType<Transform>(true))
        {
            if (string.Equals(candidate.name, "purplr box", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(candidate.name, "purple box", System.StringComparison.OrdinalIgnoreCase))
            {
                purpleBox = candidate.gameObject;
                break;
            }
        }

        if (purpleBox == null) return;
        if (purpleBox.GetComponentInChildren<Collider>(true) == null)
            AddFittedCollider(purpleBox);

        HoverScanHighlight highlight = purpleBox.GetComponent<HoverScanHighlight>();
        if (highlight == null) highlight = purpleBox.AddComponent<HoverScanHighlight>();
        highlight.Configure(20f, 5f);

        if (purpleBox.GetComponent<PurpleBoxUnlockController>() == null)
            purpleBox.AddComponent<PurpleBoxUnlockController>();
    }

    private static BoxCollider AddFittedCollider(GameObject root)
    {
        BoxCollider box = root.AddComponent<BoxCollider>();
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return box;
        Bounds world = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) world.Encapsulate(renderers[i].bounds);
        Vector3 min = world.min, max = world.max;
        Bounds local = new Bounds(root.transform.InverseTransformPoint(world.center), Vector3.zero);
        for (int x = 0; x <= 1; x++) for (int y = 0; y <= 1; y++) for (int z = 0; z <= 1; z++)
            local.Encapsulate(root.transform.InverseTransformPoint(new Vector3(
                x == 0 ? min.x : max.x,
                y == 0 ? min.y : max.y,
                z == 0 ? min.z : max.z)));
        box.center = local.center;
        box.size = local.size;
        box.isTrigger = true;
        return box;
    }

    private void Update()
    {
        if (!active)
        {
            if (postStartMessageVisible && !Input.GetMouseButton(0))
                postMessageCanBeDismissed = true;
            if (postStartMessageVisible && postMessageCanBeDismissed && Input.GetMouseButtonDown(0))
                StartCoroutine(DismissPostStartMessage());
            return;
        }
        if (starting || !Input.GetMouseButtonDown(0)) return;
        if (page == 0)
        {
            page = 1;
            return;
        }
        StartCoroutine(BeginGame());
    }

    private void PauseGameplay()
    {
        // Pause only player control and click-driven puzzle scripts. Ambient
        // visuals, the surveillance monitor, and the black-and-white effect
        // continue to render while the terminal is being read.
        foreach (MonoBehaviour behaviour in FindObjectsOfType<MonoBehaviour>(true))
        {
            if (behaviour == null || behaviour == this || !behaviour.enabled) continue;
            string type = behaviour.GetType().Name;
            bool isPlayerControl = behaviour is FirstPersonPlayer;
            bool isInteractive = type.Contains("Puzzle") || type.Contains("Pickup") ||
                                 type.Contains("Inspect") || type.Contains("Note") ||
                                 type.Contains("UseController");
            if (!isPlayerControl && !isInteractive) continue;
            pausedGameplay.Add(behaviour);
            behaviour.enabled = false;
        }
    }

    private IEnumerator BeginGame()
    {
        starting = true;
        float elapsed = 0f;
        while (elapsed < fadeSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            alpha = 1f - Mathf.Clamp01(elapsed / fadeSeconds);
            yield return null;
        }
        active = false;
        alpha = 0f;
        foreach (Behaviour behaviour in pausedGameplay)
            if (behaviour != null) behaviour.enabled = true;
        pausedGameplay.Clear();
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
        postStartMessageVisible = true;
        postStartMessageAlpha = 1f;
        postMessageCanBeDismissed = false;
    }

    /// <summary>Displays a gameplay-triggered lore message in the same fixed terminal.</summary>
    public void ShowLoreMessage(string title, string message)
    {
        if (active) return;
        StopCoroutine(nameof(DismissPostStartMessage));
        postStartTitle = title;
        postStartBody = message;
        postStartMessageAlpha = 1f;
        postStartMessageVisible = true;
        // A message can never consume the click which triggered it. The player
        // must release that click, then deliberately click again to dismiss.
        postMessageCanBeDismissed = false;
    }

    /// <summary>Shows this message immediately after the current lore card is acknowledged.</summary>
    public void QueueLoreMessage(string title, string message)
    {
        queuedLore.Enqueue(new LorePage(title, message));
    }

    private void EnsureStyles()
    {
        if (titleStyle != null) return;
        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 19,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        titleStyle.normal.textColor = new Color(.66f, .78f, 1f);
        bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Normal,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft
        };
        bodyStyle.normal.textColor = new Color(.9f, .87f, .96f);
        promptStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleLeft
        };
        promptStyle.normal.textColor = new Color(.36f, 1f, .68f);
    }

    private void OnGUI()
    {
        if (!active)
        {
            DrawPostStartMessage();
            return;
        }
        if (alpha <= 0f) return;
        EnsureStyles();
        float scale = Mathf.Clamp(Screen.height / 900f, .78f, 1.18f);
        float width = TerminalWidth * scale;
        float height = TerminalHeight * scale;
        Rect box = new Rect(TerminalLeft * scale, Screen.height - height - TerminalMargin * scale, width, height);

        Color oldColor = GUI.color;
        GUI.color = new Color(.15f, .075f, .25f, .93f * alpha);
        GUI.DrawTexture(box, Texture2D.whiteTexture);
        GUI.color = new Color(.08f, .82f, .42f, alpha);
        GUI.DrawTexture(new Rect(box.x, box.y, box.width, 3f * scale), Texture2D.whiteTexture);
        GUI.color = new Color(.25f, .62f, 1f, alpha);
        GUI.DrawTexture(new Rect(box.x + 7f * scale, box.y + 8f * scale, box.width - 14f * scale, 2f * scale), Texture2D.whiteTexture);
        GUI.color = oldColor;

        if (page == 0)
        {
            DrawLabel(new Rect(box.x + 20f * scale, box.y + 25f * scale, box.width - 40f * scale, 28f * scale), "UNIT-001", titleStyle);
            DrawLabel(new Rect(box.x + 20f * scale, box.y + 61f * scale, box.width - 40f * scale, 45f * scale),
                "AUTONOMOUS CLEANING UNIT\nMISSION: RECOVER LOST SENSORY DATA.", bodyStyle);
        }
        else
        {
            DrawLabel(new Rect(box.x + 20f * scale, box.y + 25f * scale, box.width - 40f * scale, 28f * scale), "SYSTEM INQUIRY", titleStyle);
            DrawLabel(new Rect(box.x + 20f * scale, box.y + 66f * scale, box.width - 40f * scale, 72f * scale),
                "GREETINGS, ESTEEMED ENTREPRENEUR. WHAT BRINGS YOU TO THIS IMPOVERISHED ROOM?\nWOULD YOU ABANDON YOUR WEALTH AND STATUS—TO BECOME ORDINARY?", bodyStyle);
        }
        DrawLabel(new Rect(box.x + 20f * scale, box.yMax - 31f * scale, box.width - 40f * scale, 18f * scale),
            page == 0 ? "CLICK ANYWHERE TO CONTINUE" : "CLICK ANYWHERE TO INITIALIZE", promptStyle);
    }

    private void DrawPostStartMessage()
    {
        if (postStartMessageAlpha <= 0f) return;
        EnsureStyles();
        float fade = postStartMessageAlpha;
        float scale = Mathf.Clamp(Screen.height / 900f, .78f, 1.18f);
        Rect box = new Rect(TerminalLeft * scale,
            Screen.height - TerminalHeight * scale - TerminalMargin * scale,
            TerminalWidth * scale, TerminalHeight * scale);
        Color old = GUI.color;
        GUI.color = new Color(.15f, .075f, .25f, .9f * fade);
        GUI.DrawTexture(box, Texture2D.whiteTexture);
        GUI.color = new Color(.08f, .82f, .42f, fade);
        GUI.DrawTexture(new Rect(box.x, box.y, box.width, 3f * scale), Texture2D.whiteTexture);
        GUI.color = old;

        float oldAlpha = alpha;
        alpha = fade;
        DrawLabel(new Rect(box.x + 20f * scale, box.y + 25f * scale, box.width - 40f * scale, 24f * scale), postStartTitle, promptStyle);
        DrawLabel(new Rect(box.x + 20f * scale, box.y + 64f * scale, box.width - 40f * scale, box.height - 82f * scale),
            postStartBody, bodyStyle);
        alpha = oldAlpha;
    }

    private IEnumerator DismissPostStartMessage()
    {
        if (!postStartMessageVisible) yield break;
        // Stop accepting further clicks while its short fade is in progress.
        postStartMessageVisible = false;
        postMessageCanBeDismissed = false;
        float startAlpha = postStartMessageAlpha;
        float elapsed = 0f;
        while (elapsed < .28f)
        {
            elapsed += Time.unscaledDeltaTime;
            postStartMessageAlpha = Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(elapsed / .28f));
            yield return null;
        }
        postStartMessageAlpha = 0f;
        if (queuedLore.Count > 0)
        {
            LorePage nextPage = queuedLore.Dequeue();
            postStartTitle = nextPage.Title;
            postStartBody = nextPage.Body;
            postStartMessageAlpha = 1f;
            postStartMessageVisible = true;
            // The click that dismissed the prior page may still be held.
            postMessageCanBeDismissed = false;
        }
    }

    private void DrawLabel(Rect rect, string text, GUIStyle style)
    {
        Color old = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.Label(rect, text, style);
        GUI.color = old;
    }
}
