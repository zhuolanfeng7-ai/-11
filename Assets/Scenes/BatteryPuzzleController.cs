using UnityEngine;

/// <summary>
/// Owns the BatteryPuzzle pickup logic. The Battery child is replaceable art.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public sealed class BatteryPuzzleController : MonoBehaviour
{
    [Header("Hierarchy References")]
    [SerializeField] private Transform batteryVisual;
    [SerializeField] private FirstPersonPlayer player;
    [SerializeField] private Camera interactionCamera;

    [Header("Interaction")]
    [SerializeField, Min(0.1f)] private float interactionDistance = 8f;
    [SerializeField, Min(1f)] private float clickRayDistance = 100f;

    [Header("Replaceable Art Support")]
    [SerializeField] private bool autoFitInteractionCollider = true;

    private BoxCollider interactionCollider;
    private bool pickedUp;

    private void Awake()
    {
        interactionCollider = GetComponent<BoxCollider>();

        if (batteryVisual == null)
        {
            batteryVisual = transform.Find("Battery");
            if (batteryVisual == null && transform.childCount > 0)
            {
                batteryVisual = transform.GetChild(0);
            }
        }

        if (player == null)
        {
            player = FindObjectOfType<FirstPersonPlayer>();
        }

        if (interactionCamera == null)
        {
            interactionCamera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
        }

        if (autoFitInteractionCollider)
        {
            FitColliderToVisuals();
        }

        ValidateSetup();
    }

    private void Update()
    {
        if (pickedUp || player == null || interactionCamera == null || interactionCollider == null)
        {
            return;
        }

        Vector3 closestPoint = interactionCollider.ClosestPoint(player.transform.position);
        float playerDistance = Vector3.Distance(player.transform.position, closestPoint);
        if (playerDistance > interactionDistance)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Debug.LogWarning(
                    $"[BatteryPuzzle] Pickup failed: distance {playerDistance:F2} > {interactionDistance:F2}.", this);
            }
            return;
        }

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        Ray ray = interactionCamera.ScreenPointToRay(Input.mousePosition);
        // Test this puzzle's collider directly so overlapping furniture colliders
        // cannot steal the click before the BatteryPuzzle receives it.
        if (!interactionCollider.Raycast(ray, out RaycastHit hit, clickRayDistance))
        {
            Debug.LogWarning("[BatteryPuzzle] Pickup failed: mouse ray missed the BatteryPuzzle collider.", this);
            return;
        }

        PickUpBattery();
    }

    private void PickUpBattery()
    {
        pickedUp = true;
        player.GiveBattery();
        HideBatteryVisuals();
        interactionCollider.enabled = false;
        Debug.Log("[BatteryPuzzle] Battery picked up successfully.", this);
    }

    private void HideBatteryVisuals()
    {
        // Disable every renderer first so the pickup stays invisible even if the
        // imported prefab contains unexpected nested visual objects.
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer visualRenderer in renderers)
        {
            visualRenderer.enabled = false;
        }

        // All replaceable art belongs under BatteryPuzzle, so deactivate every
        // direct visual child without disabling this controller or its coroutine.
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }

        Debug.Log(
            $"[BatteryPuzzle] Hidden {renderers.Length} renderer(s) and " +
            $"{transform.childCount} visual child object(s).", this);
    }

    private void FitColliderToVisuals()
    {
        Transform visualRoot = batteryVisual != null ? batteryVisual : transform;
        Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds worldBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            worldBounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 min = worldBounds.min;
        Vector3 max = worldBounds.max;
        Bounds localBounds = new Bounds(transform.InverseTransformPoint(min), Vector3.zero);

        for (int x = 0; x <= 1; x++)
        for (int y = 0; y <= 1; y++)
        for (int z = 0; z <= 1; z++)
        {
            Vector3 corner = new Vector3(
                x == 0 ? min.x : max.x,
                y == 0 ? min.y : max.y,
                z == 0 ? min.z : max.z);
            localBounds.Encapsulate(transform.InverseTransformPoint(corner));
        }

        interactionCollider.center = localBounds.center;
        interactionCollider.size = localBounds.size;
        interactionCollider.isTrigger = true;
    }

    private void ValidateSetup()
    {
        if (batteryVisual == null)
            Debug.LogError("BatteryPuzzle: Battery child is missing.", this);
        if (player == null)
            Debug.LogError("BatteryPuzzle: FirstPersonPlayer is missing.", this);
        if (interactionCamera == null)
            Debug.LogError("BatteryPuzzle: interaction Camera is missing.", this);
    }
}
