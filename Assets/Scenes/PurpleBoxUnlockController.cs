using System.Collections;
using UnityEngine;

/// <summary>
/// Unlocks the purple box only after the wall key has been collected.  The lock
/// position is calculated from the clicked face, rather than a fixed world point.
/// </summary>
public sealed class PurpleBoxUnlockController : MonoBehaviour
{
    [SerializeField, Min(.1f)] private float interactionDistance = 5f;
    [SerializeField, Min(.1f)] private float approachDuration = 1.15f;
    [SerializeField, Min(.1f)] private float turnDuration = .85f;
    [SerializeField, Min(.1f)] private float lidOpenDuration = 1.0f;

    private Camera viewCamera;
    private FirstPersonPlayer player;
    private bool unlocking;
    private bool unlocked;

    private void Awake()
    {
        player = FindObjectOfType<FirstPersonPlayer>();
        viewCamera = player != null ? player.PlayerCamera : Camera.main;
    }

    private void Update()
    {
        if (unlocked || unlocking || !Input.GetMouseButtonDown(0)) return;
        if (viewCamera == null) viewCamera = player != null ? player.PlayerCamera : Camera.main;
        if (viewCamera == null) return;

        Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, ~0, QueryTriggerInteraction.Collide)) return;
        if (hit.transform != transform && !hit.transform.IsChildOf(transform)) return;
        if (!KeyPickupHandler.HasCollected)
        {
            Debug.Log("[PurpleBox] Click ignored: wall key has not been collected.");
            return;
        }

        Transform distanceOrigin = player != null ? player.transform : viewCamera.transform;
        if (Vector3.Distance(distanceOrigin.position, hit.collider.ClosestPoint(distanceOrigin.position)) > interactionDistance) return;
        Debug.Log("[PurpleBox] Key accepted. Starting unlock animation.");
        StartCoroutine(UnlockAt(GetLockCore(hit.collider, -ray.direction), -ray.direction));
    }

    private Vector3 GetLockCore(Collider collider, Vector3 faceNormal)
    {
        // "22" is the explicit lock assembly under purplr box in this scene.
        // The actual black keyhole is on the lower centre of that assembly.
        Transform lockAssembly = FindLockAssembly();
        if (lockAssembly != null)
        {
            Renderer lockRenderer = lockAssembly.GetComponentInChildren<Renderer>(true);
            if (lockRenderer == null) return lockAssembly.position;
            Bounds lockBounds = lockRenderer.bounds;
            Vector3 keyhole = PointOnFacingSurface(lockBounds, faceNormal);
            // The latch occupies the upper half; the black keyhole is below it.
            return keyhole - lockAssembly.up * (lockBounds.extents.y * .38f);
        }

        // First use a named lock/keyhole sub-mesh when the source model provides one.
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            string name = child.name.ToLowerInvariant();
            if (!name.Contains("lock") && !name.Contains("keyhole") && !name.Contains("key hole")) continue;
            Renderer lockRenderer = child.GetComponentInChildren<Renderer>(true);
            if (lockRenderer != null) return lockRenderer.bounds.center;
        }

        // The supplied lock is a vertical plate: the keyhole sits on its centre
        // line, below the latch.  This fallback places the key at that opening,
        // not at the centre of the whole purple box.
        Bounds bounds = collider.bounds;
        Vector3 core = bounds.center;
        Vector3 normal = faceNormal.normalized;
        Vector3 absolute = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));
        if (absolute.x >= absolute.y && absolute.x >= absolute.z)
            core.x = normal.x >= 0f ? bounds.max.x : bounds.min.x;
        else if (absolute.y >= absolute.z)
            core.y = normal.y >= 0f ? bounds.max.y : bounds.min.y;
        else
            core.z = normal.z >= 0f ? bounds.max.z : bounds.min.z;
        core -= transform.up * (bounds.extents.y * .34f);
        return core;
    }

    private Transform FindLockAssembly()
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
            if (child.name == "22") return child;
        return null;
    }

    private Transform FindPurpleLid()
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            string name = child.name.ToLowerInvariant();
            if (name == "purple up" || name == "purplr up" || name == "purplr upx" || name == "purple upx")
                return child;
        }
        return null;
    }

    private IEnumerator OpenPurpleLid()
    {
        Transform lid = FindPurpleLid();
        if (lid == null)
        {
            Debug.LogWarning("[PurpleBox] Unlock complete, but no 'purple up' lid was found.");
            yield break;
        }

        // A suitcase lid must pivot along its rear lower edge, rather than spin
        // around its visual centre.  Build that hinge without editing the scene.
        Transform hinge = CreateLidHinge(lid);
        Quaternion closedRotation = hinge.localRotation;
        Quaternion openRotation = closedRotation * Quaternion.Euler(-90f, 0f, 0f);
        float elapsed = 0f;
        while (elapsed < lidOpenDuration)
        {
            elapsed += Time.deltaTime;
            hinge.localRotation = Quaternion.Slerp(closedRotation, openRotation, Mathf.SmoothStep(0f, 1f, elapsed / lidOpenDuration));
            yield return null;
        }
        hinge.localRotation = openRotation;
        Debug.Log("[PurpleBox] purple up opened 90 degrees.");
    }

    private static Transform CreateLidHinge(Transform lid)
    {
        Bounds bounds = GetCombinedBounds(lid);
        Vector3 rear = -lid.forward;
        float rearRadius = ProjectedBoundsRadius(bounds, rear);
        float downRadius = ProjectedBoundsRadius(bounds, -lid.up);
        Vector3 hingePosition = bounds.center + rear * rearRadius - lid.up * downRadius;

        GameObject hingeObject = new GameObject("PurpleUpHingeRuntime");
        Transform hinge = hingeObject.transform;
        Transform originalParent = lid.parent;
        hinge.SetParent(originalParent, true);
        hinge.SetPositionAndRotation(hingePosition, lid.rotation);
        lid.SetParent(hinge, true);
        return hinge;
    }

    private static Bounds GetCombinedBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(root.position, Vector3.one * .01f);
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static float ProjectedBoundsRadius(Bounds bounds, Vector3 direction)
    {
        Vector3 abs = new Vector3(Mathf.Abs(direction.x), Mathf.Abs(direction.y), Mathf.Abs(direction.z));
        return bounds.extents.x * abs.x + bounds.extents.y * abs.y + bounds.extents.z * abs.z;
    }

    private Transform GetKeyholeAnchor(Vector3 keyholePosition, Vector3 outwardNormal)
    {
        Transform lockAssembly = FindLockAssembly();
        if (lockAssembly == null) lockAssembly = transform;
        Transform anchor = lockAssembly.Find("KeyholeAnchorRuntime");
        if (anchor == null)
        {
            GameObject anchorObject = new GameObject("KeyholeAnchorRuntime");
            anchor = anchorObject.transform;
            anchor.SetParent(lockAssembly, true);
        }

        // Local +Z points into the hole.  The key's shaft is aligned to +Z.
        Vector3 intoHole = -outwardNormal.normalized;
        Vector3 up = lockAssembly.up;
        if (Mathf.Abs(Vector3.Dot(intoHole, up)) > .95f) up = Vector3.up;
        anchor.SetPositionAndRotation(keyholePosition, Quaternion.LookRotation(intoHole, up));
        return anchor;
    }

    private static Vector3 PointOnFacingSurface(Bounds bounds, Vector3 faceNormal)
    {
        Vector3 point = bounds.center;
        Vector3 normal = faceNormal.normalized;
        Vector3 absolute = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));
        if (absolute.x >= absolute.y && absolute.x >= absolute.z)
            point.x = normal.x >= 0f ? bounds.max.x : bounds.min.x;
        else if (absolute.y >= absolute.z)
            point.y = normal.y >= 0f ? bounds.max.y : bounds.min.y;
        else
            point.z = normal.z >= 0f ? bounds.max.z : bounds.min.z;
        return point;
    }

    private IEnumerator UnlockAt(Vector3 clickedFacePoint, Vector3 faceNormal)
    {
        unlocking = true;
        GameObject key = KeyPickupHandler.CreateUnlockVisual();
        if (key == null)
        {
            Debug.LogWarning("[PurpleBox] Unlock cancelled: no key visual could be created.");
            unlocking = false;
            yield break;
        }

        // The clicked point is on the visible face: it is the lock core for this interaction.
        Vector3 normal = faceNormal.sqrMagnitude > .001f ? faceNormal.normalized : transform.forward;
        // Key's forward axis points directly into the keyhole.
        float tipDistance = KeyPickupHandler.GetForwardTipDistance(key);
        Transform keyholeAnchor = GetKeyholeAnchor(clickedFacePoint, normal);
        // Preserve the already-normalised world size when placing it under 22.
        key.transform.SetParent(keyholeAnchor, true);

        // The copy is now in 22's local space.  It begins directly in front of
        // the black hole, travels only along the hole axis, then stays attached.
        Quaternion baseRotation = KeyPickupHandler.GetInsertionAlignment(key);
        float localDepthScale = Mathf.Max(.0001f, keyholeAnchor.lossyScale.z);
        float localTipDistance = tipDistance / localDepthScale;
        Vector3 start = new Vector3(0f, 0f, -(localTipDistance + 1.20f / localDepthScale));
        Vector3 target = new Vector3(0f, 0f, -localTipDistance + .018f / localDepthScale);
        key.transform.SetLocalPositionAndRotation(start, baseRotation);

        float elapsed = 0f;
        while (elapsed < approachDuration)
        {
            elapsed += Time.deltaTime;
            float eased = Mathf.SmoothStep(0f, 1f, elapsed / approachDuration);
            key.transform.localPosition = Vector3.Lerp(start, target, eased);
            yield return null;
        }

        // The key is now fully inserted and remains fixed at the lock core.
        elapsed = 0f;
        while (elapsed < turnDuration)
        {
            elapsed += Time.deltaTime;
            float eased = Mathf.SmoothStep(0f, 1f, elapsed / turnDuration);
            // Rotate around the insertion axis, exactly like turning a key in a lock.
            key.transform.localRotation = Quaternion.AngleAxis(90f * eased, Vector3.forward) * baseRotation;
            yield return null;
        }

        yield return OpenPurpleLid();
        unlocked = true;
        Debug.Log("[PurpleBox] Unlock animation complete.");
        Destroy(key);
    }
}
