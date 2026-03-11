## 1. Project Structure and Scene Setup

- [x] 1.1 Create the scene file at `Assets/Scenes/TestMaze.unity`
- [x] 1.2 Create organized root GameObjects in the scene hierarchy: `Maze` (or `MazeGeometry`), `SpawnPoints`, `Lighting`
- [x] 1.3 Ensure no loose objects exist at the hierarchy root other than designated parent containers and Main Camera
- [x] 1.4 Create the prefab folder at `Assets/Prefabs/Maze/`
- [x] 1.5 Create the materials folder at `Assets/Materials/Maze/` (or similar)

## 2. URP Materials

- [x] 2.1 Create a URP Lit wall material with flat gray base color (RGB approximately 0.5-0.7 per channel), no texture maps
- [x] 2.2 Create a URP Lit floor material with a darker/distinct flat color, visually distinguishable from walls, no texture maps
- [x] 2.3 Verify both materials render without pink/magenta error shading in URP

## 3. Maze Prefabs

- [x] 3.1 Create a straight wall prefab: 4 units long, 4 units high, 0.2-0.5 units thick, using the wall material. Save to `Assets/Prefabs/Maze/`
- [x] 3.2 Create a floor tile prefab: 4x4 units on XZ plane, 0.1-0.3 units thick, using the floor material. Save to `Assets/Prefabs/Maze/`
- [x] 3.3 Create a corner wall or pillar prefab to fill 90-degree wall junctions and prevent visual gaps. Save to `Assets/Prefabs/Maze/`
- [x] 3.4 Ensure all prefab pivots and positions align cleanly to the 4-unit grid (positions at multiples of 4.0)

## 4. Maze Layout Construction

- [x] 4.1 Design a 20x20 cell maze layout on paper or grid (each cell = 4x4 units, total footprint = 80x80 units) ensuring full connectivity (all cells reachable)
- [x] 4.2 Include at least 8 dead-end corridors suitable for player spawn placement
- [x] 4.3 Include at least 4 chokepoints where corridors narrow or intersect, creating defensive positions
- [x] 4.4 Include at least 2 open areas spanning 2x2 cells (8x8 units) or larger for combat encounters
- [x] 4.5 Ensure all corridors have a minimum clear width of 4 units (chokepoints no narrower than 3 units)
- [x] 4.6 Build the maze in the TestMaze scene using only prefab instances from `Assets/Prefabs/Maze/` -- no loose primitives
- [x] 4.7 Place all floor tiles for the 20x20 grid under the `Maze` parent GameObject
- [x] 4.8 Place all wall segments and corner pieces under the `Maze` parent GameObject, aligned to the 4-unit grid
- [x] 4.9 Verify maze geometry bounding box fits within 80x80 units on the XZ plane
- [x] 4.10 Verify every wall and floor tile has X and Z positions at multiples of 4.0

## 5. Lighting Setup

- [x] 5.1 Add a URP-compatible directional light to the scene under the `Lighting` parent GameObject
- [x] 5.2 Configure directional light with URP shadow settings for adequate corridor illumination
- [x] 5.3 Add ambient light (Environment Lighting in Lighting Settings) so no navigable area has zero illumination
- [x] 5.4 Verify all corridors are lit enough to identify objects at a distance of at least 8 units (2 cells)
- [x] 5.5 Verify no legacy light modes are used -- all lights must be URP-compatible

## 6. Spawn Point Scripts

- [x] 6.1 Create `PlayerSpawnPoint.cs` MonoBehaviour with green gizmo (sphere) drawn in `OnDrawGizmos`
- [x] 6.2 Create `KeySpawnPoint.cs` MonoBehaviour with gold/yellow gizmo drawn in `OnDrawGizmos`
- [x] 6.3 Create `ExitSpawnPoint.cs` MonoBehaviour with blue gizmo drawn in `OnDrawGizmos`
- [x] 6.4 Create `LootBoxSpawnPoint.cs` MonoBehaviour with orange gizmo drawn in `OnDrawGizmos`
- [x] 6.5 Create `EnemyWaypointSpawnPoint.cs` MonoBehaviour with red gizmo drawn in `OnDrawGizmos`
- [x] 6.6 Ensure each script has no MeshRenderer, MeshFilter, or Collider added -- spawn points are data markers only
- [x] 6.7 Verify spawn points are queryable at runtime via `FindObjectsByType<T>()` and return correct `transform.position`

## 7. Player Spawn Point Placement

