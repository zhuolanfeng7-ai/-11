using UnityEngine;

/// <summary>
/// Draws the hover scanner in OnGUI, after the camera's black-and-white/blur effect.
/// The scanner therefore remains purple and green even while the 3D world is monochrome.
/// </summary>
[DefaultExecutionOrder(11000)]
public sealed class HoverScanHighlight : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Camera viewCamera;
    [SerializeField] private Color purple = new Color(.78f, .20f, 1f, .95f);
    [SerializeField] private Color green = new Color(.15f, 1f, .42f, .9f);
    [SerializeField, Min(1f)] private float framePaddingPixels = 20f;
    [SerializeField, Min(.1f)] private float hoverDistance = 5f;
    [Header("Glasses Hover Highlight")]
    [Tooltip("Purple rim colour used only by the polySurface24 glasses frame.")]
    [SerializeField] private Color glassesOutlineColor = new Color(.55f, .28f, .90f, .34f);
    [Tooltip("Brightness of the glasses rim highlight.")]
    [SerializeField, Range(0f, 3f)] private float glassesOutlineIntensity = 1f;
    [Tooltip("Higher values make the glasses highlight stay closer to the outer edge.")]
    [SerializeField, Range(1f, 8f)] private float glassesOutlinePower = 2.8f;
    [Tooltip("Adds a subtle purple tint over the glasses surface. Keep at zero for outline only.")]
    [SerializeField, Range(0f, 1f)] private float glassesSurfaceGlow = 0f;
    [Header("Button Up / Down Hover Highlight")]
    [SerializeField] private Color buttonOutlineColor = new Color(.55f, .28f, .90f, .34f);
    [SerializeField, Range(0f, 3f)] private float buttonOutlineIntensity = 1f;
    [SerializeField, Range(1f, 8f)] private float buttonOutlinePower = 2.8f;
    [SerializeField, Range(0f, 1f)] private float buttonSurfaceGlow = 0f;
    [Header("Button Up / Down Linear Frame")]
    [SerializeField] private Color buttonFramePurple = new Color(.78f, .20f, 1f, .95f);
    [SerializeField] private Color buttonFrameGreen = new Color(.15f, 1f, .42f, .90f);
    [SerializeField, Range(2f, 20f)] private float buttonFramePadding = 6f;
    [SerializeField, Range(0f, 2f)] private float buttonFrameIntensity = 1f;
    [Header("Alloy2 Subtle Purple Highlight")]
    [SerializeField] private Color alloy2OutlineColor = new Color(.58f, .22f, .92f, .30f);
    [SerializeField, Range(.001f, .02f)] private float alloy2OutlineWidthRatio = .0045f;
    [SerializeField, Range(0f, 3f)] private float alloy2GlowIntensity = .78f;
    [SerializeField, Range(1.1f, 3f)] private float alloy2GlowSpread = 1.8f;
    [SerializeField, Range(.05f, .8f)] private float alloy2OuterGlowStrength = .28f;
    [Header("Alloy2 Linear Frame")]
    [SerializeField] private Color alloy2FramePurple = new Color(.78f, .20f, 1f, .90f);
    [SerializeField] private Color alloy2FrameGreen = new Color(.15f, 1f, .42f, .82f);
    [SerializeField, Range(2f, 20f)] private float alloy2FramePadding = 6f;
    [SerializeField, Range(0f, 2f)] private float alloy2FrameIntensity = .85f;

    private Texture2D purpleFrame;
    private Texture2D greenFrame;
    private Transform lavenderOverlayRoot;
    private Camera overlayCamera;
    private Material lavenderMaterial;
<<<<<<< Updated upstream
    private FirstPersonPlayer player;
    private Rect hoverRect;
    private bool hovered;
=======
    private Material repairOutlineMaterial;
    private Material alloy2OuterGlowMaterial;
    private Material whiteGlowMaterial;
    private FirstPersonPlayer player;
    private Rect hoverRect;
    private bool hovered;
    private bool forcedHighlight;
    private bool persistentHighlight;
    private bool singleRepairPartOutline;
    private bool backGlowOnly;
    private bool subtleAlloy2Highlight;
    private bool frameOnlyHighlight;
    private bool glassesHighlight;
    private bool buttonHighlight;
    private float repairMeshSize;
    public bool PersistentHighlight=>persistentHighlight;
    private readonly List<Transform> overlaySources = new List<Transform>();
    private readonly List<Transform> overlayProxies = new List<Transform>();
    private readonly List<MeshRenderer> overlayRenderers = new List<MeshRenderer>();

