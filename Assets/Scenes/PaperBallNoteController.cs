using UnityEngine;

/// <summary>
/// Functional controller for a paper-ball clue.  Attach this to the stable
/// PaperBallPuzzle parent and assign its child paper-ball Collider.
/// </summary>
public sealed class PaperBallNoteController : MonoBehaviour
{
    [Header("Hierarchy References")]
    [SerializeField] private Collider paperBallCollider;
    [SerializeField] private Texture2D noteTexture;

    [Header("Interaction")]
    [SerializeField, Min(.1f)] private float interactionDistance = 12f;
    [SerializeField, Min(.1f)] private float approachLoreDistance = 8f;

    [Header("Inspection Card Layout")]
    [SerializeField, Range(.1f, .95f)] private float maximumScreenWidth = .46f;
    [SerializeField, Range(.1f, .95f)] private float maximumScreenHeight = .78f;
    [SerializeField] private float maximumWidthPixels = 620f;
    [SerializeField] private float cardAspect = 1f;

    private Camera viewCamera;
    private FirstPersonPlayer player;
    private bool displaying;
    private bool approachLoreShown;

    private void Awake()
    {
        player = FindObjectOfType<FirstPersonPlayer>();
        viewCamera = player != null && player.PlayerCamera != null
            ? player.PlayerCamera
            : (Camera.main != null ? Camera.main : FindObjectOfType<Camera>());
        if (paperBallCollider == null) paperBallCollider = GetComponentInChildren<Collider>(true);
        if (noteTexture == null) noteTexture = Resources.Load<Texture2D>("ClockPaperNote-v4");
        if (paperBallCollider == null)
            Debug.LogError("PaperBallNote: Assign the paper ball's existing Collider. No collider was created by this script.", this);
    }

    private void Update()
    {
        if (player == null) player = FindObjectOfType<FirstPersonPlayer>();
        if (player != null && player.PlayerCamera != null) viewCamera = player.PlayerCamera;
        if (displaying)
        {
            if (Input.GetMouseButtonDown(0))
                displaying = false;
            return;
        }

        if (TryShowApproachLore()) return;

        if (!Input.GetMouseButtonDown(0) || viewCamera == null || paperBallCollider == null)
            return;

        Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
        if (!paperBallCollider.Raycast(ray, out _, 100f)) return;
        Transform origin = player != null ? player.transform : viewCamera.transform;
        if (Vector3.Distance(origin.position, paperBallCollider.ClosestPoint(origin.position)) > interactionDistance) return;
        displaying = true;
    }

    private bool TryShowApproachLore()
    {
        if (approachLoreShown || paperBallCollider == null || player == null) return false;
        if (Vector3.Distance(player.transform.position, paperBallCollider.ClosestPoint(player.transform.position)) > approachLoreDistance)
            return false;

        WorldStartTerminal terminal = FindObjectOfType<WorldStartTerminal>();
        if (terminal == null || !terminal.IsGameStarted) return false;

        approachLoreShown = true;
        terminal.ShowLoreMessage(
            "OBSERVATION PROTOCOL",
            "YOU ONCE SOLD YOUR ABILITY TO SEE THE WORLD.\n\nNOW, YOU MUST LEARN TO OBSERVE AGAIN.\n\nTHE MOST IMPORTANT INFORMATION RARELY APPEARS IN THE MOST OBVIOUS PLACES.");
        return true;
    }

    private void OnGUI()
    {
        if (!displaying || noteTexture == null) return;
        float width = Mathf.Min(Screen.width * maximumScreenWidth, maximumWidthPixels);
        float height = width / Mathf.Max(.01f, cardAspect);
        float heightLimit = Screen.height * maximumScreenHeight;
        if (height > heightLimit)
        {
            height = heightLimit;
            width = height * cardAspect;
        }

        Rect card = new Rect(
            (Screen.width - width) * .5f,
            Mathf.Clamp(Screen.height * .58f - height * .5f, 14f, Screen.height - height - 34f),
            width,
            height);
        GUI.DrawTexture(card, Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
        GUI.DrawTexture(card, noteTexture, ScaleMode.ScaleToFit, true);
    }
}
