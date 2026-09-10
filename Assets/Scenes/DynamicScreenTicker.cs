using System.Collections;
using UnityEngine;

/// <summary>Animates pixels inside the original poster; it creates no foreground text or mesh.</summary>
public sealed class DynamicScreenTicker : MonoBehaviour
{
    [SerializeField] private int startingPercent = 12;
    [SerializeField] private int finalPercent = 26;
    [SerializeField, Min(.1f)] private float updateInterval = 1f;
    [SerializeField, Min(1)] private int minimumUpdatesBeforeFinal = 300;
    [Tooltip("Exact baked +12% area, normalized from the poster texture.")]
    [SerializeField] private Rect percentageRegion = new Rect(.535f, .665f, .325f, .155f);
    [SerializeField] private Color paperColor = Color.white;
    [SerializeField] private Color numberColor = new Color(1f, .025f, .025f, 1f);
    [SerializeField, Range(1, 30)] private int pixelScale = 15;
    [Header("TV / LCD Presentation")]
    [SerializeField, Range(0f, 1f)] private float scanlineStrength = .16f;
    [SerializeField, Range(0f, .15f)] private float brightnessFlicker = .028f;
    [SerializeField, Range(0f, .02f)] private float rgbOffset = .0022f;
    [SerializeField, Range(.05f, 4f)] private float scanlineSpeed = .65f;

    private Renderer posterRenderer;
    private Material screenMaterial;
    private Texture2D posterCopy;
    private int textureWidth, textureHeight;

    private void Start()
    {
        posterRenderer = GetComponent<Renderer>();
        if (posterRenderer == null || posterRenderer.material.mainTexture == null)
        {
            Debug.LogError("[DynamicScreenTicker] Poster renderer or texture is missing.", this);
            enabled = false;
            return;
        }
        screenMaterial = posterRenderer.material;
        posterCopy = CopyToReadableTexture(screenMaterial.mainTexture);
        textureWidth = posterCopy.width;
        textureHeight = posterCopy.height;
        Shader tvShader = Shader.Find("Custom/TVScreenGlitch");
        if (tvShader != null) screenMaterial.shader = tvShader;
        screenMaterial.mainTexture = posterCopy;
        screenMaterial.SetFloat("_ScanlineStrength", scanlineStrength);
        screenMaterial.SetFloat("_FlickerStrength", brightnessFlicker);
        screenMaterial.SetFloat("_RgbOffset", rgbOffset);
        screenMaterial.SetFloat("_ScanlineSpeed", scanlineSpeed);
        DrawValue(startingPercent);
        StartCoroutine(AnimateMarket());
    }

    private void Update()
    {
        if (screenMaterial != null)
            screenMaterial.SetFloat("_DisplayTime", Time.time);
    }

    private Texture2D CopyToReadableTexture(Texture source)
    {
        RenderTexture target = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(source, target);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = target;
        Texture2D copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        copy.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(target);
        return copy;
    }

    private IEnumerator AnimateMarket()
    {
        int current = startingPercent;
        for (int update = 0; update < minimumUpdatesBeforeFinal; update++)
        {
            yield return new WaitForSeconds(updateInterval);
            // Stock-like movement: both rises and dips, while trending toward the eventual +26%.
            int change = Random.value < .42f ? -Random.Range(1, 4) : Random.Range(1, 5);
            current = Mathf.Clamp(current + change, 4, 29);
            DrawValue(current);
        }
        yield return new WaitForSeconds(updateInterval);
        DrawValue(finalPercent);
    }

    private void DrawValue(int value)
    {
        RectInt region = new RectInt(
            Mathf.RoundToInt(percentageRegion.x * textureWidth),
            Mathf.RoundToInt(percentageRegion.y * textureHeight),
            Mathf.RoundToInt(percentageRegion.width * textureWidth),
            Mathf.RoundToInt(percentageRegion.height * textureHeight));
        EraseOldNumberCompletely(region);
        string text = $"+{value}%";
        int glyphWidth = 5 * pixelScale;
        // Fixed left edge prevents +9%, +12%, and +26% from shifting or overlapping.
        int startX = region.x + 24;
        int startY = region.y + Mathf.Max(0, (region.height - 7 * pixelScale) / 2);
        for (int i = 0; i < text.Length; i++)
            DrawGlyph(text[i], startX + i * (glyphWidth + pixelScale), startY);
        posterCopy.Apply(false);
    }

    private void EraseOldNumberCompletely(RectInt region)
    {
        for (int y = region.y; y < region.yMax; y++)
            for (int x = region.x; x < region.xMax; x++)
                if (x >= 0 && x < textureWidth && y >= 0 && y < textureHeight)
                    posterCopy.SetPixel(x, y, paperColor);
    }

    private void DrawGlyph(char character, int startX, int startY)
    {
        string[] rows = Glyph(character);
        for (int row = 0; row < 7; row++)
            for (int column = 0; column < 5; column++)
                if (rows[row][column] == '1')
                    FillBlock(startX + column * pixelScale, startY + (6 - row) * pixelScale);
    }

    private void FillBlock(int startX, int startY)
    {
        for (int y = startY; y < startY + pixelScale; y++)
            for (int x = startX; x < startX + pixelScale; x++)
                if (x >= 0 && x < textureWidth && y >= 0 && y < textureHeight)
                    posterCopy.SetPixel(x, y, numberColor);
    }

    private static string[] Glyph(char c)
    {
        switch (c)
        {
            case '+': return new[] { "00000", "00100", "00100", "11111", "00100", "00100", "00000" };
            case '0': return new[] { "01110", "10001", "10011", "10101", "11001", "10001", "01110" };
            case '1': return new[] { "00100", "01100", "00100", "00100", "00100", "00100", "01110" };
            case '2': return new[] { "01110", "10001", "00001", "00010", "00100", "01000", "11111" };
            case '3': return new[] { "11110", "00001", "00001", "01110", "00001", "00001", "11110" };
            case '4': return new[] { "00010", "00110", "01010", "10010", "11111", "00010", "00010" };
            case '5': return new[] { "11111", "10000", "11110", "00001", "00001", "10001", "01110" };
            case '6': return new[] { "00110", "01000", "10000", "11110", "10001", "10001", "01110" };
            case '7': return new[] { "11111", "00001", "00010", "00100", "01000", "01000", "01000" };
            case '8': return new[] { "01110", "10001", "10001", "01110", "10001", "10001", "01110" };
            case '9': return new[] { "01110", "10001", "10001", "01111", "00001", "00010", "11100" };
            case '%': return new[] { "11001", "11010", "00100", "01000", "10110", "00110", "00000" };
            default: return new[] { "00000", "00000", "00000", "00000", "00000", "00000", "00000" };
        }
    }

    private void OnDestroy()
    {
        if (posterCopy != null) Destroy(posterCopy);
    }
}