>>>>>>> Stashed changes
    private TelescopingDevice telescopingDevice;
    [SerializeField] private TubeRewardReveal revealOwner;
    private bool sharedDeviceOverlay;
    private MeshFilter[] proxySources;
    private Transform[] proxyTransforms;
    [SerializeField] private bool scene2Object;
    [SerializeField] private MusicBoxRewardLift crankReward;
    [SerializeField] private Transform occlusionHousing;
    public void ConfigureOcclusionHousing(Transform housing){occlusionHousing=housing;}
    public bool IsHovered => hovered;

    private void Awake()
    {
        if(string.Equals(gameObject.scene.name.Replace(" ",""),"scene2",System.StringComparison.OrdinalIgnoreCase))scene2Object=true;
        telescopingDevice = GetComponent<TelescopingDevice>();
        if (visualRoot == null) visualRoot = transform;
        singleRepairPartOutline = IsScene2RepairPart(transform);
        backGlowOnly = IsBackGlowRepairPart(transform);
        subtleAlloy2Highlight = IsNamedInHierarchy(transform, "alloy2");
        frameOnlyHighlight = string.Equals(transform.name.Trim(), "zhuzi2", System.StringComparison.OrdinalIgnoreCase);
        glassesHighlight = IsGlassesVisual(visualRoot);
        buttonHighlight = IsButtonVisual(visualRoot);
        if (viewCamera == null) viewCamera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
        player = FindObjectOfType<FirstPersonPlayer>();
        purpleFrame = CreateNeonFrame(subtleAlloy2Highlight || buttonHighlight ? Color.white : purple);
        greenFrame = CreateNeonFrame(subtleAlloy2Highlight || buttonHighlight ? Color.white : green);
        BuildLavenderOverlay();
    }

    public void Configure(float paddingPixels, float maximumDistance)
    {
        framePaddingPixels = paddingPixels;
        hoverDistance = maximumDistance;
    }
<<<<<<< Updated upstream

    public void ConfigureReward(TubeRewardReveal owner){revealOwner=owner;}
    public void ConfigureScene2(){scene2Object=true;}
    public void ConfigureCrankReward(MusicBoxRewardLift reward){crankReward=reward;scene2Object=true;}
=======
    public void ConfigureViewCamera(Camera camera){if(camera)viewCamera=camera;}
    public void SetPersistentHighlight(bool value){persistentHighlight=value;}
    public void ConfigureVisualRoot(Transform root)
    {
        if(!root||visualRoot==root)return;
        visualRoot=root;
        singleRepairPartOutline=IsScene2RepairPart(root);
        backGlowOnly=IsBackGlowRepairPart(root);
        subtleAlloy2Highlight=IsNamedInHierarchy(root,"alloy2");
        frameOnlyHighlight=string.Equals(root.name.Trim(),"zhuzi2",System.StringComparison.OrdinalIgnoreCase);
        glassesHighlight=IsGlassesVisual(root);
        buttonHighlight=IsButtonVisual(root);
        if(lavenderOverlayRoot){lavenderOverlayRoot.gameObject.SetActive(false);Destroy(lavenderOverlayRoot.gameObject);}
        if(overlayCamera){overlayCamera.enabled=false;Destroy(overlayCamera.gameObject);}
        if(lavenderMaterial)Destroy(lavenderMaterial);if(repairOutlineMaterial)Destroy(repairOutlineMaterial);if(alloy2OuterGlowMaterial)Destroy(alloy2OuterGlowMaterial);if(whiteGlowMaterial)Destroy(whiteGlowMaterial);
        overlaySources.Clear();overlayProxies.Clear();overlayRenderers.Clear();BuildLavenderOverlay();
    }

    public void ConfigureReward(TubeRewardReveal owner){revealOwner=owner;}
    public void ConfigureScene2(){scene2Object=true;}
    public void ConfigureCrankReward(MusicBoxRewardLift owner){crankReward=owner;scene2Object=true;}

    public void SetForcedHighlight(bool enabled)
    {
        forcedHighlight = enabled;
        ApplyOverlayMaterial(enabled ? whiteGlowMaterial : lavenderMaterial);
        if (enabled)
            SetForcedHighlightIntensity(1f);
        if (!enabled)
        {
            hovered = false;
            SetOverlayVisible(false);
        }
    }

    public void SetForcedHighlightIntensity(float normalizedIntensity)
    {
        if (whiteGlowMaterial == null) return;
        float intensity = Mathf.Clamp01(normalizedIntensity);
        whiteGlowMaterial.SetFloat("_GlowIntensity", pickupFlashIntensity * intensity);
        whiteGlowMaterial.SetColor("_RimColor", new Color(
            pickupFlashColor.r,
            pickupFlashColor.g,
            pickupFlashColor.b,
            pickupFlashColor.a * intensity));
    }
>>>>>>> Stashed changes

    private void Update()
    {
        if (telescopingDevice != null || revealOwner != null || scene2Object) return;
        if (viewCamera == null) viewCamera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
        hovered = false;
        SetOverlayVisible(false);
        if (viewCamera == null) return;
        SyncOverlayCamera();
<<<<<<< Updated upstream
        Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, ~0, QueryTriggerInteraction.Collide)) return;
        if (hit.transform != transform && !hit.transform.IsChildOf(transform)) return;
        Transform distanceOrigin = player != null ? player.transform : viewCamera.transform;
        if (Vector3.Distance(distanceOrigin.position, hit.collider.ClosestPoint(distanceOrigin.position)) > hoverDistance) return;
        hovered = TryGetScreenRect(out hoverRect);
        if (lavenderOverlayRoot != null) lavenderOverlayRoot.gameObject.SetActive(hovered);
    }

