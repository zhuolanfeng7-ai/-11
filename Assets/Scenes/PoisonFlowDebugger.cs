using UnityEngine;

/// <summary>
/// Console-only diagnostics for the poison -> vase/rose sequence.
/// It observes state and never changes game behaviour.
/// </summary>
public sealed class PoisonFlowDebugger : MonoBehaviour
{
    [SerializeField, Min(.2f)] private float snapshotInterval = 1f;
    [SerializeField] private bool logEveryLeftClick = true;

    private PosionPickupController pickup;
    private PoisonUseController poisonUse;
    private FirstPersonPlayer player;
    private Camera viewCamera;
    private float nextSnapshotTime;
    private string lastSnapshot;

    private void Awake()
    {
        pickup = FindObjectOfType<PosionPickupController>();
        poisonUse = FindObjectOfType<PoisonUseController>();
        player = FindObjectOfType<FirstPersonPlayer>();
        viewCamera = player != null ? player.PlayerCamera : Camera.main;
        Debug.Log("[Poison Debug] Monitor active. Watching pickup, target distance, ray hits, and pour phases.", this);
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextSnapshotTime)
        {
            nextSnapshotTime = Time.unscaledTime + snapshotInterval;
            LogSnapshot();
        }
        if (logEveryLeftClick && Input.GetMouseButtonDown(0)) LogClick();
    }

    private void LogSnapshot()
    {
        bool collected = pickup != null && pickup.IsPickedUp;
        float maxDistance = poisonUse != null ? poisonUse.DebugUseDistance : -1f;
        string snapshot = $"pickup={collected} | vase={DistanceText(poisonUse != null ? poisonUse.DebugVase : null, maxDistance)} | rose={DistanceText(poisonUse != null ? poisonUse.DebugRose : null, maxDistance)} | phase={(poisonUse != null ? poisonUse.DebugPhase : "PoisonUseController MISSING")} | busy={(poisonUse != null && poisonUse.DebugIsBusy)} | wilted={(poisonUse != null && poisonUse.DebugRoseWilted)}";
        if (snapshot == lastSnapshot) return;
        lastSnapshot = snapshot;
        Debug.Log("[Poison Debug] " + snapshot, this);
    }

    private void LogClick()
    {
        if (viewCamera == null)
        {
            Debug.LogWarning("[Poison Debug] Left click: player camera is missing.", this);
            return;
        }
        Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
        string hitName = Physics.Raycast(ray, out RaycastHit hit, 100f, ~0, QueryTriggerInteraction.Collide)
            ? $"'{hit.collider.name}' at {hit.distance:F2}"
            : "nothing";
        Transform vase = poisonUse != null ? poisonUse.DebugVase : null;
        Transform rose = poisonUse != null ? poisonUse.DebugRose : null;
        Debug.Log($"[Poison Debug] LEFT CLICK | ray hit: {hitName} | pointer in Vase bounds={PointerTouches(vase)} | pointer in Rose bounds={PointerTouches(rose)} | pickup={(pickup != null && pickup.IsPickedUp)} | phase={(poisonUse != null ? poisonUse.DebugPhase : "missing")}", this);
    }

    private string DistanceText(Transform target, float maximum)
    {
        if (target == null) return "MISSING";
        if (player == null) return "player MISSING";
        float distance = Vector3.Distance(player.transform.position, GetBounds(target).center);
        return $"{distance:F2}/{maximum:F2} {(maximum >= 0f && distance <= maximum ? "OK" : "TOO FAR")}";
    }

    private bool PointerTouches(Transform target)
    {
        if (target == null || viewCamera == null) return false;
        Bounds b = GetBounds(target);
        Vector3 min = b.min, max = b.max;
        float left = float.PositiveInfinity, right = float.NegativeInfinity, bottom = float.PositiveInfinity, top = float.NegativeInfinity;
        bool visible = false;
        for (int x = 0; x <= 1; x++) for (int y = 0; y <= 1; y++) for (int z = 0; z <= 1; z++)
        {
            Vector3 p = viewCamera.WorldToScreenPoint(new Vector3(x == 0 ? min.x : max.x, y == 0 ? min.y : max.y, z == 0 ? min.z : max.z));
            if (p.z <= 0f) continue;
            visible = true;
            left = Mathf.Min(left, p.x); right = Mathf.Max(right, p.x);
            bottom = Mathf.Min(bottom, p.y); top = Mathf.Max(top, p.y);
        }
        return visible && new Rect(left, bottom, right - left, top - bottom).Contains(Input.mousePosition);
    }

    private static Bounds GetBounds(Transform target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(target.position, Vector3.one);
        Bounds result = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) result.Encapsulate(renderers[i].bounds);
        return result;
    }
}
