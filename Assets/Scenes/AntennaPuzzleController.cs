using System.Collections;
using UnityEngine;

/// <summary>
/// Controls both television antennas from one stable parent object.
/// The rotations authored in the scene are captured as the puzzle's correct answer.
/// </summary>
public sealed class AntennaPuzzleController : MonoBehaviour
{
    [Header("Puzzle Hierarchy")]
    [SerializeField] private Transform antennaLeftPivot;
    [SerializeField] private Transform antennaRightPivot;
    [SerializeField] private Collider buttonUpCollider;
    [SerializeField] private Collider buttonDownCollider;

    [Header("Existing Scene References")]
    [SerializeField] private Camera interactionCamera;
    [SerializeField] private FirstPersonPlayer player;

    [Header("Rotation Settings")]
    [SerializeField] private Vector3 localRotationAxis = Vector3.right;
    [SerializeField, Min(1f)] private float rotationPerClick = 20f;
    [SerializeField] private int minimumStep = -1;
    [SerializeField] private int maximumStep = 1;
    [SerializeField] private int initialLeftStep = -1;
    [SerializeField] private int initialRightStep = 1;
    [SerializeField] private int correctStep = 0;
    [SerializeField, Min(0.01f)] private float rotationDuration = 0.25f;

    [Header("Interaction")]
    [SerializeField, Min(0.1f)] private float interactionDistance = 12f;
    [SerializeField, Min(1f)] private float clickRayDistance = 100f;
    [SerializeField, Min(0f)] private float clickAssistRadiusPixels = 70f;

    [Header("TV Screen Result")]
    [SerializeField] private Renderer screenRenderer;
    [SerializeField] private Texture2D finalNoFilmTexture;
    [SerializeField] private Texture2D final2024Texture;
    [SerializeField, Min(0.1f)] private float screenGlitchDuration = 2f;
    [SerializeField, Range(0f, 0.5f)] private float glitchOffsetStrength = 0.12f;
    [SerializeField, Min(0.01f)] private float glitchFrameInterval = 0.1f;
    [SerializeField, Range(0f, 0.05f)] private float finalIdleJitterStrength = 0.006f;
    [SerializeField, Min(0.03f)] private float finalIdleJitterInterval = 0.12f;
    [SerializeField, Range(0f, 0.3f)] private float finalIdleBrightnessFlicker = 0.06f;
    [SerializeField] private bool flipFinalHorizontally;
    [SerializeField] private bool flipFinalVertically;
    [SerializeField, Range(-2, 2)] private int finalRotationQuarterTurns = 1;

    private Quaternion leftCorrectRotation;
    private Quaternion rightCorrectRotation;
    private Vector3 leftCorrectPosition;
    private Vector3 rightCorrectPosition;
    private bool leftMoving;
    private bool rightMoving;
    private bool puzzleTriggered;
    private int leftStep;
    private int rightStep;
    private int leftDirection;
    private int rightDirection;
    private Material screenMaterialInstance;
    private Texture originalScreenTexture;
    private Vector2 originalScreenScale;
    private Vector2 originalScreenOffset;
    private Coroutine finalIdleGlitchRoutine;
    public bool IsShowingNoFilm { get; private set; }

    private enum PuzzleButton
    {
        None,
        Up,
        Down
    }

