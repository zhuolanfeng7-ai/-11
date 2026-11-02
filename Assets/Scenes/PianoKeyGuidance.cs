using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// A persistent visual clue; interrupting its music never restarts it automatically.
public sealed class PianoKeyGuidance : MonoBehaviour
{
    private KeyDrawerUnlock door;
    private MusicBoxRewardLift reward;
    private AudioSource output;
    private Transform effects;
    private Material rim;
    private Light bodyLight,lockLight;
    private bool triggered,finished;
    private float started;
    public bool IsGuiding=>triggered&&!finished;
    public AudioSource Output=>output;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded-=Bind;SceneManager.sceneLoaded+=Bind;
        Bind(SceneManager.GetActiveScene(),LoadSceneMode.Single);
    }
    private static void Bind(Scene scene,LoadSceneMode mode)
    {
        if(!scene.IsValid()||!scene.name.Replace(" ","").Equals("scene2",StringComparison.OrdinalIgnoreCase))return;
        foreach(var root in scene.GetRootGameObjects())foreach(var unlock in root.GetComponentsInChildren<KeyDrawerUnlock>(true))
            if(!unlock.GetComponent<PianoKeyGuidance>())unlock.gameObject.AddComponent<PianoKeyGuidance>();
    }
    private void Awake()
    {
        door=GetComponent<KeyDrawerUnlock>();
        output=gameObject.AddComponent<AudioSource>();output.playOnAwake=false;output.loop=true;
        output.clip=Resources.Load<AudioClip>("PianoGuidance/ChopinPreludeOp28No4");
        output.volume=.8f;output.spatialBlend=.65f;output.dopplerLevel=0;
        output.rolloffMode=AudioRolloffMode.Linear;output.minDistance=50;output.maxDistance=500;
    }
    private void OnEnable(){Scene2AudioPlayback.SoundStarting+=PauseForOtherSound;}
    private void PauseForOtherSound(AudioSource other){if(other!=output&&output&&output.isPlaying)output.Pause();}
    private void Update()
    {
        if(!door||finished)return;
        if(door.IsOpened){FinishGuidance();return;}
        if(!triggered)
        {
            if(!reward&&door.KeySource)reward=door.KeySource.GetComponent<MusicBoxRewardLift>();
            if(reward&&(reward.IsRising||reward.IsRaised))BeginGuidance();
        }
        if(!IsGuiding||!effects)return;
        float pulse=.5f-.5f*Mathf.Cos((Time.time-started)*Mathf.PI*2/1.5f);
        if(rim)rim.SetFloat("_Strength",Mathf.Lerp(.18f,1.25f,pulse));
        bodyLight.intensity=Mathf.Lerp(.25f,2.5f,pulse);
        lockLight.intensity=Mathf.Lerp(2,5,pulse);
        lockLight.transform.position=door.LockPointWorld+door.OutwardWorld*1.5f;
    }
    public void BeginGuidance()
    {
        if(triggered||finished||!door||door.IsOpened)return;
        var piano=door.Housing?door.Housing:KeyDrawerUnlock.FindPianoHousing(gameObject.scene);
        if(!piano)return;
        triggered=true;started=Time.time;
        effects=new GameObject("Piano Purple Key Guidance").transform;effects.SetParent(transform,false);
        // Capture original sources before adding any glow children.
        var sources=piano.GetComponentsInChildren<MeshFilter>(true);
        var shader=Resources.Load<Shader>("OpeningEffects/PickupRim");
        if(shader)
        {
            rim=new Material(shader);rim.SetColor("_Tint",new Color(.65f,.27f,1));rim.SetFloat("_Strength",.18f);
            foreach(var source in sources)
            {
                var original=source.GetComponent<MeshRenderer>();if(!source.sharedMesh||!original||!original.enabled)continue;
                var glow=new GameObject("Piano Guidance Rim");glow.transform.SetParent(source.transform,false);
                glow.AddComponent<MeshFilter>().sharedMesh=source.sharedMesh;
                var renderer=glow.AddComponent<MeshRenderer>();var mats=new Material[source.sharedMesh.subMeshCount];
                for(int i=0;i<mats.Length;i++)mats[i]=rim;renderer.sharedMaterials=mats;
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
                glowObjects.Add(glow);
            }
        }
        bodyLight=MakeLight("Piano Lavender Spill",150);bodyLight.transform.position=piano.GetComponentInChildren<Renderer>().bounds.center+door.OutwardWorld*12;
        lockLight=MakeLight("Piano Lock Glow",22);lockLight.transform.position=door.LockPointWorld+door.OutwardWorld*1.5f;
        bodyLight.intensity=.25f;lockLight.intensity=2;
        Scene2AudioPlayback.Play(output);
    }
    private readonly List<GameObject> glowObjects=new List<GameObject>();
    private Light MakeLight(string name,float range)
    {
        var go=new GameObject(name);go.transform.SetParent(effects,false);var light=go.AddComponent<Light>();
        light.type=LightType.Point;light.color=new Color(.65f,.38f,1);light.range=range;light.shadows=LightShadows.None;return light;
    }
    public void FinishGuidance()
    {
        finished=true;if(output)output.Stop();if(effects)effects.gameObject.SetActive(false);
        foreach(var glow in glowObjects)if(glow)glow.SetActive(false);
        if(door)
        {
            var targets=new[]{door.transform,door.Housing,door.ExtraClickTarget};
            foreach(var target in targets)if(target){var hover=target.GetComponent<HoverScanHighlight>();if(hover)hover.enabled=false;}
        }
    }
    private void OnDisable(){Scene2AudioPlayback.SoundStarting-=PauseForOtherSound;if(triggered)FinishGuidance();}
    private void OnDestroy(){foreach(var glow in glowObjects)if(glow)Destroy(glow);if(rim)Destroy(rim);}
}
