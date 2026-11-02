using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(11060)]
[DisallowMultipleComponent]
public sealed class CubeDrawerSlide : MonoBehaviour
{
    [SerializeField] private Transform carriedRecord;
    [SerializeField] private float slideDistance=16f;
    [SerializeField] private float slideDuration=.8f;
    private const float InteractionDistance = 180f;
    private bool opened,busy;
    private Vector3 closedPosition;
    private void Awake(){closedPosition=transform.position;}
    public bool IsOpened => opened;
    public Vector3 OutwardWorld => Vector3.ProjectOnPlane(transform.TransformVector(Vector3.right),Vector3.up).normalized;
    public Transform CarriedRecord => carriedRecord;
    public void Configure(Transform record){carriedRecord=record;slideDistance=16f;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize(){SceneManager.sceneLoaded-=SceneLoaded;SceneManager.sceneLoaded+=SceneLoaded;Bind(SceneManager.GetActiveScene());}
    private static void SceneLoaded(Scene scene,LoadSceneMode mode){Bind(scene);}
    public static void Bind(Scene scene)
    {
        if(!scene.IsValid()||!string.Equals(scene.name.Replace(" ",""),"scene2",StringComparison.OrdinalIgnoreCase))return;
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var record=all.FirstOrDefault(t=>string.Equals(t.name.Trim(),"cp1",StringComparison.OrdinalIgnoreCase));
        var paper=all.FirstOrDefault(t=>string.Equals(t.name.Trim(),"paper11",StringComparison.OrdinalIgnoreCase));
        for(int i=1;i<=4;i++){
            string name="cube"+i;
            var t=all.FirstOrDefault(x=>string.Equals(x.name.Trim(),name,StringComparison.OrdinalIgnoreCase));
            if(!t)t=all.FirstOrDefault(x=>{var f=x.GetComponent<MeshFilter>();return f&&f.sharedMesh&&f.sharedMesh.name==name+"_FacesRepaired";});
            if(!t)continue;
            var slide=t.GetComponent<CubeDrawerSlide>();if(!slide)slide=t.gameObject.AddComponent<CubeDrawerSlide>();slide.Configure(i==1?record:i==3?paper:null);
            var filter=t.GetComponent<MeshFilter>();var collider=t.GetComponent<MeshCollider>();
            if(!collider)collider=t.gameObject.AddComponent<MeshCollider>();if(filter)collider.sharedMesh=filter.sharedMesh;
        }
    }
    private void LateUpdate()
    {
        if(busy||EvidenceInspect.BlocksWorldInput||!Application.isFocused||!Input.GetMouseButtonDown(0))return;
        var camera=Camera.main;if(!camera)return;
        var pointer=Cursor.lockState==CursorLockMode.Locked?camera.pixelRect.center:(Vector2)Input.mousePosition;
        TryClick(camera,pointer);
    }
    public bool TryClick(Camera camera,Vector2 pointer)
    {
        if(!camera||busy)return false;
        var hits=Physics.RaycastAll(camera.ScreenPointToRay(pointer),1000f,~((1<<30)|(1<<31)),QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance);
        foreach(var hit in hits){
            if(hit.collider.GetComponentInParent<FirstPersonPlayer>())continue;
            if(hit.transform!=transform&&!hit.transform.IsChildOf(transform))return false;
            if(Vector3.Distance(camera.transform.position,hit.point)>InteractionDistance)return false;
            return Toggle();
        }
        return false;
    }
    public bool Open(){if(opened)return false;return Toggle();}
    public bool Toggle(){if(busy||!isActiveAndEnabled)return false;busy=true;StartCoroutine(Slide(!opened));return true;}
    private IEnumerator Slide(bool opening)
    {
        Vector3 start=transform.position;
        Vector3 target=opening?closedPosition+OutwardWorld*slideDistance:closedPosition;
        Vector3 delta=target-start;
        var record=carriedRecord;bool carry=record&&record.gameObject.activeInHierarchy&&!record.IsChildOf(transform);
        var pickup=record?record.GetComponent<AlloyPickup>():null;
        Vector3 recordStart=record?record.position:Vector3.zero;
        for(float elapsed=0;elapsed<slideDuration;elapsed+=Time.deltaTime){float t=Mathf.SmoothStep(0,1,elapsed/slideDuration);transform.position=start+delta*t;if(carry&&record&&record.gameObject.activeInHierarchy&&(!pickup||!pickup.IsCollected))record.position=recordStart+delta*t;yield return null;}
        transform.position=target;if(carry&&record&&record.gameObject.activeInHierarchy&&(!pickup||!pickup.IsCollected))record.position=recordStart+delta;
        opened=opening;busy=false;
    }
}
