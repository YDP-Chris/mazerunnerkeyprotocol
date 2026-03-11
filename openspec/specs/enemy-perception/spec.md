## ADDED Requirements

### Requirement: Cone-Based Line-of-Sight Detection

The `EnemyPerception` component SHALL implement cone-based line-of-sight detection. Sight SHALL be defined by a configurable range (max distance) and angle (field of view). A player is visible only when they are within the cone AND a raycast from the enemy's eye position to the player is not blocked by maze walls.

#### Scenario: Player within sight cone and unobstructed

- **WHEN** a player is within the enemy's configured sight range AND within the configured sight angle (measured from the enemy's forward direction) AND a raycast from the enemy to the player does not hit a wall collider
- **THEN** the `EnemyPerception` component SHALL report that player as visible

#### Scenario: Player within range but outside sight angle

- **WHEN** a player is within the enemy's sight range but the angle between the enemy's forward direction and the direction to the player exceeds the configured sight angle
- **THEN** the `EnemyPerception` component SHALL NOT report that player as visible

#### Scenario: Player within cone but blocked by wall

- **WHEN** a player is within the enemy's sight range and angle, but a raycast from the enemy to the player hits a maze wall collider
- **THEN** the `EnemyPerception` component SHALL NOT report that player as visible

#### Scenario: Player beyond sight range

- **WHEN** a player is farther than the enemy's configured sight range
- **THEN** the `EnemyPerception` component SHALL NOT report that player as visible, regardless of angle or obstruction

#### Scenario: Sight parameters differ per enemy type

- **WHEN** a Grunt enemy and a Guard enemy are both active
- **THEN** each SHALL use its own configured sight range and sight angle values, which MAY differ between types

### Requirement: Radius-Based Sound Detection

The `EnemyPerception` component SHALL implement radius-based sound detection. Sound events (gunshots, footsteps) SHALL have a defined origin position and detection radius. An enemy detects a sound when it is within the sound's radius AND a raycast from the enemy to the sound origin is not blocked by maze walls.

#### Scenario: Gunshot within detection range and unobstructed

- **WHEN** a player fires a weapon, generating a sound event with a radius of 15-20 tiles
- **THEN** all enemies within that radius whose raycast to the sound origin is not blocked by a wall SHALL detect the sound event and receive the sound origin position

#### Scenario: Footstep within detection range and unobstructed

- **WHEN** a player moves, generating a footstep sound event with a radius of 3-5 tiles
- **THEN** all enemies within that radius whose raycast to the sound origin is not blocked by a wall SHALL detect the sound event and receive the sound origin position

#### Scenario: Sound blocked by maze wall

- **WHEN** a sound event occurs and an enemy is within the sound's radius, but a raycast from the enemy to the sound origin hits a maze wall collider
- **THEN** the enemy SHALL NOT detect that sound event

#### Scenario: Sound outside detection radius

- **WHEN** a sound event occurs and an enemy is farther than the sound's configured radius
- **THEN** the enemy SHALL NOT detect that sound event, regardless of wall obstruction

#### Scenario: Multiple enemies hear the same sound

- **WHEN** a gunshot sound event occurs and three enemies are within range with unobstructed paths
- **THEN** all three enemies SHALL independently detect the sound event and each SHALL receive the sound origin position for their own state machine to process

### Requirement: Sound Event Broadcast System

Sound events SHALL be broadcast from the source (player actions) rather than polled by enemies. The system SHALL emit sound events with a position, radius, and type (gunshot or footstep). Enemies SHALL subscribe to these events and evaluate detection based on distance and wall occlusion.

#### Scenario: Weapon fire emits sound event

- **WHEN** a player fires any weapon on the host
- **THEN** a sound event SHALL be emitted with the player's position, a gunshot detection radius (15-20 tiles configurable), and type "gunshot"

#### Scenario: Player movement emits footstep sound event

- **WHEN** a player moves (velocity above a configurable threshold) on the host
- **THEN** a footstep sound event SHALL be emitted at a configurable interval with the player's position, a footstep detection radius (3-5 tiles configurable), and type "footstep"

#### Scenario: Sound event is processed only on host

- **WHEN** a sound event is broadcast
- **THEN** only the host SHALL process the event and evaluate enemy detection; clients SHALL NOT run sound detection logic

### Requirement: Wall Occlusion for Both Senses

Maze walls SHALL block both sight and sound. Occlusion SHALL be determined by raycasting against colliders on the wall layer. The system SHALL use Unity layer masks to limit raycasts to wall geometry only, avoiding unnecessary collision checks.

#### Scenario: Wall between enemy and player blocks sight

- **WHEN** an enemy performs a sight check and a maze wall collider intersects the ray from the enemy's eye position to the player
- **THEN** the sight check SHALL return false (player not visible)

#### Scenario: Wall between enemy and sound origin blocks sound

- **WHEN** an enemy evaluates a sound event and a maze wall collider intersects the ray from the enemy to the sound origin
- **THEN** the sound detection SHALL return false (sound not heard)

#### Scenario: Raycasts use wall layer mask

- **WHEN** any perception raycast is performed (sight or sound occlusion)
- **THEN** the raycast SHALL use a layer mask that includes only the wall/obstacle layer, ignoring players, enemies, loot boxes, and other non-wall colliders

### Requirement: Last-Known Position Memory

The `EnemyPerception` component SHALL store the last-known position of a detected player. This position SHALL be retained for 5 seconds after the player was last seen or heard. The SEARCH state uses this stored position to navigate to where the player was last detected.

#### Scenario: Position stored on sight detection

- **WHEN** an enemy has line-of-sight to a player
- **THEN** the `EnemyPerception` component SHALL continuously update the stored last-known position to the player's current position

#### Scenario: Position stored on sound detection

- **WHEN** an enemy detects a sound event
- **THEN** the `EnemyPerception` component SHALL update the stored last-known position to the sound origin if no player is currently visible (sight takes priority)

#### Scenario: Memory persists for 5 seconds

- **WHEN** an enemy loses sight of a player and no new sound events are detected
- **THEN** the stored last-known position SHALL remain valid for 5 seconds, after which it SHALL be cleared

#### Scenario: Memory refreshed by new detection

- **WHEN** an enemy has a stored last-known position and detects the same or another player (by sight or sound)
- **THEN** the stored position SHALL be updated to the new detection position and the 5-second timer SHALL reset

#### Scenario: SEARCH state reads stored position

- **WHEN** an enemy transitions to the SEARCH state
- **THEN** it SHALL read the last-known position from the `EnemyPerception` component and navigate to that position

### Requirement: Perception Update Throttling

Perception checks SHALL be throttled based on the current state to balance accuracy against performance. Enemies in high-priority states (CHASE, ATTACK) SHALL check more frequently than enemies in low-priority states (PATROL).

#### Scenario: PATROL state perception rate

- **WHEN** an enemy is in the PATROL state
- **THEN** sight checks SHALL run at a reduced rate (every 0.3-0.5 seconds, configurable) rather than every frame

#### Scenario: CHASE and ATTACK state perception rate

- **WHEN** an enemy is in the CHASE or ATTACK state
- **THEN** sight checks SHALL run every frame to maintain accurate tracking of the target player

#### Scenario: Sound events are always processed immediately

- **WHEN** a sound event is broadcast, regardless of the enemy's current state
- **THEN** the enemy SHALL evaluate the sound event immediately (no throttling on sound event reception)
