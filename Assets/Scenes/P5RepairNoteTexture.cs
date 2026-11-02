using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class P5RepairNoteTexture
{
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]private static void Initialize(){SceneManager.sceneLoaded-=ApplyScene;SceneManager.sceneLoaded+=ApplyScene;ApplyScene(SceneManager.GetActiveScene(),LoadSceneMode.Single);}
 public static void ApplyScene(Scene scene,LoadSceneMode mode){if(!scene.IsValid()||!string.Equals(scene.name.Replace(" ",""),"scene2",StringComparison.OrdinalIgnoreCase))return;var material=Resources.Load<Material>("RepairNote/P5RepairNote");if(!material)return;foreach(var target in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name.Equals("p5",StringComparison.OrdinalIgnoreCase)))Apply(target,material);}
 public static void Apply(Transform target,Material material){var filter=target.GetComponent<MeshFilter>();var renderer=target.GetComponent<Renderer>();if(!filter||!filter.sharedMesh||!renderer)return;renderer.enabled=true;
 var body=target.GetComponent<Rigidbody>();if(body){if(Application.isPlaying&&!body.isKinematic){body.velocity=Vector3.zero;body.angularVelocity=Vector3.zero;}body.useGravity=false;body.isKinematic=true;}
 foreach(var existingCollider in target.GetComponents<Collider>()){existingCollider.enabled=true;existingCollider.isTrigger=false;}
 renderer.sharedMaterial=material;var bounds=filter.sharedMesh.bounds;var scale=target.lossyScale;var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);block.SetVector("_BoundsMin",bounds.min);block.SetVector("_BoundsSize",bounds.size);float aspect=material.mainTexture?(float)material.mainTexture.width/material.mainTexture.height:1.5f;block.SetFloat("_FitV",Mathf.Abs(bounds.size.y*scale.y)/Mathf.Max(.0001f,Mathf.Abs(bounds.size.z*scale.z))*aspect);renderer.SetPropertyBlock(block);
 if(!target.GetComponent<Collider>()){var collider=target.gameObject.AddComponent<BoxCollider>();collider.center=bounds.center;collider.size=bounds.size;}
 var hover=target.GetComponent<HoverScanHighlight>();if(hover)hover.enabled=false;
 var inspect=target.GetComponent<EvidenceInspect>();if(!inspect)inspect=target.gameObject.AddComponent<EvidenceInspect>();inspect.ConfigureWithoutHighlight(material.mainTexture,160);
 }
}
