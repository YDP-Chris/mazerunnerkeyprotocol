using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using Unity.Netcode;
using System;
using System.Collections.Generic;

/// <summary>
/// Runtime procedural maze generator using Recursive Backtracker (DFS).
/// Replaces StaticMazeBuilder for Phase 2. Generates maze, places exit + player spawns,
/// bakes NavMesh, then fires OnMazeReady for other systems.
/// </summary>
public class MazeGenerator : NetworkBehaviour
{
    [Header("Maze Parameters")]
    [SerializeField] private int gridWidth = 20;
    [SerializeField] private int gridHeight = 20;
    [SerializeField] private float cellSize = 4f;
    [SerializeField] private float wallHeight = 4f;
    [SerializeField] private float wallThickness = 0.3f;
    [SerializeField] private float floorThickness = 0.2f;

    [Header("Seed")]
    [SerializeField] private int seed = -1; // -1 = random

    [Header("Spawn Settings")]
    [SerializeField] private int maxPlayerSpawns = 8;
    [SerializeField] private int minPlayerSpawns = 4;

    [Header("Prefabs")]
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private GameObject pillarPrefab;

    // Static bounds for other systems to read
    public static float MinX { get; private set; }
    public static float MaxX { get; private set; }
    public static float MinZ { get; private set; }
    public static float MaxZ { get; private set; }
    public static bool IsReady { get; private set; }

    public static event Action OnMazeReady;
    public static MazeGenerator Instance { get; private set; }

    // Maze grid: true = wall present
    private bool[,] horizontalWalls; // [gridHeight+1, gridWidth]
    private bool[,] verticalWalls;   // [gridHeight, gridWidth+1]
    private bool[,] visited;

    private GameObject mazeParent;
    private GameObject spawnParent;
    private System.Random rng;

