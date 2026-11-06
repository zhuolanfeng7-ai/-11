using UnityEngine;

/// <summary>Collect the sketch through the shared three-flash archive presentation.</summary>
public sealed class PictureInspectController : MonoBehaviour
{
    [SerializeField] private Texture2D frontTexture;
    [SerializeField] private Texture2D backTexture;
    [SerializeField,Min(20f)] private float interactionDistance=20f;
    [SerializeField,Min(.1f)] private float approachLoreDistance=12f;
    private Camera viewCamera;
    private Collider pictureCollider;
    private Renderer[] pictureRenderers;
    private FirstPersonPlayer player;
    private bool approachLoreShown,completionLoreShown,collected;
    public bool IsCollected=>collected;
    private void Awake()
    {
        
        player=FindObjectOfType<FirstPersonPlayer>();ResolvePlayerCamera();
        pictureCollider=GetComponent<Collider>();
        if(!pictureCollider)pictureCollider=gameObject.AddComponent<BoxCollider>();
        pictureRenderers=GetComponentsInChildren<Renderer>(true);
        interactionDistance=Mathf.Max(interactionDistance,approachLoreDistance);
        var hover=GetComponent<HoverScanHighlight>();if(!hover)hover=gameObject.AddComponent<HoverScanHighlight>();
        hover.enabled=true;hover.ConfigureVisualRoot(transform);hover.ConfigureViewCamera(viewCamera);hover.Configure(8,interactionDistance);
    }
    private void ResolvePlayerCamera()
    {
        if(!player)player=FindObjectOfType<FirstPersonPlayer>();
        interactionDistance=Mathf.Max(20f,interactionDistance);
        var actual=player?player.PlayerCamera:null;
        if(!actual&&Camera.main&&!Camera.main.targetTexture&&Camera.main.gameObject.layer<30)actual=Camera.main;
        if(!actual)
            foreach(var candidate in FindObjectsOfType<Camera>())
                if(candidate.gameObject.scene==gameObject.scene&&candidate.enabled&&!candidate.targetTexture
                    &&candidate.gameObject.layer<30&&!candidate.name.Contains("Overlay")&&!candidate.name.Contains("Surveillance")
                    &&(candidate.cullingMask&1)!=0){actual=candidate;break;}
        if(!actual)return;
        viewCamera=actual;
        var hover=GetComponent<HoverScanHighlight>();if(hover){hover.ConfigureViewCamera(actual);hover.Configure(8,interactionDistance);}
    }
    private void Update()
    {
        if(collected)
        {
            if(!Scene2PickupPresentation.IsPlaying&&Input.GetMouseButtonDown(0))TryReview(Input.mousePosition);
            return;
        }
        ResolvePlayerCamera();
        TryShowApproachLore();
        if(Input.GetMouseButtonDown(0))TryPickup(Input.mousePosition);
    }
    public bool TryPickup(Vector2 pointer)
    {
        if(collected||Scene2PickupPresentation.IsPlaying||!viewCamera||!pictureCollider||!pictureCollider.enabled)return false;
        var ray=viewCamera.ScreenPointToRay(pointer);
        if(!pictureCollider.Raycast(ray,out var hit,100f))return false;
        var origin=player?player.transform.position:viewCamera.transform.position;
        if(Vector3.Distance(origin,hit.point)>interactionDistance)return false;
        if(Physics.Raycast(ray,out var first,hit.distance+.01f,~((1<<30)|(1<<31)),QueryTriggerInteraction.Ignore)
            &&first.collider!=pictureCollider&&!first.transform.IsChildOf(transform))return false;
        Texture portrait=frontTexture;
        if(!portrait)foreach(var renderer in pictureRenderers)if(renderer&&renderer.sharedMaterial&&renderer.sharedMaterial.mainTexture){portrait=renderer.sharedMaterial.mainTexture;break;}
        if(!Scene2PickupPresentation.BeginScene1Sketch(portrait,backTexture))return false;
        collected=true;
        foreach(var hover in GetComponentsInChildren<HoverScanHighlight>(true))hover.enabled=false;
        foreach(var renderer in pictureRenderers)if(renderer)renderer.enabled=false;
        foreach(var collider in GetComponentsInChildren<Collider>(true))collider.enabled=false;
        return true;
    }
    public bool TryReview(Vector2 pointer)
    {
        if(!collected||Scene2PickupPresentation.IsPlaying)return false;
        var ui=FindObjectOfType<ImportantItemCollectionUI>();
        if(!ui||!ui.VisibleIcon(3)||!ui.SlotScreenRect(3).Contains(pointer))return false;
        return Scene2PickupPresentation.BeginScene1Sketch(frontTexture,backTexture,true);
    }
    private void TryShowApproachLore()
    {
        if (approachLoreShown || player == null) return;
        Bounds pictureBounds = GetPictureBounds();
        if (Vector3.Distance(player.transform.position, pictureBounds.ClosestPoint(player.transform.position)) > approachLoreDistance)
            return;

        WorldStartTerminal terminal = FindObjectOfType<WorldStartTerminal>();
        if (terminal == null || !terminal.IsGameStarted) return;

        approachLoreShown = true;
        Debug.Log("[PictureSketch] Player approached picture sketch; showing visual residue lore.", this);
        terminal.ShowLoreMessage(
            "UNREAD VISUAL RESIDUE",
            "UNREAD VISUAL RESIDUE DETECTED.\n\nTHIS DATA WAS NEVER REGISTERED IN THE SENSORY TRADING SYSTEM.\n\nNO COMMERCIAL VALUE.\nNO MARKET PRICE.\nNO CORPORATE DEMAND.\n\nYET...\n\nYOUR MEMORY SYSTEM CONTINUES TO PRESERVE IT.");
    }

    private Bounds GetPictureBounds()
    {
        if (pictureRenderers == null || pictureRenderers.Length == 0)
            return new Bounds(transform.position, Vector3.one);
        Bounds bounds = pictureRenderers[0].bounds;
        for (int i = 1; i < pictureRenderers.Length; i++)
            if (pictureRenderers[i] != null) bounds.Encapsulate(pictureRenderers[i].bounds);
        return bounds;
    }

    private void ShowCompletionLore()
    {
        if (completionLoreShown) return;
        WorldStartTerminal terminal = FindObjectOfType<WorldStartTerminal>();
        if (terminal == null || !terminal.IsGameStarted) return;

        completionLoreShown = true;
        terminal.ShowLoreMessage(
            "ORIGINAL MEMORY DETECTED",
            "ORIGINAL MEMORY DETECTED.\n\nNO MARKET VALUE.\nNO COMMERCIAL PURPOSE.\nNO BUYER FOUND.\n\nYET...\n\nIT REMAINED.\n\nBECAUSE LOVE IS NOT AN ASSET.\nFAMILY CANNOT BE SOLD.");
    }

}
