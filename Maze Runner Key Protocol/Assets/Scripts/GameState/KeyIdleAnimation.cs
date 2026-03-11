using UnityEngine;

/// <summary>
/// Slow Y-axis rotation and subtle shimmer on the key when uncollected or dropped.
/// Attached to the key prefab.
/// </summary>
public class KeyIdleAnimation : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 45f; // degrees per second
    [SerializeField] private float bobAmplitude = 0.15f;
    [SerializeField] private float bobSpeed = 1.5f;

    private float baseY;

    private void OnEnable()
    {
        baseY = transform.position.y;
    }

    private void Update()
    {
        // Rotate
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        // Bob up and down
        Vector3 pos = transform.position;
        pos.y = baseY + Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        transform.position = pos;
    }
}
