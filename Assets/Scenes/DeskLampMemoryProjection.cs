using System.Collections;
using UnityEngine;

/// <summary>One-way DeskLampPuzzle reveal: a warm light and a memory image appear at the ceiling centre.</summary>
public sealed class DeskLampMemoryProjection : MonoBehaviour
{
    [Header("Click")]
    [SerializeField] private float interactionDistance = 10f;
    [SerializeField] private float rayDistance = 100f;
    [Header("Approach Lore")]
    [SerializeField, Min(.1f)] private float approachLoreDistance = 8f;
    [Header("Soft warm lamp")]
    [SerializeField] private Color warmYellow = new Color(1f, .88f, .60f, 1f);
    [SerializeField] private float lightIntensity = 6.5f;
    [SerializeField] private float lightRange = 45f;
    [SerializeField] private float fadeSeconds = 2f;
    [Header("Ceiling memory image")]
    [SerializeField] private Transform ceilingProjectionAnchor;
    [SerializeField] private Texture2D memoryImage;
    [SerializeField, Min(.1f)] private float projectionWidth = 18f;
    [SerializeField, Range(0f, 1f)] private float projectionOpacity = .88f;
    [SerializeField, Min(0f)] private float idleJitter = .009f;
    [SerializeField] private Color imageTint = new Color(1f, .96f, .78f, 1f);
    [SerializeField, Range(0f, 1f)] private float paperLighten = .28f;
    [SerializeField, Range(.001f, .3f)] private float edgeFeather = .18f;
    [SerializeField, Range(.01f, .45f)] private float cornerRadius = .22f;

    private Camera viewCamera;
    private FirstPersonPlayer player;
    private Collider clickCollider;
    private Light coreLight, glowLight;
    private Transform projectionPlane;
    private Material projectionMaterial;
    private Vector3 projectionBasePosition;
    private bool revealed;
    private bool approachLoreShown;
    private bool activationLoreShown;
    private Coroutine revealRoutine;

    private void Awake()
    {
        viewCamera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
        player = FindObjectOfType<FirstPersonPlayer>();
        clickCollider = GetComponent<Collider>();
        if (clickCollider == null) CreateClickCollider();
        if (memoryImage == null) memoryImage = Resources.Load<Texture2D>("CeilingMemoryProjection");
    }