=======
        SyncOverlayVisuals();

        if (forcedHighlight)
        {
            hovered = TryGetScreenRect(out hoverRect);
            SetOverlayVisible(hovered);
            return;
        }
        if(persistentHighlight)
        {
            var renderer=visualRoot.GetComponentInChildren<Renderer>();
            var origin=player?player.transform.position:viewCamera.transform.position;
            if(!renderer||!renderer.enabled||Vector3.Distance(origin,renderer.bounds.ClosestPoint(origin))>hoverDistance)return;
            if(lavenderMaterial){lavenderMaterial.SetColor("_RimColor",new Color(.7f,.35f,1,.7f));lavenderMaterial.SetFloat("_GlowIntensity",1.8f);}
            hovered=TryGetScreenRect(out hoverRect);
            SetOverlayVisible(hovered);
            return;
        }

        RefreshPointerHover(viewCamera,Input.mousePosition);
    }

    // Use the same real-view ray for runtime hover and interaction verification.
    public bool RefreshPointerHover(Camera camera,Vector2 pointer)
    {
        if(!enabled||forcedHighlight||persistentHighlight||!camera)return false;
        viewCamera=camera;hovered=false;
        SetOverlayVisible(false);
        SyncOverlayCamera();SyncOverlayVisuals();
        Ray ray=viewCamera.ScreenPointToRay(pointer);
        if(!Physics.Raycast(ray,out RaycastHit hit,100f,~((1<<30)|(1<<31)),QueryTriggerInteraction.Collide))return false;
        if(hit.transform!=transform&&!hit.transform.IsChildOf(transform))return false;
        Transform distanceOrigin=player?player.transform:viewCamera.transform;
        if(Vector3.Distance(distanceOrigin.position,hit.collider.ClosestPoint(distanceOrigin.position))>hoverDistance)return false;
        hovered=TryGetScreenRect(out hoverRect);
        SetOverlayVisible(hovered);
        return hovered;
    }
