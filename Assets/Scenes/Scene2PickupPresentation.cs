using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// A real model on a transparent render target: no text or full-screen strobe.
[DefaultExecutionOrder(12000)]
public sealed class Scene2PickupPresentation : MonoBehaviour
{
    public const float AppearSeconds=.28f;
    public const float PulseSeconds=.6f;
    public const float HoldSeconds=.12f;
    public const float FlightSeconds=.72f;
    public const float FlightStarts=AppearSeconds+3*PulseSeconds+HoldSeconds;
    public const float Duration=FlightStarts+FlightSeconds;
    private static Scene2PickupPresentation current;
    public static bool IsPlaying=>current&&current.running;
    private ImportantItemCollectionUI inventory;
    private int itemId,displaySlot;
    private MeshFilter itemSource;
    public int DestinationSlot=>displaySlot;
    private Texture2D icon,halo,star;
    private RenderTexture modelTexture;
    private Camera previewCamera;
    private GameObject previewRoot;
    private Transform model;
    private Quaternion restRotation;
    private Vector3 origin;
    private readonly List<Material> materials=new List<Material>();
    private Material rimMaterial;
    private float presentationTime;
    private bool running,committed;
    private bool scene1Glasses,scene1Piece,scene1Film,scene1Sketch;
    public const string GlassesIdentifier="A-21";
    public const string PieceIdentifier="A-666";
    public const string FilmIdentifier="A-623";
    public const string SketchIdentifier="A-98";
    private Texture sketchBackTexture;
    private int sketchOpenedFrame;
    private bool sketchBack,sketchReturning,sketchReview;
    private float sketchBackElapsed;
    public bool ShowingSketchBack=>scene1Sketch&&sketchBack;
    public bool WaitingForSketchFlip=>scene1Sketch&&!sketchBack&&!sketchReturning&&Elapsed>=FlightStarts-.002f;
    public float Elapsed=>presentationTime;
    public int PulseCount=>Mathf.Clamp(Mathf.FloorToInt((Elapsed-AppearSeconds)/PulseSeconds)+1,0,3);
    public RenderTexture ModelTexture=>modelTexture;
    public Rect ItemRect=>PresentationRect(Screen.width,Screen.height,Elapsed);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState(){current=null;}
    public static bool BeginScene1Glasses(MeshFilter source,Renderer renderer)
    {
        if(IsPlaying||!source||!source.sharedMesh||!renderer||!source.gameObject.scene.name.Replace(" ","").Equals("scene1",System.StringComparison.OrdinalIgnoreCase))return false;
        var ui=FindObjectOfType<ImportantItemCollectionUI>();if(!ui)ui=new GameObject("Important Item Collection UI").AddComponent<ImportantItemCollectionUI>();
        if(ui.VisibleIcon(0))return false;
        var owner=new GameObject("Scene1 A-21 Glasses Pickup Presentation");SceneManager.MoveGameObjectToScene(owner,source.gameObject.scene);
        var effect=owner.AddComponent<Scene2PickupPresentation>();effect.scene1Glasses=true;effect.itemId=211;effect.displaySlot=0;effect.inventory=ui;effect.itemSource=source;effect.BuildPreview(source,renderer);
        var target=RenderTexture.GetTemporary(160,90,0,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;
        Graphics.Blit(effect.modelTexture,target);RenderTexture.active=target;effect.icon=new Texture2D(160,90,TextureFormat.RGBA32,false);effect.icon.ReadPixels(new Rect(0,0,160,90),0,0);effect.icon.Apply();RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);
        effect.running=true;current=effect;return true;
    }
    public static bool BeginScene1Piece(Texture portrait)
    {
        if(IsPlaying||!portrait||!SceneManager.GetActiveScene().name.Replace(" ","").Equals("scene1",System.StringComparison.OrdinalIgnoreCase))return false;
        var ui=FindObjectOfType<ImportantItemCollectionUI>();if(!ui)ui=new GameObject("Important Item Collection UI").AddComponent<ImportantItemCollectionUI>();
        if(ui.VisibleIcon(1))return false;
        var owner=new GameObject("Scene1 A-666 Restored Memory Presentation");
        var effect=owner.AddComponent<Scene2PickupPresentation>();effect.scene1Piece=true;effect.itemId=666;effect.displaySlot=1;effect.inventory=ui;
        var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);
        effect.BuildPreview(quad.GetComponent<MeshFilter>(),quad.GetComponent<Renderer>());Destroy(quad);
        var material=new Material(Shader.Find("Unlit/Texture")){mainTexture=portrait};effect.materials.Add(material);
        effect.model.GetComponent<Renderer>().sharedMaterial=material;
        float aspect=portrait.width/(float)portrait.height;
        float height=Mathf.Min(2.35f,4.4f/aspect);
        effect.model.rotation=Quaternion.identity;effect.model.position=effect.origin;effect.model.localScale=new Vector3(height*aspect,height,1);
        // Fill more of the showcase while keeping the complete portrait visible.
        effect.previewCamera.orthographicSize=1.25f;
        if(effect.rimMaterial)effect.rimMaterial.SetFloat("_Strength",0);
        effect.previewCamera.Render();
        var target=RenderTexture.GetTemporary(160,90,0,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;
        Graphics.Blit(effect.modelTexture,target);RenderTexture.active=target;effect.icon=new Texture2D(160,90,TextureFormat.RGBA32,false);effect.icon.ReadPixels(new Rect(0,0,160,90),0,0);effect.icon.Apply();RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);
        effect.running=true;current=effect;return true;
    }
    public static bool BeginScene1Sketch(Texture portrait,Texture back=null,bool review=false)
    {
        if(IsPlaying||!portrait||!SceneManager.GetActiveScene().name.Replace(" ","").Equals("scene1",System.StringComparison.OrdinalIgnoreCase))return false;
        var ui=FindObjectOfType<ImportantItemCollectionUI>();if(!ui)ui=new GameObject("Important Item Collection UI").AddComponent<ImportantItemCollectionUI>();
        if(ui.VisibleIcon(3)&&!review)return false;
        var owner=new GameObject("Scene1 A-98 Sketch Pickup Presentation");
        var effect=owner.AddComponent<Scene2PickupPresentation>();effect.scene1Sketch=true;effect.sketchBackTexture=back?back:portrait;effect.sketchReview=review;effect.sketchOpenedFrame=Time.frameCount;effect.itemId=98;effect.displaySlot=3;effect.inventory=ui;
        var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);
        effect.BuildPreview(quad.GetComponent<MeshFilter>(),quad.GetComponent<Renderer>());Destroy(quad);
        var material=new Material(Shader.Find("Unlit/Texture")){mainTexture=portrait};effect.materials.Add(material);
        effect.model.GetComponent<Renderer>().sharedMaterial=material;
        float aspect=portrait.width/(float)portrait.height;
        float height=Mathf.Min(2.35f,4.4f/aspect);
        effect.model.rotation=Quaternion.identity;effect.model.position=effect.origin;effect.model.localScale=new Vector3(height*aspect,height,1);
        // Fill more of the showcase while keeping the complete portrait visible.
        effect.previewCamera.orthographicSize=1.25f;
        if(effect.rimMaterial)effect.rimMaterial.SetFloat("_Strength",0);
        effect.previewCamera.Render();
        var target=RenderTexture.GetTemporary(160,90,0,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;
        Graphics.Blit(effect.modelTexture,target);RenderTexture.active=target;effect.icon=new Texture2D(160,90,TextureFormat.RGBA32,false);effect.icon.ReadPixels(new Rect(0,0,160,90),0,0);effect.icon.Apply();RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);
        if(review)effect.presentationTime=FlightStarts-.001f;effect.running=true;current=effect;return true;
    }
    public static bool BeginScene1Film(MeshFilter source,Renderer renderer)
    {
        if(IsPlaying||!source||!source.sharedMesh||!renderer||!source.gameObject.scene.name.Replace(" ","").Equals("scene1",System.StringComparison.OrdinalIgnoreCase))return false;
        var ui=FindObjectOfType<ImportantItemCollectionUI>();if(!ui)ui=new GameObject("Important Item Collection UI").AddComponent<ImportantItemCollectionUI>();
        if(ui.VisibleIcon(2))return false;
        var owner=new GameObject("Scene1 Film Pickup Flight");var effect=owner.AddComponent<Scene2PickupPresentation>();
        effect.scene1Film=true;effect.itemId=212;effect.displaySlot=2;effect.inventory=ui;effect.BuildPreview(source,renderer);
        var target=RenderTexture.GetTemporary(160,90,0,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;
        Graphics.Blit(effect.modelTexture,target);RenderTexture.active=target;effect.icon=new Texture2D(160,90,TextureFormat.RGBA32,false);effect.icon.ReadPixels(new Rect(0,0,160,90),0,0);effect.icon.Apply();RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);
        effect.presentationTime=0;effect.running=true;current=effect;return true;
    }
    public static bool Begin(MeshFilter source,Renderer renderer,ImportantItemCollectionUI ui,Texture2D itemIcon,int item=0)
    {
        int destination=ImportantItemCollectionUI.DisplaySlotForItem(item);
        if(IsPlaying||!source||!source.sharedMesh||!renderer||!ui||destination<0||ui.VisibleIcon(destination)||!itemIcon)return false;
        var owner=new GameObject(source.name+" Pickup Presentation");SceneManager.MoveGameObjectToScene(owner,source.gameObject.scene);
        var effect=owner.AddComponent<Scene2PickupPresentation>();
        effect.inventory=ui;effect.icon=itemIcon;effect.itemId=item;effect.displaySlot=destination;effect.itemSource=source;effect.BuildPreview(source,renderer);
        effect.presentationTime=0;effect.running=true;current=effect;
        return true;
    }

    private void BuildPreview(MeshFilter source,Renderer sourceRenderer)
    {
        origin=Vector3.one*26000;
        previewRoot=new GameObject("Pickup Model Studio");previewRoot.transform.SetParent(transform,false);
        var view=new GameObject("Pickup Preview Camera");view.transform.SetParent(previewRoot.transform,false);
        previewCamera=view.AddComponent<Camera>();previewCamera.enabled=false;previewCamera.cullingMask=1<<30;
        previewCamera.clearFlags=CameraClearFlags.SolidColor;previewCamera.backgroundColor=Color.clear;
        previewCamera.orthographic=true;previewCamera.orthographicSize=1.6f;previewCamera.aspect=16f/9;
        previewCamera.nearClipPlane=.01f;previewCamera.farClipPlane=30;
        previewCamera.transform.position=origin+Vector3.back*6;
        modelTexture=new RenderTexture(1024,576,24,RenderTextureFormat.ARGB32){name="Repair Part Pickup Preview",antiAliasing=4};
        modelTexture.Create();previewCamera.targetTexture=modelTexture;
        var go=new GameObject("Centered Repair Part Model");go.layer=30;model=go.transform;model.SetParent(previewRoot.transform,false);
        go.AddComponent<MeshFilter>().sharedMesh=source.sharedMesh;
        var renderer=go.AddComponent<MeshRenderer>();
        var metal=Resources.Load<Shader>("OpeningEffects/PickupMetal");var originals=sourceRenderer.sharedMaterials;
        var assigned=new Material[Mathf.Max(1,source.sharedMesh.subMeshCount)];
        for(int i=0;i<assigned.Length;i++)
        {
            var original=originals[Mathf.Min(i,originals.Length-1)];
            var material=new Material(metal?metal:Shader.Find("Standard"));if(original)material.mainTexture=original.mainTexture;
            material.color=scene1Glasses?new Color(1,.84f,.38f):itemId==5?new Color(.85f,.83f,.84f):new Color(.72f,.76f,.86f);
            if(material.HasProperty("_TextureWeight"))material.SetFloat("_TextureWeight",scene1Glasses?1:itemId==5?.92f:.16f);
            if(scene1Glasses&&material.HasProperty("_Gold"))material.SetFloat("_Gold",1);
            if(scene1Film){Destroy(material);material=new Material(Shader.Find("Unlit/Texture"));if(original)material.mainTexture=original.mainTexture;}
            materials.Add(material);assigned[i]=material;
        }
        renderer.sharedMaterials=assigned;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        var bounds=source.sharedMesh.bounds;var scale=source.transform.lossyScale;
        scale=new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));
        var dimensions=Vector3.Scale(bounds.size,scale);int thinnest=0;
        for(int i=1;i<3;i++)if(dimensions[i]<dimensions[thinnest])thinnest=i;
        int longest=(thinnest+1)%3;
        for(int i=0;i<3;i++)if(i!=thinnest&&dimensions[i]>dimensions[longest])longest=i;
        int wide=3-longest-thinnest;
        var right=Vector3.zero;right[longest]=1;var up=Vector3.zero;up[wide]=1;
        restRotation=Quaternion.Euler(20,-12,10)*Quaternion.Inverse(Quaternion.LookRotation(Vector3.Cross(right,up),up));
        float fit=Mathf.Min(4f/Mathf.Max(.0001f,dimensions[longest]),2.35f/Mathf.Max(.0001f,dimensions[wide]));
        if(scene1Glasses)
        {
            // The temples dominate the bounds; a bounds-only orientation shows
            // the glasses from above. Preserve the actual frame's frontal view.
            restRotation=Quaternion.Inverse(Quaternion.LookRotation(new Vector3(0,-.7f,2),Vector3.up))*source.transform.rotation;
            var projected=new Bounds();bool first=true;
            var vertices=source.sharedMesh.isReadable?source.sharedMesh.vertices:new Vector3[8];
            if(!source.sharedMesh.isReadable)
            {
                int index=0;
                for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
                    vertices[index++]=bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z));
            }
            foreach(var vertex in vertices)
            {
                var point=restRotation*Vector3.Scale(vertex,scale);
                if(first){projected=new Bounds(point,Vector3.zero);first=false;}else projected.Encapsulate(point);
            }
            fit=Mathf.Min(4.4f/Mathf.Max(.0001f,projected.size.x),2.35f/Mathf.Max(.0001f,projected.size.y));
            // Centre the visible frame rather than the imported mesh pivot.
            model.localScale=scale*fit;
            model.rotation=restRotation;model.position=origin-projected.center*fit;
        }
        else
        {
            model.localScale=scale*fit;
            model.rotation=restRotation;model.position=origin-model.rotation*Vector3.Scale(bounds.center,model.lossyScale);
        }
        var shader=Resources.Load<Shader>("OpeningEffects/PickupRim");
        if(shader)
        {
            rimMaterial=new Material(shader);rimMaterial.SetColor("_Tint",new Color(.48f,.35f,1));
            var glow=new GameObject("Repair Part Edge Glow");glow.layer=30;glow.transform.SetParent(model,false);
            glow.AddComponent<MeshFilter>().sharedMesh=source.sharedMesh;
            var glowRenderer=glow.AddComponent<MeshRenderer>();var rims=new Material[assigned.Length];
            for(int i=0;i<rims.Length;i++)rims[i]=rimMaterial;glowRenderer.sharedMaterials=rims;
            glowRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        halo=MakeSoftTexture(false);star=MakeSoftTexture(true);
        previewCamera.Render();
    }
    private static Texture2D MakeSoftTexture(bool sparkle)
    {
        int size=sparkle?64:128;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp};
        var pixels=new Color[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            float u=(x+.5f)/size*2-1,v=(y+.5f)/size*2-1,r=u*u+v*v;
            float alpha=sparkle?Mathf.Clamp01(Mathf.Exp(-r*28)+Mathf.Exp(-Mathf.Abs(u)*70-Mathf.Abs(v)*5)+Mathf.Exp(-Mathf.Abs(v)*70-Mathf.Abs(u)*5)):Mathf.Exp(-r*4)*Mathf.Clamp01((1-r)*3);
            pixels[y*size+x]=new Color(1,1,1,alpha);
        }
        texture.SetPixels(pixels);texture.Apply();return texture;
    }
    public static float PulseAt(float elapsed)
    {
        float time=elapsed-AppearSeconds;if(time<0||time>=3*PulseSeconds)return 0;
        return Mathf.Pow(Mathf.Sin((time%PulseSeconds)/PulseSeconds*Mathf.PI),2);
    }
    private Rect PresentationRect(int width,int height,float elapsed)
    {
        float appear=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/AppearSeconds));
        float size=Mathf.Min(width*.67f,height*1.22f)*Mathf.Lerp(.88f,1,appear);
        if(scene1Piece)size*=1.5f;
        if(scene1Glasses)size=Mathf.Min(width*.73f,height*1.27f)*Mathf.Lerp(.88f,1,appear);
        Vector2 center=new Vector2(width*.5f,height*.5f);
        if(elapsed>=FlightStarts&&inventory)
        {
            float t=Mathf.SmoothStep(0,1,Mathf.Clamp01((elapsed-FlightStarts)/FlightSeconds));
            var destination=inventory.SlotScreenRect(displaySlot);var end=new Vector2(destination.center.x,height-destination.center.y);
            var control=center+new Vector2(width*.19f,-height*.12f);
            center=(1-t)*(1-t)*center+2*(1-t)*t*control+t*t*end;
            float padding=12*Mathf.Clamp(height/900f,.75f,1.15f);size=Mathf.Lerp(size,destination.width-padding,t);
        }
        return new Rect(center.x-size/2,center.y-size*9/32,size,size*9/16);
    }
    private void Update()
    {
        if(Time.frameCount>sketchOpenedFrame&&WaitingForSketchFlip&&Input.GetMouseButtonDown(0))FlipSketch();
    }
    public bool FlipSketch()
    {
        if(!WaitingForSketchFlip)return false;
        sketchBack=true;sketchBackElapsed=0;
        var material=model.GetComponent<Renderer>().sharedMaterial;
        material.mainTexture=sketchBackTexture;
        float aspect=sketchBackTexture.width/(float)sketchBackTexture.height;
        float height=Mathf.Min(2.35f,4.4f/aspect);
        model.localScale=new Vector3(height*aspect,height,1);
        previewCamera.Render();
        return true;
    }
    private void LateUpdate()
    {
        if(!running)return;
        // A shader-loading hitch must not skip an entire pickup flash.
        float delta=Mathf.Min(Time.unscaledDeltaTime,.1f);
        if(scene1Sketch&&!sketchReturning)
        {
            presentationTime=Mathf.Min(presentationTime+delta,FlightStarts-.001f);
            if(sketchBack)
            {
                sketchBackElapsed+=delta;
                if(sketchBackElapsed>=3f){sketchReturning=true;presentationTime=FlightStarts;}
            }
        }
        else presentationTime+=delta;
        if(Elapsed>=Duration){Complete();return;}
        float pulse=PulseAt(Elapsed);
        foreach(var material in materials)if(material.HasProperty("_Pulse"))material.SetFloat("_Pulse",pulse);
        if(rimMaterial)rimMaterial.SetFloat("_Strength",(scene1Piece||scene1Sketch)?0:scene1Glasses?.08f+pulse*.3f:.55f+pulse*1.6f);
        previewCamera.Render();
    }
    private void DrawOverlay(System.Action<Rect,Texture,Color> draw)
    {
        float time=Elapsed,pulse=PulseAt(time),appear=Mathf.SmoothStep(0,1,Mathf.Clamp01(time/AppearSeconds));
        float flight=Mathf.Clamp01((time-FlightStarts)/FlightSeconds);
        var rect=ItemRect;var center=rect.center;
        draw(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture,new Color(.015f,.005f,.04f,.22f*appear*(1-flight)));
        var glow=new Rect(center.x-rect.width*.53f,center.y-rect.height*.43f,rect.width*1.06f,rect.height*.86f);
        draw(glow,halo,new Color(.48f,.18f,1,(.24f+pulse*.42f)*appear*(1-flight)));
        draw(new Rect(glow.x+glow.width*.12f,glow.y,glow.width*.76f,glow.height),halo,new Color(.23f,.66f,1,(.15f+pulse*.25f)*appear*(1-flight)));
        float fade=(scene1Glasses||scene1Piece||scene1Film||scene1Sketch)&&time>=AppearSeconds&&time<AppearSeconds+3*PulseSeconds?.18f+.82f*pulse:1;
        draw(rect,modelTexture,new Color(1,1,1,appear*fade));
        for(int i=0;i<16;i++)
        {
            float angle=i*2.39996f;float radius=.25f+(i%4)*.055f;
            Vector2 point=center+new Vector2(Mathf.Cos(angle)*rect.width*radius,Mathf.Sin(angle)*rect.height*.30f-Mathf.Sin(time*.7f+i)*3);
            float size=(i%5==0?18:5)*Mathf.Clamp(Screen.height/900f,.75f,1.15f);
            float brightness=(.25f+pulse*.65f)*appear*(1-flight);
            draw(new Rect(point.x-size/2,point.y-size/2,size,size),star,new Color(.72f,.72f,1,brightness));
        }
    }
    private static void TintDraw(Rect rect,Texture texture,Color color){GUI.color=color;GUI.DrawTexture(rect,texture,ScaleMode.StretchToFill,true);}
    private static string IdentifierForItem(int id)
    {
        switch(id)
        {
            case 211:return GlassesIdentifier;
            case 666:return PieceIdentifier;
            case 212:return FilmIdentifier;
            case 98:return SketchIdentifier;
            case 0:return "A-012";  // alloy1
            case 1:return "A-009";  // alloy2
            case 4:return "AB-077"; // nail
            case 5:return "B-520";  // cidai1-4 / caidai1-4
            default:return null;
        }
    }
    private void DrawItemIdentifier()
    {
        string identifier=IdentifierForItem(itemId);
        if(string.IsNullOrEmpty(identifier))return;
        float appear=Mathf.SmoothStep(0,1,Mathf.Clamp01(Elapsed/AppearSeconds));
        float flight=Mathf.Clamp01((Elapsed-FlightStarts)/FlightSeconds);
        float alpha=appear*(1-flight);
        if(alpha<=.001f)return;
        float scale=Mathf.Clamp(Screen.height/900f,.75f,1.25f);
        var itemRect=ItemRect;
        var labelRect=new Rect(itemRect.x,itemRect.yMax+8f*scale,itemRect.width,58f*scale);
        if(scene1Piece)labelRect.y=Mathf.Min(labelRect.y,Screen.height-66f*scale);
        if(scene1Glasses)labelRect=new Rect(0,Screen.height*.925f-Screen.height*.045f,Screen.width,Screen.height*.09f);
        var style=new GUIStyle(GUI.skin.label)
        {
            alignment=TextAnchor.MiddleCenter,
            fontSize=scene1Glasses?Mathf.Max(12,Mathf.RoundToInt(Screen.height*.055f)):Mathf.RoundToInt(38f*scale),
            fontStyle=FontStyle.Bold
        };
        var shadowRect=new Rect(labelRect.x+2f*scale,labelRect.y+3f*scale,labelRect.width,labelRect.height);
        style.normal.textColor=new Color(.32f,.12f,.8f,.8f*alpha);
        GUI.Label(shadowRect,identifier,style);
        style.normal.textColor=new Color(.94f,.92f,1f,alpha);
        GUI.Label(labelRect,identifier,style);
    }
