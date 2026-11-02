using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MusicBoxRewardLift : MonoBehaviour
{
    [SerializeField] private Transform raisedPosition;
    [SerializeField,Min(.1f)] private float riseDuration=1.5f;
    [SerializeField] private bool worldYOnly;
    private bool started,raised;
    private Renderer[] visuals;
    private Collider[] colliders;
    private bool[] colliderStates;
    public bool IsRaised => raised;
    public bool IsRising => started&&!raised;
    public Transform RaisedPosition => raisedPosition;
    public void Configure(Transform target){raisedPosition=target;}
    public void ConfigureVerticalLift(Transform target){raisedPosition=target;worldYOnly=true;}
    private void Awake(){
        visuals=GetComponentsInChildren<Renderer>(true);colliders=GetComponentsInChildren<Collider>(true);
        colliderStates=new bool[colliders.Length];for(int i=0;i<colliders.Length;i++){colliderStates[i]=colliders[i].enabled;colliders[i].enabled=false;}
        foreach(var renderer in visuals)renderer.enabled=false;
    }
    public void Raise(){if(started||!raisedPosition)return;started=true;StartCoroutine(Rise());}
    private IEnumerator Rise(){
        var start=transform.position;var end=raisedPosition.position;
        if(worldYOnly)end=new Vector3(start.x,Mathf.Max(start.y,end.y),start.z);
        foreach(var renderer in visuals)renderer.enabled=true;
        float elapsed=0;while(elapsed<riseDuration){elapsed+=Time.deltaTime;transform.position=Vector3.Lerp(start,end,Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/riseDuration)));yield return null;}
        transform.position=end;for(int i=0;i<colliders.Length;i++)colliders[i].enabled=colliderStates[i];raised=true;
    }
}
