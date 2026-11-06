using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// A self-contained keypad puzzle for the room door.  It intentionally uses
/// OnGUI so it remains visible independently of the existing monitor Canvas.
/// </summary>
public class DoorCodePuzzleController : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private string doorObjectName = "door";
    [SerializeField] private float interactionDistance = 7f;

    [Header("Puzzle")]
    [SerializeField] private string correctCode = "20040214";
    [SerializeField] private int codeLength = 8;
    [SerializeField, Min(.1f)] private float openDuration = 1.15f;

    [Header("Exit transition")]
    [Tooltip("Optional. Fill this with the next scene's exact name once it exists.")]
    [SerializeField] private string nextSceneName = "";
    [SerializeField] private Color doorwayGlowColor = new Color(1f, .72f, .38f, 1f);
    [SerializeField, Min(.1f)] private float transitionFadeSeconds = 1.1f;

    private Camera playerCamera;
    private Transform door;
    private Collider[] doorColliders;
    private string enteredCode = "";
    private bool isOpen;
    private string feedback = "";
    private float feedbackUntil;
    private bool openingOrOpen;
    private float transitionBlackAlpha;
    private GUIStyle titleStyle;
    private GUIStyle codeStyle;
    private GUIStyle buttonStyle;
    private GUIStyle feedbackStyle;

    private void Awake()
    {
        // Keep the puzzle deterministic even when Unity has retained an older
        // inspector serialization from a previous scene reload.
        correctCode = "20040214";
        codeLength = 8;
        nextSceneName = "scene 2";
        transitionFadeSeconds = 2f;
        FindDoor();
        SuppressAccidentalPickupBehaviour();
    }

    private void Update()
    {
        if (playerCamera == null)
        {
            FirstPersonPlayer player = FindObjectOfType<FirstPersonPlayer>();
            playerCamera = player != null ? player.PlayerCamera : Camera.main;
        }

        if (!isOpen)
        {
            TryOpenFromDoorClick();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape)) ClosePanel();
        if (Input.GetMouseButtonDown(0))
        {
            // GUI coordinates are top-down. Clicking outside the visible lock
            // panel is an intentional exit, so the player can return to look
            // for clues without having to finish entering a code.
            Vector2 guiMouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            if (!GetPanelBounds().Contains(guiMouse))
            {
                ClosePanel();
                return;
            }
        }
        ReadKeyboardInput();
    }

    private void FindDoor()
    {
        GameObject found = GameObject.Find(doorObjectName);
        if (found == null) return;
        door = found.transform;
        // Imported models usually keep their MeshCollider on a child, rather
        // than on the named visual object. Cache every one of those colliders.
        doorColliders = door.GetComponentsInChildren<Collider>(true);
    }

    private void SuppressAccidentalPickupBehaviour()
    {
        if (door == null) return;
        // This scene has several click-to-pickup scripts. A copied component on
        // the door makes the whole visual disappear before the keypad can open.
        // Keep only this controller active; renderers and colliders are never
        // disabled here.
        MonoBehaviour[] childBehaviours = door.GetComponentsInChildren<MonoBehaviour>(true);
        MonoBehaviour[] parentBehaviours = door.GetComponentsInParent<MonoBehaviour>(true);
        foreach (MonoBehaviour behaviour in childBehaviours)
            DisablePickupLikeBehaviour(behaviour);
        foreach (MonoBehaviour behaviour in parentBehaviours)
            DisablePickupLikeBehaviour(behaviour);
    }

    private void DisablePickupLikeBehaviour(MonoBehaviour behaviour)
    {
        // Do not disable this UI controller itself.
        if (behaviour == null || behaviour == this) return;
        string type = behaviour.GetType().Name;
        if (type.Contains("Pickup") || type.Contains("Puzzle") || type.Contains("Inspect"))
        {
            behaviour.enabled = false;
        }
    }

    private void TryOpenFromDoorClick()
    {
        if (!Input.GetMouseButtonDown(0) || playerCamera == null) return;
        if (door == null) FindDoor();
        if (door == null) return;

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        bool clickedDoor = false;
        if (doorColliders == null || doorColliders.Length == 0) FindDoor();
        if (doorColliders != null)
        {
            foreach (Collider doorCollider in doorColliders)
            {
                if (doorCollider != null && doorCollider.enabled && doorCollider.Raycast(ray, out _, interactionDistance))
                {
                    clickedDoor = true;
                    break;
                }
            }
        }

        // Some imported doors have no usable Collider. Their renderer bounds
        // still provide a precise click area without interacting with any other
        // scene collider.
        if (!clickedDoor)
        {
            foreach (Renderer renderer in door.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.enabled && renderer.bounds.IntersectRay(ray, out float distance) && distance <= interactionDistance)
                {
                    clickedDoor = true;
                    break;
                }
            }
        }
        if (!clickedDoor) return;

        isOpen = true;
        enteredCode = "";
        feedback = "";
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ReadKeyboardInput()
    {
        for (int i = 0; i <= 9; i++)
        {
            KeyCode topRow = (KeyCode)((int)KeyCode.Alpha0 + i);
            KeyCode keypad = (KeyCode)((int)KeyCode.Keypad0 + i);
            if (Input.GetKeyDown(topRow) || Input.GetKeyDown(keypad)) AddDigit(i);
        }
        if (Input.GetKeyDown(KeyCode.Backspace)) RemoveDigit();
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) ConfirmCode();
    }

    private void AddDigit(int digit)
    {
        if (enteredCode.Length < codeLength) enteredCode += digit.ToString();
    }

    private void RemoveDigit()
    {
        if (enteredCode.Length > 0) enteredCode = enteredCode.Substring(0, enteredCode.Length - 1);
    }

    private void ConfirmCode()
    {
        if (enteredCode.Length != codeLength)
        {
            SetFeedback("ENTER " + codeLength + " DIGITS", new Color(1f, .72f, .3f));
            return;
        }
        if (enteredCode == correctCode)
        {
            SetFeedback("ACCESS GRANTED", new Color(.35f, 1f, .65f));
            if (!openingOrOpen) StartCoroutine(OpenDoor());
        }
        else
        {
            SetFeedback("ACCESS DENIED", new Color(1f, .3f, .48f));
            enteredCode = "";
        }
    }

    private IEnumerator OpenDoor()
    {
        openingOrOpen = true;
        if (door == null) yield break;

        // The imported mesh's origin is in its middle. Create a pivot at the
        // actual right-hand edge of the leaf, where it meets the door frame.
        // Reparenting with worldPositionStays preserves the closed pose.
        Vector3 hingeWorldPoint = GetRightHingeWorldPoint();
        GameObject doorwayGlow = CreateDoorwayGlow();
        Light doorwayLight = doorwayGlow != null ? doorwayGlow.GetComponentInChildren<Light>() : null;
        Material glowMaterial = doorwayGlow != null ? doorwayGlow.GetComponent<Renderer>().material : null;
        Transform originalParent = door.parent;
        GameObject hingeObject = new GameObject("Door Right Hinge Pivot");
        Transform hinge = hingeObject.transform;
        hinge.SetParent(originalParent, true);
        hinge.position = hingeWorldPoint;
        hinge.rotation = door.rotation;
        door.SetParent(hinge, true);

        Quaternion closedRotation = hinge.rotation;
        // Rotate the door leaf around the right-hand hinge on its local Z axis.
        // The frame is not parented to this pivot and therefore stays still.
        Quaternion openRotation = closedRotation * Quaternion.Euler(0f, 0f, 90f);
        float elapsed = 0f;
        while (elapsed < openDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / openDuration));
            if (hinge != null) hinge.rotation = Quaternion.Slerp(closedRotation, openRotation, t);
            if (doorwayLight != null) doorwayLight.intensity = Mathf.Lerp(0f, 1.15f, t);
            if (glowMaterial != null) glowMaterial.color = new Color(doorwayGlowColor.r, doorwayGlowColor.g, doorwayGlowColor.b, .28f * t);
            yield return null;
        }
        if (hinge != null) hinge.rotation = openRotation;
        if (doorColliders != null)
            foreach (Collider doorCollider in doorColliders)
                if (doorCollider != null) doorCollider.enabled = false;
        yield return new WaitForSecondsRealtime(.25f);
        ClosePanel();
        yield return StartCoroutine(PlayExitTransition());
    }

    private GameObject CreateDoorwayGlow()
    {
        Bounds bounds = GetWorldBounds(door);
        // A narrow transparent strip sits just behind the leaf. The closed
        // door hides it; opening gradually reveals a soft leak through the gap.
        GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Quad);
        glow.name = "Doorway Transition Glow";
        Destroy(glow.GetComponent<Collider>());
        Vector3 behindDoor = bounds.center - door.forward * (Mathf.Max(.03f, bounds.extents.z + .025f));
        glow.transform.position = behindDoor;
        glow.transform.rotation = Quaternion.LookRotation(door.forward, door.up);
        glow.transform.localScale = new Vector3(Mathf.Max(.08f, bounds.size.x * .14f), Mathf.Max(.2f, bounds.size.y * .78f), 1f);
        Shader glowShader = Shader.Find("Unlit/Transparent");
        if (glowShader == null) glowShader = Shader.Find("Sprites/Default");
        Material material = new Material(glowShader);
        material.color = new Color(doorwayGlowColor.r, doorwayGlowColor.g, doorwayGlowColor.b, 0f);
        MeshRenderer glowRenderer = glow.GetComponent<MeshRenderer>();
        glowRenderer.material = material;
        glowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        glowRenderer.receiveShadows = false;

        GameObject lightObject = new GameObject("Doorway Gap Light");
        lightObject.transform.SetParent(glow.transform, false);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = doorwayGlowColor;
        light.range = Mathf.Max(1.4f, bounds.size.magnitude * .28f);
        light.intensity = 0f;
        return glow;
    }

    private IEnumerator PlayExitTransition()
    {
        float elapsed = 0f;
        while (elapsed < transitionFadeSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            transitionBlackAlpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / transitionFadeSeconds));
            yield return null;
        }
        transitionBlackAlpha = 1f;
        if (!string.IsNullOrWhiteSpace(nextSceneName) && Application.CanStreamedLevelBeLoaded(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    private Vector3 GetRightHingeWorldPoint()
    {
        Renderer[] renderers = door.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return door.position + door.right * .5f;

        // Convert all renderer-bound corners into door-local coordinates. This
        // gives the physical right edge even when the whole imported door is
        // rotated in the room.
        float right = float.NegativeInfinity;
        float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
        float minZ = float.PositiveInfinity, maxZ = float.NegativeInfinity;
        foreach (Renderer renderer in renderers)
        {
            Bounds b = renderer.bounds;
            Vector3 min = b.min, max = b.max;
            for (int x = 0; x <= 1; x++)
            for (int y = 0; y <= 1; y++)
            for (int z = 0; z <= 1; z++)
            {
                Vector3 local = door.InverseTransformPoint(new Vector3(
                    x == 0 ? min.x : max.x,
                    y == 0 ? min.y : max.y,
                    z == 0 ? min.z : max.z));
                right = Mathf.Max(right, local.x);
                minY = Mathf.Min(minY, local.y); maxY = Mathf.Max(maxY, local.y);
                minZ = Mathf.Min(minZ, local.z); maxZ = Mathf.Max(maxZ, local.z);
            }
        }
        return door.TransformPoint(new Vector3(right, (minY + maxY) * .5f, (minZ + maxZ) * .5f));
    }

    private static Bounds GetWorldBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(root.position, Vector3.one);
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private void SetFeedback(string text, Color color)
    {
        feedback = text;
        feedbackUntil = Time.unscaledTime + 1.5f;
        if (feedbackStyle != null) feedbackStyle.normal.textColor = color;
    }

    private void ClosePanel()
    {
        isOpen = false;
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }

    private void EnsureStyles()
    {
        if (titleStyle != null) return;
        titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 19, fontStyle = FontStyle.Bold };
        titleStyle.normal.textColor = new Color(.45f, .9f, 1f);
        codeStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 30, fontStyle = FontStyle.Bold };
        codeStyle.normal.textColor = new Color(.95f, .7f, 1f);
        buttonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, fontSize = 22, fontStyle = FontStyle.Bold };
        buttonStyle.normal.textColor = Color.white;
        feedbackStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14, fontStyle = FontStyle.Bold };
    }

    private void OnGUI()
    {
        if (transitionBlackAlpha > 0f)
        {
            Color oldOverlayColor = GUI.color;
            int oldDepth = GUI.depth;
            GUI.depth = -10000;
            GUI.color = new Color(0f, 0f, 0f, transitionBlackAlpha);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = oldOverlayColor;
            GUI.depth = oldDepth;
        }
        if (!isOpen) return;
        EnsureStyles();
        float scale = Mathf.Clamp(Screen.height / 900f, .75f, 1.25f);
        Rect panel = GetPanelBounds();

        Color old = GUI.color;
        GUI.color = new Color(.08f, .025f, .13f, .96f);
        GUI.Box(panel, GUIContent.none);
        GUI.color = new Color(.15f, .95f, .52f, 1f);
        GUI.DrawTexture(new Rect(panel.x + 3, panel.y + 3, panel.width - 6, 3), Texture2D.whiteTexture);
        GUI.color = new Color(.62f, .25f, 1f, 1f);
        GUI.DrawTexture(new Rect(panel.x + 9, panel.y + 9, panel.width - 18, 2), Texture2D.whiteTexture);
        GUI.color = old;

        GUI.Label(new Rect(panel.x, panel.y + 23 * scale, panel.width, 30 * scale), "DOOR ACCESS TERMINAL", titleStyle);
        GUI.Label(new Rect(panel.x, panel.y + 80 * scale, panel.width, 45 * scale), CodeVisual(), codeStyle);
        if (Time.unscaledTime < feedbackUntil)
            GUI.Label(new Rect(panel.x, panel.y + 128 * scale, panel.width, 26 * scale), feedback, feedbackStyle);

        float keySize = 70f * scale;
        float gap = 11f * scale;
        float gridWidth = keySize * 3 + gap * 2;
        float startX = panel.x + (panel.width - gridWidth) * .5f;
        float startY = panel.y + 170f * scale;
        for (int n = 1; n <= 9; n++)
        {
            int index = n - 1;
            Rect key = new Rect(startX + (index % 3) * (keySize + gap), startY + (index / 3) * (keySize + gap), keySize, keySize);
            if (GUI.Button(key, n.ToString(), buttonStyle)) AddDigit(n);
        }
        Rect zero = new Rect(startX + keySize + gap, startY + 3 * (keySize + gap), keySize, keySize);
        if (GUI.Button(zero, "0", buttonStyle)) AddDigit(0);
        Rect delete = new Rect(startX, zero.y, keySize, keySize);
        if (GUI.Button(delete, "⌫", buttonStyle)) RemoveDigit();
        Rect confirm = new Rect(startX + 2 * (keySize + gap), zero.y, keySize, keySize);
        if (GUI.Button(confirm, "✓", buttonStyle)) ConfirmCode();
    }

    private string CodeVisual()
    {
        string visual = "";
        for (int i = 0; i < codeLength; i++) visual += i < enteredCode.Length ? enteredCode[i] + "   " : "_   ";
        return visual.TrimEnd();
    }

    private static Rect GetPanelBounds()
    {
        float scale = Mathf.Clamp(Screen.height / 900f, .75f, 1.25f);
        float width = 430f * scale;
        // The fourth keypad row (delete, 0, confirm) reaches about 483 scaled
        // pixels below the panel top. Keep it inside the panel bounds so a
        // click on 0 is not mistaken for an outside click that closes the UI.
        float height = 510f * scale;
        return new Rect((Screen.width - width) * .5f, (Screen.height - height) * .5f - 25f * scale, width, height);
    }
}
