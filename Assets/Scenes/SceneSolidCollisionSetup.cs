using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Completes solid collision for authored scene geometry without replacing interaction triggers.</summary>
public static class SceneSolidCollisionSetup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset(){SceneManager.sceneLoaded-=Apply;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register(){SceneManager.sceneLoaded-=Apply;SceneManager.sceneLoaded+=Apply;}
    static void Apply(Scene scene,LoadSceneMode mode)
    {
        string name=scene.name.Replace(" ","").ToLowerInvariant();
        if(name!="scene1"&&name!="scene2"&&name!="scene3")return;
        int added=0,existing=0;
        foreach(var root in scene.GetRootGameObjects())
        foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if(!(renderer is MeshRenderer)&&!(renderer is SkinnedMeshRenderer))continue;
            if(renderer.GetComponentInParent<Canvas>()||renderer.GetComponentInParent<Camera>()||renderer.GetComponentInParent<FirstPersonPlayer>()||renderer.GetComponentInParent<Robot3FirstPerson>())continue;
            var meshFilter=renderer.GetComponent<MeshFilter>();
            if(renderer is MeshRenderer&&(!meshFilter||!meshFilter.sharedMesh||meshFilter.sharedMesh.vertexCount==0))continue;
            if(HasSolidCoverage(renderer)){existing++;continue;}
            var body=renderer.GetComponentInParent<Rigidbody>();
            if(meshFilter&&meshFilter.sharedMesh&&body==null)
            {
                var collider=renderer.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh=meshFilter.sharedMesh;
                collider.convex=false;collider.isTrigger=false;
                collider.enabled=renderer.enabled;
            }
            else
            {
                // Moving props need a stable primitive rather than a non-convex rigidbody mesh.
                Bounds localBounds=renderer is SkinnedMeshRenderer skin?skin.localBounds:meshFilter.sharedMesh.bounds;
                var collider=renderer.gameObject.AddComponent<BoxCollider>();
                collider.center=localBounds.center;
                Vector3 size=localBounds.size;
                collider.size=new Vector3(Mathf.Max(.001f,size.x),Mathf.Max(.001f,size.y),Mathf.Max(.001f,size.z));
                collider.isTrigger=false;collider.enabled=renderer.enabled;
            }
            added++;
        }
        foreach(var root in scene.GetRootGameObjects())
        foreach(var player in root.GetComponentsInChildren<FirstPersonPlayer>(true))
        {
            var body=player.GetComponent<Rigidbody>();
            if(body){body.detectCollisions=true;body.collisionDetectionMode=body.isKinematic?CollisionDetectionMode.ContinuousSpeculative:CollisionDetectionMode.ContinuousDynamic;}
        }
        Physics.SyncTransforms();
        Debug.Log("[Scene collision] "+scene.name+": added "+added+" solid colliders; retained "+existing+" existing collider groups. Interaction triggers preserved.");
    }
    static bool HasSolidCoverage(Renderer renderer)
    {
        Bounds b=renderer.bounds;
        for(Transform t=renderer.transform;t!=null;t=t.parent)
        {
            foreach(var c in t.GetComponents<Collider>())
            {
                if(c.isTrigger||!c.enabled)continue;
                if(c is MeshCollider mesh&&!mesh.sharedMesh)continue;
                if(t==renderer.transform)return true;
                if(c.bounds.Contains(b.min)&&c.bounds.Contains(b.max))return true;
            }
        }
        return false;
    }
}
