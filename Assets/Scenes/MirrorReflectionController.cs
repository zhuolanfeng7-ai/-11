using UnityEngine;

/// <summary>
/// Owns the reflection feature for MirrorReflection.
/// MirrorFrame remains untouched; only the child named MirrorPlane receives the reflection material.
/// </summary>
public sealed class MirrorReflectionController : MonoBehaviour
{
    [Header("Phase 1: Hierarchy")]
    [SerializeField] private string mirrorPlaneName = "MirrorPlane";

    [Header("Phase 2: Reflection")]
    [SerializeField] private Camera playerCamera;
    [Min(128)] [SerializeField] private int reflectionTextureSize = 1024;
    [Min(0.001f)] [SerializeField] private float clipPlaneOffset = 0.07f;
    [SerializeField] private LayerMask reflectionCullingMask = ~0;
    [SerializeField] private Vector3 mirrorNormalLocalAxis = Vector3.forward;

    [Header("Reflection Framing")]
    [Range(10f, 90f)] [SerializeField] private float reflectionFieldOfView = 45f;
    [Min(1f)] [SerializeField] private float reflectionFarClip = 45f;
    [SerializeField] private Color outsideRoomColor = Color.black;

    private Transform mirrorPlane;
    private Renderer mirrorPlaneRenderer;
    private Material[] originalMaterials;
    private Material reflectionMaterial;
    private RenderTexture reflectionTexture;
    private Camera reflectionCamera;

    private void Awake()
    {
        mirrorPlane = transform.Find(mirrorPlaneName);
        if (mirrorPlane == null)
        {
            Debug.LogError("MirrorReflection: child 'MirrorPlane' was not found. MirrorFrame was not changed.", this);
            enabled = false;
            return;
        }

        mirrorPlaneRenderer = mirrorPlane.GetComponentInChildren<Renderer>(true);
        if (mirrorPlaneRenderer == null)
        {
            Debug.LogError("MirrorReflection: MirrorPlane needs a Renderer. MirrorFrame was not changed.", this);
            enabled = false;
            return;
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
        }

        if (playerCamera == null)
        {
            Debug.LogError("MirrorReflection: no player camera was found.", this);
            enabled = false;
            return;
        }

        // Phase 1 accepted: MirrorReflection -> MirrorPlane has been found.
        originalMaterials = mirrorPlaneRenderer.sharedMaterials;
        CreateReflectionResources();
        ApplyReflectionMaterial();
        Debug.Log("[MirrorReflection] Phase 1 and 2 ready: only MirrorPlane is using real-time reflection.", this);
    }

    private void LateUpdate()
    {
        if (reflectionCamera != null)
        {
            RenderReflection();
        }
    }

    private void CreateReflectionResources()
    {
        reflectionTexture = new RenderTexture(reflectionTextureSize, reflectionTextureSize, 16, RenderTextureFormat.ARGB32)
        {
            name = "MirrorReflection Render Texture",
            hideFlags = HideFlags.DontSave
        };

        reflectionMaterial = new Material(Shader.Find("Unlit/Texture"))
        {
            name = "MirrorReflection Runtime Material",
            hideFlags = HideFlags.DontSave
        };
        reflectionMaterial.mainTexture = reflectionTexture;

        GameObject cameraObject = new GameObject("Mirror Reflection Camera")
        {
            hideFlags = HideFlags.DontSave
        };
        cameraObject.transform.SetParent(transform, false);
        reflectionCamera = cameraObject.AddComponent<Camera>();
        reflectionCamera.enabled = false;
    }

    private void ApplyReflectionMaterial()
    {
        Material[] materials = new Material[originalMaterials.Length];
        for (int i = 0; i < materials.Length; i++)
        {
            materials[i] = reflectionMaterial;
        }

        mirrorPlaneRenderer.sharedMaterials = materials;
    }

