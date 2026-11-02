using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-120)]
public sealed class EarSafeInstallation : MonoBehaviour
{
    private Transform ear;
    private bool pointerHeld,dragging;
    private Vector2 pressPoint;
    private Vector3 dragStart,dragOffset;
    private Plane dragPlane;
    private Camera dragCamera;
    private CursorLockMode previousLock;
    private bool previousVisible;
    private static EarSafeInstallation pointerOwner;
    private Transform wall;
    private MeshCollider wallProbe;
    private sealed class WallQuery{public Transform wall;public MeshCollider probe;public Mesh mesh;public Vector3[] vertices;public int[] triangles;}
    private readonly System.Collections.Generic.List<WallQuery> wallQueries=new System.Collections.Generic.List<WallQuery>();
    public static bool BlocksPlayerInput=>pointerOwner;
    public bool Selected { get; private set; }
    public bool Installing { get; private set; }
    public bool Installed { get; private set; }
    public int InstallationPhase { get; private set; }
    private Collider[] installationObstacles=Array.Empty<Collider>();
    private BoxCollider earVolume;
    private bool routeBlocked;
    private bool IntersectsObstacle()
    {
        if(!earVolume)return false;
        foreach(var obstacle in installationObstacles)
        {
            if(!obstacle||!obstacle.enabled||!obstacle.gameObject.activeInHierarchy)continue;
            var precise=wallQueries.FirstOrDefault(q=>q.wall==obstacle.transform);
            if(precise!=null){if(SurfaceIntersectsEar(precise))return true;continue;}
            Vector3 direction;float distance;
            var collider=precise!=null?(Collider)precise.probe:obstacle;
            var rotation=precise!=null?Quaternion.identity:obstacle.transform.rotation;
            if(Physics.ComputePenetration(earVolume,ear.position,ear.rotation,collider,obstacle.transform.position,rotation,out direction,out distance)&&distance>.015f)return true;
        }
        return false;
    }
    private bool SurfaceIntersectsEar(WallQuery wallQuery)
    {
        var matrix=ear.worldToLocalMatrix;var center=earVolume.center;var extent=earVolume.size*.5f;
        var vertices=wallQuery.vertices;var triangles=wallQuery.triangles;
        for(int i=0;i<triangles.Length;i+=3)
        {
            var a=matrix.MultiplyPoint3x4(vertices[triangles[i]]+wallQuery.wall.position)-center;
            var b=matrix.MultiplyPoint3x4(vertices[triangles[i+1]]+wallQuery.wall.position)-center;
            var c=matrix.MultiplyPoint3x4(vertices[triangles[i+2]]+wallQuery.wall.position)-center;
            if(Separated(Vector3.right,a,b,c,extent)||Separated(Vector3.up,a,b,c,extent)||Separated(Vector3.forward,a,b,c,extent))continue;
            var ab=b-a;var bc=c-b;var ca=a-c;
            if(Separated(Vector3.Cross(ab,bc),a,b,c,extent))continue;
            if(EdgeSeparates(ab,a,b,c,extent)||EdgeSeparates(bc,a,b,c,extent)||EdgeSeparates(ca,a,b,c,extent))continue;
            return true;
        }
        return false;
    }
    private static bool EdgeSeparates(Vector3 edge,Vector3 a,Vector3 b,Vector3 c,Vector3 e)
    {return Separated(Vector3.Cross(edge,Vector3.right),a,b,c,e)||Separated(Vector3.Cross(edge,Vector3.up),a,b,c,e)||Separated(Vector3.Cross(edge,Vector3.forward),a,b,c,e);}
    private static bool Separated(Vector3 axis,Vector3 a,Vector3 b,Vector3 c,Vector3 e)
    {
        float p=Vector3.Dot(axis,a),q=Vector3.Dot(axis,b),r=Vector3.Dot(axis,c);
        float radius=Mathf.Abs(axis.x)*e.x+Mathf.Abs(axis.y)*e.y+Mathf.Abs(axis.z)*e.z;
        return Mathf.Min(p,q,r)>radius||Mathf.Max(p,q,r)<-radius;
    }
    private bool ClearSegment(Vector3 destination,Quaternion rotation,Vector3 scale)
    {
        var start=ear.position;var initialRotation=ear.localRotation;var initialScale=ear.localScale;
        int steps=Mathf.Max(12,Mathf.CeilToInt(Vector3.Distance(start,destination)/.4f),Mathf.CeilToInt(Quaternion.Angle(initialRotation,rotation)/2));
        for(int i=1;i<=steps;i++)
        {
            float t=i/(float)steps;ear.position=Vector3.Lerp(start,destination,t);
            ear.localRotation=Quaternion.Slerp(initialRotation,rotation,t);ear.localScale=Vector3.Lerp(initialScale,scale,t);
            if(IntersectsObstacle()){ear.position=start;ear.localRotation=initialRotation;ear.localScale=initialScale;return false;}
        }
        return true;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize(){SceneManager.sceneLoaded-=Bind;SceneManager.sceneLoaded+=Bind;Bind(SceneManager.GetActiveScene(),LoadSceneMode.Single);}
    static void Bind(Scene scene,LoadSceneMode mode)
    {
        if(!scene.IsValid()||!string.Equals(scene.name.Replace(" ",""),"scene2",StringComparison.OrdinalIgnoreCase))return;
        foreach(var t in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)))
            if(t.name=="baoxian"&&!t.GetComponent<EarSafeInstallation>())t.gameObject.AddComponent<EarSafeInstallation>();
    }
    void Start()
    {
        ear=gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t=>t.name=="ear");
        EnsureCollider(transform);if(ear)EnsureCollider(ear);
        RefreshWallQueries();
        wall=gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t=>t.name.Replace(" ","").Equals("cube62",StringComparison.OrdinalIgnoreCase));
        var wallMesh=wall?wall.GetComponent<MeshFilter>():null;
        if(wallMesh&&wallMesh.sharedMesh)
        {
            var probe=new GameObject("Safe pointer wall geometry query"){hideFlags=HideFlags.HideAndDontSave};
            probe.layer=2;probe.transform.position=Vector3.one*50000;
            probe.transform.localScale=Vector3.one/Mathf.Max(.0001f,wallMesh.sharedMesh.bounds.extents.magnitude);
            wallProbe=probe.AddComponent<MeshCollider>();wallProbe.sharedMesh=wallMesh.sharedMesh;Physics.SyncTransforms();
        }
        if(ear){var body=ear.GetComponent<Rigidbody>();if(body){body.isKinematic=true;body.useGravity=false;}}
    }
    private void RefreshWallQueries()
    {
        var template=Resources.Load<Mesh>("SafeInstallation/Cube62Mesh");if(!template)return;
        foreach(var t in gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)))
        {
            string name=t.name.Replace(" ","");
            if(!(name.Equals("cube62",StringComparison.OrdinalIgnoreCase)||name.StartsWith("cube62(",StringComparison.OrdinalIgnoreCase))||wallQueries.Any(q=>q.wall==t))continue;
            var source=t.GetComponent<MeshFilter>();if(!source||!source.sharedMesh)continue;
            if(source.sharedMesh.name=="Wall with arched hearing exit")continue;
            var mesh=Instantiate(template);var vertices=mesh.vertices;
            for(int i=0;i<vertices.Length;i++)vertices[i]=t.TransformVector(vertices[i]);
            mesh.vertices=vertices;
            // Baking a reflection reverses triangle winding.
            if(t.localToWorldMatrix.determinant<0)for(int sub=0;sub<mesh.subMeshCount;sub++){var indices=mesh.GetTriangles(sub);for(int i=0;i<indices.Length;i+=3){int swap=indices[i];indices[i]=indices[i+1];indices[i+1]=swap;}mesh.SetTriangles(indices,sub);}
            mesh.RecalculateBounds();
            var owner=new GameObject("Safe accurate wall query"){hideFlags=HideFlags.HideAndDontSave};owner.layer=2;owner.transform.position=Vector3.one*(60000+wallQueries.Count*5000);
            var query=owner.AddComponent<MeshCollider>();query.sharedMesh=mesh;
            wallQueries.Add(new WallQuery{wall=t,probe=query,mesh=mesh,vertices=vertices,triangles=mesh.triangles});
        }
        Physics.SyncTransforms();
    }
    private RaycastHit[] PointerHits(Camera camera,Vector2 point)
    {
        Physics.SyncTransforms();var ray=camera.ScreenPointToRay(point);
        var hits=new System.Collections.Generic.List<RaycastHit>();
        foreach(var raw in Physics.RaycastAll(ray,camera.farClipPlane,~((1<<30)|(1<<31)),QueryTriggerInteraction.Ignore))
        {
            var hit=raw;
            var preciseWall=wallQueries.FirstOrDefault(q=>q.wall==hit.transform);
            if(preciseWall!=null&&hit.collider is BoxCollider)
            {
                RaycastHit precise;var origin=ray.origin-preciseWall.wall.position+preciseWall.probe.transform.position;
                if(!preciseWall.probe.Raycast(new Ray(origin,ray.direction),out precise,camera.farClipPlane))continue;
                hit.point=precise.point-preciseWall.probe.transform.position+preciseWall.wall.position;hit.distance=precise.distance;
            }
            else if(wallProbe&&hit.transform==wall&&hit.collider is BoxCollider)
            {
                // The wall box fills openings in the imported wall. Query its actual
                // mesh for pointer occlusion, retaining the physical box for falling props.
                var origin=wallProbe.transform.TransformPoint(wall.InverseTransformPoint(ray.origin));
                var direction=wallProbe.transform.TransformVector(wall.InverseTransformVector(ray.direction)).normalized;
                RaycastHit precise;
                if(!wallProbe.Raycast(new Ray(origin,direction),out precise,float.MaxValue))continue;
                hit.point=wall.TransformPoint(wallProbe.transform.InverseTransformPoint(precise.point));
                hit.distance=Vector3.Distance(ray.origin,hit.point);
            }
            hits.Add(hit);
        }
        return hits.OrderBy(h=>h.distance).ToArray();
    }
    void OnDestroy(){if(wallProbe)Destroy(wallProbe.gameObject);foreach(var q in wallQueries){if(q.probe)Destroy(q.probe.gameObject);if(q.mesh)Destroy(q.mesh);}}
    static void EnsureCollider(Transform t)
    {
        if(t.GetComponentsInChildren<Collider>().Any(c=>c.enabled&&!c.isTrigger))return;
        var mesh=t.GetComponent<MeshFilter>();if(!mesh||!mesh.sharedMesh)return;
        var box=t.gameObject.AddComponent<BoxCollider>();box.center=mesh.sharedMesh.bounds.center;box.size=mesh.sharedMesh.bounds.size;
    }
    void Update()
    {
        if(pointerHeld)
        {
            if(!Application.isFocused||Input.GetKeyDown(KeyCode.Escape)){CancelDrag();return;}
            if(Input.GetMouseButtonUp(0)){ReleasePointer((Vector2)Input.mousePosition);return;}
            DragTo((Vector2)Input.mousePosition);return;
        }
        if(!Application.isFocused||EvidenceInspect.BlocksWorldInput||!Input.GetMouseButtonDown(0))return;
        var camera=Camera.main;if(!camera)return;
        Vector2 point=Cursor.lockState==CursorLockMode.Locked?camera.pixelRect.center:(Vector2)Input.mousePosition;
        if(TryClick(camera,point)&&Selected&&!Installing&&!Installed)BeginPointer(camera,point);
    }
    public void BeginPointer(Camera camera,Vector2 point)
    {
        if(!ear||!Selected||Installing||Installed)return;
        dragCamera=camera;pressPoint=point;dragStart=ear.localPosition;
        dragPlane=new Plane(camera.transform.forward,ear.position);
        float distance;var ray=camera.ScreenPointToRay(point);
        dragOffset=dragPlane.Raycast(ray,out distance)?ear.position-ray.GetPoint(distance):Vector3.zero;
        pointerHeld=true;dragging=false;pointerOwner=this;
        previousLock=Cursor.lockState;previousVisible=Cursor.visible;
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
    }
    public void DragTo(Vector2 point)
    {
        if(!pointerHeld||!dragCamera)return;
        if(!dragging&&(point-pressPoint).sqrMagnitude<36)return;
        dragging=true;float distance;var ray=dragCamera.ScreenPointToRay(point);
        if(dragPlane.Raycast(ray,out distance))ear.position=ray.GetPoint(distance)+dragOffset;
    }
    public void ReleasePointer(Vector2 point)
    {
        if(!pointerHeld)return;
        bool accepted=false;
        if(dragging)
        foreach(var hit in PointerHits(dragCamera,point))
        {
            if(hit.transform==ear||hit.transform.IsChildOf(ear)||hit.collider.GetComponentInParent<FirstPersonPlayer>())continue;
            if(hit.transform==transform||hit.transform.IsChildOf(transform))
            {
                // Require the drop near the ear socket, rather than anywhere on the door.
                Vector3 socket=ear.parent?ear.parent.TransformPoint(new Vector3(350.186768f,151.089249f,-228.723511f)):new Vector3(350.186768f,151.089249f,-228.723511f);
                var offset=hit.point-socket;
                accepted=(offset.y*offset.y/(15*15)+offset.z*offset.z/(9*9))<=1f
                    &&Vector3.Distance(dragCamera.transform.position,hit.point)<=160;
            }
            break;
        }
        bool wasDragging=dragging;EndPointer();
        if(accepted)StartCoroutine(Install());else if(wasDragging)ear.localPosition=dragStart;
    }
    void EndPointer(){pointerHeld=false;dragging=false;if(pointerOwner==this)pointerOwner=null;Cursor.lockState=previousLock;Cursor.visible=previousVisible;}
    void CancelDrag(){if(ear)ear.localPosition=dragStart;EndPointer();}
    void OnDisable(){if(pointerHeld)CancelDrag();}
    public bool TryClick(Camera camera,Vector2 point)
    {
        if(!ear||Installing||Installed||!camera||EvidenceInspect.BlocksWorldInput)return false;
        foreach(var hit in PointerHits(camera,point))
        {
            if(hit.collider.GetComponentInParent<FirstPersonPlayer>())continue;
            if(Vector3.Distance(camera.transform.position,hit.point)>160)return false;
            if(hit.transform==ear||hit.transform.IsChildOf(ear)){Selected=true;return true;}
            if((hit.transform==transform||hit.transform.IsChildOf(transform))&&Selected){StartCoroutine(Install());return true;}
            return false;
        }
        return false;
    }
    IEnumerator Install()
    {
        Installing=true;routeBlocked=false;
        var position=ear.position;var rotation=ear.localRotation;var originalScale=ear.localScale; var scale=originalScale;scale.x=-Mathf.Abs(scale.x); var fittedScale=new Vector3(-2400,280,2650);
        var destination=new Vector3(350.186768f,151.089249f,-228.723511f);
        var destinationWorld=ear.parent?ear.parent.TransformPoint(destination):destination;
        var targetRotation=new Quaternion(.5f,-.5f,-.5f,-.5f);
        var targetWorld=ear.parent?ear.parent.rotation*targetRotation:targetRotation;
        // First turn the thin axis toward the door normal; then roll within
        // that plane. This avoids the wide diagonal sweep of a single slerp.
        var intermediateWorld=Quaternion.FromToRotation(ear.up,targetWorld*Vector3.up)*ear.rotation;
        var intermediate=ear.parent?Quaternion.Inverse(ear.parent.rotation)*intermediateWorld:intermediateWorld;
        var safeSurface=GetComponent<Renderer>();
        float doorX=safeSurface?safeSurface.bounds.min.x:destinationWorld.x-4;
        earVolume=ear.GetComponent<BoxCollider>();
        if(!earVolume){var mesh=ear.GetComponent<MeshFilter>();earVolume=ear.gameObject.AddComponent<BoxCollider>();earVolume.center=mesh.sharedMesh.bounds.center;earVolume.size=mesh.sharedMesh.bounds.size;}
        RefreshWallQueries();
        installationObstacles=gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Collider>(true))
            .Where(c=>!c.isTrigger&&c.transform!=ear&&c.name.Replace(" ","").StartsWith("cube62(",StringComparison.OrdinalIgnoreCase)).ToArray();
        var aligned=destinationWorld;aligned.x=doorX-6;
        var turn=position;bool clear=false;
        for(int i=0;i<30&&!clear;i++)
        {
            ear.position=position;ear.localRotation=rotation;ear.localScale=scale;
            turn=position;turn.x=doorX-7.5f-i*.25f;
            clear=ClearSegment(turn,rotation,scale)&&ClearSegment(turn,intermediate,scale)
                &&ClearSegment(aligned,intermediate,scale)&&ClearSegment(aligned,targetRotation,scale)
                &&ClearSegment(destinationWorld,targetRotation,fittedScale);
        }
        ear.position=position;ear.localRotation=rotation;ear.localScale=scale;
        if(!clear){ear.localScale=originalScale;Installing=false;Debug.LogWarning("Ear installation route obstructed; ear retained in place.",this);yield break;}
        InstallationPhase=1;yield return MoveEar(turn,rotation,scale,.3f);
        if(!routeBlocked)yield return MoveEar(turn,intermediate,scale,.35f);
        InstallationPhase=2;
        if(!routeBlocked)yield return MoveEar(aligned,intermediate,scale,.55f);
        if(!routeBlocked)yield return MoveEar(aligned,targetRotation,scale,.4f);
        InstallationPhase=3;
        if(!routeBlocked)yield return MoveEar(destinationWorld,targetRotation,fittedScale,.5f);
        Installing=false;InstallationPhase=0;
        if(!routeBlocked){ear.localPosition=destination;ear.localRotation=targetRotation;ear.localScale=fittedScale;Installed=true;Selected=false;}
    }
    IEnumerator MoveEar(Vector3 destination,Quaternion rotation,Vector3 scale,float seconds)
    {
        var start=ear.position;var initialRotation=ear.localRotation;var initialScale=ear.localScale;
        for(float time=0;time<seconds;time+=Time.unscaledDeltaTime)
        {
            float t=Mathf.SmoothStep(0,1,time/seconds);
            if(!ClearSegment(Vector3.Lerp(start,destination,t),Quaternion.Slerp(initialRotation,rotation,t),Vector3.Lerp(initialScale,scale,t)))
            {routeBlocked=true;yield break;}
            yield return null;
        }
        if(!ClearSegment(destination,rotation,scale))routeBlocked=true;
    }
}
