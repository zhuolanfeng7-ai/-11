using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
[InitializeOnLoad]
public static class Scene3RestoreVisibility {
 const string Report="D:/-11-scene3-audit/visibility-restored.json";
 static Scene3RestoreVisibility(){EditorApplication.delayCall+=Apply;}
 [MenuItem("Tools/Scene3/Restore Visible Scene View")]
 public static void Apply(){
  if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Apply;return;}
  if(File.Exists(Report)||EditorApplication.isPlayingOrWillChangePlaymode)return;
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/scene3.unity")return;
  try{
   var roots=scene.GetRootGameObjects();var root=roots.FirstOrDefault(g=>g.name=="CompanySource");
   if(!root)throw new Exception("CompanySource root is absent; no scene objects were modified.");
   if(!root.activeSelf){Undo.RecordObject(root,"Restore Scene3 model visibility");root.SetActive(true);EditorSceneManager.MarkSceneDirty(scene);}
   UnityEditor.SceneManagement.StageUtility.GoToMainStage();
   SceneVisibilityManager.instance.ExitIsolation();SceneVisibilityManager.instance.ShowAll();Tools.visibleLayers=~0;
   var renderers=root.GetComponentsInChildren<Renderer>(true);var visible=renderers.Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.bounds.size.sqrMagnitude>0&&r.bounds.size.magnitude<10000).ToArray();
   if(visible.Length==0)throw new Exception("No enabled mesh renderers under company root.");
   var xs=visible.Select(r=>r.bounds.center.x).OrderBy(x=>x).ToArray();var ys=visible.Select(r=>r.bounds.center.y).OrderBy(x=>x).ToArray();var zs=visible.Select(r=>r.bounds.center.z).OrderBy(x=>x).ToArray();var center=new Vector3(xs[xs.Length/2],ys[ys.Length/2],zs[zs.Length/2]);var room=visible.Where(r=>(r.bounds.center-center).magnitude<8 && r.bounds.size.magnitude<15).ToArray();if(room.Length==0)room=visible;Bounds bounds=room[0].bounds;foreach(var r in room.Skip(1))bounds.Encapsulate(r.bounds);
   var view=SceneView.lastActiveSceneView;if(!view)view=EditorWindow.GetWindow<SceneView>();
   view.in2DMode=false;view.orthographic=false;view.sceneLighting=false;view.drawGizmos=true;view.cameraSettings.dynamicClip=true;
   view.cameraSettings.nearClip=0.01f;view.cameraSettings.farClip=10000f;
   view.rotation=Quaternion.Euler(25,-35,0);view.Frame(bounds,true);view.Focus();
   Selection.activeGameObject=root;SceneView.RepaintAll();
   if(scene.isDirty)EditorSceneManager.SaveScene(scene);
   File.WriteAllText(Report,"{\"renderers\":"+renderers.Length+",\"visibleRenderers\":"+visible.Length+",\"center\":\""+bounds.center+"\",\"size\":\""+bounds.size+"\",\"sceneViewFramed\":true}");
   EditorApplication.delayCall+=()=>{var cam=view.camera;if(!cam)return;var rt=RenderTexture.GetTemporary(1000,700,24);var old=cam.targetTexture;var active=RenderTexture.active;try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var img=new Texture2D(1000,700,TextureFormat.RGB24,false);img.ReadPixels(new Rect(0,0,1000,700),0,0);img.Apply();File.WriteAllBytes("D:/-11-scene3-audit/scene-view-restored.png",img.EncodeToPNG());UnityEngine.Object.DestroyImmediate(img);}finally{cam.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);}};
  }catch(Exception ex){File.WriteAllText("D:/-11-scene3-audit/visibility-error.txt",ex.ToString());Debug.LogException(ex);}
 }
}

