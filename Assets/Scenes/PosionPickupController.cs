using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
/// <summary>Click-to-pickup behaviour for the PosionPuzzle functional parent.</summary>
public sealed class PosionPickupController : MonoBehaviour
{
    [SerializeField] private Collider pickupCollider;
    [SerializeField, Min(.1f)] private float interactionDistance = 12f;
    [SerializeField] private bool autoFitParentCollider = true;

    private Camera viewCamera;
    private FirstPersonPlayer player;
    private bool pickedUp;

    public bool IsPickedUp => pickedUp;
    public event System.Action PickedUp;

    private void Awake()
    {
        player = FindObjectOfType<FirstPersonPlayer>();
        // Never use the surveillance camera.  Pickups must raycast from the
        // camera that the player is currently looking through.
        viewCamera = player != null && player.PlayerCamera != null
            ? player.PlayerCamera
            : (Camera.main != null ? Camera.main : FindObjectOfType<Camera>());
        if (pickupCollider == null) pickupCollider = GetComponent<Collider>();
        if (autoFitParentCollider && pickupCollider is BoxCollider boxCollider)
            FitColliderToChildVisuals(boxCollider);
        if (pickupCollider == null)
            Debug.LogError("PosionPickup: Assign posion's existing Collider.", this);
    }

    private void FitColliderToChildVisuals(BoxCollider boxCollider)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        Bounds worldBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) worldBounds.Encapsulate(renderers[i].bounds);

        Vector3 min = worldBounds.min;
        Vector3 max = worldBounds.max;
        Bounds localBounds = new Bounds(transform.InverseTransformPoint(min), Vector3.zero);
        for (int x = 0; x <= 1; x++)
        for (int y = 0; y <= 1; y++)
        for (int z = 0; z <= 1; z++)
        {
            localBounds.Encapsulate(transform.InverseTransformPoint(new Vector3(
                x == 0 ? min.x : max.x,
                y == 0 ? min.y : max.y,
                z == 0 ? min.z : max.z)));
        }

        boxCollider.center = localBounds.center;
        boxCollider.size = localBounds.size;
        boxCollider.isTrigger = true;
    }

    private void Update()
    {
        if (pickedUp || !Input.GetMouseButtonDown(0) || viewCamera == null || pickupCollider == null)
            return;

        Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
        if (!pickupCollider.Raycast(ray, out _, 100f)) return;
        Transform origin = player != null ? player.transform : viewCamera.transform;
        if (Vector3.Distance(origin.position, pickupCollider.ClosestPoint(origin.position)) > interactionDistance) return;

        pickedUp = true;
        // Keep this functional parent alive: PoisonUseController needs it after
        // collection to play the cap and pouring sequence. Only the world pickup
        // appearance and its collider are removed.
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            renderer.enabled = false;
        pickupCollider.enabled = false;
        HoverScanHighlight highlight = GetComponent<HoverScanHighlight>();
        if (highlight != null) highlight.enabled = false;
        PickedUp?.Invoke();
    }
}
