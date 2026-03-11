using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerCameraController : NetworkBehaviour
{
    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 70f;

    [Header("References")]
    [SerializeField] private Transform cameraTarget;

    private PlayerInputActions inputActions;
    private float yaw;
    private float pitch;
    private bool isEliminated;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        inputActions = new PlayerInputActions();
        inputActions.Player.Enable();

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Initialize yaw from current rotation
        yaw = transform.eulerAngles.y;

        // Enable Cinemachine camera for owner
        var cmCam = GetComponentInChildren<Unity.Cinemachine.CinemachineCamera>(true);
        if (cmCam != null)
            cmCam.gameObject.SetActive(true);

        var health = GetComponent<PlayerHealth>();
        if (health != null)
            health.OnDied += OnDied;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        inputActions?.Player.Disable();
        inputActions?.Dispose();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        var health = GetComponent<PlayerHealth>();
        if (health != null)
            health.OnDied -= OnDied;
    }

    private void LateUpdate()
    {
        if (!IsOwner || isEliminated || inputActions == null) return;

        Vector2 lookInput = inputActions.Player.Look.ReadValue<Vector2>();

        yaw += lookInput.x * mouseSensitivity;
        pitch -= lookInput.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        if (cameraTarget != null)
        {
            cameraTarget.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }

    private void OnDied()
    {
        isEliminated = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
