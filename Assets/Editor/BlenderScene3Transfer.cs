#if LOCAL_SCENE3_AUDIT
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
[InitializeOnLoad]
public static class BlenderScene3Transfer {
 [Serializable] public class Map { public string path,uvName; public float[] scale,offset; public float rotation; public string extension; }
 [Serializable] public class Mat {public string name,issue;public bool supported;public float[] color,emission; public float metallic,roughness,normalStrength,transmission,opacity=1; public Map @base,metal,rough,normal,emit,alpha;}
 [Serializable] public class Obj {public int[] usedSlots;public string name,activeUV;public string[] materials,fbxUVNames,verifiedUVNames;}
 [Serializable] public class SourceLight {public string name,type;public float energy,spotAngle;public float[] color;} [Serializable] public class Manifest {public Mat[] materials;public Obj[] objects;public string[] issues;public SourceLight[] lights;public float[] worldColor;public float worldStrength,cameraFov;public string cameraName;}
 const string Folder="Assets/BlenderScene3";
 static BlenderScene3Transfer(){EditorApplication.delayCall+=Run;EditorApplication.update+=Poll;} static double nextPoll;static void Poll(){if(EditorApplication.timeSinceStartup<nextPoll)return;nextPoll=EditorApplication.timeSinceStartup+5;if(!EditorApplication.isCompiling&&!EditorApplication.isUpdating)Run();}
 static string Id(string value){using(var h=System.Security.Cryptography.SHA256.Create())return string.Concat(h.ComputeHash(System.Text.Encoding.UTF8.GetBytes(value)).Take(10).Select(b=>b.ToString("x2")));}
 static Color ColorOf(float[] x){return new Color(x[0],x[1],x[2],x.Length>3?x[3]:1);}
 static Texture2D Texture(Map map,string role){
  if(map==null)return null;
  string p=Folder+"/TypedTextures/"+Path.GetFileNameWithoutExtension(map.path)+"_"+role+Path.GetExtension(map.path);
  if(File.Exists(p)){var current=AssetImporter.GetAtPath(p) as TextureImporter;if(current!=null && current.textureType==(role=="normal"?TextureImporterType.NormalMap:TextureImporterType.Default) && current.sRGBTexture==(role=="color") && current.textureCompression==TextureImporterCompression.Uncompressed && current.maxTextureSize==8192)return AssetDatabase.LoadAssetAtPath<Texture2D>(p);}else File.Copy(map.path,p);
  AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceSynchronousImport);
  var imp=(TextureImporter)AssetImporter.GetAtPath(p);
  imp.textureType=role=="normal"?TextureImporterType.NormalMap:TextureImporterType.Default;
  imp.sRGBTexture=role=="color";imp.textureCompression=TextureImporterCompression.Uncompressed;
  imp.maxTextureSize=8192;imp.wrapMode=map.extension=="REPEAT"?TextureWrapMode.Repeat:TextureWrapMode.Clamp;
  imp.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
 }
 static void SetMap(Material mat,Map map,string texture,string uv,string role){
  if(map==null)return;mat.SetTexture(texture,Texture(map,role));
  mat.SetVector(uv,new Vector4(map.scale[0],map.scale[1],map.offset[0],map.offset[1]));
 }
  static int Channel(Map map,Obj ob){if(map==null)return 0;string n=string.IsNullOrEmpty(map.uvName)?ob.activeUV:map.uvName;int index=Array.IndexOf(ob.fbxUVNames,n);if(index<0)return 0;if(index>1)throw new Exception("Unsupported UV channel: "+ob.name);return index;}
 [MenuItem("Tools/Apply Blender Scene2 Materials")] static void Run(){ if(!File.Exists("D:/-11-scene3-audit/ready.flag")||!File.Exists(Folder+"/manifest-corrected.json"))return;
  string done="D:/-11-scene3-audit/transfer-result.json";
  if(File.Exists(done)||EditorApplication.isPlayingOrWillChangePlaymode)return;
  var s=EditorSceneManager.GetActiveScene();
  if(s.path!="Assets/Scenes/scene3.unity"){File.WriteAllText("D:/-11-scene3-audit/transfer-status.txt","Waiting: Scene2 must be active and saved");return;}
  try{
   EditorSceneManager.SaveScene(s);if(!File.Exists("D:/-11-scene3-audit/scene3.saved-before.unity"))File.Copy(s.path,"D:/-11-scene3-audit/scene3.saved-before.unity");
   var model=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/CompanySource.fbx");if(!model)throw new Exception("Source FBX not imported"); if(!s.GetRootGameObjects().Any(g=>g.name=="CompanySource")){var instance=(GameObject)PrefabUtility.InstantiatePrefab(model,s);instance.name="CompanySource";Undo.RegisterCreatedObjectUndo(instance,"Import Blender company scene");}
   var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Folder+"/manifest-corrected.json"));
   var shader=Shader.Find("BlenderScene3/PrincipledTransfer");if(shader==null)throw new Exception("Transfer shader missing");
   if(ShaderUtil.ShaderHasError(shader))throw new Exception("Transfer shader compile error");
   Directory.CreateDirectory(Folder+"/Materials");Directory.CreateDirectory(Folder+"/TypedTextures");
   var created=new Dictionary<string,Material>();var notes=new List<string>(); foreach(var e in manifest.materials){if(string.IsNullOrEmpty(e.@base?.path))e.@base=null;if(string.IsNullOrEmpty(e.metal?.path))e.metal=null;if(string.IsNullOrEmpty(e.rough?.path))e.rough=null;if(string.IsNullOrEmpty(e.normal?.path))e.normal=null;if(string.IsNullOrEmpty(e.emit?.path))e.emit=null;if(string.IsNullOrEmpty(e.alpha?.path))e.alpha=null;}
   foreach(var e in manifest.materials){
    if(!e.supported){notes.Add("UNCONVERTED|"+e.name+"|"+e.issue);continue;}
    var selectedShader=(e.alpha!=null||e.transmission>0||e.opacity<0.999f)?Shader.Find("BlenderScene3/PrincipledTransparency"):shader;if(selectedShader==null||ShaderUtil.ShaderHasError(selectedShader))throw new Exception("Opacity shader error");var p=Folder+"/Materials/"+Id(e.name)+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(p);
    if(!mat){mat=new Material(selectedShader);mat.name=e.name;AssetDatabase.CreateAsset(mat,p);}mat.shader=selectedShader;foreach(var prop in new[]{"_MainTex","_MetalTex","_RoughTex","_BumpMap","_EmissionTex","_AlphaTex"})mat.SetTexture(prop,null);
    mat.SetColor("_Color",ColorOf(e.color).gamma);mat.SetFloat("_Opacity",e.transmission>0?Mathf.Max(0.08f,1-e.transmission):e.opacity);mat.SetFloat("_Metallic",e.metallic);mat.SetFloat("_Roughness",e.roughness);
    mat.SetColor("_EmissionColor",ColorOf(e.emission).gamma);mat.SetFloat("_NormalStrength",e.normalStrength);
    SetMap(mat,e.@base,"_MainTex","_BaseUV","color");SetMap(mat,e.metal,"_MetalTex","_MetalUV","data");
    SetMap(mat,e.rough,"_RoughTex","_RoughUV","data");SetMap(mat,e.normal,"_BumpMap","_NormalUV","normal");SetMap(mat,e.emit,"_EmissionTex","_EmitUV","color");SetMap(mat,e.alpha,"_AlphaTex","_AlphaUV","data");mat.SetFloat("_AlphaAngle",e.alpha?.rotation??0);
    mat.SetVector("_Angles",new Vector4(e.@base?.rotation??0,e.metal?.rotation??0,e.rough?.rotation??0,e.normal?.rotation??0));
    mat.SetFloat("_EmitAngle",e.emit?.rotation??0);mat.SetFloat("_UseMetal",e.metal!=null?1:0);mat.SetFloat("_UseRough",e.rough!=null?1:0);mat.SetFloat("_UseNormal",e.normal!=null?1:0);
    EditorUtility.SetDirty(mat);created.Add(e.name,mat);
   }
   var variants=new Dictionary<string,Material>();var entries=manifest.materials.ToDictionary(e=>e.name);var company=s.GetRootGameObjects().Single(g=>g.name=="CompanySource");foreach(var old in s.GetRootGameObjects().Where(g=>g!=company && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g)=="Assets/作品集4公司.fbx")){Undo.RecordObject(old,"Preserve original company model");old.SetActive(false);}var renderers=company.GetComponentsInChildren<Renderer>(true);
   var assignments=new List<Tuple<Renderer,Material[]>>();int slots=0;var boundNames=new HashSet<string>();
   foreach(var ob in manifest.objects){
    var matches=renderers.Where(r=>r.name==ob.name).ToArray();
    if(matches.Length==0){notes.Add("ABSENT_FROM_SCENE|"+ob.name);continue;}if(matches.Length!=1)throw new Exception("Ambiguous renderer: "+ob.name);
    var r=matches[0];var mats=r.sharedMaterials;if(ob.materials.Length==0){notes.Add("NO_SOURCE_MATERIAL|"+ob.name);continue;}
    var mf=r.GetComponent<MeshFilter>();var mesh=mf?mf.sharedMesh:(r is SkinnedMeshRenderer sk?sk.sharedMesh:null);
    if(!mesh)continue;if(mesh.subMeshCount!=ob.materials.Length){if(ob.usedSlots!=null && ob.usedSlots.Length==mesh.subMeshCount){ob.materials=ob.usedSlots.Select(i=>ob.materials[i]).ToArray();}else{notes.Add("SLOT_MISMATCH|"+ob.name);continue;}}
    if(mats.Length!=ob.materials.Length){notes.Add("RENDERER_SLOT_MISMATCH|"+ob.name);continue;}
    for(int i=0;i<mats.Length;i++){if(created.TryGetValue(ob.materials[i],out var replacement)){var e=entries[ob.materials[i]];var channels=new Vector4(Channel(e.@base,ob),Channel(e.metal,ob),Channel(e.rough,ob),Channel(e.normal,ob));var emitChannel=Channel(e.emit,ob);var alphaChannel=Channel(e.alpha,ob);string variantKey=e.name+"|"+channels.ToString()+"|"+emitChannel+"|"+alphaChannel;if(!variants.TryGetValue(variantKey,out var variant)){string vp=Folder+"/Materials/"+Id(variantKey)+".mat";variant=AssetDatabase.LoadAssetAtPath<Material>(vp);if(!variant){variant=new Material(replacement);variant.name=e.name;AssetDatabase.CreateAsset(variant,vp);}else variant.CopyPropertiesFromMaterial(replacement);variant.shader=replacement.shader;variant.SetFloat("_AlphaUVChannel",alphaChannel);variant.SetVector("_UVChannels",channels);variant.SetFloat("_EmitUVChannel",emitChannel);EditorUtility.SetDirty(variant);variants.Add(variantKey,variant);}mats[i]=variant;slots++;boundNames.Add(e.name);notes.Add("BOUND|"+ob.name+"|"+i+"|"+ob.materials[i]);}else notes.Add("PENDING|"+ob.name+"|"+i+"|"+ob.materials[i]);}
    assignments.Add(Tuple.Create(r,mats));
   }
   foreach(var pair in assignments){Undo.RecordObject(pair.Item1,"Transfer Blender Scene2 material slots");pair.Item1.sharedMaterials=pair.Item2;PrefabUtility.RecordPrefabInstancePropertyModifications(pair.Item1);}
      foreach(var l in company.GetComponentsInChildren<Light>(true)){var src=manifest.lights.FirstOrDefault(x=>x.name==l.name);if(src==null)continue;l.color=new Color(src.color[0],src.color[1],src.color[2]);l.intensity=src.type=="SUN"?src.energy*0.065f:src.energy/(4*Mathf.PI)*0.1f;l.range=12;l.lightmapBakeType=LightmapBakeType.Realtime;if(src.type=="AREA")l.type=LightType.Point;if(src.type=="SPOT")l.spotAngle=src.spotAngle*Mathf.Rad2Deg;l.shadows=LightShadows.Soft;l.shadowBias=0.001f;l.shadowNormalBias=0.01f;l.shadowNearPlane=0.01f;l.shadowCustomResolution=512;l.renderMode=LightRenderMode.ForcePixel;}
   foreach(var g in s.GetRootGameObjects().Where(g=>g.name=="Directional Light"))g.SetActive(false);
   var cam=company.GetComponentsInChildren<Camera>(true).FirstOrDefault(c=>c.name==manifest.cameraName);
   if(cam){foreach(var c in s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)))c.enabled=c==cam;cam.tag="MainCamera";cam.fieldOfView=manifest.cameraFov;cam.nearClipPlane=0.01f;cam.farClipPlane=1000;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=ColorOf(manifest.worldColor).gamma*manifest.worldStrength;cam.allowHDR=true;var view=cam.GetComponent<Scene3ColorView>();if(!view)view=cam.gameObject.AddComponent<Scene3ColorView>();view.viewShader=Shader.Find("BlenderScene3/ColorView");}
   RenderSettings.skybox=null;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=ColorOf(manifest.worldColor).gamma;RenderSettings.ambientIntensity=manifest.worldStrength;RenderSettings.sun=company.GetComponentsInChildren<Light>().FirstOrDefault(l=>l.type==LightType.Directional);
      for(int n=0;n<3;n++){string probeName="Scene3 Reflection "+n;var pg=s.GetRootGameObjects().FirstOrDefault(g=>g.name==probeName);if(!pg){pg=new GameObject(probeName);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(pg,s);Undo.RegisterCreatedObjectUndo(pg,"Add interior reflection");}pg.transform.position=new Vector3(0,1.2f,-2.5f+n*2);var probe=pg.GetComponent<ReflectionProbe>();if(!probe)probe=pg.AddComponent<ReflectionProbe>();probe.mode=UnityEngine.Rendering.ReflectionProbeMode.Realtime;probe.refreshMode=UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake;probe.timeSlicingMode=UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.IndividualFaces;probe.resolution=128;probe.hdr=true;probe.boxProjection=true;probe.size=new Vector3(4.5f,3.5f,4.5f);probe.intensity=0.45f;probe.nearClipPlane=0.02f;probe.farClipPlane=25;probe.clearFlags=UnityEngine.Rendering.ReflectionProbeClearFlags.SolidColor;probe.backgroundColor=new Color(.08f,.08f,.1f);probe.RenderProbe();}
   foreach(var r in renderers){r.reflectionProbeUsage=UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes;r.receiveShadows=true;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.TwoSided;}
         var fillGO=s.GetRootGameObjects().FirstOrDefault(g=>g.name=="Scene3 Soft Interior Fill");if(!fillGO){fillGO=new GameObject("Scene3 Soft Interior Fill");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fillGO,s);Undo.RegisterCreatedObjectUndo(fillGO,"Match Blender interior brightness");}fillGO.transform.position=new Vector3(0,1.8f,-1.0f);var fill=fillGO.GetComponent<Light>();if(!fill)fill=fillGO.AddComponent<Light>();fill.type=LightType.Point;fill.renderMode=LightRenderMode.ForcePixel;fill.color=new Color(.92f,.94f,1);fill.intensity=.28f;fill.range=6;fill.shadows=LightShadows.Soft;fill.shadowBias=.005f;fill.shadowNormalBias=.02f;fill.shadowCustomResolution=512;
   var lut=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/IridescenceLUT.png");var lutImporter=AssetImporter.GetAtPath(Folder+"/IridescenceLUT.png") as TextureImporter;if(lutImporter && lutImporter.wrapMode!=TextureWrapMode.Clamp){lutImporter.wrapMode=TextureWrapMode.Clamp;lutImporter.textureCompression=TextureImporterCompression.Uncompressed;lutImporter.mipmapEnabled=false;lutImporter.SaveAndReimport();lut=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/IridescenceLUT.png");}
   foreach(var rr in renderers.Where(x=>x.name=="table"||x.name=="table2"||x.sharedMaterials.Any(mm=>mm && mm.name.Contains("Holographic Iridescence")))){var path=Folder+"/Materials/SourceIridescence.mat";var hologram=AssetDatabase.LoadAssetAtPath<Material>(path);if(!hologram){hologram=new Material(Shader.Find("BlenderScene3/SourceIridescence"));AssetDatabase.CreateAsset(hologram,path);}hologram.SetTexture("_FacingLUT",lut);EditorUtility.SetDirty(hologram);var arr=rr.sharedMaterials;for(int j=0;j<arr.Length;j++)arr[j]=hologram;Undo.RecordObject(rr,"Restore Blender holographic tables");rr.sharedMaterials=arr;PrefabUtility.RecordPrefabInstancePropertyModifications(rr);notes.Add("IRIDESCENCE_RESTORED|"+rr.name);}
AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);
   foreach(var pair in assignments)for(int i=0;i<pair.Item2.Length;i++)if(pair.Item1.name!="table"&&pair.Item1.name!="table2"&&!pair.Item1.sharedMaterials.Any(mm=>mm&&mm.shader.name=="BlenderScene3/SourceIridescence")&&pair.Item1.sharedMaterials[i]!=pair.Item2[i])throw new Exception("Assignment verification failed");
   File.WriteAllLines("D:/-11-scene3-audit/material-bindings.txt",notes);
   File.WriteAllText(done,"{\"convertedMaterials\":"+boundNames.Count+",\"assignedSlots\":"+slots+",\"verifiedRenderers\":"+assignments.Count+",\"complete\":false,\"lightingVerified\":false}");
   if(cam){var rt=RenderTexture.GetTemporary(960,640,24,RenderTextureFormat.ARGB32);var prev=cam.targetTexture;var active=RenderTexture.active;try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var pic=new Texture2D(960,640,TextureFormat.RGB24,false);pic.ReadPixels(new Rect(0,0,960,640),0,0);pic.Apply();File.WriteAllBytes("D:/-11-scene3-audit/unity-preview.png",pic.EncodeToPNG());UnityEngine.Object.DestroyImmediate(pic);}finally{cam.targetTexture=prev;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);}}Debug.Log("Scene3 Blender transfer saved and verified");
  }catch(Exception ex){File.WriteAllText("D:/-11-scene3-audit/transfer-error.txt",ex.ToString());Debug.LogException(ex);}
 }
}

















#endif
