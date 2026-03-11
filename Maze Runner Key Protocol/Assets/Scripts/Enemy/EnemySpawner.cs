using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// Spawns enemies after NavMesh bake. Handles Grunt/Guard placement and patrol waypoint generation.
/// Attach to a scene GameObject with NetworkObject.
/// </summary>
public class EnemySpawner : NetworkBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject gruntPrefab;
    [SerializeField] private GameObject guardPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private int gruntCount = 4;
    [SerializeField] private int guardCount = 2;
    [SerializeField] private float minEnemySpacing = 10f;
    [SerializeField] private float guardKeyRadius = 15f;
    [SerializeField] private int waypointsPerRoute = 6;
    [SerializeField] private float waypointSpacing = 8f;

    private float mazeMinX;
    private float mazeMaxX;
    private float mazeMinZ;
    private float mazeMaxZ;

    private List<Vector3> allWaypoints = new List<Vector3>();
    private List<Vector3> usedSpawnPositions = new List<Vector3>();

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // Wait for MazeGenerator to signal ready
        if (MazeGenerator.IsReady)
        {
            OnMazeReady();
        }
        else
        {
            MazeGenerator.OnMazeReady += OnMazeReady;
        }
    }

    public override void OnNetworkDespawn()
    {
        MazeGenerator.OnMazeReady -= OnMazeReady;
    }

    private void OnMazeReady()
    {
        MazeGenerator.OnMazeReady -= OnMazeReady;

        // Read bounds from MazeGenerator
        mazeMinX = MazeGenerator.MinX;
        mazeMaxX = MazeGenerator.MaxX;
        mazeMinZ = MazeGenerator.MinZ;
        mazeMaxZ = MazeGenerator.MaxZ;

        StartCoroutine(SpawnAfterDelay());
    }

    private System.Collections.IEnumerator SpawnAfterDelay()
    {
        // Brief delay for NavMesh queries to stabilize
        yield return new WaitForEndOfFrame();

        // Validate NavMesh
        if (!ValidateNavMesh())
        {
            Debug.LogWarning("[EnemySpawner] NavMesh not available, skipping enemy spawn");
            yield break;
        }

        GenerateWaypoints();
        SpawnGrunts();
        SpawnGuards();

        Debug.Log($"[EnemySpawner] Spawned {gruntCount} grunts and {guardCount} guards with {allWaypoints.Count} waypoints");
    }

    private bool ValidateNavMesh()
    {
        // Spot-check positions using dynamic maze bounds
        float midX = (mazeMinX + mazeMaxX) / 2f;
        float midZ = (mazeMinZ + mazeMaxZ) / 2f;
        Vector3[] testPositions = {
            new Vector3(mazeMinX + 2f, 0, mazeMinZ + 2f),
            new Vector3(midX, 0, midZ),
            new Vector3(mazeMaxX - 2f, 0, mazeMaxZ - 2f)
        };

        int valid = 0;
        foreach (var pos in testPositions)
        {
            if (NavMesh.SamplePosition(pos, out _, 4f, NavMesh.AllAreas))
                valid++;
        }

        if (valid < 2)
            Debug.LogWarning($"[EnemySpawner] Only {valid}/3 test positions on NavMesh");

        return valid > 0;
    }

    private void GenerateWaypoints()
    {
        allWaypoints.Clear();
        int maxAttempts = 200;

        for (int i = 0; i < maxAttempts && allWaypoints.Count < gruntCount * waypointsPerRoute; i++)
        {
            Vector3 candidate = new Vector3(
                Random.Range(mazeMinX, mazeMaxX),
                0f,
                Random.Range(mazeMinZ, mazeMaxZ)
            );

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 4f, NavMesh.AllAreas))
                continue;

            // Check spacing from existing waypoints
            bool tooClose = false;
            foreach (var wp in allWaypoints)
            {
                if (Vector3.Distance(hit.position, wp) < waypointSpacing * 0.5f)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
                allWaypoints.Add(hit.position);
        }

        Debug.Log($"[EnemySpawner] Generated {allWaypoints.Count} patrol waypoints");
    }

    private void SpawnGrunts()
    {
        if (gruntPrefab == null) return;

        for (int i = 0; i < gruntCount; i++)
        {
            Vector3? pos = FindSpawnPosition(mazeMinX, mazeMaxX, mazeMinZ, mazeMaxZ);
            if (!pos.HasValue)
            {
                Debug.LogWarning($"[EnemySpawner] Could not find spawn position for grunt {i}");
                continue;
            }

            var grunt = Instantiate(gruntPrefab, pos.Value, Quaternion.identity);
            grunt.name = $"Grunt_{i}";
            grunt.GetComponent<NetworkObject>().Spawn();

            // Assign patrol route
            var stateMachine = grunt.GetComponent<EnemyStateMachine>();
            if (stateMachine != null)
            {
                var route = BuildPatrolRoute(pos.Value);
                stateMachine.SetPatrolWaypoints(route);
            }

            usedSpawnPositions.Add(pos.Value);
        }
    }

    private void SpawnGuards()
    {
        if (guardPrefab == null) return;

        // Place guards near key spawn
        float centerX = (mazeMinX + mazeMaxX) / 2f;
        float centerZ = (mazeMinZ + mazeMaxZ) / 2f;
        Vector3 keyPos = KeyManager.Instance != null
            ? KeyManager.Instance.KeyWorldPosition.Value
            : new Vector3(centerX, 0f, centerZ);

        for (int i = 0; i < guardCount; i++)
        {
            Vector3? pos = FindSpawnPosition(
                keyPos.x - guardKeyRadius, keyPos.x + guardKeyRadius,
                keyPos.z - guardKeyRadius, keyPos.z + guardKeyRadius);

            if (!pos.HasValue)
            {
                Debug.LogWarning($"[EnemySpawner] Could not find spawn position for guard {i}");
                continue;
            }

            var guard = Instantiate(guardPrefab, pos.Value, Quaternion.identity);
            guard.name = $"Guard_{i}";
            guard.GetComponent<NetworkObject>().Spawn();

            usedSpawnPositions.Add(pos.Value);
        }
    }

    private Vector3? FindSpawnPosition(float minX, float maxX, float minZ, float maxZ)
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            Vector3 candidate = new Vector3(
                Random.Range(minX, maxX),
                0f,
                Random.Range(minZ, maxZ)
            );

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 4f, NavMesh.AllAreas))
                continue;

            // Check spacing from other enemies
            bool tooClose = false;
            foreach (var used in usedSpawnPositions)
            {
                if (Vector3.Distance(hit.position, used) < minEnemySpacing)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
                return hit.position;
        }

        return null;
    }

    private List<Vector3> BuildPatrolRoute(Vector3 startPos)
    {
        var route = new List<Vector3>();

        // Find nearest waypoints to build a route
        var sorted = new List<Vector3>(allWaypoints);
        sorted.Sort((a, b) => Vector3.Distance(startPos, a).CompareTo(Vector3.Distance(startPos, b)));

        for (int i = 0; i < Mathf.Min(waypointsPerRoute, sorted.Count); i++)
        {
            route.Add(sorted[i]);
        }

        return route;
    }
}
