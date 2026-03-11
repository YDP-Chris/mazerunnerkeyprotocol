## ADDED Requirements

### Requirement: Loot Table Definition

The system SHALL define a loot table as a ScriptableObject that specifies all possible loot box drops and their weighted probabilities. Each entry in the loot table SHALL have an item type, an item reference, and a weight value. The loot table MUST contain entries for: shotgun, SMG, rifle, ammo pickup, and health pack.

#### Scenario: Loot table contains all required item types
- **WHEN** the loot table ScriptableObject is loaded at match start
- **THEN** it SHALL contain entries for at least the following items: shotgun, SMG, rifle, ammo pickup, and health pack

#### Scenario: Weights determine drop probability
- **WHEN** a loot box rolls for its contents
- **THEN** the probability of each item being selected SHALL be proportional to its weight relative to the sum of all weights in the table

#### Scenario: Default weight distribution favors supplies over weapons
- **WHEN** the default loot table is used
- **THEN** ammo pickups and health packs SHALL have higher combined weight than weapons, such that approximately 60% of loot boxes contain supplies (ammo or health) and 40% contain weapons

#### Scenario: Loot table is editable without code changes
- **WHEN** a designer wants to adjust drop rates or add a new item
- **THEN** they SHALL be able to modify the loot table ScriptableObject in the Unity Inspector without modifying any C# code

---

### Requirement: Single Item Per Loot Box

Each loot box SHALL contain exactly one item. The item is determined at match start by the host using the loot table and the match seed. The contents SHALL NOT change after determination.

#### Scenario: One item granted per loot interaction
- **WHEN** a player loots a loot box
- **THEN** exactly one item SHALL be granted to the player

#### Scenario: Contents determined at match start
- **WHEN** the host spawns loot boxes at match start
- **THEN** the host SHALL roll the loot table for each box and assign its contents immediately, storing the result in a `NetworkVariable`

#### Scenario: Contents are immutable after assignment
- **WHEN** a loot box has been assigned its contents
- **THEN** the contents SHALL NOT change for the remainder of the match, regardless of player actions or game state changes

---

### Requirement: Weapon Drops

The loot table SHALL include three weapon types as possible drops: shotgun, SMG, and rifle. Each weapon drop SHALL grant the player the weapon with its default starting ammo as defined by the weapon's `WeaponData` ScriptableObject.

#### Scenario: Shotgun drop
- **WHEN** a loot box containing a shotgun is looted
- **THEN** the player SHALL receive a shotgun weapon with its default starting ammo count as defined in the shotgun `WeaponData`

#### Scenario: SMG drop
- **WHEN** a loot box containing an SMG is looted
- **THEN** the player SHALL receive an SMG weapon with its default starting ammo count as defined in the SMG `WeaponData`

#### Scenario: Rifle drop
- **WHEN** a loot box containing a rifle is looted
- **THEN** the player SHALL receive a rifle weapon with its default starting ammo count as defined in the rifle `WeaponData`

#### Scenario: Duplicate weapon pickup adds ammo
- **WHEN** a player loots a weapon they already have in their inventory
- **THEN** the player SHALL receive the weapon's default starting ammo added to their existing ammo count for that weapon, capped at the weapon's maximum ammo capacity

---

### Requirement: Ammo Pickup Drops

The loot table SHALL include a generic ammo pickup item. Ammo pickups SHALL replenish ammunition for the player's currently equipped weapon (excluding the pistol, which has unlimited ammo). The ammo amount SHALL be configurable via a ScriptableObject.

#### Scenario: Ammo pickup for equipped weapon
- **WHEN** a player with a shotgun equipped loots a box containing an ammo pickup
- **THEN** the player's shotgun ammo SHALL increase by the configured ammo pickup amount, capped at the shotgun's maximum ammo capacity

#### Scenario: Ammo pickup with only pistol equipped
- **WHEN** a player with only the starting pistol (no looted weapons) loots a box containing an ammo pickup
- **THEN** the ammo pickup SHALL be applied to the first non-pistol weapon in the player's inventory, or if the player has no non-pistol weapons, the ammo pickup SHALL be stored as reserve ammo for the next weapon picked up

#### Scenario: Ammo does not exceed max capacity
- **WHEN** an ammo pickup would cause the weapon's ammo to exceed its maximum capacity
- **THEN** the weapon's ammo SHALL be clamped to its maximum capacity and the excess SHALL be discarded

---

### Requirement: Health Pack Drops

The loot table SHALL include a health pack item. Health packs SHALL restore a configurable amount of health to the player, not exceeding the player's maximum health. The heal amount SHALL be defined in a ScriptableObject.

#### Scenario: Health pack restores health
- **WHEN** a player with less than maximum health loots a box containing a health pack
- **THEN** the player's health SHALL increase by the configured heal amount

#### Scenario: Health does not exceed maximum
- **WHEN** a health pack would restore health beyond the player's maximum health
- **THEN** the player's health SHALL be clamped to maximum health and the excess healing SHALL be discarded

#### Scenario: Health pack at full health
- **WHEN** a player at maximum health loots a box containing a health pack
- **THEN** the health pack SHALL still be consumed (the loot box is destroyed) but the player's health SHALL remain at maximum

---

### Requirement: Loot Box Interaction and Destruction

A player SHALL loot a box by entering its trigger collider and pressing the interaction key (default: E). The loot box SHALL be destroyed (despawned from the network) after being looted. Loot boxes are single-use and MUST NOT be lootable more than once.

#### Scenario: Player loots a box successfully
- **WHEN** a player is within the loot box trigger collider and presses the interaction key
- **THEN** the loot box SHALL grant its item to the player, play a visual/audio feedback effect, and be destroyed (network despawned)

#### Scenario: Loot box cannot be looted twice
- **WHEN** a player attempts to interact with a loot box that has already been looted
- **THEN** the interaction SHALL be rejected and no item SHALL be granted

#### Scenario: Only one player can loot a box in a race condition
- **WHEN** two players attempt to loot the same box at nearly the same time
- **THEN** the host SHALL process pickup requests sequentially, granting the item to the first valid request and rejecting the second

#### Scenario: Visual indicator before looting
- **WHEN** a player approaches an available (unlooted) loot box
- **THEN** the loot box SHALL display a visual glow or pulse effect indicating it is available for interaction

#### Scenario: Interaction requires proximity
- **WHEN** a player presses the interaction key while not within any loot box trigger collider
- **THEN** no loot interaction SHALL occur

---

### Requirement: Host-Authoritative Loot Pickup

Loot pickup requests SHALL be sent as ServerRpc calls from the requesting client to the host. The host SHALL validate the request (box exists, box is available, player is in range) before granting the item. The host SHALL update the loot box `NetworkVariable` state to looted and despawn the box.

#### Scenario: Valid pickup request
- **WHEN** the host receives a loot pickup ServerRpc and the box is available and the player is within valid range
- **THEN** the host SHALL grant the item to the requesting player, set the box state to looted, and despawn the loot box network object

#### Scenario: Invalid pickup request -- box already looted
- **WHEN** the host receives a loot pickup ServerRpc for a box that has already been looted
- **THEN** the host SHALL reject the request and not grant any item

#### Scenario: Invalid pickup request -- player out of range
- **WHEN** the host receives a loot pickup ServerRpc but the player's position is outside the valid interaction range
- **THEN** the host SHALL reject the request and not grant any item
