using UnityEngine;

/// <summary>
/// Owns the Film pickup and its use on the television.
/// Attach to the stable FilmFunction parent, never to the replaceable Film mesh.
/// </summary>
public sealed class FilmFunctionController : MonoBehaviour
{
    [Header("Hierarchy References")]
    [SerializeField] private Transform filmVisualRoot;
    [SerializeField] private Collider filmCollider;
    [SerializeField] private Collider screenCollider;

    [Header("Existing Systems")]
    [SerializeField] private Camera interactionCamera;
    [SerializeField] private FirstPersonPlayer player;
    [SerializeField] private AntennaPuzzleController antennaPuzzle;

    [Header("Interaction")]
    [SerializeField, Min(20f)] private float interactionDistance = 20f;
    [SerializeField, Min(1f)] private float clickRayDistance = 100f;
    [SerializeField, Min(.1f)] private float approachLoreDistance = 8f;

    private bool pickedUp;
    private bool approachLoreShown;
    private PurpleBoxUnlockController purpleBox;
    private bool pickupArmed;
    private int pickupArmedFrame=-1;
    private HoverScanHighlight filmHighlight;
    private readonly System.Collections.Generic.List<GameObject> clickQueries=new System.Collections.Generic.List<GameObject>();
    public bool CanCollectFilm
    {
        get
        {
            if(!purpleBox)
                foreach(var candidate in FindObjectsOfType<PurpleBoxUnlockController>(true))
                    if(candidate.gameObject.scene==gameObject.scene){purpleBox=candidate;break;}
            return purpleBox&&purpleBox.IsUnlocked&&Time.frameCount>purpleBox.UnlockedFrame;
        }
    }

    private void Awake()
    {
        if (interactionCamera == null)
            interactionCamera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
        if (player == null)
            player = FindObjectOfType<FirstPersonPlayer>();
        if (antennaPuzzle == null)
            antennaPuzzle = FindObjectOfType<AntennaPuzzleController>();
        if (filmVisualRoot == null)
            filmVisualRoot = transform.Find("film pick up");
        if(filmVisualRoot==null&&filmCollider)filmVisualRoot=filmCollider.transform;
        if (filmCollider == null && filmVisualRoot != null)
            filmCollider = filmVisualRoot.GetComponentInChildren<Collider>(true);
        // Some saved controllers point at an empty "film pick up" marker.
        if(filmCollider&&(!filmVisualRoot||!filmVisualRoot.GetComponentInChildren<MeshFilter>(true)))
            filmVisualRoot=filmCollider.transform;
        if (filmCollider == null)
            Debug.LogError("FilmFunction: Please add a Collider manually to the Film object.", this);
        if (screenCollider == null)
        {
            GameObject screen = GameObject.Find("screem");
            if (screen != null)
                screenCollider = screen.GetComponent<Collider>();
        }

        pickedUp = player != null && player.HasFilm;
        if(!pickedUp&&filmCollider)
        {
            var hover=filmCollider.GetComponent<HoverScanHighlight>();if(!hover)hover=filmCollider.gameObject.AddComponent<HoverScanHighlight>();
            hover.enabled=true;hover.Configure(6,interactionDistance);hover.ConfigureViewCamera(interactionCamera);
            hover.ConfigureVisualRoot(filmVisualRoot);filmHighlight=hover;
        }
        if (pickedUp && filmVisualRoot != null)
            filmVisualRoot.gameObject.SetActive(false);
    }

    private bool IsLowerDrawerFilmCopy()
    {
        for(var ancestor=filmCollider?filmCollider.transform:transform;ancestor;ancestor=ancestor.parent)
            if(ancestor.name=="drawerUp (1)")return true;
        return false;
    }
    private void ResolvePlayerView()
    {
        if(!player)player=FindObjectOfType<FirstPersonPlayer>();
        if(player&&player.PlayerCamera)interactionCamera=player.PlayerCamera;
        interactionDistance=Mathf.Max(20f,interactionDistance);
        if(filmHighlight){filmHighlight.ConfigureViewCamera(interactionCamera);filmHighlight.Configure(6,interactionDistance);}
    }
    private void Update()
    {
        ResolvePlayerView();
        // Both saved drawers contain a Film copy. Once the item is owned,
        // neither the duplicate nor its legacy parent may leave a scan frame.
        if(IsLowerDrawerFilmCopy()||(player&&player.HasFilm))
        {
            foreach(var hover in GetComponentsInChildren<HoverScanHighlight>(true)){hover.SetPersistentHighlight(false);hover.enabled=false;}
            if(filmHighlight){filmHighlight.SetPersistentHighlight(false);filmHighlight.enabled=false;}
        }
        else if(filmHighlight)filmHighlight.SetPersistentHighlight(!pickedUp&&CanCollectFilm);
        if(!pickedUp)
        {
            if(!CanCollectFilm){pickupArmed=false;pickupArmedFrame=-1;}
            else if(!pickupArmed&&!Input.GetMouseButton(0))
            {pickupArmed=true;pickupArmedFrame=Time.frameCount;}
        }
        TryShowApproachLore();
        if (Scene2PickupPresentation.IsPlaying||!Input.GetMouseButtonDown(0) || interactionCamera == null || player == null)
            return;

        Ray ray = interactionCamera.ScreenPointToRay(Input.mousePosition);
        if(TryPickupFilm(Input.mousePosition))return;

        if (pickedUp && IsClicked(ray, screenCollider) && antennaPuzzle != null)
        {
            if (!antennaPuzzle.TryInstallFilm())
                Debug.Log("[FilmFunction] Film is held, but the TV is not ready for installation yet.", this);
        }
    }