    private void Update()
    {
        TryShowApproachLore();
        if (revealed) AnimateProjection();
        if (!Input.GetMouseButtonDown(0) || revealed || revealRoutine != null || EvidenceInspect.BlocksWorldInput) return;
        if (viewCamera == null) viewCamera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
        if (viewCamera == null || clickCollider == null) return;
        if (Vector3.Distance(viewCamera.transform.position, clickCollider.ClosestPoint(viewCamera.transform.position)) > interactionDistance) return;
        Vector2 pointer = Cursor.lockState == CursorLockMode.Locked ? viewCamera.pixelRect.center : (Vector2)Input.mousePosition;
        RaycastHit[] hits = Physics.RaycastAll(viewCamera.ScreenPointToRay(pointer), rayDistance, ~((1 << 30) | (1 << 31)), QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        bool lampClicked = false;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.GetComponentInParent<FirstPersonPlayer>() != null) continue;
            lampClicked = hit.transform == transform || hit.transform.IsChildOf(transform);
            break;
        }
        if (!lampClicked || !BuildAtCeilingCentre()) return;
        revealRoutine = StartCoroutine(RevealOnce());
    }

    private void TryShowApproachLore()
    {
        if (approachLoreShown || player == null || clickCollider == null) return;
        if (Vector3.Distance(player.transform.position, clickCollider.ClosestPoint(player.transform.position)) > approachLoreDistance)
            return;

        WorldStartTerminal terminal = FindObjectOfType<WorldStartTerminal>();
        if (terminal == null || !terminal.IsGameStarted) return;

        approachLoreShown = true;
        terminal.ShowLoreMessage(
            "VISUAL RESIDUE DETECTED",
            "UNREAD VISUAL RESIDUE DETECTED.\n\nSTRANGE...\n\nSOME MEMORIES DO NOT REVEAL THEMSELVES AUTOMATICALLY.\n\nTHEY NEED LIGHT.");
    }

    private void ShowActivationLore()
    {
        if (activationLoreShown || projectionPlane == null) return;
        WorldStartTerminal terminal = FindObjectOfType<WorldStartTerminal>();
        if (terminal == null || !terminal.IsGameStarted) return;

        activationLoreShown = true;
        terminal.ShowLoreMessage(
            "CEILING ANALYSIS",
            "LOOK UP AT THE CEILING.\n\nVISUAL RESTORATION FAILED.\n\nFACIAL INFORMATION MISSING.\n\nYOU SPENT A FORTUNE PURCHASING INFINITE POSSIBILITIES.\n\nYET YOU COULD NEVER AFFORD A CLEAR FACE.");
    }

    private void CreateClickCollider()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        box.center = transform.InverseTransformPoint(bounds.center);
        Vector3 min = transform.InverseTransformPoint(bounds.min);
        Vector3 max = transform.InverseTransformPoint(bounds.max);
        box.size = new Vector3(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y), Mathf.Abs(max.z - min.z));
        clickCollider = box;
    }

    private bool BuildAtCeilingCentre()
    {
        if (projectionPlane != null) return true;
        GameObject ceiling = GameObject.Find("ceiling");
        if (ceiling == null || memoryImage == null)
        {
            Debug.LogError("[DeskLampMemory] Ceiling or CeilingMemoryProjection image is missing.", this);
            return false;
        }

        Renderer ceilingRenderer = ceiling.GetComponent<Renderer>();
        Vector3 centre = ceilingRenderer != null ? ceilingRenderer.bounds.center : ceiling.transform.position;
        Vector3 normal = ceiling.transform.up;
        if (Vector3.Dot(normal, transform.position - centre) < 0f) normal = -normal;
        if (ceilingProjectionAnchor == null)
        {
            GameObject anchor = GameObject.Find("CeilingMemoryProjection") ?? new GameObject("CeilingMemoryProjection");
            ceilingProjectionAnchor = anchor.transform;

        }
        // Keep the runtime projection in world units; a rotated, non-uniform ceiling otherwise distorts it.
        ceilingProjectionAnchor.SetParent(null, true);
        ceilingProjectionAnchor.localScale = Vector3.one;
        // Bounds centre is world-space: this remains the visible centre despite ceiling scale/rotation.
        ceilingProjectionAnchor.SetPositionAndRotation(centre + normal * .025f, Quaternion.LookRotation(normal, Vector3.forward));

        Renderer lamp = GetComponentInChildren<Renderer>(true);
        Vector3 lampPosition = lamp != null ? lamp.bounds.center : transform.position;
        GameObject lightRoot = new GameObject("DeskLamp Ceiling Warm Light");
        lightRoot.transform.SetPositionAndRotation(lampPosition, Quaternion.LookRotation((centre - lampPosition).normalized, Vector3.forward));
        coreLight = lightRoot.AddComponent<Light>();
        coreLight.type = LightType.Spot; coreLight.color = warmYellow; coreLight.range = lightRange; coreLight.spotAngle = 94f; coreLight.shadows = LightShadows.Soft;
        glowLight = new GameObject("DeskLamp Ceiling Soft Glow").AddComponent<Light>();
        glowLight.transform.SetParent(lightRoot.transform, false);
        glowLight.type = LightType.Spot; glowLight.color = warmYellow; glowLight.range = lightRange; glowLight.spotAngle = 124f; glowLight.shadows = LightShadows.None;

        GameObject imagePlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
        imagePlane.name = "Ceiling Memory Image";
        Destroy(imagePlane.GetComponent<Collider>());
        projectionPlane = imagePlane.transform;
        projectionPlane.SetParent(ceilingProjectionAnchor, false);
        projectionPlane.localPosition = Vector3.forward * .008f;
        projectionBasePosition = projectionPlane.localPosition;
        float height = projectionWidth * memoryImage.height / memoryImage.width;
        projectionPlane.localScale = new Vector3(projectionWidth, height, 1f);
        Material template = Resources.Load<Material>("CeilingMemoryProjectionMaterial");
        Shader projectionShader = template != null ? template.shader : Shader.Find("Custom/CeilingMemoryProjection");
        if (projectionShader == null)
        {
            Debug.LogError("[DeskLampMemory] Ceiling projection shader is missing.", this);
            Destroy(imagePlane);
            projectionPlane = null;
            return false;
        }
        projectionMaterial = new Material(projectionShader);
        projectionMaterial.mainTexture = memoryImage;
        projectionMaterial.SetColor("_Tint", imageTint);
        projectionMaterial.SetFloat("_Lighten", paperLighten);
        projectionMaterial.SetFloat("_Feather", edgeFeather);
        projectionMaterial.SetFloat("_CornerRadius", cornerRadius);
        projectionMaterial.SetFloat("_Opacity", 0f);
        imagePlane.GetComponent<MeshRenderer>().sharedMaterial = projectionMaterial;
        projectionPlane.gameObject.SetActive(false);
        Debug.Log($"[DeskLampMemory] Image built at ceiling centre {centre}.", this);
        return true;
    }

    private IEnumerator RevealOnce()
    {
        if (projectionPlane == null) yield break;
        projectionPlane.gameObject.SetActive(true);
        coreLight.enabled = glowLight.enabled = true;
        for (float elapsed = 0f; elapsed < fadeSeconds; elapsed += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / fadeSeconds);
            coreLight.intensity = Mathf.Lerp(0f, lightIntensity, t);
            glowLight.intensity = Mathf.Lerp(0f, lightIntensity * .28f, t);
            SetProjectionAlpha(Mathf.Lerp(0f, projectionOpacity, t));
            yield return null;
        }
        coreLight.intensity = lightIntensity;
        glowLight.intensity = lightIntensity * .28f;
        SetProjectionAlpha(projectionOpacity);
        revealed = true;
        revealRoutine = null;
        ShowActivationLore();
    }

    private void AnimateProjection()
    {
        float time = Time.time;
        projectionPlane.localPosition = projectionBasePosition + new Vector3(Mathf.Sin(time * 7.1f), Mathf.Sin(time * 5.4f), 0f) * idleJitter;
        SetProjectionAlpha(projectionOpacity + Mathf.Sin(time * 2.7f) * .025f);
    }

    private void SetProjectionAlpha(float alpha)
    {
        if (projectionMaterial == null) return;
        projectionMaterial.SetFloat("_Opacity", Mathf.Clamp01(alpha));
    }

    private void OnDestroy()
    {
        if (projectionMaterial != null) Destroy(projectionMaterial);
    }
}



