using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Collections;
using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;

/// <summary>
/// Manages lobby connections, player tracking, and scene transitions for multiplayer.
/// Lives in the MainMenu scene on the NetworkManager GameObject. Persists via DontDestroyOnLoad.
/// </summary>
public class ConnectionManager : NetworkBehaviour
{
    public const int MaxPlayers = 8;
    public const ushort DefaultPort = 7777;
    public const float ConnectionTimeout = 5f;

    public NetworkList<PlayerLobbyData> Players;

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
        Players = new NetworkList<PlayerLobbyData>();
    }

    public override void OnNetworkSpawn()
    {
        Players.OnListChanged += OnPlayersListChanged;

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        }

        if (IsClient && !IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedFromHost;
        }
    }

    public override void OnNetworkDespawn()
    {
        Players.OnListChanged -= OnPlayersListChanged;

        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        }

        if (IsClient && !IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectedFromHost;
        }
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

        matchStarted = false;

        if (!nm.StartHost())
        {
            Debug.LogError("[ConnectionManager] Failed to start host — port may be in use");
            OnConnectionFailed?.Invoke("Failed to start host. Port may already be in use.");
            return;
        }

        Debug.Log($"[ConnectionManager] Host started on port {port}");
    }

    private void OnConnectionApproval(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        // Reject if match already started
        if (matchStarted)
        {
            response.Approved = false;
            response.Reason = "Match has already started";
            Debug.Log($"[ConnectionManager] Rejected client {request.ClientNetworkId}: match started");
            return;
        }

        // Reject if full
        int currentCount = NetworkManager.Singleton.ConnectedClientsList.Count;
        if (currentCount >= MaxPlayers)
        {
            response.Approved = false;
            response.Reason = "Lobby is full";
            Debug.Log($"[ConnectionManager] Rejected client {request.ClientNetworkId}: lobby full");
            return;
        }

        response.Approved = true;
        response.CreatePlayerObject = false; // We spawn players manually after scene load

        // Add to player list
        var data = new PlayerLobbyData
        {
            ClientId = request.ClientNetworkId,
            DisplayName = $"Player {currentCount + 1}"
        };
        Players.Add(data);

        Debug.Log($"[ConnectionManager] Approved client {request.ClientNetworkId} as {data.DisplayName}");
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

        if (!nm.StartClient())
        {
            OnConnectionFailed?.Invoke("Failed to start client.");
            return;
        }

        Debug.Log($"[ConnectionManager] Connecting to {ip}:{port}...");

        // Start timeout
        if (timeoutCoroutine != null) StopCoroutine(timeoutCoroutine);
        timeoutCoroutine = StartCoroutine(ConnectionTimeoutCoroutine());
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

        // Timed out
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
        if (!IsServer) return;

        // Remove from player list
        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].ClientId == clientId)
            {
                Debug.Log($"[ConnectionManager] Client {clientId} ({Players[i].DisplayName}) disconnected");
                Players.RemoveAt(i);
                break;
            }
        }
    }

    private void OnClientDisconnectedFromHost(ulong clientId)
    {
        // This fires on clients when they get disconnected
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
            NetworkManager.Singleton.Shutdown();
        }

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
        if (!IsServer) return;

        matchStarted = true;
        Debug.Log($"[ConnectionManager] Starting match with {Players.Count} players");

        // Use NGO scene management to sync scene load to all clients
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnGameSceneLoaded;
        NetworkManager.Singleton.SceneManager.LoadScene("SampleScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    private void OnGameSceneLoaded(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode,
        System.Collections.Generic.List<ulong> clientsCompleted, System.Collections.Generic.List<ulong> clientsTimedOut)
    {
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnGameSceneLoaded;

        if (clientsTimedOut.Count > 0)
        {
            Debug.LogWarning($"[ConnectionManager] {clientsTimedOut.Count} clients timed out during scene load");
        }

        Debug.Log($"[ConnectionManager] All clients loaded game scene. Spawning players...");

        // Spawn player objects for all connected clients
        SpawnAllPlayers();
    }

    private void SpawnAllPlayers()
    {
        if (!IsServer) return;

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
            Vector3 spawnPos = new Vector3(10, 1, 10); // fallback
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
    // Player List Events
    // ──────────────────────────────────────────

    private void OnPlayersListChanged(NetworkListEvent<PlayerLobbyData> changeEvent)
    {
        OnLobbyUpdated?.Invoke();
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
