using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Unity.Netcode;
using TMPro;

/// <summary>
/// Editor script to create Key prefab, ExitGateway prefab, and scene manager objects.
/// Run via MazeRunner > Setup Key & Exit System.
/// </summary>
public class KeyExitSetup : EditorWindow
{
    [MenuItem("MazeRunner/Setup Key and Exit System")]
    public static void Setup()
    {
        CreateKeyPrefab();
        CreateExitGatewayPrefab();
        CreateSceneManagers();
        CreateMatchResultsUI();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("Key & Exit system setup complete!");
    }

    private static void CreateKeyPrefab()
    {
        // Key root
        var key = new GameObject("Key");

        // Visual - gold diamond shape (scaled cube rotated 45 degrees)
        var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "KeyVisual";
        visual.transform.SetParent(key.transform, false);
        visual.transform.localScale = new Vector3(0.4f, 0.6f, 0.4f);
        visual.transform.localRotation = Quaternion.Euler(0, 45f, 0);
        Object.DestroyImmediate(visual.GetComponent<BoxCollider>());

        // Gold emissive material
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", new Color(1f, 0.85f, 0.2f));
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(1f, 0.85f, 0.2f) * 0.5f);
        visual.GetComponent<MeshRenderer>().sharedMaterial = mat;

        if (!AssetDatabase.IsValidFolder("Assets/Materials/GameState"))
            AssetDatabase.CreateFolder("Assets/Materials", "GameState");
        AssetDatabase.CreateAsset(mat, "Assets/Materials/GameState/KeyMaterial.mat");

        // Trigger collider for pickup
        var trigger = key.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 1.5f;

        // NetworkObject
        key.AddComponent<NetworkObject>();

        // Scripts
        key.AddComponent<KeyPickupTrigger>();
        key.AddComponent<KeyProximityFeedback>();
        key.AddComponent<KeyIdleAnimation>();

        // Point light for glow
        var lightGo = new GameObject("KeyLight");
        lightGo.transform.SetParent(key.transform, false);
        lightGo.transform.localPosition = new Vector3(0, 0.5f, 0);
        var pointLight = lightGo.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.color = new Color(1f, 0.85f, 0.2f);
        pointLight.intensity = 3f;
        pointLight.range = 5f;

        // Save as prefab
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        string prefabPath = "Assets/Prefabs/Key.prefab";
        PrefabUtility.SaveAsPrefabAsset(key, prefabPath);
        Object.DestroyImmediate(key);

