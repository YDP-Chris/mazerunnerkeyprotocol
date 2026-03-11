using UnityEngine;
using UnityEditor;

public class MazeSetup : EditorWindow
{
    [MenuItem("MazeRunner/Setup Procedural Maze")]
    public static void SetupProceduralMaze()
    {
        // 1. Remove static maze objects (Maze and SpawnPoints hierarchies)
        var maze = GameObject.Find("Maze");
        if (maze != null)
        {
            Undo.DestroyObjectImmediate(maze);
            Debug.Log("[MazeSetup] Removed static Maze hierarchy");
        }

        var spawnPoints = GameObject.Find("SpawnPoints");
        if (spawnPoints != null)
        {
            Undo.DestroyObjectImmediate(spawnPoints);
            Debug.Log("[MazeSetup] Removed static SpawnPoints hierarchy");
        }

        // 2. Find or create MazeGenerator GameObject
        var generatorObj = GameObject.Find("MazeGenerator");
        if (generatorObj == null)
        {
            generatorObj = new GameObject("MazeGenerator");
            Undo.RegisterCreatedObjectUndo(generatorObj, "Create MazeGenerator");
        }

        // Add NetworkObject if missing
        if (generatorObj.GetComponent<Unity.Netcode.NetworkObject>() == null)
            Undo.AddComponent<Unity.Netcode.NetworkObject>(generatorObj);

        // Add MazeGenerator if missing
        var generator = generatorObj.GetComponent<MazeGenerator>();
        if (generator == null)
            generator = Undo.AddComponent<MazeGenerator>(generatorObj);

        // 3. Assign prefabs
        var wallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Maze/Wall.prefab");
        var floorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Maze/FloorTile.prefab");
        var pillarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Maze/CornerPillar.prefab");

        var so = new SerializedObject(generator);
        so.FindProperty("wallPrefab").objectReferenceValue = wallPrefab;
        so.FindProperty("floorPrefab").objectReferenceValue = floorPrefab;
        so.FindProperty("pillarPrefab").objectReferenceValue = pillarPrefab;
        so.ApplyModifiedProperties();

        if (wallPrefab == null || floorPrefab == null || pillarPrefab == null)
            Debug.LogWarning("[MazeSetup] Some prefabs not found! Check Assets/Prefabs/Maze/");

        // Mark scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[MazeSetup] Procedural maze setup complete. MazeGenerator added with prefab references.");
    }
}
