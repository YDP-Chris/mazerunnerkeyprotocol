## 1. MazeGenerator Core

- [x] 1.1 Create MazeGenerator.cs NetworkBehaviour with configurable parameters (gridWidth, gridHeight, cellSize, wallHeight, seed) and static bounds properties
- [x] 1.2 Implement Recursive Backtracker algorithm (iterative stack-based) producing verticalWalls and horizontalWalls boolean arrays
- [x] 1.3 Implement runtime geometry instantiation — floors, walls, and corner pillars under a "Maze" parent, using existing prefabs from Assets/Prefabs/Maze/
- [x] 1.4 Add clean regeneration — destroy existing maze geometry before generating new

## 2. Placement Pipeline

- [x] 2.1 Implement exit placement — find edge cell, open boundary wall, create ExitSpawnPoint GameObject with ExitSpawnPoint component
- [x] 2.2 Implement player spawn placement — find dead-end cells, greedy max-distance selection for 4-8 spawns, create PlayerSpawnPoint GameObjects with correct orientation
- [x] 2.3 Wire pipeline execution order: grid generation → geometry → exit → player spawns

## 3. Runtime NavMesh

- [x] 3.1 Add NavMeshSurface component setup on floor parent and call BuildNavMesh() after geometry and placement complete
- [x] 3.2 Implement MazeReady event/callback that fires after NavMesh bake completes

## 4. Integration with Existing Systems

- [x] 4.1 Update EnemySpawner and LootBoxSpawner to read maze bounds from MazeGenerator instead of hardcoded inspector values
- [x] 4.2 Update KeyManager to use MazeGenerator bounds for placement range
- [x] 4.3 Wire spawner startup to wait for MazeReady event before running placement logic
- [x] 4.4 Remove or disable static maze objects from the scene — MazeGenerator replaces StaticMazeBuilder output

## 5. Scene Setup & Testing

- [x] 5.1 Add MazeGenerator to the scene with references to wall/floor/pillar prefabs
- [x] 5.2 Verify end-to-end: maze generates → NavMesh bakes → spawners place key/enemies/loot → match plays correctly
- [x] 5.3 Test seed reproducibility — same seed produces identical maze layout