        Debug.Log("Key prefab created at " + prefabPath);
    }

    private static void CreateExitGatewayPrefab()
    {
        // Exit root
        var exit = new GameObject("ExitGateway");

        // Locked visual - dark doorway frame
        var lockedVisual = new GameObject("LockedVisual");
        lockedVisual.transform.SetParent(exit.transform, false);

        var leftPost = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftPost.name = "LeftPost";
        leftPost.transform.SetParent(lockedVisual.transform, false);
        leftPost.transform.localPosition = new Vector3(-1.5f, 2f, 0);
        leftPost.transform.localScale = new Vector3(0.3f, 4f, 0.3f);
        Object.DestroyImmediate(leftPost.GetComponent<BoxCollider>());

        var rightPost = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightPost.name = "RightPost";
        rightPost.transform.SetParent(lockedVisual.transform, false);
        rightPost.transform.localPosition = new Vector3(1.5f, 2f, 0);
        rightPost.transform.localScale = new Vector3(0.3f, 4f, 0.3f);
        Object.DestroyImmediate(rightPost.GetComponent<BoxCollider>());

        var topBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        topBar.name = "TopBar";
        topBar.transform.SetParent(lockedVisual.transform, false);
        topBar.transform.localPosition = new Vector3(0, 4f, 0);
        topBar.transform.localScale = new Vector3(3.3f, 0.3f, 0.3f);
        Object.DestroyImmediate(topBar.GetComponent<BoxCollider>());

        // Dark material for locked state
        var lockedMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        lockedMat.SetColor("_BaseColor", new Color(0.3f, 0.3f, 0.3f));
        leftPost.GetComponent<MeshRenderer>().sharedMaterial = lockedMat;
        rightPost.GetComponent<MeshRenderer>().sharedMaterial = lockedMat;
        topBar.GetComponent<MeshRenderer>().sharedMaterial = lockedMat;
        AssetDatabase.CreateAsset(lockedMat, "Assets/Materials/GameState/ExitLockedMaterial.mat");

        // Unlocked visual - glowing doorway (starts disabled)
        var unlockedVisual = new GameObject("UnlockedVisual");
        unlockedVisual.transform.SetParent(exit.transform, false);

        var leftPostU = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftPostU.name = "LeftPost";
        leftPostU.transform.SetParent(unlockedVisual.transform, false);
        leftPostU.transform.localPosition = new Vector3(-1.5f, 2f, 0);
        leftPostU.transform.localScale = new Vector3(0.3f, 4f, 0.3f);
        Object.DestroyImmediate(leftPostU.GetComponent<BoxCollider>());

        var rightPostU = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightPostU.name = "RightPost";
        rightPostU.transform.SetParent(unlockedVisual.transform, false);
        rightPostU.transform.localPosition = new Vector3(1.5f, 2f, 0);
        rightPostU.transform.localScale = new Vector3(0.3f, 4f, 0.3f);
        Object.DestroyImmediate(rightPostU.GetComponent<BoxCollider>());

        var topBarU = GameObject.CreatePrimitive(PrimitiveType.Cube);
        topBarU.name = "TopBar";
        topBarU.transform.SetParent(unlockedVisual.transform, false);
        topBarU.transform.localPosition = new Vector3(0, 4f, 0);
        topBarU.transform.localScale = new Vector3(3.3f, 0.3f, 0.3f);
        Object.DestroyImmediate(topBarU.GetComponent<BoxCollider>());

        // Glowing blue material for unlocked state
        var unlockedMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        unlockedMat.SetColor("_BaseColor", new Color(0.2f, 0.5f, 1f));
        unlockedMat.EnableKeyword("_EMISSION");
        unlockedMat.SetColor("_EmissionColor", new Color(0.2f, 0.5f, 1f) * 2f);
        leftPostU.GetComponent<MeshRenderer>().sharedMaterial = unlockedMat;
        rightPostU.GetComponent<MeshRenderer>().sharedMaterial = unlockedMat;
        topBarU.GetComponent<MeshRenderer>().sharedMaterial = unlockedMat;
        AssetDatabase.CreateAsset(unlockedMat, "Assets/Materials/GameState/ExitUnlockedMaterial.mat");

        unlockedVisual.SetActive(false);

        // Beacon light (starts disabled)
        var beaconGo = new GameObject("BeaconLight");
        beaconGo.transform.SetParent(exit.transform, false);
        beaconGo.transform.localPosition = new Vector3(0, 6f, 0);
        var beacon = beaconGo.AddComponent<Light>();
        beacon.type = LightType.Point;
        beacon.color = new Color(0.2f, 0.5f, 1f);
        beacon.intensity = 8f;
        beacon.range = 20f;
        beacon.enabled = false;

        // Trigger zone for escape
        var triggerZone = exit.AddComponent<BoxCollider>();
        triggerZone.isTrigger = true;
        triggerZone.size = new Vector3(3f, 4f, 3f);
        triggerZone.center = new Vector3(0, 2f, 0);

        // NetworkObject
        exit.AddComponent<NetworkObject>();

        // ExitGateway script
        var gateway = exit.AddComponent<ExitGateway>();
        var gatewaySO = new SerializedObject(gateway);
        gatewaySO.FindProperty("lockedVisual").objectReferenceValue = lockedVisual;
        gatewaySO.FindProperty("unlockedVisual").objectReferenceValue = unlockedVisual;
        gatewaySO.FindProperty("beaconLight").objectReferenceValue = beacon;
        gatewaySO.ApplyModifiedProperties();

        // Save as prefab
        string prefabPath = "Assets/Prefabs/ExitGateway.prefab";
        PrefabUtility.SaveAsPrefabAsset(exit, prefabPath);
        Object.DestroyImmediate(exit);

        Debug.Log("ExitGateway prefab created at " + prefabPath);
    }

    private static void CreateSceneManagers()
    {
        // KeyManager
        if (Object.FindFirstObjectByType<KeyManager>() == null)
        {
            var kmGo = new GameObject("KeyManager");
            var km = kmGo.AddComponent<KeyManager>();
            var keyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Key.prefab");
            if (keyPrefab != null)
            {
                var kmSO = new SerializedObject(km);
                kmSO.FindProperty("keyPrefab").objectReferenceValue = keyPrefab;
                kmSO.ApplyModifiedProperties();
            }
            kmGo.AddComponent<NetworkObject>();
            Debug.Log("KeyManager added to scene.");
        }

        // MatchManager
        if (Object.FindFirstObjectByType<MatchManager>() == null)
        {
            var mmGo = new GameObject("MatchManager");
            mmGo.AddComponent<MatchManager>();
            mmGo.AddComponent<NetworkObject>();
            Debug.Log("MatchManager added to scene.");
        }

        // Register prefabs with NetworkManager
        var nm = Object.FindFirstObjectByType<NetworkManager>();
        if (nm != null)
        {
            var keyPrefabRef = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Key.prefab");
            var exitPrefabRef = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ExitGateway.prefab");

            bool hasKey = false, hasExit = false;
            foreach (var p in nm.NetworkConfig.Prefabs.Prefabs)
            {
                if (p.Prefab == keyPrefabRef) hasKey = true;
                if (p.Prefab == exitPrefabRef) hasExit = true;
            }

            if (!hasKey && keyPrefabRef != null)
            {
                nm.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = keyPrefabRef });
                Debug.Log("Key prefab registered with NetworkManager.");
            }
            if (!hasExit && exitPrefabRef != null)
            {
                nm.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = exitPrefabRef });
                Debug.Log("ExitGateway prefab registered with NetworkManager.");
            }

            EditorUtility.SetDirty(nm);
        }
    }

    private static void CreateMatchResultsUI()
    {
        // Find existing HUD canvas on player prefab - we'll add to it via a separate scene canvas
        // Create a scene-level UI canvas for match results (not per-player)
        if (Object.FindFirstObjectByType<MatchResultsUI>() != null) return;

        var canvasGo = new GameObject("MatchResultsCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200; // Above player HUD

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Results panel (starts hidden)
        var panel = new GameObject("ResultsPanel");
        panel.transform.SetParent(canvasGo.transform, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0, 0, 0, 0.8f);
        panelImg.raycastTarget = false;

        // Outcome text
        var outcomeGo = new GameObject("OutcomeText");
        outcomeGo.transform.SetParent(panel.transform, false);
        var outcomeRect = outcomeGo.AddComponent<RectTransform>();
        outcomeRect.anchorMin = new Vector2(0.5f, 0.5f);
        outcomeRect.anchorMax = new Vector2(0.5f, 0.5f);
        outcomeRect.pivot = new Vector2(0.5f, 0.5f);
        outcomeRect.anchoredPosition = new Vector2(0, 60);
        outcomeRect.sizeDelta = new Vector2(800, 80);
        var outcomeTmp = outcomeGo.AddComponent<TextMeshProUGUI>();
        outcomeTmp.text = "MATCH RESULT";
        outcomeTmp.fontSize = 56;
        outcomeTmp.color = Color.white;
        outcomeTmp.alignment = TextAlignmentOptions.Center;

        // Winner text
        var winnerGo = new GameObject("WinnerText");
        winnerGo.transform.SetParent(panel.transform, false);
        var winnerRect = winnerGo.AddComponent<RectTransform>();
        winnerRect.anchorMin = new Vector2(0.5f, 0.5f);
        winnerRect.anchorMax = new Vector2(0.5f, 0.5f);
        winnerRect.pivot = new Vector2(0.5f, 0.5f);
        winnerRect.anchoredPosition = new Vector2(0, -10);
        winnerRect.sizeDelta = new Vector2(800, 50);
        var winnerTmp = winnerGo.AddComponent<TextMeshProUGUI>();
        winnerTmp.text = "";
        winnerTmp.fontSize = 36;
        winnerTmp.color = Color.white;
        winnerTmp.alignment = TextAlignmentOptions.Center;

        // Duration text
        var durationGo = new GameObject("DurationText");
        durationGo.transform.SetParent(panel.transform, false);
        var durationRect = durationGo.AddComponent<RectTransform>();
        durationRect.anchorMin = new Vector2(0.5f, 0.5f);
        durationRect.anchorMax = new Vector2(0.5f, 0.5f);
        durationRect.pivot = new Vector2(0.5f, 0.5f);
        durationRect.anchoredPosition = new Vector2(0, -60);
        durationRect.sizeDelta = new Vector2(800, 40);
        var durationTmp = durationGo.AddComponent<TextMeshProUGUI>();
        durationTmp.text = "";
        durationTmp.fontSize = 24;
        durationTmp.color = Color.gray;
        durationTmp.alignment = TextAlignmentOptions.Center;

        panel.SetActive(false);

        // Wire up MatchResultsUI
        var resultsUI = canvasGo.AddComponent<MatchResultsUI>();
        var uiSO = new SerializedObject(resultsUI);
        uiSO.FindProperty("resultsPanel").objectReferenceValue = panel;
        uiSO.FindProperty("outcomeText").objectReferenceValue = outcomeTmp;
        uiSO.FindProperty("winnerText").objectReferenceValue = winnerTmp;
        uiSO.FindProperty("durationText").objectReferenceValue = durationTmp;
        uiSO.ApplyModifiedProperties();

        // Escape Progress UI (same canvas)
        var progressPanel = new GameObject("EscapeProgressPanel");
        progressPanel.transform.SetParent(canvasGo.transform, false);
        var ppRect = progressPanel.AddComponent<RectTransform>();
        ppRect.anchorMin = new Vector2(0.5f, 0.3f);
        ppRect.anchorMax = new Vector2(0.5f, 0.3f);
        ppRect.pivot = new Vector2(0.5f, 0.5f);
        ppRect.anchoredPosition = Vector2.zero;
        ppRect.sizeDelta = new Vector2(400, 50);

        // Progress bar background
        var progBg = new GameObject("Background");
        progBg.transform.SetParent(progressPanel.transform, false);
        var progBgRect = progBg.AddComponent<RectTransform>();
        progBgRect.anchorMin = Vector2.zero;
        progBgRect.anchorMax = Vector2.one;
        progBgRect.sizeDelta = Vector2.zero;
        var progBgImg = progBg.AddComponent<Image>();
        progBgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        // Progress bar fill
        var progFill = new GameObject("Fill");
        progFill.transform.SetParent(progressPanel.transform, false);
        var progFillRect = progFill.AddComponent<RectTransform>();
        progFillRect.anchorMin = Vector2.zero;
        progFillRect.anchorMax = Vector2.one;
        progFillRect.sizeDelta = Vector2.zero;
        var progFillImg = progFill.AddComponent<Image>();
        progFillImg.color = new Color(0.2f, 0.5f, 1f);
        progFillImg.type = Image.Type.Filled;
        progFillImg.fillMethod = Image.FillMethod.Horizontal;
        progFillImg.fillAmount = 0f;

        // Status text
        var statusGo = new GameObject("StatusText");
        statusGo.transform.SetParent(progressPanel.transform, false);
        var statusRect = statusGo.AddComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.5f, 1f);
        statusRect.anchorMax = new Vector2(0.5f, 1f);
        statusRect.pivot = new Vector2(0.5f, 0f);
        statusRect.anchoredPosition = new Vector2(0, 5);
        statusRect.sizeDelta = new Vector2(400, 30);
        var statusTmp = statusGo.AddComponent<TextMeshProUGUI>();
        statusTmp.text = "";
        statusTmp.fontSize = 20;
        statusTmp.color = Color.white;
        statusTmp.alignment = TextAlignmentOptions.Center;

        progressPanel.SetActive(false);

        // Wire up EscapeProgressUI
        var escapeUI = canvasGo.AddComponent<EscapeProgressUI>();
        var escapeSO = new SerializedObject(escapeUI);
        escapeSO.FindProperty("progressPanel").objectReferenceValue = progressPanel;
        escapeSO.FindProperty("progressFill").objectReferenceValue = progFillImg;
        escapeSO.FindProperty("statusText").objectReferenceValue = statusTmp;
        escapeSO.ApplyModifiedProperties();

        Debug.Log("Match results and escape progress UI created.");
    }
}