- [x] 7.1 Place at least 8 player spawn point GameObjects (with `PlayerSpawnPoint` component) in dead-end corridors under the `SpawnPoints` parent
- [x] 7.2 Ensure no two player spawns share the same dead-end corridor
- [x] 7.3 Maximize minimum navigable path distance between all player spawn pairs
- [x] 7.4 Orient each spawn point's forward direction (transform.forward) toward the open corridor exit, away from the dead-end wall
- [x] 7.5 Ensure each spawn has at least 4 units of clearance in the forward direction for initial movement

## 8. Key Spawn Point Placement

- [x] 8.1 Place exactly 1 key spawn point GameObject (with `KeySpawnPoint` component) under the `SpawnPoints` parent
- [x] 8.2 Position the key spawn at least 20 units (5 cells) of navigable path distance from every player spawn point
- [x] 8.3 Position the key spawn at least 20 units (5 cells) of navigable path distance from the exit spawn point
- [x] 8.4 Ensure the key spawn is on a walkable corridor or open area, not inside a wall or in a player-reserved dead-end

## 9. Exit Spawn Point Placement

- [x] 9.1 Place exactly 1 exit spawn point GameObject (with `ExitSpawnPoint` component) under the `SpawnPoints` parent
- [x] 9.2 Position the exit on the maze perimeter, adjacent to an outer wall, with the exit path leading out of the maze
- [x] 9.3 Ensure the exit zone has room for a trigger collider area of at least 3x3 units
- [x] 9.4 Verify a valid NavMesh path exists from every player spawn point to the exit

## 10. Loot Box Spawn Point Placement

- [x] 10.1 Place at least 15 loot box spawn point GameObjects (with `LootBoxSpawnPoint` component) under the `SpawnPoints` parent
- [x] 10.2 Distribute loot box spawns so each maze quadrant contains at least 2
- [x] 10.3 Ensure each loot box spawn is at least 1.0 unit from any wall surface
- [x] 10.4 Ensure no loot box spawn is positioned directly in a chokepoint or doorway where it would block movement
- [x] 10.5 Ensure all loot box spawns are on walkable floor surfaces at Y near 0

## 11. Enemy Patrol Waypoint Placement

- [x] 11.1 Place at least 20 enemy waypoint GameObjects (with `EnemyWaypointSpawnPoint` component) under the `SpawnPoints` parent
- [x] 11.2 Distribute waypoints so each maze quadrant contains at least 3
- [x] 11.3 Ensure each waypoint is at least 1.0 unit from any wall surface
- [x] 11.4 Ensure each waypoint is on a walkable NavMesh surface
- [x] 11.5 Organize waypoints into patrol routes of at least 3 waypoints each, with consecutive waypoints within 16 units of NavMesh path distance
- [x] 11.6 Verify a valid NavMesh path exists between each consecutive pair of waypoints in every route

## 12. NavMesh Bake

- [x] 12.1 Mark all floor tile GameObjects as Navigation Static with the Walkable area type
- [x] 12.2 Mark all wall GameObjects as either not Navigation Static, or as Not Walkable area type
- [x] 12.3 Set NavMesh agent radius to no more than 0.5 units
- [x] 12.4 Set NavMesh agent height to at least 2.0 units
- [x] 12.5 Set NavMesh step height to at least 0.3 units
- [x] 12.6 Bake the NavMesh at edit time (not runtime) -- do not add NavMeshSurface runtime bake components
- [x] 12.7 Verify NavMesh covers all navigable floor surfaces (corridors, open areas, dead-ends) with no gaps
- [x] 12.8 Verify no NavMesh exists on wall tops, wall sides, or outside the maze boundary
- [x] 12.9 Verify NavMesh edges at corners are carved inward by the agent radius for smooth corner navigation

## 13. NavMesh Validation

- [x] 13.1 Verify NavMesh path succeeds (PathComplete) between every pair of player spawn points
- [x] 13.2 Verify NavMesh path succeeds from every player spawn to the key spawn point
- [x] 13.3 Verify NavMesh path succeeds from every player spawn to the exit spawn point
- [x] 13.4 Verify NavMesh path succeeds between all consecutive enemy waypoints in each patrol route
- [x] 13.5 Verify NavMesh path succeeds from every enemy waypoint to every player spawn point
- [x] 13.6 Verify an agent can navigate 90-degree corners, T-intersections, and consecutive turns without getting stuck
- [x] 13.7 Verify an agent can navigate through the narrowest chokepoint (minimum 3 units wide) without stopping or oscillating
- [x] 13.8 Verify the NavMesh persists after closing and reopening the scene (no re-bake needed on load)
