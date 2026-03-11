using UnityEngine;
using UnityEditor;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine.UI;

public class LootSetup : EditorWindow
{
    [MenuItem("MazeRunner/Setup Loot System")]
    public static void Setup()
    {
        CreateWeaponDataAssets();
        CreateSupplyDataAssets();
        CreateLootTableAsset();
        CreateLootBoxPrefab();
        CreateLootBoxSpawner();
        RegisterPrefabsWithNetworkManager();
        AddWeaponInventoryToPlayer();
        CreateWeaponHUDCanvas();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("Loot system setup complete!");
    }

    private static void CreateWeaponDataAssets()
    {
        string path = "Assets/ScriptableObjects/Weapons";
        EnsureFolder("Assets/ScriptableObjects");
        EnsureFolder(path);

        // Pistol
        if (AssetDatabase.LoadAssetAtPath<WeaponData>($"{path}/PistolData.asset") == null)
        {
            var pistol = ScriptableObject.CreateInstance<WeaponData>();
            pistol.weaponName = "Pistol";
            pistol.damage = 15f;
            pistol.fireRate = 3f;
            pistol.ammoCapacity = -1;
            pistol.maxAmmo = -1;
            pistol.spreadAngle = 0f;
            pistol.effectiveRange = 40f;
            pistol.fireMode = FireMode.Single;
            pistol.reloadTime = 0f;
            pistol.pelletCount = 1;
            AssetDatabase.CreateAsset(pistol, $"{path}/PistolData.asset");
        }

        // Shotgun
        if (AssetDatabase.LoadAssetAtPath<WeaponData>($"{path}/ShotgunData.asset") == null)
        {
            var shotgun = ScriptableObject.CreateInstance<WeaponData>();
            shotgun.weaponName = "Shotgun";
            shotgun.damage = 12f;
            shotgun.fireRate = 1f;
            shotgun.ammoCapacity = 8;
            shotgun.maxAmmo = 24;
            shotgun.spreadAngle = 15f;
            shotgun.effectiveRange = 15f;
            shotgun.fireMode = FireMode.Single;
            shotgun.reloadTime = 2f;
            shotgun.pelletCount = 7;
            AssetDatabase.CreateAsset(shotgun, $"{path}/ShotgunData.asset");
        }

        // SMG
        if (AssetDatabase.LoadAssetAtPath<WeaponData>($"{path}/SMGData.asset") == null)
        {
            var smg = ScriptableObject.CreateInstance<WeaponData>();
            smg.weaponName = "SMG";
            smg.damage = 8f;
            smg.fireRate = 10f;
            smg.ammoCapacity = 30;
            smg.maxAmmo = 90;
            smg.spreadAngle = 3f;
            smg.effectiveRange = 30f;
            smg.fireMode = FireMode.Automatic;
            smg.reloadTime = 1.5f;
            smg.pelletCount = 1;
            AssetDatabase.CreateAsset(smg, $"{path}/SMGData.asset");
        }

        // Rifle
        if (AssetDatabase.LoadAssetAtPath<WeaponData>($"{path}/RifleData.asset") == null)
        {
            var rifle = ScriptableObject.CreateInstance<WeaponData>();
            rifle.weaponName = "Rifle";
            rifle.damage = 35f;
            rifle.fireRate = 1.5f;
            rifle.ammoCapacity = 10;
            rifle.maxAmmo = 30;
            rifle.spreadAngle = 0f;
            rifle.effectiveRange = 60f;
            rifle.fireMode = FireMode.Single;
            rifle.reloadTime = 2.5f;
            rifle.pelletCount = 1;
            AssetDatabase.CreateAsset(rifle, $"{path}/RifleData.asset");
        }

        Debug.Log("Weapon data assets created.");
    }

