using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(11060)]
public sealed class Scene2MicrophoneLighting : MonoBehaviour
{
 private Scene2MicrophoneDialogue dialogue;private Transform microphone;private TelescopingDevice device;private AlloyPickup reward;
 private readonly List<Light> pathLights=new List<Light>();
 private Light spotlight,fill;private Light[] roomLights;private float[] intensities;
 private AmbientMode ambientMode;private Color ambientLight,sky,equator,ground;private float ambientIntensity,reflectionIntensity;
 private readonly Dictionary<Material,Material> copies=new Dictionary<Material,Material>();
 private readonly Dictionary<Renderer,Material[]> originals=new Dictionary<Renderer,Material[]>();
 private readonly Dictionary<Material,Color> emissions=new Dictionary<Material,Color>();
 private bool initialized,restoring,restored;private float progress;
 public float RecoveryProgress=>restored?1:progress;public bool IsRestored=>restored;public Light Spotlight=>spotlight;
 public Transform Microphone=>microphone;
 public Vector3[] XFGuideRoute{get;private set;}public float XFGuideRightShift{get;private set;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 private static void Initialize(){SceneManager.sceneLoaded-=Bind;SceneManager.sceneLoaded+=Bind;Bind(SceneManager.GetActiveScene(),LoadSceneMode.Single);}
 private static void Bind(Scene scene,LoadSceneMode mode){if(!scene.IsValid()||!string.Equals(scene.name.Replace(" ",""),"scene2",StringComparison.OrdinalIgnoreCase))return;if(scene.GetRootGameObjects().Any(g=>g.GetComponentInChildren<Scene2MicrophoneLighting>(true)))return;var owner=new GameObject("Scene2 Microphone Lighting");SceneManager.MoveGameObjectToScene(owner,scene);owner.AddComponent<Scene2MicrophoneLighting>();}
 private void Start(){
  var all=gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
  microphone=all.FirstOrDefault(t=>string.Equals(t.name,"maikefeng",StringComparison.OrdinalIgnoreCase));
  device=all.Select(t=>t.GetComponent<TelescopingDevice>()).FirstOrDefault(d=>d&&d.FollowingParts.Contains(microphone));
  if(!device)device=all.Select(t=>t.GetComponent<TelescopingDevice>()).FirstOrDefault(d=>d);
  var reveal=device?device.GetComponent<TubeRewardReveal>():null;reward=reveal&&reveal.Alloy?reveal.Alloy.GetComponent<AlloyPickup>():null;
  if(!microphone||!device||!reward){Debug.LogWarning("Opening lighting needs maikefeng, its retracting assembly and alloy1 pickup.",this);return;}
  var filter=microphone.GetComponent<MeshFilter>();if(filter&&!microphone.GetComponent<Collider>()){var box=microphone.gameObject.AddComponent<BoxCollider>();box.center=filter.sharedMesh.bounds.center;box.size=filter.sharedMesh.bounds.size;}
  var hover=microphone.GetComponent<HoverScanHighlight>();if(!hover)hover=microphone.gameObject.AddComponent<HoverScanHighlight>();hover.ConfigureScene2();hover.Configure(6,100);
  dialogue=microphone.GetComponent<Scene2MicrophoneDialogue>();if(!dialogue)dialogue=microphone.gameObject.AddComponent<Scene2MicrophoneDialogue>();dialogue.Configure(device);
  roomLights=FindObjectsOfType<Light>().Where(l=>l.gameObject.scene==gameObject.scene).ToArray();intensities=roomLights.Select(l=>l.intensity).ToArray();
  ambientMode=RenderSettings.ambientMode;ambientLight=RenderSettings.ambientLight;sky=RenderSettings.ambientSkyColor;equator=RenderSettings.ambientEquatorColor;ground=RenderSettings.ambientGroundColor;ambientIntensity=RenderSettings.ambientIntensity;reflectionIntensity=RenderSettings.reflectionIntensity;
  // Runtime material copies leave the authored materials and neon colours intact.
  foreach(var renderer in all.Select(t=>t.GetComponent<Renderer>()).Where(r=>r)){
   var materials=renderer.sharedMaterials;if(!materials.Any(m=>m&&m.HasProperty("_EmissionColor")&&m.GetColor("_EmissionColor").maxColorComponent>.001f))continue;
   originals[renderer]=materials;renderer.sharedMaterials=materials.Select(m=>{if(!m||!m.HasProperty("_EmissionColor"))return m;Material copy;if(!copies.TryGetValue(m,out copy)){copy=new Material(m);copies[m]=copy;emissions[copy]=m.GetColor("_EmissionColor");}return copy;}).ToArray();
  }
  spotlight=MakeLight("Microphone Pale Lavender Spotlight",LightType.Spot,new Color(.83f,.85f,1f),14,250);spotlight.spotAngle=75;spotlight.innerSpotAngle=42;spotlight.shadows=LightShadows.Soft;spotlight.shadowBias=.03f;spotlight.shadowNormalBias=.1f;
  fill=MakeLight("Opening Path Soft Fill",LightType.Point,new Color(.34f,.30f,.55f),.3f,210);
  initialized=true;BuildFloorGuidance();Apply(0);PositionLights();
 }
 private Light MakeLight(string label,LightType type,Color color,float intensity,float range){var owner=new GameObject(label);owner.transform.SetParent(transform,false);var light=owner.AddComponent<Light>();light.type=type;light.color=color;light.intensity=intensity;light.range=range;light.renderMode=LightRenderMode.ForcePixel;light.cullingMask=~((1<<30)|(1<<31));return light;}
 private bool WalkSegment(Vector3 a,Vector3 b){var delta=b-a;float distance=delta.magnitude;if(distance<.01f)return true;
  foreach(var hit in Physics.CapsuleCastAll(a+Vector3.up*9,a+Vector3.up*90,6,delta.normalized,distance,~((1<<30)|(1<<31)),QueryTriggerInteraction.Ignore)){if(hit.collider.GetComponentInParent<FirstPersonPlayer>())continue;return false;}return true;
 }
 private Vector3 lastRouteEnd;private bool routeBuilt;
 private void BuildFloorGuidance(){
  var camera=Camera.main;if(!camera)return;
  BuildFloorRoute(camera.transform.position,microphone.GetComponentInChildren<Renderer>().bounds.center,"Microphone");
  if(!routeBuilt)return;var approach=lastRouteEnd;
  var xf=gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t=>string.Equals(t.name.Trim(),"xf",StringComparison.OrdinalIgnoreCase));
  if(xf){var renderers=xf.GetComponentsInChildren<Renderer>(true);if(renderers.Length>0){var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);BuildFloorRoute(approach,bounds.center,"XF");}}
 }
 private void BuildFloorRoute(Vector3 start,Vector3 goal,string destination){
  routeBuilt=false;Physics.SyncTransforms();
  float floor=float.PositiveInfinity;foreach(var hit in Physics.RaycastAll(start+Vector3.up*10,Vector3.down,1000,~((1<<30)|(1<<31)),QueryTriggerInteraction.Ignore)){if(hit.normal.y<.7f||hit.collider.GetComponentInParent<FirstPersonPlayer>())continue;floor=Mathf.Min(floor,hit.point.y);}if(float.IsPositiveInfinity(floor))return;
  start.y=floor;goal.y=floor;goal+= (start-goal).normalized*35;
  const float step=18;var goalCell=new Vector2Int(Mathf.RoundToInt((goal.x-start.x)/step),Mathf.RoundToInt((goal.z-start.z)/step));
  Func<Vector2Int,Vector3> point=k=>start+new Vector3(k.x*step,0,k.y*step);
  var open=new List<Vector2Int>{Vector2Int.zero};var closed=new HashSet<Vector2Int>();var parent=new Dictionary<Vector2Int,Vector2Int>();var costs=new Dictionary<Vector2Int,float>{{Vector2Int.zero,0}};Vector2Int reached=Vector2Int.zero;bool found=false;
  for(int iteration=0;iteration<2500&&open.Count>0;iteration++){
   var node=open.OrderBy(k=>costs[k]+Vector2.Distance(k,goalCell)).First();open.Remove(node);if(Vector2Int.Distance(node,goalCell)<=2.5f){reached=node;found=true;break;}closed.Add(node);
   for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++){if(x==0&&z==0)continue;var next=node+new Vector2Int(x,z);if(next.x<Mathf.Min(0,goalCell.x)-6||next.x>Mathf.Max(0,goalCell.x)+6||next.y<Mathf.Min(0,goalCell.y)-6||next.y>Mathf.Max(0,goalCell.y)+6||closed.Contains(next)||!WalkSegment(point(node),point(next)))continue;float cost=costs[node]+Mathf.Sqrt(x*x+z*z);float previous;if(costs.TryGetValue(next,out previous)&&previous<=cost)continue;costs[next]=cost;parent[next]=node;if(!open.Contains(next))open.Add(next);}
  }
  if(!found){Debug.LogWarning("No clear floor guidance route to "+destination+" found. start="+start+" goal="+goal+" closed="+closed.Count+" blockers="+string.Join(",",Physics.CapsuleCastAll(start+Vector3.up*9,start+Vector3.up*90,6,Vector3.forward,18,~((1<<30)|(1<<31)),QueryTriggerInteraction.Ignore).Select(h=>h.collider.name)),this);return;}
  var route=new List<Vector3>{point(reached)};if(WalkSegment(point(reached),goal))route.Insert(0,goal);while(parent.ContainsKey(reached)){reached=parent[reached];route.Add(point(reached));}route.Reverse();
  if(destination=="XF"&&route.Count>1){var forward=Vector3.ProjectOnPlane(route[route.Count-1]-route[0],Vector3.up).normalized;var right=Vector3.Cross(Vector3.up,forward);var shifted=route.Select(p=>p+right*9).ToList();
   // Keep the visual route on the walking side of the dark raised platform.
   bool clear=true;for(int i=1;i<shifted.Count;i++)if(!WalkSegment(shifted[i-1],shifted[i])){clear=false;break;}if(clear){route=shifted;XFGuideRightShift=9;}
   XFGuideRoute=route.ToArray();}
