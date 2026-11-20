using UnityEngine;
using UnityEngine.Video;

[DisallowMultipleComponent]
[RequireComponent(typeof(VideoPlayer))]
[DefaultExecutionOrder(-1000)]
public sealed class WebGLVideoUrlSource : MonoBehaviour
{
    [SerializeField] private string streamingAssetsRelativePath;

    public void Configure(string relativePath)
    {
        streamingAssetsRelativePath = relativePath.Replace('\\', '/').TrimStart('/');
        Apply();
    }

    private void Awake()
    {
        Apply();
    }

    private void Apply()
    {
        if (string.IsNullOrWhiteSpace(streamingAssetsRelativePath)) return;
        VideoPlayer player = GetComponent<VideoPlayer>();
        player.Stop();
        player.clip = null;
        player.source = VideoSource.Url;
        player.url = Application.streamingAssetsPath.TrimEnd('/') + "/" + streamingAssetsRelativePath;
    }
}
