using System;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEngine.SceneManagement;

[DefaultExecutionOrder(12500)]
public sealed class Scene2RepairCelebration:MonoBehaviour
{
    const float Enter=.55f,Pulse=.65f,Hold=.15f,Return=.65f;
    const float ReturnAt=Enter+3*Pulse+Hold,Duration=ReturnAt+Return;
    static Scene2RepairCelebration current;
    public static bool IsPlaying=>current&&current.playing;
    public bool HasPlayed{get;private set;}public int FlashCount=>Mathf.Clamp(Mathf.FloorToInt((elapsed-Enter)/Pulse)+1,0,3);
    public float Elapsed=>elapsed;public RenderTexture Preview=>texture;
    bool playing;float elapsed;Transform machine;Renderer[] originals;Camera studio;GameObject stage;RenderTexture texture;
    Material rim;Texture2D halo,star,ring;Rect sourceRect;
    Material soft;RenderTexture background,bloom;Texture2D rays;
    readonly List<Light> lights=new List<Light>(); readonly List<UnityEngine.Object> presentationAssets=new List<UnityEngine.Object>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){current=null;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Initialize(){SceneManager.sceneLoaded-=Bind;SceneManager.sceneLoaded+=Bind;Bind(SceneManager.GetActiveScene(),LoadSceneMode.Single);}
    static void Bind(Scene s,LoadSceneMode mode){if(!s.IsValid()||!s.name.Replace(" ","").Equals("scene2",StringComparison.OrdinalIgnoreCase)||s.GetRootGameObjects().Any(r=>r.GetComponent<Scene2RepairCelebration>()))return;var go=new GameObject("XF Four Part Repair Celebration");SceneManager.MoveGameObjectToScene(go,s);go.AddComponent<Scene2RepairCelebration>();}
    public bool AllPartsInstalled()
    {
        var plates=FindObjectsOfType<Scene2PlateInstallation>();var nails=FindObjectOfType<Scene2NailInstallation>();
        return nails&&nails.IsInstalled&&new[]{0,1,5}.All(id=>plates.Any(p=>p.InventorySlot==id&&p.IsInstalled));
    }
    void LateUpdate()
    {
        if(!playing){if(!HasPlayed&&!Scene2PickupPresentation.IsPlaying&&AllPartsInstalled())Begin();return;}
        elapsed+=Mathf.Min(Time.unscaledDeltaTime,.1f);
        if(elapsed>=Duration){Finish();return;}
        float pulse=PulseValue();if(rim)rim.SetFloat("_Strength",.3f+pulse*1.5f);
        for(int i=0;i<lights.Count;i++)lights[i].intensity=(i==0?.95f:.38f)*(1+pulse*.18f);
        if(studio){studio.Render();UpdateBloom();}
    }
    float PulseValue(){if(elapsed<Enter||elapsed>=Enter+3*Pulse)return 0;return Mathf.Pow(Mathf.Sin(Mathf.Repeat(elapsed-Enter,Pulse)/Pulse*Mathf.PI),2);}
    bool Begin()
    {
        machine=gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t=>t.name.Equals("xf",StringComparison.OrdinalIgnoreCase));if(!machine||!Camera.main)return false;
        originals=machine.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.GetComponent<MeshFilter>()&&!r.name.ToLower().Contains("preview")).ToArray();if(originals.Length==0)return false;
        var bounds=originals[0].bounds;foreach(var renderer in originals)bounds.Encapsulate(renderer.bounds);
        Vector3 screen=Camera.main.WorldToScreenPoint(bounds.center);float size=Mathf.Clamp(bounds.size.magnitude/Mathf.Max(1,screen.z)*Screen.height,100,Screen.width*.7f);sourceRect=new Rect(screen.x-size*.5f,Screen.height-screen.y-size*.3f,size,size*.6f);
        var shader=Resources.Load<Shader>("OpeningEffects/RepairSoftGlow");if(shader)soft=new Material(shader);
        foreach(var renderer in originals)renderer.enabled=false;
        CaptureBackground();BuildStudio(bounds);UpdateBloom();
        halo=SoftTexture(0);star=SoftTexture(1);ring=SoftTexture(2);rays=BurstTexture();elapsed=0;HasPlayed=true;playing=true;current=this;return true;
    }
    void CaptureBackground()
    {
        background=new RenderTexture(960,540,24);background.Create();var camera=Camera.main;var previous=camera.targetTexture;
        var position=camera.transform.position;var rotation=camera.transform.rotation;try{camera.targetTexture=background;camera.transform.position=new Vector3(-180,position.y,211);camera.transform.rotation=Quaternion.LookRotation(new Vector3(1,-.05f,.12f),Vector3.up);camera.Render();}finally{camera.targetTexture=previous;camera.transform.SetPositionAndRotation(position,rotation);}
        Blur(background,4,2.2f);
    }
    void Blur(RenderTexture target,int count,float radius)
    {
        if(!soft)return;var temporary=RenderTexture.GetTemporary(target.width,target.height,0,RenderTextureFormat.ARGB32);
        for(int i=0;i<count;i++){soft.SetVector("_Step",new Vector4(radius/target.width,0,0,0));Graphics.Blit(target,temporary,soft,0);soft.SetVector("_Step",new Vector4(0,radius/target.height,0,0));Graphics.Blit(temporary,target,soft,0);}RenderTexture.ReleaseTemporary(temporary);
    }
    void UpdateBloom(){if(!soft||!texture)return;if(!bloom){bloom=new RenderTexture(640,360,0);bloom.Create();}Graphics.Blit(texture,bloom,soft,1);Blur(bloom,2,2.8f);}
    static Texture2D BurstTexture()
    {
        const int size=512;var pixels=new Color[size*size];for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {float u=(x+.5f)/size*2-1,v=(y+.5f)/size*2-1,r=Mathf.Sqrt(u*u+v*v),a=Mathf.Atan2(v,u);float beam=Mathf.Pow(Mathf.Max(0,Mathf.Sin(a*23+Mathf.Sin(a*7)*1.5f)),7);float alpha=beam*Mathf.Exp(-r*1.2f)*Mathf.SmoothStep(.04f,.28f,r)*Mathf.Clamp01((1-r)*5);pixels[y*size+x]=new Color(.85f,.82f,1,alpha);}
        var result=new Texture2D(size,size,TextureFormat.RGBA32,false);result.SetPixels(pixels);result.Apply();return result;
    }
    void BuildStudio(Bounds bounds)
    {
        var origin=Vector3.one*34000;stage=new GameObject("Repaired Recorder Presentation Studio");stage.transform.SetParent(transform,false);
        var root=new GameObject("Complete Recorder With Installed Parts").transform;root.SetParent(stage.transform,false);root.position=origin;
        var shader=Resources.Load<Shader>("OpeningEffects/PickupRim");if(shader){rim=new Material(shader);rim.SetColor("_Tint",new Color(.65f,.5f,1));}
        foreach(var source in originals)
        {
            if(source.GetComponentsInParent<Transform>(true).Any(t=>t.name.StartsWith("XF Repair Status LED",StringComparison.OrdinalIgnoreCase)))continue;
            var originalMesh=source.GetComponent<MeshFilter>().sharedMesh;if(!originalMesh)continue;
            var model=new GameObject(source.name+" Presentation Copy");model.layer=30;model.transform.SetParent(root,false);model.transform.localPosition=source.transform.position-bounds.center;model.transform.localRotation=source.transform.rotation;model.transform.localScale=source.transform.lossyScale;
            model.AddComponent<MeshFilter>().sharedMesh=originalMesh;var renderer=model.AddComponent<MeshRenderer>();renderer.sharedMaterials=source.sharedMaterials;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            if(rim){var glow=new GameObject("Victory Rim");glow.layer=30;glow.transform.SetParent(model.transform,false);glow.AddComponent<MeshFilter>().sharedMesh=originalMesh;var edge=glow.AddComponent<MeshRenderer>();edge.sharedMaterials=Enumerable.Repeat(rim,originalMesh.subMeshCount).ToArray();edge.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
        }
        root.rotation=Quaternion.Euler(-17,8,3.5f);var renderers=root.GetComponentsInChildren<Renderer>();var fitBounds=renderers[0].bounds;foreach(var r in renderers)fitBounds.Encapsulate(r.bounds);
        float scale=Mathf.Min(4.9f/fitBounds.size.x,2.7f/fitBounds.size.y);root.localScale=Vector3.one*scale;root.position=origin-(fitBounds.center-origin)*scale;
        var camera=new GameObject("Victory Preview Camera");camera.transform.SetParent(stage.transform,false);studio=camera.AddComponent<Camera>();studio.enabled=false;studio.orthographic=true;studio.orthographicSize=1.8f;studio.aspect=16f/9;studio.clearFlags=CameraClearFlags.SolidColor;studio.backgroundColor=Color.clear;studio.cullingMask=1<<30;studio.nearClipPlane=.01f;studio.farClipPlane=40;camera.transform.position=origin+Vector3.back*8;
        texture=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32){antiAliasing=4};texture.Create();studio.targetTexture=texture;
        foreach(var angles in new[]{new Vector3(30,-30,0),new Vector3(20,140,0),new Vector3(-30,35,0)}){var owner=new GameObject("Victory Studio Light");owner.transform.SetParent(stage.transform,false);owner.transform.rotation=Quaternion.Euler(angles);var light=owner.AddComponent<Light>();light.type=LightType.Directional;light.cullingMask=1<<30;light.color=lights.Count==1?new Color(.6f,.7f,1):Color.white;lights.Add(light);}
        studio.Render();
    }
    static Texture2D SoftTexture(int kind)
    {
        int size=kind==2?512:128;var pixels=new Color[size*size];for(int y=0;y<size;y++)for(int x=0;x<size;x++){float u=(x+.5f)/size*2-1,v=(y+.5f)/size*2-1,r=u*u+v*v;float alpha=kind==0?Mathf.Exp(-r*3)*Mathf.Clamp01((1-r)*4):kind==2?Mathf.Exp(-Mathf.Pow((Mathf.Sqrt(r)-.77f)*38,2)):Mathf.Clamp01(Mathf.Exp(-r*55)+Mathf.Exp(-Mathf.Abs(u)*95)*Mathf.Pow(Mathf.Clamp01(1-Mathf.Abs(v)),1.4f)+Mathf.Exp(-Mathf.Abs(v)*95)*Mathf.Pow(Mathf.Clamp01(1-Mathf.Abs(u)),1.4f));pixels[y*size+x]=new Color(1,1,1,alpha);}var result=new Texture2D(size,size,TextureFormat.RGBA32,false);result.SetPixels(pixels);result.Apply();return result;
    }
    Rect CenterRect(){float width=Mathf.Min(Screen.width*.84f,Screen.height*1.55f);return new Rect((Screen.width-width)*.5f,(Screen.height-width*9/16)*.5f,width,width*9/16);}
    void DrawOverlay(Action<Rect,Texture,Color> draw)
    {
        float weight=elapsed<Enter?Mathf.SmoothStep(0,1,elapsed/Enter):elapsed>=ReturnAt?1-Mathf.SmoothStep(0,1,(elapsed-ReturnAt)/Return):1;
        var full=new Rect(0,0,Screen.width,Screen.height);
        if(background)draw(full,background,new Color(1,1,1,weight));
        draw(full,Texture2D.whiteTexture,new Color(.12f,.06f,.25f,.3f*weight));
        var center=CenterRect();var rect=new Rect(Mathf.Lerp(sourceRect.x,center.x,weight),Mathf.Lerp(sourceRect.y,center.y,weight),Mathf.Lerp(sourceRect.width,center.width,weight),Mathf.Lerp(sourceRect.height,center.height,weight));
        float pulse=PulseValue();
        draw(new Rect(rect.center.x-rect.width*.72f,rect.center.y-rect.height*.85f,rect.width*1.44f,rect.height*1.7f),halo,new Color(.54f,.36f,1,(.65f+pulse*.3f)*weight));
        draw(new Rect(rect.center.x-rect.width*.8f,rect.center.y-rect.width*.65f,rect.width*1.6f,rect.width*1.3f),rays,new Color(1,1,1,(.65f+pulse*.35f)*weight));
        var orbit=new Rect(rect.x+rect.width*.08f,rect.yMax-rect.height*.27f,rect.width*.84f,rect.height*.32f);
        draw(new Rect(orbit.x-10,orbit.y-6,orbit.width+20,orbit.height+12),ring,new Color(.5f,.65f,1,.25f*weight));
        draw(orbit,ring,new Color(.9f,.97f,1,(.85f+pulse*.15f)*weight));
        if(bloom){draw(rect,bloom,new Color(.82f,.73f,1,(.5f+pulse*.5f)*weight));draw(new Rect(rect.x-4,rect.y-3,rect.width+8,rect.height+6),bloom,new Color(.9f,.85f,1,.35f*pulse*weight));}
        draw(rect,texture,Color.white);
        for(int i=0;i<7;i++){float a=i*2.39996f+.2f;var point=rect.center+new Vector2(Mathf.Cos(a)*rect.width*.44f,Mathf.Sin(a)*rect.height*.45f);float size=(48+(i%3)*18)*(1+pulse*.22f);draw(new Rect(point.x-size/2,point.y-size/2,size,size),star,new Color(.96f,.95f,1,(.5f+.5f*pulse)*weight));}
        for(int i=0;i<32;i++){float a=i*2.39996f;float radius=.42f+(i%7)*.022f;var point=rect.center+new Vector2(Mathf.Cos(a)*rect.width*radius,Mathf.Sin(a)*rect.height*radius);float size=3+i%3;draw(new Rect(point.x,point.y,size,size),halo,new Color(.8f,.8f,1,(.3f+.4f*pulse)*weight));}
    }
    void OnGUI()
    {
        if(!playing||!texture)return;var color=GUI.color;int depth=GUI.depth;GUI.depth=-600;
        DrawOverlay((rect,image,tint)=>{GUI.color=tint;GUI.DrawTexture(rect,image,ScaleMode.StretchToFill,true);});GUI.color=color;GUI.depth=depth;
    }
#if UNITY_EDITOR
    public void DrawPreviewTo(RenderTexture destination)
    {
        var previous=RenderTexture.active;RenderTexture.active=destination;GL.PushMatrix();
        try{GL.LoadPixelMatrix(0,Screen.width,Screen.height,0);DrawOverlay((rect,image,color)=>Graphics.DrawTexture(rect,image,new Rect(0,0,1,1),0,0,0,0,color*.5f));}
        finally{GL.PopMatrix();RenderTexture.active=previous;}
    }
#endif
    void Finish(){if(!playing)return;playing=false;if(current==this)current=null;foreach(var renderer in originals)if(renderer)renderer.enabled=true;if(stage)Destroy(stage);if(texture){texture.Release();Destroy(texture);}if(rim)Destroy(rim);if(halo)Destroy(halo);if(star)Destroy(star);if(ring)Destroy(ring);if(rays)Destroy(rays);foreach(var asset in presentationAssets)if(asset)Destroy(asset);presentationAssets.Clear();if(soft)Destroy(soft);if(background){background.Release();Destroy(background);}if(bloom){bloom.Release();Destroy(bloom);}}
    void OnDisable(){Finish();}
}
