using UnityEngine;
[DisallowMultipleComponent]
public sealed class Scene2RecoveryGlitch : MonoBehaviour
{
 private Material material;private float began;private bool playing;
 public bool IsPlaying=>playing;
 public const float Duration=1.25f;
 public bool Begin(){
  if(!material){var shader=Resources.Load<Shader>("OpeningEffects/RecoveryGlitch");if(!shader||!shader.isSupported)return false;material=new Material(shader){hideFlags=HideFlags.HideAndDontSave};}
  began=Time.unscaledTime;playing=true;return true;
 }
 private void Update(){if(playing&&Time.unscaledTime-began>=Duration)playing=false;}
 private void OnRenderImage(RenderTexture source,RenderTexture destination){
  if(!playing||!material){Graphics.Blit(source,destination);return;}
  float t=Time.unscaledTime-began;
  // Three soft irregular interruptions, never a rapid white strobe.
  float strength=Pulse(t,.04f,.24f)*.85f+Pulse(t,.39f,.13f)*.65f+Pulse(t,.65f,.32f)*.8f;
  material.SetFloat("_Phase",t);material.SetFloat("_Strength",Mathf.Clamp01(strength));Graphics.Blit(source,destination,material);
 }
 private static float Pulse(float time,float start,float width){float t=(time-start)/width;return t>0&&t<1?Mathf.Sin(t*Mathf.PI):0;}
 private void OnDisable(){playing=false;}
 private void OnDestroy(){if(material)Destroy(material);}
}
