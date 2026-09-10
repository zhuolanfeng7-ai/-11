using System.Collections;
using UnityEngine;

/// <summary>
/// Turns the world object named "piece" into a six-part family portrait repair puzzle.
/// The texture is read from the existing piece renderer, so no duplicate artwork is needed.
/// </summary>
public sealed class FamilyPiecePuzzleController : MonoBehaviour
{
    [SerializeField] private string worldPieceName = "piece";
    [Tooltip("Optional explicit front-side family portrait texture. If empty, the script selects the front-looking material automatically.")]
    [SerializeField] private Texture frontPortraitTexture;
    [SerializeField, Min(.1f)] private float interactionDistance = 10f;
    [SerializeField, Min(5f)] private float snapDistancePixels = 125f;
    [Header("Completion Effect")]
    [Tooltip("The camera effect that controls the world's black-and-white saturation.")]
    [SerializeField] private VisionBlurEffect visionEffect;
    [SerializeField, Min(.01f)] private float colorRestoreDuration = 1.5f;
    [Header("Approach Lore")]
    [SerializeField, Min(.1f)] private float approachLoreDistance = 8f;

    private const int Columns = 3;
    private const int Rows = 2;
    private Transform worldPiece;
    private Collider interactionCollider;
    private HoverScanHighlight pieceHoverHighlight;
    private Camera viewCamera;
    private FirstPersonPlayer player;
    private Texture sourceTexture;
    private Rect[] currentRects = new Rect[Columns * Rows];
    private Rect[] targetRects = new Rect[Columns * Rows];
    private bool[] locked = new bool[Columns * Rows];
    private int[] quarterTurns = new int[Columns * Rows];
    private bool open;
    private bool completed;
    private bool colorRestored;
    private bool approachLoreShown;
    private bool completionLorePending;
    private bool completionLoreShown;
    private bool everCompleted;
    private GUIStyle headerStyle;
    private GUIStyle hintStyle;
    private GUIStyle completionStyle;
    private GUIStyle restoreHintStyle;

    public bool IsCollected => everCompleted;

    private void Awake()
    {
        player = FindObjectOfType<FirstPersonPlayer>();
        viewCamera = player != null ? player.PlayerCamera : Camera.main;
        if (visionEffect == null && viewCamera != null)
            visionEffect = viewCamera.GetComponent<VisionBlurEffect>();
        FindWorldPiece();
    }

    private void Update()
    {
        if (viewCamera == null) viewCamera = player != null ? player.PlayerCamera : Camera.main;
        if (!open)
        {
            if (TryShowApproachLore()) return;
            TryOpenPuzzle();
            return;
        }
        HandleRotationInput();
    }

    private void FindWorldPiece()
    {
        GameObject found = GameObject.Find(worldPieceName);
        if (found == null) return;
        worldPiece = found.transform;
        // The repair target is the supplied complete, upright family portrait.
        // Never fall back to the piece model's unrelated/side material when
        // this dedicated front image is present.
        Texture2D completePortrait = Resources.Load<Texture2D>("FamilyPortraitComplete");
        sourceTexture = completePortrait != null
            ? completePortrait
            : (frontPortraitTexture != null ? frontPortraitTexture : FindSourceTexture(worldPiece));
        interactionCollider = worldPiece.GetComponent<Collider>();
        if (interactionCollider == null) interactionCollider = AddFittedCollider(worldPiece);
        EnsurePieceHoverHighlight();
    }

    private void EnsurePieceHoverHighlight()
    {
        // Match the Glass pickup feedback: a lavender edge glow with one
        // purple and one green close-fitting scan frame.
        pieceHoverHighlight = worldPiece.GetComponent<HoverScanHighlight>();
        if (pieceHoverHighlight == null) pieceHoverHighlight = worldPiece.gameObject.AddComponent<HoverScanHighlight>();
        pieceHoverHighlight.Configure(20f, 5f);
    }

