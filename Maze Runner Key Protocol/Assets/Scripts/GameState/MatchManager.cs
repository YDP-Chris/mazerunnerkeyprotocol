using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections;
using System.Collections.Generic;

public class MatchManager : NetworkBehaviour
{
    public enum MatchOutcome : byte { InProgress, Win, Draw }

    [Header("Settings")]
    [SerializeField] private float inputFreezeDelay = 0.5f;

    public NetworkVariable<MatchOutcome> CurrentOutcome = new NetworkVariable<MatchOutcome>(
        MatchOutcome.InProgress, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<ulong> WinnerClientId = new NetworkVariable<ulong>(
        ulong.MaxValue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<float> MatchDuration = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private float matchStartTime;
    private int alivePlayerCount;
    private bool matchEnded;

    public event Action<MatchOutcome, ulong> OnMatchEnded; // outcome, winnerClientId
    public static MatchManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            matchStartTime = Time.time;
            alivePlayerCount = NetworkManager.Singleton.ConnectedClientsList.Count;
            Debug.Log($"[MatchManager] Match started with {alivePlayerCount} players");
        }

        // Subscribe to exit escape completion
        if (ExitGateway.Instance != null)
            ExitGateway.Instance.OnEscapeComplete += OnEscapeComplete;

    }

    public override void OnNetworkDespawn()
    {
        if (ExitGateway.Instance != null)
            ExitGateway.Instance.OnEscapeComplete -= OnEscapeComplete;

        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (IsServer && !matchEnded)
        {
            MatchDuration.Value = Time.time - matchStartTime;
        }
    }

    /// <summary>
    /// Called by health system when a player is eliminated.
    /// </summary>
    public void OnPlayerEliminated(ulong eliminatedClientId)
    {
        if (!IsServer || matchEnded) return;

        alivePlayerCount--;
        Debug.Log($"[MatchManager] Player {eliminatedClientId} eliminated. {alivePlayerCount} alive.");

        // Check if eliminated player held the key
        var keyMgr = KeyManager.Instance;
        if (keyMgr != null && keyMgr.IsKeyHolder(eliminatedClientId))
        {
            // Get death position from the player's NetworkObject
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                GetNetworkObjectIdForClient(eliminatedClientId), out var netObj))
            {
                keyMgr.DropKey(netObj.transform.position);
            }

            // Cancel escape if in progress
            var exit = ExitGateway.Instance;
            if (exit != null)
                exit.CancelEscape();
        }

        // Check draw condition
        if (alivePlayerCount <= 0)
        {
            EndMatch(MatchOutcome.Draw, ulong.MaxValue);
        }
    }

    private ulong GetNetworkObjectIdForClient(ulong clientId)
    {
        foreach (var kvp in NetworkManager.Singleton.SpawnManager.SpawnedObjects)
        {
            if (kvp.Value.OwnerClientId == clientId && kvp.Value.GetComponent<PlayerHealth>() != null)
                return kvp.Key;
        }
        return 0;
    }

    private void OnEscapeComplete(ulong winnerClientId)
    {
        if (!IsServer) return;
        EndMatch(MatchOutcome.Win, winnerClientId);
    }

    private void EndMatch(MatchOutcome outcome, ulong winnerClientId)
    {
        if (matchEnded) return;
        matchEnded = true;

        CurrentOutcome.Value = outcome;
        WinnerClientId.Value = winnerClientId;
        MatchDuration.Value = Time.time - matchStartTime;

        string outcomeStr = outcome == MatchOutcome.Win
            ? $"Win by client {winnerClientId}"
            : "Draw - No Escape";
        Debug.Log($"[MatchManager] Match ended: {outcomeStr} (Duration: {MatchDuration.Value:F1}s)");

        MatchEndClientRpc(outcome, winnerClientId, MatchDuration.Value);
    }

    [ClientRpc]
    private void MatchEndClientRpc(MatchOutcome outcome, ulong winnerClientId, float duration)
    {
        OnMatchEnded?.Invoke(outcome, winnerClientId);
        StartCoroutine(FreezeAfterDelay());
    }

    private IEnumerator FreezeAfterDelay()
    {
        yield return new WaitForSeconds(inputFreezeDelay);
        FreezeAllPlayers();
    }

    private void FreezeAllPlayers()
    {
        // Disable all player input on this client
        foreach (var kvp in NetworkManager.Singleton.SpawnManager.SpawnedObjects)
        {
            var movement = kvp.Value.GetComponent<PlayerMovement>();
            if (movement != null) movement.enabled = false;

            var combat = kvp.Value.GetComponent<PlayerCombat>();
            if (combat != null) combat.enabled = false;

            var camera = kvp.Value.GetComponent<PlayerCameraController>();
            if (camera != null) camera.enabled = false;
        }

        Debug.Log("[MatchManager] All players frozen");
    }
}
