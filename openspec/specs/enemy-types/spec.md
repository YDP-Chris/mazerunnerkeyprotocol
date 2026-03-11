## ADDED Requirements

### Requirement: Shared Enemy Base Configuration

All enemy types SHALL share a common base built on the same `EnemyStateMachine`, `EnemyPerception`, and NavMesh Agent components. Behavioral differences between enemy types SHALL be driven by configuration data (serialized fields or ScriptableObject), not by subclassing the state machine. Every enemy script SHALL extend `NetworkBehaviour`.

#### Scenario: All enemies use the same state machine component

- **WHEN** a Grunt and a Guard are both active in the maze
- **THEN** both SHALL use the same `EnemyStateMachine` component class with different configuration values driving their behavior

#### Scenario: Enemy configuration is data-driven

- **WHEN** a new enemy type needs to be added (e.g., the future Hunter)
- **THEN** it SHALL be possible to create it by defining a new configuration (health, speed, damage, perception parameters, patrol behavior) without modifying the state machine code

#### Scenario: All enemy scripts use NetworkBehaviour

- **WHEN** any enemy component is attached to an enemy prefab
- **THEN** that component SHALL extend `NetworkBehaviour`, not `MonoBehaviour`

### Requirement: Grunt Enemy Type

The Grunt SHALL be a basic patrolling enemy with low health, medium damage, and standard movement speed. It is an MVP enemy type. The Grunt follows an assigned patrol route and engages players that it detects through sight or sound.

#### Scenario: Grunt patrol behavior

- **WHEN** a Grunt is spawned and no players are detected
- **THEN** it SHALL enter the PATROL state and follow its assigned waypoint route at its configured patrol speed

#### Scenario: Grunt engages detected player

- **WHEN** a Grunt in PATROL or INVESTIGATE state detects a player via sight
- **THEN** it SHALL transition to CHASE and pursue the player at its configured chase speed

#### Scenario: Grunt health configuration

- **WHEN** a Grunt is spawned
- **THEN** it SHALL have low health (configurable, default value lower than Guard health) tracked as a `NetworkVariable`

#### Scenario: Grunt damage output

- **WHEN** a Grunt is in ATTACK state and attacks a player
- **THEN** it SHALL deal medium damage per attack at its configured attack interval

#### Scenario: Grunt sight parameters

- **WHEN** a Grunt performs sight detection
- **THEN** it SHALL use its configured sight range and sight angle, which represent standard detection capabilities (not enhanced or reduced)

#### Scenario: Grunt elimination

- **WHEN** a Grunt's health reaches zero
- **THEN** the Grunt SHALL be eliminated, its game object deactivated or destroyed, and it SHALL NOT respawn during the current match

### Requirement: Guard Enemy Type

The Guard SHALL be a stationary enemy positioned near the key spawn location with higher health than the Grunt. The Guard does not patrol; it remains in place until alerted by a player through sight or sound, at which point it engages using the standard state machine transitions.

#### Scenario: Guard initial position near key spawn

- **WHEN** a Guard is spawned during maze setup
- **THEN** it SHALL be placed at or near the key spawn location, within a configurable radius of the key's position

#### Scenario: Guard stationary behavior before alert

- **WHEN** a Guard has not detected any player (no sight or sound detection)
- **THEN** it SHALL remain stationary at its spawn position rather than following a patrol route
- **THEN** the Guard's PATROL state SHALL behave as idle/stationary (no waypoint movement)

#### Scenario: Guard alerted by player sight

- **WHEN** a Guard detects a player via line-of-sight
- **THEN** it SHALL transition from its stationary idle state to the CHASE state and pursue the detected player

#### Scenario: Guard alerted by sound

- **WHEN** a Guard detects a sound event (gunshot or footstep within range, not blocked by walls)
- **THEN** it SHALL transition to the INVESTIGATE state and move toward the sound origin

#### Scenario: Guard health configuration

- **WHEN** a Guard is spawned
- **THEN** it SHALL have higher health than a Grunt (configurable, default value meaningfully greater than Grunt health) tracked as a `NetworkVariable`

#### Scenario: Guard returns to key spawn area after losing target

- **WHEN** a Guard's SEARCH timer expires without finding a player
- **THEN** the Guard SHALL return to its original spawn position near the key spawn and resume its stationary idle behavior

#### Scenario: Guard elimination

- **WHEN** a Guard's health reaches zero
- **THEN** the Guard SHALL be eliminated, its game object deactivated or destroyed, and it SHALL NOT respawn during the current match

### Requirement: Enemy Health as NetworkVariable

All enemy types SHALL track their health using a `NetworkVariable<float>` (or `NetworkVariable<int>`). Health SHALL only be modified on the host. Clients SHALL read the health value for UI display (health bars) and death effects.

#### Scenario: Health modified only on host

- **WHEN** an enemy takes damage
- **THEN** the health `NetworkVariable` SHALL only be decremented on the host (server)

#### Scenario: Client reads health for display

- **WHEN** a client needs to render an enemy health bar
- **THEN** it SHALL read the health value from the synced `NetworkVariable`

#### Scenario: Enemy eliminated at zero health

- **WHEN** an enemy's health `NetworkVariable` reaches zero or below on the host
- **THEN** the host SHALL trigger the elimination sequence and notify all clients via RPC or NetworkVariable state change

### Requirement: Enemy Spawn Placement

Enemies SHALL be placed during the maze generation pipeline (step 6: place enemy patrol waypoints). Grunts SHALL be distributed throughout the maze. Guards SHALL be placed specifically near the key spawn location. Spawn positions MUST be valid NavMesh positions.

#### Scenario: Grunt spawn distribution

- **WHEN** the maze generation pipeline places Grunt enemies
- **THEN** Grunts SHALL be distributed across the maze at valid NavMesh positions, with spacing to avoid clustering

#### Scenario: Guard spawn near key

- **WHEN** the maze generation pipeline places Guard enemies
- **THEN** Guards SHALL be placed within a configurable radius of the key spawn position at valid NavMesh positions

#### Scenario: Invalid spawn position fallback

- **WHEN** a calculated enemy spawn position is not on the NavMesh
- **THEN** the system SHALL sample the nearest valid NavMesh position within a configurable search radius, and if no valid position is found, the enemy SHALL not be spawned (no infinite retry loop)

#### Scenario: Enemy count is fixed at match start

- **WHEN** a match begins and enemies are spawned
- **THEN** the total number of enemies SHALL remain fixed for the duration of the match; no additional enemies SHALL spawn after the match starts
