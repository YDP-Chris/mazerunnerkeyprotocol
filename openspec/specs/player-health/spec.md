## ADDED Requirements

### Requirement: Health NetworkVariable

The `PlayerHealth` script SHALL store the player's current health as a `NetworkVariable<int>`. The script SHALL inherit from `NetworkBehaviour`. Maximum health SHALL default to 100 and be configurable via a serialized field. Health SHALL be initialized to maximum health when the player spawns.

#### Scenario: Player spawn health

- **WHEN** the player prefab is instantiated at the start of a match
- **THEN** the player's health SHALL be set to 100 (or the configured maximum)

#### Scenario: Health stored as NetworkVariable

- **WHEN** the `PlayerHealth` script is inspected
- **THEN** health SHALL be declared as `NetworkVariable<int>` with server write permission

#### Scenario: NetworkBehaviour base class

- **WHEN** the `PlayerHealth` script is inspected
- **THEN** it SHALL inherit from `Unity.Netcode.NetworkBehaviour`, not `MonoBehaviour`

---

### Requirement: Damage Intake

The `PlayerHealth` script SHALL expose a public `TakeDamage(int amount)` method. This method SHALL reduce current health by the specified amount. Health SHALL NOT drop below zero. The method SHALL only execute on the server/host (checked via `IsServer` or equivalent guard).

#### Scenario: Taking damage

- **WHEN** `TakeDamage(25)` is called on a player with 100 health
- **THEN** the player's health SHALL become 75

#### Scenario: Damage does not go below zero

- **WHEN** `TakeDamage(150)` is called on a player with 100 health
- **THEN** the player's health SHALL become 0, not -50

#### Scenario: Multiple damage events

- **WHEN** `TakeDamage(30)` is called twice on a player with 100 health
- **THEN** the player's health SHALL be 40 after both calls

#### Scenario: Zero damage

- **WHEN** `TakeDamage(0)` is called
- **THEN** the player's health SHALL remain unchanged

#### Scenario: Server authority

- **WHEN** `TakeDamage` is called and `IsServer` is false
- **THEN** the damage SHALL NOT be applied (method returns without modifying health)

---

### Requirement: Damage Event

The `PlayerHealth` script SHALL expose a C# event `OnDamaged` that fires whenever the player takes damage. The event SHALL pass the damage amount and the current health after damage as parameters. Other systems (UI, audio, VFX) SHALL subscribe to this event rather than polling health values.

#### Scenario: Damage event fires

- **WHEN** the player takes 25 damage
- **THEN** the `OnDamaged` event SHALL fire with parameters (damageAmount: 25, currentHealth: 75)

#### Scenario: UI subscribes to damage

- **WHEN** the `PlayerHealthUI` component subscribes to `OnDamaged`
- **THEN** it SHALL receive a callback every time damage is applied

#### Scenario: No event on zero damage

- **WHEN** `TakeDamage(0)` is called
- **THEN** the `OnDamaged` event SHALL NOT fire

---

### Requirement: Death and Elimination

When health reaches zero, the player SHALL be marked as eliminated. The `PlayerHealth` script SHALL expose a C# event `OnDied` that fires when health reaches zero. The player's GameObject SHALL be deactivated (not destroyed) to preserve the NetworkObject identity for multiplayer. Eliminated players SHALL NOT be able to take further damage.

#### Scenario: Health reaches zero

- **WHEN** the player's health is reduced to 0
- **THEN** the `OnDied` event SHALL fire

#### Scenario: GameObject deactivation

- **WHEN** the player is eliminated
- **THEN** the player's GameObject SHALL be deactivated via `SetActive(false)` after a configurable delay (default 0.5 seconds) to allow death effects

#### Scenario: No respawn

- **WHEN** the player is eliminated during a match
- **THEN** the player SHALL NOT respawn for the remainder of that match

#### Scenario: Damage after death

- **WHEN** `TakeDamage` is called on an already-eliminated player
- **THEN** no damage SHALL be applied and no events SHALL fire

#### Scenario: Elimination state flag

- **WHEN** the player is eliminated
- **THEN** a public `IsEliminated` property SHALL return true

#### Scenario: Elimination state at spawn

- **WHEN** the player first spawns
- **THEN** `IsEliminated` SHALL return false

---

### Requirement: Health Clamping

Health SHALL always be clamped between 0 and the maximum health value. Health SHALL NOT exceed maximum health through any means. Health SHALL NOT go below zero.

#### Scenario: Health cannot exceed max

- **WHEN** the player has 100 max health and 100 current health
- **THEN** any attempt to increase health above 100 SHALL result in health remaining at 100

#### Scenario: Health cannot go negative

- **WHEN** the player has 10 health and takes 50 damage
- **THEN** health SHALL be clamped to 0

---

### Requirement: Heal Method

The `PlayerHealth` script SHALL expose a public `Heal(int amount)` method for use by health packs from loot boxes. Healing SHALL add the specified amount to current health, clamped to maximum health. Healing SHALL NOT work on eliminated players. The method SHALL only execute on the server/host.

#### Scenario: Healing from partial health

- **WHEN** `Heal(30)` is called on a player with 60 health (max 100)
- **THEN** the player's health SHALL become 90

#### Scenario: Healing does not exceed max

- **WHEN** `Heal(50)` is called on a player with 80 health (max 100)
- **THEN** the player's health SHALL become 100, not 130

#### Scenario: Healing a dead player

- **WHEN** `Heal(50)` is called on an eliminated player
- **THEN** health SHALL remain at 0 and the player SHALL remain eliminated

#### Scenario: Heal event

- **WHEN** the player is healed
- **THEN** an `OnHealed` event SHALL fire with the heal amount and current health as parameters

---

### Requirement: NetworkVariable Change Callback

The health `NetworkVariable` SHALL have an `OnValueChanged` callback registered. This callback SHALL be used to drive client-side reactions (UI updates, damage effects) so that all clients reflect health changes even when only the server modifies the value.

#### Scenario: Health value changes on server

- **WHEN** the server modifies the health NetworkVariable
- **THEN** the `OnValueChanged` callback SHALL fire on all clients with the previous and new health values

#### Scenario: UI updates from callback

- **WHEN** `OnValueChanged` fires on a client
- **THEN** the health bar UI SHALL update to reflect the new health value
