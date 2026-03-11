## ADDED Requirements

### Requirement: HUD Canvas Setup

A screen-space overlay `Canvas` SHALL exist in the scene for rendering all player HUD elements. The canvas SHALL use a `CanvasScaler` set to "Scale With Screen Size" with a reference resolution of 1920x1080. The HUD SHALL only be visible for the local (owning) player. The `PlayerUI` script SHALL inherit from `NetworkBehaviour`.

#### Scenario: Canvas exists in scene

- **WHEN** the game scene is loaded
- **THEN** a Canvas with render mode "Screen Space - Overlay" SHALL exist for the player HUD

#### Scenario: Canvas scaling

- **WHEN** the game runs at a resolution other than 1920x1080 (e.g., 2560x1440 or 1280x720)
- **THEN** all HUD elements SHALL scale proportionally to maintain correct sizing and positioning

#### Scenario: HUD owner-only visibility

- **WHEN** a player instance has `IsOwner` set to false
- **THEN** the HUD Canvas SHALL be disabled for that instance so only the local player sees their own HUD

#### Scenario: NetworkBehaviour base class

- **WHEN** the `PlayerUI` script is inspected
- **THEN** it SHALL inherit from `Unity.Netcode.NetworkBehaviour`

---

### Requirement: Health Bar Display

The HUD SHALL display the player's current health as a horizontal health bar. The health bar SHALL be positioned in the lower-left area of the screen. The bar SHALL visually represent the ratio of current health to maximum health (full bar = max health, empty bar = 0 health). The bar SHALL update in real-time as the player takes damage or heals.

#### Scenario: Full health display

- **WHEN** the player has 100/100 health
- **THEN** the health bar fill SHALL be at 100% width

#### Scenario: Partial health display

- **WHEN** the player has 50/100 health
- **THEN** the health bar fill SHALL be at 50% width

#### Scenario: Zero health display

- **WHEN** the player's health reaches 0
- **THEN** the health bar fill SHALL be at 0% width (completely empty)

#### Scenario: Damage updates bar

- **WHEN** the player takes 25 damage from 100 health
- **THEN** the health bar fill SHALL decrease from 100% to 75% width

#### Scenario: Heal updates bar

- **WHEN** the player heals 20 health from 60 health (max 100)
- **THEN** the health bar fill SHALL increase from 60% to 80% width

---

### Requirement: Health Bar Color Coding

The health bar fill color SHALL change based on the player's health percentage to provide at-a-glance status. Green when above 60%, yellow when between 30% and 60%, red when below 30%. Color transitions SHALL be immediate (no lerping required in Phase 1).

#### Scenario: High health color

- **WHEN** the player's health is at 75% (75/100)
- **THEN** the health bar fill color SHALL be green

#### Scenario: Medium health color

- **WHEN** the player's health is at 45% (45/100)
- **THEN** the health bar fill color SHALL be yellow

#### Scenario: Low health color

- **WHEN** the player's health is at 20% (20/100)
- **THEN** the health bar fill color SHALL be red

#### Scenario: Color updates with damage

- **WHEN** the player takes damage that reduces health from 65% to 55%
- **THEN** the health bar color SHALL change from green to yellow

---

### Requirement: Health Text Display

The HUD SHALL display a numeric health readout alongside the health bar showing current health and maximum health in the format "current / max" (e.g., "75 / 100"). The text SHALL update whenever health changes.

#### Scenario: Full health text

- **WHEN** the player has 100/100 health
- **THEN** the health text SHALL display "100 / 100"

#### Scenario: Damaged health text

- **WHEN** the player has 42/100 health
- **THEN** the health text SHALL display "42 / 100"

#### Scenario: Zero health text

- **WHEN** the player's health reaches 0
- **THEN** the health text SHALL display "0 / 100"

---

### Requirement: Crosshair Display

The HUD SHALL display a crosshair image at the exact center of the screen. The crosshair SHALL be a simple static image (e.g., a dot or cross shape). The crosshair SHALL remain fixed at screen center regardless of camera movement or player position. The crosshair SHALL be hidden when the player is eliminated.

