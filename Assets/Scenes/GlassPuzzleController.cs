using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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
    [Tooltip("Scene object under Camera that defines the glasses' centre-screen pose.")]
    [SerializeField] private Transform presentationAnchor;
    [Tooltip("Editable scene entity behind the centred glasses. It produces the soft pickup glow.")]
    [SerializeField] private GameObject glowBackdrop;
    [SerializeField] private Renderer glowBackdropRenderer;
    [Tooltip("Entity UI entry on the right side. It is enabled after the presentation.")]
    [SerializeField] private GameObject inventoryEntry;

    [Header("Interaction")]
    [Tooltip("Maximum distance between Player and this puzzle for clicking.")]
    [SerializeField, Min(0.1f)] private float interactionDistance = 5f;
    [Tooltip("Seconds for the screen to become completely clear.")]
    [SerializeField, Min(0.01f)] private float clearDuration = 2f;
    [SerializeField, Min(1f)] private float clickRayDistance = 100f;
    [Tooltip("Hide the replaceable Glass visual immediately after it is picked up.")]
    [SerializeField] private bool hideGlassOnPickup = true;

    [Header("Pickup Presentation")]
    [SerializeField, Min(0f)] private float riseHeight = 0.75f;
    [SerializeField, Min(0.01f)] private float riseDuration = 0.45f;
    [SerializeField, Min(0.01f)] private float centreMoveDuration = 0.75f;
    [SerializeField, Min(0.01f)] private float presentationScaleMultiplier = 1f;
    [Tooltip("Fine adjustment in GlassesPresentationAnchor local space after visual bounds are centred.")]
    [SerializeField] private Vector3 presentationLocalOffset = Vector3.zero;
    [SerializeField, Min(1)] private int outlineFlashCount = 3;
    [SerializeField, Min(0.01f)] private float outlineOnDuration = 0.22f;
    [SerializeField, Min(0.01f)] private float outlineOffDuration = 0.18f;
    [SerializeField, Range(0f, 2f)] private float glowPeakIntensity = 0.35f;
    [SerializeField, Min(0f)] private float finalHoldDuration = 0.25f;

    [Header("Inventory Icon")]
    [Tooltip("Project asset baked into the concrete GlassesInventoryIcon GameObject in Edit Mode.")]
    [SerializeField] private string inventoryIconAssetPath = "Assets/Resources/UI/GlassesInventoryIcon.png";
    [Tooltip("Pixel size of the glasses icon inside its inventory slot.")]
    [SerializeField] private Vector2 inventoryIconSize = new Vector2(72f, 24f);

    [Header("Diagnostics")]
    [Tooltip("Writes click-by-click pickup diagnostics to the Unity Console.")]
    [SerializeField] private bool enableDebugLogs = true;

    private Transform player;
    private FirstPersonPlayer playerController;
    private Collider pickupCollider;
    private GameObject inventoryIconObject;
    private bool solved;
    private MaterialPropertyBlock glowProperties;
    private static readonly int GlowIntensityId = Shader.PropertyToID("_Intensity");

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

        playerController = FindObjectOfType<FirstPersonPlayer>();
        if (playerController != null)
        {
            player = playerController.transform;
        }

        if (inventoryEntry != null)
        {
            inventoryEntry.SetActive(false);
            RawImage icon = inventoryEntry.GetComponentInChildren<RawImage>(true);
            if (icon != null)
            {
                inventoryIconObject = icon.gameObject;
                inventoryIconObject.SetActive(false);
            }
        }

        SetGlowIntensity(0f);
        if (glowBackdrop != null)
            glowBackdrop.SetActive(false);

        // The Glass frame owns this collider; it is added manually in the Inspector.
        if (glassTarget != null)
        {
            pickupCollider = glassTarget.GetComponentInChildren<Collider>(true);
        }

        ValidateSetup();
        if(Application.isPlaying&&glassTarget)
        {
            var hover=glassTarget.GetComponent<HoverScanHighlight>();
            if(!hover)hover=glassTarget.gameObject.AddComponent<HoverScanHighlight>();
            hover.enabled=true;hover.SetForcedHighlight(false);
            hover.Configure(6,interactionDistance);hover.ConfigureViewCamera(interactionCamera);
        }
        Log($"Ready. Visual='{NameOf(glassTarget)}', collider='{NameOf(pickupCollider)}' (enabled={pickupCollider != null && pickupCollider.enabled}, trigger={pickupCollider != null && pickupCollider.isTrigger}), camera='{NameOf(interactionCamera)}', blur={(blurEffect != null)}, player='{NameOf(player)}'.");
    }

    private void Update()
    {
        if(Application.isPlaying&&Input.GetMouseButtonDown(0))TryPickup(Input.mousePosition);
    }
    public bool TryPickup(Vector2 pointer)
    {
        if(Scene2PickupPresentation.IsPlaying)return false;
        if (solved)
        {
            Log("Click ignored: this GlassPuzzle was already picked up.");
            return false;
        }

        if (player == null || interactionCamera == null || blurEffect == null || pickupCollider == null)
        {
            Log("Click blocked: missing " +
                (player == null ? "Player " : string.Empty) +
                (interactionCamera == null ? "Camera " : string.Empty) +
                (blurEffect == null ? "VisionBlurEffect " : string.Empty) +
                (pickupCollider == null ? "Collider" : string.Empty));
            return false;
        }

        Vector3 closestPoint = pickupCollider.ClosestPoint(player.position);
        float distance = Vector3.Distance(player.position, closestPoint);
        if (distance > interactionDistance)
        {
            Log($"Click blocked: player is {distance:F2} units away; allowed distance is {interactionDistance:F2}.");
            return false;
        }

        Ray ray = interactionCamera.ScreenPointToRay(pointer);
        if (!Physics.Raycast(ray, out RaycastHit hit, clickRayDistance, ~0, QueryTriggerInteraction.Collide))
        {
            Log("Click blocked: raycast hit nothing. Check that the Collider is enabled and that the mouse is over the glasses.");
            return false;
        }

        bool hitPickupCollider = hit.collider == pickupCollider || hit.collider.transform.IsChildOf(pickupCollider.transform);
        if (!hitPickupCollider)
        {
            Log($"Click blocked: ray hit '{NameOf(hit.collider)}' instead of the glasses collider '{NameOf(pickupCollider)}'.");
            return false;
        }

        solved = true;
        Log($"Pickup succeeded: ray hit '{NameOf(hit.collider)}'. Starting centre-screen presentation and clearing blur over {clearDuration:F2} seconds.");

        pickupCollider.enabled = false;
        StartCoroutine(ClearVision());
        StartCoroutine(PlayPickupPresentation());
        return true;
    }

    private IEnumerator PlayPickupPresentation()
    {
        var mesh=glassTarget?glassTarget.GetComponentInChildren<MeshFilter>(true):null;
        if(mesh&&Scene2PickupPresentation.BeginScene1Glasses(mesh,mesh.GetComponent<Renderer>()))
        {
            foreach(var hover in glassTarget.GetComponentsInChildren<HoverScanHighlight>(true))hover.enabled=false;
            glassTarget.gameObject.SetActive(false);
            if(inventoryEntry)inventoryEntry.SetActive(false);
            if(glowBackdrop)glowBackdrop.SetActive(false);
            while(Scene2PickupPresentation.IsPlaying)yield return null;
            ShowVisualLore();
            yield break;
        }
        if (glassTarget == null || presentationAnchor == null)
        {
            Debug.LogError("GlassPuzzle: presentation cannot start because Glass Target or Presentation Anchor is missing.", this);
            yield break;
        }

        if (playerController != null)
        {
            Rigidbody body = playerController.GetComponent<Rigidbody>();
            if (body != null)
                body.velocity = new Vector3(0f, body.velocity.y, 0f);
            playerController.enabled = false;
        }

        Vector3 startPosition = glassTarget.position;
        Quaternion startRotation = glassTarget.rotation;
        Vector3 raisedPosition = startPosition + Vector3.up * riseHeight;
        float elapsed = 0f;
        while (elapsed < riseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / riseDuration));
            glassTarget.SetPositionAndRotation(Vector3.Lerp(startPosition, raisedPosition, t), startRotation);
            yield return null;
        }

        // The original scene glasses become the presentation object. No runtime
        // duplicate is created, and the editable scene anchor owns the final pose.
        glassTarget.SetParent(presentationAnchor, true);
        Vector3 localStartPosition = glassTarget.localPosition;
        Quaternion localStartRotation = glassTarget.localRotation;
        Vector3 localStartScale = glassTarget.localScale;
        Vector3 localTargetScale = localStartScale * presentationScaleMultiplier;
        Vector3 localTargetPosition = CalculateCentredLocalPosition(localTargetScale) + presentationLocalOffset;

        elapsed = 0f;
        while (elapsed < centreMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / centreMoveDuration));
            glassTarget.localPosition = Vector3.Lerp(localStartPosition, localTargetPosition, t);
            glassTarget.localRotation = Quaternion.Slerp(localStartRotation, Quaternion.identity, t);
            glassTarget.localScale = Vector3.Lerp(localStartScale, localTargetScale, t);
            yield return null;
        }
        glassTarget.localPosition = localTargetPosition;
        glassTarget.localRotation = Quaternion.identity;
        glassTarget.localScale = localTargetScale;

        if (glowBackdrop != null)
            glowBackdrop.SetActive(true);

        for (int i = 0; i < outlineFlashCount; i++)
        {
            float pulseDuration = outlineOnDuration + outlineOffDuration;
            float pulseElapsed = 0f;
            while (pulseElapsed < pulseDuration)
            {
                pulseElapsed += Time.unscaledDeltaTime;
                float pulse = Mathf.Sin(Mathf.Clamp01(pulseElapsed / pulseDuration) * Mathf.PI);
                SetGlowIntensity(pulse * glowPeakIntensity);
                yield return null;
            }
            SetGlowIntensity(0f);
        }

        if (finalHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(finalHoldDuration);

        SetGlowIntensity(0f);
        if (glowBackdrop != null)
            glowBackdrop.SetActive(false);
        if (inventoryEntry != null)
            inventoryEntry.SetActive(true);
        if (inventoryIconObject != null)
            inventoryIconObject.SetActive(true);

        if (hideGlassOnPickup)
            glassTarget.gameObject.SetActive(false);

        if (playerController != null)
            playerController.enabled = true;

        ShowVisualLore();
    }