>>>>>>> Stashed changes
    private void LateUpdate()
    {
        if (telescopingDevice == null && revealOwner == null && !scene2Object) return;
        var camera = viewCamera != null ? viewCamera : Camera.main;
        Vector2 pointer = Cursor.lockState == CursorLockMode.Locked && camera != null
            ? camera.pixelRect.center : (Vector2)Input.mousePosition;
        RefreshDeviceHover(camera, pointer);
    }

    public bool RefreshDeviceHover(Camera camera, Vector2 pointer)
    {
        hovered = false;
        SetOverlayVisible(false);
        if ((telescopingDevice == null && revealOwner == null && !scene2Object) || camera == null || !camera.pixelRect.Contains(pointer)) return false;
        if(EvidenceInspect.BlocksWorldInput)return false;
        if (revealOwner != null && !revealOwner.IsRevealed) return false;
        if (crankReward != null && !crankReward.IsRaised) return false;
        viewCamera = camera;
        if(proxySources!=null)for(int i=0;i<proxySources.Length;i++)if(proxySources[i]&&proxyTransforms[i]){
            proxyTransforms[i].SetPositionAndRotation(proxySources[i].transform.position,proxySources[i].transform.rotation);
            proxyTransforms[i].localScale=proxySources[i].transform.lossyScale;
        }
        var originalRenderer = visualRoot.GetComponent<Renderer>();
        if (originalRenderer == null || !originalRenderer.enabled) return false;
        SyncOverlayCamera();
        Ray ray = camera.ScreenPointToRay(pointer);
        ray.origin = camera.transform.position;
        Physics.SyncTransforms();
        if (telescopingDevice != null){
            if (!telescopingDevice.CanInteract(ray, camera.transform.position, camera.farClipPlane)) return false;
        } else {
            Collider target=GetComponent<Collider>();
            if(!target || !target.enabled)return false;
            Vector3 closest=target is MeshCollider mesh && !mesh.convex
                ? target.bounds.ClosestPoint(camera.transform.position) : target.ClosestPoint(camera.transform.position);
            if(Vector3.Distance(camera.transform.position,closest)>hoverDistance+.001f)return false;
            var hits=Physics.RaycastAll(ray,camera.farClipPlane,~0,QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            bool hitReward=false;
            foreach(var hit in hits){
                if(player && hit.transform.IsChildOf(player.transform))continue;
                hitReward=hit.transform==transform || hit.transform.IsChildOf(transform);
                if(hitReward)break;
                if(occlusionHousing && (hit.transform==occlusionHousing || hit.transform.IsChildOf(occlusionHousing)))continue;
                break;
            }
            if(!hitReward)return false;
        }
        hovered = TryGetScreenRect(out hoverRect);
        if (hovered)
        {
            // At close range the tall tube can extend beyond the Game view.
            // Keep the selection frame within the visible viewport.
            float margin=Mathf.Clamp(framePaddingPixels,4f,10f)*1.5f;
            Rect viewport=camera.pixelRect;
            float left=Mathf.Max(hoverRect.xMin,viewport.xMin+margin);
            float right=Mathf.Min(hoverRect.xMax,viewport.xMax-margin);
            float bottom=Mathf.Max(hoverRect.yMin,viewport.yMin+margin);
            float top=Mathf.Min(hoverRect.yMax,viewport.yMax-margin);
            hoverRect=Rect.MinMaxRect(left,bottom,right,top);
            hovered=right>left && top>bottom;
        }
        SetOverlayVisible(hovered);
        return hovered;
    }

    private void OnGUI()
    {
        if (!hovered || purpleFrame == null || greenFrame == null) return;
        // GUI y is top-down, while camera screen coordinates are bottom-up.
        Rect guiRect = new Rect(hoverRect.x, Screen.height - hoverRect.yMax, hoverRect.width, hoverRect.height);
        float padding = Mathf.Clamp(framePaddingPixels, 4f, 10f);
        Rect glowRect = Expand(guiRect, padding * 1.25f);
        Rect purpleRect = Expand(guiRect, padding * .65f);
        Rect greenRect = Expand(guiRect, padding * 1.25f);
        if (subtleAlloy2Highlight)
        {
            float framePadding = Mathf.Max(2f, alloy2FramePadding);
            purpleRect = Expand(guiRect, framePadding * .65f);
            greenRect = Expand(guiRect, framePadding * 1.25f);
            Color old = GUI.color;
            GUI.color = new Color(alloy2FramePurple.r, alloy2FramePurple.g, alloy2FramePurple.b,
                Mathf.Clamp01(alloy2FramePurple.a * alloy2FrameIntensity));
            GUI.DrawTexture(purpleRect, purpleFrame, ScaleMode.StretchToFill, true);
            GUI.color = new Color(alloy2FrameGreen.r, alloy2FrameGreen.g, alloy2FrameGreen.b,
                Mathf.Clamp01(alloy2FrameGreen.a * alloy2FrameIntensity));
            GUI.DrawTexture(greenRect, greenFrame, ScaleMode.StretchToFill, true);
            GUI.color = old;
            return;
        }
        if (buttonHighlight)
        {
            float framePadding = Mathf.Max(2f, buttonFramePadding);
            purpleRect = Expand(guiRect, framePadding * .65f);
            greenRect = Expand(guiRect, framePadding * 1.25f);
            Color old = GUI.color;
            GUI.color = new Color(buttonFramePurple.r, buttonFramePurple.g, buttonFramePurple.b,
                Mathf.Clamp01(buttonFramePurple.a * buttonFrameIntensity));
            GUI.DrawTexture(purpleRect, purpleFrame, ScaleMode.StretchToFill, true);
            GUI.color = new Color(buttonFrameGreen.r, buttonFrameGreen.g, buttonFrameGreen.b,
                Mathf.Clamp01(buttonFrameGreen.a * buttonFrameIntensity));
            GUI.DrawTexture(greenRect, greenFrame, ScaleMode.StretchToFill, true);
            GUI.color = old;
            return;
        }
        if (singleRepairPartOutline && !backGlowOnly)
        {
            // Repair pickups already receive the mesh rim highlight. Drawing
            // all three offset frames made alloy, nails and multi-mesh tapes
            // look as if they had duplicate/ghost outlines.
            GUI.DrawTexture(purpleRect, purpleFrame, ScaleMode.StretchToFill, true);
            return;
        }
        GUI.DrawTexture(glowRect, purpleFrame, ScaleMode.StretchToFill, true);
        GUI.DrawTexture(purpleRect, purpleFrame, ScaleMode.StretchToFill, true);
        GUI.DrawTexture(greenRect, greenFrame, ScaleMode.StretchToFill, true);
    }

    private static bool IsScene2RepairPart(Transform candidate)
    {
        for (Transform current = candidate; current != null; current = current.parent)
        {
            string objectName = current.name.Trim().ToLowerInvariant();
            if (objectName == "alloy1" || objectName == "alloy2" || objectName == "nail")
                return true;
            if (objectName == "cidai1" || objectName == "cidai2" ||
                objectName == "cidai3" || objectName == "cidai4" ||
                objectName == "caidai1" || objectName == "caidai2" ||
                objectName == "caidai3" || objectName == "caidai4")
                return true;
        }
        return false;
    }

    private static bool IsBackGlowRepairPart(Transform candidate)
    {
        for (Transform current = candidate; current != null; current = current.parent)
        {
            string objectName = current.name.Trim().ToLowerInvariant();
            if (objectName == "alloy1" || objectName == "alloy2" || objectName == "nail")
                return true;
        }
        return false;
    }

    private static bool IsNamedInHierarchy(Transform candidate, string expectedName)
    {
        for (Transform current = candidate; current != null; current = current.parent)
            if (string.Equals(current.name.Trim(), expectedName, System.StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private static bool IsGlassesVisual(Transform candidate)
    {
        if (!candidate) return false;
        foreach (Transform child in candidate.GetComponentsInChildren<Transform>(true))
        {
            string objectName = child.name.Trim();
            if (string.Equals(objectName, "polySurface24", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(objectName, "眼镜框", System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static bool IsButtonVisual(Transform candidate)
    {
        if (!candidate) return false;
        return IsNamedInHierarchy(candidate, "button up") ||
               IsNamedInHierarchy(candidate, "button down");
    }

    private void BuildLavenderOverlay()
    {
        // zhuzi2 owns the whole telescoping assembly. Cloning its mesh hierarchy
        // creates a second glowing pillar behind the real device, so it uses the
        // screen-space linear frame only and never creates projection meshes.
        if (frameOnlyHighlight) return;
        // A second camera renders these proxy meshes after the monochrome camera effect.
        // The original glasses stay untouched for gameplay, colliders, and pickup.
        const int overlayLayer = 31;
<<<<<<< Updated upstream
        sharedDeviceOverlay=telescopingDevice != null || revealOwner != null || scene2Object;
        GameObject cameraObject=sharedDeviceOverlay ? GameObject.Find("Scene2 Hover Overlay Camera") : null;
        if(cameraObject==null){
            cameraObject=new GameObject(sharedDeviceOverlay ? "Scene2 Hover Overlay Camera" : "Lavender Hover Overlay Camera");
            overlayCamera=cameraObject.AddComponent<Camera>();
        } else overlayCamera=cameraObject.GetComponent<Camera>();
        // Preserve the main camera's depth buffer.  Clearing it here made a hidden
        // object (such as the Film inside the drawer) appear to glow through the
        // drawer and book, which visually detached the glow from the Film.
        overlayCamera.clearFlags = CameraClearFlags.Nothing;
        overlayCamera.cullingMask = 1 << overlayLayer;
        overlayCamera.depth = viewCamera != null ? viewCamera.depth + 10f : 10f;
        overlayCamera.allowHDR = false;
        overlayCamera.allowMSAA = true;
=======
        if (!backGlowOnly)
        {
            GameObject cameraObject = new GameObject("Lavender Hover Overlay Camera");
            overlayCamera = cameraObject.AddComponent<Camera>();
            // Preserve the main camera's depth buffer.  Clearing it here made a hidden
            // object (such as the Film inside the drawer) appear to glow through the
            // drawer and book, which visually detached the glow from the Film.
            overlayCamera.clearFlags = CameraClearFlags.Nothing;
            overlayCamera.cullingMask = 1 << overlayLayer;
            overlayCamera.depth = viewCamera != null ? viewCamera.depth + 10f : 10f;
            overlayCamera.allowHDR = false;
            overlayCamera.allowMSAA = true;
            // Every highlight proxy uses the same reserved layer. Leaving all
            // overlay cameras enabled makes every camera draw the currently
            // active proxy again, producing duplicate/offset silhouettes.
            overlayCamera.enabled = false;
        }
>>>>>>> Stashed changes
        Shader rimShader = Shader.Find("Custom/HoverLavenderRim");
        if (rimShader == null) return;
        // Scene2 uses the same dedicated overlay layer as the Scene1 scanner.
        // Render its tint only once, through the overlay camera.
        if (sharedDeviceOverlay && viewCamera != null) viewCamera.cullingMask &= ~(1 << overlayLayer);
        lavenderMaterial = new Material(rimShader);
<<<<<<< Updated upstream
        lavenderMaterial.SetColor("_RimColor", new Color(.55f, .28f, .90f, .34f));
        lavenderMaterial.SetFloat("_RimPower", 2.8f);
=======
        Color repairTint = subtleAlloy2Highlight ? new Color(.58f, .25f, .92f, .25f) : new Color(.70f, .28f, 1f, .72f);
        lavenderMaterial.SetColor("_RimColor", backGlowOnly ? repairTint : glassesHighlight ? glassesOutlineColor : buttonHighlight ? buttonOutlineColor : new Color(.55f, .28f, .90f, .34f));
        lavenderMaterial.SetColor("_AccentColor", new Color(.18f, 1f, .48f, .55f));
        lavenderMaterial.SetFloat("_AccentStrength", backGlowOnly ? (subtleAlloy2Highlight ? .30f : .62f) : 0f);
        lavenderMaterial.SetFloat("_RimPower", backGlowOnly ? 2.15f : glassesHighlight ? glassesOutlinePower : buttonHighlight ? buttonOutlinePower : 2.8f);
        lavenderMaterial.SetFloat("_GlowIntensity", backGlowOnly ? (subtleAlloy2Highlight ? .72f : 1.65f) : glassesHighlight ? glassesOutlineIntensity : buttonHighlight ? buttonOutlineIntensity : 1f);
        lavenderMaterial.SetFloat("_Additive", 0f);
        // A very light full-surface tint matches the reference while the rim
        // remains dominant. The proxy is exactly aligned, so this cannot trail.
        lavenderMaterial.SetFloat("_FullSurfaceGlow", backGlowOnly ? (subtleAlloy2Highlight ? .035f : .26f) : glassesHighlight ? glassesSurfaceGlow : buttonHighlight ? buttonSurfaceGlow : 0f);
        lavenderMaterial.SetFloat("_BackGlow", 0f);
        lavenderMaterial.SetInt("_Cull", (int)CullMode.Off);
        lavenderMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        lavenderMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);

        whiteGlowMaterial = new Material(rimShader);
        whiteGlowMaterial.SetColor("_RimColor", pickupFlashColor);
        whiteGlowMaterial.SetFloat("_RimPower", 2.8f);
        whiteGlowMaterial.SetFloat("_GlowIntensity", pickupFlashIntensity);
        whiteGlowMaterial.SetFloat("_Additive", 1f);
        whiteGlowMaterial.SetFloat("_FullSurfaceGlow", 1f);
        whiteGlowMaterial.SetInt("_SrcBlend", (int)BlendMode.One);
        whiteGlowMaterial.SetInt("_DstBlend", (int)BlendMode.One);
>>>>>>> Stashed changes

        lavenderOverlayRoot = new GameObject(backGlowOnly ? "Repair Part Aligned Highlight" : "Lavender Glasses Hover Overlay").transform;
        if (backGlowOnly)
        {
            lavenderOverlayRoot.SetParent(visualRoot.parent, false);
            CopyLocalTransform(visualRoot, lavenderOverlayRoot);
        }
        MeshFilter[] filters = visualRoot.GetComponentsInChildren<MeshFilter>(true);
<<<<<<< Updated upstream
        var sources=new System.Collections.Generic.List<MeshFilter>();
        var proxies=new System.Collections.Generic.List<Transform>();
=======
        if (backGlowOnly)
        {
            lavenderMaterial.SetFloat("_OutlineWidth", 0f);
            float meshSize = 0f;
            foreach (MeshFilter item in filters)
                if (item.sharedMesh) meshSize = Mathf.Max(meshSize, item.sharedMesh.bounds.size.magnitude);
            repairMeshSize = meshSize;
            repairOutlineMaterial = new Material(rimShader);
            repairOutlineMaterial.SetColor("_RimColor", subtleAlloy2Highlight ? alloy2OutlineColor : new Color(.72f, .22f, 1f, .78f));
            repairOutlineMaterial.SetColor("_AccentColor", new Color(.16f, 1f, .45f, .72f));
            repairOutlineMaterial.SetFloat("_AccentStrength", subtleAlloy2Highlight ? 0f : .45f);
            repairOutlineMaterial.SetFloat("_RimPower", 2f);
            repairOutlineMaterial.SetFloat("_GlowIntensity", subtleAlloy2Highlight ? alloy2GlowIntensity : 2.1f);
            repairOutlineMaterial.SetFloat("_Additive", 1f);
            repairOutlineMaterial.SetFloat("_FullSurfaceGlow", 0f);
            repairOutlineMaterial.SetFloat("_BackGlow", 1f);
            repairOutlineMaterial.SetFloat("_OutlineWidth", Mathf.Max(.0005f, meshSize * (subtleAlloy2Highlight ? alloy2OutlineWidthRatio : .012f)));
            repairOutlineMaterial.SetInt("_Cull", (int)CullMode.Front);
            repairOutlineMaterial.SetInt("_SrcBlend", (int)BlendMode.One);
            repairOutlineMaterial.SetInt("_DstBlend", (int)BlendMode.One);
            if (subtleAlloy2Highlight)
            {
                alloy2OuterGlowMaterial = new Material(repairOutlineMaterial);
                Color outerColor = alloy2OutlineColor;
                outerColor.a *= alloy2OuterGlowStrength;
                alloy2OuterGlowMaterial.SetColor("_RimColor", outerColor);
                alloy2OuterGlowMaterial.SetFloat("_GlowIntensity", alloy2GlowIntensity * alloy2OuterGlowStrength);
                alloy2OuterGlowMaterial.SetFloat("_OutlineWidth", Mathf.Max(.0005f, meshSize * alloy2OutlineWidthRatio * alloy2GlowSpread));
            }
        }
>>>>>>> Stashed changes
        foreach (MeshFilter source in filters)
        {
            if (source.sharedMesh == null) continue;
            GameObject proxy = new GameObject(source.name + " Lavender Proxy");
<<<<<<< Updated upstream
            sources.Add(source);proxies.Add(proxy.transform);
            proxy.layer = overlayLayer;
            proxy.transform.SetParent(lavenderOverlayRoot, true);
            proxy.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            proxy.transform.localScale = source.transform.lossyScale;
=======
            proxy.layer = backGlowOnly ? source.gameObject.layer : overlayLayer;
            if (backGlowOnly)
            {
                if (source.transform == visualRoot)
                {
                    proxy.transform.SetParent(lavenderOverlayRoot, false);
                    proxy.transform.localPosition = Vector3.zero;
                    proxy.transform.localRotation = Quaternion.identity;
                    proxy.transform.localScale = Vector3.one;
                }
                else
                {
                    Transform proxyParent = BuildMirroredPath(source.transform.parent);
                    proxy.transform.SetParent(proxyParent, false);
                    CopyLocalTransform(source.transform, proxy.transform);
                }
            }
            else
            {
                proxy.transform.SetParent(lavenderOverlayRoot, true);
                proxy.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
                proxy.transform.localScale = source.transform.lossyScale;
            }
>>>>>>> Stashed changes
            MeshFilter filter = proxy.AddComponent<MeshFilter>();
            filter.sharedMesh = source.sharedMesh;
            MeshRenderer renderer = proxy.AddComponent<MeshRenderer>();
            int materialCount = Mathf.Max(1, source.GetComponent<MeshRenderer>() != null ? source.GetComponent<MeshRenderer>().sharedMaterials.Length : 1);
            Material[] materials = new Material[materialCount];
            for (int i = 0; i < materials.Length; i++) materials[i] = lavenderMaterial;
            renderer.sharedMaterials = materials;
<<<<<<< Updated upstream
        }
        proxySources=sources.ToArray();proxyTransforms=proxies.ToArray();
        lavenderOverlayRoot.gameObject.SetActive(false);
        SyncOverlayCamera();
    }

=======
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            // alloy2 only needs the outer purple contour. Its exact-surface
            // duplicate is hidden to prevent the pink overexposed appearance.
            if (subtleAlloy2Highlight) renderer.enabled = false;
            overlaySources.Add(source.transform);
            overlayProxies.Add(proxy.transform);
            overlayRenderers.Add(renderer);

            if (backGlowOnly && repairOutlineMaterial)
            {
                GameObject outlineProxy = new GameObject(source.name + " Back Glow Outline");
                outlineProxy.layer = source.gameObject.layer;
                outlineProxy.transform.SetParent(proxy.transform.parent, false);
                CopyLocalTransform(proxy.transform, outlineProxy.transform);
                outlineProxy.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
                MeshRenderer outlineRenderer = outlineProxy.AddComponent<MeshRenderer>();
                Material[] outlineMaterials = new Material[materialCount];
                for (int i = 0; i < outlineMaterials.Length; i++) outlineMaterials[i] = repairOutlineMaterial;
                outlineRenderer.sharedMaterials = outlineMaterials;
                outlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
                outlineRenderer.receiveShadows = false;

                if (subtleAlloy2Highlight && alloy2OuterGlowMaterial)
                {
                    GameObject outerGlowProxy = new GameObject(source.name + " Soft Outer Purple Glow");
                    outerGlowProxy.layer = source.gameObject.layer;
                    outerGlowProxy.transform.SetParent(proxy.transform.parent, false);
                    CopyLocalTransform(proxy.transform, outerGlowProxy.transform);
                    outerGlowProxy.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
                    MeshRenderer outerGlowRenderer = outerGlowProxy.AddComponent<MeshRenderer>();
                    Material[] outerGlowMaterials = new Material[materialCount];
                    for (int i = 0; i < outerGlowMaterials.Length; i++) outerGlowMaterials[i] = alloy2OuterGlowMaterial;
                    outerGlowRenderer.sharedMaterials = outerGlowMaterials;
                    outerGlowRenderer.shadowCastingMode = ShadowCastingMode.Off;
                    outerGlowRenderer.receiveShadows = false;
                }
            }
        }
        SetOverlayVisible(false);
        SyncOverlayCamera();
    }

    private void SetOverlayVisible(bool visible)
    {
        if (lavenderOverlayRoot != null)
            lavenderOverlayRoot.gameObject.SetActive(visible);
        if (overlayCamera != null)
            overlayCamera.enabled = visible;
    }

    private Transform BuildMirroredPath(Transform sourceParent)
    {
        if (!sourceParent || sourceParent == visualRoot) return lavenderOverlayRoot;
        var chain = new List<Transform>();
        for (Transform current = sourceParent; current && current != visualRoot; current = current.parent)
            chain.Add(current);
        Transform parent = lavenderOverlayRoot;
        for (int i = chain.Count - 1; i >= 0; i--)
        {
            Transform copy = new GameObject(chain[i].name + " Glow Transform").transform;
            copy.SetParent(parent, false);
            CopyLocalTransform(chain[i], copy);
            parent = copy;
        }
        return parent;
    }

    private static void CopyLocalTransform(Transform source, Transform destination)
    {
        destination.localPosition = source.localPosition;
        destination.localRotation = source.localRotation;
        destination.localScale = source.localScale;
    }

    private void ApplyOverlayMaterial(Material material)
    {
        if (material == null) return;
        foreach (MeshRenderer renderer in overlayRenderers)
        {
            if (renderer == null) continue;
            int count = Mathf.Max(1, renderer.sharedMaterials.Length);
            Material[] materials = new Material[count];
            for (int i = 0; i < count; i++) materials[i] = material;
            renderer.sharedMaterials = materials;
        }
    }

    private void SyncOverlayVisuals()
    {
        if (backGlowOnly)
        {
            if (lavenderOverlayRoot && visualRoot) CopyLocalTransform(visualRoot, lavenderOverlayRoot);
            // Keep exposed alloy2 settings live while adjusting them in Play Mode.
            if (subtleAlloy2Highlight && repairOutlineMaterial)
            {
                repairOutlineMaterial.SetColor("_RimColor", alloy2OutlineColor);
                repairOutlineMaterial.SetFloat("_GlowIntensity", alloy2GlowIntensity);
                repairOutlineMaterial.SetFloat("_OutlineWidth", Mathf.Max(.0005f, repairMeshSize * alloy2OutlineWidthRatio));
                if (alloy2OuterGlowMaterial)
                {
                    Color outerColor = alloy2OutlineColor;
                    outerColor.a *= alloy2OuterGlowStrength;
                    alloy2OuterGlowMaterial.SetColor("_RimColor", outerColor);
                    alloy2OuterGlowMaterial.SetFloat("_GlowIntensity", alloy2GlowIntensity * alloy2OuterGlowStrength);
                    alloy2OuterGlowMaterial.SetFloat("_OutlineWidth", Mathf.Max(.0005f, repairMeshSize * alloy2OutlineWidthRatio * alloy2GlowSpread));
                }
            }
            return;
        }
        // Keep the exposed glasses settings live while tuning in Play Mode.
        if (glassesHighlight && lavenderMaterial)
        {
            lavenderMaterial.SetColor("_RimColor", glassesOutlineColor);
            lavenderMaterial.SetFloat("_GlowIntensity", glassesOutlineIntensity);
            lavenderMaterial.SetFloat("_RimPower", glassesOutlinePower);
            lavenderMaterial.SetFloat("_FullSurfaceGlow", glassesSurfaceGlow);
        }
        if (buttonHighlight && lavenderMaterial)
        {
            lavenderMaterial.SetColor("_RimColor", buttonOutlineColor);
            lavenderMaterial.SetFloat("_GlowIntensity", buttonOutlineIntensity);
            lavenderMaterial.SetFloat("_RimPower", buttonOutlinePower);
            lavenderMaterial.SetFloat("_FullSurfaceGlow", buttonSurfaceGlow);
        }
        int count = Mathf.Min(overlaySources.Count, overlayProxies.Count);
        for (int i = 0; i < count; i++)
        {
            if (overlaySources[i] == null || overlayProxies[i] == null) continue;
            overlayProxies[i].SetPositionAndRotation(overlaySources[i].position, overlaySources[i].rotation);
            overlayProxies[i].localScale = overlaySources[i].lossyScale;
        }
    }

>>>>>>> Stashed changes
    private void SyncOverlayCamera()
    {
        if (overlayCamera == null || viewCamera == null) return;
        overlayCamera.transform.SetPositionAndRotation(viewCamera.transform.position, viewCamera.transform.rotation);
        overlayCamera.fieldOfView = viewCamera.fieldOfView;
        overlayCamera.nearClipPlane = viewCamera.nearClipPlane;
        overlayCamera.farClipPlane = viewCamera.farClipPlane;
        overlayCamera.aspect = viewCamera.aspect;
    }

    private bool TryGetScreenRect(out Rect result)
    {
<<<<<<< Updated upstream
        Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) { result = default; return false; }
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
=======
        result=default;
        if(!visualRoot||!visualRoot.gameObject.activeInHierarchy)return false;
        bool hasVisibleSource=false;
        Bounds bounds=default;
        foreach(var renderer in visualRoot.GetComponentsInChildren<Renderer>(true))
        {
            if(!renderer||renderer.gameObject.layer==31||!renderer.enabled||!renderer.gameObject.activeInHierarchy||renderer.forceRenderingOff)continue;
            if(!hasVisibleSource){bounds=renderer.bounds;hasVisibleSource=true;}
            else bounds.Encapsulate(renderer.bounds);
        }
        if(!hasVisibleSource)return false;
>>>>>>> Stashed changes
        Vector3 min = bounds.min, max = bounds.max;
        Vector3[] corners =
        {
            new Vector3(min.x,min.y,min.z), new Vector3(max.x,min.y,min.z), new Vector3(min.x,max.y,min.z), new Vector3(max.x,max.y,min.z),
            new Vector3(min.x,min.y,max.z), new Vector3(max.x,min.y,max.z), new Vector3(min.x,max.y,max.z), new Vector3(max.x,max.y,max.z)
        };
        float left = float.PositiveInfinity, right = float.NegativeInfinity, bottom = float.PositiveInfinity, top = float.NegativeInfinity;
        bool inFront = false;
        foreach (Vector3 corner in corners)
        {
            Vector3 point = viewCamera.WorldToScreenPoint(corner);
            if (point.z <= 0f) continue;
            inFront = true;
            left = Mathf.Min(left, point.x); right = Mathf.Max(right, point.x);
            bottom = Mathf.Min(bottom, point.y); top = Mathf.Max(top, point.y);
        }
        result = new Rect(left, bottom, right - left, top - bottom);
        return inFront && result.width > 1f && result.height > 1f;
    }

    private static Rect Expand(Rect rect, float amount)
    {
        return new Rect(rect.x - amount, rect.y - amount, rect.width + amount * 2f, rect.height + amount * 2f);
    }

    private static Texture2D CreateNeonFrame(Color color)
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Vector2 uv = new Vector2((x + .5f) / size, (y + .5f) / size);
            float radius = .115f;
            Vector2 p = new Vector2(Mathf.Abs(uv.x - .5f), Mathf.Abs(uv.y - .5f)) - new Vector2(.5f - radius, .5f - radius);
            float signedDistance = new Vector2(Mathf.Max(p.x, 0f), Mathf.Max(p.y, 0f)).magnitude + Mathf.Min(Mathf.Max(p.x, p.y), 0f) - radius;
            float sharpLine = Mathf.Exp(-Mathf.Abs(signedDistance) * 520f);
            float softGlow = Mathf.Exp(-Mathf.Abs(signedDistance) * 85f) * .34f;
            texture.SetPixel(x, y, new Color(color.r, color.g, color.b, Mathf.Clamp01((sharpLine + softGlow) * color.a)));
        }
        texture.Apply(false, true);
        return texture;
    }

    private void OnDisable()
    {
        // The purple rim is rendered by a separate overlay object, so disabling
        // the pickup model alone would otherwise leave that proxy visible.
        hovered = false;
<<<<<<< Updated upstream
        if (lavenderOverlayRoot != null)
            lavenderOverlayRoot.gameObject.SetActive(false);
=======
        forcedHighlight = false;
        SetOverlayVisible(false);
>>>>>>> Stashed changes
    }

    private void OnDestroy()
    {
        if (purpleFrame != null) Destroy(purpleFrame);
        if (greenFrame != null) Destroy(greenFrame);
        if (lavenderMaterial != null) Destroy(lavenderMaterial);
<<<<<<< Updated upstream
=======
        if (repairOutlineMaterial != null) Destroy(repairOutlineMaterial);
        if (alloy2OuterGlowMaterial != null) Destroy(alloy2OuterGlowMaterial);
        if (whiteGlowMaterial != null) Destroy(whiteGlowMaterial);
>>>>>>> Stashed changes
        if (lavenderOverlayRoot != null) Destroy(lavenderOverlayRoot.gameObject);
        if (overlayCamera != null && !sharedDeviceOverlay) Destroy(overlayCamera.gameObject);
    }
}
