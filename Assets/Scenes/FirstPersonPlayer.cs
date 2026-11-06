using UnityEngine;

/// <summary>
/// First-person controller: WASD movement, direct mouse look and a visible pointer.
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
        playerBody.detectCollisions = true;
        playerBody.collisionDetectionMode = playerBody.isKinematic
            ? CollisionDetectionMode.ContinuousSpeculative : CollisionDetectionMode.ContinuousDynamic;
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
        SetCursorLocked(true);
    }

    private void Update()
    {
        if(Scene2RepairCelebration.IsPlaying||EarSafeInstallation.BlocksPlayerInput||Scene2PlateInstallation.BlocksWorldInput||Scene2WorldIntroduction.BlocksWorldInput||Scene2PickupPresentation.IsPlaying){movementInput=Vector3.zero;return;}
        UpdateCursorLock();

        if (Application.isFocused && Cursor.lockState != CursorLockMode.None)
        {
            // Keep the view stable on the left-click frame so object selection
            // uses the pose shown to the player, then resume mouse look.
            if (!Input.GetMouseButtonDown(0)) LookWithMouse();
        }

        ReadMovementInput();
        if(movementInput.sqrMagnitude>.0001f&&
            (Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.D)))
            Scene2WorldIntroduction.NotifyPlayerWalking();
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
        if (!Application.isFocused || Cursor.lockState == CursorLockMode.None)
        {
            movementInput = Vector3.zero;
            return;
        }
        Vector2 input = new Vector2(
            (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f),
            (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f));
        movementInput = CalculatePlanarMovement(input, viewCamera.forward);
    }

    public static Vector3 CalculatePlanarMovement(Vector2 input, Vector3 viewForward)
    {
        Vector3 forward = Vector3.ProjectOnPlane(viewForward, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        return Vector3.ClampMagnitude(right * input.x + forward * input.y, 1f);
    }

    private void OnDisable()
    {
        movementInput = Vector3.zero;
        if (playerBody != null && !playerBody.isKinematic)
            playerBody.velocity = new Vector3(0f, playerBody.velocity.y, 0f);
    }

    private void UpdateCursorLock()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetCursorLocked(false);
        }
        else if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            SetCursorLocked(true);
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
        if (viewCamera.IsChildOf(transform) && playerCamera.GetComponent<Scene2RobotCameraFollow>() == null)
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

        var eyeView = playerCamera.GetComponent<Scene2RobotCameraFollow>();
        if (eyeView != null)
        {
            if (!eyeView.IsEyeView) eyeView.Bind(transform);
            eyeView.SetEyePitch(cameraPitch);
            eyeView.SnapToTarget();
            return;
        }
        viewCamera.position = transform.position + Vector3.up * cameraHeight;
        viewCamera.rotation = Quaternion.Euler(cameraPitch, horizontalLook, 0f);
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Confined : CursorLockMode.None;
        Cursor.visible = true;
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


