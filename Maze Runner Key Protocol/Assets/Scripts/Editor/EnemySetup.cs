using UnityEngine;
using UnityEditor;
using Unity.Netcode;
using Unity.Netcode.Components;

/// <summary>
/// Editor script to create enemy prefabs, configs, and scene spawner.
/// Run via MazeRunner > Setup Enemy AI.
/// </summary>
public class EnemySetup : EditorWindow
{
    [MenuItem("MazeRunner/Setup Enemy AI")]
    public static void Setup()
    {
        CreateEnemyConfigs();
        CreateGruntPrefab();
        CreateGuardPrefab();
        CreateEnemySpawner();
        RegisterPrefabsWithNetworkManager();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("Enemy AI setup complete!");
    }

    private static void CreateEnemyConfigs()
    {
        string configPath = "Assets/ScriptableObjects/EnemyConfigs";
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        if (!AssetDatabase.IsValidFolder(configPath))
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "EnemyConfigs");

        // Grunt config
        if (AssetDatabase.LoadAssetAtPath<EnemyConfig>($"{configPath}/GruntConfig.asset") == null)
        {
            var grunt = ScriptableObject.CreateInstance<EnemyConfig>();
            grunt.health = 30f;
            grunt.patrolSpeed = 2f;
            grunt.alertSpeed = 3.5f;
            grunt.chaseSpeed = 4.5f;
            grunt.attackSpeed = 1f;
            grunt.attackRange = 2.5f;
            grunt.attackDamage = 8f;
            grunt.attackInterval = 1.5f;
            grunt.sightRange = 12f;
            grunt.sightAngle = 90f;
            grunt.soundDetectionRadius = 20f;
            grunt.searchDuration = 10f;
            grunt.memoryDuration = 5f;
            grunt.patrolBehavior = EnemyConfig.PatrolBehavior.Patrol;
            AssetDatabase.CreateAsset(grunt, $"{configPath}/GruntConfig.asset");
            Debug.Log("Grunt config created.");
        }

