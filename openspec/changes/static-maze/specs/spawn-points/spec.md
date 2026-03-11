## ADDED Requirements

### Requirement: Spawn Point Architecture

All spawn points MUST be implemented as empty GameObjects with a dedicated MonoBehaviour component per spawn type. Each spawn type SHALL use a distinct gizmo color and icon for editor visibility. Spawn point GameObjects MUST NOT have mesh renderers or colliders -- they are data markers only.

#### Scenario: Spawn point component structure

- **WHEN** a spawn point GameObject is inspected in the Unity Editor
- **THEN** it SHALL have exactly one spawn-type MonoBehaviour component (e.g., `PlayerSpawnPoint`, `KeySpawnPoint`, `ExitSpawnPoint`, `LootBoxSpawnPoint`, or `EnemyWaypointSpawnPoint`)
- **THEN** it SHALL NOT have a MeshRenderer, MeshFilter, or Collider component

#### Scenario: Editor gizmo visibility

- **WHEN** the maze scene is viewed in the Unity Editor Scene view with gizmos enabled
- **THEN** each spawn point type SHALL render a distinct colored gizmo (sphere, icon, or wireframe shape) visible at the default scene zoom level

#### Scenario: Gizmo color differentiation

- **WHEN** multiple spawn point types are visible simultaneously in the Scene view
- **THEN** each type SHALL use a unique color: player spawns (green), key spawn (gold/yellow), exit spawn (blue), loot box spawns (orange), enemy waypoints (red)

---

### Requirement: Player Spawn Points

The maze MUST contain at least 8 player spawn points to support the maximum player count. Player spawn points SHALL be placed in dead-end corridors and MUST be positioned at maximum navigable distance from each other.

#### Scenario: Minimum player spawn count

- **WHEN** all player spawn points in the scene are counted
- **THEN** there SHALL be at least 8 player spawn points

#### Scenario: Dead-end placement

- **WHEN** a player spawn point position is evaluated
- **THEN** the spawn point SHALL be located within a dead-end corridor (a corridor with only one navigable exit direction)

#### Scenario: Maximum separation

- **WHEN** the NavMesh path distance between any two player spawn points is measured
- **THEN** no two player spawn points SHALL be in the same dead-end corridor
- **THEN** the spawn point placement SHALL maximize the minimum path distance between any pair of spawns

#### Scenario: Navigable position

- **WHEN** a player character is spawned at any player spawn point
- **THEN** the character SHALL be standing on walkable ground with at least 4 Unity units of clearance in at least one direction for initial movement

#### Scenario: Spawn orientation

- **WHEN** a player spawns at a player spawn point
- **THEN** the spawn point's forward direction (transform.forward) SHALL face toward the open corridor exit of the dead-end, not toward a wall

---

### Requirement: Key Spawn Point

The maze MUST contain exactly one key spawn point. The key spawn point MUST be placed at a minimum navigable distance from all player spawn points and from the exit spawn point.

#### Scenario: Single key spawn

- **WHEN** all key spawn points in the scene are counted
- **THEN** there SHALL be exactly 1 key spawn point

#### Scenario: Distance from player spawns

- **WHEN** the NavMesh path distance from the key spawn point to each player spawn point is measured
- **THEN** the key spawn point SHALL be at least 20 Unity units (5 cells) of navigable path distance from every player spawn point

#### Scenario: Distance from exit

- **WHEN** the NavMesh path distance from the key spawn point to the exit spawn point is measured
- **THEN** the key spawn point SHALL be at least 20 Unity units (5 cells) of navigable path distance from the exit spawn point

#### Scenario: Accessible location

- **WHEN** the key spawn point position is evaluated
- **THEN** it SHALL be on a walkable NavMesh surface within a navigable corridor or open area -- not inside a wall, outside the maze, or in an unreachable location

#### Scenario: Not in a dead-end reserved for players

- **WHEN** the key spawn point position is compared to player spawn point positions
- **THEN** the key spawn point SHALL NOT be located in the same dead-end corridor as any player spawn point

---

### Requirement: Exit Spawn Point

The maze MUST contain exactly one exit spawn point. The exit MUST be placed on the edge of the maze (adjacent to an outer wall).

#### Scenario: Single exit spawn

- **WHEN** all exit spawn points in the scene are counted
- **THEN** there SHALL be exactly 1 exit spawn point

#### Scenario: Edge placement

