## ADDED Requirements

### Requirement: NavMesh Agent-Based Pathfinding

All enemy movement SHALL use Unity's NavMesh Agent component. Enemies MUST NOT use manual transform manipulation or physics-based movement for navigation. The NavMesh Agent SHALL handle pathfinding, steering, and obstacle avoidance automatically.

#### Scenario: Enemy moves to destination via NavMesh

- **WHEN** the state machine sets a navigation destination (patrol waypoint, sound origin, player position, or last-known position)
- **THEN** the enemy SHALL use `NavMeshAgent.SetDestination()` to navigate to that position along a valid NavMesh path

#### Scenario: NavMesh Agent handles path recalculation

- **WHEN** an enemy's destination changes (e.g., chasing a moving player)
- **THEN** the NavMesh Agent SHALL recalculate the path to the new destination automatically

#### Scenario: Enemy does not move without NavMesh Agent

- **WHEN** the NavMesh Agent component is disabled or missing
- **THEN** the enemy SHALL not move, and the state machine SHALL log a warning

### Requirement: NavMesh Bake After Maze Generation

The NavMesh SHALL be baked at runtime after the procedural maze is generated. Enemy spawning and patrol waypoint assignment MUST NOT occur until the NavMesh bake is complete. The bake SHALL cover all walkable floor surfaces within the maze.

#### Scenario: NavMesh baked after maze geometry is placed

- **WHEN** the maze generation pipeline completes placing wall and floor geometry
- **THEN** the NavMesh SHALL be baked before any enemies are spawned or waypoints are assigned

#### Scenario: Enemies wait for NavMesh bake completion

- **WHEN** enemies are queued for spawning
- **THEN** enemy instantiation and NavMesh Agent activation SHALL be deferred until the NavMesh bake has fully completed

#### Scenario: NavMesh covers walkable areas

- **WHEN** the NavMesh is baked
- **THEN** all floor tiles within maze corridors SHALL be marked as walkable NavMesh surface, and wall tiles SHALL be excluded

#### Scenario: NavMesh bake uses correct agent settings

- **WHEN** the NavMesh is baked at runtime
- **THEN** the agent radius and height settings SHALL match the enemy agent dimensions so that enemies can navigate corridors that are 3-4 Unity units wide without getting stuck

### Requirement: Obstacle Avoidance

Enemies SHALL use NavMesh Agent obstacle avoidance to prevent getting stuck on maze corners, other enemies, or dynamic obstacles. The avoidance priority and radius SHALL be configured to handle the narrow corridor geometry of the maze.

#### Scenario: Enemy navigates around maze corners

- **WHEN** an enemy's path requires turning at a maze corner
- **THEN** the NavMesh Agent's steering SHALL smoothly navigate the corner without the enemy getting stuck on wall colliders

#### Scenario: Enemies avoid each other

- **WHEN** two enemies approach each other in a corridor
- **THEN** the NavMesh Agent obstacle avoidance SHALL prevent them from overlapping or blocking each other indefinitely

#### Scenario: Enemy avoidance radius fits maze corridors

- **WHEN** the NavMesh Agent avoidance radius is configured
- **THEN** it SHALL be small enough that two enemies can pass each other in a standard corridor (3-4 Unity units wide) without deadlocking

#### Scenario: Enemy recovers from stuck position

- **WHEN** an enemy's NavMesh Agent reports that its path is invalid or it has not made progress toward its destination for a configurable timeout (e.g., 3 seconds)
- **THEN** the enemy SHALL attempt to recalculate its path, and if still stuck, SHALL return to the nearest patrol waypoint

### Requirement: Patrol Waypoint System

Patrol routes SHALL be defined as ordered lists of waypoint positions. Waypoints SHALL be generated during the maze generation pipeline at valid NavMesh positions. Each patrolling enemy (Grunt) SHALL be assigned a patrol route at spawn time.

#### Scenario: Waypoints generated at valid NavMesh positions

- **WHEN** patrol waypoints are created during the maze generation pipeline
- **THEN** each waypoint position SHALL be validated as a point on the baked NavMesh using `NavMesh.SamplePosition()`

#### Scenario: Patrol route assigned to Grunt at spawn

- **WHEN** a Grunt enemy is spawned
- **THEN** it SHALL be assigned a patrol route consisting of an ordered list of waypoint positions from the generated waypoint pool

#### Scenario: Guard has no patrol route

- **WHEN** a Guard enemy is spawned
- **THEN** it SHALL NOT be assigned a patrol route; its stationary position near the key spawn serves as its sole reference point

#### Scenario: Waypoint arrival threshold

- **WHEN** an enemy navigating to a patrol waypoint is within a configurable arrival distance (default 0.5 Unity units) of the waypoint
- **THEN** the enemy SHALL consider the waypoint reached and advance to the next waypoint in its route

#### Scenario: Patrol routes avoid dead-ends where possible

- **WHEN** patrol routes are generated
- **THEN** the waypoint generation system SHALL prefer positions along corridors and intersections over dead-end positions, to produce routes that cover more of the maze

### Requirement: Speed Configuration Per State

Enemy movement speed SHALL be configurable per state and per enemy type. The NavMesh Agent speed SHALL be updated when the state machine transitions between states.

#### Scenario: PATROL state uses slow speed

- **WHEN** an enemy enters the PATROL state
- **THEN** the NavMesh Agent speed SHALL be set to the enemy's configured patrol speed (slowest movement speed)

#### Scenario: INVESTIGATE state uses alert speed

- **WHEN** an enemy enters the INVESTIGATE state
- **THEN** the NavMesh Agent speed SHALL be set to the enemy's configured alert speed (between patrol and chase speed)

#### Scenario: CHASE state uses fast speed

- **WHEN** an enemy enters the CHASE state
- **THEN** the NavMesh Agent speed SHALL be set to the enemy's configured chase speed (fastest movement speed)

#### Scenario: ATTACK state uses reduced speed

- **WHEN** an enemy enters the ATTACK state
- **THEN** the NavMesh Agent speed SHALL be set to the enemy's configured attack movement speed (slower than chase, allowing the enemy to track but not at full speed)

#### Scenario: SEARCH state uses alert speed

- **WHEN** an enemy enters the SEARCH state
- **THEN** the NavMesh Agent speed SHALL be set to the enemy's configured alert speed (same as INVESTIGATE)

#### Scenario: Speed values differ per enemy type

- **WHEN** a Grunt and a Guard have different configured speed values
- **THEN** each enemy SHALL use its own configured speeds when transitioning between states

### Requirement: Host-Only Navigation Execution

NavMesh Agent movement and destination updates SHALL execute only on the host. Enemy positions SHALL be synced to clients via `NetworkTransform`. Clients SHALL NOT run NavMesh Agent logic.

#### Scenario: NavMesh Agent active only on host

- **WHEN** an enemy is instantiated on a client
- **THEN** the NavMesh Agent component SHALL be disabled on the client; only the host SHALL have an active NavMesh Agent for that enemy

#### Scenario: Position synced via NetworkTransform

- **WHEN** an enemy moves on the host via its NavMesh Agent
- **THEN** the enemy's position and rotation SHALL be synced to all clients via `NetworkTransform`

#### Scenario: Client sees smooth enemy movement

- **WHEN** a client receives position updates for an enemy via `NetworkTransform`
- **THEN** the enemy's movement SHALL appear smooth on the client through NetworkTransform's built-in interpolation
