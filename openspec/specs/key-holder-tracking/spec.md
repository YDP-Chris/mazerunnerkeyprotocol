## ADDED Requirements

### Requirement: Directional Indicator Activation

When the key is picked up, all non-holder players SHALL receive a HUD-based directional indicator pointing toward the key holder. The indicator SHALL remain active as long as another player holds the key.

#### Scenario: Key is picked up and indicators activate

- **WHEN** a player picks up the key
- **THEN** all other connected players SHALL immediately see a directional arrow indicator on their HUD pointing in the direction of the key holder's current position

#### Scenario: Key holder does not see their own indicator

- **WHEN** a player picks up the key
- **THEN** that player SHALL NOT see a directional indicator pointing to themselves; the indicator is only displayed to non-holder players

#### Scenario: Indicator deactivates when key is dropped

- **WHEN** the key holder is eliminated and the key drops to the ground
- **THEN** the directional indicator SHALL be removed from all players' HUDs, as no player currently holds the key

#### Scenario: Indicator reactivates when key is re-picked-up

- **WHEN** a new player picks up a previously dropped key
- **THEN** the directional indicator SHALL reactivate on all non-holder players' HUDs, now pointing toward the new key holder

---

### Requirement: Indicator Direction Updates

The directional indicator SHALL update in real-time to track the key holder's position relative to each observing player's camera orientation.

#### Scenario: Indicator updates as key holder moves

- **WHEN** the key holder changes position within the maze
- **THEN** each non-holder player's directional indicator SHALL update its pointing direction to reflect the key holder's new position relative to the observing player's current camera facing direction

#### Scenario: Indicator updates as observing player rotates

- **WHEN** a non-holder player rotates their camera
- **THEN** the directional indicator SHALL update to maintain correct directional pointing toward the key holder relative to the new camera orientation

#### Scenario: Indicator direction is computed client-side

- **WHEN** the directional indicator is rendered
- **THEN** each client SHALL compute the indicator direction locally using the key holder's synchronized position (from NetworkVariable) and the local player's camera transform, without requiring per-frame network messages

---

### Requirement: Indicator Visual Design

The directional indicator SHALL be an on-screen arrow displayed at the edge of the player's HUD. It SHALL convey direction only, not distance or exact position.

#### Scenario: Indicator is displayed at screen edge

- **WHEN** the key holder's position is outside the observing player's camera viewport
- **THEN** the directional arrow SHALL be rendered at the edge of the screen in the direction of the key holder

#### Scenario: Indicator does not reveal distance

- **WHEN** the directional indicator is displayed
- **THEN** the indicator SHALL NOT change size, color, opacity, or any other visual property based on the distance between the observing player and the key holder

#### Scenario: Indicator does not reveal exact position

- **WHEN** the directional indicator is displayed
- **THEN** the indicator SHALL only convey the general direction toward the key holder; it SHALL NOT show a minimap icon, coordinate readout, or path overlay for the key holder's position

#### Scenario: Indicator is visually distinct from other HUD elements

- **WHEN** the directional indicator is rendered on the HUD
- **THEN** the indicator SHALL use a distinct color and style (e.g., a glowing gold arrow) that differentiates it from health bars, minimap elements, weapon indicators, and other UI components

#### Scenario: Key holder is within camera viewport

- **WHEN** the key holder's player model is visible within the observing player's camera viewport
- **THEN** the directional indicator MAY be hidden or reduced in opacity to avoid redundant visual noise, since the key holder is directly visible

---

### Requirement: Indicator Network Synchronization

The key holder's position MUST be available to all clients for indicator computation. The system SHALL use the existing player position NetworkVariable to avoid additional network traffic.

#### Scenario: Key holder position is available via existing sync

- **WHEN** the directional indicator system needs the key holder's position
- **THEN** the system SHALL read the key holder's position from the existing NetworkTransform or position NetworkVariable that is already synchronized for player movement, without sending additional position updates

#### Scenario: Key holder identity is synchronized

- **WHEN** a player picks up or drops the key
- **THEN** the host SHALL update a NetworkVariable identifying the current key holder (or indicating no holder), and all clients SHALL use this variable to determine which player's position to track

#### Scenario: Indicator handles key holder disconnection

- **WHEN** the key holder disconnects from the match
- **THEN** the host SHALL treat the disconnection as an elimination, trigger the key drop, and the directional indicator SHALL be removed from all players' HUDs

#### Scenario: Late-joining client receives indicator state

- **WHEN** a client joins or reconnects to a match where a player already holds the key
- **THEN** the joining client SHALL read the key holder NetworkVariable and immediately display the directional indicator pointing toward the current key holder
