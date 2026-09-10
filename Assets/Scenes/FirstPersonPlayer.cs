using UnityEngine;

/// <summary>
/// Minimal first-person controller: hold W to move and use the mouse horizontally to turn.
/// Attach this component to an existing Player. It only uses an existing camera
/// and never creates a Player or Camera.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public sealed class FirstPersonPlayer : MonoBehaviour
{
    /// <summary>True after the player has picked up the battery.</summary>
    public bool HasBattery { get; private set; }
    /// <summary>True after the player has picked up the film.</summary>
    public bool HasFilm { get; private set; }

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 4.5f;
    [Header("Mouse Look")]
    [SerializeField, Min(0f)] private float mouseSensitivity = 1.5f;
    [SerializeField, Range(1f, 89f)] private float verticalLookLimit = 85f;
    [SerializeField] private bool invertVerticalLook;
    [SerializeField, Min(0f)] private float cameraHeight = 1.5f;

    private Rigidbody playerBody;
    [Header("Existing Scene References")]
    [Tooltip("Optional. If empty, the script searches for an existing Camera.")]
    [SerializeField] private Camera playerCamera;

    /// <summary>The scene camera used for the player's actual first-person view.</summary>
    public Camera PlayerCamera => playerCamera;

    private Transform viewCamera;
    private float cameraPitch;
    private float horizontalLook;
    private float initialPlayerPitch;
    private float initialPlayerRoll;
    private Vector3 movementInput;

    private void Awake()
    {
        playerBody = GetComponent<Rigidbody>();
        playerBody.constraints |= RigidbodyConstraints.FreezeRotation;
        playerBody.interpolation = RigidbodyInterpolation.Interpolate;
        if (!FindExistingViewCamera())
        {
            enabled = false;
            return;
        }

        Vector3 playerAngles = transform.eulerAngles;
        horizontalLook = playerAngles.y;
        initialPlayerPitch = NormalizeAngle(playerAngles.x);
        initialPlayerRoll = NormalizeAngle(playerAngles.z);
        cameraPitch = 0f;
        UpdateCameraTransform();
        SetCursorConfined();
    }

    private void Update()
    {
        UpdateCursorLock();

        if (Application.isFocused && Cursor.lockState != CursorLockMode.None)
        {
            LookWithMouse();
        }

        ReadMovementInput();
    }

    private void FixedUpdate()
    {
        Vector3 planarVelocity = movementInput * moveSpeed;
        playerBody.velocity = new Vector3(planarVelocity.x, playerBody.velocity.y, planarVelocity.z);
    }

    private void LateUpdate()
    {
        UpdateCameraTransform();
    }

    private void LookWithMouse()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        horizontalLook += mouseX;
        cameraPitch += (invertVerticalLook ? mouseY : -mouseY);
        cameraPitch = Mathf.Clamp(cameraPitch, -verticalLookLimit, verticalLookLimit);

        // Imported character models often use an X rotation such as -90 degrees
        // to correct their coordinate system. Preserve that correction while
        // changing only the player's horizontal facing direction.
        transform.rotation = Quaternion.Euler(initialPlayerPitch, horizontalLook, initialPlayerRoll);
    }

    private void ReadMovementInput()
    {
        Vector3 forward = Vector3.ProjectOnPlane(viewCamera.forward, Vector3.up).normalized;
        float direction = 0f;

        if (Input.GetKey(KeyCode.W))
        {
            direction += 1f;
        }

        if (Input.GetKey(KeyCode.S))
        {
            direction -= 1f;
        }

        movementInput = forward * direction;
    }

    private void UpdateCursorLock()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetCursorLocked(false);
        }
        else if (Input.GetMouseButtonDown(1))
        {
            SetCursorLocked(true);
        }
        else if (Input.GetMouseButtonUp(1))
        {
            SetCursorConfined();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            SetCursorLocked(false);
        }
    }

    private bool FindExistingViewCamera()
    {
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>(true);
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (playerCamera == null)
        {
            playerCamera = FindObjectOfType<Camera>();
        }

        if (playerCamera == null)
        {
            Debug.LogError(
                "FirstPersonPlayer: No existing Camera was found. " +
                "Assign your Main Camera to Player Camera in the Inspector.",
                this);
            return false;
        }

        viewCamera = playerCamera.transform;

        // Keep the camera outside the imported model hierarchy. The Player uses an
        // X-axis correction of -90 degrees, which a child camera would inherit.
        if (viewCamera.IsChildOf(transform))
        {
            viewCamera.SetParent(null, true);
        }

        return true;
    }

    private void UpdateCameraTransform()
    {
        if (viewCamera == null)
        {
            return;
        }

        viewCamera.position = transform.position + Vector3.up * cameraHeight;
        viewCamera.rotation = Quaternion.Euler(cameraPitch, horizontalLook, 0f);
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private static void SetCursorConfined()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }

    private static float NormalizeAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }

    public void GiveBattery()
    {
        if (HasBattery)
        {
            return;
        }

        HasBattery = true;
        Debug.Log("[Player State] Battery acquired. HasBattery = true", this);
    }

    public void GiveFilm()
    {
        if (HasFilm)
            return;

        HasFilm = true;
        Debug.Log("[Player State] Film acquired. HasFilm = true", this);
    }
}
