using System;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Runtime diagnostics for the Scene3 robot player. This component only observes
/// and reports physics state; it deliberately does not repair or override it.
/// Press F8 in Play Mode to show or hide the live panel.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(32000)]
public sealed class Scene3PlayerPhysicsDebug : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private bool showOverlay = false;
    [SerializeField] private KeyCode overlayKey = KeyCode.F8;
    [SerializeField, Min(.05f)] private float sampleInterval = .2f;

    [Header("Anomaly thresholds")]
    [SerializeField, Min(0f)] private float tiltWarningDegrees = 4f;
    [SerializeField, Min(0f)] private float verticalSpeedWarning = .15f;
    [SerializeField, Min(0f)] private float airborneWarningSeconds = 1f;
    [SerializeField, Min(.01f)] private float groundProbeDistance = .18f;

    private Rigidbody body;
    private CapsuleCollider capsule;
    private RigidbodyConstraints expectedConstraints;
    private bool expectedUseGravity;
    private bool expectedDetectCollisions;
    private bool expectedColliderEnabled;
    private bool expectedIsTrigger;
    private int initialLayer;
    private Quaternion uprightReference;
    private float nextSample;
    private float airborneSince = -1f;
    private int contacts;
    private int collisionEnterCount;
    private int collisionExitCount;
    private string lastCollision = "none";
    private string lastWarning = "none";
    private readonly StringBuilder panel = new StringBuilder(1024);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Attach(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Attach(scene);

    private static void Attach(Scene scene)
    {
        if (!scene.IsValid() || !string.Equals(scene.name.Replace(" ", ""), "scene3", StringComparison.OrdinalIgnoreCase)) return;
        Robot3FirstPerson player = FindObjectOfType<Robot3FirstPerson>();
        if (player && !player.GetComponent<Scene3PlayerPhysicsDebug>())
            player.gameObject.AddComponent<Scene3PlayerPhysicsDebug>();
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        if (!body || !capsule)
        {
            Debug.LogError("[Scene3 Player Physics] Rigidbody or CapsuleCollider is missing.", this);
            enabled = false;
            return;
        }

        expectedConstraints = body.constraints;
        expectedUseGravity = body.useGravity;
        expectedDetectCollisions = body.detectCollisions;
        expectedColliderEnabled = capsule.enabled;
        expectedIsTrigger = capsule.isTrigger;
        initialLayer = gameObject.layer;
        uprightReference = body.rotation;
        Debug.Log(BuildSnapshot("Debugger attached"), this);
    }

    private void Update()
    {
        if (Input.GetKeyDown(overlayKey)) showOverlay = !showOverlay;
    }

    private void FixedUpdate()
    {
        if (!body || !capsule || Time.unscaledTime < nextSample) return;
        nextSample = Time.unscaledTime + sampleInterval;

        bool grounded = ProbeGround(out RaycastHit groundHit);
        if (grounded) airborneSince = -1f;
        else if (airborneSince < 0f) airborneSince = Time.unscaledTime;

        Check(body.constraints != expectedConstraints,
            "Rigidbody constraints changed: expected " + expectedConstraints + ", actual " + body.constraints);
        Check(body.useGravity != expectedUseGravity,
            "useGravity changed: expected " + expectedUseGravity + ", actual " + body.useGravity);
        Check(body.detectCollisions != expectedDetectCollisions,
            "detectCollisions changed: expected " + expectedDetectCollisions + ", actual " + body.detectCollisions);
        Check(capsule.enabled != expectedColliderEnabled,
            "CapsuleCollider enabled changed: expected " + expectedColliderEnabled + ", actual " + capsule.enabled);
        Check(capsule.isTrigger != expectedIsTrigger,
            "CapsuleCollider trigger changed: expected " + expectedIsTrigger + ", actual " + capsule.isTrigger);
        Check(gameObject.layer != initialLayer,
            "Player layer changed: expected " + LayerMask.LayerToName(initialLayer) + "(" + initialLayer + "), actual " + LayerMask.LayerToName(gameObject.layer) + "(" + gameObject.layer + ")");

        float tilt = Quaternion.Angle(body.rotation, YawOnly(body.rotation) * Quaternion.Inverse(YawOnly(uprightReference)) * uprightReference);
        Check(tilt > tiltWarningDegrees,
            "Player tilted " + tilt.ToString("F2") + " degrees. Rotation constraints may have been cleared or Transform.rotation was written directly.");
        Check(Mathf.Abs(body.velocity.y) > verticalSpeedWarning,
            "Unexpected vertical velocity: " + body.velocity.y.ToString("F3"));
        Check(!grounded && airborneSince >= 0f && Time.unscaledTime - airborneSince > airborneWarningSeconds,
            "Player has no ground below the capsule for " + (Time.unscaledTime - airborneSince).ToString("F2") + "s. Nearest hit=" + (groundHit.collider ? groundHit.collider.name : "none"));

        BuildPanel(grounded, groundHit, tilt);
    }

    private void Check(bool condition, string message)
    {
        if (!condition || message == lastWarning) return;
        lastWarning = message;
        Debug.LogWarning("[Scene3 Player Physics] " + message + "\n" + BuildSnapshot("Anomaly"), this);
    }

    private bool ProbeGround(out RaycastHit hit)
    {
        Bounds bounds = capsule.bounds;
        Vector3 origin = new Vector3(bounds.center.x, bounds.min.y + .03f, bounds.center.z);
        float radius = Mathf.Max(.025f, Mathf.Min(bounds.extents.x, bounds.extents.z) * .8f);
        int mask = Physics.DefaultRaycastLayers & ~(1 << gameObject.layer);
        return Physics.SphereCast(origin + Vector3.up * .04f, radius, Vector3.down, out hit,
            groundProbeDistance + .04f, mask, QueryTriggerInteraction.Ignore);
    }

    private static Quaternion YawOnly(Quaternion rotation) => Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);

    private void OnCollisionEnter(Collision collision)
    {
        collisionEnterCount++;
        contacts = collision.contactCount;
        lastCollision = "ENTER " + CollisionName(collision);
        Debug.Log("[Scene3 Player Physics] " + lastCollision + " contacts=" + contacts, this);
    }

    private void OnCollisionStay(Collision collision)
    {
        contacts = collision.contactCount;
        lastCollision = "STAY " + CollisionName(collision);
    }

    private void OnCollisionExit(Collision collision)
    {
        collisionExitCount++;
        contacts = 0;
        lastCollision = "EXIT " + CollisionName(collision);
        Debug.Log("[Scene3 Player Physics] " + lastCollision, this);
    }

    private static string CollisionName(Collision collision)
    {
        if (collision == null || !collision.collider) return "null";
        return collision.collider.name + " layer=" + LayerMask.LayerToName(collision.gameObject.layer);
    }

    private string BuildSnapshot(string reason)
    {
        return "[Scene3 Player Physics] " + reason +
               " pos=" + transform.position.ToString("F3") +
               " rot=" + transform.eulerAngles.ToString("F2") +
               " velocity=" + (body ? body.velocity.ToString("F3") : "missing") +
               " angular=" + (body ? body.angularVelocity.ToString("F3") : "missing") +
               " constraints=" + (body ? body.constraints.ToString() : "missing") +
               " detectCollisions=" + (body && body.detectCollisions) +
               " collider=" + (capsule && capsule.enabled) +
               " trigger=" + (capsule && capsule.isTrigger) +
               " layer=" + gameObject.layer;
    }

    private void BuildPanel(bool grounded, RaycastHit hit, float tilt)
    {
        panel.Length = 0;
        panel.AppendLine("SCENE3 PLAYER PHYSICS DEBUG  [F8]");
        panel.Append("Grounded: ").Append(grounded).Append("  ground: ").AppendLine(hit.collider ? hit.collider.name : "none");
        panel.Append("Position: ").AppendLine(transform.position.ToString("F3"));
        panel.Append("Rotation: ").Append(transform.eulerAngles.ToString("F2")).Append("  tilt: ").Append(tilt.ToString("F2")).AppendLine(" deg");
        panel.Append("Velocity: ").AppendLine(body.velocity.ToString("F3"));
        panel.Append("Constraints: ").AppendLine(body.constraints.ToString());
        panel.Append("Gravity/Kinematic/Detect: ").Append(body.useGravity).Append(" / ").Append(body.isKinematic).Append(" / ").AppendLine(body.detectCollisions.ToString());
        panel.Append("Capsule enabled/trigger/direction: ").Append(capsule.enabled).Append(" / ").Append(capsule.isTrigger).Append(" / ").AppendLine(capsule.direction.ToString());
        panel.Append("Collision: ").Append(lastCollision).Append("  contacts=").Append(contacts).Append("  enter/exit=").Append(collisionEnterCount).Append('/').AppendLine(collisionExitCount.ToString());
        panel.Append("Last warning: ").Append(lastWarning);
    }

    private void OnGUI()
    {
        if (!showOverlay || string.IsNullOrEmpty(panel.ToString())) return;
        GUI.color = new Color(1f, 1f, 1f, .94f);
        GUI.Box(new Rect(12f, 12f, 590f, 230f), panel.ToString());
        GUI.color = Color.white;
    }

    private void OnDrawGizmosSelected()
    {
        CapsuleCollider current = capsule ? capsule : GetComponent<CapsuleCollider>();
        if (!current) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(current.bounds.center, current.bounds.size);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(current.bounds.center.x, current.bounds.min.y, current.bounds.center.z),
            new Vector3(current.bounds.center.x, current.bounds.min.y - groundProbeDistance, current.bounds.center.z));
    }
}
