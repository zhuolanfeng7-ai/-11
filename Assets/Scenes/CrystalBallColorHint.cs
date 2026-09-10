using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Restores a deliberately small amount of original colour to the CrystalBall
/// after the global monochrome effect has run.
/// </summary>
public sealed class CrystalBallColorHint : MonoBehaviour
{
    [SerializeField] private Transform crystalBallVisual;
    [SerializeField] private VisionBlurEffect visionEffect;
    [SerializeField, Range(0f, .5f)] private float colourHintStrength = .22f;

    private Camera viewCamera;
    private Camera overlayCamera;
    private Transform overlayRoot;
    private readonly List<Material> overlayMaterials = new List<Material>();

    private void Awake()
    {
        if (crystalBallVisual == null && transform.childCount > 0)
            crystalBallVisual = transform.GetChild(0);
        FirstPersonPlayer player = FindObjectOfType<FirstPersonPlayer>();
        viewCamera = player != null ? player.PlayerCamera : (Camera.main != null ? Camera.main : FindObjectOfType<Camera>());
        if (visionEffect == null && viewCamera != null) visionEffect = viewCamera.GetComponent<VisionBlurEffect>();
        BuildOverlay();
    }

    private void LateUpdate()
    {
        if (overlayCamera == null || viewCamera == null) return;
        overlayCamera.transform.SetPositionAndRotation(viewCamera.transform.position, viewCamera.transform.rotation);
        overlayCamera.fieldOfView = viewCamera.fieldOfView;
        overlayCamera.nearClipPlane = viewCamera.nearClipPlane;
        overlayCamera.farClipPlane = viewCamera.farClipPlane;
        overlayCamera.aspect = viewCamera.aspect;

        float strength = colourHintStrength * (visionEffect != null ? 1f - visionEffect.Saturation : 1f);
        bool showHint = strength > .002f;
        if (overlayRoot != null && overlayRoot.gameObject.activeSelf != showHint)
            overlayRoot.gameObject.SetActive(showHint);
        foreach (Material material in overlayMaterials)
            if (material != null) material.SetFloat("_HintAlpha", strength);
    }

    private void BuildOverlay()
    {
        if (crystalBallVisual == null || viewCamera == null) return;
        const int overlayLayer = 30;
        Shader hintShader = Shader.Find("Custom/CrystalBallColourHint");
        if (hintShader == null) return;

        overlayRoot = new GameObject("CrystalBall Subtle Colour Hint").transform;
        GameObject cameraObject = new GameObject("CrystalBall Colour Hint Camera");
        overlayCamera = cameraObject.AddComponent<Camera>();
        overlayCamera.clearFlags = CameraClearFlags.Nothing;
        overlayCamera.cullingMask = 1 << overlayLayer;
        overlayCamera.depth = viewCamera.depth + 9f;
        overlayCamera.allowHDR = false;
        overlayCamera.allowMSAA = true;

        foreach (MeshFilter source in crystalBallVisual.GetComponentsInChildren<MeshFilter>(true))
        {
            if (source.sharedMesh == null) continue;
            MeshRenderer sourceRenderer = source.GetComponent<MeshRenderer>();
            Material sourceMaterial = sourceRenderer != null ? sourceRenderer.sharedMaterial : null;
            Material material = new Material(hintShader);
            material.mainTexture = sourceMaterial != null ? sourceMaterial.mainTexture : null;
            Color baseColour = sourceMaterial != null && sourceMaterial.HasProperty("_Color") ? sourceMaterial.color : Color.white;
            material.SetColor("_Tint", baseColour);
            overlayMaterials.Add(material);

            GameObject proxy = new GameObject(source.name + " Colour Hint Proxy");
            proxy.layer = overlayLayer;
            proxy.transform.SetParent(overlayRoot, true);
            proxy.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            proxy.transform.localScale = source.transform.lossyScale;
            proxy.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
            proxy.AddComponent<MeshRenderer>().sharedMaterial = material;
        }
    }

    private void OnDestroy()
    {
        foreach (Material material in overlayMaterials) if (material != null) Destroy(material);
        if (overlayRoot != null) Destroy(overlayRoot.gameObject);
        if (overlayCamera != null) Destroy(overlayCamera.gameObject);
    }
}
