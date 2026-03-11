## ADDED Requirements

### Requirement: EnemyStateMachine starts in PATROL state
After network spawn, an enemy with a Patrol-type config SHALL begin in the PATROL state and navigate toward its first waypoint.

#### Scenario: Initial state is PATROL on spawn
- **WHEN** an enemy NetworkObject is spawned on the host with patrol waypoints assigned
- **THEN** `CurrentState.Value` SHALL equal `EnemyState.PATROL`

### Requirement: PATROL transitions to INVESTIGATE on sound event
An enemy in PATROL state SHALL transition to INVESTIGATE when a sound event is broadcast within its hearing radius and not blocked by a wall.

#### Scenario: Gunshot within radius triggers INVESTIGATE
- **WHEN** the enemy is in PATROL state
- **WHEN** `SoundEventSystem.BroadcastSound` is called with an origin within the enemy's hearing radius and no wall between
- **THEN** `CurrentState.Value` SHALL transition to `EnemyState.INVESTIGATE`
- **THEN** the enemy's NavMeshAgent destination SHALL be near the sound origin

### Requirement: PATROL transitions to CHASE on visible player
An enemy in PATROL state SHALL transition to CHASE when a player is within sight range, within the sight cone angle, and not occluded by a wall.

#### Scenario: Player within cone and range triggers CHASE
- **WHEN** the enemy is in PATROL state
- **WHEN** a network-spawned player object is positioned within the enemy's sight range and cone angle with no wall between
- **WHEN** at least one perception update occurs
- **THEN** `CurrentState.Value` SHALL transition to `EnemyState.CHASE`

### Requirement: INVESTIGATE transitions to CHASE on visible player
An enemy in INVESTIGATE state SHALL transition to CHASE if it gains line-of-sight to a player during investigation.

#### Scenario: Player becomes visible during investigation
- **WHEN** the enemy is in INVESTIGATE state moving toward a sound origin
- **WHEN** a player is positioned within the enemy's sight cone and range
- **WHEN** at least one perception update occurs
- **THEN** `CurrentState.Value` SHALL transition to `EnemyState.CHASE`

### Requirement: INVESTIGATE transitions to PATROL on arrival with no target
An enemy in INVESTIGATE state SHALL transition back to PATROL when it reaches the investigation target and finds no player.

#### Scenario: Arrive at investigation target with no player
- **WHEN** the enemy is in INVESTIGATE state
- **WHEN** the enemy's NavMeshAgent reaches the investigation target (remainingDistance <= waypointArrivalThreshold)
- **WHEN** no player is visible
- **THEN** `CurrentState.Value` SHALL transition to `EnemyState.PATROL`

### Requirement: CHASE transitions to ATTACK when within attack range
An enemy in CHASE state SHALL transition to ATTACK when the distance to the chase target is within the config's attackRange and the target is visible.

#### Scenario: Player enters attack range during chase
- **WHEN** the enemy is in CHASE state pursuing a player
- **WHEN** the player is within `config.attackRange` distance and visible
- **THEN** `CurrentState.Value` SHALL transition to `EnemyState.ATTACK`

### Requirement: CHASE transitions to SEARCH after 5 seconds without line-of-sight
An enemy in CHASE state SHALL transition to SEARCH if it loses line-of-sight to the target for 5 continuous seconds.

#### Scenario: LOS lost for 5 seconds triggers SEARCH
- **WHEN** the enemy is in CHASE state
- **WHEN** the player moves behind a wall or out of sight range
- **WHEN** 5 seconds elapse without the enemy regaining line-of-sight
- **THEN** `CurrentState.Value` SHALL transition to `EnemyState.SEARCH`

### Requirement: ATTACK deals damage on cooldown interval
An enemy in ATTACK state SHALL deal damage to the target PlayerHealth at intervals defined by `config.attackInterval`.

#### Scenario: Enemy deals damage after attack cooldown
- **WHEN** the enemy is in ATTACK state with a visible target in range
- **WHEN** `config.attackInterval` seconds elapse
- **THEN** the target's `PlayerHealth.CurrentHealth.Value` SHALL decrease by `config.attackDamage`

### Requirement: ATTACK transitions to CHASE when target leaves range
An enemy in ATTACK state SHALL transition back to CHASE when the target moves outside attackRange or line-of-sight is lost.

