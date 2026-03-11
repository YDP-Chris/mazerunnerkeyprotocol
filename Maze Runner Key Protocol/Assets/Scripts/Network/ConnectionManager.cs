using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Collections;
using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages lobby connections, player tracking, and scene transitions for multiplayer.
/// Lives in the MainMenu scene on the NetworkManager GameObject. Persists via DontDestroyOnLoad.
/// Uses MonoBehaviour (not NetworkBehaviour) because it lives on the NetworkManager GameObject
/// which is not a spawned NetworkObject.
/// </summary>
public class ConnectionManager : MonoBehaviour
{
    public const int MaxPlayers = 8;
    public const ushort DefaultPort = 7777;
    public const float ConnectionTimeout = 15f;

    /// <summary>
    /// Host-authoritative player list. On clients, synced via custom messages.
    /// </summary>
    public List<PlayerLobbyData> Players = new List<PlayerLobbyData>();

    public static ConnectionManager Instance { get; private set; }

    public event Action OnLobbyUpdated;
    public event Action<string> OnConnectionFailed;
    public event Action OnHostDisconnected;

    private bool matchStarted;
    private Coroutine timeoutCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ──────────────────────────────────────────
    // Host
    // ──────────────────────────────────────────

    public void HostGame(ushort port = DefaultPort)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        var transport = nm.GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.SetConnectionData("0.0.0.0", port);
        }

        nm.NetworkConfig.ConnectionApproval = true;
        nm.ConnectionApprovalCallback = OnConnectionApproval;
        nm.NetworkConfig.ForceSamePrefabs = false;
        nm.LogLevel = LogLevel.Normal;

        matchStarted = false;
        Players.Clear();

        Debug.Log($"[ConnectionManager] NetworkConfig hash: {nm.NetworkConfig.GetConfig()}");

        if (!nm.StartHost())
        {
            Debug.LogError("[ConnectionManager] Failed to start host — port may be in use");
            OnConnectionFailed?.Invoke("Failed to start host. Port may already be in use.");
            return;
        }

        // Subscribe to disconnect events
        nm.OnClientDisconnectCallback += OnClientDisconnect;

        // Register custom message handler for player list sync
        nm.CustomMessagingManager.RegisterNamedMessageHandler("PlayerListSync", OnReceivePlayerList);

        Debug.Log($"[ConnectionManager] Host started on port {port}");
    }

    private void OnConnectionApproval(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        if (matchStarted)
        {
            response.Approved = false;
            response.Reason = "Match has already started";
            Debug.Log($"[ConnectionManager] Rejected client {request.ClientNetworkId}: match started");
            return;
        }

        int currentCount = NetworkManager.Singleton.ConnectedClientsList.Count;
        if (currentCount >= MaxPlayers)
        {
            response.Approved = false;
            response.Reason = "Lobby is full";
            Debug.Log($"[ConnectionManager] Rejected client {request.ClientNetworkId}: lobby full");
            return;
        }

        response.Approved = true;
        response.CreatePlayerObject = false;

        var data = new PlayerLobbyData
        {
            ClientId = request.ClientNetworkId,
            DisplayName = $"Player {currentCount + 1}"
        };
        Players.Add(data);
        OnLobbyUpdated?.Invoke();

        // Sync updated player list to all clients
        StartCoroutine(SyncPlayerListNextFrame());

        Debug.Log($"[ConnectionManager] Approved client {request.ClientNetworkId} as {data.DisplayName}");
    }

    /// <summary>
    /// Broadcasts the player list to all connected clients via custom named messages.
    /// Delayed one frame so the newly connected client's messaging manager is ready.
    /// </summary>
    private IEnumerator SyncPlayerListNextFrame()
    {
        yield return null;
        BroadcastPlayerList();
    }

    private void BroadcastPlayerList()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return;

        using var writer = new FastBufferWriter(1024, Unity.Collections.Allocator.Temp);
        writer.WriteValueSafe(Players.Count);
        for (int i = 0; i < Players.Count; i++)
        {
            var p = Players[i];
            writer.WriteValueSafe(p.ClientId);
            writer.WriteValueSafe(p.DisplayName);
        }

        foreach (var clientId in nm.ConnectedClientsIds)
        {
            if (clientId == nm.LocalClientId) continue; // Don't send to self (host)
            nm.CustomMessagingManager.SendNamedMessage("PlayerListSync", clientId, writer);
        }
    }

    private void OnReceivePlayerList(ulong senderId, FastBufferReader reader)
    {
        reader.ReadValueSafe(out int count);
        Players.Clear();
        for (int i = 0; i < count; i++)
        {
            reader.ReadValueSafe(out ulong clientId);
            reader.ReadValueSafe(out FixedString32Bytes displayName);
            Players.Add(new PlayerLobbyData { ClientId = clientId, DisplayName = displayName });
        }
        OnLobbyUpdated?.Invoke();
    }

    // ──────────────────────────────────────────
    // Client Join
    // ──────────────────────────────────────────

    public void JoinGame(string ip, ushort port = DefaultPort)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        var transport = nm.GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.SetConnectionData(ip, port);
        }

        nm.NetworkConfig.ForceSamePrefabs = false;
        nm.NetworkConfig.ConnectionApproval = true;

        if (!nm.StartClient())
        {
            OnConnectionFailed?.Invoke("Failed to start client.");
            return;
        }

        // Subscribe to events after StartClient
        nm.OnClientConnectedCallback += OnClientConnectedAsClient;
        nm.OnClientDisconnectCallback += OnClientDisconnectedFromHost;

        Debug.Log($"[ConnectionManager] Connecting to {ip}:{port} (transport: {nm.NetworkConfig.NetworkTransport?.GetType().Name})...");

        if (timeoutCoroutine != null) StopCoroutine(timeoutCoroutine);
        timeoutCoroutine = StartCoroutine(ConnectionTimeoutCoroutine());
    }

    private void OnClientConnectedAsClient(ulong clientId)
    {
        if (NetworkManager.Singleton == null) return;
        if (clientId != NetworkManager.Singleton.LocalClientId) return;

        Debug.Log($"[ConnectionManager] Connected to host as client {clientId}");

        // Register to receive player list updates from host
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("PlayerListSync", OnReceivePlayerList);
    }

    private IEnumerator ConnectionTimeoutCoroutine()
    {
        float elapsed = 0f;
        while (elapsed < ConnectionTimeout)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            {
                timeoutCoroutine = null;
                yield break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsConnectedClient)
        {
            NetworkManager.Singleton.Shutdown();
            OnConnectionFailed?.Invoke("Connection timed out. Check the IP and port.");
            Debug.Log("[ConnectionManager] Connection timed out");
        }
        timeoutCoroutine = null;
    }

    // ──────────────────────────────────────────
    // Disconnect Handling
    // ──────────────────────────────────────────

    private void OnClientDisconnect(ulong clientId)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].ClientId == clientId)
            {
                Debug.Log($"[ConnectionManager] Client {clientId} ({Players[i].DisplayName}) disconnected");
                Players.RemoveAt(i);
                OnLobbyUpdated?.Invoke();
                BroadcastPlayerList();
                break;
            }
        }
    }

    private void OnClientDisconnectedFromHost(ulong clientId)
    {
        if (NetworkManager.Singleton == null) return;
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("[ConnectionManager] Disconnected from host");
            OnHostDisconnected?.Invoke();
            ReturnToMainMenu();
        }
    }

    public void Disconnect()
    {
        if (timeoutCoroutine != null)
        {
            StopCoroutine(timeoutCoroutine);
            timeoutCoroutine = null;
        }

        if (NetworkManager.Singleton != null)
        {
            // Unsubscribe before shutdown
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectedFromHost;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedAsClient;
            NetworkManager.Singleton.Shutdown();
        }

        Players.Clear();
        ReturnToMainMenu();
    }

    private void ReturnToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    // ──────────────────────────────────────────
    // Match Start & Scene Transition
    // ──────────────────────────────────────────

    public void StartMatch()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("[ConnectionManager] StartMatch called but we are not the server");
            return;
        }

        matchStarted = true;
        Debug.Log($"[ConnectionManager] Starting match with {Players.Count} players");

        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnGameSceneLoaded;
        NetworkManager.Singleton.SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
    }

    private void OnGameSceneLoaded(string sceneName, LoadSceneMode loadSceneMode,
        List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnGameSceneLoaded;

        if (clientsTimedOut.Count > 0)
        {
            Debug.LogWarning($"[ConnectionManager] {clientsTimedOut.Count} clients timed out during scene load");
        }

        Debug.Log($"[ConnectionManager] All clients loaded game scene. Spawning players...");
        SpawnAllPlayers();
    }

    private void SpawnAllPlayers()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        var spawnPoints = FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None);
        var playerPrefab = NetworkManager.Singleton.NetworkConfig.PlayerPrefab;

        if (playerPrefab == null)
        {
            Debug.LogError("[ConnectionManager] No player prefab assigned in NetworkManager!");
            return;
        }

        for (int i = 0; i < Players.Count; i++)
        {
            var playerData = Players[i];
            Vector3 spawnPos = new Vector3(10, 1, 10);
            Quaternion spawnRot = Quaternion.identity;

            if (spawnPoints.Length > 0)
            {
                int spawnIndex = i % spawnPoints.Length;
                spawnPos = spawnPoints[spawnIndex].transform.position;
                spawnRot = spawnPoints[spawnIndex].transform.rotation;
            }

            var playerObj = Instantiate(playerPrefab, spawnPos, spawnRot);
            var netObj = playerObj.GetComponent<NetworkObject>();
            netObj.SpawnAsPlayerObject(playerData.ClientId);

            Debug.Log($"[ConnectionManager] Spawned {playerData.DisplayName} (client {playerData.ClientId}) at {spawnPos}");
        }
    }

    // ──────────────────────────────────────────
    // Utility
    // ──────────────────────────────────────────

    public static string GetLocalIPAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ConnectionManager] Could not get local IP: {e.Message}");
        }
        return "127.0.0.1";
    }

    public bool IsHost => NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
    public bool IsConnected => NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient;
}
