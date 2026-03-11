## Why

A playable maze environment is needed before procedural generation (Phase 2). A hand-built static maze provides an immediate test environment for all Phase 1 systems: player movement, combat, key/exit mechanics, enemy AI, and loot boxes. It also establishes the visual style and spatial constraints (corridor width, wall height) that procedural generation must match.

## What Changes

- Build a static test maze in Unity with walls, floors, and corridors
- Establish corridor width (3-4 Unity units) and wall height standards
- Create chokepoints, dead-ends, and open areas for tactical variety
- Add spawn point markers for players, key, exit, loot boxes, and enemies
- Set up lighting and basic URP materials for maze surfaces
- Bake NavMesh for enemy AI pathfinding

## Capabilities

### New Capabilities
- `maze-environment`: Static maze level with walls, floors, corridors meeting the 3-4 unit width requirement, including chokepoints and dead-ends
- `spawn-points`: Configurable spawn point system for players, key, exit, loot boxes, and enemy patrol waypoints
- `navmesh-setup`: Baked NavMesh on the static maze for AI pathfinding

### Modified Capabilities

## Impact

- New scene `Assets/Scenes/TestMaze.unity`
- New materials in `Assets/Materials/`
- New prefabs for maze wall/floor tiles in `Assets/Prefabs/Maze/`
- NavMesh bake data stored with scene
- Spawn point GameObjects with gizmo visualization for editor
