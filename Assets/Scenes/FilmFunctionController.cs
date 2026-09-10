using UnityEngine;

/// <summary>
/// Owns the Film pickup and its use on the television.
/// Attach to the stable FilmFunction parent, never to the replaceable Film mesh.
/// </summary>
public sealed class FilmFunctionController : MonoBehaviour
{
    [Header("Hierarchy References")]
    [SerializeField] private Transform filmVisualRoot;
    [SerializeField] private Collider filmCollider;
    [SerializeField] private Collider screenCollider;

    [Header("Existing Systems")]
    [SerializeField] private Camera interactionCamera;
    [SerializeField] private FirstPersonPlayer player;
    [SerializeField] private AntennaPuzzleController antennaPuzzle;

    [Header("Interaction")]
    [SerializeField, Min(0.1f)] private float interactionDistance = 12f;
    [SerializeField, Min(1f)] private float clickRayDistance = 100f;
    [SerializeField, Min(.1f)] private float approachLoreDistance = 8f;

    private bool pickedUp;
    private bool approachLoreShown;

    private void Awake()
    {
        if (interactionCamera == null)
            interactionCamera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
        if (player == null)
            player = FindObjectOfType<FirstPersonPlayer>();
        if (antennaPuzzle == null)
            antennaPuzzle = FindObjectOfType<AntennaPuzzleController>();
        if (filmVisualRoot == null)
            filmVisualRoot = transform.Find("film pick up");
        if (filmCollider == null && filmVisualRoot != null)
            filmCollider = filmVisualRoot.GetComponentInChildren<Collider>(true);
        if (filmCollider == null)
            Debug.LogError("FilmFunction: Please add a Collider manually to the Film object.", this);
        if (screenCollider == null)
        {
            GameObject screen = GameObject.Find("screem");
            if (screen != null)
                screenCollider = screen.GetComponent<Collider>();
        }

        pickedUp = player != null && player.HasFilm;
        if (pickedUp && filmVisualRoot != null)
            filmVisualRoot.gameObject.SetActive(false);
    }

    private void Update()
    {
        TryShowApproachLore();
        if (!Input.GetMouseButtonDown(0) || interactionCamera == null || player == null)
            return;

        Ray ray = interactionCamera.ScreenPointToRay(Input.mousePosition);
        if (!pickedUp && IsClicked(ray, filmCollider))
        {
            pickedUp = true;
            player.GiveFilm();
            if (filmVisualRoot != null)
                filmVisualRoot.gameObject.SetActive(false);
            Debug.Log("[FilmFunction] Film picked up.", this);
            return;
        }

        if (pickedUp && IsClicked(ray, screenCollider) && antennaPuzzle != null)
        {
            if (!antennaPuzzle.TryInstallFilm())
                Debug.Log("[FilmFunction] Film is held, but the TV is not ready for installation yet.", this);
        }
    }

    private void TryShowApproachLore()
    {
        if (approachLoreShown || pickedUp || filmCollider == null || player == null) return;
        if (Vector3.Distance(player.transform.position, filmCollider.ClosestPoint(player.transform.position)) > approachLoreDistance)
            return;

        WorldStartTerminal terminal = FindObjectOfType<WorldStartTerminal>();
        if (terminal == null || !terminal.IsGameStarted) return;

        approachLoreShown = true;
        terminal.ShowLoreMessage(
            "ADDITIONAL VISUAL DATA",
            "ADDITIONAL VISUAL DATA DETECTED.\n\nAN UNREAD MEMORY FILE STILL EXISTS.\n\nUNLIKE DIGITAL RECORDS...\nTHIS MEMORY HAS BEEN PRESERVED IN ITS MOST ORIGINAL FORM.");
    }

    private bool IsClicked(Ray ray, Collider target)
    {
        if (target == null)
            return false;

        Vector3 closest = target.ClosestPoint(player.transform.position);
        if (Vector3.Distance(player.transform.position, closest) > interactionDistance)
            return false;

        // Film lives inside a drawer; the drawer's collider can block a global
        // raycast even while the Film is visibly clickable. Test this exact collider.
        return target.Raycast(ray, out _, clickRayDistance);
    }

}