    private void Awake()
    {
        Instance = this;
        IsReady = false;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        GenerateAndBuild();
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this)
        {
            Instance = null;
            IsReady = false;
        }
    }

    public void GenerateAndBuild()
    {
        float startTime = Time.realtimeSinceStartup;

        // Resolve seed
        int usedSeed = seed >= 0 ? seed : UnityEngine.Random.Range(0, int.MaxValue);
        rng = new System.Random(usedSeed);
        Debug.Log($"[MazeGenerator] Generating {gridWidth}x{gridHeight} maze with seed {usedSeed}");

        // Set static bounds
        MinX = cellSize * 0.5f;
        MaxX = gridWidth * cellSize - cellSize * 0.5f;
        MinZ = cellSize * 0.5f;
        MaxZ = gridHeight * cellSize - cellSize * 0.5f;

        // Clean previous maze
        CleanUp();

        // Create parents
        mazeParent = new GameObject("Maze");
        spawnParent = new GameObject("SpawnPoints");

        // 1. Generate logical grid
        GenerateMazeGrid();

        // 2. Create open areas
        CreateOpenAreas();

        // 3. Instantiate geometry
        PlaceFloors();
        PlaceWalls();

        // 4. Place exit on maze edge
        PlaceExit();

        // 4b. Move ExitGateway scene object to generated exit position
        PositionExitGateway();

        // 5. Place player spawns in dead-ends
        PlacePlayerSpawns();

        // 6. Bake NavMesh
        BakeNavMesh();

        float elapsed = Time.realtimeSinceStartup - startTime;
        Debug.Log($"[MazeGenerator] Maze ready in {elapsed:F2}s. Bounds: ({MinX},{MinZ}) to ({MaxX},{MaxZ})");

        // 7. Signal ready
        IsReady = true;
        OnMazeReady?.Invoke();
    }

    private void CleanUp()
    {
        IsReady = false;

        var existingMaze = GameObject.Find("Maze");
        if (existingMaze != null) Destroy(existingMaze);

        var existingSpawns = GameObject.Find("SpawnPoints");
        if (existingSpawns != null) Destroy(existingSpawns);
    }

    #region Maze Algorithm

    private void GenerateMazeGrid()
    {
        // Initialize all walls as present
        horizontalWalls = new bool[gridHeight + 1, gridWidth];
        verticalWalls = new bool[gridHeight, gridWidth + 1];
        visited = new bool[gridHeight, gridWidth];

        for (int r = 0; r <= gridHeight; r++)
            for (int c = 0; c < gridWidth; c++)
                horizontalWalls[r, c] = true;

        for (int r = 0; r < gridHeight; r++)
            for (int c = 0; c <= gridWidth; c++)
                verticalWalls[r, c] = true;

        // Iterative stack-based Recursive Backtracker
        var stack = new Stack<Vector2Int>();
        var start = new Vector2Int(0, 0);
        visited[0, 0] = true;
        stack.Push(start);

        while (stack.Count > 0)
        {
            var current = stack.Peek();
            var neighbors = GetUnvisitedNeighbors(current);

            if (neighbors.Count > 0)
            {
                var next = neighbors[0];
                RemoveWallBetween(current, next);
                visited[next.x, next.y] = true;
                stack.Push(next);
            }
            else
            {
                stack.Pop();
            }
        }
    }

    private List<Vector2Int> GetUnvisitedNeighbors(Vector2Int cell)
    {
        var neighbors = new List<Vector2Int>();
        var dirs = new Vector2Int[] {
            new Vector2Int(-1, 0), new Vector2Int(1, 0),
            new Vector2Int(0, -1), new Vector2Int(0, 1)
        };

        foreach (var d in dirs)
        {
            int nr = cell.x + d.x;
            int nc = cell.y + d.y;
            if (nr >= 0 && nr < gridHeight && nc >= 0 && nc < gridWidth && !visited[nr, nc])
                neighbors.Add(new Vector2Int(nr, nc));
        }

        // Fisher-Yates shuffle with seeded RNG
        for (int i = neighbors.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            var tmp = neighbors[i];
            neighbors[i] = neighbors[j];
            neighbors[j] = tmp;
        }

        return neighbors;
    }

    private void RemoveWallBetween(Vector2Int a, Vector2Int b)
    {
        if (a.x == b.x)
        {
            // Same row, different col — vertical wall
            int col = Mathf.Max(a.y, b.y);
            verticalWalls[a.x, col] = false;
        }
        else
        {
            // Same col, different row — horizontal wall
            int row = Mathf.Max(a.x, b.x);
            horizontalWalls[row, a.y] = false;
        }
    }

    private void CreateOpenAreas()
    {
        // Create 2x2 clearings spread across the maze
        int qw = gridWidth / 4;
        int qh = gridHeight / 4;

        var candidates = new Vector2Int[] {
            new Vector2Int(qh, qw),
            new Vector2Int(gridHeight - qh - 2, gridWidth - qw - 2),
            new Vector2Int(gridHeight / 2, gridWidth / 2),
        };

        foreach (var origin in candidates)
        {
            if (origin.x + 1 >= gridHeight || origin.y + 1 >= gridWidth) continue;

            horizontalWalls[origin.x + 1, origin.y] = false;
            horizontalWalls[origin.x + 1, origin.y + 1] = false;
            verticalWalls[origin.x, origin.y + 1] = false;
            verticalWalls[origin.x + 1, origin.y + 1] = false;
        }
    }

    #endregion

    #region Geometry

    private void PlaceFloors()
    {
        var floorsParent = new GameObject("Floors");
        floorsParent.transform.SetParent(mazeParent.transform, false);

        for (int r = 0; r < gridHeight; r++)
        {
            for (int c = 0; c < gridWidth; c++)
            {
                float x = c * cellSize + cellSize / 2f;
                float z = r * cellSize + cellSize / 2f;

                var floor = Instantiate(floorPrefab, new Vector3(x, -floorThickness / 2f, z), Quaternion.identity);
                floor.name = $"Floor_{r}_{c}";
                floor.transform.localScale = new Vector3(cellSize, floorThickness, cellSize);
                floor.transform.SetParent(floorsParent.transform, true);
            }
        }
    }

    private void PlaceWalls()
    {
        var wallsParent = new GameObject("Walls");
        wallsParent.transform.SetParent(mazeParent.transform, false);

        var pillarsParent = new GameObject("Pillars");
        pillarsParent.transform.SetParent(mazeParent.transform, false);

        // Horizontal walls (along X axis)
        for (int r = 0; r <= gridHeight; r++)
        {
            for (int c = 0; c < gridWidth; c++)
            {
                if (!horizontalWalls[r, c]) continue;

                float x = c * cellSize + cellSize / 2f;
                float z = r * cellSize;
                float y = wallHeight / 2f;

                var wall = Instantiate(wallPrefab, new Vector3(x, y, z), Quaternion.identity);
                wall.name = $"HWall_{r}_{c}";
                wall.transform.localScale = new Vector3(cellSize, wallHeight, wallThickness);
                wall.transform.SetParent(wallsParent.transform, true);
            }
        }

        // Vertical walls (along Z axis)
        for (int r = 0; r < gridHeight; r++)
        {
            for (int c = 0; c <= gridWidth; c++)
            {
                if (!verticalWalls[r, c]) continue;

                float x = c * cellSize;
                float z = r * cellSize + cellSize / 2f;
                float y = wallHeight / 2f;

                var wall = Instantiate(wallPrefab, new Vector3(x, y, z), Quaternion.Euler(0, 90, 0));
                wall.name = $"VWall_{r}_{c}";
                wall.transform.localScale = new Vector3(cellSize, wallHeight, wallThickness);
                wall.transform.SetParent(wallsParent.transform, true);
            }
        }

        // Corner pillars at grid intersections where walls meet
        for (int r = 0; r <= gridHeight; r++)
        {
            for (int c = 0; c <= gridWidth; c++)
            {
                bool hasWall = false;
                if (c < gridWidth && horizontalWalls[r, c]) hasWall = true;
                if (c > 0 && horizontalWalls[r, c - 1]) hasWall = true;
                if (r < gridHeight && verticalWalls[r, c]) hasWall = true;
                if (r > 0 && verticalWalls[r - 1, c]) hasWall = true;

                if (!hasWall) continue;

                float x = c * cellSize;
                float z = r * cellSize;
                float y = wallHeight / 2f;

                var pillar = Instantiate(pillarPrefab, new Vector3(x, y, z), Quaternion.identity);
                pillar.name = $"Pillar_{r}_{c}";
                pillar.transform.localScale = new Vector3(wallThickness, wallHeight, wallThickness);
                pillar.transform.SetParent(pillarsParent.transform, true);
            }
        }
    }

    #endregion

    #region Placement Pipeline

    private void PlaceExit()
    {
        // Pick a random edge cell and open the boundary wall
        // Choose from bottom or right edge for variety
        int edge = rng.Next(4); // 0=top, 1=bottom, 2=left, 3=right
        int pos;
        Vector2Int exitCell;

        switch (edge)
        {
            case 0: // Top edge
                pos = rng.Next(gridWidth);
                exitCell = new Vector2Int(gridHeight - 1, pos);
                horizontalWalls[gridHeight, pos] = false;
                break;
            case 1: // Bottom edge
                pos = rng.Next(gridWidth);
                exitCell = new Vector2Int(0, pos);
                horizontalWalls[0, pos] = false;
                break;
            case 2: // Left edge
                pos = rng.Next(gridHeight);
                exitCell = new Vector2Int(pos, 0);
                verticalWalls[pos, 0] = false;
                break;
            default: // Right edge
                pos = rng.Next(gridHeight);
                exitCell = new Vector2Int(pos, gridWidth - 1);
                verticalWalls[pos, gridWidth] = false;
                break;
        }

        // Position exit just outside the maze boundary
        Vector3 exitWorldPos;
        switch (edge)
        {
            case 0:
                exitWorldPos = new Vector3(exitCell.y * cellSize + cellSize / 2f, 0.5f, gridHeight * cellSize);
                break;
            case 1:
                exitWorldPos = new Vector3(exitCell.y * cellSize + cellSize / 2f, 0.5f, 0f);
                break;
            case 2:
                exitWorldPos = new Vector3(0f, 0.5f, exitCell.x * cellSize + cellSize / 2f);
                break;
            default:
                exitWorldPos = new Vector3(gridWidth * cellSize, 0.5f, exitCell.x * cellSize + cellSize / 2f);
                break;
        }

        var exitGo = new GameObject("ExitSpawn");
        exitGo.transform.position = exitWorldPos;
        exitGo.transform.SetParent(spawnParent.transform, true);
        exitGo.AddComponent<ExitSpawnPoint>();

        Debug.Log($"[MazeGenerator] Exit placed at {exitWorldPos} (edge {edge})");
    }

    private void PositionExitGateway()
    {
        // Move the scene's ExitGateway to the generated exit position
        var exitGateway = ExitGateway.Instance;
        if (exitGateway != null)
        {
            var exitSpawn = UnityEngine.Object.FindAnyObjectByType<ExitSpawnPoint>();
            if (exitSpawn != null)
            {
                exitGateway.transform.position = exitSpawn.transform.position;
                Debug.Log($"[MazeGenerator] ExitGateway moved to {exitSpawn.transform.position}");
            }
        }
    }

    private void PlacePlayerSpawns()
    {
        // Find dead-ends (cells with exactly 1 opening)
        var deadEnds = new List<Vector2Int>();
        var deadEndDirections = new Dictionary<Vector2Int, Vector2Int>();

        for (int r = 0; r < gridHeight; r++)
        {
            for (int c = 0; c < gridWidth; c++)
            {
                int openings = 0;
                Vector2Int openDir = Vector2Int.zero;

                if (!horizontalWalls[r + 1, c]) { openings++; openDir = new Vector2Int(1, 0); }
                if (!horizontalWalls[r, c]) { openings++; openDir = new Vector2Int(-1, 0); }
                if (c < gridWidth && !verticalWalls[r, c + 1]) { openings++; openDir = new Vector2Int(0, 1); }
                if (c > 0 && !verticalWalls[r, c]) { openings++; openDir = new Vector2Int(0, -1); }

                if (openings == 1)
                {
                    deadEnds.Add(new Vector2Int(r, c));
                    deadEndDirections[new Vector2Int(r, c)] = openDir;
                }
            }
        }

        Debug.Log($"[MazeGenerator] Found {deadEnds.Count} dead-ends");

        // Greedy max-distance selection
        int spawnCount = Mathf.Clamp(deadEnds.Count, minPlayerSpawns, maxPlayerSpawns);
        var selected = SelectSpreadSpawns(deadEnds, spawnCount);

        for (int i = 0; i < selected.Count; i++)
        {
            var cell = selected[i];
            var worldPos = CellToWorld(cell);
            var dir = deadEndDirections.ContainsKey(cell) ? deadEndDirections[cell] : Vector2Int.zero;

            var go = new GameObject($"PlayerSpawn_{i + 1}");
            go.transform.position = new Vector3(worldPos.x, 0.5f, worldPos.z);

            if (dir != Vector2Int.zero)
            {
                var worldDir = new Vector3(dir.y * cellSize, 0, dir.x * cellSize).normalized;
                if (worldDir.sqrMagnitude > 0.01f)
                    go.transform.forward = worldDir;
            }

            go.transform.SetParent(spawnParent.transform, true);
            go.AddComponent<PlayerSpawnPoint>();
        }

        Debug.Log($"[MazeGenerator] Placed {selected.Count} player spawn points");
    }

    private List<Vector2Int> SelectSpreadSpawns(List<Vector2Int> candidates, int count)
    {
        if (candidates.Count <= count)
            return new List<Vector2Int>(candidates);

        var selected = new List<Vector2Int>();
        var remaining = new List<Vector2Int>(candidates);

        // Start with corner-most dead-end
        remaining.Sort((a, b) =>
        {
            float da = Vector2Int.Distance(a, Vector2Int.zero);
            float db = Vector2Int.Distance(b, Vector2Int.zero);
            return db.CompareTo(da);
        });
        selected.Add(remaining[0]);
        remaining.RemoveAt(0);

        // Greedily add the candidate farthest from all selected
        while (selected.Count < count && remaining.Count > 0)
        {
            int bestIdx = 0;
            float bestMinDist = -1;

            for (int i = 0; i < remaining.Count; i++)
            {
                float minDist = float.MaxValue;
                foreach (var s in selected)
                {
                    float d = Vector2Int.Distance(remaining[i], s);
                    if (d < minDist) minDist = d;
                }
                if (minDist > bestMinDist)
                {
                    bestMinDist = minDist;
                    bestIdx = i;
                }
            }

            selected.Add(remaining[bestIdx]);
            remaining.RemoveAt(bestIdx);
        }

        return selected;
    }

    #endregion

    #region NavMesh

    private void BakeNavMesh()
    {
        var floorsParent = mazeParent.transform.Find("Floors");
        if (floorsParent == null)
        {
            Debug.LogError("[MazeGenerator] Floors parent not found for NavMesh bake!");
            return;
        }

        // Add NavMeshSurface to the maze parent for runtime baking
        var surface = mazeParent.GetComponent<NavMeshSurface>();
        if (surface == null)
            surface = mazeParent.AddComponent<NavMeshSurface>();

        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();

        Debug.Log("[MazeGenerator] NavMesh baked successfully");
    }

    #endregion

    #region Utility

    private Vector3 CellToWorld(Vector2Int cell)
    {
        return new Vector3(
            cell.y * cellSize + cellSize / 2f,
            0f,
            cell.x * cellSize + cellSize / 2f
        );
    }

    #endregion
}
