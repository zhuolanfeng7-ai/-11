using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-225)]
public sealed class Scene2NailInstallation : MonoBehaviour
{
    private static Scene2NailInstallation active;
    private static int consumedFrame = -1;

    private readonly List<GameObject> previews = new List<GameObject>();
    private readonly List<GameObject> installedNails = new List<GameObject>();
    private readonly Vector3[] normalizedHoles =
    {
        // Every pink-circled ferrule in the supplied overview, ordered back-to-front.
        // The large hole marked with a pink X on the far right is deliberately excluded.
        new Vector3(.104f, 0f, .877f), // upper-left raised bracket
        new Vector3(.685f, 0f, .618f), // ferrule beside the large upper cylinder
        new Vector3(.974f, 0f, .489f), // right side rail
        new Vector3(.232f, 0f, .411f), // left middle ferrule, to the right of the long slot
        new Vector3(.258f, 0f, .211f), // lower-left ferrule, to the right of the long slot
        new Vector3(.531f, 0f, .167f), // lower centre plate
        new Vector3(.877f, 0f, .167f)  // lower-right plate
    };

    private Transform machine;
    private BoxCollider socket;
    private ImportantItemCollectionUI inventory;
    private Mesh originalNailMesh;
    private Material[] originalNailMaterials;
    private Quaternion originalNailRotation;
    private Vector3 originalNailOffset;
    private float originalNailScale;
    private Material previewMaterial;
    private Vector3[] holes;
    private bool dragging;
    private bool installing;
    private bool installed;
    private float nextSearch;

