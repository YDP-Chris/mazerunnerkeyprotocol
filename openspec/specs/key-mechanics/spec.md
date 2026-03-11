## ADDED Requirements

### Requirement: Key Spawning

The system SHALL spawn exactly one key per match at a random position within the maze after maze generation is complete. The key spawn position MUST NOT be within a configurable minimum distance of any player spawn point or the exit. The key MUST be placed on a valid, navigable floor tile. The key spawn position MUST be determined by the host and communicated to all clients.

#### Scenario: Key spawns at valid position after maze generation

- **WHEN** the maze generation pipeline completes and player spawn points and exit have been placed
- **THEN** the host SHALL select a random navigable floor tile that is at least the configured minimum distance from every player spawn point and the exit, and spawn the key GameObject at that position

#### Scenario: Key spawn position avoids player spawns

- **WHEN** the key placement algorithm evaluates candidate positions
- **THEN** any position within the minimum distance threshold of any player spawn point SHALL be rejected

#### Scenario: Key spawn position avoids exit

- **WHEN** the key placement algorithm evaluates candidate positions
- **THEN** any position within the minimum distance threshold of the exit SHALL be rejected

#### Scenario: Key spawn uses max-attempts guard

- **WHEN** the key placement algorithm has attempted 50 candidate positions without finding a valid location
- **THEN** the system SHALL place the key at a known-safe fallback position (the navigable tile farthest from all spawn points and the exit) and log a warning

#### Scenario: Key spawn is synchronized to all clients

- **WHEN** the host determines the key spawn position
- **THEN** the position SHALL be replicated to all clients via a NetworkVariable so that every client instantiates the key at the same world position

---

### Requirement: Key Proximity Feedback

The key SHALL provide visual feedback to players who are within a configurable detection radius. The key MUST NOT appear on any player's minimap until the player is within a separate, smaller minimap-reveal radius.

#### Scenario: Key glows when a player is within detection radius

- **WHEN** any player enters the key's detection radius (default: 8 Unity units)
- **THEN** the key SHALL begin a pulsing glow effect visible to that player, with pulse intensity increasing as the player gets closer

#### Scenario: Key is invisible on minimap beyond reveal radius

- **WHEN** no player is within the key's minimap-reveal radius (default: 5 Unity units)
- **THEN** the key SHALL NOT appear on any player's minimap

#### Scenario: Key appears on minimap within reveal radius

- **WHEN** a player enters the key's minimap-reveal radius
- **THEN** the key's position SHALL appear as an icon on that player's minimap

#### Scenario: Key glow is client-side only

- **WHEN** the key proximity feedback is rendered
- **THEN** the glow effect SHALL be computed locally on each client based on the local player's distance to the key, and SHALL NOT require network synchronization

---

### Requirement: Key Pickup

The key SHALL be automatically picked up when a player's character collider overlaps the key's trigger collider. Pickup MUST be validated and authorized by the host. Only one player can hold the key at any time.

#### Scenario: Player contacts the key and picks it up

- **WHEN** a player's character collider enters the key's trigger collider and no other player currently holds the key
- **THEN** the host SHALL assign key ownership to that player, the key GameObject SHALL be hidden from the world, and all clients SHALL be notified of the new key holder via a NetworkVariable update

#### Scenario: Two players contact the key simultaneously

- **WHEN** two or more players' colliders overlap the key's trigger collider on the same physics tick
- **THEN** the host SHALL assign the key to the player whose collision was processed first by the physics engine, and all other players SHALL NOT receive the key

#### Scenario: Player contacts the key while another player already holds it

- **WHEN** a player's collider enters the key's trigger collider but another player is already the key holder
- **THEN** the system SHALL ignore the collision and the key SHALL remain with the current holder

#### Scenario: Key pickup triggers global notification

- **WHEN** a player picks up the key
- **THEN** the host SHALL broadcast an RPC to all clients indicating which player now holds the key, triggering the key-holder tracking indicator and exit unlock sequence

#### Scenario: Key pickup is host-authoritative

- **WHEN** a client detects a local collision between its player and the key
- **THEN** the client SHALL send a pickup request to the host, and the host SHALL validate that the key is still available before confirming the pickup

---

### Requirement: Key Drop on Death

When the key holder is eliminated, the key SHALL drop at the holder's last known position and become available for pickup by any surviving player.

#### Scenario: Key holder is eliminated

- **WHEN** the key holder's health reaches zero
- **THEN** the host SHALL remove key ownership from the eliminated player, spawn the key GameObject at the eliminated player's last world position, and notify all clients via NetworkVariable update and RPC

#### Scenario: Dropped key is immediately available for pickup

- **WHEN** the key is dropped due to key holder elimination
- **THEN** the key's trigger collider SHALL be re-enabled immediately, and any surviving player who contacts it SHALL be able to pick it up under the standard pickup rules

#### Scenario: Dropped key retains proximity feedback

- **WHEN** the key is dropped and placed back into the world
- **THEN** the key SHALL resume its proximity glow and minimap-reveal behavior as defined in the Key Proximity Feedback requirement

#### Scenario: Key drop position is navigable

- **WHEN** the key holder is eliminated and the key is dropped
- **THEN** the key SHALL be placed at the holder's last position, snapped to the nearest navigable floor tile if the death position is not directly on a walkable surface

---

### Requirement: Key Visual State

The key SHALL have distinct visual states that communicate its current status to players.

#### Scenario: Key is uncollected and no player is nearby

- **WHEN** the key is spawned and no player is within the detection radius
- **THEN** the key SHALL render with a subtle idle animation (slow rotation or shimmer) to distinguish it from other world objects

#### Scenario: Key is picked up

- **WHEN** a player picks up the key
- **THEN** the key world GameObject SHALL be hidden, and the key holder's player model SHALL display a visible key attachment (e.g., on belt or back) so other players can visually identify the holder in 3rd-person view

#### Scenario: Key is dropped after holder death

- **WHEN** the key is dropped back into the world
- **THEN** the key SHALL reappear at the drop position with its idle animation and proximity feedback behavior fully restored
