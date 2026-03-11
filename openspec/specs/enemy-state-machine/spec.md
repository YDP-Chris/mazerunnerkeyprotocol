## ADDED Requirements

### Requirement: Five-State Machine Architecture

The enemy AI system SHALL implement exactly five discrete states: PATROL, INVESTIGATE, CHASE, ATTACK, and SEARCH. The system MUST NOT use simple patrol/chase conditionals. Each state SHALL have clearly defined entry conditions, exit conditions, and per-frame behavior. The state machine SHALL be implemented as a component (`EnemyStateMachine`) that extends `NetworkBehaviour`.

#### Scenario: Enemy initializes into PATROL state

- **WHEN** an enemy is spawned into the maze
- **THEN** the enemy SHALL begin in the PATROL state and follow its assigned patrol path at slow speed

#### Scenario: Only one state is active at a time

- **WHEN** the enemy is in any state
- **THEN** exactly one state SHALL be active, and the enemy MUST NOT execute behavior from multiple states simultaneously

#### Scenario: State transitions are deterministic

- **WHEN** a state transition condition is met
- **THEN** the enemy SHALL transition to the specified target state on the same frame or the next frame, with no intermediate undefined state

### Requirement: PATROL State Behavior

The PATROL state SHALL cause the enemy to follow its assigned patrol path at slow movement speed. The enemy SHALL cycle through patrol waypoints in order. The enemy SHALL exit the PATROL state when it detects a player through sight or sound.

#### Scenario: Grunt follows patrol waypoints

- **WHEN** a Grunt enemy is in the PATROL state
- **THEN** it SHALL move toward its current patrol waypoint using the NavMesh Agent at its configured patrol speed
- **THEN** upon reaching a waypoint (within a configurable arrival threshold), it SHALL advance to the next waypoint in its route

#### Scenario: Patrol path loops

- **WHEN** an enemy reaches the last waypoint in its patrol route
- **THEN** it SHALL return to the first waypoint and continue the cycle

#### Scenario: Player detected by sight during PATROL

- **WHEN** an enemy in PATROL state gains line-of-sight to a player within its sight range and angle
- **THEN** the enemy SHALL transition to the CHASE state with that player as its target

#### Scenario: Sound detected during PATROL

- **WHEN** an enemy in PATROL state detects a sound event (gunshot or footstep) that is within range and not blocked by walls
- **THEN** the enemy SHALL transition to the INVESTIGATE state with the sound origin as its investigation target

### Requirement: INVESTIGATE State Behavior

The INVESTIGATE state SHALL cause the enemy to move toward the last-known sound position at an alert movement speed (between patrol and chase speed). The enemy SHALL transition to CHASE if it finds a player, or return to PATROL if it reaches the investigation point and finds nothing.

#### Scenario: Enemy moves to sound origin

- **WHEN** an enemy enters the INVESTIGATE state with a sound origin position
- **THEN** it SHALL navigate to that position using the NavMesh Agent at its configured alert speed

#### Scenario: Player found during investigation

- **WHEN** an enemy in INVESTIGATE state gains line-of-sight to a player while moving toward the sound origin
- **THEN** the enemy SHALL immediately transition to the CHASE state with that player as its target

#### Scenario: Nothing found at sound origin

- **WHEN** an enemy in INVESTIGATE state reaches the sound origin position (within arrival threshold) and has no line-of-sight to any player
- **THEN** the enemy SHALL transition back to the PATROL state and resume its patrol route

#### Scenario: New sound heard during investigation

- **WHEN** an enemy in INVESTIGATE state detects a new sound event closer than its current investigation target
- **THEN** the enemy SHALL update its investigation target to the new sound origin position

### Requirement: CHASE State Behavior

The CHASE state SHALL cause the enemy to pursue its target player via NavMesh pathfinding at a faster movement speed than PATROL. The enemy SHALL transition to ATTACK when within attack range, or to SEARCH if it loses sight of the player for 5 seconds.

#### Scenario: Enemy pursues player

