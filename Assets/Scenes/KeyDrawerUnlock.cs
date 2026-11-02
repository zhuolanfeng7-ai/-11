using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(11050)]
[DisallowMultipleComponent]
public sealed class KeyDrawerUnlock : MonoBehaviour
{
    [SerializeField] private Transform keySource;
    [SerializeField] private Transform movingRoot;
    [SerializeField] private Transform extraClickTarget;
    [SerializeField] private Transform housing;
    [SerializeField] private Transform slidingItem;
    [SerializeField] private Vector3 outwardLocal=Vector3.down;
    [SerializeField] private Vector3 lockPointLocal;
    [SerializeField] private float openDistance=22f;
    [SerializeField] private float interactionDistance=100f;
    private Vector3 closedPosition;
    private Vector3 buttonClosedPosition,itemClosedPosition;
    private bool moveItemSeparately;
    private bool moveButtonSeparately;
    private bool busy,opened;
    private Transform keyVisual;
    public bool IsBusy => busy;
    public bool IsOpened => opened;
    public Vector3 LockPointWorld => transform.TransformPoint(lockPointLocal);
    public Transform KeySource => keySource;
    public Transform MotionRoot => movingRoot?movingRoot:transform;
    public Transform ExtraClickTarget => extraClickTarget;
    public Transform Housing => housing;
    public Transform SlidingItem => slidingItem;
    public void ConfigureSlidingItem(Transform item){slidingItem=item;}
    public void ConfigureHousing(Transform piano){housing=piano;}
    public void ConfigureTargets(Transform assembly,Transform button){movingRoot=assembly;extraClickTarget=button;}
    public Vector3 OutwardWorld => transform.TransformDirection(outwardLocal).normalized;
    public float OpenDistance => openDistance;
    public void Configure(Transform key,Vector3 outward,Vector3 lockPoint,float distance)
    {
        keySource=key;outwardLocal=transform.InverseTransformDirection(outward.normalized);lockPointLocal=transform.InverseTransformPoint(lockPoint);openDistance=distance;
    }
    private void Awake(){closedPosition=MotionRoot.position;ResolveButton();}
    public static Transform FindPianoHousing(UnityEngine.SceneManagement.Scene scene)
    {
        if(!scene.IsValid())return null;
        foreach(var root in scene.GetRootGameObjects())
        foreach(var candidate in root.GetComponentsInChildren<Transform>(true))
        {
            string name=candidate.name.Trim();
            if(string.Equals(name,"piano",System.StringComparison.OrdinalIgnoreCase)
                ||string.Equals(name,"pinao",System.StringComparison.OrdinalIgnoreCase)
                ||string.Equals(name,"tripo_node_31432bef",System.StringComparison.OrdinalIgnoreCase))return candidate;
        }
        return null;
    }
    private void ResolveButton()
    {
        if(!extraClickTarget){foreach(var candidate in FindObjectsOfType<Transform>())if(string.Equals(candidate.name,"bottona",System.StringComparison.OrdinalIgnoreCase)){extraClickTarget=candidate;break;}}
        if(!slidingItem){var item=GameObject.Find("nail");if(item)slidingItem=item.transform;}
        if(!housing)housing=FindPianoHousing(gameObject.scene);
        if(!extraClickTarget)return;
        foreach(var mesh in extraClickTarget.GetComponentsInChildren<MeshFilter>(true))if(mesh.sharedMesh && !mesh.GetComponent<Collider>())mesh.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh.sharedMesh;
    }
    private void LateUpdate()
    {
        if(!Application.isFocused || EvidenceInspect.BlocksWorldInput || !Input.GetMouseButtonDown(0))return;
        var camera=Camera.main;if(!camera)return;
        TryInteract(camera,Cursor.lockState==CursorLockMode.Locked?camera.pixelRect.center:(Vector2)Input.mousePosition);
    }
    public bool TryInteract(Camera camera,Vector2 point)
    {
        if(!camera || !camera.pixelRect.Contains(point) || EvidenceInspect.BlocksWorldInput || busy || opened)return false;
        
        if(!extraClickTarget||!housing)ResolveButton();
        var ray=camera.ScreenPointToRay(point);ray.origin=camera.transform.position;Physics.SyncTransforms();
        var hits=Physics.RaycastAll(ray,camera.farClipPlane,~0,QueryTriggerInteraction.Ignore);System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
        foreach(var hit in hits){
            if(hit.transform.GetComponentInParent<FirstPersonPlayer>())continue;
            bool drawerHit=hit.transform==transform || hit.transform.IsChildOf(transform) || hit.transform==MotionRoot || hit.transform.IsChildOf(MotionRoot);
            bool buttonHit=extraClickTarget && (hit.transform==extraClickTarget || hit.transform.IsChildOf(extraClickTarget));
            if(drawerHit || buttonHit)return Vector3.Distance(camera.transform.position,hit.point)<=interactionDistance && TryUnlock();
            // The piano body overlaps its inset button/tray. It belongs to this device,
            // so allow the ray to continue to those targets without removing its collider.
            if(housing && (hit.transform==housing || hit.transform.IsChildOf(housing)))continue;
            return false;
        }return false;
    }
    public bool TryUnlock()
    {
        var inventory=FindObjectOfType<ImportantItemCollectionUI>();
        if(busy || opened || !inventory || !inventory.HasScene2Key || !keySource)return false;
        var mesh=keySource.GetComponent<MeshFilter>();var source=keySource.GetComponent<MeshRenderer>();if(!mesh || !mesh.sharedMesh || !source)return false;
        moveButtonSeparately=extraClickTarget && extraClickTarget!=MotionRoot && !extraClickTarget.IsChildOf(MotionRoot) && !MotionRoot.IsChildOf(extraClickTarget);
        if(moveButtonSeparately)buttonClosedPosition=extraClickTarget.position;
        moveItemSeparately=slidingItem && slidingItem!=MotionRoot && !slidingItem.IsChildOf(MotionRoot) && !(moveButtonSeparately && (slidingItem==extraClickTarget || slidingItem.IsChildOf(extraClickTarget)));
        if(moveItemSeparately)itemClosedPosition=slidingItem.position;
        busy=true;StartCoroutine(Unlock(mesh,source));return true;
    }
    private IEnumerator Unlock(MeshFilter mesh,MeshRenderer source)
    {
        Vector3 outward=OutwardWorld;Vector3 lockPoint=transform.TransformPoint(lockPointLocal);
        keyVisual=new GameObject("Drawer Unlock Key").transform;keyVisual.position=lockPoint;
        var model=new GameObject("KEY Animation Model").transform;model.SetParent(keyVisual,false);
        model.rotation=Quaternion.FromToRotation(Vector3.left,-outward)*keySource.rotation;
        // Fit the full-size collectible into a hand-sized unlocking key.
        float scale=.35f;model.localScale=keySource.lossyScale*scale;
        model.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh.sharedMesh;
        var renderer=model.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterials=source.sharedMaterials;
        var b=renderer.bounds;float extent=Vector3.Dot(new Vector3(Mathf.Abs(outward.x),Mathf.Abs(outward.y),Mathf.Abs(outward.z)),b.extents);
        Vector3 tip=b.center-outward*extent;model.position+=lockPoint-tip;
        Vector3 outside=lockPoint+outward*Mathf.Max(3,extent*.8f)+Vector3.up*1.2f;
        keyVisual.position=outside;
        float elapsed=0;
        while(elapsed<.7f){elapsed+=Time.deltaTime;keyVisual.position=Vector3.Lerp(outside,lockPoint,Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.7f)));yield return null;}
        keyVisual.position=lockPoint;
        elapsed=0;while(elapsed<.5f){elapsed+=Time.deltaTime;keyVisual.rotation=Quaternion.AngleAxis(90*Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.5f)),outward);yield return null;}
        elapsed=0;while(elapsed<.15f){elapsed+=Time.deltaTime;yield return null;}
        elapsed=0;while(elapsed<.3f){elapsed+=Time.deltaTime;keyVisual.rotation=Quaternion.AngleAxis(90*(1-Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.3f))),outward);yield return null;}
        elapsed=0;while(elapsed<.35f){elapsed+=Time.deltaTime;keyVisual.position=Vector3.Lerp(lockPoint,outside,Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.35f)));yield return null;}
        Destroy(keyVisual.gameObject);keyVisual=null;
        Vector3 end=closedPosition+outward*openDistance;elapsed=0;
        while(elapsed<1.1f){elapsed+=Time.deltaTime;SetSlidePosition(Vector3.Lerp(closedPosition,end,Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/1.1f))));yield return null;}
        SetSlidePosition(end);busy=false;opened=true;
        var guidance=GetComponent<PianoKeyGuidance>();if(guidance)guidance.FinishGuidance();
    }
    private void SetSlidePosition(Vector3 position)
    {
        MotionRoot.position=position;
        if(moveButtonSeparately && extraClickTarget)extraClickTarget.position=buttonClosedPosition+(position-closedPosition);
        if(moveItemSeparately && slidingItem)slidingItem.position=itemClosedPosition+(position-closedPosition);
    }
    private void OnDisable()
    {
        StopAllCoroutines();if(keyVisual)Destroy(keyVisual.gameObject);keyVisual=null;
        if(busy){SetSlidePosition(closedPosition);busy=false;}
    }
    private void OnDestroy(){if(keyVisual)Destroy(keyVisual.gameObject);}
}
