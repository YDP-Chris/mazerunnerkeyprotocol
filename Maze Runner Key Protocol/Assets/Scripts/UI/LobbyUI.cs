using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

/// <summary>
/// UI controller for the MainMenu/Lobby scene.
/// Manages three panels: MainMenu, Join, and Lobby.
/// </summary>
public class LobbyUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject joinPanel;
    [SerializeField] private GameObject lobbyPanel;

    [Header("Main Menu")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;

    [Header("Join Panel")]
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private TMP_InputField portInputField;
    [SerializeField] private Button connectButton;
    [SerializeField] private Button backButton;
    [SerializeField] private TextMeshProUGUI joinErrorText;

    [Header("Lobby Panel")]
    [SerializeField] private TextMeshProUGUI connectionInfoText;
    [SerializeField] private TextMeshProUGUI playerListText;
    [SerializeField] private Button startMatchButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private TextMeshProUGUI lobbyStatusText;

    private ConnectionManager connectionManager;

    private void Start()
    {
        // Unlock cursor for menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Wire buttons
        hostButton.onClick.AddListener(OnHostClicked);
        joinButton.onClick.AddListener(OnJoinClicked);
        connectButton.onClick.AddListener(OnConnectClicked);
        backButton.onClick.AddListener(OnBackClicked);
        startMatchButton.onClick.AddListener(OnStartMatchClicked);
        leaveButton.onClick.AddListener(OnLeaveClicked);

        // Default state
        ShowPanel(mainMenuPanel);

        // Set default IP
        if (ipInputField != null)
            ipInputField.text = "127.0.0.1";
        if (portInputField != null)
            portInputField.text = ConnectionManager.DefaultPort.ToString();
        if (joinErrorText != null)
            joinErrorText.text = "";
    }

    private void OnEnable()
    {
        SubscribeToConnectionManager();
    }

    private void OnDisable()
    {
        UnsubscribeFromConnectionManager();
    }

    private void SubscribeToConnectionManager()
    {
        if (ConnectionManager.Instance != null)
        {
            connectionManager = ConnectionManager.Instance;
            connectionManager.OnLobbyUpdated += RefreshLobbyUI;
            connectionManager.OnConnectionFailed += OnConnectionFailed;
            connectionManager.OnHostDisconnected += OnHostDisconnectedHandler;
        }
    }

    private void UnsubscribeFromConnectionManager()
    {
        if (connectionManager != null)
        {
            connectionManager.OnLobbyUpdated -= RefreshLobbyUI;
            connectionManager.OnConnectionFailed -= OnConnectionFailed;
            connectionManager.OnHostDisconnected -= OnHostDisconnectedHandler;
        }
    }

    // ──────────────────────────────────────────
    // Button Handlers
    // ──────────────────────────────────────────

    private void OnHostClicked()
    {
        ushort port = ConnectionManager.DefaultPort;
        if (portInputField != null && ushort.TryParse(portInputField.text, out ushort parsed))
            port = parsed;

        // ConnectionManager may not exist yet if NetworkManager hasn't spawned it
        var nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Debug.LogError("[LobbyUI] No NetworkManager found!");
            return;
        }

        // Get or add ConnectionManager on NetworkManager
        var cm = nm.GetComponent<ConnectionManager>();
        if (cm == null)
        {
            Debug.LogError("[LobbyUI] No ConnectionManager on NetworkManager!");
            return;
        }

        cm.HostGame(port);

        // Re-subscribe after network spawn
        StartCoroutine(WaitAndSubscribe());
        ShowPanel(lobbyPanel);
        RefreshLobbyUI();
    }

    private System.Collections.IEnumerator WaitAndSubscribe()
    {
        // Wait a frame for network to initialize
        yield return null;
        UnsubscribeFromConnectionManager();
        SubscribeToConnectionManager();
        RefreshLobbyUI();
    }

    private void OnJoinClicked()
    {
        ShowPanel(joinPanel);
        if (joinErrorText != null)
            joinErrorText.text = "";
    }

    private void OnConnectClicked()
    {
        string ip = ipInputField != null ? ipInputField.text.Trim() : "127.0.0.1";
        ushort port = ConnectionManager.DefaultPort;
        if (portInputField != null && ushort.TryParse(portInputField.text, out ushort parsed))
            port = parsed;

        if (string.IsNullOrEmpty(ip))
        {
            if (joinErrorText != null) joinErrorText.text = "Please enter an IP address";
            return;
        }

        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        var cm = nm.GetComponent<ConnectionManager>();
        if (cm == null) return;

        // Subscribe to connection failure before connecting
        cm.OnConnectionFailed += OnConnectionFailed;

        cm.JoinGame(ip, port);

        if (joinErrorText != null) joinErrorText.text = "Connecting...";

        // Wait for connection then show lobby
        StartCoroutine(WaitForConnection());
    }

    private System.Collections.IEnumerator WaitForConnection()
    {
        float elapsed = 0f;
        while (elapsed < ConnectionManager.ConnectionTimeout)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            {
                // Connected - switch to lobby
                yield return null; // Wait a frame for NetworkBehaviour spawn
                yield return null;
                UnsubscribeFromConnectionManager();
                SubscribeToConnectionManager();
                ShowPanel(lobbyPanel);
                RefreshLobbyUI();
                yield break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void OnBackClicked()
    {
        ShowPanel(mainMenuPanel);
    }

    private void OnStartMatchClicked()
    {
        if (ConnectionManager.Instance != null)
        {
            if (lobbyStatusText != null)
                lobbyStatusText.text = "Starting match...";
            ConnectionManager.Instance.StartMatch();
        }
    }

    private void OnLeaveClicked()
    {
        if (ConnectionManager.Instance != null)
            ConnectionManager.Instance.Disconnect();
        else if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown();

        ShowPanel(mainMenuPanel);
    }

    // ──────────────────────────────────────────
    // UI Refresh
    // ──────────────────────────────────────────

    private void RefreshLobbyUI()
    {
        if (ConnectionManager.Instance == null) return;
        var cm = ConnectionManager.Instance;

        // Connection info
        if (connectionInfoText != null)
        {
            if (cm.IsHost)
            {
                string localIP = ConnectionManager.GetLocalIPAddress();
                connectionInfoText.text = $"Host IP: {localIP}:{ConnectionManager.DefaultPort}\nShare this with other players";
            }
            else
            {
                connectionInfoText.text = "Connected to host";
            }
        }

        // Player list
        if (playerListText != null)
        {
            string list = "";
            for (int i = 0; i < cm.Players.Count; i++)
            {
                var p = cm.Players[i];
                string prefix = (p.ClientId == NetworkManager.Singleton.LocalClientId) ? "> " : "  ";
                list += $"{prefix}{p.DisplayName} (ID: {p.ClientId})\n";
            }
            playerListText.text = list;
        }

        // Start match button - host only
        if (startMatchButton != null)
        {
            startMatchButton.gameObject.SetActive(cm.IsHost);
        }

        // Status
        if (lobbyStatusText != null && cm.IsHost)
        {
            lobbyStatusText.text = $"Waiting for players... ({cm.Players.Count}/{ConnectionManager.MaxPlayers})";
        }
    }

    private void OnConnectionFailed(string reason)
    {
        if (joinErrorText != null)
            joinErrorText.text = reason;
        ShowPanel(joinPanel);
    }

    private void OnHostDisconnectedHandler()
    {
        ShowPanel(mainMenuPanel);
    }

    // ──────────────────────────────────────────
    // Panel Management
    // ──────────────────────────────────────────

    private void ShowPanel(GameObject panel)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(panel == mainMenuPanel);
        if (joinPanel != null) joinPanel.SetActive(panel == joinPanel);
        if (lobbyPanel != null) lobbyPanel.SetActive(panel == lobbyPanel);
    }
}
