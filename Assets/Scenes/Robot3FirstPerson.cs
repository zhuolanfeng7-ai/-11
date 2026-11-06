using UnityEngine;

// Scene 3 robot first-person movement. The imported mesh retains its original rotation.
[DisallowMultipleComponent]
[RequireComponent(typeof(CapsuleCollider))]
public sealed class Robot3FirstPerson : MonoBehaviour
{
    [SerializeField] private Camera eyeCamera;
    [SerializeField, Min(0f)] private float moveSpeed = 0.48f;
    [SerializeField, Min(0f)] private float mouseSensitivity = 1.5f;
    [SerializeField] private float lookLimit = 80f;
    private float yaw;
    private float pitch;
    private Rigidbody body;
    private Vector2 moveInput;
    private float pendingYaw;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        if (!body) body = gameObject.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.constraints = RigidbodyConstraints.FreezePositionY
            | RigidbodyConstraints.FreezeRotationX
            | RigidbodyConstraints.FreezeRotationZ;
    }

    private void Start()
    {
        if (!eyeCamera) { enabled = false; Debug.LogError("Robot3 eye camera is missing.", this); return; }
        yaw = eyeCamera.transform.eulerAngles.y;
        pitch = 0f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        if (!Application.isFocused || Cursor.lockState != CursorLockMode.Locked) { moveInput = Vector2.zero; return; }
        float deltaYaw = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        yaw += deltaYaw;
        pendingYaw += deltaYaw;
        pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * mouseSensitivity, -lookLimit, lookLimit);
        eyeCamera.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        moveInput = new Vector2((Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f), (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f));
        moveInput = Vector2.ClampMagnitude(moveInput, 1f);
    }

    private void FixedUpdate()
    {
        if (!body) return;
        if (Mathf.Abs(pendingYaw) > Mathf.Epsilon)
        {
            body.MoveRotation(Quaternion.AngleAxis(pendingYaw, Vector3.up) * body.rotation);
            pendingYaw = 0f;
        }
        Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        Vector3 right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
        Vector3 velocity = (forward * moveInput.y + right * moveInput.x) * moveSpeed;
        body.velocity = new Vector3(velocity.x, 0f, velocity.z);
    }
    private void OnDisable() { moveInput = Vector2.zero; if (body) body.velocity = Vector3.zero; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    private void OnApplicationFocus(bool focused) { if (!focused) { moveInput = Vector2.zero; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; } }
}