#### Scenario: Target moves out of attack range
- **WHEN** the enemy is in ATTACK state
- **WHEN** the target player moves beyond `config.attackRange`
- **THEN** `CurrentState.Value` SHALL transition to `EnemyState.CHASE`

### Requirement: ATTACK transitions to PATROL when target is eliminated
An enemy in ATTACK state SHALL transition to PATROL when the target's PlayerHealth.IsEliminated becomes true.

#### Scenario: Target eliminated during attack
- **WHEN** the enemy is in ATTACK state attacking a player
- **WHEN** the player's health reaches 0 and `IsEliminated` becomes true
- **THEN** `CurrentState.Value` SHALL transition to `EnemyState.PATROL`
- **THEN** perception memory SHALL be cleared

### Requirement: SEARCH transitions to CHASE on player found
An enemy in SEARCH state SHALL transition to CHASE if it spots a player during the search.

#### Scenario: Player found during search
- **WHEN** the enemy is in SEARCH state
- **WHEN** a player becomes visible within the enemy's sight cone
- **THEN** `CurrentState.Value` SHALL transition to `EnemyState.CHASE`

### Requirement: SEARCH transitions to PATROL on timeout
An enemy in SEARCH state SHALL transition to PATROL when `config.searchDuration` seconds elapse without finding a player.

#### Scenario: Search timer expires without finding player
- **WHEN** the enemy is in SEARCH state
- **WHEN** `config.searchDuration` seconds elapse with no player visible
- **THEN** `CurrentState.Value` SHALL transition to `EnemyState.PATROL`
- **THEN** perception memory SHALL be cleared

### Requirement: EnemyPerception cone detection respects angle and range
EnemyPerception SHALL only detect players that are within both `config.sightRange` distance and `config.sightAngle / 2` degrees of the enemy's forward direction.

#### Scenario: Player inside cone and range is detected
- **WHEN** a player is positioned 5 units ahead of the enemy, within the sight cone
- **WHEN** `UpdatePerception(0)` is called
- **THEN** `GetVisiblePlayer()` SHALL return the player's transform

#### Scenario: Player outside cone angle is not detected
- **WHEN** a player is positioned 5 units to the side of the enemy, outside the sight cone angle but within range
- **WHEN** `UpdatePerception(0)` is called
- **THEN** `GetVisiblePlayer()` SHALL return null

#### Scenario: Player outside range is not detected
- **WHEN** a player is positioned directly ahead but beyond `config.sightRange`
- **WHEN** `UpdatePerception(0)` is called
- **THEN** `GetVisiblePlayer()` SHALL return null

### Requirement: EnemyPerception wall occlusion blocks detection
EnemyPerception SHALL NOT detect a player when a wall (matching wallLayerMask) exists on the raycast path between the enemy's eye position and the player.

#### Scenario: Wall between enemy and player blocks sight
- **WHEN** a player is positioned within cone and range
- **WHEN** a wall collider exists on the line between the enemy's eye offset position and the player
- **THEN** `GetVisiblePlayer()` SHALL return null after `UpdatePerception(0)`

### Requirement: SoundEventSystem broadcasts to subscribed listeners
When `SoundEventSystem.BroadcastSound` is called, all subscribed listeners SHALL receive the event with the correct origin, radius, and sound type.

#### Scenario: Broadcast delivers event to subscriber
- **WHEN** a listener subscribes to `SoundEventSystem.OnSoundBroadcast`
- **WHEN** `BroadcastSound(position, 20f, SoundType.Gunshot)` is called
- **THEN** the listener SHALL receive the event with the matching position, radius, and type

#### Scenario: Listener outside radius ignores sound in EnemyPerception
- **WHEN** an EnemyPerception component is subscribed
- **WHEN** a sound is broadcast with origin 30 units away and radius 15
- **THEN** the enemy's `GetLastKnownPosition` SHALL return false (no memory stored)

### Requirement: Sound blocked by wall does not update enemy memory
EnemyPerception SHALL ignore sound events when a wall blocks the path between the enemy and the sound origin.

#### Scenario: Wall blocks sound event
- **WHEN** a sound is broadcast within the enemy's hearing radius
- **WHEN** a wall exists between the enemy and the sound origin
- **THEN** the enemy's `GetLastKnownPosition` SHALL return false
