using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// A listener-relative opening vignette. Other puzzle sounds keep their own playback rules.
[DisallowMultipleComponent]
public sealed class Scene2OpeningAudio : MonoBehaviour
{
 public AudioSource Output { get; private set; }
 public bool HasStarted { get; private set; }
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 private static void Initialize(){SceneManager.sceneLoaded-=OnSceneLoaded;SceneManager.sceneLoaded+=OnSceneLoaded;OnSceneLoaded(SceneManager.GetActiveScene(),LoadSceneMode.Single);}
 private static void OnSceneLoaded(Scene scene,LoadSceneMode mode){
  if(!scene.IsValid()||!string.Equals(scene.name.Replace(" ",""),"scene2",StringComparison.OrdinalIgnoreCase))return;
  if(scene.GetRootGameObjects().Any(root=>root.GetComponentInChildren<Scene2OpeningAudio>(true)))return;
  var owner=new GameObject("Scene2 Opening Hearing Failure");SceneManager.MoveGameObjectToScene(owner,scene);owner.AddComponent<Scene2OpeningAudio>();
 }
 private void Awake(){
  Output=gameObject.AddComponent<AudioSource>();Output.playOnAwake=false;Output.loop=false;Output.spatialBlend=0;Output.dopplerLevel=0;Output.volume=.85f;
  Output.clip=Resources.Load<AudioClip>("OpeningAudio/HearingFailure");
 }
 private void Start(){if(HasStarted||!Output.clip)return;HasStarted=true;Scene2AudioPlayback.Play(Output);}
 public void FadeOutForDialogue(){StartCoroutine(FadeOut());}
 private System.Collections.IEnumerator FadeOut(){if(!Output)yield break;float volume=Output.volume;for(float t=0;t<.3f;t+=Time.unscaledDeltaTime){Output.volume=Mathf.Lerp(volume,0,t/.3f);yield return null;}Output.Stop();Output.volume=volume;}
 private void OnDisable(){if(Output)Output.Stop();}
}