    public static bool IsDraggingSlot(int slot) => active && active.dragging && slot == 2;
    public static bool BlocksWorldInput => (active && (active.dragging || active.installing)) || consumedFrame == Time.frameCount;
    public bool IsInstalled => installed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics(){active=null;consumedFrame=-1;}

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= Bind;
        SceneManager.sceneLoaded += Bind;
        Bind(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void Bind(Scene scene, LoadSceneMode mode)
    {
        if(!scene.IsValid() || !string.Equals(scene.name.Replace(" ",""),"scene2",StringComparison.OrdinalIgnoreCase)) return;
        if(scene.GetRootGameObjects().Any(root=>root.GetComponent<Scene2NailInstallation>())) return;
        var owner=new GameObject("XF Marked-Hole Nail Repair Installation");
        SceneManager.MoveGameObjectToScene(owner,scene);
        owner.AddComponent<Scene2NailInstallation>();
    }

    private void Configure()
    {
        var all=gameObject.scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Transform>(true)).ToArray();
        machine=all.FirstOrDefault(item=>item.name.Equals("xf",StringComparison.OrdinalIgnoreCase));
        if(!machine)return;
        var source=all.FirstOrDefault(item=>item.name.Equals("nail",StringComparison.OrdinalIgnoreCase));
        var sourceMesh=source?source.GetComponentInChildren<MeshFilter>(true):null;
        var sourceRenderer=sourceMesh?sourceMesh.GetComponent<Renderer>():null;
        if(!sourceMesh||!sourceMesh.sharedMesh||!sourceRenderer)return;
        originalNailMesh=sourceMesh.sharedMesh;originalNailMaterials=sourceRenderer.sharedMaterials;
        ConfigureOriginalNailFit();
        var renderer=machine.GetComponentInChildren<Renderer>();
        var filter=machine.GetComponentInChildren<MeshFilter>();
        if(!renderer||!filter||!filter.sharedMesh)return;
        var bounds=renderer.bounds;
        foreach(var other in machine.GetComponentsInChildren<Renderer>(true))bounds.Encapsulate(other.bounds);
        holes=new Vector3[normalizedHoles.Length];

        // Raycast against the actual recorder mesh so every fastener sits on its own local surface height.
        var probe=new GameObject("XF Nail Surface Probe"){hideFlags=HideFlags.HideAndDontSave};
        probe.transform.SetPositionAndRotation(filter.transform.position,filter.transform.rotation);
        probe.transform.localScale=filter.transform.lossyScale;
        var meshCollider=probe.AddComponent<MeshCollider>();
        meshCollider.sharedMesh=filter.sharedMesh;
        Physics.SyncTransforms();
        for(int i=0;i<holes.Length;i++)
        {
            float x=Mathf.Lerp(bounds.min.x,bounds.max.x,normalizedHoles[i].x);
            float z=Mathf.Lerp(bounds.min.z,bounds.max.z,normalizedHoles[i].z);
            holes[i]=FindMetalHoleSeat(meshCollider,bounds,x,z);
        }
        Destroy(probe);

        // The player clicks the recorder (xf/SF), not an inventory slot or one tiny hole.
        // The precise marked-hole coordinates are still used only by the installation animation.
        var anchor=new GameObject("XF click target for marked metal holes");
        anchor.transform.SetPositionAndRotation(bounds.center,Quaternion.identity);
        anchor.transform.SetParent(machine,true);
        socket=anchor.AddComponent<BoxCollider>();
        socket.isTrigger=true;
        socket.size=Vector3.Scale(bounds.size+new Vector3(.8f,.8f,.8f),new Vector3(1f/anchor.transform.lossyScale.x,1f/anchor.transform.lossyScale.y,1f/anchor.transform.lossyScale.z));

        var standard=Shader.Find("Standard");
        previewMaterial=new Material(standard){hideFlags=HideFlags.HideAndDontSave,color=new Color(.32f,1f,.63f,.42f)};
        previewMaterial.SetFloat("_Mode",3);previewMaterial.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);previewMaterial.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);previewMaterial.SetInt("_ZWrite",0);previewMaterial.DisableKeyword("_ALPHATEST_ON");previewMaterial.EnableKeyword("_ALPHABLEND_ON");previewMaterial.renderQueue=3000;
        for(int i=0;i<holes.Length;i++){var visual=MakeNail("Nail fit preview "+(i+1),previewMaterial);SetNailPose(visual.transform,holes[i]);visual.SetActive(false);previews.Add(visual);}
    }

    private static Vector3 FindMetalHoleSeat(MeshCollider meshCollider,Bounds bounds,float x,float z)
    {
        // The centre ray falls into the dark cavity. Sample the surrounding metal ferrule instead,
        // then place the fastener at that lip height so its shaft enters the cavity and its head sits flush.
        float lipHeight=float.MinValue;
        const int samples=16;
        foreach(float radius in new[]{.42f,.58f,.74f})
        {
            for(int sample=0;sample<samples;sample++)
            {
                float angle=sample*Mathf.PI*2f/samples;
                var origin=new Vector3(x+Mathf.Cos(angle)*radius,bounds.max.y+5f,z+Mathf.Sin(angle)*radius);
                RaycastHit hit;
                if(meshCollider.Raycast(new Ray(origin,Vector3.down),out hit,bounds.size.y+10f))lipHeight=Mathf.Max(lipHeight,hit.point.y);
            }
            if(lipHeight>float.MinValue)break;
        }
        if(lipHeight==float.MinValue)
        {
            RaycastHit centreHit;
            if(meshCollider.Raycast(new Ray(new Vector3(x,bounds.max.y+5f,z),Vector3.down),out centreHit,bounds.size.y+10f))lipHeight=centreHit.point.y;
            else lipHeight=bounds.max.y;
        }
        return new Vector3(x,lipHeight+.055f,z);
    }

    private GameObject MakeNail(string label,Material material)
    {
        var root=new GameObject(label);root.transform.SetParent(machine,true);
        var model=new GameObject("Original nail model");model.transform.SetParent(root.transform,false);
        model.AddComponent<MeshFilter>().sharedMesh=originalNailMesh;
        var renderer=model.AddComponent<MeshRenderer>();renderer.sharedMaterials=material?Enumerable.Repeat(material,originalNailMesh.subMeshCount).ToArray():originalNailMaterials;
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
        model.transform.localRotation=originalNailRotation;model.transform.localScale=Vector3.one*originalNailScale;model.transform.localPosition=originalNailOffset;
        return root;
    }
    private void ConfigureOriginalNailFit()
    {
        // The original screw is authored diagonally; use its real shaft axis,
        // rather than stretching it to the dimensions of a primitive cylinder.
        var axis=new Vector3(.71647052f,-.00599175f,.69759163f).normalized;
        originalNailRotation=Quaternion.FromToRotation(axis,Vector3.up);
        originalNailScale=1.3125f/.36902582f;
        var seat=new Vector3(.29413848f,-.00555591f,.75379814f);
        originalNailOffset=-(originalNailRotation*seat)*originalNailScale;
    }

    private static void SetNailPose(Transform nail,Vector3 position){nail.SetPositionAndRotation(position,Quaternion.identity);}
    private ImportantItemCollectionUI Inventory(){if(!inventory)inventory=FindObjectOfType<ImportantItemCollectionUI>();return inventory;}
    private bool HasNail(){var ui=Inventory();return ui&&ui.HasNail;}

    private void Update()
    {
        if(!socket){if(Time.unscaledTime>=nextSearch){nextSearch=Time.unscaledTime+.5f;Configure();}return;}
        if(installing||installed)return;
        var camera=Camera.main;var pointer=(Vector2)Input.mousePosition;
        if(dragging)
        {
            bool valid=CanInstall(camera,pointer);foreach(var preview in previews)preview.SetActive(valid);
            if(Input.GetMouseButtonUp(0))TryDrop(camera,pointer);else if(Input.GetKeyDown(KeyCode.Escape)||!Application.isFocused)CancelDrag();
            return;
        }
        if(!Application.isFocused||EvidenceInspect.BlocksWorldInput||Scene2PlateInstallation.BlocksWorldInput||!Input.GetMouseButtonDown(0))return;
        var ui=Inventory();if(!ui||!ui.HasNail)return;
        if(ui.SlotScreenRect(2).Contains(pointer)){BeginDrag();return;}
        for(int i=0;i<ui.VisibleSlotCount;i++)if(ui.SlotScreenRect(i).Contains(pointer))return;
        TryInstall(camera,Cursor.lockState==CursorLockMode.Locked&&camera?camera.pixelRect.center:pointer);
    }

    private void LateUpdate()
    {
        if(!socket||dragging||installing||installed)return;
        var camera=Camera.main;var pointer=Cursor.lockState==CursorLockMode.Locked&&camera?camera.pixelRect.center:(Vector2)Input.mousePosition;
        bool visible=!EvidenceInspect.BlocksWorldInput&&!Scene2PlateInstallation.BlocksWorldInput&&CanInstall(camera,pointer);
        foreach(var preview in previews)preview.SetActive(visible);
    }

    private void BeginDrag(){if(!HasNail())return;active=this;dragging=true;consumedFrame=Time.frameCount;Cursor.visible=true;}
    private bool CanInstall(Camera camera,Vector2 pointer)
    {
        if(!camera||!socket||installed||installing||!HasNail()||!camera.pixelRect.Contains(pointer))return false;
        RaycastHit hit;if(!socket.Raycast(camera.ScreenPointToRay(pointer),out hit,1000f))return false;
        return Vector3.Distance(camera.transform.position,hit.point)<=160f;
    }
    private void TryDrop(Camera camera,Vector2 pointer){bool valid=CanInstall(camera,pointer);dragging=false;foreach(var preview in previews)preview.SetActive(false);if(valid)StartInstall();else if(active==this)active=null;consumedFrame=Time.frameCount;}
    private void TryInstall(Camera camera,Vector2 pointer){if(CanInstall(camera,pointer))StartInstall();}
    private void StartInstall(){if(installing||installed||!HasNail())return;active=this;installing=true;foreach(var preview in previews)preview.SetActive(false);StartCoroutine(InstallMarkedNails());}

    private IEnumerator InstallMarkedNails()
    {
        for(int i=0;i<holes.Length;i++)
        {
            var nail=MakeNail("Installed AB-077 fastener "+(i+1),null);installedNails.Add(nail);
            Vector3 end=holes[i],start=end+Vector3.up*5f;nail.transform.position=start;
            for(float t=0;t<.48f;t+=Time.unscaledDeltaTime){float f=Mathf.SmoothStep(0f,1f,t/.48f);nail.transform.position=Vector3.Lerp(start,end,f);yield return null;}
            SetNailPose(nail.transform,end);
            var sound=Resources.Load<AudioClip>("PropFalls/P1Land");if(sound)Scene2AudioPlayback.PlayAtPoint(sound,end,.16f);
            yield return new WaitForSecondsRealtime(.12f);
        }
        if(Inventory()&&inventory.ConsumeNail())
        {
            installed=true;socket.enabled=false;
            var guidance=FindObjectOfType<Scene2XfGuidance>();if(guidance)guidance.SetRepaired();
        }
        else{foreach(var nail in installedNails)if(nail)Destroy(nail);installedNails.Clear();}
        installing=false;consumedFrame=Time.frameCount;if(active==this)active=null;
    }

    private void CancelDrag(){dragging=false;foreach(var preview in previews)if(preview)preview.SetActive(false);consumedFrame=Time.frameCount;if(active==this)active=null;}

    private void OnGUI()
    {
        if(dragging&&Inventory()&&inventory.NailIcon)
        {
            var pointer=Event.current.mousePosition;float size=72f*Mathf.Clamp(Screen.height/900f,.75f,1.15f);int depth=GUI.depth;GUI.depth=-100;GUI.DrawTexture(new Rect(pointer.x-size/2,pointer.y-size/2,size,size),inventory.NailIcon,ScaleMode.ScaleToFit,true);GUI.depth=depth;
        }
    }

    private void OnDisable(){StopAllCoroutines();CancelDrag();}
    private void OnDestroy(){if(socket)Destroy(socket.gameObject);foreach(var item in previews)if(item)Destroy(item);foreach(var item in installedNails)if(item)Destroy(item);if(previewMaterial)Destroy(previewMaterial);}
}
