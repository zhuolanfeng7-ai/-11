using System.Collections;
using UnityEngine;

/// <summary>
/// Restores the world's color when a player carrying the battery clicks the crystal ball.
/// All interaction logic stays on the CrystalBallPuzzle parent.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public sealed class CrystalBallPuzzleController : MonoBehaviour
{
    [Header("Hierarchy References")]
    [SerializeField] private Transform crystalBallVisual;
    [SerializeField] private FirstPersonPlayer player;
    [SerializeField] private Camera interactionCamera;
    [SerializeField] private VisionBlurEffect visionEffect;

    [Header("Interaction")]
    [SerializeField, Min(0.1f)] private float interactionDistance = 8f;
    [SerializeField, Min(1f)] private float clickRayDistance = 100f;
    [Tooltip("Seconds for black and white to transition back to full color.")]
    [SerializeField, Min(0.01f)] private float colorRestoreDuration = 2f;
    [Tooltip("Allows activation by clicking while close when imported art colliders are unreliable.")]
    [SerializeField] private bool assistClickWhenInRange = true;

    [Header("Replaceable Art Support")]
    [SerializeField] private bool autoFitInteractionCollider = true;

    private BoxCollider interactionCollider;
    private bool activated;

    private void Awake()
    {
        interactionCollider = GetComponent<BoxCollider>();

        if (crystalBallVisual == null)
        {
            crystalBallVisual = transform.Find("CrystalBall");
            if (crystalBallVisual == null && transform.childCount > 0)
            {
                crystalBallVisual = transform.GetChild(0);
            }
        }

        if (player == null)
            player = FindObjectOfType<FirstPersonPlayer>();
        if (interactionCamera == null)
            interactionCamera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
        if (visionEffect == null && interactionCamera != null)
            visionEffect = interactionCamera.GetComponent<VisionBlurEffect>();

        if (autoFitInteractionCollider)
            FitColliderToVisuals();

        ValidateSetup();
    }

    private void Update()
    {
        if (activated || player == null || interactionCamera == null ||
            visionEffect == null || interactionCollider == null)
        {
            return;
        }

        Vector3 closestPoint = interactionCollider.ClosestPoint(player.transform.position);
        float distance = Vector3.Distance(player.transform.position, closestPoint);
        if (distance > interactionDistance || !Input.GetMouseButtonDown(0))
        {
            return;
        }

        Ray ray = interactionCamera.ScreenPointToRay(Input.mousePosition);
        bool directlyHitCrystalBall = interactionCollider.Raycast(ray, out RaycastHit hit, clickRayDistance);
        if (!directlyHitCrystalBall && !assistClickWhenInRange)
        {
            Debug.LogWarning("[CrystalBallPuzzle] Click missed the crystal ball collider.", this);
            return;
        }

        if (!player.HasBattery)
        {
            Debug.Log("[CrystalBallPuzzle] Crystal ball needs a battery. HasBattery = false", this);
            return;
        }

        activated = true;
        interactionCollider.enabled = false;
        Debug.Log(
            $"[CrystalBallPuzzle] Activated with battery. DirectHit={directlyHitCrystalBall}. " +
            $"Restoring color over {colorRestoreDuration:F2}s.", this);
        StartCoroutine(RestoreWorldColor());
    }

    private IEnumerator RestoreWorldColor()
    {
        float initialSaturation = visionEffect.Saturation;
        float elapsed = 0f;

        while (elapsed < colorRestoreDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / colorRestoreDuration);
            visionEffect.Saturation = Mathf.Lerp(
                initialSaturation, 1f, Mathf.SmoothStep(0f, 1f, progress));
            yield return null;
        }

        visionEffect.Saturation = 1f;
        Debug.Log("[CrystalBallPuzzle] World color restored. Saturation = 1", this);
    }

    private void FitColliderToVisuals()
    {
        Transform visualRoot = crystalBallVisual != null ? crystalBallVisual : transform;
        Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        Bounds worldBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            worldBounds.Encapsulate(renderers[i].bounds);

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
        if (crystalBallVisual == null)
            Debug.LogError("CrystalBallPuzzle: CrystalBall child is missing.", this);
        if (player == null)
            Debug.LogError("CrystalBallPuzzle: FirstPersonPlayer is missing.", this);
        if (interactionCamera == null)
            Debug.LogError("CrystalBallPuzzle: interaction Camera is missing.", this);
        if (visionEffect == null)
            Debug.LogError("CrystalBallPuzzle: VisionBlurEffect is missing.", this);
    }
}
