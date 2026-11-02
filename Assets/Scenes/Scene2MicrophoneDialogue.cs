using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Scene2MicrophoneDialogue : MonoBehaviour
{
 private TelescopingDevice device;private AudioSource voice;private Light indicatorLight;private Renderer indicator;private Material indicatorMaterial;
 private bool configured,started;private float began;
 public bool IsPlaying{get;private set;}
 public bool IsFinished{get;private set;}
 public AudioSource Voice=>voice;
 public string CurrentSubtitle=>string.Empty;
 public void Configure(TelescopingDevice owner){
  if(configured)return;device=owner;if(!device)return;
  voice=gameObject.AddComponent<AudioSource>();voice.playOnAwake=false;voice.loop=false;voice.spatialBlend=0;voice.minDistance=45;voice.maxDistance=250;voice.dopplerLevel=0;voice.volume=.65f;voice.clip=Resources.Load<AudioClip>("OpeningAudio/MicrophoneAmbientRobot");
  var bounds=GetComponentInChildren<Renderer>().bounds;var view=Camera.main;var toward=view?(view.transform.position-bounds.center).normalized:Vector3.back;
  var dot=GameObject.CreatePrimitive(PrimitiveType.Sphere);dot.name="Microphone Status Indicator";Destroy(dot.GetComponent<Collider>());dot.transform.position=bounds.center+toward*(bounds.extents.magnitude*.5f);dot.transform.localScale=Vector3.one*1.1f;dot.transform.SetParent(transform,true);indicator=dot.GetComponent<Renderer>();
  var shader=Shader.Find("Standard");if(shader){indicatorMaterial=new Material(shader);indicatorMaterial.color=new Color(.08f,.18f,.24f);indicatorMaterial.EnableKeyword("_EMISSION");indicator.sharedMaterial=indicatorMaterial;}
  indicatorLight=dot.AddComponent<Light>();indicatorLight.type=LightType.Point;indicatorLight.color=new Color(.58f,.78f,1);indicatorLight.range=8;indicatorLight.intensity=0;indicatorLight.cullingMask=~((1<<30)|(1<<31));
  device.RetractionGate=null;configured=true;SetIndicator(false);
 }
 private void Update(){if(configured&&!started&&Application.isFocused&&!EvidenceInspect.BlocksWorldInput&&Input.GetKey(KeyCode.W))TryBegin();}
 public bool TryBegin(){if(!configured||started||!voice.clip||!isActiveAndEnabled)return false;started=true;IsPlaying=true;began=Time.unscaledTime;StartCoroutine(Sequence());return true;}
 private IEnumerator Sequence(){
  var opening=FindObjectOfType<Scene2OpeningAudio>();if(opening)opening.FadeOutForDialogue();Scene2AudioPlayback.Play(voice);
  while(Time.unscaledTime-began<voice.clip.length){float t=Time.unscaledTime-began;bool on=t<1||(t>=1.12f&&t<1.28f)||(t>=1.57f&&t<1.74f)||(t>=2&&t<4);SetIndicator(on);yield return null;}
  SetIndicator(false);IsPlaying=false;IsFinished=true;
 }
 private void SetIndicator(bool on){if(indicatorLight)indicatorLight.intensity=on?.65f:0;if(indicatorMaterial)indicatorMaterial.SetColor("_EmissionColor",on?new Color(.58f,.78f,1)*2:Color.black);}
 private void OnDisable(){StopAllCoroutines();if(voice)voice.Stop();SetIndicator(false);IsPlaying=false;}
 private void OnDestroy(){if(device)device.RetractionGate=null;if(indicatorMaterial)Destroy(indicatorMaterial);}
}
