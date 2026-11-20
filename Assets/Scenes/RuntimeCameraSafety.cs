using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Restores an authored scene camera only when Display 1 has no active camera.
/// It never creates or replaces cameras and therefore preserves scene framing.
/// </summary>
[DefaultExecutionOrder(-32000)]
public sealed class RuntimeCameraSafety : MonoBehaviour
{
    private static RuntimeCameraSafety instance;
    private float nextCheck;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (instance) return;
        var owner = new GameObject("Runtime Camera Safety")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        DontDestroyOnLoad(owner);
        instance = owner.AddComponent<RuntimeCameraSafety>();
    }

    private void Awake()
    {
        if (instance && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
        RestoreCameraIfNeeded();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        nextCheck = 0f;
        RestoreCameraIfNeeded();
    }

    private void LateUpdate()
    {
        if (Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + .5f;
        RestoreCameraIfNeeded();
    }

    private static void RestoreCameraIfNeeded()
    {
        if (HasDisplayOneCamera()) return;

        Scene activeScene = SceneManager.GetActiveScene();
        Camera candidate = FindAuthoredCamera(activeScene);
        if (!candidate) return;

        ActivateHierarchy(candidate.transform);
        candidate.targetDisplay = 0;
        candidate.targetTexture = null;
        candidate.enabled = true;
        Debug.LogWarning("[Camera Safety] Restored scene camera '" + candidate.name +
                         "' in scene '" + activeScene.name + "'.", candidate);
    }

    private static bool HasDisplayOneCamera()
    {
        foreach (Camera camera in Camera.allCameras)
            if (camera && camera.enabled && camera.gameObject.activeInHierarchy &&
                camera.targetDisplay == 0 && camera.targetTexture == null)
                return true;
        return false;
    }

    private static Camera FindAuthoredCamera(Scene activeScene)
    {
        Camera fallback = null;
        foreach (Camera camera in Resources.FindObjectsOfTypeAll<Camera>())
        {
            if (!camera || !camera.gameObject.scene.IsValid() ||
                !camera.gameObject.scene.isLoaded || camera.gameObject.scene != activeScene)
                continue;
            if ((camera.hideFlags & (HideFlags.HideAndDontSave | HideFlags.DontSave)) != 0)
                continue;
            if (camera.targetTexture != null || IsUtilityCamera(camera.name))
                continue;

            if (camera.CompareTag("MainCamera") ||
                string.Equals(camera.name, "Camera", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(camera.name, "Main Camera", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(camera.name, "Robot3 Eye Camera", StringComparison.OrdinalIgnoreCase))
                return camera;

            if (!fallback) fallback = camera;
        }
        return fallback;
    }

    private static bool IsUtilityCamera(string cameraName)
    {
        string value = cameraName.ToLowerInvariant();
        return value.Contains("preview") || value.Contains("overlay") ||
               value.Contains("reflection") || value.Contains("monitor") ||
               value.Contains("surveillance");
    }

    private static void ActivateHierarchy(Transform target)
    {
        if (!target) return;
        ActivateHierarchy(target.parent);
        if (!target.gameObject.activeSelf) target.gameObject.SetActive(true);
    }
}
