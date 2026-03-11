## ADDED Requirements

### Requirement: WeaponData ScriptableObject

Each weapon type SHALL be defined as a `WeaponData` ScriptableObject containing all configurable properties for that weapon. The following properties MUST be defined: weapon name, damage per hit, fire rate (shots per second), ammo capacity (magazine size), max ammo (total carrying capacity), spread angle (degrees), effective range (Unity units), fire mode (single-shot or automatic), and reload time (seconds).

#### Scenario: Pistol weapon data
- **WHEN** the pistol `WeaponData` is loaded
- **THEN** it SHALL have low damage, moderate fire rate, single-shot fire mode, unlimited ammo (max ammo set to -1 to indicate infinite), zero spread angle, and medium range

#### Scenario: Shotgun weapon data
- **WHEN** the shotgun `WeaponData` is loaded
- **THEN** it SHALL have high per-pellet damage, low fire rate, single-shot fire mode, limited ammo, a wide spread angle, short effective range, and a pellet count property defining the number of raycasts per shot

#### Scenario: SMG weapon data
- **WHEN** the SMG `WeaponData` is loaded
- **THEN** it SHALL have low damage per hit, high fire rate, automatic fire mode, limited ammo, small spread angle, and medium range

#### Scenario: Rifle weapon data
- **WHEN** the rifle `WeaponData` is loaded
- **THEN** it SHALL have high damage per hit, low fire rate, single-shot fire mode, limited ammo, zero spread angle, and long effective range

#### Scenario: Designer can tune values in Inspector
- **WHEN** a designer opens a `WeaponData` ScriptableObject in the Unity Inspector
- **THEN** all weapon properties SHALL be exposed as serialized fields editable without code modification

---

### Requirement: Weapon Inventory

The player SHALL maintain a weapon inventory consisting of the starting pistol (permanent, cannot be dropped) and up to 2 additional weapon slots for looted weapons. The player SHALL be able to switch between weapons in their inventory using number keys (1 for pistol, 2 and 3 for looted weapons) or the scroll wheel.

#### Scenario: Starting state
- **WHEN** a player spawns at match start
- **THEN** the player's inventory SHALL contain only the starting pistol in slot 1, with slots 2 and 3 empty

#### Scenario: Picking up first looted weapon
- **WHEN** a player with empty weapon slots picks up a weapon from a loot box
- **THEN** the weapon SHALL be placed in the first available slot (slot 2 if empty, otherwise slot 3)

#### Scenario: Picking up weapon with full inventory
- **WHEN** a player with both looted weapon slots occupied picks up a new weapon
- **THEN** the currently equipped looted weapon SHALL be dropped at the player's position and replaced by the new weapon in the same slot

#### Scenario: Dropped weapon is not recoverable in Phase 1
- **WHEN** a weapon is dropped due to inventory swap
- **THEN** the dropped weapon SHALL be destroyed (not available for re-pickup) in Phase 1, with the architecture supporting future drop-and-pickup functionality

#### Scenario: Switching weapons with number keys
- **WHEN** a player presses the 1, 2, or 3 key
- **THEN** the player SHALL equip the weapon in the corresponding slot, if a weapon exists in that slot

#### Scenario: Switching weapons with scroll wheel
- **WHEN** a player scrolls the mouse wheel
- **THEN** the player SHALL cycle to the next occupied weapon slot in order (skipping empty slots)

#### Scenario: Cannot switch to empty slot
- **WHEN** a player presses a number key for a slot that contains no weapon
- **THEN** no weapon switch SHALL occur and the currently equipped weapon SHALL remain active

#### Scenario: Pistol cannot be dropped
- **WHEN** any game action would attempt to remove the pistol from slot 1
- **THEN** the operation SHALL be rejected and the pistol SHALL remain in slot 1

---

### Requirement: Weapon Firing Mechanics

Each weapon SHALL fire according to the properties defined in its `WeaponData` ScriptableObject. All weapons SHALL use hitscan (raycasting) for hit detection. The fire input SHALL be the left mouse button. Automatic weapons SHALL continue firing while the button is held. Single-shot weapons SHALL fire once per button press.