    private void RenderReflection()
    {
        reflectionCamera.CopyFrom(playerCamera);
        reflectionCamera.enabled = false;
        reflectionCamera.targetTexture = reflectionTexture;
        reflectionCamera.cullingMask = reflectionCullingMask;
        reflectionCamera.useOcclusionCulling = false;
        // Do not inherit the broad gameplay view. The reflection should stay focused on the room.
        reflectionCamera.fieldOfView = reflectionFieldOfView;
        reflectionCamera.farClipPlane = reflectionFarClip;
        reflectionCamera.clearFlags = CameraClearFlags.SolidColor;
        reflectionCamera.backgroundColor = outsideRoomColor;
        Vector3 planePosition = mirrorPlane.position;
        Vector3 planeNormal = mirrorPlane.TransformDirection(mirrorNormalLocalAxis).normalized;
        if (Vector3.Dot(planeNormal, playerCamera.transform.position - planePosition) < 0f)
        {
            planeNormal = -planeNormal;
        }

        Vector4 reflectionPlane = new Vector4(
            planeNormal.x,
            planeNormal.y,
            planeNormal.z,
            -Vector3.Dot(planeNormal, planePosition) - clipPlaneOffset);
        Matrix4x4 reflectionMatrix = CalculateReflectionMatrix(reflectionPlane);

        Vector3 reflectedPosition = reflectionMatrix.MultiplyPoint(playerCamera.transform.position);
        Vector3 reflectedForward = reflectionMatrix.MultiplyVector(playerCamera.transform.forward);
        Vector3 reflectedUp = reflectionMatrix.MultiplyVector(playerCamera.transform.up);
        reflectionCamera.transform.SetPositionAndRotation(reflectedPosition, Quaternion.LookRotation(reflectedForward, reflectedUp));
        reflectionCamera.worldToCameraMatrix = playerCamera.worldToCameraMatrix * reflectionMatrix;
        reflectionCamera.projectionMatrix = playerCamera.CalculateObliqueMatrix(CameraSpacePlane(planePosition, planeNormal));

        bool wasPlaneEnabled = mirrorPlaneRenderer.enabled;
        mirrorPlaneRenderer.enabled = false;
        GL.invertCulling = true;
        try
        {
            reflectionCamera.Render();
        }
        finally
        {
            GL.invertCulling = false;
            mirrorPlaneRenderer.enabled = wasPlaneEnabled;
        }
    }

    private Vector4 CameraSpacePlane(Vector3 position, Vector3 normal)
    {
        Vector3 offsetPosition = position + normal * clipPlaneOffset;
        Matrix4x4 worldToCamera = reflectionCamera.worldToCameraMatrix;
        Vector3 cameraPosition = worldToCamera.MultiplyPoint(offsetPosition);
        Vector3 cameraNormal = worldToCamera.MultiplyVector(normal).normalized;
        return new Vector4(cameraNormal.x, cameraNormal.y, cameraNormal.z, -Vector3.Dot(cameraPosition, cameraNormal));
    }

    private static Matrix4x4 CalculateReflectionMatrix(Vector4 plane)
    {
        Matrix4x4 matrix = Matrix4x4.identity;
        matrix.m00 = 1f - 2f * plane[0] * plane[0];
        matrix.m01 = -2f * plane[0] * plane[1];
        matrix.m02 = -2f * plane[0] * plane[2];
        matrix.m03 = -2f * plane[3] * plane[0];
        matrix.m10 = -2f * plane[1] * plane[0];
        matrix.m11 = 1f - 2f * plane[1] * plane[1];
        matrix.m12 = -2f * plane[1] * plane[2];
        matrix.m13 = -2f * plane[3] * plane[1];
        matrix.m20 = -2f * plane[2] * plane[0];
        matrix.m21 = -2f * plane[2] * plane[1];
        matrix.m22 = 1f - 2f * plane[2] * plane[2];
        matrix.m23 = -2f * plane[3] * plane[2];
        return matrix;
    }

    private void OnDisable()
    {
        if (mirrorPlaneRenderer != null && originalMaterials != null)
        {
            mirrorPlaneRenderer.sharedMaterials = originalMaterials;
        }
    }

    private void OnDestroy()
    {
        if (reflectionTexture != null)
        {
            reflectionTexture.Release();
            Destroy(reflectionTexture);
        }

        if (reflectionMaterial != null)
        {
            Destroy(reflectionMaterial);
        }
    }
}
