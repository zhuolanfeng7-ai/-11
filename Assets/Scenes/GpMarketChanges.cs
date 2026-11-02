using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Renderer))]
public sealed class GpMarketChanges : MonoBehaviour
{
    [SerializeField] private Shader overlayShader;
    [SerializeField] private float minInterval = 1f;
    [SerializeField] private float maxInterval = 1f;
    private readonly float[] values = {12.8f,7.4f,18.6f,-2.1f,1.3f};
    private readonly float[] next = new float[5];
    private Renderer targetRenderer;
    private Texture source;
    private RenderTexture output;
    private Material compositor;
    private MaterialPropertyBlock properties;
    private void Start()
    {
        targetRenderer = GetComponent<Renderer>();
        if (!targetRenderer || !targetRenderer.sharedMaterial || !overlayShader) { Debug.LogError("GP market display is missing its renderer or shader.", this); enabled=false; return; }
        source = targetRenderer.sharedMaterial.GetTexture("_MainTex");
        if (!source) { Debug.LogError("GP market display is missing its original texture.", this); enabled=false; return; }
        compositor = new Material(overlayShader) { hideFlags = HideFlags.HideAndDontSave };
        output = new RenderTexture(source.width, source.height, 0, RenderTextureFormat.ARGB32) { name = "GP Dynamic Market Display", useMipMap = true, autoGenerateMips = true };
        output.Create();
        properties = new MaterialPropertyBlock();
        for (int i=0;i<next.Length;i++) next[i] = Time.time + Random.Range(minInterval,maxInterval);
        Draw();
    }
    private void Update()
    {
        if (!compositor) return;
        bool changed=false;
        for (int i=0;i<values.Length;i++) if (Time.time>=next[i]) {
            values[i]=Mathf.Round(Mathf.Clamp(values[i]+Random.Range(-5f,5f),-30f,30f)*10f)/10f;
            next[i]=Time.time+Random.Range(minInterval,maxInterval); changed=true;
        }
        if(changed) Draw();
    }
    private void Draw()
    {
        compositor.SetVector("_Values",new Vector4(values[0],values[1],values[2],values[3]));
        compositor.SetFloat("_Value5",values[4]);
        Graphics.Blit(source,output,compositor);
        targetRenderer.GetPropertyBlock(properties);
        properties.SetTexture("_MainTex",output);
        targetRenderer.SetPropertyBlock(properties);
    }
    private void OnDestroy()
    {
        if(targetRenderer && properties != null){targetRenderer.GetPropertyBlock(properties);properties.SetTexture("_MainTex",source);targetRenderer.SetPropertyBlock(properties);}
        if(output){output.Release();Destroy(output);} if(compositor)Destroy(compositor);
    }
}


