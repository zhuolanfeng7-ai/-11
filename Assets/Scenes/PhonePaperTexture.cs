using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PhonePaperTexture
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded-=ApplyScene;
        SceneManager.sceneLoaded+=ApplyScene;
        ApplyScene(SceneManager.GetActiveScene(),LoadSceneMode.Single);
    }

    public static void ApplyScene(Scene scene,LoadSceneMode mode)
    {
        if(!scene.IsValid()||!string.Equals(scene.name.Replace(" ",""),"scene2",StringComparison.OrdinalIgnoreCase))return;
        var material=Resources.Load<Material>("PhonePaper/PhonePaperReceipt");
        if(!material||!material.mainTexture)return;
        foreach(var target in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)))
            if(string.Equals(target.name,"phonepaper",StringComparison.OrdinalIgnoreCase)) Apply(target,material);
    }

    public static void GetAxes(MeshFilter filter,out Vector3 horizontal,out Vector3 vertical,out float width,out float height)
    {
        var bounds=filter.sharedMesh.bounds;
        var dimensions=Vector3.Scale(bounds.size,filter.transform.lossyScale);
        var axes=new[]{0,1,2}.OrderBy(i=>Mathf.Abs(dimensions[i])).ToArray();
        horizontal=Vector3.zero;horizontal[axes[1]]=1;
        vertical=Vector3.zero;vertical[axes[2]]=1;
        width=Mathf.Abs(dimensions[axes[1]]);height=Mathf.Abs(dimensions[axes[2]]);
        // Present the readable side upwards for a receipt lying on a table.
        var normal=Vector3.Cross(filter.transform.TransformVector(horizontal),filter.transform.TransformVector(vertical));
        if(Vector3.Dot(normal,Vector3.up)>0)horizontal=-horizontal;
    }

    public static void Apply(Transform target,Material material)
    {
        var filter=target.GetComponent<MeshFilter>();var renderer=target.GetComponent<Renderer>();
        if(!filter||!filter.sharedMesh||!renderer)return;
        GetAxes(filter,out var u,out var v,out float width,out float height);
        float paperAspect=width/Mathf.Max(height,.0001f);
        float imageAspect=(float)material.mainTexture.width/material.mainTexture.height;
        material.SetVector("_BoundsMin",filter.sharedMesh.bounds.min);
        material.SetVector("_BoundsSize",filter.sharedMesh.bounds.size);
        material.SetVector("_UAxis",u);material.SetVector("_VAxis",v);
        material.SetVector("_Fit",new Vector4(Mathf.Max(1,paperAspect/imageAspect),Mathf.Max(1,imageAspect/paperAspect),0,0));
        renderer.sharedMaterials=Enumerable.Repeat(material,Mathf.Max(1,renderer.sharedMaterials.Length)).ToArray();
        if(!target.GetComponent<Collider>())
        {
            var collider=target.gameObject.AddComponent<BoxCollider>();
            collider.center=filter.sharedMesh.bounds.center;collider.size=filter.sharedMesh.bounds.size;
        }
        var inspect=target.GetComponent<EvidenceInspect>();
        if(!inspect)inspect=target.gameObject.AddComponent<EvidenceInspect>();
        inspect.ConfigureWithoutHighlight(material.mainTexture,160f,4f);
    }
}
