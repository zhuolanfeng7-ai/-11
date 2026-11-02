using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(11080)]
public sealed class Scene2XfGuidance : MonoBehaviour
{
 private Transform target,microphone;private Scene2MicrophoneLighting lighting;
 private readonly List<GameObject> indicatorRoots=new List<GameObject>();
 private readonly List<Light> indicatorLights=new List<Light>();
 private readonly List<Renderer> haloRenderers=new List<Renderer>();
 private GameObject indicatorRoot,halo;private Material coreMaterial,bezelMaterial,haloMaterial;
 private Renderer coreRenderer,haloRenderer;private Light localLight;private float weight,nextSearch;
 private bool repaired;
 public bool IsBound=>target&&localLight;public float GlowStrength=>weight;
 public Transform Indicator=>indicatorRoot?indicatorRoot.transform:null;
 public void SetRepaired(){repaired=true;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 private static void Initialize(){SceneManager.sceneLoaded-=Bind;SceneManager.sceneLoaded+=Bind;Bind(SceneManager.GetActiveScene(),LoadSceneMode.Single);}
 private static void Bind(Scene scene,LoadSceneMode mode){if(!scene.IsValid()||!string.Equals(scene.name.Replace(" ",""),"scene2",StringComparison.OrdinalIgnoreCase))return;if(scene.GetRootGameObjects().Any(g=>g.GetComponent<Scene2XfGuidance>()))return;var owner=new GameObject("Scene2 XF Proximity Guidance");SceneManager.MoveGameObjectToScene(owner,scene);owner.AddComponent<Scene2XfGuidance>();}
 private void FindTarget(){
  var all=gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();target=all.FirstOrDefault(t=>string.Equals(t.name.Trim(),"xf",StringComparison.OrdinalIgnoreCase));microphone=all.FirstOrDefault(t=>string.Equals(t.name.Trim(),"maikefeng",StringComparison.OrdinalIgnoreCase));lighting=FindObjectOfType<Scene2MicrophoneLighting>();if(!target||!microphone)return;
  // Rebuild safely after an in-play script reload, replacing stale generated lamps.
  foreach(var old in target.GetComponentsInChildren<Transform>(true).Where(t=>t.parent==target&&t.name.StartsWith("XF Repair Status LED",StringComparison.Ordinal)).ToArray()){old.gameObject.SetActive(false);old.SetParent(null,true);Destroy(old.gameObject);}

  var renderers=target.GetComponentsInChildren<Renderer>(true);if(renderers.Length==0)return;var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
  // Three fixed, synchronized LEDs remain active throughout play; recorder materials stay untouched.
  var view=Camera.main;var toward=Vector3.ProjectOnPlane((view?view.transform.position:microphone.position)-bounds.center,Vector3.up).normalized;if(toward.sqrMagnitude<.1f)toward=Vector3.back;
  // Choose an actual axis-aligned casing face, rather than a point floating beyond a corner.
  var normal=Mathf.Abs(toward.x)>Mathf.Abs(toward.z)?Vector3.right*Mathf.Sign(toward.x):Vector3.forward*Mathf.Sign(toward.z);
  var tangent=Vector3.Cross(Vector3.up,normal);float halfDepth=Mathf.Abs(normal.x)*bounds.extents.x+Mathf.Abs(normal.z)*bounds.extents.z;float halfWidth=Mathf.Abs(tangent.x)*bounds.extents.x+Mathf.Abs(tangent.z)*bounds.extents.z;
  float diameter=Mathf.Clamp(Mathf.Max(bounds.size.x,bounds.size.z)*.014f,.65f,1.3f);
  var position=bounds.center+normal*(halfDepth+.08f)+Vector3.up*(bounds.extents.y*.47f)+tangent*(halfWidth*.52f);
  indicatorRoot=new GameObject("XF Repair Status LED");indicatorRoot.transform.position=position;indicatorRoot.transform.rotation=Quaternion.LookRotation(normal);indicatorRoot.transform.SetParent(target,true);
  var standard=Shader.Find("Standard");bezelMaterial=new Material(standard){hideFlags=HideFlags.HideAndDontSave};bezelMaterial.color=new Color(.035f,.04f,.07f);bezelMaterial.SetFloat("_Metallic",.65f);bezelMaterial.SetFloat("_Glossiness",.75f);
  coreMaterial=new Material(standard){hideFlags=HideFlags.HideAndDontSave};coreMaterial.color=new Color(.12f,.17f,.28f);coreMaterial.EnableKeyword("_EMISSION");coreMaterial.SetColor("_EmissionColor",Color.black);
  MakeBulb("LED dark bezel",position,diameter*1.5f,diameter*.36f,normal,bezelMaterial);
  var core=MakeBulb("LED blue lavender lens",position+normal*(diameter*.17f),diameter,diameter*.35f,normal,coreMaterial);coreRenderer=core.GetComponent<Renderer>();
  var haloShader=Resources.Load<Shader>("OpeningEffects/XFIndicatorHalo");if(haloShader&&haloShader.isSupported){haloMaterial=new Material(haloShader){hideFlags=HideFlags.HideAndDontSave};haloMaterial.SetColor("_Tint",new Color(.60f,.70f,1));haloMaterial.SetFloat("_Strength",0);halo=GameObject.CreatePrimitive(PrimitiveType.Quad);halo.name="LED soft halo";Destroy(halo.GetComponent<Collider>());halo.transform.position=position+normal*(diameter*.4f);halo.transform.localScale=Vector3.one*(diameter*7);halo.transform.SetParent(indicatorRoot.transform,true);haloRenderer=halo.GetComponent<Renderer>();haloRenderer.sharedMaterial=haloMaterial;haloRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;haloRenderer.enabled=false;}
  var lamp=new GameObject("XF Tiny Indicator Light");lamp.transform.position=position+normal*.5f;lamp.transform.SetParent(indicatorRoot.transform,true);localLight=lamp.AddComponent<Light>();localLight.type=LightType.Point;localLight.color=new Color(.66f,.73f,1);localLight.range=diameter*13;localLight.intensity=0;localLight.shadows=LightShadows.None;localLight.cullingMask=~((1<<30)|(1<<31));
  indicatorRoots.Add(indicatorRoot);indicatorLights.Add(localLight);if(haloRenderer)haloRenderers.Add(haloRenderer);
  float spacing=Mathf.Max(diameter*5,halfWidth*.30f);
  for(int i=1;i<3;i++){var copy=Instantiate(indicatorRoot,target);copy.name="XF Repair Status LED "+(i+1);copy.transform.position=position-tangent*(spacing*i);indicatorRoots.Add(copy);indicatorLights.Add(copy.GetComponentInChildren<Light>());var copyHalo=copy.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r=>r.name=="LED soft halo");if(copyHalo)haloRenderers.Add(copyHalo);}

 }
 private GameObject MakeBulb(string label,Vector3 position,float diameter,float depth,Vector3 normal,Material material){var bulb=GameObject.CreatePrimitive(PrimitiveType.Sphere);bulb.name=label;Destroy(bulb.GetComponent<Collider>());bulb.transform.SetPositionAndRotation(position,Quaternion.LookRotation(normal));bulb.transform.localScale=new Vector3(diameter,diameter,depth);bulb.transform.SetParent(indicatorRoot.transform,true);var renderer=bulb.GetComponent<Renderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return bulb;}
 private void LateUpdate(){
  if(!IsBound){if(Time.unscaledTime>=nextSearch){nextSearch=Time.unscaledTime+1;FindTarget();}return;}
  var camera=Camera.main;float desired=0;if(camera&&target.gameObject.activeInHierarchy){float pulse=repaired?1f:.28f+.72f*(.5f-.5f*Mathf.Cos(Time.unscaledTime*Mathf.PI*2/1.5f));desired=pulse;}
  var glowColor=repaired?new Color(.28f,1f,.52f):new Color(.58f,.67f,1);weight=Mathf.MoveTowards(weight,desired,Time.unscaledDeltaTime*3);coreMaterial.SetColor("_EmissionColor",glowColor*weight*15);foreach(var light in indicatorLights)if(light){light.color=glowColor;light.intensity=4.2f*weight;}
  if(haloMaterial){haloMaterial.SetFloat("_Strength",weight);foreach(var renderer in haloRenderers){if(!renderer)continue;renderer.enabled=weight>.001f;if(camera)renderer.transform.rotation=Quaternion.LookRotation(renderer.transform.position-camera.transform.position,camera.transform.up);}}
 }
 private void OnDisable(){foreach(var light in indicatorLights)if(light)light.intensity=0;if(coreMaterial)coreMaterial.SetColor("_EmissionColor",Color.black);foreach(var renderer in haloRenderers)if(renderer)renderer.enabled=false;}
 private void OnDestroy(){foreach(var root in indicatorRoots)if(root)Destroy(root);if(coreMaterial)Destroy(coreMaterial);if(bezelMaterial)Destroy(bezelMaterial);if(haloMaterial)Destroy(haloMaterial);}
}

