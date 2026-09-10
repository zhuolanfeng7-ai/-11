using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Keeps the surveillance camera separate from the player's camera and sends its
/// output exclusively to the upper-right RawImage.
/// Attach to the stable SurveillanceMonitorSystem parent.
/// </summary>
public sealed class SurveillanceMonitorController : MonoBehaviour
{
    [SerializeField] private Camera surveillanceCamera;
    [SerializeField] private RawImage monitorImage;
    [SerializeField, Min(64)] private int textureResolution = 512;
    [SerializeField] private bool startVisible = true;

    private RenderTexture monitorTexture;
    private Canvas monitorCanvas;

    private void Awake()
    {
        if (surveillanceCamera == null) surveillanceCamera = GetComponentInChildren<Camera>(true);
        if (monitorImage == null) monitorImage = GetComponentInChildren<RawImage>(true);
        monitorCanvas = monitorImage != null ? monitorImage.GetComponentInParent<Canvas>() : null;
        if (surveillanceCamera == null || monitorImage == null || monitorCanvas == null)
        {
            Debug.LogError("SurveillanceMonitor: Camera or MonitorImage is missing.", this);
            enabled = false;
            return;
        }

        // A surveillance camera must never draw directly over the player's view.
        // Its only destination is the monitor RenderTexture.
        AudioListener surveillanceListener = surveillanceCamera.GetComponent<AudioListener>();
        if (surveillanceListener != null) surveillanceListener.enabled = false;

        monitorTexture = new RenderTexture(textureResolution, textureResolution, 16, RenderTextureFormat.ARGB32)
        {
            name = "Surveillance Monitor Render Texture"
        };
        monitorTexture.Create();
        surveillanceCamera.targetTexture = monitorTexture;
        monitorImage.texture = monitorTexture;
        monitorCanvas.gameObject.SetActive(startVisible);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && monitorCanvas != null)
            monitorCanvas.gameObject.SetActive(!monitorCanvas.gameObject.activeSelf);
    }

    private void OnDestroy()
    {
        if (surveillanceCamera != null && surveillanceCamera.targetTexture == monitorTexture)
            surveillanceCamera.targetTexture = null;
        if (monitorTexture == null) return;
        monitorTexture.Release();
        Destroy(monitorTexture);
    }
}