    private static void CreateSupplyDataAssets()
    {
        string path = "Assets/ScriptableObjects";
        EnsureFolder(path);

        if (AssetDatabase.LoadAssetAtPath<AmmoPickupData>($"{path}/AmmoPickupData.asset") == null)
        {
            var ammo = ScriptableObject.CreateInstance<AmmoPickupData>();
            ammo.ammoAmount = 15;
            AssetDatabase.CreateAsset(ammo, $"{path}/AmmoPickupData.asset");
        }

        if (AssetDatabase.LoadAssetAtPath<HealthPackData>($"{path}/HealthPackData.asset") == null)
        {
            var health = ScriptableObject.CreateInstance<HealthPackData>();
            health.healAmount = 25f;
            AssetDatabase.CreateAsset(health, $"{path}/HealthPackData.asset");
        }

        Debug.Log("Supply data assets created.");
    }

    private static void CreateLootTableAsset()
    {
        string path = "Assets/ScriptableObjects/DefaultLootTable.asset";
        if (AssetDatabase.LoadAssetAtPath<LootTable>(path) != null) return;

        var table = ScriptableObject.CreateInstance<LootTable>();

        var shotgunData = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/ScriptableObjects/Weapons/ShotgunData.asset");
        var smgData = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/ScriptableObjects/Weapons/SMGData.asset");
        var rifleData = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/ScriptableObjects/Weapons/RifleData.asset");

        // 40% weapons (shotgun 15%, SMG 15%, rifle 10%), 60% supplies (ammo 35%, health 25%)
        table.entries.Add(new LootTableEntry { itemType = LootItemType.Shotgun, weaponData = shotgunData, weight = 15 });
        table.entries.Add(new LootTableEntry { itemType = LootItemType.SMG, weaponData = smgData, weight = 15 });
        table.entries.Add(new LootTableEntry { itemType = LootItemType.Rifle, weaponData = rifleData, weight = 10 });
        table.entries.Add(new LootTableEntry { itemType = LootItemType.AmmoPack, weaponData = null, weight = 35 });
        table.entries.Add(new LootTableEntry { itemType = LootItemType.HealthPack, weaponData = null, weight = 25 });

        AssetDatabase.CreateAsset(table, path);
        Debug.Log("Default loot table created.");
    }

    private static void CreateLootBoxPrefab()
    {
        string prefabPath = "Assets/Prefabs/LootBox.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;

        EnsureFolder("Assets/Prefabs");

        var box = new GameObject("LootBox");

        // Visual mesh (crate-like cube)
        var model = GameObject.CreatePrimitive(PrimitiveType.Cube);
        model.name = "LootBoxModel";
        model.transform.SetParent(box.transform, false);
        model.transform.localPosition = new Vector3(0, 0.4f, 0);
        model.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        Object.DestroyImmediate(model.GetComponent<BoxCollider>());

        // Material - green glow
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", new Color(0.2f, 0.8f, 0.3f));
        mat.SetColor("_EmissionColor", new Color(0.1f, 0.4f, 0.15f));
        mat.EnableKeyword("_EMISSION");
        model.GetComponent<MeshRenderer>().sharedMaterial = mat;

        EnsureFolder("Assets/Materials/Loot");
        AssetDatabase.CreateAsset(mat, "Assets/Materials/Loot/LootBoxMaterial.mat");

        // Glow light
        var lightGo = new GameObject("GlowLight");
        lightGo.transform.SetParent(box.transform, false);
        lightGo.transform.localPosition = new Vector3(0, 1.2f, 0);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.3f, 1f, 0.4f);
        light.intensity = 1.5f;
        light.range = 5f;

        // Trigger collider for interaction
        var trigger = box.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 2f;
        trigger.center = new Vector3(0, 0.4f, 0);

        // Physics collider (non-trigger)
        var physCol = box.AddComponent<BoxCollider>();
        physCol.center = new Vector3(0, 0.4f, 0);
        physCol.size = new Vector3(0.8f, 0.8f, 0.8f);

        // Network components
        box.AddComponent<NetworkObject>();
        box.AddComponent<NetworkTransform>();

        // LootBox script
        var lootBox = box.AddComponent<LootBox>();

