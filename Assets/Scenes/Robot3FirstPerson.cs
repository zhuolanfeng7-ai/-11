using UnityEngine;

// Scene 3 robot first-person movement. The imported mesh retains its original rotation.
[DisallowMultipleComponent]
public sealed class Robot3FirstPerson : MonoBehaviour
{
    [SerializeField] private Camera eyeCamera;
    [SerializeField, Min(0f)] private float moveSpeed = 0.48f;
    [SerializeField, Min(0f)] private float mouseSensitivity = 1.5f;
    [SerializeField] private float lookLimit = 80f;
    private float yaw;
    private float pitch;
<<<<<<< Updated upstream
=======
    private Rigidbody body;
    private CapsuleCollider capsule;
    private Vector2 moveInput;
    private Quaternion importedModelRotation;
    private float startingYaw;
    private float lockedWorldY;
    private static readonly RigidbodyConstraints StableConstraints =
        RigidbodyConstraints.FreezePositionY |
        RigidbodyConstraints.FreezeRotationX |
        RigidbodyConstraints.FreezeRotationZ;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        if (!body) body = gameObject.AddComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        importedModelRotation = transform.rotation;
        lockedWorldY = transform.position.y;
        body.useGravity = false;
        body.isKinematic = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.detectCollisions = true;
        body.constraints = StableConstraints;
        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        if (capsule) { capsule.enabled = true; capsule.isTrigger = false; }
    }
>>>>>>> Stashed changes

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
        if (!Application.isFocused || Cursor.lockState != CursorLockMode.Locked) return;
        float deltaYaw = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        yaw += deltaYaw;
        pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * mouseSensitivity, -lookLimit, lookLimit);
        eyeCamera.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
<<<<<<< Updated upstream
        Vector2 keys = new Vector2((Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f), (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f));
        keys = Vector2.ClampMagnitude(keys, 1f);
=======
        moveInput = new Vector2((Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f), (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f));
        moveInput = Vector2.ClampMagnitude(moveInput, 1f);
    }

    private void FixedUpdate()
    {
        if (!body) return;

        // The imported robot is rotated -90 degrees. If its Rigidbody rotation
        // constraints or collision flags are changed at runtime, the Z-axis
        // capsule falls sideways and the player appears to float through the room.
        // Reassert the authored physics contract before every simulation step.
        body.useGravity = false;
        body.isKinematic = false;
        body.detectCollisions = true;
        body.constraints = StableConstraints;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        if (capsule) { capsule.enabled = true; capsule.isTrigger = false; }

        Quaternion targetRotation = Quaternion.AngleAxis(yaw - startingYaw, Vector3.up) * importedModelRotation;
        body.angularVelocity = Vector3.zero;
        body.MoveRotation(targetRotation);

        // FreezePositionY normally protects this value. Restoring it explicitly
        // also recovers from a one-frame constraint loss or a direct Transform write.
        Vector3 position = body.position;
        if (Mathf.Abs(position.y - lockedWorldY) > .001f)
        {
            position.y = lockedWorldY;
            body.position = position;
        }

>>>>>>> Stashed changes
        Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        Vector3 right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
        Vector3 desiredVelocity = (forward * moveInput.y + right * moveInput.x) * moveSpeed;

        // A dynamic Rigidbody receives an impulse when it touches another
        // collider. MovePosition used to preserve that impulse, so the player
        // continued drifting after input stopped. Owning the planar velocity
        // every physics step keeps real collision blocking while discarding the
        // unwanted push/bounce from the previous contact.
        body.velocity = new Vector3(desiredVelocity.x, 0f, desiredVelocity.z);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!body) return;
        body.angularVelocity = Vector3.zero;
        body.velocity = Vector3.zero;
    }
    private void OnDisable() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    private void OnApplicationFocus(bool focused) { if (!focused) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; } }
}
