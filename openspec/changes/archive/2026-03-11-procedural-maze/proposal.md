## Why

Phase 1 uses a hardcoded static 20x20 test maze. The PRD requires procedurally generated mazes so no two sessions play the same. This is Phase 2's core deliverable — without it, replayability (a core pillar) doesn't exist.

## What Changes

- Replace static maze geometry with runtime-generated maze using Recursive Backtracker (DFS) algorithm
- Add configurable maze parameters: grid size (default 20x20), corridor width (3-4 Unity units), random seed per match
- Implement full placement pipeline: exit on maze edge → player spawns in dead-ends → key placement with min distance constraints → loot box scattering (50-attempt guard) → enemy patrol waypoints
- Runtime NavMesh baking after maze generation
- Integrate generated maze with all existing Phase 1 systems (KeyManager, ExitGateway, MatchManager, EnemySpawner, LootBoxSpawner)
- Remove static maze prefabs/objects, replaced by runtime geometry

## Capabilities

### New Capabilities
- `maze-generation`: Recursive Backtracker algorithm producing a wall/floor grid, configurable size and corridor width, seeded RNG for reproducibility
- `placement-pipeline`: Sequential placement of exit, player spawns, key, loot boxes, and enemy waypoints with distance constraints and fallback guards
- `runtime-navmesh`: Runtime NavMesh baking after procedural geometry is created, replacing design-time baked NavMesh

### Modified Capabilities
- None — Phase 1 systems (key-exit, enemy-ai, loot-boxes, player-controller) keep their existing requirements; only their integration points change at the implementation level.

## Impact

- **Scripts**: New MazeGenerator, PlacementPipeline, RuntimeNavMesh scripts. Modifications to MatchManager (trigger generation on match start), EnemySpawner and LootBoxSpawner (receive spawn positions from pipeline instead of hardcoded)
- **Prefabs**: Static maze objects removed. New wall/floor prefab for runtime instantiation. Existing prefabs (LootBox, enemies, key, exit) unchanged
- **Dependencies**: May need Unity AI Navigation package for runtime NavMesh baking (NavMeshSurface component)
- **Performance**: Maze generation + NavMesh bake adds startup time per match (target < 2 seconds)
