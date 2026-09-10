using UnityEngine;

/// <summary>
/// Full-screen blur for Unity's Built-in Render Pipeline.
/// Attach to the rendering Camera.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Image Effects/Vision Blur Effect")]
public sealed class VisionBlurEffect : MonoBehaviour
{
    [Header("Blur Appearance")]
    [Tooltip("Blur spread. Higher values make object outlines less distinct.")]
    [SerializeField, Range(0f, 10f)] private float blurRadius = 3.5f;

    [Tooltip("Number of horizontal/vertical blur cycles. Higher is smoother but more expensive.")]
    [SerializeField, Range(1, 6)] private int iterations = 3;

    [Tooltip("Render at a smaller resolution. 2 is a good balance; 3-4 is blurrier and faster.")]
    [SerializeField, Range(1, 4)] private int downsample = 2;

    [Tooltip("0 = completely black and white, 1 = original full color.")]
    [SerializeField, Range(0f, 1f)] private float saturation = 0f;

    [Header("Shader Reference")]
    [SerializeField] private Shader blurShader;

    private Material blurMaterial;

    public float BlurRadius
    {
        get => blurRadius;
        set => blurRadius = Mathf.Clamp(value, 0f, 10f);
    }

    public float Saturation
    {
        get => saturation;
        set => saturation = Mathf.Clamp01(value);
    }

    private void OnEnable()
    {
        EnsureMaterial();
    }

    private void OnDisable()
    {
        if (blurMaterial == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(blurMaterial);
        }
        else
        {
            DestroyImmediate(blurMaterial);
        }

        blurMaterial = null;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!EnsureMaterial())
        {
            Graphics.Blit(source, destination);
            return;
        }

        blurMaterial.SetFloat("_Saturation", saturation);

        if (blurRadius <= 0f)
        {
            Graphics.Blit(source, destination, blurMaterial, 1);
            return;
        }

        int width = Mathf.Max(1, source.width / downsample);
        int height = Mathf.Max(1, source.height / downsample);
        RenderTexture first = RenderTexture.GetTemporary(width, height, 0, source.format);
        RenderTexture second = RenderTexture.GetTemporary(width, height, 0, source.format);
        first.filterMode = FilterMode.Bilinear;
        second.filterMode = FilterMode.Bilinear;

        Graphics.Blit(source, first);

        for (int i = 0; i < iterations; i++)
        {
            float spread = blurRadius * (1f + i * 0.35f);

            blurMaterial.SetVector("_BlurDirection", new Vector4(spread / width, 0f, 0f, 0f));
            Graphics.Blit(first, second, blurMaterial, 0);

            blurMaterial.SetVector("_BlurDirection", new Vector4(0f, spread / height, 0f, 0f));
            Graphics.Blit(second, first, blurMaterial, 0);
        }

        Graphics.Blit(first, destination, blurMaterial, 1);
        RenderTexture.ReleaseTemporary(first);
        RenderTexture.ReleaseTemporary(second);
    }

    private bool EnsureMaterial()
    {
        if (blurMaterial != null)
        {
            return true;
        }

        if (blurShader == null)
        {
            blurShader = Shader.Find("Hidden/VisionBlur");
        }

        if (blurShader == null || !blurShader.isSupported)
        {
            return false;
        }

        blurMaterial = new Material(blurShader) { hideFlags = HideFlags.HideAndDontSave };
        return true;
    }
}