    private void Awake()
    {
        if (interactionCamera == null)
            interactionCamera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
        if (player == null)
            player = FindObjectOfType<FirstPersonPlayer>();
        EnsureHoverScan(buttonUpCollider);
        EnsureHoverScan(buttonDownCollider);

        if (screenRenderer != null)
        {
            screenMaterialInstance = screenRenderer.material;
            originalScreenTexture = screenMaterialInstance.mainTexture;
            originalScreenScale = screenMaterialInstance.mainTextureScale;
            originalScreenOffset = screenMaterialInstance.mainTextureOffset;
            Shader screenShader = Resources.Load<Shader>("TVScreenGlitch");
            if (screenShader != null)
                screenMaterialInstance.shader = screenShader;
        }

        // Load the result texture only as a reference. Do not put it on screen
        // until PlayScreenResult finishes its two-second glitch.
        if (finalNoFilmTexture == null)
            finalNoFilmTexture = Resources.Load<Texture2D>("TV_NoFilm_Install_Glitch");
        if (finalNoFilmTexture != null)
        {
            finalNoFilmTexture.filterMode = FilterMode.Bilinear;
            finalNoFilmTexture.wrapMode = TextureWrapMode.Clamp;
        }
        if (final2024Texture == null)
            final2024Texture = Resources.Load<Texture2D>("TV_2024_Glitch");
        if (final2024Texture != null)
        {
            final2024Texture.filterMode = FilterMode.Bilinear;
            final2024Texture.wrapMode = TextureWrapMode.Clamp;
        }

        if (antennaLeftPivot != null)
        {
            leftCorrectPosition = antennaLeftPivot.localPosition;
            leftCorrectRotation = antennaLeftPivot.localRotation;
        }
        if (antennaRightPivot != null)
        {
            rightCorrectPosition = antennaRightPivot.localPosition;
            rightCorrectRotation = antennaRightPivot.localRotation;
        }
    }

