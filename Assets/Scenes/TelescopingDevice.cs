using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public sealed class TelescopingDevice : MonoBehaviour
{
    [SerializeField] private Transform fixedTube;
    [SerializeField] private Transform fixedCap;
    [SerializeField] private Transform movingCap;
    [SerializeField] private Collider clickCollider;
    [SerializeField] private Transform[] followingParts = new Transform[0];
    [SerializeField, Min(.1f)] private float duration = 2.5f;
    [SerializeField, Min(1f)] private float interactionDistance = 100f;
    [SerializeField] private UnityEvent onRetracted = new UnityEvent();
    private bool retracting;
    private bool retracted;
    public System.Func<bool> RetractionGate { get; set; }
    public bool IsRetracted => retracted;
    public bool IsRetracting => retracting;
    public Transform MovingCap => movingCap;

    public void Configure(Transform tube, Transform cap, Transform upperCap, Collider hitTarget)
    {
        fixedTube=tube; fixedCap=cap; movingCap=upperCap; clickCollider=hitTarget;
        interactionDistance=100f;
    }
    public Transform[] FollowingParts => followingParts;
    public void ConfigureFollowingParts(params Transform[] parts)
    {
        followingParts=parts.Where(t=>t && t!=transform && t!=fixedTube && t!=fixedCap && t!=movingCap
            && !transform.IsChildOf(t) && !movingCap.IsChildOf(t)).Distinct().ToArray();
    }
    private void Update()
    {
        if(EvidenceInspect.BlocksWorldInput)return;
        if(retracting || retracted || !Application.isFocused || !Input.GetMouseButtonDown(0)) return;
        Camera camera=Camera.main;
        if(!camera) return;
        Vector2 pointer=Cursor.lockState==CursorLockMode.Locked
            ? new Vector2(camera.pixelRect.center.x,camera.pixelRect.center.y)
            : (Vector2)Input.mousePosition;
        TryInteractAtScreenPosition(camera,pointer);
    }
    public bool TryInteractAtScreenPosition(Camera camera,Vector2 pointer)
    {
        if(!camera || !camera.pixelRect.Contains(pointer)) return false;
        Ray ray=camera.ScreenPointToRay(pointer);
        ray.origin=camera.transform.position;
        Physics.SyncTransforms();
        return CanInteract(ray,camera.transform.position,camera.farClipPlane) && TryRetract();
    }
    private void OnDisable()
    {
        // Code reloads or disabling a script stop coroutines. Permit the next
        // click to finish the remaining travel rather than leaving it busy forever.
        StopAllCoroutines();
        retracting=false;
    }
    public bool CanInteract(Ray ray, Vector3 viewerPosition, float rayDistance)
    {
        if(retracting || retracted || !clickCollider) return false;
        var targets=GetComponentsInChildren<Collider>().Where(c=>c.enabled && !c.isTrigger && c.gameObject.activeInHierarchy && OwnsCollider(c)).ToArray();
        if(targets.Length==0) return false;
        // Measure proximity to the surface, not the pivot or the particular
        // pixel hit. There is no minimum range, including right beside the tube.
        float distanceSquared=targets.Min(c=>(c.ClosestPoint(viewerPosition)-viewerPosition).sqrMagnitude);
        if(distanceSquared > (interactionDistance+.001f)*(interactionDistance+.001f)) return false;
        if(targets.Any(c=>(c.ClosestPoint(ray.origin)-ray.origin).sqrMagnitude<.000001f)) return true;
        var player=FindObjectOfType<FirstPersonPlayer>();
        var hits=Physics.RaycastAll(ray,rayDistance,~0,QueryTriggerInteraction.Ignore);
        foreach(var hit in hits.OrderBy(h=>h.distance))
        {
            if(player && hit.transform.IsChildOf(player.transform)) continue;
            return OwnsCollider(hit.collider);
        }
        return false;
    }
    private bool OwnsCollider(Collider candidate)
    {
        return candidate && candidate.GetComponentInParent<TelescopingDevice>()==this;
    }
    public bool TryRetract()
    {
        if(retracting || retracted || !fixedTube || !fixedCap || !movingCap) return false;
        if(RetractionGate!=null&&!RetractionGate())return false;
        var tubeRenderer=GetComponent<Renderer>();
        var baseRenderer=fixedCap.GetComponent<Renderer>();
        var upperRenderer=movingCap.GetComponent<Renderer>();
        var outerRenderer=fixedTube.GetComponent<Renderer>();
        if(!tubeRenderer || !baseRenderer || !upperRenderer || !outerRenderer) return false;
        float gap=Mathf.Max(.1f,baseRenderer.bounds.size.y*.15f);
        float capTravel=upperRenderer.bounds.min.y-(baseRenderer.bounds.max.y+gap);
        float tubeTravel=tubeRenderer.bounds.max.y-(baseRenderer.bounds.max.y-gap*.25f);
        if(capTravel<=0 || tubeTravel<=0) return false;
        // The inner rod fits inside the fixed outer tube. Both parts retain their
        // authored shape; the short extra rod travel tucks its tip under the cap.
        retracting=true;
        StartCoroutine(Retract(tubeTravel,capTravel));
        return true;
    }
    private IEnumerator Retract(float tubeTravel,float capTravel)
    {
        Vector3 tubeStart=transform.position;
        Vector3 capStart=movingCap.position;
        // Move assembly roots only: children already inherit their parent motion.
        var roots=followingParts.Where(t=>t && !t.IsChildOf(movingCap)
            && !followingParts.Any(other=>other && other!=t && t.IsChildOf(other))).ToArray();
        var starts=roots.Select(t=>t.position).ToArray();
        float elapsed=0f;
        while(elapsed<duration)
        {
            elapsed+=Time.deltaTime;
            float progress=Mathf.SmoothStep(0f,1f,Mathf.Clamp01(elapsed/duration));
            transform.position=tubeStart+Vector3.down*tubeTravel*progress;
            movingCap.position=capStart+Vector3.down*capTravel*progress;
            for(int i=0;i<roots.Length;i++) if(roots[i]) roots[i].position=starts[i]+Vector3.down*capTravel*progress;
            yield return null;
        }
        transform.position=tubeStart+Vector3.down*tubeTravel;
        movingCap.position=capStart+Vector3.down*capTravel;
        for(int i=0;i<roots.Length;i++) if(roots[i]) roots[i].position=starts[i]+Vector3.down*capTravel;
        // It is completely inside the opaque fixed tube now. Disable its own
        // renderer and collider to avoid nearly coincident sleeve surfaces.
        GetComponent<Renderer>().enabled=false;
        foreach(var collider in GetComponentsInChildren<Collider>()) if(OwnsCollider(collider)) collider.enabled=false;
        retracted=true; retracting=false;
        var reveal=GetComponent<TubeRewardReveal>();
        if(reveal)reveal.BeginReveal(movingCap.GetComponent<Renderer>().bounds.max.y);
        onRetracted.Invoke();
    }
}