#### Scenario: Single-shot weapon fires once per click
- **WHEN** a player clicks the fire button while equipped with a single-shot weapon (pistol, shotgun, rifle)
- **THEN** exactly one shot SHALL be fired, regardless of how long the button is held

#### Scenario: Automatic weapon fires continuously
- **WHEN** a player holds the fire button while equipped with an automatic weapon (SMG)
- **THEN** the weapon SHALL fire at its configured fire rate (shots per second) until the button is released or ammo is depleted

#### Scenario: Fire rate enforcement
- **WHEN** a player attempts to fire faster than the weapon's configured fire rate
- **THEN** the weapon SHALL ignore fire inputs that occur before the fire rate cooldown has elapsed

#### Scenario: Shotgun fires multiple pellets
- **WHEN** a player fires the shotgun
- **THEN** the weapon SHALL cast multiple rays (pellet count defined in `WeaponData`) within the configured spread cone angle, each ray independently checking for hits and applying per-pellet damage

#### Scenario: Hitscan range limit
- **WHEN** a weapon raycast is fired
- **THEN** the ray SHALL extend only to the weapon's configured effective range and SHALL NOT register hits beyond that distance

#### Scenario: Spread angle application
- **WHEN** a weapon with a non-zero spread angle fires
- **THEN** the ray direction SHALL be randomly deviated within the spread cone angle from the aim direction, with the deviation applied per-shot for single-ray weapons and per-pellet for multi-ray weapons

---

### Requirement: Ammo Management

All looted weapons SHALL track current ammo as an integer value. The starting pistol SHALL have unlimited ammo (never depleted). When a looted weapon's ammo reaches zero, the weapon SHALL not fire until ammo is replenished. Ammo counts SHALL be stored per weapon instance in the player's inventory.

#### Scenario: Firing consumes ammo
- **WHEN** a player fires a looted weapon with ammo remaining
- **THEN** the weapon's current ammo count SHALL decrease by 1 (or by pellet count for shotgun)

#### Scenario: Weapon cannot fire at zero ammo
- **WHEN** a player attempts to fire a looted weapon with zero ammo
- **THEN** the weapon SHALL not fire and SHALL play an empty/click audio cue

#### Scenario: Pistol never runs out of ammo
- **WHEN** a player fires the starting pistol
- **THEN** the ammo count SHALL not decrease and the pistol SHALL always be fireable

#### Scenario: Ammo replenishment from ammo pickup
- **WHEN** a player receives an ammo pickup for a weapon in their inventory
- **THEN** the weapon's current ammo SHALL increase by the ammo pickup amount, capped at the weapon's max ammo defined in `WeaponData`

#### Scenario: Ammo display on HUD
- **WHEN** a player has a looted weapon equipped
- **THEN** the HUD SHALL display the weapon's current ammo count and maximum ammo capacity

#### Scenario: Pistol ammo display
- **WHEN** a player has the pistol equipped
- **THEN** the HUD SHALL display an infinity symbol or equivalent indicator instead of a numeric ammo count

---

### Requirement: Weapon NetworkBehaviour and State Sync

All weapon-related scripts SHALL inherit from `NetworkBehaviour`. The player's equipped weapon index and per-weapon ammo counts SHALL be stored as `NetworkVariable` values for multiplayer readiness. Weapon switching and firing events SHALL be structured to support future ServerRpc/ClientRpc networking.

#### Scenario: Weapon scripts use NetworkBehaviour
- **WHEN** a weapon system script is created
- **THEN** it MUST inherit from `Unity.Netcode.NetworkBehaviour`

#### Scenario: Equipped weapon synced via NetworkVariable
- **WHEN** a player switches weapons
- **THEN** the equipped weapon index SHALL be updated in a `NetworkVariable<int>` so other clients can display the correct weapon on that player

#### Scenario: Ammo state stored in NetworkVariable
- **WHEN** a weapon's ammo count changes (firing or pickup)
- **THEN** the ammo count SHALL be updated in a network-synced data structure readable by the host for validation purposes
