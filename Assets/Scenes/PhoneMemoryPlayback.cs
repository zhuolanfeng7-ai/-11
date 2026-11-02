using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class PhoneMemoryPlayback : MonoBehaviour
{
    public const string RecordingResource = "PhoneAudio/PastSelfMemory";
    private AudioSource output;
    public AudioSource Output => output;
    public float InteractionDistance = 160f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= Bind;
        SceneManager.sceneLoaded += Bind;
        Bind(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void Bind(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() || !string.Equals(scene.name.Replace(" ", ""), "scene2", StringComparison.OrdinalIgnoreCase)) return;
        foreach (var root in scene.GetRootGameObjects())
        foreach (var target in root.GetComponentsInChildren<Transform>(true))
            if (string.Equals(target.name.Trim(), "phone", StringComparison.OrdinalIgnoreCase)
                && !target.GetComponent<PhoneMemoryPlayback>()) target.gameObject.AddComponent<PhoneMemoryPlayback>();
    }

    private void Awake()
    {
        output = gameObject.AddComponent<AudioSource>();
        output.playOnAwake = false;
        output.loop = false;
        output.spatialBlend = 0;
        output.dopplerLevel = 0;
        output.volume = .85f;
        output.clip = Resources.Load<AudioClip>(RecordingResource);
        // Fit clickable colliders to the existing model without moving the phone.
        foreach (var mesh in GetComponentsInChildren<MeshFilter>(true))
        {
            if (!mesh.sharedMesh || mesh.GetComponents<Collider>().Any(c => c.enabled && !c.isTrigger)) continue;
            var box = mesh.gameObject.AddComponent<BoxCollider>();
            box.center = mesh.sharedMesh.bounds.center;
            box.size = mesh.sharedMesh.bounds.size;
        }
    }

    private void OnEnable(){Scene2AudioPlayback.SoundStarting += StopForOtherSound;}
    private void StopForOtherSound(AudioSource source)
    {
        if (source == output || !output || !output.isPlaying) return;
        output.Stop();
    }

    private void LateUpdate()
    {
        if (!Application.isFocused || EvidenceInspect.BlocksWorldInput || !Input.GetMouseButtonDown(0)) return;
        var camera = Camera.main;
        if (camera) TryClick(camera, Cursor.lockState == CursorLockMode.Locked ? camera.pixelRect.center : (Vector2)Input.mousePosition);
    }

    public bool TryClick(Camera camera, Vector2 point)
    {
        if (!isActiveAndEnabled || !camera || !camera.pixelRect.Contains(point) || EvidenceInspect.BlocksWorldInput) return false;
        foreach (var hit in Physics.RaycastAll(camera.ScreenPointToRay(point), camera.farClipPlane,
                     ~((1 << 30) | (1 << 31)), QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
        {
            if (hit.collider.GetComponentInParent<FirstPersonPlayer>()) continue;
            return (hit.transform == transform || hit.transform.IsChildOf(transform))
                && Vector3.Distance(camera.transform.position, hit.point) <= InteractionDistance && TryPlay();
        }
        return false;
    }

    public bool TryPlay()
    {
        if (!isActiveAndEnabled || !output || !output.clip) return false;
        output.Stop();
        Scene2AudioPlayback.Play(output);
        return true;
    }

    private void OnDisable()
    {
        Scene2AudioPlayback.SoundStarting -= StopForOtherSound;
        if (output) output.Stop();
    }
}
