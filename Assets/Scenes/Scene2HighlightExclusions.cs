using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Scene2HighlightExclusions
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded-=Bind;
        SceneManager.sceneLoaded+=Bind;
        Apply(SceneManager.GetActiveScene());
    }
    private static void Bind(Scene scene,LoadSceneMode mode){Apply(scene);}
    public static bool Apply(Scene scene)
    {
        if(!scene.IsValid()||!string.Equals(scene.name.Replace(" ",""),"scene2",StringComparison.OrdinalIgnoreCase))return false;
        bool changed=false;
        foreach(var root in scene.GetRootGameObjects())
        foreach(var target in root.GetComponentsInChildren<Transform>(true))
        {
            bool note=string.Equals(target.name,"zhengju1",StringComparison.OrdinalIgnoreCase);
            bool spareRecord=string.Equals(target.name,"cp2",StringComparison.OrdinalIgnoreCase)
                ||string.Equals(target.name,"cp3",StringComparison.OrdinalIgnoreCase)
                ||string.Equals(target.name,"cp4",StringComparison.OrdinalIgnoreCase);
            if(!note&&!string.Equals(target.name,"hezi",StringComparison.OrdinalIgnoreCase)
                &&!string.Equals(target.name,"Drawer Complete Shell",StringComparison.OrdinalIgnoreCase)&&!spareRecord)continue;
            foreach(var hover in spareRecord?target.GetComponentsInChildren<HoverScanHighlight>(true):target.GetComponents<HoverScanHighlight>())
                if(hover&&hover.enabled){hover.enabled=false;changed=true;}
            if(!note)continue;
            var inspect=target.GetComponent<EvidenceInspect>();
            var renderer=target.GetComponent<Renderer>();
            var texture=inspect&&inspect.DisplayTexture?inspect.DisplayTexture:renderer&&renderer.sharedMaterial?renderer.sharedMaterial.mainTexture:null;
            if(!texture)continue;
            if(!inspect){inspect=target.gameObject.AddComponent<EvidenceInspect>();changed=true;}
            inspect.ConfigureWithoutHighlight(texture,100f);
        }
        return changed;
    }
}
