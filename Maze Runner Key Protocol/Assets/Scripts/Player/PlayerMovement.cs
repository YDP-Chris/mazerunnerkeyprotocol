using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    public enum MovementState { Idle, Moving, Sprinting, Dead }

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5.0f;
    [SerializeField] private float sprintSpeed = 8.0f;
    [SerializeField] private float rotationSpeed = 10.0f;
    [SerializeField] private float gravity = 9.81f;

    public MovementState CurrentState { get; private set; } = MovementState.Idle;

    private CharacterController controller;
    private PlayerInputActions inputActions;
    private Transform cameraTransform;
    private float verticalVelocity;
    private Vector3 lastFacingDirection = Vector3.forward;
    private bool isEliminated;
    private float footstepTimer;
    private const float FOOTSTEP_INTERVAL = 0.5f;
    private const float FOOTSTEP_RADIUS = 5f;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            // Disable CharacterController on non-owner so NetworkTransform can drive position
            var cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            enabled = false;
            return;
        }

        controller = GetComponent<CharacterController>();
        inputActions = new PlayerInputActions();
        inputActions.Player.Enable();

        var health = GetComponent<PlayerHealth>();
        if (health != null)
            health.OnDied += OnDied;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        inputActions?.Player.Disable();
        inputActions?.Dispose();

        var health = GetComponent<PlayerHealth>();
        if (health != null)
            health.OnDied -= OnDied;
    }

    private float debugTimer;

    private void Update()
    {
        if (!IsOwner || isEliminated || inputActions == null) return;

        // Lazily find camera — may not be available at spawn time on clients
        if (cameraTransform == null)
        {
            if (Camera.main != null)
                cameraTransform = Camera.main.transform;
            else
            {
                debugTimer += Time.deltaTime;
                if (debugTimer >= 2f)
                {
                    debugTimer = 0f;
                    Debug.LogWarning("[PlayerMovement] Camera.main is NULL — cannot move");
                }
                return;
            }
        }

        // Read movement: try Keyboard.current first (most reliable in editor),
        // then fall back to Input Actions
        var kb = UnityEngine.InputSystem.Keyboard.current;
        Vector2 moveInput = Vector2.zero;
        bool sprintPressed = false;

        if (kb != null)
        {
            float x = 0f, y = 0f;
            if (kb.wKey.isPressed) y += 1f;
            if (kb.sKey.isPressed) y -= 1f;
            if (kb.aKey.isPressed) x -= 1f;
            if (kb.dKey.isPressed) x += 1f;
            moveInput = new Vector2(x, y);
            sprintPressed = kb.leftShiftKey.isPressed;
        }

        if (moveInput == Vector2.zero && inputActions != null)
        {
            moveInput = inputActions.Player.Move.ReadValue<Vector2>();
            sprintPressed = inputActions.Player.Sprint.IsPressed();
        }

        // Debug: log input state periodically
        debugTimer += Time.deltaTime;
        if (debugTimer >= 2f)
        {
            debugTimer = 0f;
            bool wPressed = kb != null && kb.wKey.isPressed;
            Debug.Log($"[PlayerMovement] moveInput={moveInput} kb.w={wPressed} kb={kb != null} controller={controller.enabled} pos={transform.position}");
        }

        // Camera-relative movement direction
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x);
        if (moveDirection.magnitude > 1f)
            moveDirection.Normalize();

        // Sprint only when moving forward
        bool isSprinting = sprintPressed && moveInput.y > 0f;
        float speed = isSprinting ? sprintSpeed : walkSpeed;

        // Gravity
        if (controller.isGrounded)
        {
            verticalVelocity = -1f; // Stick force
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        // Apply movement
        Vector3 velocity = moveDirection * speed;
        velocity.y = verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        // Rotation - face movement direction
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            lastFacingDirection = moveDirection.normalized;
        }

        Quaternion targetRotation = Quaternion.LookRotation(lastFacingDirection, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // Update state
        if (moveDirection.sqrMagnitude > 0.01f)
            CurrentState = isSprinting ? MovementState.Sprinting : MovementState.Moving;
        else
            CurrentState = MovementState.Idle;

        // Broadcast footstep sounds for enemy AI
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            footstepTimer += Time.deltaTime;
            if (footstepTimer >= FOOTSTEP_INTERVAL)
            {
                footstepTimer = 0f;
                if (IsServer)
                    SoundEventSystem.BroadcastSound(transform.position, FOOTSTEP_RADIUS, SoundType.Footstep);
                else
                    BroadcastFootstepServerRpc(transform.position);
            }
        }
    }

    [ServerRpc]
    private void BroadcastFootstepServerRpc(Vector3 position)
    {
        SoundEventSystem.BroadcastSound(position, FOOTSTEP_RADIUS, SoundType.Footstep);
    }

    private void OnDied()
    {
        isEliminated = true;
        CurrentState = MovementState.Dead;
    }
}
