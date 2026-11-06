using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Draws the hover scanner in OnGUI, after the camera's black-and-white/blur effect.
/// The scanner therefore remains purple and green even while the 3D world is monochrome.
/// </summary>
public sealed class HoverScanHighlight : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Camera viewCamera;
    [SerializeField] private Color purple = new Color(.78f, .20f, 1f, .95f);
    [SerializeField] private Color green = new Color(.15f, 1f, .42f, .9f);
    [Header("Pickup Soft Full-Surface Glow")]
    [SerializeField] private Color pickupFlashColor = Color.white;
    [SerializeField, Min(0f)] private float pickupFlashIntensity = 0.55f;
    [SerializeField, Min(1f)] private float framePaddingPixels = 20f;
    [SerializeField, Min(.1f)] private float hoverDistance = 5f;

    private Texture2D purpleFrame;
    private Texture2D greenFrame;
    private Transform lavenderOverlayRoot;
    private Camera overlayCamera;
    private Material lavenderMaterial;
    private Material whiteGlowMaterial;
    private FirstPersonPlayer player;
    private Rect hoverRect;
    private bool hovered;
    private bool forcedHighlight;
    private bool persistentHighlight;
    public bool PersistentHighlight=>persistentHighlight;
    private readonly List<Transform> overlaySources = new List<Transform>();
    private readonly List<Transform> overlayProxies = new List<Transform>();
    private readonly List<MeshRenderer> overlayRenderers = new List<MeshRenderer>();

    private TelescopingDevice telescopingDevice;
    [SerializeField] private TubeRewardReveal revealOwner;
    private bool sharedDeviceOverlay;
    [SerializeField] private bool scene2Object;
    [SerializeField] private MusicBoxRewardLift crankReward;
    [SerializeField] private Transform occlusionHousing;
    public void ConfigureOcclusionHousing(Transform housing){occlusionHousing=housing;}
    public bool IsHovered => hovered;


    private void Awake()
    {
        scene2Object=scene2Object||string.Equals(gameObject.scene.name.Replace(" ",""),"scene2",System.StringComparison.OrdinalIgnoreCase);
        telescopingDevice=GetComponent<TelescopingDevice>();
        if (visualRoot == null) visualRoot = transform;
        if (viewCamera == null) viewCamera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
        player = FindObjectOfType<FirstPersonPlayer>();
        purpleFrame = CreateNeonFrame(purple);
        greenFrame = CreateNeonFrame(green);
        BuildLavenderOverlay();
    }

    public void Configure(float paddingPixels, float maximumDistance)
    {
        framePaddingPixels = paddingPixels;
        hoverDistance = maximumDistance;
    }
    public void ConfigureViewCamera(Camera camera){if(camera)viewCamera=camera;}
    public void SetPersistentHighlight(bool value){persistentHighlight=value;}
    public void ConfigureVisualRoot(Transform root)
    {
        if(!root||visualRoot==root)return;
        visualRoot=root;
        if(lavenderOverlayRoot){lavenderOverlayRoot.gameObject.SetActive(false);Destroy(lavenderOverlayRoot.gameObject);}
        if(overlayCamera){overlayCamera.enabled=false;Destroy(overlayCamera.gameObject);}
        if(lavenderMaterial)Destroy(lavenderMaterial);if(whiteGlowMaterial)Destroy(whiteGlowMaterial);
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
            if (lavenderOverlayRoot != null)
                lavenderOverlayRoot.gameObject.SetActive(false);
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

    private void Update()
    {
        if(!forcedHighlight&&!persistentHighlight&&(scene2Object||telescopingDevice||revealOwner))return;
        if (viewCamera == null) viewCamera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
        hovered = false;
        if (lavenderOverlayRoot != null) lavenderOverlayRoot.gameObject.SetActive(false);
        if (viewCamera == null) return;
        SyncOverlayCamera();
        SyncOverlayVisuals();

        if (forcedHighlight)
        {
            hovered = TryGetScreenRect(out hoverRect);
            if (lavenderOverlayRoot != null) lavenderOverlayRoot.gameObject.SetActive(hovered);
            return;
        }
        if(persistentHighlight)
        {
            var renderer=visualRoot.GetComponentInChildren<Renderer>();
            var origin=player?player.transform.position:viewCamera.transform.position;
            if(!renderer||!renderer.enabled||Vector3.Distance(origin,renderer.bounds.ClosestPoint(origin))>hoverDistance)return;
            if(lavenderMaterial){lavenderMaterial.SetColor("_RimColor",new Color(.7f,.35f,1,.7f));lavenderMaterial.SetFloat("_GlowIntensity",1.8f);}
            hovered=TryGetScreenRect(out hoverRect);
            if(lavenderOverlayRoot)lavenderOverlayRoot.gameObject.SetActive(hovered);
            return;
        }

        RefreshPointerHover(viewCamera,Input.mousePosition);
    }

    // Use the same real-view ray for runtime hover and interaction verification.
    public bool RefreshPointerHover(Camera camera,Vector2 pointer)
    {
        if(!enabled||forcedHighlight||persistentHighlight||!camera)return false;
        viewCamera=camera;hovered=false;
        if(lavenderOverlayRoot)lavenderOverlayRoot.gameObject.SetActive(false);
        SyncOverlayCamera();SyncOverlayVisuals();
        Ray ray=viewCamera.ScreenPointToRay(pointer);
        if(!Physics.Raycast(ray,out RaycastHit hit,100f,~((1<<30)|(1<<31)),QueryTriggerInteraction.Collide))return false;
        if(hit.transform!=transform&&!hit.transform.IsChildOf(transform))return false;
        Transform distanceOrigin=player?player.transform:viewCamera.transform;
        if(Vector3.Distance(distanceOrigin.position,hit.collider.ClosestPoint(distanceOrigin.position))>hoverDistance)return false;
        hovered=TryGetScreenRect(out hoverRect);
        if(lavenderOverlayRoot)lavenderOverlayRoot.gameObject.SetActive(hovered);
        return hovered;
    }
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
        if(!enabled || forcedHighlight||persistentHighlight)return false;
        hovered = false;
        if (lavenderOverlayRoot != null) lavenderOverlayRoot.gameObject.SetActive(false);
        if ((telescopingDevice == null && revealOwner == null && !scene2Object) || camera == null || !camera.pixelRect.Contains(pointer)) return false;
        if(EvidenceInspect.BlocksWorldInput)return false;
        if (revealOwner != null && !revealOwner.IsRevealed) return false;
        if (crankReward != null && !crankReward.IsRaised) return false;
        viewCamera = camera;
        SyncOverlayVisuals();
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
        if (lavenderOverlayRoot != null) lavenderOverlayRoot.gameObject.SetActive(hovered);
        return hovered;
    }


    private void OnGUI()
    {
        if (forcedHighlight) return;
        if (!hovered || purpleFrame == null || greenFrame == null) return;
        // GUI y is top-down, while camera screen coordinates are bottom-up.
        Rect guiRect = new Rect(hoverRect.x, Screen.height - hoverRect.yMax, hoverRect.width, hoverRect.height);
        float padding = Mathf.Clamp(framePaddingPixels, 4f, 10f);
        Rect glowRect = Expand(guiRect, padding * 1.25f);
        Rect purpleRect = Expand(guiRect, padding * .65f);
        Rect greenRect = Expand(guiRect, padding * 1.25f);
        GUI.DrawTexture(glowRect, purpleFrame, ScaleMode.StretchToFill, true);
        GUI.DrawTexture(purpleRect, purpleFrame, ScaleMode.StretchToFill, true);
        GUI.DrawTexture(greenRect, greenFrame, ScaleMode.StretchToFill, true);
    }

    private void BuildLavenderOverlay()
    {
        // A second camera renders these proxy meshes after the monochrome camera effect.
        // The original glasses stay untouched for gameplay, colliders, and pickup.
        const int overlayLayer = 31;
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
        Shader rimShader = Shader.Find("Custom/HoverLavenderRim");
        if (rimShader == null) return;
        lavenderMaterial = new Material(rimShader);
        lavenderMaterial.SetColor("_RimColor", new Color(.55f, .28f, .90f, .34f));
        lavenderMaterial.SetFloat("_RimPower", 2.8f);
        lavenderMaterial.SetFloat("_GlowIntensity", 1f);
        lavenderMaterial.SetFloat("_Additive", 0f);
        lavenderMaterial.SetFloat("_FullSurfaceGlow", 0f);
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

        lavenderOverlayRoot = new GameObject("Lavender Glasses Hover Overlay").transform;
        MeshFilter[] filters = visualRoot.GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter source in filters)
        {
            if (source.sharedMesh == null) continue;
            GameObject proxy = new GameObject(source.name + " Lavender Proxy");
            proxy.layer = overlayLayer;
            proxy.transform.SetParent(lavenderOverlayRoot, true);
            proxy.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            proxy.transform.localScale = source.transform.lossyScale;
            MeshFilter filter = proxy.AddComponent<MeshFilter>();
            filter.sharedMesh = source.sharedMesh;
            MeshRenderer renderer = proxy.AddComponent<MeshRenderer>();
            int materialCount = Mathf.Max(1, source.GetComponent<MeshRenderer>() != null ? source.GetComponent<MeshRenderer>().sharedMaterials.Length : 1);
            Material[] materials = new Material[materialCount];
            for (int i = 0; i < materials.Length; i++) materials[i] = lavenderMaterial;
            renderer.sharedMaterials = materials;
            overlaySources.Add(source.transform);
            overlayProxies.Add(proxy.transform);
            overlayRenderers.Add(renderer);
        }
        lavenderOverlayRoot.gameObject.SetActive(false);
        SyncOverlayCamera();
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
        int count = Mathf.Min(overlaySources.Count, overlayProxies.Count);
        for (int i = 0; i < count; i++)
        {
            if (overlaySources[i] == null || overlayProxies[i] == null) continue;
            overlayProxies[i].SetPositionAndRotation(overlaySources[i].position, overlaySources[i].rotation);
            overlayProxies[i].localScale = overlaySources[i].lossyScale;
        }
    }

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
        result=default;
        if(!visualRoot||!visualRoot.gameObject.activeInHierarchy)return false;
        bool hasVisibleSource=false;
        Bounds bounds=default;
        foreach(var renderer in visualRoot.GetComponentsInChildren<Renderer>(true))
        {
            if(!renderer||!renderer.enabled||!renderer.gameObject.activeInHierarchy||renderer.forceRenderingOff)continue;
            if(!hasVisibleSource){bounds=renderer.bounds;hasVisibleSource=true;}
            else bounds.Encapsulate(renderer.bounds);
        }
        if(!hasVisibleSource)return false;
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
        forcedHighlight = false;
        if (lavenderOverlayRoot != null)
            lavenderOverlayRoot.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (purpleFrame != null) Destroy(purpleFrame);
        if (greenFrame != null) Destroy(greenFrame);
        if (lavenderMaterial != null) Destroy(lavenderMaterial);
        if (whiteGlowMaterial != null) Destroy(whiteGlowMaterial);
        if (lavenderOverlayRoot != null) Destroy(lavenderOverlayRoot.gameObject);
        if (overlayCamera != null) Destroy(overlayCamera.gameObject);
    }
}
