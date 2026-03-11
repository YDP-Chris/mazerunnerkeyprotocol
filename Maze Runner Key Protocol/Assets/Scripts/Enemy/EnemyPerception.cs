using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Handles sight (cone + raycast) and sound detection for enemies.
/// Runs on host only. Stores last-known player position with memory timer.
/// </summary>
public class EnemyPerception : NetworkBehaviour
{
    [SerializeField] private EnemyConfig config;
    [SerializeField] private LayerMask wallLayerMask;
    [SerializeField] private Vector3 eyeOffset = new Vector3(0, 1.5f, 0);

    private Transform visiblePlayer;
    private Vector3 lastKnownPosition;
    private float memoryTimer;
    private bool hasMemory;
    private float sightCheckTimer;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            enabled = false;
            return;
        }

        SoundEventSystem.OnSoundBroadcast += OnSoundEvent;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
            SoundEventSystem.OnSoundBroadcast -= OnSoundEvent;
    }

    /// <summary>
    /// Call from state machine with throttle interval.
    /// CHASE/ATTACK pass 0 (every frame), PATROL passes 0.3-0.5s.
    /// </summary>
    public void UpdatePerception(float throttleInterval)
    {
        if (!IsServer) return;

        // Throttle sight checks
        sightCheckTimer += Time.deltaTime;
        if (sightCheckTimer >= throttleInterval)
        {
            sightCheckTimer = 0f;
            visiblePlayer = CheckSight();
        }

        // Update memory timer
        if (hasMemory)
        {
            if (visiblePlayer != null)
            {
                // Refresh memory with current visible player position
                lastKnownPosition = visiblePlayer.position;
                memoryTimer = 0f;
            }
            else
            {
                memoryTimer += Time.deltaTime;
                if (memoryTimer >= config.memoryDuration)
                {
                    hasMemory = false;
                }
            }
        }
    }

    /// <summary>
    /// Returns the currently visible player, or null.
    /// </summary>
    public Transform GetVisiblePlayer()
    {
        return visiblePlayer;
    }

    /// <summary>
    /// Returns true if enemy has a remembered last-known position.
    /// </summary>
    public bool GetLastKnownPosition(out Vector3 position)
    {
        position = lastKnownPosition;
        return hasMemory;
    }

    public void ClearMemory()
    {
        hasMemory = false;
        visiblePlayer = null;
    }

    private Transform CheckSight()
    {
        Vector3 eyePos = transform.position + eyeOffset;
        Transform closest = null;
        float closestDist = float.MaxValue;

        foreach (var kvp in NetworkManager.Singleton.SpawnManager.SpawnedObjects)
        {
            var playerHealth = kvp.Value.GetComponent<PlayerHealth>();
            if (playerHealth == null || playerHealth.IsEliminated) continue;

            Vector3 targetPos = kvp.Value.transform.position + new Vector3(0, 1f, 0);
            Vector3 toTarget = targetPos - eyePos;
            float distance = toTarget.magnitude;

            // Distance check
            if (distance > config.sightRange) continue;

            // Angle check (cone)
            float angle = Vector3.Angle(transform.forward, toTarget.normalized);
            if (angle > config.sightAngle * 0.5f) continue;

            // Wall occlusion raycast
            if (IsWallBlocking(eyePos, targetPos)) continue;

            if (distance < closestDist)
            {
                closestDist = distance;
                closest = kvp.Value.transform;
            }
        }

        if (closest != null)
        {
            lastKnownPosition = closest.position;
            memoryTimer = 0f;
            hasMemory = true;
        }

        return closest;
    }

    private bool IsWallBlocking(Vector3 from, Vector3 to)
    {
        Vector3 direction = to - from;
        return Physics.Raycast(from, direction.normalized, direction.magnitude, wallLayerMask);
    }

    public void OnSoundEvent(Vector3 origin, float radius, SoundType type)
    {
        if (!IsServer) return;

        float distance = Vector3.Distance(transform.position, origin);
        if (distance > radius) return;

        // Check wall occlusion for sound
        Vector3 eyePos = transform.position + eyeOffset;
        if (IsWallBlocking(eyePos, origin)) return;

        // Only update if no player currently visible
        if (visiblePlayer == null)
        {
            lastKnownPosition = origin;
            memoryTimer = 0f;
            hasMemory = true;
        }
    }

    public void SetConfig(EnemyConfig newConfig)
    {
        config = newConfig;
    }
}