#### Scenario: Crosshair at screen center

- **WHEN** the game is running and the player is alive
- **THEN** a crosshair image SHALL be rendered at the exact center of the screen

#### Scenario: Crosshair stays centered during movement

- **WHEN** the player moves or rotates the camera
- **THEN** the crosshair SHALL remain fixed at the screen center

#### Scenario: Crosshair hidden on death

- **WHEN** the player is eliminated
- **THEN** the crosshair SHALL be hidden (disabled or set to transparent)

#### Scenario: Crosshair visible at all times while alive

- **WHEN** the player is alive and the game is active
- **THEN** the crosshair SHALL always be visible, not obscured by other UI elements (highest sort order among HUD elements)

---

### Requirement: Damage Indicator Flash

When the player takes damage, the HUD SHALL briefly flash a red vignette or screen-edge tint to indicate damage was received. The flash SHALL last approximately 0.2-0.4 seconds and fade out. The flash SHALL not obstruct the crosshair or critical HUD elements.

#### Scenario: Damage flash triggers

- **WHEN** the player takes any amount of damage
- **THEN** a red damage flash effect SHALL appear on the screen edges

#### Scenario: Damage flash duration

- **WHEN** the damage flash appears
- **THEN** it SHALL fade out over 0.2 to 0.4 seconds

#### Scenario: Damage flash does not block crosshair

- **WHEN** the damage flash is active
- **THEN** the crosshair at screen center SHALL remain clearly visible

#### Scenario: Multiple rapid damage flashes

- **WHEN** the player takes damage twice in quick succession
- **THEN** each damage event SHALL trigger the flash effect (restart the flash timer if already active)

---

### Requirement: Elimination Screen

When the player is eliminated, the HUD SHALL display an elimination message. The message SHALL read "ELIMINATED" in large text at the center of the screen. The elimination message SHALL appear after the death delay (0.5 seconds, matching the `PlayerHealth` deactivation delay). The HUD health bar and crosshair SHALL be hidden during the elimination screen.

#### Scenario: Elimination message appears

- **WHEN** the player's health reaches 0 and the death delay completes
- **THEN** the text "ELIMINATED" SHALL be displayed at the center of the screen

#### Scenario: HUD elements hidden

- **WHEN** the elimination screen is shown
- **THEN** the health bar, health text, and crosshair SHALL be hidden

#### Scenario: Elimination message persists

- **WHEN** the elimination message appears
- **THEN** it SHALL remain visible for the remainder of the match (until the match ends)

---

### Requirement: HUD Subscribes to Health Events

The `PlayerUI` script SHALL subscribe to the `PlayerHealth` component's `OnDamaged`, `OnHealed`, and `OnDied` events to update the UI. The UI SHALL NOT poll health values each frame. Event subscription SHALL occur in `OnNetworkSpawn` and unsubscription SHALL occur in `OnNetworkDespawn`.

#### Scenario: Event-driven updates

- **WHEN** `OnDamaged` fires on `PlayerHealth`
- **THEN** `PlayerUI` SHALL update the health bar fill, health text, color, and trigger the damage flash

#### Scenario: Heal event updates

- **WHEN** `OnHealed` fires on `PlayerHealth`
- **THEN** `PlayerUI` SHALL update the health bar fill, health text, and color

#### Scenario: Death event triggers elimination screen

- **WHEN** `OnDied` fires on `PlayerHealth`
- **THEN** `PlayerUI` SHALL display the elimination screen and hide combat HUD elements

#### Scenario: Subscription lifecycle

- **WHEN** `OnNetworkSpawn` is called on the player
- **THEN** `PlayerUI` SHALL subscribe to all `PlayerHealth` events

#### Scenario: Unsubscription lifecycle

- **WHEN** `OnNetworkDespawn` is called on the player
- **THEN** `PlayerUI` SHALL unsubscribe from all `PlayerHealth` events to prevent memory leaks