#if false // One-shot inventory UI baker already applied; retained only as implementation history.
    [InitializeOnLoadMethod]
    private static void ScheduleInventoryUIBake()
    {
        EditorApplication.delayCall += BakeAllInventoryUI;
        EditorApplication.playModeStateChanged -= HandleEditorPlayModeChanged;
        EditorApplication.playModeStateChanged += HandleEditorPlayModeChanged;
    }

    private static void HandleEditorPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += BakeAllInventoryUI;
    }

    private static void BakeAllInventoryUI()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        foreach (GlassPuzzleController controller in Resources.FindObjectsOfTypeAll<GlassPuzzleController>())
        {
            if (controller != null && controller.gameObject.scene.IsValid() && controller.gameObject.scene.isLoaded)
                controller.OnValidate();
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying || inventoryEntry == null)
            return;

        RawImage icon = inventoryEntry.GetComponentInChildren<RawImage>(true);
        if (icon == null)
            return;

        Texture2D iconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(inventoryIconAssetPath);
        if (iconTexture == null)
            return;

        RectTransform iconRect = icon.rectTransform;
        icon.texture = iconTexture;
        icon.color = Color.white;
        icon.uvRect = new Rect(0.0175f, 0.3397f, 0.9657f, 0.3158f);
        icon.raycastTarget = false;
        icon.enabled = true;

        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = inventoryIconSize;
        iconRect.localScale = Vector3.one;
        iconRect.localRotation = Quaternion.identity;

        RectTransform panel = inventoryEntry.transform.parent as RectTransform;
        if (panel != null)
            BakeConcreteInventorySlots(panel);

        EditorUtility.SetDirty(icon);
        EditorUtility.SetDirty(iconRect);
        if (gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            EditorApplication.delayCall -= SaveBakedInventoryUI;
            EditorApplication.delayCall += SaveBakedInventoryUI;
        }
    }

    private void SaveBakedInventoryUI()
    {
        if (this != null && !Application.isPlaying && gameObject.scene.IsValid() && gameObject.scene.isDirty)
            EditorSceneManager.SaveScene(gameObject.scene);
    }

    private void BakeConcreteInventorySlots(RectTransform panel)
    {
        panel.anchorMin = new Vector2(1f, 0.5f);
        panel.anchorMax = new Vector2(1f, 0.5f);
        panel.pivot = new Vector2(1f, 0.5f);
        panel.anchoredPosition = new Vector2(-22f, 0f);
        panel.sizeDelta = new Vector2(58f, 338f);
        panel.localScale = Vector3.one;

        Image panelImage = panel.GetComponent<Image>();
        if (panelImage != null)
            panelImage.enabled = false;

        VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            layout = Undo.AddComponent<VerticalLayoutGroup>(panel.gameObject);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 12f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ConfigureConcreteSlot(inventoryEntry, 0);
        string[] names = { "CollectionSlot_Film", "CollectionSlot_Family", "CollectionSlot_Sketch", "CollectionSlot_Key" };
        for (int i = 0; i < names.Length; i++)
        {
            Transform existing = panel.Find(names[i]);
            GameObject slot = existing != null ? existing.gameObject : CreateConcreteSlot(names[i], panel);
            ConfigureConcreteSlot(slot, i + 1);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
        EditorUtility.SetDirty(panel);
        EditorUtility.SetDirty(layout);
    }

    private static GameObject CreateConcreteSlot(string slotName, RectTransform parent)
    {
        GameObject slot = new GameObject(slotName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement), typeof(Outline));
        Undo.RegisterCreatedObjectUndo(slot, "Create concrete inventory slot");
        slot.layer = 5;
        slot.transform.SetParent(parent, false);
        return slot;
    }

    private static void ConfigureConcreteSlot(GameObject slot, int siblingIndex)
    {
        slot.SetActive(true);
        slot.transform.SetSiblingIndex(siblingIndex);

        RectTransform rect = slot.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(58f, 58f);
        rect.localScale = Vector3.one;

        LayoutElement element = slot.GetComponent<LayoutElement>();
        if (element == null)
            element = Undo.AddComponent<LayoutElement>(slot);
        element.preferredWidth = 58f;
        element.preferredHeight = 58f;

        Image image = slot.GetComponent<Image>();
        if (image == null)
            image = Undo.AddComponent<Image>(slot);
        image.color = new Color(0.035f, 0.012f, 0.065f, 0.68f);
        image.raycastTarget = false;

        Outline outline = slot.GetComponent<Outline>();
        if (outline == null)
            outline = Undo.AddComponent<Outline>(slot);
        outline.effectColor = new Color(0.58f, 0.22f, 0.82f, 0.72f);
        outline.effectDistance = new Vector2(1f, -1f);

        EditorUtility.SetDirty(rect);
        EditorUtility.SetDirty(element);
        EditorUtility.SetDirty(image);
        EditorUtility.SetDirty(outline);
    }
