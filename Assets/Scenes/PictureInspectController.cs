using UnityEngine;

/// <summary>Opens the existing picture as a lower-centre inspection card; the world model is never hidden.</summary>
public sealed class PictureInspectController : MonoBehaviour
{
    [SerializeField] private Texture2D frontTexture;
    [SerializeField] private Texture2D backTexture;
    [SerializeField] private float interactionDistance = 5f;
    [SerializeField, Min(.1f)] private float approachLoreDistance = 12f;
    [SerializeField] private float flipDelay = 2f;
    [SerializeField, Min(.1f)] private float backVisibleDuration = 3f;
    [Header("Inspection Card Layout")]
    [SerializeField, Range(.1f, .95f)] private float maximumScreenWidth = .46f;
    [SerializeField, Range(.1f, .95f)] private float maximumScreenHeight = .78f;
    [SerializeField] private float maximumWidthPixels = 620f;
    [SerializeField] private float inspectionCardAspect = 1f;
    private Camera viewCamera;
    private Collider pictureCollider;
    private Renderer[] pictureRenderers;
    private FirstPersonPlayer player;
    private bool inspecting, showingBack;
    private bool approachLoreShown;
    private bool completionLoreShown;
    private bool collected;
    private float openedAt;
    private float backShownAt;

    public bool IsCollected => collected;

    private void Awake()
    {
        viewCamera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
        player = FindObjectOfType<FirstPersonPlayer>();
        pictureCollider = GetComponent<Collider>();
        if (pictureCollider == null) pictureCollider = gameObject.AddComponent<BoxCollider>();
        pictureRenderers = GetComponentsInChildren<Renderer>(true);
    }

    private void Update()
    {
        TryShowApproachLore();
        // The back is a short-lived reveal.  After three seconds the inspection
        // card closes completely and returns the player to the game view.
        if (inspecting && showingBack && Time.time - backShownAt >= backVisibleDuration)
        {
            inspecting = false;
            showingBack = false;
            ShowCompletionLore();
            return;
        }

        if (!Input.GetMouseButtonDown(0) || viewCamera == null) return;
        if (inspecting)
        {
            if (!showingBack && Time.time - openedAt >= flipDelay)
            {
                showingBack = true;
                collected = true;
                backShownAt = Time.time;
            }
            return;
        }
        Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
        if (!pictureCollider.Raycast(ray, out _, 100f)) return;
        Transform origin = player != null ? player.transform : viewCamera.transform;
        if (Vector3.Distance(origin.position, pictureCollider.ClosestPoint(origin.position)) > interactionDistance) return;
        inspecting = true;
        showingBack = false;
        openedAt = Time.time;
    }

    private void TryShowApproachLore()
    {
        if (approachLoreShown || player == null) return;
        Bounds pictureBounds = GetPictureBounds();
        if (Vector3.Distance(player.transform.position, pictureBounds.ClosestPoint(player.transform.position)) > approachLoreDistance)
            return;

        WorldStartTerminal terminal = FindObjectOfType<WorldStartTerminal>();
        if (terminal == null || !terminal.IsGameStarted) return;

        approachLoreShown = true;
        Debug.Log("[PictureSketch] Player approached picture sketch; showing visual residue lore.", this);
        terminal.ShowLoreMessage(
            "UNREAD VISUAL RESIDUE",
            "UNREAD VISUAL RESIDUE DETECTED.\n\nTHIS DATA WAS NEVER REGISTERED IN THE SENSORY TRADING SYSTEM.\n\nNO COMMERCIAL VALUE.\nNO MARKET PRICE.\nNO CORPORATE DEMAND.\n\nYET...\n\nYOUR MEMORY SYSTEM CONTINUES TO PRESERVE IT.");
    }

    private Bounds GetPictureBounds()
    {
        if (pictureRenderers == null || pictureRenderers.Length == 0)
            return new Bounds(transform.position, Vector3.one);
        Bounds bounds = pictureRenderers[0].bounds;
        for (int i = 1; i < pictureRenderers.Length; i++)
            if (pictureRenderers[i] != null) bounds.Encapsulate(pictureRenderers[i].bounds);
        return bounds;
    }

    private void ShowCompletionLore()
    {
        if (completionLoreShown) return;
        WorldStartTerminal terminal = FindObjectOfType<WorldStartTerminal>();
        if (terminal == null || !terminal.IsGameStarted) return;

        completionLoreShown = true;
        terminal.ShowLoreMessage(
            "ORIGINAL MEMORY DETECTED",
            "ORIGINAL MEMORY DETECTED.\n\nNO MARKET VALUE.\nNO COMMERCIAL PURPOSE.\nNO BUYER FOUND.\n\nYET...\n\nIT REMAINED.\n\nBECAUSE LOVE IS NOT AN ASSET.\nFAMILY CANNOT BE SOLD.");
    }

    private void OnGUI()
    {
        if (!inspecting) return;
        Texture texture = showingBack ? backTexture : frontTexture;
        if (texture == null) return;
        // The inspection surface is deliberately fixed-size for both sides.
        // The portrait front is fitted *inside* it, instead of changing the
        // surface dimensions and appearing smaller than the square back.
        float width = Mathf.Min(Screen.width * maximumScreenWidth, maximumWidthPixels);
        float height = width / Mathf.Max(.01f, inspectionCardAspect);
        float heightLimit = Screen.height * maximumScreenHeight;
        if (height > heightLimit)
        {
            height = heightLimit;
            width = height * inspectionCardAspect;
        }

        float x = (Screen.width - width) * .5f;
        float y = Mathf.Clamp(Screen.height * .58f - height * .5f, 14f, Screen.height - height - 34f);
        Rect card = new Rect(x, y, width, height);
        GUI.DrawTexture(card, Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
        GUI.DrawTexture(card, texture, ScaleMode.ScaleToFit, true);
        if (Time.time - openedAt < flipDelay)
            GUI.Label(new Rect(card.x, card.yMax + 8f, card.width, 24f), "…", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } });
    }
}