#if UNITY_EDITOR
    public void DrawPreviewTo(RenderTexture destination)
    {
        var previous=RenderTexture.active;RenderTexture.active=destination;GL.PushMatrix();
        try
        {
            GL.LoadPixelMatrix(0,Screen.width,Screen.height,0);
            DrawOverlay((rect,texture,color)=>Graphics.DrawTexture(rect,texture,new Rect(0,0,1,1),0,0,0,0,color*.5f));
        }
        finally{GL.PopMatrix();RenderTexture.active=previous;}
    }
#endif
    private void OnGUI()
    {
        if(!running||!modelTexture)return;
        int depth=GUI.depth;Color color=GUI.color;GUI.depth=-400;DrawOverlay(TintDraw);DrawItemIdentifier();GUI.color=color;GUI.depth=depth;
    }
    private void Complete()
    {
        if(committed)return;
        committed=true;running=false;
        if(icon)
        {
            if(inventory&&((scene1Sketch&&sketchReview&&inventory.VisibleIcon(3))?false:scene1Sketch?inventory.StoreScene1Sketch(icon):scene1Film?inventory.StoreScene1Film(icon):scene1Piece?inventory.StoreScene1Piece(icon):scene1Glasses?inventory.StoreScene1Glasses(icon):inventory.StoreAlloyInSlot(itemId,icon)))
            {
                if(itemId==5)inventory.RecordTapeSource(itemSource);
                icon=null;
            }
            else Destroy(icon);
        }
        if(current==this)current=null;
        Destroy(gameObject);
    }
    private void OnDisable(){if(running)Complete();}
    private void OnDestroy()
    {
        if(current==this)current=null;
        if(previewRoot)previewRoot.SetActive(false);
        if(modelTexture){modelTexture.Release();Destroy(modelTexture);}
        foreach(var material in materials)if(material)Destroy(material);
        if(rimMaterial)Destroy(rimMaterial);if(halo)Destroy(halo);if(star)Destroy(star);if(icon)Destroy(icon);
    }
}
