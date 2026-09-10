using System.Collections;
using UnityEngine;

/// <summary>
/// Owns all interaction logic for the GlassPuzzle hierarchy.
/// The child named Glass is treated as replaceable visual content.
/// </summary>
public sealed class GlassPuzzleController : MonoBehaviour
{
    [Header("Hierarchy References")]
    [Tooltip("The replaceable Glass visual child.")]
    [SerializeField] private Transform glassTarget;
    [SerializeField] private Camera interactionCamera;
    [SerializeField] private VisionBlurEffect blurEffect;

    [Header("Interaction")]
    [Tooltip("Maximum distance between Player and this puzzle for clicking.")]
    [SerializeField, Min(0.1f)] private float interactionDistance = 5f;
    [Tooltip("Seconds for the screen to become completely clear.")]
    [SerializeField, Min(0.01f)] private float clearDuration = 2f;
    [SerializeField, Min(1f)] private float clickRayDistance = 100f;
    [Tooltip("Hide the replaceable Glass visual immediately after it is picked up.")]
    [SerializeField] private bool hideGlassOnPickup = true;

    [Header("Diagnostics")]
    [Tooltip("Writes click-by-click pickup diagnostics to the Unity Console.")]
    [SerializeField] private bool enableDebugLogs = true;

    private Transform player;
    private Collider pickupCollider;
    private bool solved;

    private void Awake()
    {
        if (glassTarget == null)
        {
            glassTarget = transform.Find("Glass");

            if (glassTarget == null && transform.childCount > 0)
            {
                glassTarget = transform.GetChild(0);
            }
        }

        if (interactionCamera == null)
        {
            interactionCamera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
        }

        if (blurEffect == null && interactionCamera != null)
        {
            blurEffect = interactionCamera.GetComponent<VisionBlurEffect>();
        }

        FirstPersonPlayer playerController = FindObjectOfType<FirstPersonPlayer>();
        if (playerController != null)
        {
            player = playerController.transform;
        }

        // The Glass frame owns this collider; it is added manually in the Inspector.
        if (glassTarget != null)
        {
            pickupCollider = glassTarget.GetComponentInChildren<Collider>(true);
        }

        ValidateSetup();
        Log($"Ready. Visual='{NameOf(glassTarget)}', collider='{NameOf(pickupCollider)}' (enabled={pickupCollider != null && pickupCollider.enabled}, trigger={pickupCollider != null && pickupCollider.isTrigger}), camera='{NameOf(interactionCamera)}', blur={(blurEffect != null)}, player='{NameOf(player)}'.");
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (solved)
        {
            Log("Click ignored: this GlassPuzzle was already picked up.");
            return;
        }

        if (player == null || interactionCamera == null || blurEffect == null || pickupCollider == null)
        {
            Log("Click blocked: missing " +
                (player == null ? "Player " : string.Empty) +
                (interactionCamera == null ? "Camera " : string.Empty) +
                (blurEffect == null ? "VisionBlurEffect " : string.Empty) +
                (pickupCollider == null ? "Collider" : string.Empty));
            return;
        }

        Vector3 closestPoint = pickupCollider.ClosestPoint(player.position);
        float distance = Vector3.Distance(player.position, closestPoint);
        if (distance > interactionDistance)
        {
            Log($"Click blocked: player is {distance:F2} units away; allowed distance is {interactionDistance:F2}.");
            return;
        }

        Ray ray = interactionCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, clickRayDistance, ~0, QueryTriggerInteraction.Collide))
        {
            Log("Click blocked: raycast hit nothing. Check that the Collider is enabled and that the mouse is over the glasses.");
            return;
        }

        bool hitPickupCollider = hit.collider == pickupCollider || hit.collider.transform.IsChildOf(pickupCollider.transform);
        if (!hitPickupCollider)
        {
            Log($"Click blocked: ray hit '{NameOf(hit.collider)}' instead of the glasses collider '{NameOf(pickupCollider)}'.");
            return;
        }

        solved = true;
        Log($"Pickup succeeded: ray hit '{NameOf(hit.collider)}'. Hiding glasses and clearing blur over {clearDuration:F2} seconds.");

        if (hideGlassOnPickup && glassTarget != null)
        {
            glassTarget.gameObject.SetActive(false);
        }

        pickupCollider.enabled = false;
        StartCoroutine(ClearVision());
        StartCoroutine(ShowVisualLoreAfterDelay());
    }

    private IEnumerator ClearVision()
    {
        float initialRadius = blurEffect.BlurRadius;
        float elapsed = 0f;

        while (elapsed < clearDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / clearDuration);
            // SmoothStep makes the improvement obvious while avoiding a hard visual cut.
            blurEffect.BlurRadius = Mathf.Lerp(initialRadius, 0f, Mathf.SmoothStep(0f, 1f, progress));
            yield return null;
        }

        blurEffect.BlurRadius = 0f;
    }

    private IEnumerator ShowVisualLoreAfterDelay()
    {
        // This is deliberately independent from the blur animation: the first
        // lore line should arrive one second after collection, not after the
        // full vision-recovery duration.
        yield return new WaitForSeconds(1f);
        WorldStartTerminal terminal = FindObjectOfType<WorldStartTerminal>();
        if (terminal != null)
        {
            terminal.ShowLoreMessage(
                "VISUAL LINK RESTORED",
                "YOU CAN FINALLY SEE CLEARLY. BUT SEEING DOES NOT MEAN UNDERSTANDING.");
        }
    }

    private void ValidateSetup()
    {
        if (glassTarget == null)
            Debug.LogError("GlassPuzzle: child 'Glass' was not found.", this);
        if (interactionCamera == null)
            Debug.LogError("GlassPuzzle: interaction Camera is missing.", this);
        if (blurEffect == null)
            Debug.LogError("GlassPuzzle: VisionBlurEffect reference is missing.", this);
        if (pickupCollider == null)
            Debug.LogError("GlassPuzzle: add a Collider manually to the Glass frame or one of its children.", this);
        if (player == null)
            Debug.LogError("GlassPuzzle: FirstPersonPlayer was not found.", this);
    }

    private void Log(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log("[GlassPuzzle] " + message, this);
        }
    }

    private static string NameOf(Object target)
    {
        return target != null ? target.name : "<missing>";
    }
}
