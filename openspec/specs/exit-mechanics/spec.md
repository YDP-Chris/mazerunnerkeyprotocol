## ADDED Requirements

### Requirement: Exit Placement

The system SHALL place exactly one exit per maze on the maze edge during the maze generation pipeline. The exit position MUST be determined by the host and synchronized to all clients via the shared maze seed.

#### Scenario: Exit is placed on the maze edge

- **WHEN** the maze generation pipeline reaches the exit placement step
- **THEN** the system SHALL select a position on the outer edge of the maze grid and place the exit gateway GameObject at that position

#### Scenario: Exit is not placed near player spawns

- **WHEN** the exit placement algorithm evaluates candidate edge positions
- **THEN** positions within the minimum distance threshold of any player spawn point SHALL be deprioritized, favoring edge positions that maximize distance from the nearest spawn

#### Scenario: Exit placement is deterministic from seed

- **WHEN** the host generates the maze with a given seed and distributes that seed to clients
- **THEN** every client running the same generation pipeline with the same seed SHALL produce the exit at the identical position

---

### Requirement: Exit Lock State

The exit SHALL be locked and non-interactable from match start until the key is picked up. Once the key is picked up, the exit SHALL unlock and become interactable for the key holder.

#### Scenario: Exit is locked at match start

- **WHEN** the match begins and no player has picked up the key
- **THEN** the exit gateway SHALL be in a locked state, its door/barrier visually closed, and no player SHALL be able to trigger the escape sequence by entering the exit zone

#### Scenario: Exit unlocks when the key is picked up

- **WHEN** any player picks up the key
- **THEN** the host SHALL set the exit's lock state NetworkVariable to unlocked, the exit SHALL begin its unlocked visual effects (glow and pulse), and all clients SHALL update the exit's appearance

#### Scenario: Exit remains unlocked after key drop

- **WHEN** the key holder is eliminated and the key drops
- **THEN** the exit SHALL remain in the unlocked state; it does NOT re-lock when the key changes hands or is dropped

#### Scenario: Only the key holder can use the exit

- **WHEN** a player without the key enters the exit zone while the exit is unlocked
- **THEN** the escape sequence SHALL NOT begin, and the system SHALL provide no feedback to that player (the exit is usable only by the key holder)

---

### Requirement: Exit Visibility

The exit's location SHALL be hidden from all players until the key is picked up. Once the key is picked up, the exit SHALL become visible on all players' minimaps and display a visual beacon.

#### Scenario: Exit is not visible on minimap before key pickup

- **WHEN** the match is in progress and no player holds the key
- **THEN** the exit's position SHALL NOT appear on any player's minimap, and the exit SHALL not display any long-range visual beacon

#### Scenario: Exit appears on minimap after key pickup

- **WHEN** a player picks up the key
- **THEN** the exit's position SHALL immediately appear as an icon on every player's minimap

#### Scenario: Exit displays visual beacon after key pickup

- **WHEN** the exit transitions from locked to unlocked
- **THEN** the exit SHALL emit a visible glow and pulsing light effect that is visible from a distance within the maze, helping players navigate toward it

#### Scenario: Exit remains visible after key drop

- **WHEN** the key is dropped due to key holder elimination
- **THEN** the exit SHALL remain visible on all minimaps and continue displaying its visual beacon

---

### Requirement: Escape Animation

When the key holder enters the exit zone, a channeled escape animation SHALL begin. The animation MUST last a configurable duration (default: 2.5 seconds). The animation is interruptible by damage.

#### Scenario: Key holder enters the exit zone

- **WHEN** a player holding the key enters the exit zone trigger collider
- **THEN** the system SHALL begin the escape animation, the player's movement SHALL be locked (no movement input accepted), and a visible progress indicator SHALL display the remaining escape time to all players

#### Scenario: Escape animation completes without interruption

- **WHEN** the escape animation timer reaches zero and the key holder has not taken any damage during the animation
- **THEN** the host SHALL declare the key holder as the match winner, broadcast the win event to all clients, and trigger the match-end sequence

#### Scenario: Escape animation is interrupted by damage

- **WHEN** the key holder takes any damage from any source (player or enemy) during the escape animation
- **THEN** the escape animation SHALL immediately cancel, the progress timer SHALL reset to zero, the player's movement SHALL be unlocked, and the player SHALL remain in the exit zone able to re-trigger the escape

#### Scenario: Key holder re-enters exit zone after interruption

- **WHEN** the key holder's escape animation was previously interrupted and the key holder is still alive and still in (or re-enters) the exit zone
- **THEN** the escape animation SHALL restart from zero, requiring the full duration to complete again

#### Scenario: Key holder leaves exit zone during animation

- **WHEN** the key holder voluntarily leaves the exit zone trigger collider during the escape animation (e.g., pushed by an explosion or leaving intentionally)
- **THEN** the escape animation SHALL immediately cancel and the progress timer SHALL reset to zero

#### Scenario: Escape animation is visible to all players

- **WHEN** the escape animation begins
- **THEN** all clients SHALL display visual feedback at the exit location indicating that an escape attempt is in progress, creating urgency for pursuing players

#### Scenario: Escape progress is host-authoritative

- **WHEN** the escape animation is running
- **THEN** the host SHALL own the escape progress timer, and the escape completion event SHALL only be triggered by the host to prevent client-side manipulation
