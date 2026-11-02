using System.Collections;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TubeRewardReveal : MonoBehaviour
{
    [SerializeField] private Transform board;
    [SerializeField] private Transform alloy;
    [SerializeField] private Shader revealShader;
    [SerializeField, Min(.1f)] private float riseDuration=1.5f;
    private Renderer[] visuals;
    private Collider[] colliders;
    private bool[] colliderStates;
    private Material[][] originalMaterials;
    private Material[] temporaryMaterials;
    private bool initialized, started, revealing, revealed;
    private Transform[] revealRoots;
    private Vector3[] finalPositions;
    private Quaternion[] finalRotations;
    private Vector3[] finalScales;
    private Bounds finalBounds;
    public bool IsRevealing => revealing;
    public bool IsRevealed => revealed;
    public Transform Board => board;
    public Transform Alloy => alloy;
    public void Configure(Transform panel, Transform item, Shader shader)
    {
        board=panel;alloy=item;revealShader=shader;
        if(Application.isPlaying&&!initialized) Initialize();
    }
    private void Awake(){Initialize();}
    private void Initialize()
    {
        if(initialized||!board||!alloy)return;
        visuals=board.GetComponentsInChildren<Renderer>(true).Concat(alloy.GetComponentsInChildren<Renderer>(true)).Distinct().ToArray();
        colliders=board.GetComponentsInChildren<Collider>(true).Concat(alloy.GetComponentsInChildren<Collider>(true)).Distinct().ToArray();
        colliderStates=colliders.Select(c=>c.enabled).ToArray();
        // The authored scene pose is the exact final layout. Do not center,
        // rotate or lift the assembly to a different destination.
        revealRoots=new[]{board,alloy}.Where(t=>!new[]{board,alloy}.Any(other=>other!=t&&t.IsChildOf(other))).ToArray();
        finalPositions=revealRoots.Select(t=>t.position).ToArray();
        finalRotations=revealRoots.Select(t=>t.rotation).ToArray();
        finalScales=revealRoots.Select(t=>t.localScale).ToArray();
        finalBounds=visuals[0].bounds;
        foreach(var renderer in visuals)finalBounds.Encapsulate(renderer.bounds);
        foreach(var renderer in visuals)renderer.enabled=false;
        foreach(var collider in colliders)collider.enabled=false;
        initialized=true;
    }
    public void BeginReveal(float mouthHeight)
    {
        Initialize();
        if(!initialized||started)return;
        if(!revealShader){Debug.LogError("Tube reveal shader is missing",this);return;}
        started=true;revealing=true;
        StartCoroutine(Rise(mouthHeight));
    }
    private IEnumerator Rise(float mouthHeight)
    {
        float clearance=Mathf.Max(.1f,finalBounds.size.y*.05f);
        float revealPlane=finalBounds.min.y-clearance;
        var parts=revealRoots;
        var ends=finalPositions;
        float travel=finalBounds.size.y+clearance*2;
        originalMaterials=visuals.Select(r=>r.sharedMaterials).ToArray();
        var materials=new System.Collections.Generic.List<Material>();
        for(int i=0;i<visuals.Length;i++){
            var copies=originalMaterials[i].Select(original=>{
                var copy=new Material(original){shader=revealShader};copy.SetFloat("_RevealHeight",revealPlane);materials.Add(copy);return copy;
            }).ToArray();
            visuals[i].sharedMaterials=copies;
        }
        temporaryMaterials=materials.ToArray();
        for(int i=0;i<parts.Length;i++)parts[i].position=ends[i]+Vector3.down*travel;
        foreach(var renderer in visuals)renderer.enabled=true;
        float elapsed=0;
        while(elapsed<riseDuration){
            elapsed+=Time.deltaTime;
            float progress=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/riseDuration));
            for(int i=0;i<parts.Length;i++)parts[i].position=ends[i]+Vector3.down*travel*(1-progress);
            yield return null;
        }
        for(int i=0;i<parts.Length;i++){
            parts[i].SetPositionAndRotation(ends[i],finalRotations[i]);
            parts[i].localScale=finalScales[i];
        }
        RestoreMaterials();
        for(int i=0;i<colliders.Length;i++)colliders[i].enabled=colliderStates[i];
        revealing=false;revealed=true;
    }
    private void RestoreMaterials()
    {
        if(originalMaterials!=null)for(int i=0;i<visuals.Length;i++)if(visuals[i])visuals[i].sharedMaterials=originalMaterials[i];
        if(temporaryMaterials!=null)foreach(var material in temporaryMaterials)if(material)Destroy(material);
        temporaryMaterials=null;originalMaterials=null;
    }
    private void OnDestroy(){RestoreMaterials();}
}
