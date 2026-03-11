using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;
using System;

public class KeyManager : NetworkBehaviour
{
    public enum KeyState : byte { Uncollected, Held, Dropped }

    [Header("Placement Settings")]
    [SerializeField] private float minDistanceFromSpawnsAndExit = 20f;
    [SerializeField] private int maxPlacementAttempts = 50;
    [SerializeField] private float mazeMinX = 0f;
    [SerializeField] private float mazeMaxX = 80f;
    [SerializeField] private float mazeMinZ = 0f;
    [SerializeField] private float mazeMaxZ = 80f;

    [Header("Prefab")]
    [SerializeField] private GameObject keyPrefab;

    public NetworkVariable<Vector3> KeyWorldPosition = new NetworkVariable<Vector3>(
        Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<ulong> KeyHolderClientId = new NetworkVariable<ulong>(
        ulong.MaxValue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<KeyState> CurrentKeyState = new NetworkVariable<KeyState>(
        KeyState.Uncollected, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private GameObject spawnedKeyInstance;

    public event Action<ulong> OnKeyPickedUp;   // holderClientId
    public event Action<Vector3> OnKeyDropped;   // dropPosition
    public static KeyManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            PlaceKey();
            SpawnKeyObject();
        }

        KeyWorldPosition.OnValueChanged += OnKeyPositionChanged;
        CurrentKeyState.OnValueChanged += OnKeyStateChanged;
    }

    private void SpawnKeyObject()
    {
        if (keyPrefab == null)
        {
            Debug.LogWarning("[KeyManager] No key prefab assigned!");
            return;
        }

        var keyObj = Instantiate(keyPrefab, KeyWorldPosition.Value, Quaternion.identity);
        keyObj.GetComponent<NetworkObject>().Spawn();
        RegisterKeyInstance(keyObj);
        Debug.Log($"[KeyManager] Key spawned at {KeyWorldPosition.Value}");
    }

    public override void OnNetworkDespawn()
    {
        KeyWorldPosition.OnValueChanged -= OnKeyPositionChanged;
        CurrentKeyState.OnValueChanged -= OnKeyStateChanged;

        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Host-only: place key at a random navigable position, min distance from spawns and exit.
    /// Max 50 attempts with fallback to best-distance tile.
    /// </summary>
    public void PlaceKey()
    {
        if (!IsServer) return;

        var spawnPoints = FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None);
        var exitPoints = FindObjectsByType<ExitSpawnPoint>(FindObjectsSortMode.None);

        Vector3 bestPosition = new Vector3(40f, 0.5f, 40f); // center fallback
        float bestMinDist = 0f;

        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {
            Vector3 candidate = new Vector3(
                UnityEngine.Random.Range(mazeMinX + 2f, mazeMaxX - 2f),
                0.5f,
                UnityEngine.Random.Range(mazeMinZ + 2f, mazeMaxZ - 2f)
            );

            // Snap to NavMesh
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit navHit, 4f, NavMesh.AllAreas))
                continue;

            candidate = navHit.position;
            candidate.y = 0.5f;

            float minDist = GetMinDistanceFromSpawnsAndExit(candidate, spawnPoints, exitPoints);

            // Track best candidate regardless
            if (minDist > bestMinDist)
            {
                bestMinDist = minDist;
                bestPosition = candidate;
            }

            if (minDist >= minDistanceFromSpawnsAndExit)
            {
                KeyWorldPosition.Value = candidate;
                Debug.Log($"[KeyManager] Key placed at {candidate} (attempt {attempt + 1})");
                return;
            }
        }

        // Fallback: use best candidate found
        Debug.LogWarning($"[KeyManager] Could not find ideal key position in {maxPlacementAttempts} attempts. Using best candidate at {bestPosition} (min dist: {bestMinDist:F1})");
        KeyWorldPosition.Value = bestPosition;
    }

    private float GetMinDistanceFromSpawnsAndExit(Vector3 position, PlayerSpawnPoint[] spawns, ExitSpawnPoint[] exits)
    {
        float minDist = float.MaxValue;

        foreach (var spawn in spawns)
        {
            float dist = Vector3.Distance(position, spawn.transform.position);
            if (dist < minDist) minDist = dist;
        }

        foreach (var exit in exits)
        {
            float dist = Vector3.Distance(position, exit.transform.position);
            if (dist < minDist) minDist = dist;
        }

        return minDist;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestPickupServerRpc(ulong clientId)
    {
        if (CurrentKeyState.Value == KeyState.Held) return;

        CurrentKeyState.Value = KeyState.Held;
        KeyHolderClientId.Value = clientId;
        NotifyKeyPickupClientRpc(clientId);
        Debug.Log($"[KeyManager] Key picked up by client {clientId}");
    }

    [ClientRpc]
    private void NotifyKeyPickupClientRpc(ulong holderClientId)
    {
        OnKeyPickedUp?.Invoke(holderClientId);

        // Hide world key
        if (spawnedKeyInstance != null)
            spawnedKeyInstance.SetActive(false);
    }

    /// <summary>
    /// Host-only: drop key at death position, snapped to NavMesh.
    /// </summary>
    public void DropKey(Vector3 deathPosition)
    {
        if (!IsServer) return;

        // Snap to NavMesh
        if (NavMesh.SamplePosition(deathPosition, out NavMeshHit navHit, 4f, NavMesh.AllAreas))
            deathPosition = navHit.position;

        deathPosition.y = 0.5f;

        CurrentKeyState.Value = KeyState.Dropped;
        KeyHolderClientId.Value = ulong.MaxValue;
        KeyWorldPosition.Value = deathPosition;
        NotifyKeyDropClientRpc(deathPosition);
        Debug.Log($"[KeyManager] Key dropped at {deathPosition}");
    }

    [ClientRpc]
    private void NotifyKeyDropClientRpc(Vector3 dropPosition)
    {
        OnKeyDropped?.Invoke(dropPosition);

        // Show world key at new position
        if (spawnedKeyInstance != null)
        {
            spawnedKeyInstance.transform.position = dropPosition;
            spawnedKeyInstance.SetActive(true);
        }
    }

    public bool IsKeyHolder(ulong clientId)
    {
        return CurrentKeyState.Value == KeyState.Held && KeyHolderClientId.Value == clientId;
    }

    private void OnKeyPositionChanged(Vector3 oldPos, Vector3 newPos)
    {
        // Move world key visual if it exists and is active
        if (spawnedKeyInstance != null && spawnedKeyInstance.activeSelf)
            spawnedKeyInstance.transform.position = newPos;
    }

    private void OnKeyStateChanged(KeyState oldState, KeyState newState)
    {
        if (spawnedKeyInstance == null) return;

        spawnedKeyInstance.SetActive(newState != KeyState.Held);
    }

    /// <summary>
    /// Called by the key prefab instance to register itself.
    /// </summary>
    public void RegisterKeyInstance(GameObject keyObj)
    {
        spawnedKeyInstance = keyObj;
    }
}
