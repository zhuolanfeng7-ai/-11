using System.Collections;
using UnityEngine;

/// <summary>
/// Toggles drawerUp when its Handrail Up child collider is clicked.
/// This component belongs on the drawerUp parent, never on the replaceable handle art.
/// </summary>
public sealed class DrawerUpController : MonoBehaviour
{
    [Header("Hierarchy References")]
    [SerializeField] private Collider handrailCollider;
    [SerializeField] private Camera interactionCamera;
    [SerializeField] private FirstPersonPlayer player;

    [Header("Drawer Movement")]
    [Tooltip("Distance moved along drawerUp's negative local Y axis.")]
    [SerializeField, Min(0f)] private float moveDistance = 3.5f;
    [Tooltip("Approximate movement speed in local units per second.")]
    [SerializeField, Min(0.01f)] private float moveSpeed = 2f;

    [Header("Interaction")]
    [SerializeField, Min(0.1f)] private float interactionDistance = 8f;
    [SerializeField, Min(1f)] private float clickRayDistance = 100f;

    private Vector3 closedLocalPosition;
    private bool opened;
    private bool moving;

    private void Awake()
    {
        closedLocalPosition = transform.localPosition;

        if (interactionCamera == null)
            interactionCamera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
        if (player == null)
            player = FindObjectOfType<FirstPersonPlayer>();

        if (handrailCollider == null)
        {
            Transform handle = transform.Find("Handrail Up");
            if (handle != null)
                handrailCollider = handle.GetComponent<Collider>();
        }

        if (handrailCollider == null)
            Debug.LogError("drawerUp: Handrail Up Collider is missing.", this);
    }

    private void Update()
    {
        if (moving || handrailCollider == null ||
            interactionCamera == null || player == null || !Input.GetMouseButtonDown(0))
        {
            return;
        }

        Vector3 closestPoint = handrailCollider.ClosestPoint(player.transform.position);
        if (Vector3.Distance(player.transform.position, closestPoint) > interactionDistance)
        {
            return;
        }

        Ray ray = interactionCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, clickRayDistance, ~0, QueryTriggerInteraction.Ignore) ||
            hit.collider != handrailCollider)
        {
            return;
        }

        StartCoroutine(MoveDrawer(!opened));
    }

    private IEnumerator MoveDrawer(bool open)
    {
        moving = true;
        Vector3 start = transform.localPosition;
        Vector3 target = open
            ? closedLocalPosition + Vector3.down * moveDistance
            : closedLocalPosition;
        float travelDistance = Vector3.Distance(start, target);
        float duration = travelDistance / Mathf.Max(0.01f, moveSpeed);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            transform.localPosition = Vector3.LerpUnclamped(start, target, easedProgress);
            yield return null;
        }

        transform.localPosition = target;
        moving = false;
        opened = open;
        Debug.Log(open
            ? "[drawerUp] Drawer opened."
            : "[drawerUp] Drawer returned to its original position.", this);
    }
}