- **WHEN** an enemy is in the CHASE state with a valid target player
- **THEN** it SHALL continuously update its NavMesh destination to the target player's current position at its configured chase speed

#### Scenario: Player enters attack range during chase

- **WHEN** an enemy in CHASE state is within its configured attack range of the target player AND has line-of-sight
- **THEN** the enemy SHALL transition to the ATTACK state

#### Scenario: Line-of-sight lost for 5 seconds

- **WHEN** an enemy in CHASE state loses line-of-sight to its target player
- **THEN** the enemy SHALL continue moving toward the player's last-known position
- **THEN** if line-of-sight is not regained within 5 seconds, the enemy SHALL transition to the SEARCH state with the last-known position stored

#### Scenario: Line-of-sight regained before timeout

- **WHEN** an enemy in CHASE state loses line-of-sight but regains it within 5 seconds
- **THEN** the enemy SHALL remain in the CHASE state and resume direct pursuit

### Requirement: ATTACK State Behavior

The ATTACK state SHALL cause the enemy to fire or strike at its target player with reduced movement speed. The enemy SHALL transition to CHASE if the player moves out of attack range, or to PATROL if the target player is eliminated.

#### Scenario: Enemy attacks target player

- **WHEN** an enemy is in the ATTACK state with a valid target player in range and line-of-sight
- **THEN** the enemy SHALL deal damage to the target player at its configured damage rate and attack interval
- **THEN** the enemy SHALL move at a reduced speed (slower than chase speed)

#### Scenario: Player moves out of attack range

- **WHEN** an enemy in ATTACK state loses its target player from attack range (player moves beyond attack range OR line-of-sight is broken)
- **THEN** the enemy SHALL transition to the CHASE state to re-engage

#### Scenario: Target player is eliminated

- **WHEN** an enemy in ATTACK state's target player is eliminated (health reaches zero)
- **THEN** the enemy SHALL transition to the PATROL state and resume its patrol route

#### Scenario: Enemy faces target while attacking

- **WHEN** an enemy is in the ATTACK state
- **THEN** the enemy SHALL continuously rotate to face its target player

### Requirement: SEARCH State Behavior

The SEARCH state SHALL cause the enemy to check the last-known player position and nearby waypoints. The enemy SHALL store the last-known position for 5 seconds. The enemy SHALL transition to CHASE if it finds the player, or to PATROL when the search timer expires.

#### Scenario: Enemy moves to last-known position

- **WHEN** an enemy enters the SEARCH state
- **THEN** it SHALL navigate to the stored last-known player position using the NavMesh Agent

#### Scenario: Enemy checks nearby waypoints after reaching last-known position

- **WHEN** an enemy in SEARCH state reaches the last-known position without finding the player
- **THEN** it SHALL investigate up to 3 nearby patrol waypoints (within a configurable search radius) before giving up

#### Scenario: Player found during search

- **WHEN** an enemy in SEARCH state gains line-of-sight to any player while searching
- **THEN** the enemy SHALL immediately transition to the CHASE state with that player as its target

#### Scenario: Search timer expires

- **WHEN** an enemy in SEARCH state has been searching for longer than its configured search duration (default 10 seconds) without finding a player
- **THEN** the enemy SHALL transition to the PATROL state and resume its patrol route from the nearest waypoint

### Requirement: Host-Authoritative State Execution

The state machine logic SHALL execute only on the host (server). The current state SHALL be synced to all clients via a `NetworkVariable` so clients can play appropriate animations and visual effects.

#### Scenario: State machine runs on host only

- **WHEN** the `EnemyStateMachine` update loop executes
- **THEN** state transition logic and behavior updates SHALL only run if `IsServer` is true

#### Scenario: State synced to clients

- **WHEN** the enemy transitions to a new state on the host
- **THEN** the `NetworkVariable` representing the current state SHALL be updated
- **THEN** all connected clients SHALL receive the updated state value

#### Scenario: Client receives state change

- **WHEN** a client receives a state change via `NetworkVariable` callback
- **THEN** the client SHALL update the enemy's visual representation (animation, alert indicators) to match the new state