        // Guard config
        if (AssetDatabase.LoadAssetAtPath<EnemyConfig>($"{configPath}/GuardConfig.asset") == null)
        {
            var guard = ScriptableObject.CreateInstance<EnemyConfig>();
            guard.health = 60f;
            guard.patrolSpeed = 0f;
            guard.alertSpeed = 3f;
            guard.chaseSpeed = 4f;
            guard.attackSpeed = 1f;
            guard.attackRange = 3f;
            guard.attackDamage = 12f;
            guard.attackInterval = 1.2f;
            guard.sightRange = 15f;
            guard.sightAngle = 120f;
            guard.soundDetectionRadius = 25f;
            guard.searchDuration = 12f;
            guard.memoryDuration = 5f;
            guard.patrolBehavior = EnemyConfig.PatrolBehavior.Stationary;
            AssetDatabase.CreateAsset(guard, $"{configPath}/GuardConfig.asset");
            Debug.Log("Guard config created.");
        }
    }

    private static void CreateGruntPrefab()
    {
        string prefabPath = "Assets/Prefabs/Enemy/Grunt.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;

        var grunt = CreateEnemyBase("Grunt", new Color(0.8f, 0.3f, 0.2f), "GruntMaterial");

        // Wire config
        var config = AssetDatabase.LoadAssetAtPath<EnemyConfig>("Assets/ScriptableObjects/EnemyConfigs/GruntConfig.asset");
        WireConfig(grunt, config);

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Enemy"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Enemy");
        PrefabUtility.SaveAsPrefabAsset(grunt, prefabPath);
        Object.DestroyImmediate(grunt);
        Debug.Log("Grunt prefab created.");
    }

    private static void CreateGuardPrefab()
    {
        string prefabPath = "Assets/Prefabs/Enemy/Guard.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;

        var guard = CreateEnemyBase("Guard", new Color(0.6f, 0.1f, 0.1f), "GuardMaterial");

        // Make guard slightly larger
        var model = guard.transform.Find("EnemyModel");
        if (model != null)
            model.localScale = new Vector3(1.2f, 1.2f, 1.2f);

        // Wire config
        var config = AssetDatabase.LoadAssetAtPath<EnemyConfig>("Assets/ScriptableObjects/EnemyConfigs/GuardConfig.asset");
        WireConfig(guard, config);

        PrefabUtility.SaveAsPrefabAsset(guard, "Assets/Prefabs/Enemy/Guard.prefab");
        Object.DestroyImmediate(guard);
        Debug.Log("Guard prefab created.");
    }

    private static GameObject CreateEnemyBase(string name, Color color, string materialName)
    {
        var enemy = new GameObject(name);

        // Model
        var model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        model.name = "EnemyModel";
        model.transform.SetParent(enemy.transform, false);
        model.transform.localPosition = new Vector3(0, 1f, 0);
        Object.DestroyImmediate(model.GetComponent<CapsuleCollider>());

        // Material
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", color);
        model.GetComponent<MeshRenderer>().sharedMaterial = mat;

        if (!AssetDatabase.IsValidFolder("Assets/Materials/Enemy"))
            AssetDatabase.CreateFolder("Assets/Materials", "Enemy");
        AssetDatabase.CreateAsset(mat, $"Assets/Materials/Enemy/{materialName}.mat");

        // Collider on root
        var col = enemy.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, 1f, 0);
        col.height = 2f;
        col.radius = 0.5f;

        // NetworkObject
        enemy.AddComponent<NetworkObject>();

        // NetworkTransform
        enemy.AddComponent<NetworkTransform>();

        // NavMeshAgent
        var agent = enemy.AddComponent<UnityEngine.AI.NavMeshAgent>();
        agent.radius = 0.4f;
        agent.height = 2f;
        agent.speed = 2f;
        agent.angularSpeed = 120f;
        agent.acceleration = 8f;
        agent.stoppingDistance = 0.5f;
        agent.autoBraking = true;
        agent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.MedQualityObstacleAvoidance;

        // AI Components
        enemy.AddComponent<EnemyStateMachine>();
        enemy.AddComponent<EnemyPerception>();
        enemy.AddComponent<EnemyHealth>();

        return enemy;
    }

    private static void WireConfig(GameObject enemy, EnemyConfig config)
    {
        if (config == null) return;

        var sm = enemy.GetComponent<EnemyStateMachine>();
        if (sm != null)
        {
            var so = new SerializedObject(sm);
            so.FindProperty("config").objectReferenceValue = config;
            so.ApplyModifiedProperties();
        }

        var perception = enemy.GetComponent<EnemyPerception>();
        if (perception != null)
        {
            var so = new SerializedObject(perception);
            so.FindProperty("config").objectReferenceValue = config;
            so.ApplyModifiedProperties();
        }

        var health = enemy.GetComponent<EnemyHealth>();
        if (health != null)
        {
            var so = new SerializedObject(health);
            so.FindProperty("config").objectReferenceValue = config;
            so.ApplyModifiedProperties();
        }
    }

    private static void CreateEnemySpawner()
    {
        if (Object.FindFirstObjectByType<EnemySpawner>() != null) return;

        var spawnerGo = new GameObject("EnemySpawner");
        var spawner = spawnerGo.AddComponent<EnemySpawner>();
        spawnerGo.AddComponent<NetworkObject>();

        var gruntPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy/Grunt.prefab");
        var guardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy/Guard.prefab");

        var so = new SerializedObject(spawner);
        so.FindProperty("gruntPrefab").objectReferenceValue = gruntPrefab;
        so.FindProperty("guardPrefab").objectReferenceValue = guardPrefab;
        so.ApplyModifiedProperties();

        Debug.Log("EnemySpawner added to scene.");
    }

    private static void RegisterPrefabsWithNetworkManager()
    {
        var nm = Object.FindFirstObjectByType<NetworkManager>();
        if (nm == null) return;

        var gruntPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy/Grunt.prefab");
        var guardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy/Guard.prefab");

        bool hasGrunt = false, hasGuard = false;
        foreach (var p in nm.NetworkConfig.Prefabs.Prefabs)
        {
            if (p.Prefab == gruntPrefab) hasGrunt = true;
            if (p.Prefab == guardPrefab) hasGuard = true;
        }

        if (!hasGrunt && gruntPrefab != null)
        {
            nm.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = gruntPrefab });
            Debug.Log("Grunt prefab registered with NetworkManager.");
        }
        if (!hasGuard && guardPrefab != null)
        {
            nm.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = guardPrefab });
            Debug.Log("Guard prefab registered with NetworkManager.");
        }

        EditorUtility.SetDirty(nm);
    }
}
