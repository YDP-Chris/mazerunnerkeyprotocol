using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Unity.Netcode.Components;
using TMPro;

/// <summary>
/// Editor script to add NetworkTransform, player body visuals, and nameplate
/// to the existing Player prefab for Phase 3 multiplayer sync.
/// Run via MazeRunner > Setup Multiplayer Visuals menu or MCP script-execute.
/// </summary>
public static class MultiplayerVisualSetup
{
    [MenuItem("MazeRunner/Setup Multiplayer Visuals")]
    public static void Setup()
    {
        string prefabPath = "Assets/Prefabs/Player.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError("[MultiplayerVisualSetup] Player prefab not found at " + prefabPath);
            return;
        }

        // Open prefab for editing
        var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

        AddNetworkTransform(prefabRoot);
        SetupNameplate(prefabRoot);
        AddRemotePlayerVisuals(prefabRoot);
        CreatePlayerColorMaterials();

        // Save prefab
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        Debug.Log("[MultiplayerVisualSetup] Multiplayer visuals setup complete!");
    }

    private static void AddNetworkTransform(GameObject player)
    {
        // Add NetworkTransform if not present
        var nt = player.GetComponent<NetworkTransform>();
        if (nt == null)
        {
            nt = player.AddComponent<NetworkTransform>();
        }

        // Configure via SerializedObject for proper serialization
        var so = new SerializedObject(nt);

        // Owner-authoritative is the default in NGO — the owner writes, others read
        // Interpolation is enabled by default
        // Sync position (all axes) and rotation (Y only for yaw)
        var syncRotX = so.FindProperty("SyncRotAngleX");
        var syncRotZ = so.FindProperty("SyncRotAngleZ");
        if (syncRotX != null) syncRotX.boolValue = false;
        if (syncRotZ != null) syncRotZ.boolValue = false;

        // Scale sync not needed
        var syncScaleX = so.FindProperty("SyncScaleX");
        var syncScaleY = so.FindProperty("SyncScaleY");
        var syncScaleZ = so.FindProperty("SyncScaleZ");
        if (syncScaleX != null) syncScaleX.boolValue = false;
        if (syncScaleY != null) syncScaleY.boolValue = false;
        if (syncScaleZ != null) syncScaleZ.boolValue = false;

        so.ApplyModifiedProperties();

        Debug.Log("[MultiplayerVisualSetup] NetworkTransform added (owner-authoritative, Y rotation only)");
    }

    private static void SetupNameplate(GameObject player)
    {
        // Check if nameplate already exists
        var existing = player.transform.Find("NameplateCanvas");
        if (existing != null)
        {
            Debug.Log("[MultiplayerVisualSetup] Nameplate already exists, skipping");
            return;
        }

        // World Space Canvas above player
        var canvasGo = new GameObject("NameplateCanvas");
        canvasGo.transform.SetParent(player.transform, false);
        canvasGo.transform.localPosition = new Vector3(0, 2.5f, 0);
        canvasGo.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f); // Scale down for world space

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var rectTransform = canvasGo.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200, 50);

        // Billboard behavior
        canvasGo.AddComponent<BillboardCanvas>();

        // Text element
        var textGo = new GameObject("NameplateText");
        textGo.transform.SetParent(canvasGo.transform, false);
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = "Player";
        tmp.fontSize = 36;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        Debug.Log("[MultiplayerVisualSetup] Nameplate canvas added");
    }

    private static void AddRemotePlayerVisuals(GameObject player)
    {
        var rpv = player.GetComponent<RemotePlayerVisuals>();
        if (rpv == null)
        {
            rpv = player.AddComponent<RemotePlayerVisuals>();
        }

        var so = new SerializedObject(rpv);

        // Find body renderer (PlayerModel child)
        var model = player.transform.Find("PlayerModel");
        if (model != null)
        {
            var renderer = model.GetComponent<MeshRenderer>();
            so.FindProperty("bodyRenderer").objectReferenceValue = renderer;
        }

        // Find nameplate
        var nameplate = player.transform.Find("NameplateCanvas");
        if (nameplate != null)
        {
            so.FindProperty("nameplateCanvas").objectReferenceValue = nameplate.GetComponent<Canvas>();
            var textGo = nameplate.Find("NameplateText");
            if (textGo != null)
            {
                so.FindProperty("nameplateText").objectReferenceValue = textGo.GetComponent<TextMeshProUGUI>();
            }
        }

        so.ApplyModifiedProperties();
        Debug.Log("[MultiplayerVisualSetup] RemotePlayerVisuals component wired up");
    }

    private static void CreatePlayerColorMaterials()
    {
        // Ensure directory exists
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Players"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");
            AssetDatabase.CreateFolder("Assets/Materials", "Players");
        }

        var colors = new (string name, Color color)[]
        {
            ("Blue", new Color(0.2f, 0.5f, 0.9f)),
            ("Red", new Color(0.9f, 0.2f, 0.2f)),
            ("Green", new Color(0.2f, 0.8f, 0.3f)),
            ("Yellow", new Color(0.9f, 0.8f, 0.1f)),
            ("Purple", new Color(0.8f, 0.3f, 0.8f)),
            ("Orange", new Color(0.9f, 0.5f, 0.1f)),
            ("Cyan", new Color(0.1f, 0.8f, 0.8f)),
            ("Pink", new Color(0.9f, 0.4f, 0.6f)),
        };

        foreach (var (name, color) in colors)
        {
            string path = $"Assets/Materials/Players/Player{name}.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) continue;

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(mat, path);
        }

        AssetDatabase.Refresh();
        Debug.Log("[MultiplayerVisualSetup] Player color materials created");
    }
}