    public bool TryPickupFilm(Vector2 pointer)
    {
        ResolvePlayerView();
        if(pickedUp||!pickupArmed||Time.frameCount<=pickupArmedFrame||!CanCollectFilm||Scene2PickupPresentation.IsPlaying||!player||!interactionCamera||!IsFilmClicked(interactionCamera.ScreenPointToRay(pointer)))return false;
            var mesh=filmVisualRoot?filmVisualRoot.GetComponentInChildren<MeshFilter>(true):null;
            if(!mesh&&filmCollider)mesh=filmCollider.GetComponentInChildren<MeshFilter>(true);
            if(!mesh||!Scene2PickupPresentation.BeginScene1Film(mesh,mesh.GetComponent<Renderer>()))return false;
            pickedUp = true;
            player.GiveFilm();
            foreach(var hover in filmCollider.GetComponentsInChildren<HoverScanHighlight>(true))hover.enabled=false;
            filmCollider.enabled=false;
            foreach(var renderer in mesh.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
            if(mesh.transform!=transform)mesh.gameObject.SetActive(false);
            if (filmVisualRoot != null)
            {
                foreach(var renderer in filmVisualRoot.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
                filmVisualRoot.gameObject.SetActive(false);
            }
            else mesh.gameObject.SetActive(false);
            Debug.Log("[FilmFunction] Film picked up.", this);
            return true;
    }
    private void TryShowApproachLore()
    {
        if (approachLoreShown || pickedUp || filmCollider == null || player == null) return;
        if (Vector3.Distance(player.transform.position, filmCollider.ClosestPoint(player.transform.position)) > approachLoreDistance)
            return;

        WorldStartTerminal terminal = FindObjectOfType<WorldStartTerminal>();
        if (terminal == null || !terminal.IsGameStarted) return;

        approachLoreShown = true;
        terminal.ShowLoreMessage(
            "ADDITIONAL VISUAL DATA",
            "ADDITIONAL VISUAL DATA DETECTED.\n\nAN UNREAD MEMORY FILE STILL EXISTS.\n\nUNLIKE DIGITAL RECORDS...\nTHIS MEMORY HAS BEEN PRESERVED IN ITS MOST ORIGINAL FORM.");
    }
    private bool IsFilmClicked(Ray ray)
    {
        if(!filmCollider||!filmCollider.enabled||!IsClicked(ray,filmCollider)||!filmCollider.Raycast(ray,out var filmHit,clickRayDistance))return false;
        var hits=Physics.RaycastAll(ray,clickRayDistance,~((1<<30)|(1<<31)),QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
        foreach(var hit in hits)
        {
            if(player&&(hit.transform==player.transform||hit.transform.IsChildOf(player.transform)))continue;
            if(hit.collider==filmCollider||hit.transform.IsChildOf(filmCollider.transform))return true;
            // The auto-fitted enclosure is not the opened lid or the lock plate.
            if(purpleBox&&purpleBox.IsUnlocked&&hit.transform==purpleBox.transform&&hit.collider is BoxCollider)continue;
            if(purpleBox&&purpleBox.IsUnlocked&&hit.transform.IsChildOf(purpleBox.transform)&&hit.collider is BoxCollider)
            {
                // Imported box bounds include empty interior space. Check its
                // actual surface so the open container cannot mask the Film.
                var mesh=hit.transform.GetComponent<MeshFilter>();
                if(mesh&&mesh.sharedMesh)
                {
                    var query=new GameObject("Film Click Surface Query");query.layer=30;query.transform.SetParent(hit.transform,false);clickQueries.Add(query);
                    var exact=query.AddComponent<MeshCollider>();exact.sharedMesh=mesh.sharedMesh;Physics.SyncTransforms();
                    bool blocked=exact.Raycast(ray,out var surface,filmHit.distance-.001f);
                    exact.enabled=false;Destroy(query);clickQueries.Remove(query);
                    if(!blocked)continue;
                }
            }
            return false;
        }
        return false;
    }
    private void OnDestroy(){foreach(var query in clickQueries)if(query)Destroy(query);}

    private bool IsClicked(Ray ray, Collider target)
    {
        if (target == null)
            return false;

        Vector3 closest = target.ClosestPoint(player.transform.position);
        if (Vector3.Distance(player.transform.position, closest) > interactionDistance)
            return false;

        // Film lives inside a drawer; the drawer's collider can block a global
        // raycast even while the Film is visibly clickable. Test this exact collider.
        return target.Raycast(ray, out _, clickRayDistance);
    }

}
