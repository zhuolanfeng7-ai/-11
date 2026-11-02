using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-230)]
public sealed class Scene2PlateInstallation : MonoBehaviour
{
 [SerializeField] private int inventorySlot;
 public int InventorySlot=>inventorySlot;
 public int DisplaySlot=>ImportantItemCollectionUI.DisplaySlotForItem(inventorySlot);
 private static readonly Dictionary<Mesh,Vector3> tapeHoleAnchors=new Dictionary<Mesh,Vector3>();
 private static Scene2PlateInstallation active;private static int consumedFrame=-1;
 private Transform machine,slot;private MeshFilter plate;private Renderer plateRenderer;
 private BoxCollider socket;private GameObject preview,installedVisual;private Material previewMaterial;
 private ImportantItemCollectionUI inventory;private Vector3 fitSize;private bool dragging,installing,installed;private float nextSearch;
 public static bool IsDragging=>active&&active.dragging;
 public static bool IsDraggingSlot(int index)=>active&&active.dragging&&active.DisplaySlot==index;
 public static bool BlocksWorldInput=>(active&&(active.dragging||active.installing))||consumedFrame==Time.frameCount;
 public bool IsReady=>socket&&plate;
 public bool IsInstalled=>installed;
 public bool IsInstalling=>installing;
 public Transform Slot=>slot;
 public GameObject InstalledVisual=>installedVisual;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]private static void ResetStatics(){active=null;consumedFrame=-1;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]private static void Initialize(){SceneManager.sceneLoaded-=Bind;SceneManager.sceneLoaded+=Bind;Bind(SceneManager.GetActiveScene(),LoadSceneMode.Single);}
 private static void Bind(Scene scene,LoadSceneMode mode){if(!scene.IsValid()||!string.Equals(scene.name.Replace(" ",""),"scene2",StringComparison.OrdinalIgnoreCase))return;foreach(int index in new[]{0,1,5}){if(scene.GetRootGameObjects().SelectMany(g=>g.GetComponents<Scene2PlateInstallation>()).Any(c=>c.inventorySlot==index))continue;var owner=new GameObject(index==0?"XF Plate Installation":index==1?"XF Spring Installation":"XF Tape Reel Installation");SceneManager.MoveGameObjectToScene(owner,scene);owner.AddComponent<Scene2PlateInstallation>().inventorySlot=index;}}
 private void Configure(){
  var all=gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();machine=all.FirstOrDefault(t=>t.name.Equals("xf",StringComparison.OrdinalIgnoreCase)||t.name.Equals("XP",StringComparison.OrdinalIgnoreCase));var source=all.FirstOrDefault(t=>t.name.Equals(inventorySlot==0?"alloy1":"alloy2",StringComparison.OrdinalIgnoreCase)||(inventorySlot==0&&t.name.Equals("L01",StringComparison.OrdinalIgnoreCase)));if(inventorySlot==5){var ui=Inventory();source=ui&&ui.TapeSource?ui.TapeSource.transform:all.FirstOrDefault(t=>new[]{"cidai1","cidai2","cidai3","cidai4","caidai1","caidai2","caidai3","caidai4"}.Contains(t.name.ToLowerInvariant()));}if(!machine||!source)return;
  plate=source.GetComponent<MeshFilter>();if(!plate)plate=source.GetComponentInChildren<MeshFilter>(true);plateRenderer=plate?plate.GetComponent<Renderer>():null;if(!plate||!plate.sharedMesh||!plateRenderer)return;
  var r=machine.GetComponentInChildren<MeshRenderer>();if(!r)return;var b=r.bounds;
  // Measured against this machine's actual open left channel (including its sloped base).
  fitSize=new Vector3(b.size.x*.085f,1.15f,b.size.z*.62f);
  var center=new Vector3(b.min.x+b.size.x*.115f,b.min.y+b.size.y*.644f,b.min.z+b.size.z*.515f);
  if(inventorySlot==1){fitSize=new Vector3(b.size.x*.164f,.9f,b.size.z*.182f);center=new Vector3(b.min.x+b.size.x*.80f,b.min.y+b.size.y*.709f,b.min.z+b.size.z*.71875f);}
  if(inventorySlot==5){fitSize=new Vector3(b.size.x*.396f,1.65f,b.size.z*.4785f);center=new Vector3(b.min.x+b.size.x*.4625f,b.min.y+b.size.y*.735f,b.min.z+b.size.z*.54375f);}
  // Keep fitted parts visibly proud of their recess; previews and animation share this anchor.
  if(inventorySlot!=5)center+=Vector3.up*b.size.y*(inventorySlot==1?.085f:.035f);
  var anchor=new GameObject(inventorySlot==0?"Alloy1 Rectangular Installation Slot":inventorySlot==1?"Alloy2 Circular Spring Seat":"Tape Reel Central Spindle");slot=anchor.transform;slot.SetPositionAndRotation(center,Quaternion.Euler(-8,0,0));slot.SetParent(machine,true);
  socket=anchor.AddComponent<BoxCollider>();socket.size=new Vector3(fitSize.x+3,6,fitSize.z+4);socket.center=Vector3.up*1.5f;socket.isTrigger=true;
  // Match local dimensions in world units despite the imported machine's 51x scale.
  socket.size=Vector3.Scale(socket.size,new Vector3(1/slot.lossyScale.x,1/slot.lossyScale.y,1/slot.lossyScale.z));socket.center=Vector3.Scale(socket.center,new Vector3(1/slot.lossyScale.x,1/slot.lossyScale.y,1/slot.lossyScale.z));
  var shader=Resources.Load<Shader>("OpeningEffects/PlatePreview");if(shader&&shader.isSupported){previewMaterial=new Material(shader){hideFlags=HideFlags.HideAndDontSave};preview=MakeVisual("Plate Fit Preview",new[]{previewMaterial});SetFitPose(preview.transform);preview.SetActive(false);}
 }
 private GameObject MakeVisual(string label,Material[] materials){var model=new GameObject(label);model.AddComponent<MeshFilter>().sharedMesh=plate.sharedMesh;var renderer=model.AddComponent<MeshRenderer>();renderer.sharedMaterials=materials.Length==1?Enumerable.Repeat(materials[0],plate.sharedMesh.subMeshCount).ToArray():materials;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;var bounds=plate.sharedMesh.bounds;model.transform.rotation=ModelRotation();model.transform.localScale=inventorySlot==5?TapeScale(bounds.size):inventorySlot==0?new Vector3(fitSize.x/bounds.size.x,fitSize.y/bounds.size.y,fitSize.z/bounds.size.z):new Vector3(fitSize.x/bounds.size.x,fitSize.z/bounds.size.y,fitSize.y/bounds.size.z);model.transform.SetParent(machine,true);return model;}
 private int TapeThinAxis(){var size=plate.sharedMesh.bounds.size;return size.x<size.y&&size.x<size.z?0:size.y<size.z?1:2;}
 private Vector3 TapeScale(Vector3 size){int thin=TapeThinAxis();return thin==0?new Vector3(fitSize.y/size.x,fitSize.x/size.y,fitSize.z/size.z):thin==1?new Vector3(fitSize.x/size.x,fitSize.y/size.y,fitSize.z/size.z):new Vector3(fitSize.x/size.x,fitSize.z/size.y,fitSize.y/size.z);}
 private Quaternion ModelRotation(){var orient=inventorySlot==1?Quaternion.Euler(-90,0,0):Quaternion.identity;if(inventorySlot==5){int thin=TapeThinAxis();orient=thin==0?Quaternion.Euler(0,0,90):thin==1?Quaternion.identity:Quaternion.Euler(-90,0,0);}return slot.rotation*orient;}
 private void RefreshTapeSource(){if(inventorySlot!=5||installing||installed)return;var ui=Inventory();if(!ui||!ui.TapeSource||ui.TapeSource==plate)return;plate=ui.TapeSource;plateRenderer=plate.GetComponent<Renderer>();if(preview){Destroy(preview);preview=null;}if(previewMaterial&&IsReady){preview=MakeVisual("Tape Fit Preview",new[]{previewMaterial});SetFitPose(preview.transform);preview.SetActive(false);}}

 private void SetFitPose(Transform model){model.rotation=ModelRotation();model.position=slot.position-model.rotation*Vector3.Scale(inventorySlot==5?FindTapeHoleAnchor(plate.sharedMesh):plate.sharedMesh.bounds.center,model.lossyScale);}
 public static Vector3 FindTapeHoleAnchor(Mesh mesh){
  Vector3 cached;if(tapeHoleAnchors.TryGetValue(mesh,out cached))return cached;var bounds=mesh.bounds;var size=bounds.size;int thin=size.x<size.y&&size.x<size.z?0:size.y<size.z?1:2;int a=thin==0?1:0,b=thin==2?1:2;const int count=57;
  var probe=new GameObject("Tape Hole Alignment Probe"){hideFlags=HideFlags.HideAndDontSave};probe.layer=31;probe.transform.position=Vector3.one*20000;probe.transform.localScale=Vector3.one*100;var collider=probe.AddComponent<MeshCollider>();collider.sharedMesh=mesh;Physics.SyncTransforms();
  var empty=new bool[count,count];var visited=new bool[count,count];var normal=Vector3.zero;normal[thin]=1;float padding=Mathf.Max(size[a],size[b])*.02f+.0001f;float reach=(size[thin]+padding*2)*100;
  for(int x=0;x<count;x++)for(int z=0;z<count;z++){var p=bounds.center;p[a]=Mathf.Lerp(bounds.min[a],bounds.max[a],(x+.5f)/count);p[b]=Mathf.Lerp(bounds.min[b],bounds.max[b],(z+.5f)/count);RaycastHit hit;var front=probe.transform.TransformPoint(p+normal*(bounds.extents[thin]+padding));var back=probe.transform.TransformPoint(p-normal*(bounds.extents[thin]+padding));empty[x,z]=!collider.Raycast(new Ray(front,-normal),out hit,reach)&&!collider.Raycast(new Ray(back,normal),out hit,reach);}
  Vector3 anchor=bounds.center;float best=float.MaxValue;var directions=new[]{Vector2Int.left,Vector2Int.right,Vector2Int.up,Vector2Int.down};
  for(int x=0;x<count;x++)for(int z=0;z<count;z++){if(visited[x,z]||!empty[x,z])continue;var queue=new Queue<Vector2Int>();queue.Enqueue(new Vector2Int(x,z));visited[x,z]=true;int pixels=0;float sumX=0,sumZ=0;bool edge=false;while(queue.Count>0){var point=queue.Dequeue();pixels++;sumX+=point.x+.5f;sumZ+=point.y+.5f;edge|=point.x==0||point.y==0||point.x==count-1||point.y==count-1;foreach(var d in directions){var n=point+d;if(n.x<0||n.x>=count||n.y<0||n.y>=count||visited[n.x,n.y]||!empty[n.x,n.y])continue;visited[n.x,n.y]=true;queue.Enqueue(n);}}if(edge||pixels<3)continue;float u=sumX/pixels/count,v=sumZ/pixels/count;float distance=(u-.5f)*(u-.5f)+(v-.5f)*(v-.5f);if(distance>=best)continue;best=distance;anchor[a]=Mathf.Lerp(bounds.min[a],bounds.max[a],u);anchor[b]=Mathf.Lerp(bounds.min[b],bounds.max[b],v);}
  probe.SetActive(false);Destroy(probe);tapeHoleAnchors[mesh]=anchor;return anchor;
 }
 private ImportantItemCollectionUI Inventory(){if(!inventory)inventory=FindObjectOfType<ImportantItemCollectionUI>();return inventory;}
 private bool HasItem(){var ui=Inventory();return ui&&(inventorySlot==0?ui.HasAlloy:inventorySlot==1?ui.HasAlloy2:ui.HasTape);}
 private bool ConsumeItem(){return inventorySlot==0?inventory.ConsumeAlloy():inventorySlot==1?inventory.ConsumeAlloy2():inventory.ConsumeTape();}
 private bool OtherBusy=>active&&active!=this&&(active.dragging||active.installing);
 private void Update(){
  RefreshTapeSource();
  if(!IsReady){if(Time.unscaledTime>=nextSearch){nextSearch=Time.unscaledTime+.5f;Configure();}return;}
  if(installing||installed)return;var camera=Camera.main;var pointer=(Vector2)Input.mousePosition;
  if(dragging){if(preview)preview.SetActive(CanInstall(camera,pointer));if(Input.GetMouseButtonUp(0))TryDrop(camera,pointer);else if(Input.GetKeyDown(KeyCode.Escape)||!Application.isFocused)CancelDrag();return;}
  if(!Application.isFocused||EvidenceInspect.BlocksWorldInput||!Input.GetMouseButtonDown(0))return;var ui=Inventory();if(!ui||!HasItem())return;
  if(ui.SlotScreenRect(DisplaySlot).Contains(pointer)){TryBeginDrag(pointer);return;}
  for(int i=0;i<ui.VisibleSlotCount;i++)if(ui.SlotScreenRect(i).Contains(pointer))return;
  TryAutoInstall(camera,Cursor.lockState==CursorLockMode.Locked?camera.pixelRect.center:pointer);
 }
 private void LateUpdate(){if(!IsReady||!preview||dragging||installing||installed)return;var camera=Camera.main;var pointer=Cursor.lockState==CursorLockMode.Locked&&camera?camera.pixelRect.center:(Vector2)Input.mousePosition;preview.SetActive(!EvidenceInspect.BlocksWorldInput&&CanInstall(camera,pointer));}
 public bool TryBeginDrag(Vector2 pointer){RefreshTapeSource();var ui=Inventory();if(OtherBusy||!IsReady||installed||installing||dragging||!ui||!HasItem()||!ui.SlotScreenRect(DisplaySlot).Contains(pointer))return false;active=this;dragging=true;consumedFrame=Time.frameCount;Cursor.visible=true;return true;}
 public bool CanInstall(Camera camera,Vector2 pointer){RefreshTapeSource();if(OtherBusy||!IsReady||!camera||installed||installing||!camera.pixelRect.Contains(pointer))return false;var ui=Inventory();if(!ui||!HasItem())return false;Physics.SyncTransforms();RaycastHit targetHit;if(!socket.Raycast(camera.ScreenPointToRay(pointer),out targetHit,1000))return false;if(Vector3.Distance(camera.transform.position,targetHit.point)>160)return false;
  foreach(var hit in Physics.RaycastAll(camera.ScreenPointToRay(pointer),targetHit.distance,~((1<<30)|(1<<31)),QueryTriggerInteraction.Ignore)){if(hit.collider.GetComponentInParent<FirstPersonPlayer>())continue;if(hit.transform==machine||hit.transform.IsChildOf(machine))continue;return false;}return true;}
 public bool TryDrop(Camera camera,Vector2 pointer){if(!dragging)return false;bool valid=CanInstall(camera,pointer);dragging=false;if(preview)preview.SetActive(false);consumedFrame=Time.frameCount;if(!valid){if(active==this)active=null;return false;}return Install();}
 public bool TryAutoInstall(Camera camera,Vector2 pointer){if(dragging||!CanInstall(camera,pointer))return false;return Install();}
 private bool Install(){var ui=Inventory();if(OtherBusy||installing||installed||!ui||!HasItem())return false;active=this;installing=true;if(preview)preview.SetActive(false);consumedFrame=Time.frameCount;StartCoroutine(InstallAnimation());return true;}
 private IEnumerator InstallAnimation(){
  installedVisual=MakeVisual(inventorySlot==0?"Installed Alloy1 Support Plate":inventorySlot==1?"Installed Alloy2 Spiral Spring":"Installed Tape Reel",plateRenderer.sharedMaterials);SetFitPose(installedVisual.transform);var end=installedVisual.transform.position;var rotation=installedVisual.transform.rotation;var start=end+Vector3.up*13;installedVisual.transform.SetPositionAndRotation(start,Quaternion.AngleAxis(12,slot.right)*rotation);
  for(float t=0;t<1.1f;t+=Time.unscaledDeltaTime){float f=Mathf.SmoothStep(0,1,t/1.1f);installedVisual.transform.position=Vector3.Lerp(start,end,f);installedVisual.transform.rotation=Quaternion.Slerp(Quaternion.AngleAxis(12,slot.right)*rotation,rotation,f);yield return null;}
  SetFitPose(installedVisual.transform);if(Inventory()&&ConsumeItem()){installed=true;socket.enabled=false;var sound=Resources.Load<AudioClip>("PropFalls/P1Land");if(sound)Scene2AudioPlayback.PlayAtPoint(sound,slot.position,.2f);}else{Destroy(installedVisual);installedVisual=null;}installing=false;consumedFrame=Time.frameCount;if(active==this)active=null;
 }
 private void CancelDrag(){dragging=false;consumedFrame=Time.frameCount;if(preview)preview.SetActive(false);if(active==this)active=null;}
 private void OnGUI(){if(!dragging)return;var ui=Inventory();if(!ui)return;var icon=inventorySlot==0?ui.AlloyIcon:inventorySlot==1?ui.Alloy2Icon:ui.TapeIcon;if(!icon)return;var pointer=Event.current.mousePosition;float size=72*Mathf.Clamp(Screen.height/900f,.75f,1.15f);int oldDepth=GUI.depth;GUI.depth=-100;GUI.DrawTexture(new Rect(pointer.x-size/2,pointer.y-size/2,size,size),icon,ScaleMode.ScaleToFit,true);GUI.depth=oldDepth;}
 private void OnDisable(){StopAllCoroutines();if(installing&&!installed&&installedVisual)Destroy(installedVisual);installing=false;CancelDrag();}
 private void OnDestroy(){if(slot)Destroy(slot.gameObject);if(preview)Destroy(preview);if(previewMaterial)Destroy(previewMaterial);}
}

