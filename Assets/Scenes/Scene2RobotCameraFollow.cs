using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// The existing script identity is retained so Scene2 keeps its serialized camera reference.
[DefaultExecutionOrder(10000)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class Scene2RobotCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Transform eyeAnchor;
    [SerializeField, Range(30f, 95f)] private float normalFieldOfView = 60f;
    [SerializeField] private bool hideSelfInEyeView = true;
    private Renderer[] robotRenderers;
    private bool[] previousRenderingState;
    private bool hiddenForRender;
    private Quaternion neutralEyeRotation;
    private bool bound;
    public Transform Target => target;
    public Transform EyeAnchor => eyeAnchor;
    public bool IsEyeView => bound && target != null && eyeAnchor != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        AttachForActiveScene();
    }
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => AttachForActiveScene();

    public static void AttachForActiveScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name.Replace(" ", ""), "scene2", StringComparison.OrdinalIgnoreCase)) return;
        var transforms = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        Camera camera = transforms.Select(t => t.GetComponent<Camera>()).FirstOrDefault(c => c && c.CompareTag("MainCamera"));
        if (!camera) camera = transforms.Select(t => t.GetComponent<Camera>()).FirstOrDefault(c => c && c.enabled);
        if (!camera) { Debug.LogWarning("Scene2 eye view: an existing Main Camera is required."); return; }
        var existing = camera.GetComponent<Scene2RobotCameraFollow>();
        Transform robot = existing && existing.target && existing.target.gameObject.scene == scene ? existing.target : null;
        foreach (string name in new[] { "jiqiren1", "JIQI", "tripo_convert_1cc7c5b6-9aed-4f94-99ec-e97186673414" })
        {
            if (robot) break;
            var matches = transforms.Where(t => string.Equals(t.name, name, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length > 1) { Debug.LogError("Scene2 eye view: duplicate target name " + name); return; }
            if (matches.Length == 1) robot = matches[0];
        }
        if (!robot) { Debug.LogWarning("Scene2 eye view: JIQI was not found."); return; }
        if (!existing) existing = camera.gameObject.AddComponent<Scene2RobotCameraFollow>();
        existing.enabled = true;
        existing.Bind(robot);
    }

    private void OnEnable()
    {
        if (Application.isPlaying && target) Bind(target);
    }

    public void Bind(Transform robot)
    {
        RestoreSelfRendering();
        target = robot;
        robotRenderers = robot.GetComponentsInChildren<Renderer>(true);
        previousRenderingState = new bool[robotRenderers.Length];
        if (!eyeAnchor || !eyeAnchor.IsChildOf(robot)) eyeAnchor = robot.Find("JIQI_Eye");
        if (!eyeAnchor)
        {
            var bounds = BodyBounds();
            var heading = Quaternion.Euler(0f, robot.eulerAngles.y, 0f) * Vector3.forward;
            eyeAnchor = new GameObject("JIQI_Eye").transform;
            eyeAnchor.SetParent(robot, false);
            eyeAnchor.SetPositionAndRotation(
                new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * 0.91f, bounds.center.z) + heading * bounds.size.y * 0.08f,
                Quaternion.LookRotation(heading, Vector3.up));
        }
        neutralEyeRotation = eyeAnchor.localRotation;
        // Parenting is explicit; using world-space pose also tolerates controllers
        // that temporarily detach the camera from an imported character hierarchy.
        transform.SetParent(eyeAnchor, true);
        GetComponent<Camera>().fieldOfView = normalFieldOfView;
        GetComponent<Camera>().nearClipPlane = Mathf.Clamp(BodyBounds().size.y * 0.002f, 0.01f, 0.3f);
        bound = true;
        SnapToTarget();
    }

    public void SetEyePitch(float pitch)
    {
        if (eyeAnchor) eyeAnchor.localRotation = neutralEyeRotation * Quaternion.Euler(pitch, 0f, 0f);
    }

    public void SnapToTarget()
    {
        if (!target || !eyeAnchor) return;
        transform.SetPositionAndRotation(eyeAnchor.position, eyeAnchor.rotation);
    }

    private Bounds BodyBounds()
    {
        if (robotRenderers == null || robotRenderers.Length == 0)
            return new Bounds(target.position + Vector3.up, new Vector3(1f, 2f, 1f));
        Bounds bounds = robotRenderers[0].bounds;
        for (int i = 1; i < robotRenderers.Length; i++) if (robotRenderers[i]) bounds.Encapsulate(robotRenderers[i].bounds);
        return bounds;
    }

    private void LateUpdate() => SnapToTarget();

    // Hide the single-piece head/body only while this camera renders. Other
    // cameras retain the robot, and its colliders and gameplay state are untouched.
    private void OnPreCull()
    {
        if (!hideSelfInEyeView || robotRenderers == null || hiddenForRender) return;
        for (int i = 0; i < robotRenderers.Length; i++)
        {
            if (!robotRenderers[i]) continue;
            previousRenderingState[i] = robotRenderers[i].forceRenderingOff;
            robotRenderers[i].forceRenderingOff = true;
        }
        hiddenForRender = true;
    }
    private void OnPostRender() => RestoreSelfRendering();
    private void OnDisable() => RestoreSelfRendering();
    private void RestoreSelfRendering()
    {
        if (!hiddenForRender || robotRenderers == null) return;
        for (int i = 0; i < robotRenderers.Length; i++)
            if (robotRenderers[i]) robotRenderers[i].forceRenderingOff = previousRenderingState[i];
        hiddenForRender = false;
    }
}

