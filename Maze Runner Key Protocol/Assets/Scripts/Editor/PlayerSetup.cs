using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Unity.Netcode;
using Unity.Cinemachine;
using TMPro;

public class PlayerSetup : EditorWindow
{
    [MenuItem("MazeRunner/Setup Player Controller")]
    public static void Setup()
    {
        CreatePlayerPrefab();
        SetupNetworkManager();
        SetupTestEnemy();
        CreateImpactPrefabs();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("Player controller setup complete!");
    }

    private static void CreatePlayerPrefab()
    {
        // Create player root
        var player = new GameObject("Player");

        // CharacterController
        var cc = player.AddComponent<CharacterController>();
        cc.height = 2.0f;
        cc.radius = 0.5f;
        cc.skinWidth = 0.08f;
        cc.center = new Vector3(0, 1.0f, 0);

        // NetworkObject
        player.AddComponent<NetworkObject>();

        // Placeholder capsule model
        var model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        model.name = "PlayerModel";
        model.transform.SetParent(player.transform, false);
        model.transform.localPosition = new Vector3(0, 1.0f, 0);
        // Remove collider from model (CharacterController handles collision)
        Object.DestroyImmediate(model.GetComponent<CapsuleCollider>());
        // Give it a color
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", new Color(0.2f, 0.6f, 0.8f));
        model.GetComponent<MeshRenderer>().sharedMaterial = mat;
        AssetDatabase.CreateAsset(mat, "Assets/Materials/PlayerMaterial.mat");

        // Camera target (for orbit)
        var cameraTarget = new GameObject("CameraTarget");
        cameraTarget.transform.SetParent(player.transform, false);
        cameraTarget.transform.localPosition = new Vector3(0, 1.5f, 0);

        // Muzzle point
        var muzzle = new GameObject("MuzzlePoint");
        muzzle.transform.SetParent(player.transform, false);
        muzzle.transform.localPosition = new Vector3(0.3f, 1.2f, 0.8f);

        // Muzzle flash (simple sphere, starts disabled)
        var muzzleFlash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        muzzleFlash.name = "MuzzleFlash";
        muzzleFlash.transform.SetParent(muzzle.transform, false);
        muzzleFlash.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
        Object.DestroyImmediate(muzzleFlash.GetComponent<SphereCollider>());
        var flashMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        flashMat.SetColor("_BaseColor", new Color(1f, 0.8f, 0.2f));
        muzzleFlash.GetComponent<MeshRenderer>().sharedMaterial = flashMat;
        AssetDatabase.CreateAsset(flashMat, "Assets/Materials/MuzzleFlashMaterial.mat");
        muzzleFlash.SetActive(false);

        // AudioSource for weapon
        var audioSrc = player.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;
        audioSrc.spatialBlend = 0f;

        // Cinemachine camera (child of player for now, will be activated per owner)
        var cmCamGo = new GameObject("PlayerCamera");
        cmCamGo.transform.SetParent(player.transform, false);
        var cmCam = cmCamGo.AddComponent<CinemachineCamera>();

        // 3rd Person Follow
        var follow = cmCamGo.AddComponent<CinemachineThirdPersonFollow>();
        follow.ShoulderOffset = new Vector3(0.5f, 0f, 0f);
        follow.VerticalArmLength = 1.5f;
        follow.CameraDistance = 4.0f;
        follow.CameraSide = 0.6f;

        cmCam.Follow = cameraTarget.transform;
        cmCam.LookAt = cameraTarget.transform;

        // Deoccluder for wall collision
        cmCamGo.AddComponent<CinemachineDeoccluder>();

        // Start disabled (enabled by owner in PlayerCameraController)
        cmCamGo.SetActive(false);

        // Add player scripts
        var movement = player.AddComponent<PlayerMovement>();
        var health = player.AddComponent<PlayerHealth>();

        var cameraCtrl = player.AddComponent<PlayerCameraController>();
        // Set camera target reference
        var cameraCtrlSO = new SerializedObject(cameraCtrl);
        cameraCtrlSO.FindProperty("cameraTarget").objectReferenceValue = cameraTarget.transform;
        cameraCtrlSO.ApplyModifiedProperties();

        var combat = player.AddComponent<PlayerCombat>();
        // Set combat references
        var combatSO = new SerializedObject(combat);
        combatSO.FindProperty("muzzlePoint").objectReferenceValue = muzzle.transform;
        combatSO.FindProperty("muzzleFlashObject").objectReferenceValue = muzzleFlash;
        combatSO.FindProperty("damageableMask").intValue = 1 << LayerMask.NameToLayer("Default"); // Will be updated to proper layer
        combatSO.ApplyModifiedProperties();

        // --- HUD Canvas ---
        var canvasGo = new GameObject("HUDCanvas");
        canvasGo.transform.SetParent(player.transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGo.AddComponent<GraphicRaycaster>();

        // --- Health Bar ---
        var healthBarContainer = new GameObject("HealthBar");
        healthBarContainer.transform.SetParent(canvasGo.transform, false);
        var hbRect = healthBarContainer.AddComponent<RectTransform>();
        hbRect.anchorMin = new Vector2(0, 0);
        hbRect.anchorMax = new Vector2(0, 0);
        hbRect.pivot = new Vector2(0, 0);
        hbRect.anchoredPosition = new Vector2(20, 20);
        hbRect.sizeDelta = new Vector2(300, 30);

        // Background
        var hbBg = new GameObject("Background");
        hbBg.transform.SetParent(healthBarContainer.transform, false);
        var bgRect = hbBg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        var bgImg = hbBg.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        // Fill
        var hbFill = new GameObject("Fill");
        hbFill.transform.SetParent(healthBarContainer.transform, false);
        var fillRect = hbFill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        var fillImg = hbFill.AddComponent<Image>();
        fillImg.color = Color.green;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 1f;

        // Health text
        var healthTextGo = new GameObject("HealthText");
        healthTextGo.transform.SetParent(canvasGo.transform, false);
        var htRect = healthTextGo.AddComponent<RectTransform>();
        htRect.anchorMin = new Vector2(0, 0);
        htRect.anchorMax = new Vector2(0, 0);
        htRect.pivot = new Vector2(0, 0);
        htRect.anchoredPosition = new Vector2(330, 22);
        htRect.sizeDelta = new Vector2(120, 30);
        var healthTmp = healthTextGo.AddComponent<TextMeshProUGUI>();
        healthTmp.text = "100 / 100";
        healthTmp.fontSize = 18;
        healthTmp.color = Color.white;
        healthTmp.alignment = TextAlignmentOptions.Left;

        // --- Crosshair ---
        var crosshairGo = new GameObject("Crosshair");
        crosshairGo.transform.SetParent(canvasGo.transform, false);
        var chRect = crosshairGo.AddComponent<RectTransform>();
        chRect.anchorMin = new Vector2(0.5f, 0.5f);
        chRect.anchorMax = new Vector2(0.5f, 0.5f);
        chRect.pivot = new Vector2(0.5f, 0.5f);
        chRect.anchoredPosition = Vector2.zero;
        chRect.sizeDelta = new Vector2(4, 4);
        var chImg = crosshairGo.AddComponent<Image>();
        chImg.color = Color.white;

        // --- Damage Flash ---
        var flashGo = new GameObject("DamageFlash");
        flashGo.transform.SetParent(canvasGo.transform, false);
        var dfRect = flashGo.AddComponent<RectTransform>();
        dfRect.anchorMin = Vector2.zero;
        dfRect.anchorMax = Vector2.one;
        dfRect.sizeDelta = Vector2.zero;
        var dfImg = flashGo.AddComponent<Image>();
        dfImg.color = new Color(1f, 0f, 0f, 0f);
        dfImg.raycastTarget = false;

        // --- Elimination Panel ---
        var elimPanel = new GameObject("EliminationPanel");
        elimPanel.transform.SetParent(canvasGo.transform, false);
        var epRect = elimPanel.AddComponent<RectTransform>();
        epRect.anchorMin = Vector2.zero;
        epRect.anchorMax = Vector2.one;
        epRect.sizeDelta = Vector2.zero;
        var epImg = elimPanel.AddComponent<Image>();
        epImg.color = new Color(0f, 0f, 0f, 0.7f);
        epImg.raycastTarget = false;

        var elimText = new GameObject("EliminatedText");
        elimText.transform.SetParent(elimPanel.transform, false);
        var etRect = elimText.AddComponent<RectTransform>();
        etRect.anchorMin = new Vector2(0.5f, 0.5f);
        etRect.anchorMax = new Vector2(0.5f, 0.5f);
        etRect.pivot = new Vector2(0.5f, 0.5f);
        etRect.anchoredPosition = Vector2.zero;
        etRect.sizeDelta = new Vector2(600, 100);
        var elimTmp = elimText.AddComponent<TextMeshProUGUI>();
        elimTmp.text = "ELIMINATED";
        elimTmp.fontSize = 72;
        elimTmp.color = Color.red;
        elimTmp.alignment = TextAlignmentOptions.Center;

        elimPanel.SetActive(false);

        // Wire up PlayerUI
        var playerUI = player.AddComponent<PlayerUI>();
        var uiSO = new SerializedObject(playerUI);
        uiSO.FindProperty("healthBarFill").objectReferenceValue = fillImg;
        uiSO.FindProperty("healthBarBackground").objectReferenceValue = bgImg;
        uiSO.FindProperty("healthText").objectReferenceValue = healthTmp;
        uiSO.FindProperty("crosshairImage").objectReferenceValue = chImg;
        uiSO.FindProperty("damageFlashImage").objectReferenceValue = dfImg;
        uiSO.FindProperty("eliminationPanel").objectReferenceValue = elimPanel;
        uiSO.FindProperty("hudCanvas").objectReferenceValue = canvas;
        uiSO.ApplyModifiedProperties();

        // Save as prefab
        string prefabPath = "Assets/Prefabs/Player.prefab";
        PrefabUtility.SaveAsPrefabAsset(player, prefabPath);
        Object.DestroyImmediate(player);

        Debug.Log("Player prefab created at " + prefabPath);
    }

    private static void SetupNetworkManager()
    {
        // Check if NetworkManager already exists
        var existing = Object.FindFirstObjectByType<NetworkManager>();
        if (existing != null)
        {
            Debug.Log("NetworkManager already exists, updating player prefab reference.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            existing.NetworkConfig.PlayerPrefab = prefab;
            EditorUtility.SetDirty(existing);
            return;
        }

        var nmGo = new GameObject("NetworkManager");
        var nm = nmGo.AddComponent<NetworkManager>();

        // Add Unity Transport
        var transport = nmGo.AddComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
        nm.NetworkConfig.NetworkTransport = transport;

        // Set player prefab
        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
        nm.NetworkConfig.PlayerPrefab = playerPrefab;

        Debug.Log("NetworkManager created in scene.");
    }

    private static void SetupTestEnemy()
    {
        // Check if test enemy already exists
        if (GameObject.Find("TestEnemy") != null) return;

        var enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemy.name = "TestEnemy";
        enemy.transform.position = new Vector3(42, 1, 42); // Somewhere in the maze
        enemy.transform.localScale = new Vector3(1, 1, 1);

        // Give it a red material
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", new Color(0.8f, 0.2f, 0.2f));
        enemy.GetComponent<MeshRenderer>().sharedMaterial = mat;
        AssetDatabase.CreateAsset(mat, "Assets/Materials/EnemyMaterial.mat");

        // Add NetworkObject and Health
        enemy.AddComponent<NetworkObject>();
        enemy.AddComponent<PlayerHealth>();

        Debug.Log("Test enemy placed in maze.");
    }

    private static void CreateImpactPrefabs()
    {
        // Hit impact (damageable) - small red sphere
        if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/HitImpact.prefab") == null)
        {
            var hit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hit.name = "HitImpact";
            hit.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            Object.DestroyImmediate(hit.GetComponent<SphereCollider>());
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.SetColor("_BaseColor", Color.red);
            hit.GetComponent<MeshRenderer>().sharedMaterial = mat;
            AssetDatabase.CreateAsset(mat, "Assets/Materials/HitImpactMaterial.mat");
            PrefabUtility.SaveAsPrefabAsset(hit, "Assets/Prefabs/HitImpact.prefab");
            Object.DestroyImmediate(hit);
        }

        // Wall impact - small gray sphere
        if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/WallImpact.prefab") == null)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            wall.name = "WallImpact";
            wall.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
            Object.DestroyImmediate(wall.GetComponent<SphereCollider>());
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.SetColor("_BaseColor", new Color(0.7f, 0.7f, 0.7f));
            wall.GetComponent<MeshRenderer>().sharedMaterial = mat;
            AssetDatabase.CreateAsset(mat, "Assets/Materials/WallImpactMaterial.mat");
            PrefabUtility.SaveAsPrefabAsset(wall, "Assets/Prefabs/WallImpact.prefab");
            Object.DestroyImmediate(wall);
        }

        // Wire impact prefabs to player combat
        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
        if (playerPrefab != null)
        {
            var combat = playerPrefab.GetComponent<PlayerCombat>();
            if (combat != null)
            {
                var so = new SerializedObject(combat);
                so.FindProperty("hitImpactPrefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/HitImpact.prefab");
                so.FindProperty("wallImpactPrefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/WallImpact.prefab");
                so.ApplyModifiedProperties();
            }
        }
    }
}