#endif

    private void SetGlowIntensity(float intensity)
    {
        if (glowBackdropRenderer == null)
            return;

        if (glowProperties == null)
            glowProperties = new MaterialPropertyBlock();

        glowBackdropRenderer.GetPropertyBlock(glowProperties);
        glowProperties.SetFloat(GlowIntensityId, intensity);
        glowBackdropRenderer.SetPropertyBlock(glowProperties);
    }

    private Vector3 CalculateCentredLocalPosition(Vector3 targetLocalScale)
    {
        Vector3 savedPosition = glassTarget.localPosition;
        Quaternion savedRotation = glassTarget.localRotation;
        Vector3 savedScale = glassTarget.localScale;

        // Evaluate the imported mesh in its final presentation rotation/scale.
        // Imported FBX roots often have a pivot far away from visible geometry.
        glassTarget.localPosition = Vector3.zero;
        glassTarget.localRotation = Quaternion.identity;
        glassTarget.localScale = targetLocalScale;

        Renderer[] renderers = glassTarget.GetComponentsInChildren<Renderer>(true);
        Vector3 target = Vector3.zero;
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            Vector3 correctedWorldPosition = glassTarget.position + (presentationAnchor.position - bounds.center);
            target = presentationAnchor.InverseTransformPoint(correctedWorldPosition);
            Log($"Presentation centred from renderer bounds. Visual centre was {bounds.center}; anchor is {presentationAnchor.position}; target local position is {target}.");
        }

        glassTarget.localPosition = savedPosition;
        glassTarget.localRotation = savedRotation;
        glassTarget.localScale = savedScale;
        return target;
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

    private void ShowVisualLore()
    {
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
        if (presentationAnchor == null)
            Debug.LogError("GlassPuzzle: assign the scene GlassesPresentationAnchor.", this);
        if (inventoryEntry == null)
            Debug.LogError("GlassPuzzle: assign the scene GlassesInventoryEntry.", this);
        if (glowBackdrop == null || glowBackdropRenderer == null)
            Debug.LogError("GlassPuzzle: assign the scene GlassesGlowBackdrop and its Renderer.", this);
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
