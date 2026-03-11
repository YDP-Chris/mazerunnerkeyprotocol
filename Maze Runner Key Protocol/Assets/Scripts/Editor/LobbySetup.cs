using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Editor script to create the MainMenu scene with lobby UI,
/// configure NetworkManager with ConnectionManager, and update Build Settings.
/// Run via MazeRunner > Setup Lobby menu or MCP script-execute.
/// </summary>
public static class LobbySetup
{
    [MenuItem("MazeRunner/Setup Lobby")]
    public static void SetupLobby()
    {
        CreateMainMenuScene();
        ConfigureNetworkManager();
        UpdateBuildSettings();
        Debug.Log("[LobbySetup] Lobby setup complete!");
    }

    public static void CreateMainMenuScene()
    {
        // Create new scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Remove default directional light (keep camera)
        var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var l in lights) Object.DestroyImmediate(l.gameObject);

        // ── EventSystem ──
        var eventSystemGo = new GameObject("EventSystem");
        eventSystemGo.AddComponent<EventSystem>();
        eventSystemGo.AddComponent<StandaloneInputModule>();

        // ── Canvas ──
        var canvasGo = new GameObject("LobbyCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        // ── Background ──
        var bgGo = CreateUIElement("Background", canvasGo.transform);
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0.12f, 0.12f, 0.18f, 1f);
        StretchToParent(bgGo);