- **WHEN** the exit spawn point position is evaluated
- **THEN** the exit spawn point SHALL be located at the maze perimeter, adjacent to an outer wall, with the exit path leading out of the maze bounds

#### Scenario: Exit zone size

- **WHEN** the exit spawn point is used to define the escape trigger zone
- **THEN** the exit zone SHALL have a trigger collider area of at least 3x3 Unity units, large enough for a player character to enter without pixel-perfect positioning

#### Scenario: Navigable from all points

- **WHEN** a NavMesh path is calculated from any player spawn point to the exit spawn point
- **THEN** a valid path SHALL exist (the exit MUST be reachable from every spawn point)

---

### Requirement: Loot Box Spawn Points

The maze MUST contain spawn points for loot box placement distributed throughout the maze. Loot box spawn points SHALL be placed on open floor tiles in corridors and open areas.

#### Scenario: Minimum loot box spawn count

- **WHEN** all loot box spawn points in the scene are counted
- **THEN** there SHALL be at least 15 loot box spawn points for the default 20x20 maze

#### Scenario: Distribution across maze

- **WHEN** the maze is divided into four equal quadrants
- **THEN** each quadrant SHALL contain at least 2 loot box spawn points, ensuring coverage across the entire maze

#### Scenario: Corridor clearance

- **WHEN** a loot box spawn point position is evaluated
- **THEN** the spawn point SHALL be at least 1.0 Unity unit away from any wall surface, ensuring a spawned loot box does not clip into walls

#### Scenario: Not blocking critical paths

- **WHEN** a loot box spawn point is placed
- **THEN** it SHALL NOT be positioned directly in a chokepoint or doorway where a spawned loot box would block player movement entirely

#### Scenario: Walkable surface

- **WHEN** a loot box spawn point position is evaluated
- **THEN** it SHALL be located on a walkable NavMesh surface at floor level (Y near 0)

---

### Requirement: Enemy Patrol Waypoints

The maze MUST contain enemy patrol waypoints that define patrol routes for enemy AI. Waypoints SHALL be connected into patrol routes that form walkable loops or paths through the maze.

#### Scenario: Minimum waypoint count

- **WHEN** all enemy waypoint spawn points in the scene are counted
- **THEN** there SHALL be at least 20 enemy patrol waypoints distributed throughout the maze

#### Scenario: Waypoint NavMesh placement

- **WHEN** an enemy patrol waypoint position is evaluated
- **THEN** the waypoint SHALL be located on a walkable NavMesh surface

#### Scenario: Patrol route connectivity

- **WHEN** patrol waypoints are assigned to patrol routes
- **THEN** each route SHALL contain at least 3 waypoints
- **THEN** a valid NavMesh path SHALL exist between each consecutive pair of waypoints in the route

#### Scenario: Coverage distribution

- **WHEN** the maze is divided into four equal quadrants
- **THEN** each quadrant SHALL contain at least 3 enemy patrol waypoints, ensuring enemies patrol throughout the maze rather than clustering in one area

#### Scenario: Waypoint corridor clearance

- **WHEN** an enemy patrol waypoint position is evaluated
- **THEN** the waypoint SHALL be at least 1.0 Unity unit from any wall surface, ensuring an enemy AI agent can navigate to the waypoint without colliding with walls

#### Scenario: Waypoint-to-waypoint line of sight

- **WHEN** two consecutive waypoints in a patrol route are evaluated
- **THEN** they SHOULD be within 16 Unity units (4 cells) of NavMesh path distance from each other, preventing excessively long patrol legs that reduce encounter frequency

---

### Requirement: Spawn Point Queryability

All spawn points MUST be discoverable at runtime through code. Game systems SHALL be able to query spawn points by type without hard-coded references to specific GameObjects.

#### Scenario: Find all spawn points by type

- **WHEN** a game system calls `FindObjectsByType<PlayerSpawnPoint>()` (or equivalent for any spawn type)
- **THEN** it SHALL return all spawn points of that type placed in the scene

#### Scenario: Spawn point position access

- **WHEN** a spawn point component is accessed at runtime
- **THEN** its `transform.position` SHALL return the world-space position where the associated entity should be spawned

#### Scenario: Random selection from spawn points

- **WHEN** a game system needs to select a random player spawn point
- **THEN** it SHALL be able to retrieve the full list of player spawn points and select from them without errors or null references
