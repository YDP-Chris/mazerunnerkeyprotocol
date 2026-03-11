## ADDED Requirements

### Requirement: NavMesh Bake Coverage

The static maze MUST have a baked Unity NavMesh that covers all navigable floor surfaces. Every walkable corridor, open area, and dead-end in the maze SHALL be included in the NavMesh.

#### Scenario: Full floor coverage

- **WHEN** the NavMesh is visualized in the Unity Editor (Navigation window)
- **THEN** blue NavMesh surface SHALL be visible on every floor tile in every navigable corridor, open area, and dead-end

#### Scenario: No NavMesh on walls

- **WHEN** the NavMesh is visualized
- **THEN** no NavMesh surface SHALL exist on top of walls, on wall sides, or on any non-floor geometry

#### Scenario: No gaps in corridors

- **WHEN** a NavMesh path is calculated between any two walkable floor positions in the maze
- **THEN** the path SHALL succeed (NavMeshPath.status == PathComplete) if a navigable route exists between those positions in the maze layout

#### Scenario: Dead-end accessibility

- **WHEN** a NavMesh path is calculated from a dead-end corridor floor to the nearest corridor intersection
- **THEN** the path SHALL succeed, confirming dead-ends are connected to the NavMesh

---

### Requirement: NavMesh Agent Settings

The NavMesh MUST be baked with agent settings compatible with the enemy AI agents defined in the project. Agent radius and height SHALL allow enemies to navigate all corridors without getting stuck.

#### Scenario: Agent radius clearance

- **WHEN** the NavMesh is baked
- **THEN** the NavMesh agent radius SHALL be set to no more than 0.5 Unity units, ensuring agents can navigate the minimum 3-unit-wide chokepoints with clearance

#### Scenario: Agent height setting

- **WHEN** the NavMesh is baked
- **THEN** the NavMesh agent height SHALL be set to at least 2.0 Unity units, matching expected enemy character height

#### Scenario: Step height

- **WHEN** the NavMesh is baked
- **THEN** the NavMesh step height SHALL be set to at least 0.3 Unity units to handle minor floor surface irregularities without agents getting stuck on seams between floor tiles

#### Scenario: Agent navigates narrow chokepoint

- **WHEN** an enemy AI agent with NavMeshAgent component attempts to path through the narrowest chokepoint in the maze (minimum 3 Unity units wide)
- **THEN** the agent SHALL successfully navigate through without stopping, oscillating, or getting stuck on the walls

---

### Requirement: NavMesh Obstacle Avoidance at Corners

Enemy AI agents navigating the maze MUST NOT get stuck on maze corners. The NavMesh bake settings and maze geometry MUST cooperate to provide smooth corner navigation.

#### Scenario: 90-degree corner navigation

- **WHEN** an enemy AI agent paths around a 90-degree corridor corner
- **THEN** the agent SHALL smoothly follow the path around the corner without stopping, reversing, or clipping into the wall geometry

#### Scenario: T-intersection navigation

- **WHEN** an enemy AI agent paths through a T-intersection
- **THEN** the agent SHALL navigate the turn without hesitation or collision with the wall at the intersection

#### Scenario: Corner carving

- **WHEN** the NavMesh is baked around wall corners
- **THEN** the NavMesh edge at corners SHALL be carved inward by the agent radius, preventing agents from pathing into corner geometry

#### Scenario: Consecutive turns

- **WHEN** an enemy AI agent navigates a section of maze with multiple consecutive turns (e.g., an S-curve or zigzag corridor)
- **THEN** the agent SHALL navigate all turns in sequence without getting stuck at any individual corner

---

### Requirement: NavMesh Path Validity for All Spawn Points

A valid NavMesh path MUST exist between every pair of spawn points in the maze. This ensures all game entities can reach each other through AI pathfinding.

#### Scenario: Player spawn to player spawn

- **WHEN** a NavMesh path is calculated between any two player spawn points
- **THEN** the path SHALL complete successfully (PathComplete status)

#### Scenario: Player spawn to key spawn

- **WHEN** a NavMesh path is calculated from any player spawn point to the key spawn point
- **THEN** the path SHALL complete successfully

#### Scenario: Player spawn to exit

- **WHEN** a NavMesh path is calculated from any player spawn point to the exit spawn point
- **THEN** the path SHALL complete successfully

#### Scenario: Enemy waypoint to enemy waypoint

- **WHEN** a NavMesh path is calculated between any two enemy patrol waypoints
- **THEN** the path SHALL complete successfully

#### Scenario: Enemy waypoint to player spawn

- **WHEN** a NavMesh path is calculated from any enemy patrol waypoint to any player spawn point
- **THEN** the path SHALL complete successfully, confirming enemies can chase players to any location in the maze

---

### Requirement: NavMesh Bake as Edit-Time Asset

The NavMesh MUST be baked at edit time and stored as part of the scene data. Runtime NavMesh baking SHALL NOT be used for the static maze.

#### Scenario: NavMesh persistence

- **WHEN** the TestMaze scene is loaded in Play mode
- **THEN** the NavMesh SHALL be immediately available without any runtime bake step or loading delay

#### Scenario: NavMesh stored with scene

- **WHEN** the project files are inspected
- **THEN** NavMesh bake data SHALL be stored in the scene's associated data folder (Unity's default NavMesh storage location for the scene)

#### Scenario: No runtime bake components

- **WHEN** the scene hierarchy is inspected
- **THEN** there SHALL be no GameObjects with `NavMeshSurface` runtime bake components or scripts that invoke `NavMeshSurface.BuildNavMesh()` at runtime

#### Scenario: NavMesh survives scene reload

- **WHEN** the TestMaze scene is closed and reopened in the Unity Editor
- **THEN** the NavMesh SHALL still be present and valid without requiring a re-bake

---

### Requirement: NavMesh Area Configuration

The NavMesh MUST use the default Walkable area type for all navigable surfaces. Non-navigable surfaces (walls, maze exterior) MUST be excluded from the NavMesh or marked as Not Walkable.

#### Scenario: Floor surfaces are Walkable

- **WHEN** floor tile GameObjects are inspected for their Navigation Static settings
- **THEN** they SHALL be marked as Navigation Static with the Walkable area type

#### Scenario: Wall surfaces are excluded

- **WHEN** wall GameObjects are inspected for their Navigation Static settings
- **THEN** they SHALL either not be marked as Navigation Static, or be marked with the Not Walkable area type

#### Scenario: Maze exterior excluded

- **WHEN** the NavMesh is visualized
- **THEN** no NavMesh surface SHALL exist outside the maze boundary walls
