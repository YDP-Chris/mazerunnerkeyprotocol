## Context

Phase 1 uses `StaticMazeBuilder.cs` — an editor-only tool that pre-generates a 20x20 maze at design time. The maze geometry, spawn points, and NavMesh are all baked in the editor. For Phase 2, we need runtime procedural generation so every match has a unique maze.

Key discovery: most Phase 1 spawners (EnemySpawner, LootBoxSpawner, KeyManager) already do their own random placement at runtime using NavMesh sampling. They only need: (1) baked NavMesh, (2) PlayerSpawnPoint components for exclusion zones, (3) ExitSpawnPoint for key distance checks, and (4) correct maze bounds. The procedural generator doesn't need to place enemies, loot, or keys directly — just provide the infrastructure these systems rely on.

## Goals / Non-Goals

**Goals:**
- Runtime maze generation using Recursive Backtracker (same algorithm StaticMazeBuilder already uses)
- Seeded RNG for reproducible mazes (host generates seed, shares with clients)
- Configurable grid size (default 20x20) and corridor width (default 4 units)
- Full placement pipeline: exit → player spawns → spawn point markers
- Runtime NavMesh baking after geometry creation
- Update existing spawners to receive maze bounds dynamically instead of hardcoded values
- Generation completes in < 2 seconds

**Non-Goals:**
- Multiplayer seed sync (Phase 3)
- Multiple exits or keys
- Maze themes or visual variation
- Dynamic maze changes during a match
- Replacing the spawner logic in KeyManager, EnemySpawner, or LootBoxSpawner — they work fine with NavMesh

## Decisions

### 1. Runtime script vs Editor tool
**Decision:** Create a new `MazeGenerator` runtime MonoBehaviour/NetworkBehaviour that runs on Awake/Start, replacing StaticMazeBuilder (editor-only).

**Why not modify StaticMazeBuilder:** It uses EditorWindow, Undo, PrefabUtility — all editor-only APIs. Cleaner to write a runtime version that reuses the same algorithm.

**Alternative considered:** Making StaticMazeBuilder dual-purpose — rejected because editor APIs can't run at runtime.

### 2. Maze algorithm
**Decision:** Recursive Backtracker (iterative stack-based variant to avoid stack overflow on large grids).

**Rationale:** Already proven in StaticMazeBuilder. PRD explicitly recommends it. Produces long corridors good for shooter gameplay.

### 3. NavMesh baking
**Decision:** Use Unity's `NavMeshSurface` component for runtime baking via `BuildNavMesh()`.

**Rationale:** The AI Navigation package supports runtime baking through NavMeshSurface. This replaces the editor-time "Navigation Static" marking approach.

**Alternative considered:** NavMeshBuilder.UpdateNavMeshData — lower level, more complex, no advantage for our use case.

### 4. Geometry approach
**Decision:** Instantiate wall/floor/pillar prefabs at runtime, same as StaticMazeBuilder does at edit time.

**Rationale:** Reuses existing prefabs (Wall.prefab, FloorTile.prefab, CornerPillar.prefab). Consistent visual style. Simple and proven.

**Alternative considered:** Single-mesh generation — better performance but much more complex, premature optimization.

### 5. Maze bounds distribution
**Decision:** MazeGenerator exposes static properties (MinX, MaxX, MinZ, MaxZ) that spawners read instead of inspector-configured values.

**Rationale:** Eliminates hardcoded bounds. Spawners already have these as serialized fields — we just need to override them at runtime or have spawners check MazeGenerator first.

### 6. Generation ordering
**Decision:** Sequential pipeline triggered by MazeGenerator:
1. Generate logical grid (wall/floor arrays)
2. Instantiate geometry (walls, floors, pillars)
3. Place exit on maze edge (create ExitSpawnPoint + open wall)
4. Place player spawns in dead-ends (create PlayerSpawnPoint components)
5. Add NavMeshSurface to floor parent and call BuildNavMesh()
6. Existing spawners (KeyManager, EnemySpawner, LootBoxSpawner) run after NavMesh is ready

**Rationale:** Steps 1-5 must be sequential. Step 6 uses existing systems unmodified. A "maze ready" event signals spawners to proceed.

## Risks / Trade-offs

- **[Risk] Runtime NavMesh bake may be slow on large grids** → Mitigation: Profile at 20x20. Can reduce to 15x15 if needed. NavMeshSurface.BuildNavMesh() is typically fast for simple geometry.
- **[Risk] Prefab instantiation at runtime creates many GameObjects** → Mitigation: Acceptable for 20x20 (~400 floors + ~800 walls). Object pooling is premature at this scale.
- **[Risk] Spawners may run before NavMesh is ready** → Mitigation: MazeGenerator fires an event/callback; spawners wait for it. Or use script execution order.
- **[Trade-off] Keeping existing spawner logic means key/enemy/loot placement isn't deterministic from maze seed** → Acceptable for Phase 2. Can add seeded placement later if needed.