    private void Start()
    {
        if (!ValidateReferences())
            return;

        if (minimumStep > maximumStep)
        {
            int oldMinimum = minimumStep;
            minimumStep = maximumStep;
            maximumStep = oldMinimum;
        }

        correctStep = Mathf.Clamp(correctStep, minimumStep, maximumStep);
        leftStep = Mathf.Clamp(initialLeftStep, minimumStep, maximumStep);
        rightStep = Mathf.Clamp(initialRightStep, minimumStep, maximumStep);
        leftDirection = leftStep >= maximumStep ? -1 : 1;
        rightDirection = rightStep >= maximumStep ? -1 : 1;
        antennaLeftPivot.localPosition = leftCorrectPosition;
        antennaRightPivot.localPosition = rightCorrectPosition;
        antennaLeftPivot.localRotation = GetRotationForStep(leftCorrectRotation, leftStep);
        antennaRightPivot.localRotation = GetRotationForStep(rightCorrectRotation, rightStep);
        Debug.Log("[AntennaPuzzle] 已记录当前天线位置和角度；游戏以左右错开的错误角度开始。", this);
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0) || interactionCamera == null || player == null)
            return;

        PuzzleButton clickedButton = GetClickedButton();

        if (clickedButton == PuzzleButton.Up && !leftMoving)
        {
            TryRotateAntenna(true);
            return;
        }

        if (clickedButton == PuzzleButton.Down && !rightMoving)
            TryRotateAntenna(false);
    }

    private PuzzleButton GetClickedButton()
    {
        bool upInRange = IsColliderInRange(buttonUpCollider);
        bool downInRange = IsColliderInRange(buttonDownCollider);
        Ray ray = interactionCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit upHit = new RaycastHit();
        RaycastHit downHit = new RaycastHit();

        bool hitUp = upInRange && buttonUpCollider.Raycast(ray, out upHit, clickRayDistance);
        bool hitDown = downInRange && buttonDownCollider.Raycast(ray, out downHit, clickRayDistance);

        if (hitUp && hitDown)
            return upHit.distance <= downHit.distance ? PuzzleButton.Up : PuzzleButton.Down;
        if (hitUp)
            return PuzzleButton.Up;
        if (hitDown)
            return PuzzleButton.Down;

        float upScreenDistance = upInRange ? GetPointerDistance(buttonUpCollider) : float.PositiveInfinity;
        float downScreenDistance = downInRange ? GetPointerDistance(buttonDownCollider) : float.PositiveInfinity;
        float nearestDistance = Mathf.Min(upScreenDistance, downScreenDistance);
        if (nearestDistance > clickAssistRadiusPixels)
            return PuzzleButton.None;

        return upScreenDistance <= downScreenDistance ? PuzzleButton.Up : PuzzleButton.Down;
    }

    private bool IsColliderInRange(Collider buttonCollider)
    {
        if (buttonCollider == null)
            return false;

        Vector3 closestPoint = buttonCollider.ClosestPoint(player.transform.position);
        return Vector3.Distance(player.transform.position, closestPoint) <= interactionDistance;
    }

    private float GetPointerDistance(Collider buttonCollider)
    {
        Vector3 screenPoint = interactionCamera.WorldToScreenPoint(buttonCollider.bounds.center);
        if (screenPoint.z <= 0f)
            return float.PositiveInfinity;

        return Vector2.Distance(Input.mousePosition, new Vector2(screenPoint.x, screenPoint.y));
    }

    private void TryRotateAntenna(bool isLeft)
    {
        int currentStep = isLeft ? leftStep : rightStep;
        int direction = isLeft ? leftDirection : rightDirection;
        int targetStep = currentStep + direction;

        if (targetStep < minimumStep || targetStep > maximumStep)
        {
            direction = -direction;
            targetStep = currentStep + direction;
        }

        if (isLeft)
            leftDirection = direction;
        else
            rightDirection = direction;

        StartCoroutine(RotateAntenna(isLeft, targetStep));
    }

    private IEnumerator RotateAntenna(bool isLeft, int targetStep)
    {
        if (isLeft)
            leftMoving = true;
        else
            rightMoving = true;

        Transform antenna = isLeft ? antennaLeftPivot : antennaRightPivot;
        Quaternion correctRotation = isLeft ? leftCorrectRotation : rightCorrectRotation;
        Vector3 correctPosition = isLeft ? leftCorrectPosition : rightCorrectPosition;
        antenna.localPosition = correctPosition;
        Quaternion start = antenna.localRotation;
        Quaternion target = GetRotationForStep(correctRotation, targetStep);
        float elapsed = 0f;

        while (elapsed < rotationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / rotationDuration));
            antenna.localPosition = correctPosition;
            antenna.localRotation = Quaternion.Slerp(start, target, progress);
            yield return null;
        }

        antenna.SetLocalPositionAndRotation(correctPosition, target);
        if (isLeft)
        {
            leftStep = targetStep;
            leftMoving = false;
        }
        else
        {
            rightStep = targetStep;
            rightMoving = false;
        }

        CheckPuzzleState();
    }

    private void CheckPuzzleState()
    {
        if (puzzleTriggered)
            return;

        if (leftStep == correctStep && rightStep == correctStep)
        {
            puzzleTriggered = true;
            Debug.Log("机关被触发", this);
            if (screenMaterialInstance != null && finalNoFilmTexture != null)
                StartCoroutine(PlayScreenResult());
            else
                Debug.LogError("AntennaPuzzle: Screen Renderer or final No Film texture is missing.", this);
        }
    }

    private IEnumerator PlayScreenResult()
    {
        if (screenMaterialInstance.HasProperty("_UVRotation"))
            screenMaterialInstance.SetFloat("_UVRotation", 0f);

        Vector2 originalScale = screenMaterialInstance.mainTextureScale;
        Vector2 originalOffset = screenMaterialInstance.mainTextureOffset;
        float elapsed = 0f;

        while (elapsed < screenGlitchDuration)
        {
            float horizontalOffset = Random.Range(-glitchOffsetStrength, glitchOffsetStrength);
            float verticalOffset = Random.Range(-glitchOffsetStrength * 0.15f, glitchOffsetStrength * 0.15f);
            screenMaterialInstance.mainTextureOffset = originalOffset + new Vector2(horizontalOffset, verticalOffset);
            screenMaterialInstance.mainTextureScale = new Vector2(
                originalScale.x * Random.Range(0.9f, 1.1f),
                originalScale.y);

            if (screenMaterialInstance.HasProperty("_Color"))
            {
                bool briefBlackout = Random.value < 0.18f;
                screenMaterialInstance.color = briefBlackout
                    ? Color.black
                    : new Color(Random.Range(0.65f, 1f), Random.Range(0.65f, 1f), Random.Range(0.65f, 1f), 1f);
            }

            yield return new WaitForSeconds(glitchFrameInterval);
            elapsed += glitchFrameInterval;
        }

        // This is the only place where the final message texture is assigned.
        // Therefore the game always starts on the original color-bar texture.
        screenMaterialInstance.mainTexture = finalNoFilmTexture;
        screenMaterialInstance.mainTextureScale = new Vector2(
            flipFinalHorizontally ? -1f : 1f,
            flipFinalVertically ? -1f : 1f);
        screenMaterialInstance.mainTextureOffset = new Vector2(
            flipFinalHorizontally ? 1f : 0f,
            flipFinalVertically ? 1f : 0f);
        if (screenMaterialInstance.HasProperty("_UVRotation"))
            screenMaterialInstance.SetFloat("_UVRotation", finalRotationQuarterTurns * 90f);
        if (screenMaterialInstance.HasProperty("_Color"))
            screenMaterialInstance.color = Color.white;

        Debug.Log("[AntennaPuzzle] 电视故障结束，显示 No film, install required。", this);
        IsShowingNoFilm = true;
        finalIdleGlitchRoutine = StartCoroutine(PlayFinalScreenIdleGlitch());
    }

    /// <summary>Called only after the player has picked up the physical Film item.</summary>
    public bool TryInstallFilm()
    {
        if (!IsShowingNoFilm || screenMaterialInstance == null || final2024Texture == null)
            return false;

        if (finalIdleGlitchRoutine != null)
            StopCoroutine(finalIdleGlitchRoutine);

        screenMaterialInstance.mainTexture = final2024Texture;
        screenMaterialInstance.mainTextureScale = new Vector2(
            flipFinalHorizontally ? -1f : 1f,
            flipFinalVertically ? -1f : 1f);
        screenMaterialInstance.mainTextureOffset = new Vector2(
            flipFinalHorizontally ? 1f : 0f,
            flipFinalVertically ? 1f : 0f);
        if (screenMaterialInstance.HasProperty("_UVRotation"))
            screenMaterialInstance.SetFloat("_UVRotation", finalRotationQuarterTurns * 90f);
        if (screenMaterialInstance.HasProperty("_Color"))
            screenMaterialInstance.color = Color.white;

        IsShowingNoFilm = false;
        finalIdleGlitchRoutine = StartCoroutine(PlayFinalScreenIdleGlitch());
        Debug.Log("[AntennaPuzzle] Film installed. Screen now shows 2024.", this);
        return true;
    }

    private IEnumerator PlayFinalScreenIdleGlitch()
    {
        Vector2 baseScale = new Vector2(
            flipFinalHorizontally ? -1f : 1f,
            flipFinalVertically ? -1f : 1f);
        Vector2 baseOffset = new Vector2(
            flipFinalHorizontally ? 1f : 0f,
            flipFinalVertically ? 1f : 0f);

        while (true)
        {
            Vector2 jitter = new Vector2(
                Random.Range(-finalIdleJitterStrength, finalIdleJitterStrength),
                Random.Range(-finalIdleJitterStrength * 0.35f,
                    finalIdleJitterStrength * 0.35f));
            screenMaterialInstance.mainTextureScale = baseScale;
            screenMaterialInstance.mainTextureOffset = baseOffset + jitter;

            if (screenMaterialInstance.HasProperty("_Color"))
            {
                float brightness = 1f - Random.Range(0f, finalIdleBrightnessFlicker);
                screenMaterialInstance.color = new Color(brightness, brightness, brightness, 1f);
            }

            yield return new WaitForSeconds(finalIdleJitterInterval);
        }
    }

    private Quaternion GetRotationForStep(Quaternion correctRotation, int step)
    {
        return correctRotation * Quaternion.AngleAxis(step * rotationPerClick, GetRotationAxis());
    }

    private Vector3 GetRotationAxis()
    {
        return localRotationAxis.sqrMagnitude > 0.0001f
            ? localRotationAxis.normalized
            : Vector3.right;
    }

    private bool ValidateReferences()
    {
        bool valid = antennaLeftPivot != null && antennaRightPivot != null &&
                     buttonUpCollider != null && buttonDownCollider != null;
        if (!valid)
            Debug.LogError("AntennaPuzzle: Left/Right Pivot or button Collider references are missing.", this);
        return valid;
    }

    private static void EnsureHoverScan(Collider buttonCollider)
    {
        if (buttonCollider == null) return;
        HoverScanHighlight scan = buttonCollider.GetComponent<HoverScanHighlight>();
        if (scan == null) scan = buttonCollider.gameObject.AddComponent<HoverScanHighlight>();
        // Match the CrystalBallPuzzle hover treatment: the same purple/green
        // neon outline, screen-space padding, and interaction range.
        scan.Configure(20f, 12f);
    }
}
