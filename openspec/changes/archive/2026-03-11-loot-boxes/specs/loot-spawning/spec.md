## ADDED Requirements

### Requirement: Loot Box Spawn Placement

The loot box spawn system SHALL place loot boxes on valid open floor tiles within the maze at match start. A valid floor tile is one that is walkable, not occupied by another loot box, not within a minimum distance of any player spawn point, and not within a minimum distance of the key spawn location. The minimum distance from player spawns SHALL be configurable and default to 3 tiles. The minimum distance from the key spawn SHALL be configurable and default to 2 tiles.

#### Scenario: Successful placement on valid tiles
- **WHEN** the maze has been generated and player spawn points, key location, and exit have been placed
- **THEN** the spawner SHALL attempt to place each loot box on a randomly selected valid open floor tile within the maze boundaries

#### Scenario: Loot boxes do not spawn on walls or obstacles
- **WHEN** the spawner selects a candidate position for a loot box
- **THEN** the position MUST be verified as a walkable floor tile before placement is accepted

#### Scenario: Loot boxes do not spawn on top of each other
- **WHEN** the spawner places a loot box at a position
- **THEN** no other loot box SHALL be placed within a minimum separation distance of 2 tiles from that position

#### Scenario: Loot boxes do not spawn near player spawns
- **WHEN** a candidate position is within the configured minimum distance of any player spawn point
- **THEN** the position SHALL be rejected and the spawner SHALL try a different position

#### Scenario: Loot boxes do not spawn near the key
- **WHEN** a candidate position is within the configured minimum distance of the key spawn location
- **THEN** the position SHALL be rejected and the spawner SHALL try a different position

---

### Requirement: Max-Attempts Guard with Safe Fallback

The loot box placement algorithm MUST use a max-attempts guard of 50 attempts per loot box. If a valid position is not found within 50 attempts, the system MUST fall back to a known-safe position. The system MUST NOT use an infinite loop or unbounded retry logic under any circumstances.

#### Scenario: Successful placement within attempt limit
- **WHEN** the spawner finds a valid floor tile within 50 random attempts for a given loot box
- **THEN** the loot box SHALL be placed at that position and the attempt counter SHALL reset for the next loot box

#### Scenario: Fallback after 50 failed attempts
- **WHEN** the spawner fails to find a valid position after exactly 50 attempts for a given loot box
- **THEN** the system SHALL place the loot box at a known-safe fallback position selected from a pre-validated list of fallback positions distributed across the maze

#### Scenario: Fallback positions are distributed
- **WHEN** fallback positions are initialized at maze generation time
- **THEN** the fallback list SHALL contain at least 4 positions, one in each quadrant of the maze, all verified as valid walkable floor tiles

#### Scenario: Fallback positions are consumed without duplication
- **WHEN** a fallback position is used for one loot box
- **THEN** that fallback position SHALL be removed from the available fallback list so no two loot boxes share a fallback position

#### Scenario: No infinite loop under any circumstance
- **WHEN** the placement algorithm executes for any loot box
- **THEN** the total iterations for that single loot box MUST NOT exceed 50, regardless of maze configuration or player count

---

### Requirement: Loot Density Scaling with Player Count

The number of loot boxes spawned per match SHALL scale with the number of players. The formula SHALL be `lootCount = baseLootCount + (playerCount - 2) * additionalLootPerPlayer` where `baseLootCount` defaults to 8 and `additionalLootPerPlayer` defaults to 3. The minimum loot count SHALL be the `baseLootCount`. The maximum loot count SHALL be capped relative to the maze size to prevent over-saturation.

#### Scenario: Two-player match loot count
- **WHEN** a match starts with 2 players and default configuration
- **THEN** the system SHALL spawn exactly 8 loot boxes

#### Scenario: Four-player match loot count
- **WHEN** a match starts with 4 players and default configuration
- **THEN** the system SHALL spawn exactly 14 loot boxes (8 + 2 * 3)

#### Scenario: Eight-player match loot count
- **WHEN** a match starts with 8 players and default configuration
- **THEN** the system SHALL spawn exactly 26 loot boxes (8 + 6 * 3)

#### Scenario: Maximum cap enforcement
- **WHEN** the calculated loot count exceeds the maximum cap for the current maze size
- **THEN** the system SHALL clamp the loot count to the maximum cap, defined as `mazeWidth * mazeHeight / 15` (rounded down)

#### Scenario: Single-player match uses base count
- **WHEN** a match starts with 1 player
- **THEN** the system SHALL spawn the `baseLootCount` (8 boxes by default), not fewer

---

### Requirement: Host-Authoritative Spawn Determination

The host SHALL be the sole authority for determining loot box spawn positions and contents. All clients SHALL receive loot box positions and states via `NetworkVariable` synchronization. The host SHALL use the match seed to generate deterministic loot placements.

#### Scenario: Host generates loot positions
- **WHEN** the match starts and the maze has been generated on the host
- **THEN** the host SHALL run the loot box placement algorithm and spawn loot box network objects at the determined positions

#### Scenario: Clients receive loot positions via network sync
- **WHEN** a client joins a match where loot boxes have already been placed
- **THEN** the client SHALL receive the positions and states of all loot boxes through network object synchronization, not by running the placement algorithm locally

#### Scenario: Deterministic placement from seed
- **WHEN** the same maze seed and player count are used in two separate matches
- **THEN** the loot box positions and contents SHALL be identical, ensuring reproducibility for debugging

---

### Requirement: Loot Box NetworkBehaviour Base Class

All loot box scripts SHALL inherit from `Unity.Netcode.NetworkBehaviour` instead of `MonoBehaviour`. The loot box availability state SHALL be stored as a `NetworkVariable<bool>`.

#### Scenario: Script inheritance
- **WHEN** a loot box script is created or modified
- **THEN** it MUST inherit from `NetworkBehaviour` and MUST NOT inherit directly from `MonoBehaviour`

#### Scenario: State synchronization via NetworkVariable
- **WHEN** a loot box is spawned
- **THEN** its availability state (available or looted) SHALL be stored as a `NetworkVariable<bool>` readable by all clients and writable only by the host