        // Wire light reference
        var so = new SerializedObject(lootBox);
        so.FindProperty("glowLight").objectReferenceValue = light;
        so.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(box, prefabPath);
        Object.DestroyImmediate(box);
        Debug.Log("LootBox prefab created.");
    }

    private static void CreateLootBoxSpawner()
    {
        if (Object.FindFirstObjectByType<LootBoxSpawner>() != null) return;

        var spawnerGo = new GameObject("LootBoxSpawner");
        var spawner = spawnerGo.AddComponent<LootBoxSpawner>();
        spawnerGo.AddComponent<NetworkObject>();

        var lootBoxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LootBox.prefab");
        var lootTable = AssetDatabase.LoadAssetAtPath<LootTable>("Assets/ScriptableObjects/DefaultLootTable.asset");
        var ammoData = AssetDatabase.LoadAssetAtPath<AmmoPickupData>("Assets/ScriptableObjects/AmmoPickupData.asset");
        var healthData = AssetDatabase.LoadAssetAtPath<HealthPackData>("Assets/ScriptableObjects/HealthPackData.asset");
        var shotgunData = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/ScriptableObjects/Weapons/ShotgunData.asset");
        var smgData = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/ScriptableObjects/Weapons/SMGData.asset");
        var rifleData = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/ScriptableObjects/Weapons/RifleData.asset");

        var so = new SerializedObject(spawner);
        so.FindProperty("lootBoxPrefab").objectReferenceValue = lootBoxPrefab;
        so.FindProperty("lootTable").objectReferenceValue = lootTable;
        so.FindProperty("ammoPickupData").objectReferenceValue = ammoData;
        so.FindProperty("healthPackData").objectReferenceValue = healthData;
        so.FindProperty("shotgunData").objectReferenceValue = shotgunData;
        so.FindProperty("smgData").objectReferenceValue = smgData;
        so.FindProperty("rifleData").objectReferenceValue = rifleData;
        so.ApplyModifiedProperties();

        Debug.Log("LootBoxSpawner added to scene.");
    }

    private static void AddWeaponInventoryToPlayer()
    {
        string playerPath = "Assets/Prefabs/Player.prefab";
        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPath);
        if (playerPrefab == null) return;

        // Check if already has WeaponInventory
        if (playerPrefab.GetComponent<WeaponInventory>() != null) return;

        var instance = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
        var inventory = instance.AddComponent<WeaponInventory>();

        var pistolData = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/ScriptableObjects/Weapons/PistolData.asset");
        var so = new SerializedObject(inventory);
        so.FindProperty("pistolData").objectReferenceValue = pistolData;
        so.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(instance, playerPath);
        Object.DestroyImmediate(instance);
        Debug.Log("WeaponInventory added to Player prefab.");
    }

    private static void CreateWeaponHUDCanvas()
    {
        // Check if already exists
        var existing = Object.FindFirstObjectByType<WeaponHUD>();
        if (existing != null) return;

        var canvas = new GameObject("WeaponHUDCanvas");
        var c = canvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 5;
        canvas.AddComponent<CanvasScaler>();
        canvas.AddComponent<GraphicRaycaster>();

        // Weapon name
        var nameGo = CreateUIText(canvas.transform, "WeaponName", "Pistol",
            new Vector2(0, 0), new Vector2(200, 30), new Vector2(-100, 60), 516);

        // Ammo text
        var ammoGo = CreateUIText(canvas.transform, "AmmoText", "INF",
            new Vector2(0, 0), new Vector2(200, 30), new Vector2(-100, 30), 516);

        // Slot indicators
        var slot1Go = CreateUIText(canvas.transform, "Slot1", "1: Pistol",
            new Vector2(1, 0), new Vector2(150, 20), new Vector2(-80, 120), 516);
        var slot2Go = CreateUIText(canvas.transform, "Slot2", "2: ---",
            new Vector2(1, 0), new Vector2(150, 20), new Vector2(-80, 98), 516);
        var slot3Go = CreateUIText(canvas.transform, "Slot3", "3: ---",
            new Vector2(1, 0), new Vector2(150, 20), new Vector2(-80, 76), 516);

        // Crosshair
        var crosshairParent = new GameObject("Crosshair");
        var crosshairRect = crosshairParent.AddComponent<RectTransform>();
        crosshairRect.SetParent(canvas.transform, false);
        crosshairRect.anchorMin = new Vector2(0.5f, 0.5f);
        crosshairRect.anchorMax = new Vector2(0.5f, 0.5f);
        crosshairRect.sizeDelta = Vector2.zero;

        var top = CreateCrosshairLine(crosshairRect, "Top", new Vector2(0, 10));
        var bottom = CreateCrosshairLine(crosshairRect, "Bottom", new Vector2(0, -10));
        var left = CreateCrosshairLine(crosshairRect, "Left", new Vector2(-10, 0));
        var right = CreateCrosshairLine(crosshairRect, "Right", new Vector2(10, 0));

        // WeaponHUD component
        var hud = canvas.AddComponent<WeaponHUD>();
        var tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        var so = new SerializedObject(hud);
        if (tmpType != null)
        {
            so.FindProperty("weaponNameText").objectReferenceValue = nameGo.GetComponent(tmpType);
            so.FindProperty("ammoText").objectReferenceValue = ammoGo.GetComponent(tmpType);
            so.FindProperty("slot1Text").objectReferenceValue = slot1Go.GetComponent(tmpType);
            so.FindProperty("slot2Text").objectReferenceValue = slot2Go.GetComponent(tmpType);
            so.FindProperty("slot3Text").objectReferenceValue = slot3Go.GetComponent(tmpType);
        }
        so.FindProperty("crosshairTop").objectReferenceValue = top.GetComponent<RectTransform>();
        so.FindProperty("crosshairBottom").objectReferenceValue = bottom.GetComponent<RectTransform>();
        so.FindProperty("crosshairLeft").objectReferenceValue = left.GetComponent<RectTransform>();
        so.FindProperty("crosshairRight").objectReferenceValue = right.GetComponent<RectTransform>();
        so.ApplyModifiedProperties();

        Debug.Log("WeaponHUD canvas created.");
    }

    private static GameObject CreateUIText(Transform parent, string name, string defaultText,
        Vector2 anchor, Vector2 size, Vector2 offset, int alignment)
    {
        var go = new GameObject(name);
        var rect = go.AddComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = offset;

        // Add TextMeshProUGUI via reflection to avoid editor assembly dependency
        var tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        if (tmpType != null)
        {
            var tmp = go.AddComponent(tmpType);
            var so = new SerializedObject(tmp);
            so.FindProperty("m_text").stringValue = defaultText;
            so.FindProperty("m_fontSize").floatValue = 16f;
            so.FindProperty("m_fontColor").colorValue = Color.white;
            so.FindProperty("m_textAlignment").intValue = alignment;
            so.ApplyModifiedProperties();
        }

        return go;
    }

    private static GameObject CreateCrosshairLine(RectTransform parent, string name, Vector2 position)
    {
        var go = new GameObject(name);
        var rect = go.AddComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = new Vector2(2, 8);
        rect.anchoredPosition = position;

        var img = go.AddComponent<Image>();
        img.color = Color.white;

        return go;
    }

    private static void RegisterPrefabsWithNetworkManager()
    {
        var nm = Object.FindFirstObjectByType<NetworkManager>();
        if (nm == null) return;

        var lootBoxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LootBox.prefab");
        if (lootBoxPrefab == null) return;

        bool hasLootBox = false;
        foreach (var p in nm.NetworkConfig.Prefabs.Prefabs)
        {
            if (p.Prefab == lootBoxPrefab) hasLootBox = true;
        }

        if (!hasLootBox)
        {
            nm.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = lootBoxPrefab });
            Debug.Log("LootBox prefab registered with NetworkManager.");
        }

        EditorUtility.SetDirty(nm);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
        string folder = System.IO.Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, folder);
    }
}
