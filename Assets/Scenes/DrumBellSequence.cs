using System;using System.Linq;using UnityEngine;using UnityEngine.SceneManagement;
[DefaultExecutionOrder(11070)]
[DisallowMultipleComponent]
public sealed class DrumBellSequence : MonoBehaviour
{
    private BellMicroSwing[] bells;private AudioSource source;private AudioClip strike;
    public long HitCount {get;private set;}
    public event Action PatternChanged;
    public bool IsCompletionPlaying {get;private set;}
    public void BeginCompletion(){IsCompletionPlaying=true;foreach(var bell in bells)bell.ExciteSynchronized();}
    public void EndCompletion(){IsCompletionPlaying=false;}
    public int MissingBellNumber => HitCount==0?0:(int)((HitCount+2)%4)+1;
    public void RingBell(int number){if(number>=1&&number<=4&&bells!=null&&bells[number-1])bells[number-1].Excite();}
    public void RingMissingBell(){RingBell(MissingBellNumber);}
    public BellMicroSwing[] Bells => bells;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]private static void Initialize(){SceneManager.sceneLoaded-=Bind;SceneManager.sceneLoaded+=Bind;Bind(SceneManager.GetActiveScene(),LoadSceneMode.Single);}
    private static void Bind(Scene scene,LoadSceneMode mode){if(!scene.IsValid()||!string.Equals(scene.name.Replace(" ",""),"scene2",StringComparison.OrdinalIgnoreCase))return;
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();var drum=all.FirstOrDefault(t=>t.name.Equals("drump",StringComparison.OrdinalIgnoreCase))??all.FirstOrDefault(t=>t.name.Equals("drum",StringComparison.OrdinalIgnoreCase));if(!drum)return;
        var sequence=drum.GetComponent<DrumBellSequence>();if(!sequence)sequence=drum.gameObject.AddComponent<DrumBellSequence>();if(sequence.bells!=null)return;
        var b=new BellMicroSwing[4];for(int i=0;i<4;i++){string number=(i+1).ToString();var t=all.FirstOrDefault(x=>x.name.Equals("bling"+number,StringComparison.OrdinalIgnoreCase)||x.name.Equals("bring"+number,StringComparison.OrdinalIgnoreCase));if(!t){Debug.LogError("Bell "+number+" missing",sequence);return;}b[i]=t.GetComponent<BellMicroSwing>();if(!b[i])b[i]=t.gameObject.AddComponent<BellMicroSwing>();b[i].Configure(i+1);}sequence.bells=b;foreach(var bell in b)bell.ConfigureSideClearance(b);
        var mesh=drum.GetComponent<MeshFilter>();if(!drum.GetComponent<Collider>()&&mesh)drum.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh.sharedMesh;
        sequence.source=drum.gameObject.AddComponent<AudioSource>();sequence.source.playOnAwake=false;sequence.source.spatialBlend=.65f;sequence.source.minDistance=35;sequence.source.maxDistance=400;sequence.strike=Resources.Load<AudioClip>("DrumBells/DrumHit");
        var drumSet=all.FirstOrDefault(t=>t.name.Equals("drum set",StringComparison.OrdinalIgnoreCase));
        if(drumSet){var puzzle=drumSet.GetComponent<DrumSetSupplement>();if(!puzzle)puzzle=drumSet.gameObject.AddComponent<DrumSetSupplement>();var tapes=Enumerable.Range(1,4).Select(i=>all.FirstOrDefault(t=>t.name.Equals("cidai"+i,StringComparison.OrdinalIgnoreCase))).ToArray();puzzle.Configure(sequence,tapes);}
    }
    private void LateUpdate(){if(!Application.isFocused||EvidenceInspect.BlocksWorldInput||!Input.GetMouseButtonDown(0))return;var cam=Camera.main;if(cam)TryClick(cam,Cursor.lockState==CursorLockMode.Locked?cam.pixelRect.center:(Vector2)Input.mousePosition);}
    public bool TryClick(Camera camera,Vector2 pointer){if(!camera||!camera.pixelRect.Contains(pointer)||EvidenceInspect.BlocksWorldInput)return false;var hits=Physics.RaycastAll(camera.ScreenPointToRay(pointer),1000f,~((1<<30)|(1<<31)),QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance);foreach(var hit in hits){if(hit.collider.GetComponentInParent<FirstPersonPlayer>())continue;if(hit.transform!=transform&&!hit.transform.IsChildOf(transform))return false;if(Vector3.Distance(camera.transform.position,hit.point)>180)return false;return Hit();}return false;}
    public bool Hit(){var inventory=FindObjectOfType<ImportantItemCollectionUI>();if(IsCompletionPlaying||!inventory||!inventory.HasWood||bells==null||bells.Any(b=>!b))return false;int step=(int)(HitCount%4);HitCount++;bells[(step+3)%4].StopSwing();for(int offset=0;offset<3;offset++)bells[(step+offset)%4].Excite(offset*.09f);if(strike&&source)Scene2AudioPlayback.PlayOneShot(source,strike,.55f);PatternChanged?.Invoke();return true;}
}
