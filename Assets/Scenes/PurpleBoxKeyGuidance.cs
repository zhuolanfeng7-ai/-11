using System.Collections.Generic;
using UnityEngine;

public sealed class PurpleBoxKeyGuidance:MonoBehaviour
{
    private PurpleBoxUnlockController box;
    private Material glowMaterial;
    private Light spill;
    private readonly List<GameObject> glows=new List<GameObject>();
    private bool started,finished;
    private float start;
    public bool IsGuiding=>started&&!finished;
    private void Awake(){box=GetComponent<PurpleBoxUnlockController>();}
    private void Update()
    {
        if(finished||!box)return;
        if(box.IsUnlocked){Finish();return;}
        if(!started&&KeyPickupHandler.HasCollected)Begin();
        if(!IsGuiding)return;
        float pulse=.5f-.5f*Mathf.Cos((Time.time-start)*Mathf.PI*2/1.5f);
        if(glowMaterial)glowMaterial.SetFloat("_Strength",Mathf.Lerp(.12f,1.3f,pulse));
        if(spill)spill.intensity=Mathf.Lerp(.2f,3,pulse);
    }
    private void Begin()
    {
        started=true;start=Time.time;
        var shader=Resources.Load<Shader>("OpeningEffects/PickupRim");
        if(shader){glowMaterial=new Material(shader);glowMaterial.SetColor("_Tint",new Color(.7f,.23f,1));glowMaterial.SetFloat("_Strength",.12f);}
        Bounds bounds=new Bounds(transform.position,Vector3.one);bool found=false;
        var sources=GetComponentsInChildren<MeshFilter>(true);
        foreach(var source in sources)
        {
            var original=source.GetComponent<MeshRenderer>();if(!source.sharedMesh||!original||!original.enabled)continue;
            if(!found){bounds=original.bounds;found=true;}else bounds.Encapsulate(original.bounds);
            if(!glowMaterial)continue;
            var go=new GameObject("Purple Box Guidance Glow");go.transform.SetParent(source.transform,false);
            go.AddComponent<MeshFilter>().sharedMesh=source.sharedMesh;
            var renderer=go.AddComponent<MeshRenderer>();var materials=new Material[source.sharedMesh.subMeshCount];for(int i=0;i<materials.Length;i++)materials[i]=glowMaterial;renderer.sharedMaterials=materials;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;glows.Add(go);
        }
        var lightObject=new GameObject("Purple Box Breathing Light");lightObject.transform.SetParent(transform,true);lightObject.transform.position=bounds.center+Vector3.up*bounds.extents.y;
        spill=lightObject.AddComponent<Light>();spill.type=LightType.Point;spill.color=new Color(.7f,.3f,1);spill.range=Mathf.Max(3,bounds.size.magnitude*1.5f);spill.intensity=.2f;spill.shadows=LightShadows.None;
    }
    public void Finish(){finished=true;foreach(var glow in glows)if(glow)glow.SetActive(false);if(spill)spill.enabled=false;var hover=GetComponent<HoverScanHighlight>();if(hover)hover.enabled=false;}
    private void OnDisable(){if(started)Finish();}
    private void OnDestroy(){foreach(var glow in glows)if(glow)Destroy(glow);if(glowMaterial)Destroy(glowMaterial);if(spill)Destroy(spill.gameObject);}
}