    private void TryOpenPuzzle()
    {
        if (!Input.GetMouseButtonDown(0) || viewCamera == null) return;
        if (worldPiece == null) FindWorldPiece();
        if (worldPiece == null || sourceTexture == null || interactionCollider == null) return;
        Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
        if (!interactionCollider.Raycast(ray, out _, 100f)) return;
        Transform origin = player != null ? player.transform : viewCamera.transform;
        if (Vector3.Distance(origin.position, interactionCollider.ClosestPoint(origin.position)) > interactionDistance) return;
        OpenPuzzle();
    }

    private bool TryShowApproachLore()
    {
        if (approachLoreShown || worldPiece == null || interactionCollider == null || player == null) return false;
        if (Vector3.Distance(player.transform.position, interactionCollider.ClosestPoint(player.transform.position)) > approachLoreDistance)
            return false;

        WorldStartTerminal terminal = FindObjectOfType<WorldStartTerminal>();
        if (terminal == null || !terminal.IsGameStarted) return false;

        approachLoreShown = true;
        terminal.ShowLoreMessage(
            "VISUAL SYSTEM DIAGNOSTIC",
            "VISUAL SYSTEM: OPERATING NORMALLY.\nCOLOR FEEDBACK: UNAVAILABLE.\n\nSTRANGE...");
        terminal.QueueLoreMessage(
            "MEMORY ANOMALY",
            "YOU ONCE PAID FOR COUNTLESS EXPENSIVE VISUAL EXPERIENCES: VIRTUAL WORLDS, HIGH-END IMAGERY, ARTIFICIAL LANDSCAPES.\n\nBUT NOW... A FORGOTTEN MEMORY IS ENOUGH TO DRAIN THE COLOR FROM YOUR WORLD.");
        return true;
    }

    private void OpenPuzzle()
    {
        open = true;
        // The world-space scanner would otherwise remain visible behind the
        // puzzle UI and break the focused, immersive puzzle view.
        if (pieceHoverHighlight != null) pieceHoverHighlight.enabled = false;
        completed = false;
        for (int i = 0; i < locked.Length; i++) locked[i] = false;
        BuildLayout();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void BuildLayout()
    {
        float scale = Mathf.Clamp(Screen.height / 900f, .7f, 1.05f);
        float canvasWidth = 480f * scale;
        float textureAspect = Mathf.Max(.1f, sourceTexture.width / (float)sourceTexture.height);
        float canvasHeight = canvasWidth / textureAspect;
        float pieceWidth = canvasWidth / Columns;
        float pieceHeight = canvasHeight / Rows;
        float x = (Screen.width - canvasWidth) * .5f;
        float y = (Screen.height - canvasHeight) * .5f - 5f * scale;
        for (int row = 0; row < Rows; row++)
        for (int col = 0; col < Columns; col++)
        {
            int index = row * Columns + col;
            targetRects[index] = new Rect(x + col * pieceWidth, y + row * pieceHeight, pieceWidth, pieceHeight);
        }

        for (int i = 0; i < currentRects.Length; i++)
        {
            // Every piece begins in its real final position. Five of the six
            // only need their orientation corrected; no dragging is involved.
            currentRects[i] = targetRects[i];
            quarterTurns[i] = new[] { 1, 2, 3, 1, 2, 0 }[i];
            locked[i] = quarterTurns[i] == 0;
        }
    }

    private void HandleRotationInput()
    {
        Vector2 mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        if (Input.GetKeyDown(KeyCode.Escape)) { ClosePuzzle(); return; }
        if (!Input.GetMouseButtonDown(0)) return;
        if (completed)
        {
            ClosePuzzle();
            return;
        }
        for (int i = 0; i < currentRects.Length; i++)
        {
            if (!currentRects[i].Contains(mouse) || locked[i]) continue;
            quarterTurns[i] = (quarterTurns[i] + 1) % 4;
            locked[i] = quarterTurns[i] == 0;
            if (AllLocked())
            {
                completed = true;
                everCompleted = true;
                RestoreWorldColor();
                completionLorePending = true;
            }
            break;
        }
    }

    private void RestoreWorldColor()
    {
        if (colorRestored) return;
        if (visionEffect == null && viewCamera != null)
            visionEffect = viewCamera.GetComponent<VisionBlurEffect>();
        if (visionEffect == null) return;

        colorRestored = true;
        StartCoroutine(RestoreWorldColorRoutine());
    }

    private IEnumerator RestoreWorldColorRoutine()
    {
        float initialSaturation = visionEffect.Saturation;
        float elapsed = 0f;
        while (elapsed < colorRestoreDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / colorRestoreDuration);
            visionEffect.Saturation = Mathf.Lerp(initialSaturation, 1f, Mathf.SmoothStep(0f, 1f, progress));
            yield return null;
        }
        visionEffect.Saturation = 1f;
    }

