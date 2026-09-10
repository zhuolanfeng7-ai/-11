using System.Collections;
using UnityEngine;

/// <summary>
/// After PosionPuzzle is collected, clicking the vase or rose pours the potion.
/// It deliberately uses renderer bounds for selection, so the two art objects do
/// not need extra hand-authored colliders.
/// </summary>
[RequireComponent(typeof(PosionPickupController))]
public sealed class PoisonUseController : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Transform vase;
    [SerializeField] private Transform rose;
    [SerializeField, Min(.1f)] private float useDistance = 23f;

    [Header("First-person hand presentation")]
    [SerializeField] private Vector3 handLocalPosition = new Vector3(.43f, -.34f, .72f);
    [SerializeField] private Vector3 handLocalEulerAngles = new Vector3(8f, 195f, -8f);
    [SerializeField, Min(.01f)] private float handScale = .24f;

    [Header("Pour presentation")]
    [SerializeField, Min(.1f)] private float capSpinSeconds = 1.0f;
    [SerializeField, Min(.1f)] private float worldBottleScaleMultiplier = 4f;
    [SerializeField, Min(.01f)] private float bottleHeightAboveTarget = .20f;
    [SerializeField, Min(.1f)] private float pourSeconds = 2.2f;
    [SerializeField] private Color liquidColor = new Color(0.035f, 0.38f, 0.07f, 0.94f);
    [SerializeField] private Color wiltColor = new Color(0.22f, 0.12f, 0.12f, 1f);

    private PosionPickupController pickup;
    private FirstPersonPlayer player;
    private Camera viewCamera;
    private bool busy;
    private bool roseWilted;
    private bool handPresentationShown;
    private bool cluePetalVisible;
    private string debugPhase = "Waiting for poison pickup";
    private float cluePetalStartTime;
    private Texture2D cluePetalTexture;
    private TargetWorldState vaseState;
    private TargetWorldState roseState;
    private Collider vaseInteractionCollider;
    private Collider roseInteractionCollider;

    public bool DebugIsBusy => busy;
    public bool DebugRoseWilted => roseWilted;
    public bool DebugHandPresentationShown => handPresentationShown;
    public string DebugPhase => debugPhase;
    public Transform DebugVase => vase;
    public Transform DebugRose => rose;
    public float DebugUseDistance => useDistance;

    private void Awake()
    {
        pickup = GetComponent<PosionPickupController>();
        player = FindObjectOfType<FirstPersonPlayer>();
        viewCamera = player != null && player.PlayerCamera != null
            ? player.PlayerCamera : (Camera.main != null ? Camera.main : FindObjectOfType<Camera>());
    }

    private void Start()
    {
        if (vase == null) vase = FindNamedTransform("vase");
        if (rose == null) rose = FindNamedTransform("rose");
        vaseState = TargetWorldState.Capture(vase);
        roseState = TargetWorldState.Capture(rose);
        vaseInteractionCollider = EnsureInteractionCollider(vase);
        roseInteractionCollider = EnsureInteractionCollider(rose);
        debugPhase = vase != null && rose != null ? "Ready: waiting for a click on Vase or Rose" : "ERROR: Vase or Rose reference missing";
    }

    private void Update()
    {
        // These are permanent world props. Restore their original state before
        // handling any poison interaction, even if an older component changed it.
        vaseState?.Restore();
        roseState?.Restore();
        if (cluePetalVisible)
        {
            Vector2 guiMousePosition = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            if (Input.GetMouseButtonDown(0) && GetCluePetalRect().Contains(guiMousePosition))
                cluePetalVisible = false;
            return;
        }
        if (busy || handPresentationShown || pickup == null || !pickup.IsPickedUp || !Input.GetMouseButtonDown(0)) return;
        if (!TryGetClickedTarget(out Transform target)) return;
        StartCoroutine(PourRoutine(target));
    }

    private bool TryGetClickedTarget(out Transform target)
    {
        target = null;
        if (viewCamera == null) return false;
        Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);

        // Do not use a single global Physics.Raycast here. The television
        // screen sits in front of the flower in camera space and was stealing
        // that ray, after which the overlapping GUI-bounds fallback always
        // chose Vase first. Raycast the two intended targets directly instead.
        float nearestDistance = float.PositiveInfinity;
        if (TryRaycastTarget(ray, vase, vaseInteractionCollider, out float vaseDistance) && vaseDistance < nearestDistance)
        {
            nearestDistance = vaseDistance;
            target = vase;
        }
        if (TryRaycastTarget(ray, rose, roseInteractionCollider, out float roseDistance) && roseDistance < nearestDistance)
        {
            target = rose;
            nearestDistance = roseDistance;
        }
        if (target != null) return true;

        // Safety fallback for unusual imported meshes. Prefer Rose if their
        // screen projections overlap, because Rose is the only target that
        // needs the wilt-and-petal result.
        bool roseHovered = rose != null && IsNear(rose) && PointerTouchesRenderer(rose);
        bool vaseHovered = vase != null && IsNear(vase) && PointerTouchesRenderer(vase);
        if (roseHovered)
        {
            target = rose;
            return true;
        }
        if (vaseHovered) { target = vase; return true; }
        return false;
    }

    private bool TryRaycastTarget(Ray ray, Transform target, Collider targetCollider, out float distance)
    {
        distance = float.PositiveInfinity;
        if (target == null || targetCollider == null || !targetCollider.enabled || !IsNear(target)) return false;
        if (!targetCollider.Raycast(ray, out RaycastHit hit, 100f)) return false;
        distance = hit.distance;
        return true;
    }

    private Collider EnsureInteractionCollider(Transform target)
    {
        if (target == null) return null;
        Collider existing = target.GetComponent<Collider>();
        if (existing != null) return existing;

        // Imported flower/vase art did not have reliable root colliders. Add a
        // runtime-only BoxCollider fitted to the visual bounds, so direct target
        // raycasts remain precise and are never blocked by the TV screen.
        BoxCollider box = target.gameObject.AddComponent<BoxCollider>();
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return box;
        Bounds world = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) world.Encapsulate(renderers[i].bounds);
        Vector3 min = world.min, max = world.max;
        Bounds local = new Bounds(target.InverseTransformPoint(min), Vector3.zero);
        for (int x = 0; x <= 1; x++) for (int y = 0; y <= 1; y++) for (int z = 0; z <= 1; z++)
            local.Encapsulate(target.InverseTransformPoint(new Vector3(
                x == 0 ? min.x : max.x,
                y == 0 ? min.y : max.y,
                z == 0 ? min.z : max.z)));
        box.center = local.center;
        box.size = local.size;
        box.isTrigger = true;
        return box;
    }

    private bool IsNear(Transform target)
    {
        Transform origin = player != null ? player.transform : viewCamera.transform;
        return Vector3.Distance(origin.position, GetBounds(target).center) <= useDistance;
    }

    private bool PointerTouchesRenderer(Transform target)
    {
        Bounds bounds = GetBounds(target);
        Vector3 min = bounds.min, max = bounds.max;
        float left = float.PositiveInfinity, right = float.NegativeInfinity;
        float bottom = float.PositiveInfinity, top = float.NegativeInfinity;
        bool visible = false;
        for (int x = 0; x <= 1; x++) for (int y = 0; y <= 1; y++) for (int z = 0; z <= 1; z++)
        {
            Vector3 point = viewCamera.WorldToScreenPoint(new Vector3(x == 0 ? min.x : max.x, y == 0 ? min.y : max.y, z == 0 ? min.z : max.z));
            if (point.z <= 0f) continue;
            visible = true;
            left = Mathf.Min(left, point.x); right = Mathf.Max(right, point.x);
            bottom = Mathf.Min(bottom, point.y); top = Mathf.Max(top, point.y);
        }
        return visible && new Rect(left, bottom, right - left, top - bottom).Contains(Input.mousePosition);
    }

    private IEnumerator PourRoutine(Transform target)
    {
        busy = true;
        debugPhase = "Target accepted: opening PosionCap";
        KeepTargetVisible(target);
        Bounds targetBounds = GetBounds(target);
        Vector3 impactPoint = targetBounds.center + Vector3.up * targetBounds.extents.y * .35f;
        // The pickup remains hidden in the world. A temporary first-person copy is
        // shown only while the player uses it, fixed to the lower-right camera view.
        GameObject handBottle = CreateHandBottle();
        Transform handCap = handBottle != null ? FindCap(handBottle.transform) : null;
        if (handCap != null)
        {
            Vector3 capStartPosition = handCap.localPosition;
            Quaternion capStartRotation = handCap.localRotation;
            for (float elapsed = 0f; elapsed < capSpinSeconds; elapsed += Time.deltaTime)
            {
                float t = elapsed / capSpinSeconds;
                handCap.localRotation = capStartRotation * Quaternion.Euler(0f, 0f, -720f * t);
                handCap.localPosition = capStartPosition + Vector3.up * (.12f * t);
                yield return null;
            }
        }

        // Once opened, place the whole bottle directly over the selected flower.
        // Its lowest point is kept twenty centimetres above the top of the rose.
        if (handBottle != null)
        {
            handBottle.transform.SetParent(null, true);
            handBottle.transform.localScale *= worldBottleScaleMultiplier;
            Quaternion startRotation = handBottle.transform.rotation;
            Quaternion endRotation = startRotation * Quaternion.Euler(0f, 0f, 105f);
            handBottle.transform.rotation = endRotation;
            handBottle.transform.position = new Vector3(targetBounds.center.x, handBottle.transform.position.y, targetBounds.center.z);
            Bounds bottleBounds = GetBounds(handBottle.transform);
            handBottle.transform.position += Vector3.up * (targetBounds.max.y + bottleHeightAboveTarget - bottleBounds.min.y);
        }

        debugPhase = "Cap open: positioning bottle over target";

        // The green liquid now travels only from the world-space bottle above the
        // target to the flower head, never across the screen from the UI corner.
        Vector3 bottleMouth = handCap != null
            ? handCap.position
            : targetBounds.center + Vector3.up * (targetBounds.extents.y + bottleHeightAboveTarget);
        GameObject stream = CreateLiquidStream(bottleMouth, impactPoint);
        GameObject pourParticles = CreatePourParticles(bottleMouth, impactPoint);
        debugPhase = "Pouring green liquid";
        for (float elapsed = 0f; elapsed < pourSeconds; elapsed += Time.deltaTime)
            yield return null;
        if (stream != null) Destroy(stream);
        if (pourParticles != null) Destroy(pourParticles);
        CreateLiquidSplash(impactPoint);

        // Vase and Rose are two valid ways to begin the same poison event.
        // Regardless of which one is clicked, the completed pour always
        // affects the Rose and triggers its wilt/petal sequence once.
        if (!roseWilted && rose != null)
        {
            debugPhase = "Liquid reached Rose: applying wilt";
            yield return StartCoroutine(WiltRose());
        }

        if (handBottle != null) Destroy(handBottle);
        handPresentationShown = true;
        busy = false;
        debugPhase = roseWilted ? "Complete: Rose wilted" : "Complete: Vase interaction finished";
    }

    // vase and rose are permanent world props. This interaction only makes a
    // temporary camera-attached copy of the poison; it never hides or moves either target.
    private static void KeepTargetVisible(Transform target)
    {
        if (target == null) return;
        target.gameObject.SetActive(true);
        foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = true;
    }

    private GameObject CreateHandBottle()
    {
        if (viewCamera == null) return null;
        GameObject copy = Instantiate(gameObject);
        copy.name = "Posion First-Person Use Presentation";
        // The clone has the original scripts and collider because it comes from the
        // functional parent. They must not run a second pickup or hover interaction.
        foreach (MonoBehaviour behaviour in copy.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
        foreach (Collider collider in copy.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (Renderer renderer in copy.GetComponentsInChildren<Renderer>(true)) renderer.enabled = true;
        copy.transform.SetParent(viewCamera.transform, false);
        copy.transform.localPosition = handLocalPosition;
        copy.transform.localRotation = Quaternion.Euler(handLocalEulerAngles);
        copy.transform.localScale = Vector3.one * handScale;
        return copy;
    }

    private GameObject CreateLiquidStream(Vector3 from, Vector3 to)
    {
        Vector3 direction = to - from;
        GameObject stream = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stream.name = "Posion Liquid Stream";
        stream.transform.position = (from + to) * .5f;
        stream.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
        stream.transform.localScale = new Vector3(.045f, direction.magnitude * .5f, .045f);
        Destroy(stream.GetComponent<Collider>());
        Material material = new Material(Shader.Find("Standard"));
        material.color = liquidColor;
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", liquidColor * 1.6f);
        stream.GetComponent<Renderer>().material = material;
        return stream;
    }

    private GameObject CreatePourParticles(Vector3 from, Vector3 to)
    {
        Vector3 direction = to - from;
        GameObject particles = new GameObject("Green Poison Pour Particles");
        particles.transform.SetPositionAndRotation(from, Quaternion.LookRotation(direction.normalized));
        ParticleSystem ps = particles.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = pourSeconds;
        main.startLifetime = Mathf.Max(.2f, direction.magnitude / 8f);
        main.startSpeed = 8f;
        main.startSize = .045f;
        main.startColor = liquidColor;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = ps.emission;
        emission.rateOverTime = 55f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 4f;
        shape.radius = .025f;
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        ps.Play(true);
        Destroy(particles, pourSeconds + 1.2f);
        return particles;
    }

    private void CreateLiquidSplash(Vector3 at)
    {
        GameObject splash = new GameObject("Green Poison Impact Splash");
        splash.transform.position = at;
        ParticleSystem ps = splash.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = .8f;
        main.startLifetime = .7f;
        main.startSpeed = .8f;
        main.startSize = .06f;
        main.startColor = liquidColor;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = .08f;
        ps.Play(true);
        Destroy(splash, 2f);
    }

    private IEnumerator WiltRose()
    {
        roseWilted = true;
        Debug.Log("[Poison] Green poison reached rose: applying persistent brown wilt material.", rose);
        Renderer[] renderers = rose.GetComponentsInChildren<Renderer>(true);
        Material[][] materials = new Material[renderers.Length][];
        Color[][] startColors = new Color[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] sourceMaterials = renderers[i].materials;
            materials[i] = new Material[sourceMaterials.Length];
            startColors[i] = new Color[materials[i].Length];
            for (int j = 0; j < materials[i].Length; j++)
            {
                startColors[i][j] = GetMaterialColor(sourceMaterials[j]);
                // A new Standard material guarantees a visible brown wilt even if
                // the original imported material does not expose a tint property.
                Shader shader = Shader.Find("Standard");
                Shader fallbackShader = sourceMaterials[j] != null ? sourceMaterials[j].shader : Shader.Find("Unlit/Color");
                Material wiltMaterial = new Material(shader != null ? shader : fallbackShader);
                // Do not retain the colourful source texture: a solid, matte brown
                // is unmistakable once the global world-colour effect is restored.
                wiltMaterial.color = startColors[i][j];
                wiltMaterial.SetFloat("_Glossiness", .12f);
                materials[i][j] = wiltMaterial;
            }
            renderers[i].materials = materials[i];
        }
        CreatePetalFall(GetBounds(rose).center);
        for (float elapsed = 0f; elapsed < 1.6f; elapsed += Time.deltaTime)
        {
            float t = elapsed / 1.6f;
            for (int i = 0; i < materials.Length; i++)
            for (int j = 0; j < materials[i].Length; j++)
            {
                SetMaterialColor(materials[i][j], Color.Lerp(startColors[i][j], wiltColor, t));
            }
            yield return null;
        }
        for (int i = 0; i < materials.Length; i++)
        for (int j = 0; j < materials[i].Length; j++)
            SetMaterialColor(materials[i][j], wiltColor);
        Debug.Log("[Poison] Rose wilt material is now fully brown and persistent.", rose);
        ShowAngleCluePetal();
    }

    private void CreatePetalFall(Vector3 at)
    {
        GameObject petals = new GameObject("Rose Falling Petals");
        ParticleSystem ps = petals.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.duration = 2f; main.startLifetime = 1.8f; main.startSpeed = 1.1f; main.startSize = .13f; main.startColor = new Color(.32f, .12f, .035f, .96f); main.loop = false; main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = ps.emission; emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });
        var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .22f;
        var velocity = ps.velocityOverLifetime; velocity.enabled = true; velocity.y = -.8f;
        petals.transform.position = at;
        ps.Play(true);
        Destroy(petals, 3f);
    }

    private void ShowAngleCluePetal()
    {
        if (cluePetalTexture == null) cluePetalTexture = CreateCluePetalTexture();
        cluePetalStartTime = Time.unscaledTime;
        cluePetalVisible = true;
    }

    private void OnGUI()
    {
        if (!cluePetalVisible || cluePetalTexture == null) return;
        Rect finalRect = GetCluePetalRect();
        float t = Mathf.Clamp01((Time.unscaledTime - cluePetalStartTime) / .55f);
        float eased = 1f - Mathf.Pow(1f - t, 3f);
        Rect fallingRect = finalRect;
        fallingRect.y = Mathf.Lerp(-finalRect.height, finalRect.y, eased);
        GUI.DrawTexture(fallingRect, cluePetalTexture, ScaleMode.StretchToFill, true);

        GUIStyle clueStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.width * .045f, 28f, 56f)),
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(.12f, .035f, .015f, 1f) }
        };
        GUI.Label(fallingRect, "WATCH THE\nANGLE", clueStyle);
    }

    private static Rect GetCluePetalRect()
    {
        float width = Mathf.Min(Screen.width * .5f, 560f);
        float height = width * .72f;
        // Centered horizontally, with its visual center slightly below screen center.
        return new Rect((Screen.width - width) * .5f, (Screen.height - height) * .5f + Screen.height * .12f, width, height);
    }

    private static Texture2D CreateCluePetalTexture()
    {
        const int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
        Color edge = new Color(.13f, .035f, .012f, 1f);
        Color center = new Color(.34f, .12f, .035f, 1f);
        Color vein = new Color(.09f, .02f, .008f, 1f);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float px = (x + .5f) / size * 2f - 1f;
            float py = (y + .5f) / size * 2f - 1f;
            // A slightly pointed, asymmetrical rose petal silhouette.
            float width = .78f * (1f - .18f * py);
            float d = Mathf.Sqrt((px / width) * (px / width) + (py / .92f) * (py / .92f));
            float alpha = d <= 1f ? 1f : 0f;
            float tone = Mathf.Clamp01(1f - d * .52f);
            Color color = Color.Lerp(edge, center, tone);
            float centralVein = Mathf.Exp(-Mathf.Abs(px) * 28f) * .26f;
            float diagonalVeins = Mathf.Max(
                Mathf.Exp(-Mathf.Abs(py - px * .42f) * 30f),
                Mathf.Exp(-Mathf.Abs(py + px * .42f) * 30f)) * .1f;
            color = Color.Lerp(color, vein, centralVein + diagonalVeins);
            color.a = alpha;
            texture.SetPixel(x, y, color);
        }
        texture.Apply(false, true);
        return texture;
    }

    private static Bounds GetBounds(Transform target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(target.position, Vector3.one);
        Bounds result = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) result.Encapsulate(renderers[i].bounds);
        return result;
    }

    private static Transform FindNamedTransform(string objectName)
    {
        GameObject found = GameObject.Find(objectName);
        return found != null ? found.transform : null;
    }

    private static Transform FindCap(Transform root)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name.Trim().Equals("PosionCap", System.StringComparison.OrdinalIgnoreCase)) return child;
        return null;
    }

    private bool IsRoseTarget(Transform target)
    {
        return target != null && rose != null &&
               (target == rose || target.IsChildOf(rose) || rose.IsChildOf(target));
    }

    private static Color GetMaterialColor(Material material)
    {
        if (material == null) return Color.white;
        if (material.HasProperty("_Color")) return material.color;
        return material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : Color.white;
    }

    private static void SetMaterialColor(Material material, Color color)
    {
        if (material == null) return;
        if (material.HasProperty("_Color")) material.color = color;
        else if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", Color.black);
    }

    private void OnDestroy()
    {
        if (cluePetalTexture != null) Destroy(cluePetalTexture);
    }

    private sealed class TargetWorldState
    {
        private readonly Transform target;
        private readonly Transform parent;
        private readonly Vector3 localPosition;
        private readonly Quaternion localRotation;
        private readonly Vector3 localScale;

        private TargetWorldState(Transform value)
        {
            target = value;
            parent = value.parent;
            localPosition = value.localPosition;
            localRotation = value.localRotation;
            localScale = value.localScale;
        }

        public static TargetWorldState Capture(Transform value) => value == null ? null : new TargetWorldState(value);

        public void Restore()
        {
            if (target == null) return;
            if (target.parent != parent) target.SetParent(parent, false);
            target.gameObject.SetActive(true);
            target.localPosition = localPosition;
            target.localRotation = localRotation;
            target.localScale = localScale;
            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true)) renderer.enabled = true;
        }
    }
}
