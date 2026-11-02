using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(11090)]
[DisallowMultipleComponent]
public sealed class ClickablePanelFall : MonoBehaviour
{
 private static AudioSource musicPlayer;
 private static ClickablePanelFall currentMusicPanel;
 private AudioClip musicClip,impactClip;
 private AudioSource impactPlayer;
 private Rigidbody body;private Vector3 releaseOutward;private int panelNumber;
 private bool released,landed;
 private float groundY;private float floorSurfaceY;
 public bool IsFalling=>released&&!landed;
 public bool IsLanded=>landed;
 public int ImpactCount{get;private set;}
 public float GroundY=>groundY;

 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 private static void Initialize(){SceneManager.sceneLoaded-=Bind;SceneManager.sceneLoaded+=Bind;Bind(SceneManager.GetActiveScene(),LoadSceneMode.Single);}
 private static void Bind(Scene scene,LoadSceneMode mode){
  if(!scene.IsValid()||!string.Equals(scene.name.Replace(" ",""),"scene2",StringComparison.OrdinalIgnoreCase))return;
  var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
  for(int n=1;n<=4;n++){var target=all.FirstOrDefault(t=>t.name.Equals("p"+n,StringComparison.OrdinalIgnoreCase));if(!target)continue;var panel=target.GetComponent<ClickablePanelFall>();if(!panel)panel=target.gameObject.AddComponent<ClickablePanelFall>();panel.Configure(n);}
 }
 private void Configure(int number){
  if(body)return;panelNumber=number;
  var mesh=GetComponent<MeshFilter>();if(!mesh||!mesh.sharedMesh)return;
  body=GetComponent<Rigidbody>();if(!body)body=gameObject.AddComponent<Rigidbody>();
  // Keep the user's body and box. Fit the collision volume to the visible frame.
  var box=GetComponent<BoxCollider>();if(!box)box=gameObject.AddComponent<BoxCollider>();
  box.center=mesh.sharedMesh.bounds.center;box.size=mesh.sharedMesh.bounds.size;box.isTrigger=false;box.enabled=true;box.contactOffset=.25f;
  foreach(var collider in GetComponents<Collider>()){if(collider is MeshCollider)collider.enabled=false;}
  body.isKinematic=true;body.useGravity=false;body.interpolation=RigidbodyInterpolation.Interpolate;
  body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
  body.solverIterations=Mathf.Max(body.solverIterations,64);body.solverVelocityIterations=Mathf.Max(body.solverVelocityIterations,16);
  body.detectCollisions=true;body.maxDepenetrationVelocity=50;
  var scaled=Vector3.Scale(mesh.sharedMesh.bounds.size,transform.lossyScale);scaled=new Vector3(Mathf.Abs(scaled.x),Mathf.Abs(scaled.y),Mathf.Abs(scaled.z));var thin=scaled.x<scaled.y&&scaled.x<scaled.z?Vector3.right:scaled.y<scaled.z?Vector3.up:Vector3.forward;releaseOutward=Vector3.ProjectOnPlane(transform.TransformDirection(thin),Vector3.up).normalized;
  var wall=FindObjectsOfType<MeshRenderer>().FirstOrDefault(r=>{var key=r.name.ToLowerInvariant().Replace(" ","").Replace(".","");return key=="cube62"||key=="cube062"||key=="立方体062";});
  var toward=Vector3.ProjectOnPlane(wall?transform.position-wall.bounds.center:Vector3.zero-transform.position,Vector3.up);if(Vector3.Dot(releaseOutward,toward)<0)releaseOutward=-releaseOutward;if(releaseOutward.sqrMagnitude<.1f)releaseOutward=Vector3.left;
  if(wall){var collider=wall.GetComponent<Collider>();if(!collider){var wallMesh=wall.GetComponent<MeshFilter>();if(wallMesh){var wallBox=wall.gameObject.AddComponent<BoxCollider>();wallBox.center=wallMesh.sharedMesh.bounds.center;wallBox.size=wallMesh.sharedMesh.bounds.size;collider=wallBox;}}if(collider){collider.enabled=true;collider.isTrigger=false;}}

  musicClip=Resources.Load<AudioClip>("PaintingMusic/P"+number);impactClip=Resources.Load<AudioClip>("PropFalls/P"+number+"Thud");
  impactPlayer=gameObject.AddComponent<AudioSource>();impactPlayer.playOnAwake=false;impactPlayer.spatialBlend=.2f;impactPlayer.minDistance=180;impactPlayer.maxDistance=600;impactPlayer.priority=40;impactPlayer.dopplerLevel=0;
  floorSurfaceY=transform.position.y-200;var floorHits=Physics.RaycastAll(transform.position+Vector3.up*5,Vector3.down,800,~((1<<30)|(1<<31)),QueryTriggerInteraction.Ignore).Where(h=>h.normal.y>.7f&&!h.collider.GetComponentInParent<ClickablePanelFall>()&&!h.collider.GetComponentInParent<FirstPersonPlayer>()).ToArray();if(floorHits.Length>0)floorSurfaceY=floorHits.Min(h=>h.point.y);
 }
 private void LateUpdate(){if(!Application.isFocused||EvidenceInspect.BlocksWorldInput||!Input.GetMouseButtonDown(0))return;var cam=Camera.main;if(cam)TryClick(cam,Cursor.lockState==CursorLockMode.Locked?cam.pixelRect.center:(Vector2)Input.mousePosition);}
 public bool TryClick(Camera camera,Vector2 pointer){
  if(!camera||!isActiveAndEnabled||!camera.pixelRect.Contains(pointer)||EvidenceInspect.BlocksWorldInput)return false;
  foreach(var hit in Physics.RaycastAll(camera.ScreenPointToRay(pointer),1000,~((1<<30)|(1<<31)),QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance)){
   if(hit.collider.GetComponentInParent<FirstPersonPlayer>())continue;
   if(hit.transform!=transform&&!hit.transform.IsChildOf(transform))return false;
   return Vector3.Distance(camera.transform.position,hit.point)<=180&&ActivatePainting();
  }return false;
 }
 public bool ActivatePainting(){
  if(!isActiveAndEnabled||!body||!musicClip)return false;
  if(!musicPlayer){var owner=new GameObject("Painting Music Playback");SceneManager.MoveGameObjectToScene(owner,gameObject.scene);musicPlayer=owner.AddComponent<AudioSource>();musicPlayer.playOnAwake=false;musicPlayer.loop=false;musicPlayer.spatialBlend=0;musicPlayer.volume=.8f;}
  musicPlayer.Stop();currentMusicPanel=this;musicPlayer.clip=musicClip;Scene2AudioPlayback.Play(musicPlayer);BeginFall();return true;
 }
 public bool BeginFall(){
  if(released||!body||!isActiveAndEnabled)return false;
  released=true;body.constraints=RigidbodyConstraints.None;body.isKinematic=false;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;body.useGravity=true;body.drag=.035f;body.angularDrag=.55f;body.maxAngularVelocity=3.5f;
  // Initial release impulse only. All subsequent translation, rotation and settling are simulated.
  body.velocity=releaseOutward*(18+panelNumber)+Vector3.down*8;
  var tipAxis=Vector3.Cross(Vector3.up,releaseOutward).normalized;
  float tilt=panelNumber==1?2.1f:panelNumber==2?2.3f:panelNumber==3?2.45f:2f;
  body.angularVelocity=tipAxis*tilt+releaseOutward*(panelNumber%2==0?.07f:-.07f)+Vector3.up*(panelNumber%2==0?.025f:-.025f);
  body.WakeUp();return true;
 }
 // Scene geometry uses large world units: stronger per-body gravity gives a brisk drop without changing the player or other props.
 private void FixedUpdate(){if(released&&body&&!body.isKinematic&&!body.IsSleeping())body.AddForce(Physics.gravity*9,ForceMode.Acceleration);}
 private void OnCollisionEnter(Collision collision){
  if(!released)return;var otherPanel=collision.collider.GetComponentInParent<ClickablePanelFall>();
  for(int i=0;i<collision.contactCount;i++){var contact=collision.GetContact(i);if(contact.normal.y<.5f)continue;bool supported=otherPanel?otherPanel.IsLanded:contact.point.y<=floorSurfaceY+1;
   if(!supported)continue;landed=true;groundY=contact.point.y;if(ImpactCount==0){ImpactCount++;if(impactClip)Scene2AudioPlayback.PlayOneShot(impactPlayer,impactClip,1f);}break;}
 }
}