    private bool AllLocked()
    {
        for (int i = 0; i < locked.Length; i++) if (!locked[i]) return false;
        return true;
    }

    private void ClosePuzzle()
    {
        open = false;
        if (pieceHoverHighlight != null) pieceHoverHighlight.enabled = true;
        if (completionLorePending && !completionLoreShown)
        {
            completionLorePending = false;
            StartCoroutine(ShowCompletionLoreAfterPuzzleCloses());
        }
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }

    private IEnumerator ShowCompletionLoreAfterPuzzleCloses()
    {
        // The click that closes the puzzle must never also dismiss this lore.
        yield return null;
        while (Input.GetMouseButton(0)) yield return null;
        WorldStartTerminal terminal = FindObjectOfType<WorldStartTerminal>();
        if (terminal == null || !terminal.IsGameStarted) yield break;

        completionLoreShown = true;
        terminal.ShowLoreMessage(
            "VISUAL ARCHIVE RESTORED",
            "VISUAL ARCHIVE RESTORED.\nASSET VALUE: 0.\nMARKET VALUE: 0.\nCOMMERCIAL VALUE: 0.\n\nSTRANGE.\n\nYOU ONCE BELIEVED THAT EVERYTHING HAD A PRICE.\nYET THIS IMAGE... IS THE ONLY THING YOU REFUSED TO TRADE.\n\nORIGINAL VISUAL EXPERIENCE RESTORED.");
    }

