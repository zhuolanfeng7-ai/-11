using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(11100)]
public sealed class Scene2XFRouteSigns : MonoBehaviour
{
 private Transform target;private Bounds bounds;private Material routeMaterial;private Mesh arrowMesh;
 private readonly List<Renderer> arrows=new List<Renderer>();private MaterialPropertyBlock block;
 private bool built,reached;private float opacity=1,nextSearch;
 public bool IsBuilt=>built;public bool HasArrived=>reached;public int ArrowCount=>arrows.Count;public float Opacity=>opacity;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 private static void Initialize(){SceneManager.sceneLoaded-=Bind;SceneManager.sceneLoaded+=Bind;Bind(SceneManager.GetActiveScene(),LoadSceneMode.Single);}
 private static void Bind(Scene scene,LoadSceneMode mode){if(!scene.IsValid()||!string.Equals(scene.name.Replace(" ",""),"scene2",StringComparison.OrdinalIgnoreCase))return;if(scene.GetRootGameObjects().Any(g=>g.GetComponent<Scene2XFRouteSigns>()))return;var root=new GameObject("XF Repair Route Signs");SceneManager.MoveGameObjectToScene(root,scene);root.AddComponent<Scene2XFRouteSigns>();}
 private void Build(){
  if(FindObjectOfType<Scene2XFTurnArrow>()){built=true;return;}
  var lighting=FindObjectOfType<Scene2MicrophoneLighting>();if(!lighting||lighting.XFGuideRoute==null||lighting.XFGuideRoute.Length<2)return;
  target=gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t=>string.Equals(t.name.Trim(),"xf",StringComparison.OrdinalIgnoreCase));if(!target)return;
  var renderers=target.GetComponentsInChildren<Renderer>(true).Where(r=>r.GetComponent<MeshFilter>()).ToArray();if(renderers.Length==0)return;bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
  var shader=Resources.Load<Shader>("OpeningEffects/RouteMarker");if(!shader||!shader.isSupported)return;routeMaterial=new Material(shader){hideFlags=HideFlags.HideAndDontSave};block=new MaterialPropertyBlock();
  arrowMesh=new Mesh{name="Floor Direction Arrow"};arrowMesh.vertices=new[]{new Vector3(0,0,5),new Vector3(-3.6f,0,.8f),new Vector3(-1.35f,0,.8f),new Vector3(-1.35f,0,-4),new Vector3(1.35f,0,-4),new Vector3(1.35f,0,.8f),new Vector3(3.6f,0,.8f)};arrowMesh.triangles=new[]{0,1,2,0,2,5,0,5,6,2,3,4,2,4,5};arrowMesh.RecalculateNormals();arrowMesh.RecalculateBounds();
  var route=lighting.XFGuideRoute;float carried=0;for(int i=1;i<route.Length;i++){var delta=route[i]-route[i-1];float length=delta.magnitude;if(length<.01f)continue;float distance=carried;for(;distance<length;distance+=20){var arrow=new GameObject("XF Floor Arrow "+arrows.Count);arrow.transform.SetParent(transform,false);arrow.transform.position=route[i-1]+delta.normalized*distance+Vector3.up*.12f;arrow.transform.rotation=Quaternion.LookRotation(delta.normalized,Vector3.up);arrow.AddComponent<MeshFilter>().sharedMesh=arrowMesh;var renderer=arrow.AddComponent<MeshRenderer>();renderer.sharedMaterial=routeMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;arrows.Add(renderer);}carried=distance-length;}
  built=true;
 }
 private void LateUpdate(){
  if(!built){if(Time.unscaledTime>=nextSearch){nextSearch=Time.unscaledTime+.5f;Build();}return;}
  var camera=Camera.main;if(camera){if(Vector3.Distance(camera.transform.position,bounds.ClosestPoint(camera.transform.position))<65)reached=true;}
  // Unlike the light pools, route signs survive the alloy1 pickup and world recovery.
  opacity=Mathf.MoveTowards(opacity,reached?0:1,Time.unscaledDeltaTime/1.2f);
  for(int i=0;i<arrows.Count;i++){float phase=Mathf.Repeat(Time.unscaledTime*.65f-i*.16f,1);float pulse=Mathf.Pow(.5f+.5f*Mathf.Cos(phase*Mathf.PI*2),3);block.SetColor("_Color",new Color(.66f,.80f,1,(.48f+.52f*pulse)*opacity));arrows[i].SetPropertyBlock(block);arrows[i].enabled=opacity>.001f;}

 }
 private void OnDestroy(){if(arrowMesh)Destroy(arrowMesh);if(routeMaterial)Destroy(routeMaterial);}
}
