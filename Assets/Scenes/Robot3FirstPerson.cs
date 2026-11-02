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
        transform.rotation = Quaternion.AngleAxis(deltaYaw, Vector3.up) * transform.rotation;
        eyeCamera.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector2 keys = new Vector2((Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f), (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f));
        keys = Vector2.ClampMagnitude(keys, 1f);
        Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        Vector3 right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
        transform.position += (forward * keys.y + right * keys.x) * (moveSpeed * Time.deltaTime);
    }
    private void OnDisable() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    private void OnApplicationFocus(bool focused) { if (!focused) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; } }
}
