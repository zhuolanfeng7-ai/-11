using UnityEngine;

/// <summary>
/// Opens the four archive sheets carried by Cube (2) through Cube (5) when
/// the world object named Box Box is clicked.
/// </summary>
public sealed class BoxArchiveViewer : MonoBehaviour
{
    [SerializeField] private string boxName = "Box Box";
    [SerializeField, Min(.1f)] private float interactionDistance = 7f;

    private readonly string[] archiveNames = { "Cube (2)", "Cube (3)", "Cube (4)", "Cube (5)" };
    private GameObject boxObject;
    private Collider boxCollider;
    private Camera viewCamera;
    private FirstPersonPlayer player;
    private GameObject[] archives = new GameObject[4];
    private Texture[] archiveTextures = new Texture[4];
    private bool[] rendererStates = new bool[4];
    private bool open;
    private GUIStyle titleStyle;
    private GUIStyle closeStyle;

    private void Awake()
    {
        player = FindObjectOfType<FirstPersonPlayer>();
        viewCamera = player != null ? player.PlayerCamera : Camera.main;
        FindBox();
        CacheArchives();
    }

    private void Update()
    {
        if (viewCamera == null) viewCamera = player != null ? player.PlayerCamera : Camera.main;
        if (open)
        {
            if (Input.GetMouseButtonDown(0)) CloseArchiveViewer();
            return;
        }
        TryOpenArchiveViewer();
    }

    private void FindBox()
    {
        if (boxObject != null) return;
        boxObject = GameObject.Find(boxName) ?? GameObject.Find("Box") ?? GameObject.Find("BoxPuzzle");
        if (boxObject == null)
        {
            foreach (Transform candidate in FindObjectsOfType<Transform>())
            {
                string normalized = candidate.name.Replace(" ", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
                if (normalized.Contains("box")) { boxObject = candidate.gameObject; break; }
            }
        }
        if (boxObject == null) return;
        boxCollider = boxObject.GetComponentInChildren<Collider>(true);
        if (boxCollider == null) boxCollider = AddFittedCollider(boxObject);
    }

    private void CacheArchives()
    {
        for (int i = 0; i < archiveNames.Length; i++)
        {
            archives[i] = GameObject.Find(archiveNames[i]);
            if (archives[i] == null) continue;
            Renderer renderer = archives[i].GetComponentInChildren<Renderer>(true);
            archiveTextures[i] = renderer != null && renderer.sharedMaterial != null ? renderer.sharedMaterial.mainTexture : null;
        }
    }

    private void TryOpenArchiveViewer()
    {
        if (!Input.GetMouseButtonDown(0) || viewCamera == null) return;
        FindBox();
        // A missing or renamed box must never throw a NullReferenceException
        // and prevent subsequent player clicks from being processed.
        if (boxObject == null) return;
        Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, ~0, QueryTriggerInteraction.Collide)) return;
        // Imported boxes commonly have separate colliders on their child mesh
        // pieces. Accept any of those pieces, not only the first collider.
        if (hit.transform != boxObject.transform && !hit.transform.IsChildOf(boxObject.transform)) return;
        Transform origin = player != null ? player.transform : viewCamera.transform;
        if (Vector3.Distance(origin.position, hit.collider.ClosestPoint(origin.position)) > interactionDistance) return;
        OpenArchiveViewer();
    }

    private static BoxCollider AddFittedCollider(GameObject root)
    {
        BoxCollider box = root.AddComponent<BoxCollider>();
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return box;
        Bounds world = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) world.Encapsulate(renderers[i].bounds);
        Bounds local = new Bounds(root.transform.InverseTransformPoint(world.center), Vector3.zero);
        Vector3 min = world.min, max = world.max;
        for (int x = 0; x <= 1; x++) for (int y = 0; y <= 1; y++) for (int z = 0; z <= 1; z++)
            local.Encapsulate(root.transform.InverseTransformPoint(new Vector3(x == 0 ? min.x : max.x, y == 0 ? min.y : max.y, z == 0 ? min.z : max.z)));
        box.center = local.center;
        box.size = local.size;
        box.isTrigger = true;
        return box;
    }

    private void OpenArchiveViewer()
    {
        CacheArchives();
        open = true;
        for (int i = 0; i < archives.Length; i++)
        {
            if (archives[i] == null) continue;
            Renderer[] renderers = archives[i].GetComponentsInChildren<Renderer>(true);
            rendererStates[i] = renderers.Length > 0 && renderers[0].enabled;
            foreach (Renderer renderer in renderers) renderer.enabled = false;
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void CloseArchiveViewer()
    {
        open = false;
        for (int i = 0; i < archives.Length; i++)
        {
            if (archives[i] == null) continue;
            foreach (Renderer renderer in archives[i].GetComponentsInChildren<Renderer>(true)) renderer.enabled = rendererStates[i];
        }
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }

    private void OnGUI()
    {
        if (!open) return;
        EnsureStyles();
        GUI.color = new Color(.015f, .005f, .03f, .91f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(0, 25f, Screen.width, 28f), "PERSONAL CAPACITY ARCHIVE", titleStyle);
        GUI.Label(new Rect(0, Screen.height - 48f, Screen.width, 22f), "CLICK ANYWHERE TO CLOSE", closeStyle);

        float maxHeight = Mathf.Min(360f, Screen.height - 145f);
        float gap = 22f;
        float[] widths = new float[archiveTextures.Length];
        float totalWidth = 0f;
        for (int i = 0; i < archiveTextures.Length; i++)
        {
            float aspect = archiveTextures[i] != null ? archiveTextures[i].width / (float)archiveTextures[i].height : .72f;
            widths[i] = maxHeight * Mathf.Clamp(aspect, .45f, 1.15f);
            totalWidth += widths[i];
        }
        totalWidth += gap * (archiveTextures.Length - 1);
        float x = Mathf.Max(22f, (Screen.width - totalWidth) * .5f);
        float y = (Screen.height - maxHeight) * .5f + 10f;
        for (int i = 0; i < archiveTextures.Length; i++)
        {
            Rect card = new Rect(x, y, widths[i], maxHeight);
            GUI.color = new Color(.68f, .28f, 1f, .9f);
            GUI.DrawTexture(new Rect(card.x - 3f, card.y - 3f, card.width + 6f, card.height + 6f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (archiveTextures[i] != null) GUI.DrawTexture(card, archiveTextures[i], ScaleMode.ScaleToFit, true);
            else GUI.Label(card, "ARCHIVE\nUNAVAILABLE", titleStyle);
            x += widths[i] + gap;
        }
    }

    private void EnsureStyles()
    {
        if (titleStyle != null) return;
        titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20, fontStyle = FontStyle.Bold };
        titleStyle.normal.textColor = new Color(.72f, .3f, 1f);
        closeStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 13 };
        closeStyle.normal.textColor = new Color(.3f, 1f, .54f);
    }
}