        // ── Title ──
        var titleGo = CreateTextElement("Title", canvasGo.transform, "MAZE RUNNER\nKEY PROTOCOL", 48);
        var titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -40);
        titleRect.sizeDelta = new Vector2(800, 130);

        // ════════════════════════════════════════
        // Main Menu Panel
        // ════════════════════════════════════════
        var mainMenuPanel = CreatePanel("MainMenuPanel", canvasGo.transform);

        var hostBtn = CreateButton("HostButton", mainMenuPanel.transform, "HOST GAME", new Vector2(0, 40));
        var joinBtn = CreateButton("JoinButton", mainMenuPanel.transform, "JOIN GAME", new Vector2(0, -40));

        // ════════════════════════════════════════
        // Join Panel
        // ════════════════════════════════════════
        var joinPanel = CreatePanel("JoinPanel", canvasGo.transform);
        joinPanel.SetActive(false);

        var joinLabel = CreateTextElement("JoinLabel", joinPanel.transform, "Enter Host IP & Port", 24);
        var joinLabelRect = joinLabel.GetComponent<RectTransform>();
        joinLabelRect.anchoredPosition = new Vector2(0, 100);
        joinLabelRect.sizeDelta = new Vector2(400, 40);

        var ipInput = CreateInputField("IPInput", joinPanel.transform, "IP Address (e.g. 192.168.1.10)", new Vector2(0, 50));
        var portInput = CreateInputField("PortInput", joinPanel.transform, "Port (7777)", new Vector2(0, -10));

        var connectBtn = CreateButton("ConnectButton", joinPanel.transform, "CONNECT", new Vector2(0, -70));
        var backBtn = CreateButton("BackButton", joinPanel.transform, "BACK", new Vector2(0, -140));

        var joinError = CreateTextElement("JoinErrorText", joinPanel.transform, "", 18);
        var joinErrorRect = joinError.GetComponent<RectTransform>();
        joinErrorRect.anchoredPosition = new Vector2(0, -200);
        joinErrorRect.sizeDelta = new Vector2(500, 40);
        joinError.GetComponent<TextMeshProUGUI>().color = new Color(1f, 0.4f, 0.4f);

        // ════════════════════════════════════════
        // Lobby Panel
        // ════════════════════════════════════════
        var lobbyPanel = CreatePanel("LobbyPanel", canvasGo.transform);
        lobbyPanel.SetActive(false);

        var connInfo = CreateTextElement("ConnectionInfoText", lobbyPanel.transform, "Host IP: ...", 22);
        var connInfoRect = connInfo.GetComponent<RectTransform>();
        connInfoRect.anchoredPosition = new Vector2(0, 150);
        connInfoRect.sizeDelta = new Vector2(600, 60);

        var playerList = CreateTextElement("PlayerListText", lobbyPanel.transform, "Players:\n", 20);
        var playerListRect = playerList.GetComponent<RectTransform>();
        playerListRect.anchoredPosition = new Vector2(0, 30);
        playerListRect.sizeDelta = new Vector2(500, 200);
        playerList.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.TopLeft;

        var startBtn = CreateButton("StartMatchButton", lobbyPanel.transform, "START MATCH", new Vector2(0, -120));
        var leaveBtn = CreateButton("LeaveButton", lobbyPanel.transform, "LEAVE", new Vector2(0, -190));

        var lobbyStatus = CreateTextElement("LobbyStatusText", lobbyPanel.transform, "Waiting for players...", 18);
        var lobbyStatusRect = lobbyStatus.GetComponent<RectTransform>();
        lobbyStatusRect.anchoredPosition = new Vector2(0, -250);
        lobbyStatusRect.sizeDelta = new Vector2(500, 40);

        // ════════════════════════════════════════
        // LobbyUI Component
        // ════════════════════════════════════════
        var lobbyUI = canvasGo.AddComponent<LobbyUI>();

        // Assign references via SerializedObject
        var so = new SerializedObject(lobbyUI);
        so.FindProperty("mainMenuPanel").objectReferenceValue = mainMenuPanel;
        so.FindProperty("joinPanel").objectReferenceValue = joinPanel;
        so.FindProperty("lobbyPanel").objectReferenceValue = lobbyPanel;
        so.FindProperty("hostButton").objectReferenceValue = hostBtn.GetComponent<Button>();
        so.FindProperty("joinButton").objectReferenceValue = joinBtn.GetComponent<Button>();
        so.FindProperty("ipInputField").objectReferenceValue = ipInput.GetComponent<TMP_InputField>();
        so.FindProperty("portInputField").objectReferenceValue = portInput.GetComponent<TMP_InputField>();
        so.FindProperty("connectButton").objectReferenceValue = connectBtn.GetComponent<Button>();
        so.FindProperty("backButton").objectReferenceValue = backBtn.GetComponent<Button>();
        so.FindProperty("joinErrorText").objectReferenceValue = joinError.GetComponent<TextMeshProUGUI>();
        so.FindProperty("connectionInfoText").objectReferenceValue = connInfo.GetComponent<TextMeshProUGUI>();
        so.FindProperty("playerListText").objectReferenceValue = playerList.GetComponent<TextMeshProUGUI>();
        so.FindProperty("startMatchButton").objectReferenceValue = startBtn.GetComponent<Button>();
        so.FindProperty("leaveButton").objectReferenceValue = leaveBtn.GetComponent<Button>();
        so.FindProperty("lobbyStatusText").objectReferenceValue = lobbyStatus.GetComponent<TextMeshProUGUI>();
        so.ApplyModifiedProperties();

        // Save scene
        string scenePath = "Assets/Scenes/MainMenu.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.Refresh();

        Debug.Log($"[LobbySetup] MainMenu scene created at {scenePath}");
    }

    public static void ConfigureNetworkManager()
    {
        // Open the game scene to add ConnectionManager to the NetworkManager
        string gameScenePath = "Assets/Scenes/SampleScene.unity";
        string mainMenuPath = "Assets/Scenes/MainMenu.unity";

        // Open MainMenu scene to add NetworkManager there
        EditorSceneManager.OpenScene(mainMenuPath);

        // Check for existing NetworkManager
        var existingNM = Object.FindAnyObjectByType<Unity.Netcode.NetworkManager>();
        if (existingNM != null)
        {
            // Add ConnectionManager if not present
            if (existingNM.GetComponent<ConnectionManager>() == null)
            {
                existingNM.gameObject.AddComponent<ConnectionManager>();
                Debug.Log("[LobbySetup] Added ConnectionManager to existing NetworkManager");
            }
            EditorSceneManager.SaveOpenScenes();
            return;
        }

        // Create NetworkManager in MainMenu scene
        var nmGo = new GameObject("NetworkManager");
        var nm = nmGo.AddComponent<Unity.Netcode.NetworkManager>();
        var transport = nmGo.AddComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
        nm.NetworkConfig.NetworkTransport = transport;

        // Load player prefab
        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
        if (playerPrefab != null)
        {
            nm.NetworkConfig.PlayerPrefab = playerPrefab;
        }
        else
        {
            Debug.LogWarning("[LobbySetup] Player prefab not found at Assets/Prefabs/Player.prefab");
        }

        // Add ConnectionManager
        nmGo.AddComponent<ConnectionManager>();

        // Don't destroy on load — persists through scene transitions
        // (NGO NetworkManager already handles DontDestroyOnLoad)

        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[LobbySetup] NetworkManager + ConnectionManager created in MainMenu scene");
    }

    public static void UpdateBuildSettings()
    {
        var scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", true),
        };
        EditorBuildSettings.scenes = scenes;
        Debug.Log("[LobbySetup] Build Settings updated: MainMenu (0), SampleScene (1)");
    }

    // ════════════════════════════════════════
    // UI Helpers
    // ════════════════════════════════════════

    private static GameObject CreatePanel(string name, Transform parent)
    {
        var go = CreateUIElement(name, parent);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(600, 500);
        rect.anchoredPosition = Vector2.zero;
        return go;
    }

    private static GameObject CreateButton(string name, Transform parent, string label, Vector2 position)
    {
        var go = CreateUIElement(name, parent);
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 50);
        rect.anchoredPosition = position;

        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.4f, 0.7f, 1f);

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.3f, 0.5f, 0.8f);
        colors.pressedColor = new Color(0.15f, 0.3f, 0.55f);
        btn.colors = colors;

        var textGo = CreateTextElement("Text", go.transform, label, 22);
        StretchToParent(textGo);

        return go;
    }

    private static GameObject CreateInputField(string name, Transform parent, string placeholder, Vector2 position)
    {
        var go = CreateUIElement(name, parent);
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 45);
        rect.anchoredPosition = position;

        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.25f, 1f);

        // Text area
        var textArea = CreateUIElement("Text Area", go.transform);
        var textAreaRect = textArea.GetComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(10, 5);
        textAreaRect.offsetMax = new Vector2(-10, -5);

        // Placeholder
        var phGo = CreateTextElement("Placeholder", textArea.transform, placeholder, 18);
        StretchToParent(phGo);
        var phText = phGo.GetComponent<TextMeshProUGUI>();
        phText.fontStyle = FontStyles.Italic;
        phText.color = new Color(0.5f, 0.5f, 0.5f);

        // Input text
        var inputTextGo = CreateTextElement("Text", textArea.transform, "", 18);
        StretchToParent(inputTextGo);

        // TMP_InputField
        var inputField = go.AddComponent<TMP_InputField>();
        inputField.textViewport = textAreaRect;
        inputField.textComponent = inputTextGo.GetComponent<TextMeshProUGUI>();
        inputField.placeholder = phText;

        return go;
    }

    private static GameObject CreateTextElement(string name, Transform parent, string text, int fontSize)
    {
        var go = CreateUIElement(name, parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        return go;
    }

    private static GameObject CreateUIElement(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.AddComponent<RectTransform>();
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void StretchToParent(GameObject go)
    {
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
