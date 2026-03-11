using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class StaticMazeBuilder : EditorWindow
{
    private const int GRID_SIZE = 20;
    private const float CELL_SIZE = 4f;
    private const float WALL_HEIGHT = 4f;
    private const float WALL_THICKNESS = 0.3f;
    private const float FLOOR_THICKNESS = 0.2f;

    // Maze grid: true = wall present
    // Walls are stored on cell edges
    // horizontalWalls[row, col] = wall on top edge of cell (row, col)
    // verticalWalls[row, col] = wall on left edge of cell (row, col)
    private bool[,] horizontalWalls;
    private bool[,] verticalWalls;
    private bool[,] visited;

    // Open areas (2x2 regions to clear)
    private List<Vector2Int> openAreaOrigins = new List<Vector2Int>();

    [MenuItem("MazeRunner/Build Static Maze")]
    public static void BuildMaze()
    {
        var builder = new StaticMazeBuilder();
        builder.Execute();
    }

    private void Execute()
    {
        // Find or create parent objects
        var mazeParent = FindOrCreateRoot("Maze");
        var spawnParent = FindOrCreateRoot("SpawnPoints");
        var lightingParent = FindOrCreateRoot("Lighting");

        // Clear existing children
        ClearChildren(mazeParent);
        ClearChildren(spawnParent);

        // Load or create materials
        var wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Maze/WallMaterial.mat");
        var floorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Maze/FloorMaterial.mat");

        if (wallMat == null || floorMat == null)
        {
            Debug.LogError("Materials not found! Create WallMaterial.mat and FloorMaterial.mat first.");
            return;
        }

        // Create prefabs if they don't exist
        CreatePrefabs(wallMat, floorMat);

        // Load prefabs
        var wallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Maze/Wall.prefab");
        var floorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Maze/FloorTile.prefab");
        var pillarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Maze/CornerPillar.prefab");

        // Generate maze layout
        GenerateMaze();

        // Create open areas (2x2 clearings)
        CreateOpenAreas();

        // Place floor tiles
        PlaceFloors(mazeParent, floorPrefab);

        // Place walls
        PlaceWalls(mazeParent, wallPrefab, pillarPrefab);

        // Place spawn points
        PlaceSpawnPoints(spawnParent);

        // Setup lighting
        SetupLighting(lightingParent);

        // Mark floor as navigation static
        MarkNavigationStatic(mazeParent);

        // Save
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("Static maze built successfully! 20x20 grid, 80x80 units.");
        Debug.Log("Next: Bake NavMesh via Window > AI > Navigation.");
    }

    private void CreatePrefabs(Material wallMat, Material floorMat)
    {
        // Wall prefab: 4 long (X), 4 high (Y), 0.3 thick (Z)
        if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Maze/Wall.prefab") == null)
        {
            var wallGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallGo.name = "Wall";
            wallGo.transform.localScale = new Vector3(CELL_SIZE, WALL_HEIGHT, WALL_THICKNESS);
            wallGo.GetComponent<MeshRenderer>().sharedMaterial = wallMat;
            // Mark wall as NOT navigation static via GameObjectUtility
            GameObjectUtility.SetStaticEditorFlags(wallGo, 0);
            PrefabUtility.SaveAsPrefabAsset(wallGo, "Assets/Prefabs/Maze/Wall.prefab");
            Object.DestroyImmediate(wallGo);
        }

        // Floor tile prefab: 4x4 on XZ, 0.2 thick
        if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Maze/FloorTile.prefab") == null)
        {
            var floorGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floorGo.name = "FloorTile";
            floorGo.transform.localScale = new Vector3(CELL_SIZE, FLOOR_THICKNESS, CELL_SIZE);
            floorGo.GetComponent<MeshRenderer>().sharedMaterial = floorMat;
            // Mark floor as navigation static + walkable
            GameObjectUtility.SetStaticEditorFlags(floorGo, StaticEditorFlags.NavigationStatic);
            PrefabUtility.SaveAsPrefabAsset(floorGo, "Assets/Prefabs/Maze/FloorTile.prefab");
            Object.DestroyImmediate(floorGo);
        }

        // Corner pillar prefab: fills wall junctions
        if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Maze/CornerPillar.prefab") == null)
        {
            var pillarGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillarGo.name = "CornerPillar";
            pillarGo.transform.localScale = new Vector3(WALL_THICKNESS, WALL_HEIGHT, WALL_THICKNESS);
            pillarGo.GetComponent<MeshRenderer>().sharedMaterial = wallMat;
            GameObjectUtility.SetStaticEditorFlags(pillarGo, 0);
            PrefabUtility.SaveAsPrefabAsset(pillarGo, "Assets/Prefabs/Maze/CornerPillar.prefab");
            Object.DestroyImmediate(pillarGo);
        }

        AssetDatabase.Refresh();
    }

    private void GenerateMaze()
    {
        // Initialize all walls as present
        // horizontalWalls: 21 rows x 20 cols (top/bottom edges of each row)
        // verticalWalls: 20 rows x 21 cols (left/right edges of each col)
        horizontalWalls = new bool[GRID_SIZE + 1, GRID_SIZE];
        verticalWalls = new bool[GRID_SIZE, GRID_SIZE + 1];
        visited = new bool[GRID_SIZE, GRID_SIZE];

        for (int r = 0; r <= GRID_SIZE; r++)
            for (int c = 0; c < GRID_SIZE; c++)
                horizontalWalls[r, c] = true;

        for (int r = 0; r < GRID_SIZE; r++)
            for (int c = 0; c <= GRID_SIZE; c++)
                verticalWalls[r, c] = true;

        // Recursive backtracker with explicit stack (no actual recursion)
        var stack = new Stack<Vector2Int>();
        var rng = new System.Random(42); // Fixed seed for reproducibility

        var start = new Vector2Int(0, 0);
        visited[0, 0] = true;
        stack.Push(start);

        while (stack.Count > 0)
        {
            var current = stack.Peek();
            var neighbors = GetUnvisitedNeighbors(current, rng);

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

    private List<Vector2Int> GetUnvisitedNeighbors(Vector2Int cell, System.Random rng)
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
            if (nr >= 0 && nr < GRID_SIZE && nc >= 0 && nc < GRID_SIZE && !visited[nr, nc])
                neighbors.Add(new Vector2Int(nr, nc));
        }

        // Shuffle
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
            // Same row, different col — vertical wall between them
            int col = Mathf.Max(a.y, b.y);
            verticalWalls[a.x, col] = false;
        }
        else
        {
            // Same col, different row — horizontal wall between them
            int row = Mathf.Max(a.x, b.x);
            horizontalWalls[row, a.y] = false;
        }
    }

    private void CreateOpenAreas()
    {
        // Create open areas by removing internal walls of 2x2 cell blocks
        // Place them in strategic locations spread across the maze
        openAreaOrigins.Clear();

        // 3 open areas at different quadrants
        var candidates = new Vector2Int[] {
            new Vector2Int(4, 4),    // Q1-ish
            new Vector2Int(14, 14),  // Q4-ish
            new Vector2Int(9, 9),    // Center
        };

        foreach (var origin in candidates)
        {
            openAreaOrigins.Add(origin);
            // Remove all internal walls within this 2x2 block
            // Horizontal wall between row origin.x and origin.x+1 at col origin.y
            horizontalWalls[origin.x + 1, origin.y] = false;
            // Horizontal wall between row origin.x and origin.x+1 at col origin.y+1
            horizontalWalls[origin.x + 1, origin.y + 1] = false;
            // Vertical wall between col origin.y and origin.y+1 at row origin.x
            verticalWalls[origin.x, origin.y + 1] = false;
            // Vertical wall between col origin.y and origin.y+1 at row origin.x+1
            verticalWalls[origin.x + 1, origin.y + 1] = false;
        }
    }

    private void PlaceFloors(GameObject parent, GameObject floorPrefab)
    {
        var floorsParent = new GameObject("Floors");
        floorsParent.transform.SetParent(parent.transform, false);

        for (int r = 0; r < GRID_SIZE; r++)
        {
            for (int c = 0; c < GRID_SIZE; c++)
            {
                float x = c * CELL_SIZE + CELL_SIZE / 2f;
                float z = r * CELL_SIZE + CELL_SIZE / 2f;
                var floor = (GameObject)PrefabUtility.InstantiatePrefab(floorPrefab);
                floor.name = $"Floor_{r}_{c}";
                floor.transform.position = new Vector3(x, -FLOOR_THICKNESS / 2f, z);
                floor.transform.SetParent(floorsParent.transform, true);
                // Ensure navigation static
                GameObjectUtility.SetStaticEditorFlags(floor, StaticEditorFlags.NavigationStatic);
            }
        }
    }

    private void PlaceWalls(GameObject parent, GameObject wallPrefab, GameObject pillarPrefab)
    {
        var wallsParent = new GameObject("Walls");
        wallsParent.transform.SetParent(parent.transform, false);

        var pillarsParent = new GameObject("Pillars");
        pillarsParent.transform.SetParent(parent.transform, false);

        // Place horizontal walls (along X axis)
        for (int r = 0; r <= GRID_SIZE; r++)
        {
            for (int c = 0; c < GRID_SIZE; c++)
            {
                if (!horizontalWalls[r, c]) continue;

                float x = c * CELL_SIZE + CELL_SIZE / 2f;
                float z = r * CELL_SIZE;
                float y = WALL_HEIGHT / 2f;

                var wall = (GameObject)PrefabUtility.InstantiatePrefab(wallPrefab);
                wall.name = $"HWall_{r}_{c}";
                wall.transform.position = new Vector3(x, y, z);
                // Wall prefab is 4 along X by default, perfect for horizontal
                wall.transform.SetParent(wallsParent.transform, true);
            }
        }

        // Place vertical walls (along Z axis)
        for (int r = 0; r < GRID_SIZE; r++)
        {
            for (int c = 0; c <= GRID_SIZE; c++)
            {
                if (!verticalWalls[r, c]) continue;

                float x = c * CELL_SIZE;
                float z = r * CELL_SIZE + CELL_SIZE / 2f;
                float y = WALL_HEIGHT / 2f;

                var wall = (GameObject)PrefabUtility.InstantiatePrefab(wallPrefab);
                wall.name = $"VWall_{r}_{c}";
                wall.transform.position = new Vector3(x, y, z);
                wall.transform.rotation = Quaternion.Euler(0, 90, 0);
                wall.transform.SetParent(wallsParent.transform, true);
            }
        }

        // Place corner pillars at grid intersections where walls meet
        for (int r = 0; r <= GRID_SIZE; r++)
        {
            for (int c = 0; c <= GRID_SIZE; c++)
            {
                // Check if any adjacent wall exists
                bool hasWall = false;
                // Check horizontal walls adjacent to this corner
                if (c < GRID_SIZE && r <= GRID_SIZE && horizontalWalls[r, c]) hasWall = true;
                if (c > 0 && r <= GRID_SIZE && horizontalWalls[r, c - 1]) hasWall = true;
                // Check vertical walls adjacent to this corner
                if (r < GRID_SIZE && c <= GRID_SIZE && verticalWalls[r, c]) hasWall = true;
                if (r > 0 && c <= GRID_SIZE && verticalWalls[r - 1, c]) hasWall = true;

                if (!hasWall) continue;

                float x = c * CELL_SIZE;
                float z = r * CELL_SIZE;
                float y = WALL_HEIGHT / 2f;

                var pillar = (GameObject)PrefabUtility.InstantiatePrefab(pillarPrefab);
                pillar.name = $"Pillar_{r}_{c}";
                pillar.transform.position = new Vector3(x, y, z);
                pillar.transform.SetParent(pillarsParent.transform, true);
            }
        }
    }

    private void PlaceSpawnPoints(GameObject parent)
    {
        // Find dead-ends (cells with exactly 1 opening)
        var deadEnds = new List<Vector2Int>();
        var deadEndDirections = new Dictionary<Vector2Int, Vector2Int>(); // cell -> direction of opening

        for (int r = 0; r < GRID_SIZE; r++)
        {
            for (int c = 0; c < GRID_SIZE; c++)
            {
                int openings = 0;
                Vector2Int openDir = Vector2Int.zero;

                // Top wall
                if (!horizontalWalls[r + 1, c]) { openings++; openDir = new Vector2Int(1, 0); }
                // Bottom wall
                if (!horizontalWalls[r, c]) { openings++; openDir = new Vector2Int(-1, 0); }
                // Right wall
                if (c < GRID_SIZE && !verticalWalls[r, c + 1]) { openings++; openDir = new Vector2Int(0, 1); }
                // Left wall
                if (c > 0 && !verticalWalls[r, c]) { openings++; openDir = new Vector2Int(0, -1); }

                if (openings == 1)
                {
                    deadEnds.Add(new Vector2Int(r, c));
                    deadEndDirections[new Vector2Int(r, c)] = openDir;
                }
            }
        }

        Debug.Log($"Found {deadEnds.Count} dead-ends in maze.");

        // Select 8 player spawns from dead-ends, maximizing spread
        var playerSpawnCells = SelectSpreadSpawns(deadEnds, 8);
        var usedDeadEnds = new HashSet<Vector2Int>(playerSpawnCells);

        // Place player spawns
        for (int i = 0; i < playerSpawnCells.Count; i++)
        {
            var cell = playerSpawnCells[i];
            var pos = CellToWorld(cell);
            var dir = deadEndDirections.ContainsKey(cell) ? deadEndDirections[cell] : Vector2Int.zero;

            var go = new GameObject($"PlayerSpawn_{i + 1}");
            go.transform.position = new Vector3(pos.x, 0.5f, pos.z);
            // Face toward the corridor opening
            if (dir != Vector2Int.zero)
            {
                var worldDir = new Vector3(dir.y * CELL_SIZE, 0, dir.x * CELL_SIZE).normalized;
                if (worldDir.sqrMagnitude > 0.01f)
                    go.transform.forward = worldDir;
            }
            go.transform.SetParent(parent.transform, true);
            go.AddComponent<PlayerSpawnPoint>();
        }

        // Key spawn: center-ish, far from spawns and exit
        // Pick a cell near center that's not a dead-end used for players
        var keyCell = FindKeyPosition(playerSpawnCells);
        var keyPos = CellToWorld(keyCell);
        var keyGo = new GameObject("KeySpawn");
        keyGo.transform.position = new Vector3(keyPos.x, 0.5f, keyPos.z);
        keyGo.transform.SetParent(parent.transform, true);
        keyGo.AddComponent<KeySpawnPoint>();

        // Exit spawn: on maze edge
        var exitCell = new Vector2Int(GRID_SIZE - 1, GRID_SIZE - 1); // Bottom-right corner area
        // Open the outer wall to create an exit
        horizontalWalls[GRID_SIZE, GRID_SIZE - 1] = false;
        var exitPos = CellToWorld(exitCell);
        var exitGo = new GameObject("ExitSpawn");
        exitGo.transform.position = new Vector3(exitPos.x, 0.5f, (GRID_SIZE) * CELL_SIZE); // On the edge
        exitGo.transform.SetParent(parent.transform, true);
        exitGo.AddComponent<ExitSpawnPoint>();

        // Loot box spawns: distributed across quadrants
        PlaceLootBoxSpawns(parent, playerSpawnCells, keyCell);

        // Enemy waypoints: patrol routes distributed across quadrants
        PlaceEnemyWaypoints(parent, usedDeadEnds);
    }

    private List<Vector2Int> SelectSpreadSpawns(List<Vector2Int> candidates, int count)
    {
        if (candidates.Count <= count)
            return new List<Vector2Int>(candidates);

        var selected = new List<Vector2Int>();
        var remaining = new List<Vector2Int>(candidates);

        // Start with the corner-most dead-end
        remaining.Sort((a, b) =>
        {
            float da = Vector2Int.Distance(a, new Vector2Int(0, 0));
            float db = Vector2Int.Distance(b, new Vector2Int(0, 0));
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

    private Vector2Int FindKeyPosition(List<Vector2Int> playerSpawns)
    {
        // Find a cell that maximizes minimum distance from all player spawns
        // and is roughly in the center
        Vector2Int best = new Vector2Int(GRID_SIZE / 2, GRID_SIZE / 2);
        float bestScore = -1;

        for (int r = 3; r < GRID_SIZE - 3; r++)
        {
            for (int c = 3; c < GRID_SIZE - 3; c++)
            {
                var cell = new Vector2Int(r, c);
                float minDist = float.MaxValue;
                foreach (var sp in playerSpawns)
                {
                    float d = Vector2Int.Distance(cell, sp);
                    if (d < minDist) minDist = d;
                }

                // Must be at least 5 cells away from all spawns
                if (minDist >= 5f && minDist > bestScore)
                {
                    bestScore = minDist;
                    best = cell;
                }
            }
        }

        return best;
    }

    private void PlaceLootBoxSpawns(GameObject parent, List<Vector2Int> playerSpawns, Vector2Int keyCell)
    {
        var usedCells = new HashSet<Vector2Int>(playerSpawns);
        usedCells.Add(keyCell);

        // 4 quadrants, at least 4 per quadrant = 16+ loot boxes
        int halfGrid = GRID_SIZE / 2;
        var quadrants = new (int rMin, int rMax, int cMin, int cMax)[] {
            (0, halfGrid, 0, halfGrid),
            (0, halfGrid, halfGrid, GRID_SIZE),
            (halfGrid, GRID_SIZE, 0, halfGrid),
            (halfGrid, GRID_SIZE, halfGrid, GRID_SIZE)
        };

        int lootIdx = 0;
        foreach (var (rMin, rMax, cMin, cMax) in quadrants)
        {
            int placed = 0;
            for (int r = rMin; r < rMax && placed < 4; r++)
            {
                for (int c = cMin; c < cMax && placed < 4; c++)
                {
                    var cell = new Vector2Int(r, c);
                    if (usedCells.Contains(cell)) continue;

                    // Check it's not a chokepoint (cell with exactly 2 openings in a line)
                    int openings = CountOpenings(r, c);
                    if (openings < 2) continue;

                    usedCells.Add(cell);
                    var pos = CellToWorld(cell);
                    var go = new GameObject($"LootBoxSpawn_{++lootIdx}");
                    go.transform.position = new Vector3(pos.x, 0.1f, pos.z);
                    go.transform.SetParent(parent.transform, true);
                    go.AddComponent<LootBoxSpawnPoint>();
                    placed++;
                }
            }
        }

        Debug.Log($"Placed {lootIdx} loot box spawn points.");
    }

    private void PlaceEnemyWaypoints(GameObject parent, HashSet<Vector2Int> usedDeadEnds)
    {
        int halfGrid = GRID_SIZE / 2;
        var quadrants = new (int rMin, int rMax, int cMin, int cMax)[] {
            (0, halfGrid, 0, halfGrid),
            (0, halfGrid, halfGrid, GRID_SIZE),
            (halfGrid, GRID_SIZE, 0, halfGrid),
            (halfGrid, GRID_SIZE, halfGrid, GRID_SIZE)
        };

        int wpIdx = 0;
        int routeIdx = 0;

        foreach (var (rMin, rMax, cMin, cMax) in quadrants)
        {
            // Create 2 patrol routes per quadrant, 3 waypoints each = 24 total
            for (int route = 0; route < 2; route++)
            {
                int wpInRoute = 0;
                int startR = rMin + route * (rMax - rMin) / 2 + 1;
                int startC = cMin + 1;

                for (int r = startR; r < rMax && wpInRoute < 3; r += 2)
                {
                    for (int c = startC; c < cMax && wpInRoute < 3; c += 3)
                    {
                        var cell = new Vector2Int(r, c);
                        if (usedDeadEnds.Contains(cell)) continue;
                        if (CountOpenings(r, c) < 2) continue;

                        var pos = CellToWorld(cell);
                        var go = new GameObject($"EnemyWaypoint_{++wpIdx}");
                        go.transform.position = new Vector3(pos.x, 0.1f, pos.z);
                        go.transform.SetParent(parent.transform, true);
                        var wp = go.AddComponent<EnemyWaypointSpawnPoint>();
                        wp.patrolRouteIndex = routeIdx;
                        wp.waypointOrder = wpInRoute;
                        wpInRoute++;
                    }
                }
                routeIdx++;
            }
        }

        Debug.Log($"Placed {wpIdx} enemy waypoints in {routeIdx} patrol routes.");
    }

    private int CountOpenings(int r, int c)
    {
        int openings = 0;
        if (r + 1 <= GRID_SIZE && !horizontalWalls[r + 1, c]) openings++;
        if (r >= 0 && !horizontalWalls[r, c]) openings++;
        if (c + 1 <= GRID_SIZE && !verticalWalls[r, c + 1]) openings++;
        if (c >= 0 && !verticalWalls[r, c]) openings++;
        return openings;
    }

    private Vector3 CellToWorld(Vector2Int cell)
    {
        return new Vector3(
            cell.y * CELL_SIZE + CELL_SIZE / 2f,
            0f,
            cell.x * CELL_SIZE + CELL_SIZE / 2f
        );
    }

    private void SetupLighting(GameObject lightingParent)
    {
        // Check if directional light already exists under Lighting
        var existingLight = lightingParent.GetComponentInChildren<Light>();
        if (existingLight != null)
        {
            // Configure it
            existingLight.type = LightType.Directional;
            existingLight.color = new Color(1f, 0.96f, 0.9f);
            existingLight.intensity = 1.5f;
            existingLight.shadows = LightShadows.Soft;
            existingLight.transform.rotation = Quaternion.Euler(50, -30, 0);
        }

        // Set ambient light
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.4f, 0.4f, 0.45f);
    }

    private void MarkNavigationStatic(GameObject mazeParent)
    {
        // Mark all floor tiles as navigation static
        var floors = mazeParent.transform.Find("Floors");
        if (floors != null)
        {
            foreach (Transform child in floors)
            {
                GameObjectUtility.SetStaticEditorFlags(child.gameObject, StaticEditorFlags.NavigationStatic);
            }
        }

        // Ensure walls are NOT navigation static
        var walls = mazeParent.transform.Find("Walls");
        if (walls != null)
        {
            foreach (Transform child in walls)
            {
                GameObjectUtility.SetStaticEditorFlags(child.gameObject, 0);
            }
        }

        var pillars = mazeParent.transform.Find("Pillars");
        if (pillars != null)
        {
            foreach (Transform child in pillars)
            {
                GameObjectUtility.SetStaticEditorFlags(child.gameObject, 0);
            }
        }
    }

    private static GameObject FindOrCreateRoot(string name)
    {
        var go = GameObject.Find(name);
        if (go == null)
        {
            go = new GameObject(name);
        }
        return go;
    }

    private static void ClearChildren(GameObject parent)
    {
        while (parent.transform.childCount > 0)
        {
            Object.DestroyImmediate(parent.transform.GetChild(0).gameObject);
        }
    }
}
