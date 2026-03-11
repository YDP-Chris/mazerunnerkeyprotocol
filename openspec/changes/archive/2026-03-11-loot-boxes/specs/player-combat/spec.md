## MODIFIED Requirements

### Requirement: Combat System Supports Multiple Weapon Types

The player combat system SHALL support firing any weapon in the player's inventory, not only the starting pistol. The combat system SHALL delegate all firing behavior (damage, fire rate, spread, range, fire mode) to the currently equipped weapon's `WeaponData` ScriptableObject. The combat system SHALL no longer hardcode pistol-specific firing logic.

#### Scenario: Firing delegates to equipped weapon data
- **WHEN** a player presses the fire button
- **THEN** the combat system SHALL read damage, fire rate, spread angle, range, and fire mode from the currently equipped weapon's `WeaponData` and fire accordingly

#### Scenario: Switching weapons changes combat behavior
- **WHEN** a player switches from the pistol to a shotgun
- **THEN** the next fire input SHALL use the shotgun's `WeaponData` properties (multi-pellet spread, higher damage, lower fire rate) instead of the pistol's properties

#### Scenario: Weapon swap interrupts fire cooldown
- **WHEN** a player switches weapons while in the middle of a fire rate cooldown
- **THEN** the cooldown from the previous weapon SHALL be canceled and the new weapon's fire rate cooldown SHALL apply from the moment of the switch

---

### Requirement: Damage Application Uses Weapon-Specific Values

The combat system SHALL apply damage to hit targets based on the equipped weapon's damage value from its `WeaponData`. The existing raycast hit detection logic SHALL be extended to support per-weapon range limits and spread patterns. Damage SHALL be applied per ray hit (per pellet for shotgun).

#### Scenario: Pistol damage unchanged
- **WHEN** a player fires the starting pistol and hits a target
- **THEN** the target SHALL receive the pistol's configured damage value (low damage, matching pre-modification behavior)

#### Scenario: Shotgun damage per pellet
- **WHEN** a player fires the shotgun and multiple pellets hit the same target
- **THEN** the target SHALL receive damage equal to the per-pellet damage multiplied by the number of pellets that hit, allowing for significant close-range burst damage

#### Scenario: Rifle damage at long range
- **WHEN** a player fires the rifle and hits a target within the rifle's effective range
- **THEN** the target SHALL receive the rifle's configured high damage value

#### Scenario: Shot beyond effective range
- **WHEN** a weapon raycast hits a target beyond the weapon's effective range
- **THEN** no damage SHALL be applied to the target

---

### Requirement: Fire Input Supports Automatic and Single-Shot Modes

The combat system SHALL support two fire modes: single-shot (fire once per button press) and automatic (fire continuously while button is held). The fire mode SHALL be determined by the equipped weapon's `WeaponData`. The existing fire input handling SHALL be modified to check the fire mode before processing input.

#### Scenario: Single-shot mode on button press
- **WHEN** a player presses and holds the fire button with a single-shot weapon equipped
- **THEN** the weapon SHALL fire exactly once, and SHALL NOT fire again until the button is released and pressed again

#### Scenario: Automatic mode while button held
- **WHEN** a player holds the fire button with an automatic weapon equipped
- **THEN** the weapon SHALL fire repeatedly at the weapon's configured fire rate until the button is released, ammo is depleted, or the player switches weapons

#### Scenario: Releasing fire button stops automatic fire
- **WHEN** a player releases the fire button during automatic fire
- **THEN** the weapon SHALL stop firing immediately after the current shot completes

---

### Requirement: Ammo Consumption Integrated into Combat Flow

The combat system SHALL check ammo availability before each shot. If the equipped weapon has zero ammo (and is not the pistol), the shot SHALL be blocked. The combat system SHALL decrement ammo after each successful shot and notify the HUD of the updated count.

#### Scenario: Shot consumes ammo
- **WHEN** a player fires a looted weapon that has ammo remaining
- **THEN** the weapon's ammo count SHALL decrease by 1 after the shot is fired

#### Scenario: Empty weapon blocks firing
- **WHEN** a player presses the fire button with a looted weapon that has zero ammo
- **THEN** no shot SHALL be fired, no raycast SHALL be performed, and an empty-weapon audio/visual cue SHALL be triggered

#### Scenario: Pistol bypasses ammo check
- **WHEN** a player fires the starting pistol
- **THEN** the ammo check SHALL be skipped and the shot SHALL always proceed

#### Scenario: Ammo depletion during automatic fire
- **WHEN** a player is holding the fire button with an automatic weapon and ammo reaches zero
- **THEN** automatic fire SHALL stop immediately and the empty-weapon cue SHALL be triggered

---

### Requirement: Raycast Hit Detection Extended for Weapon Variety

The existing single-ray hitscan system SHALL be extended to support multi-ray spread patterns (for shotgun) and per-weapon range limits. The `LayerMask` for damageable targets SHALL remain unchanged. Maze walls SHALL continue to block all weapon raycasts.

#### Scenario: Single-ray weapons
- **WHEN** a player fires the pistol, SMG, or rifle
- **THEN** a single raycast SHALL be performed from the camera center forward, subject to the weapon's spread angle and range

#### Scenario: Multi-ray shotgun pattern
- **WHEN** a player fires the shotgun
- **THEN** multiple raycasts (count defined in `WeaponData`) SHALL be performed, each with a random direction within the spread cone, and each ray independently detecting hits and applying damage

#### Scenario: Maze walls block all weapon fire
- **WHEN** a weapon raycast hits a maze wall before reaching a target
- **THEN** the ray SHALL stop at the wall and no damage SHALL be applied to any target behind it

#### Scenario: Range-limited raycast
- **WHEN** a raycast extends beyond the weapon's effective range without hitting anything
- **THEN** the ray SHALL terminate at the effective range distance and no hit SHALL be registered

---

### Requirement: Combat HUD Updated for Active Weapon

The combat HUD SHALL display information about the currently equipped weapon, including the weapon name or icon, current ammo count (or infinity symbol for pistol), and a crosshair appropriate to the weapon type. The HUD SHALL update immediately when the player switches weapons.

#### Scenario: HUD shows equipped weapon name
- **WHEN** a player switches to a different weapon
- **THEN** the HUD SHALL display the newly equipped weapon's name as defined in its `WeaponData`

#### Scenario: HUD shows ammo for looted weapons
- **WHEN** a player has a looted weapon equipped
- **THEN** the HUD SHALL display the current ammo count and max ammo in the format "current / max"

#### Scenario: HUD shows infinity for pistol
- **WHEN** a player has the starting pistol equipped
- **THEN** the HUD SHALL display an infinity symbol or "INF" instead of a numeric ammo count

#### Scenario: HUD updates on ammo change
- **WHEN** a player fires a shot or receives an ammo pickup
- **THEN** the HUD ammo display SHALL update immediately to reflect the new ammo count

#### Scenario: Crosshair adapts to weapon spread
- **WHEN** a player equips a weapon with a non-zero spread angle
- **THEN** the crosshair SHALL visually indicate the spread area (wider crosshair for shotgun, tighter for rifle/pistol)
