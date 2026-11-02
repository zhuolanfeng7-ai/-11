using UnityEngine;

[DefaultExecutionOrder(-90)]
[DisallowMultipleComponent]
public sealed class AlloyPickup : MonoBehaviour
{
    [SerializeField] private TubeRewardReveal revealOwner;
    [SerializeField] private MusicBoxRewardLift crankReward;
    [SerializeField,Range(0,5)] private int inventorySlot;
    [SerializeField] private bool standalonePickup;
    [SerializeField] private Transform pickupRoot;
    [SerializeField] private bool woodTool;
    private bool collected;
    public Transform PickupRoot => pickupRoot?pickupRoot:transform;
    public bool IsCollected => collected;
    public void Configure(TubeRewardReveal owner){revealOwner=owner;crankReward=null;inventorySlot=0;}
    public void ConfigureMusicBox(MusicBoxRewardLift owner){crankReward=owner;revealOwner=null;inventorySlot=1;}
    public void ConfigureKey(MusicBoxRewardLift owner){crankReward=owner;revealOwner=null;standalonePickup=false;inventorySlot=3;}
    public void ConfigureCp1(){standalonePickup=true;revealOwner=null;crankReward=null;inventorySlot=2;}
    public void ConfigureNail(){standalonePickup=true;revealOwner=null;crankReward=null;inventorySlot=4;}
    public void ConfigureWood(Transform root){standalonePickup=true;revealOwner=null;crankReward=null;inventorySlot=5;pickupRoot=root;woodTool=true;}
    public void ConfigureTape(Transform root){standalonePickup=true;revealOwner=null;crankReward=null;inventorySlot=5;pickupRoot=root;woodTool=false;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeNailPickup(){UnityEngine.SceneManagement.SceneManager.sceneLoaded-=BindNail;UnityEngine.SceneManagement.SceneManager.sceneLoaded+=BindNail;BindNail(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),UnityEngine.SceneManagement.LoadSceneMode.Single);}
    private static void BindNail(UnityEngine.SceneManagement.Scene scene,UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if(!scene.IsValid()||!string.Equals(scene.name.Replace(" ",""),"scene2",System.StringComparison.OrdinalIgnoreCase))return;
        foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true)){
            bool wood=string.Equals(t.name.Trim(),"wood",System.StringComparison.OrdinalIgnoreCase);
            if(!wood&&!string.Equals(t.name.Trim(),"nail",System.StringComparison.OrdinalIgnoreCase))continue;
            var filter=t.GetComponent<MeshFilter>();if(!filter&&wood)filter=t.GetComponentInChildren<MeshFilter>(true);if(!filter||!filter.sharedMesh)continue;
            var target=filter.transform;
            if(!target.GetComponent<Collider>())target.gameObject.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;
            var hover=target.GetComponent<HoverScanHighlight>();if(!hover)hover=target.gameObject.AddComponent<HoverScanHighlight>();hover.ConfigureScene2();hover.Configure(6,100);
            var pickup=target.GetComponent<AlloyPickup>();if(!pickup)pickup=target.gameObject.AddComponent<AlloyPickup>();if(wood)pickup.ConfigureWood(t);else pickup.ConfigureNail();
        }
    }
    private void Update()
    {
        if(EvidenceInspect.BlocksWorldInput)return;
        if(!Application.isFocused || !Input.GetMouseButtonDown(0))return;
        var camera=Camera.main;if(!camera)return;
        Vector2 pointer=Cursor.lockState==CursorLockMode.Locked ? camera.pixelRect.center : (Vector2)Input.mousePosition;
        TryPickupAtScreenPosition(camera,pointer);
    }
    public bool TryPickupAtScreenPosition(Camera camera,Vector2 pointer)
    {
        if(collected || !isActiveAndEnabled || !gameObject.activeInHierarchy)return false;
        bool ready=standalonePickup || (revealOwner?revealOwner.IsRevealed:crankReward&&crankReward.IsRaised);
        if(!ready)return false;
        var hover=GetComponent<HoverScanHighlight>();
        if(!hover || !hover.RefreshDeviceHover(camera,pointer))return false;
        var inventory=FindObjectOfType<ImportantItemCollectionUI>();
        if(!inventory)inventory=new GameObject("Important Item Collection UI").AddComponent<ImportantItemCollectionUI>();
        if(woodTool){if(!inventory.CollectWood())return false;collected=true;PickupRoot.gameObject.SetActive(false);return true;}
        if(inventorySlot==2||inventorySlot==3)
        {
            bool accepted=inventorySlot==2?inventory.CollectCp1():inventory.CollectScene2Key();
            if(!accepted)return false;
            collected=true;PickupRoot.gameObject.SetActive(false);return true;
        }
        Texture2D icon=CreateIcon();
        if(!icon)return false;
        if(!Scene2PickupPresentation.Begin(GetComponent<MeshFilter>(),GetComponent<Renderer>(),inventory,icon,inventorySlot)){Destroy(icon);return false;}
        collected=true;
        PickupRoot.gameObject.SetActive(false);
        return true;
    }
    private Texture2D CreateIcon()
    {
        var source=GetComponent<MeshFilter>();var original=GetComponent<MeshRenderer>();
        if(!source || !source.sharedMesh || !original)return null;
        const int layer=30;
        var preview=new GameObject("Alloy Inventory Preview");preview.layer=layer;
        preview.transform.SetPositionAndRotation(new Vector3(10000,10000,10000),transform.rotation);
        preview.transform.localScale=transform.lossyScale;
        preview.AddComponent<MeshFilter>().sharedMesh=source.sharedMesh;
        var renderer=preview.AddComponent<MeshRenderer>();renderer.sharedMaterials=original.sharedMaterials;
        var bounds=renderer.bounds;float size=Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);
        var cameraObject=new GameObject("Alloy Icon Camera");var camera=cameraObject.AddComponent<Camera>();
        camera.enabled=false;camera.cullingMask=1<<layer;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
        camera.orthographic=true;camera.orthographicSize=size*.68f;camera.aspect=1;camera.nearClipPlane=.01f;camera.farClipPlane=size*6+10;
        camera.transform.position=bounds.center+new Vector3(1,1,-1).normalized*(size*2+1);camera.transform.LookAt(bounds.center);
        var lightObject=new GameObject("Alloy Icon Light");var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.cullingMask=1<<layer;light.intensity=1.5f;light.shadows=LightShadows.None;light.transform.rotation=Quaternion.Euler(45,-30,0);
        var rt=new RenderTexture(192,192,24,RenderTextureFormat.ARGB32);
        var previous=RenderTexture.active;
        Texture2D icon=null;
        try{
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            icon=new Texture2D(192,192,TextureFormat.RGBA32,false){name=gameObject.name+" Inventory Icon"};
            icon.ReadPixels(new Rect(0,0,192,192),0,0);icon.Apply();
        }finally{
            RenderTexture.active=previous;camera.targetTexture=null;rt.Release();
            preview.SetActive(false);lightObject.SetActive(false);
            Destroy(rt);Destroy(cameraObject);Destroy(lightObject);Destroy(preview);
        }
        return icon;
    }
}
