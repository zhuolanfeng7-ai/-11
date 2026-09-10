using UnityEngine;

/// <summary>Runtime pickup behaviour for the one scene object named key.</summary>
public sealed class KeyPickupHandler : MonoBehaviour
{
    private const float PickupDistance = 5f;
    public static bool HasCollected { get; private set; }
    private static GameObject visualSource;
    private Camera viewCamera;
    private FirstPersonPlayer player;
    private bool collected;

    private void Awake()
    {
        visualSource = gameObject;
        player = FindObjectOfType<FirstPersonPlayer>();
        viewCamera = player != null ? player.PlayerCamera : Camera.main;
    }

    private void Update()
    {
        if (collected || !Input.GetMouseButtonDown(0)) return;
        if (viewCamera == null) viewCamera = player != null ? player.PlayerCamera : Camera.main;
        if (viewCamera == null) return;

        Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, ~0, QueryTriggerInteraction.Collide)) return;
        if (hit.transform != transform && !hit.transform.IsChildOf(transform)) return;

        Transform origin = player != null ? player.transform : viewCamera.transform;
        if (Vector3.Distance(origin.position, hit.collider.ClosestPoint(origin.position)) > PickupDistance) return;

        collected = true;
        HasCollected = true;
        // Disabling the key root also disables HoverScanHighlight, whose
        // OnDisable removes its purple/green overlay immediately.
        gameObject.SetActive(false);
    }

    /// <summary>Creates a renderer-only copy, safe to use for the unlock animation.</summary>
    public static GameObject CreateUnlockVisual()
    {
        if (visualSource == null) return null;
        GameObject copy = new GameObject("Key Unlock Animation Visual");
        int copiedMeshCount = 0;
        foreach (MeshFilter source in visualSource.GetComponentsInChildren<MeshFilter>(true))
        {
            if (source.sharedMesh == null) continue;
            GameObject part = new GameObject(source.name);
            part.transform.SetParent(copy.transform, false);
            part.transform.localPosition = visualSource.transform.InverseTransformPoint(source.transform.position);
            part.transform.localRotation = Quaternion.Inverse(visualSource.transform.rotation) * source.transform.rotation;
            part.transform.localScale = source.transform.lossyScale;
            MeshFilter filter = part.AddComponent<MeshFilter>();
            filter.sharedMesh = source.sharedMesh;
            MeshRenderer renderer = part.AddComponent<MeshRenderer>();
            MeshRenderer sourceRenderer = source.GetComponent<MeshRenderer>();
            if (sourceRenderer != null) renderer.sharedMaterials = sourceRenderer.sharedMaterials;
            copiedMeshCount++;
        }
        if (copiedMeshCount == 0) BuildFallbackKey(copy.transform);
        ResizeForUnlockAnimation(copy);
        return copy;
    }

    /// <summary>Rotates the model so its visually longest axis points forward.</summary>
    public static Quaternion GetInsertionAlignment(GameObject keyVisual)
    {
        if (keyVisual == null) return Quaternion.identity;
        Renderer[] renderers = keyVisual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return Quaternion.identity;
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        Vector3 size = bounds.size;
        Vector3 longAxis = size.x >= size.y && size.x >= size.z ? Vector3.right :
                           size.y >= size.z ? Vector3.up : Vector3.forward;
        return Quaternion.FromToRotation(longAxis, Vector3.forward);
    }

    /// <summary>Distance from the model pivot to the forward key tip.</summary>
    public static float GetForwardTipDistance(GameObject keyVisual)
    {
        if (keyVisual == null) return .3f;
        Renderer[] renderers = keyVisual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return .3f;
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        Vector3 size = bounds.size;
        if (size.x >= size.y && size.x >= size.z) return Mathf.Max(.01f, bounds.max.x);
        if (size.y >= size.z) return Mathf.Max(.01f, bounds.max.y);
        return Mathf.Max(.01f, bounds.max.z);
    }

    private static void ResizeForUnlockAnimation(GameObject copy)
    {
        Renderer[] renderers = copy.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        float largestDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (largestDimension > .0001f)
            // Large first-person presentation scale used only during unlock.
            copy.transform.localScale = Vector3.one * (1.55f / largestDimension);
    }

    private static void BuildFallbackKey(Transform root)
    {
        Material gold = new Material(Shader.Find("Standard"));
        gold.color = new Color(1f, .67f, .08f, 1f);
        gold.SetFloat("_Metallic", .8f);
        gold.SetFloat("_Glossiness", .65f);

        GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shaft.name = "Key Shaft";
        shaft.transform.SetParent(root, false);
        shaft.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        shaft.transform.localScale = new Vector3(.08f, .62f, .08f);
        shaft.GetComponent<Renderer>().sharedMaterial = gold;
        Destroy(shaft.GetComponent<Collider>());

        // Unity primitives have no torus, so a thin circular hub forms the bow.
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "Key Bow";
        ring.transform.SetParent(root, false);
        ring.transform.localPosition = new Vector3(-.62f, 0f, 0f);
        ring.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        ring.transform.localScale = new Vector3(.27f, .055f, .27f);
        ring.GetComponent<Renderer>().sharedMaterial = gold;
        Destroy(ring.GetComponent<Collider>());

        for (int i = 0; i < 2; i++)
        {
            GameObject tooth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tooth.name = "Key Tooth";
            tooth.transform.SetParent(root, false);
            tooth.transform.localPosition = new Vector3(.44f - i * .18f, -.12f, 0f);
            tooth.transform.localScale = new Vector3(.15f, .22f + i * .08f, .12f);
            tooth.GetComponent<Renderer>().sharedMaterial = gold;
            Destroy(tooth.GetComponent<Collider>());
        }
    }
}
