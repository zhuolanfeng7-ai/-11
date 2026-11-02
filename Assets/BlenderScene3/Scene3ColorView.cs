using UnityEngine;
[ImageEffectAllowedInSceneView,ExecuteAlways,RequireComponent(typeof(Camera))]
public class Scene3ColorView:MonoBehaviour {
 public Shader viewShader;Material view;
 void OnRenderImage(RenderTexture src,RenderTexture dst){if(!viewShader){Graphics.Blit(src,dst);return;}if(!view)view=new Material(viewShader){hideFlags=HideFlags.HideAndDontSave};Graphics.Blit(src,dst,view);}
 void OnDisable(){if(view){if(Application.isPlaying)Destroy(view);else DestroyImmediate(view);}}
}

