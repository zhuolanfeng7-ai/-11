using UnityEngine;

[DisallowMultipleComponent]
public sealed class ContinuousSelfRotation : MonoBehaviour
{
    [SerializeField] private float degreesPerSecond = 45f;
    private void Update()
    {
        transform.Rotate(Vector3.forward, degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