    private void EnsureStyles()
    {
        if (headerStyle != null) return;
        headerStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20, fontStyle = FontStyle.Bold };
        headerStyle.normal.textColor = new Color(.7f, .9f, 1f);
        hintStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 13 };
        hintStyle.normal.textColor = new Color(.55f, 1f, .7f);
        completionStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 24,
            fontStyle = FontStyle.Bold
        };
        completionStyle.normal.textColor = new Color(.22f, 1f, .48f, 1f);
        restoreHintStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            wordWrap = true
        };
        restoreHintStyle.normal.textColor = new Color(.76f, .28f, 1f, 1f);
    }

    private void OnGUI()
    {
        if (!open || sourceTexture == null) return;
        EnsureStyles();
        GUI.color = new Color(.025f, .01f, .05f, .88f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Faint target cells make the intended 3x2 assembly immediately clear.
        for (int i = 0; i < targetRects.Length; i++)
            DrawFrame(targetRects[i], new Color(.24f, .8f, .54f, .25f), 2f);

        Rect canvas = GetCanvasBounds();
        GUI.color = new Color(.16f, .07f, .25f, .96f);
        GUI.DrawTexture(new Rect(canvas.x - 12, canvas.y - 12, canvas.width + 24, canvas.height + 24), Texture2D.whiteTexture);
        GUI.color = new Color(.14f, .95f, .55f, 1f);
        GUI.DrawTexture(new Rect(canvas.x - 12, canvas.y - 12, canvas.width + 24, 3f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(0, canvas.y - 65f, Screen.width, 28f), completed ? "" : "ROTATE THE MEMORY", headerStyle);
        GUI.Label(new Rect(0, canvas.y - 36f, Screen.width, 20f), completed ? "CLICK ANYWHERE TO CLOSE" : "CLICK A PIECE TO ROTATE IT", hintStyle);
        if (!completed)
        {
            Rect restoreHintRect = new Rect(Screen.width - 310f, Screen.height * .5f - 46f, 280f, 92f);
            GUI.Label(restoreHintRect, "RESTORE THE PHOTO\nTO ITS ORIGINAL STATE.", restoreHintStyle);
        }

        for (int i = 0; i < currentRects.Length; i++)
        {
            Rect uv = new Rect((i % Columns) / (float)Columns, 1f - ((i / Columns + 1) / (float)Rows), 1f / (float)Columns, 1f / (float)Rows);
            DrawRotatedPiece(currentRects[i], uv, quarterTurns[i]);
            DrawFrame(currentRects[i], locked[i] ? new Color(.28f, 1f, .55f, .96f) : new Color(.75f, .3f, 1f, .94f), locked[i] ? 3f : 2.5f);
        }

        if (completed) DrawCompletionMessage();
    }

    private void DrawCompletionMessage()
    {
        // Place the completion text on the left side so the restored portrait
        // remains unobstructed in the middle of the screen.
        Rect textRect = new Rect(32f, Screen.height * .5f - 18f, 280f, 36f);
        Color previous = GUI.color;
        GUI.color = new Color(.05f, .65f, .22f, .32f);
        GUI.Label(new Rect(textRect.x - 2f, textRect.y - 2f, textRect.width + 4f, textRect.height + 4f), "PUZZLE COMPLETE", completionStyle);
        GUI.color = Color.white;
        GUI.Label(textRect, "PUZZLE COMPLETE", completionStyle);
        GUI.color = previous;
    }

    private Rect GetCanvasBounds()
    {
        Rect result = targetRects[0];
        for (int i = 1; i < targetRects.Length; i++) result = Encapsulate(result, targetRects[i]);
        return result;
    }

    private static Rect Encapsulate(Rect a, Rect b)
    {
        float xMin = Mathf.Min(a.xMin, b.xMin), yMin = Mathf.Min(a.yMin, b.yMin);
        float xMax = Mathf.Max(a.xMax, b.xMax), yMax = Mathf.Max(a.yMax, b.yMax);
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private static void DrawFrame(Rect rect, Color color, float thickness)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private void DrawRotatedPiece(Rect rect, Rect uv, int turns)
    {
        GUI.BeginGroup(rect);
        Matrix4x4 previous = GUI.matrix;
        GUIUtility.RotateAroundPivot(turns * 90f, new Vector2(rect.width * .5f, rect.height * .5f));
        GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, rect.width, rect.height), sourceTexture, uv, true);
        GUI.matrix = previous;
        GUI.EndGroup();
    }

    private static Texture FindSourceTexture(Transform root)
    {
        // Imported double-sided paper commonly uses a material named Front.
        // Prefer that material so the repair UI shows the family portrait face,
        // not the paper's reverse/edge material.
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        foreach (Material material in renderer.sharedMaterials)
        {
            if (material == null || material.mainTexture == null) continue;
            string name = material.name.ToLowerInvariant();
            if (name.Contains("front") || name.Contains("family") || name.Contains("portrait"))
                return material.mainTexture;
        }
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            foreach (Material material in renderer.sharedMaterials)
                if (material != null && material.mainTexture != null) return material.mainTexture;
        }
        return null;
    }

    private static Collider AddFittedCollider(Transform root)
    {
        BoxCollider box = root.gameObject.AddComponent<BoxCollider>();
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return box;
        Bounds world = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) world.Encapsulate(renderers[i].bounds);
        Vector3 min = world.min, max = world.max;
        Bounds local = new Bounds(root.InverseTransformPoint(min), Vector3.zero);
        for (int x = 0; x <= 1; x++) for (int y = 0; y <= 1; y++) for (int z = 0; z <= 1; z++)
            local.Encapsulate(root.InverseTransformPoint(new Vector3(x == 0 ? min.x : max.x, y == 0 ? min.y : max.y, z == 0 ? min.z : max.z)));
        box.center = local.center;
        box.size = local.size;
        box.isTrigger = true;
        return box;
    }
}