lastRouteEnd=route[route.Count-1];routeBuilt=true;
  // Soft, overlapping pools on the actual floor. No beams or geometry obstruct walking.
  float carried=0;for(int i=1;i<route.Count;i++){var delta=route[i]-route[i-1];float length=delta.magnitude;if(length<.01f)continue;float d=carried;for(;d<length;d+=22){var light=MakeLight("Floor Guidance "+destination+" "+pathLights.Count,LightType.Spot,new Color(.66f,.70f,1f),2.15f,80);light.spotAngle=92;light.innerSpotAngle=35;light.shadows=LightShadows.None;light.transform.position=route[i-1]+delta.normalized*d+Vector3.up*24;light.transform.rotation=Quaternion.Euler(90,0,0);pathLights.Add(light);}carried=d-length;}
 }
 private void PositionLights(){if(!microphone)return;var renderer=microphone.GetComponentInChildren<Renderer>();var point=renderer?renderer.bounds.center:microphone.position;var camera=Camera.main;var toward=camera?Vector3.ProjectOnPlane(camera.transform.position-point,Vector3.up).normalized:Vector3.back;spotlight.transform.position=point+Vector3.up*85+toward*30;spotlight.transform.LookAt(point);if(camera)fill.transform.position=camera.transform.position+Vector3.down*35+camera.transform.forward*20;}
 private void Apply(float amount){
  for(int i=0;i<roomLights.Length;i++)if(roomLights[i])roomLights[i].intensity=intensities[i]*Mathf.Lerp(.1f,1,amount);
  RenderSettings.ambientMode=amount>=1?ambientMode:AmbientMode.Flat;
  RenderSettings.ambientLight=Color.Lerp(new Color(.055f,.045f,.085f),ambientLight,amount);
  RenderSettings.ambientIntensity=Mathf.Lerp(.15f*ambientIntensity,ambientIntensity,amount);
  RenderSettings.reflectionIntensity=Mathf.Lerp(.12f*reflectionIntensity,reflectionIntensity,amount);
  foreach(var pair in emissions)if(pair.Key)pair.Key.SetColor("_EmissionColor",pair.Value*Mathf.Lerp(.16f,1,amount));
  foreach(var light in pathLights)if(light)light.intensity=2.15f*(1-amount);
  if(spotlight)spotlight.intensity=14*(1-amount);if(fill)fill.intensity=.3f*(1-amount);
 }
 private void LateUpdate(){if(!initialized)return;if(!restored)PositionLights();if(!restoring&&!restored&&reward&&reward.IsCollected){restoring=true;StartCoroutine(Restore());}if(!Application.isFocused||EvidenceInspect.BlocksWorldInput||!Input.GetMouseButtonDown(0))return;var camera=Camera.main;if(camera)TryInteract(camera,Cursor.lockState==CursorLockMode.Locked?camera.pixelRect.center:(Vector2)Input.mousePosition);}
 public bool TryInteract(Camera camera,Vector2 pointer){
  if(!initialized||!camera||EvidenceInspect.BlocksWorldInput||!camera.pixelRect.Contains(pointer)||device.IsRetracted||device.IsRetracting)return false;
  Physics.SyncTransforms();foreach(var hit in Physics.RaycastAll(camera.ScreenPointToRay(pointer),camera.farClipPlane,~((1<<30)|(1<<31)),QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance)){
   if(hit.collider.GetComponentInParent<FirstPersonPlayer>())continue;
   if(hit.transform!=microphone&&!hit.transform.IsChildOf(microphone))return false;
   if(Vector3.Distance(camera.transform.position,hit.point)>100)return false;return device.TryRetract();
  }return false;
 }
 private IEnumerator Restore(){
  var camera=Camera.main;if(camera){var effect=camera.GetComponent<Scene2RecoveryGlitch>();if(!effect)effect=camera.gameObject.AddComponent<Scene2RecoveryGlitch>();if(effect.Begin()){while(effect&&effect.IsPlaying)yield return null;}}
  for(float t=0;t<2;t+=Time.unscaledDeltaTime){progress=Mathf.SmoothStep(0,1,t/2);Apply(progress);yield return null;}Apply(1);RestoreOriginals();restored=true;}
 private void RestoreOriginals(){if(!initialized)return;for(int i=0;i<roomLights.Length;i++)if(roomLights[i])roomLights[i].intensity=intensities[i];RenderSettings.ambientMode=ambientMode;RenderSettings.ambientLight=ambientLight;RenderSettings.ambientSkyColor=sky;RenderSettings.ambientEquatorColor=equator;RenderSettings.ambientGroundColor=ground;RenderSettings.ambientIntensity=ambientIntensity;RenderSettings.reflectionIntensity=reflectionIntensity;foreach(var pair in originals)if(pair.Key)pair.Key.sharedMaterials=pair.Value;foreach(var copy in copies.Values)if(copy)Destroy(copy);originals.Clear();copies.Clear();emissions.Clear();}
 private void OnDestroy(){RestoreOriginals();}
}






