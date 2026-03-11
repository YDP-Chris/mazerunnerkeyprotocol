## ADDED Requirements

### Requirement: LootBox ServerRpc rejects pickup when already looted
When `RequestPickupServerRpc` is called on a LootBox whose `isAvailable` is false, the request SHALL be ignored and no item SHALL be granted.

#### Scenario: Second pickup attempt is rejected
- **WHEN** a LootBox has `isAvailable.Value == false` (already looted)
- **WHEN** `RequestPickupServerRpc` is called
- **THEN** no weapon, ammo, or health SHALL be granted to the requesting player
- **THEN** `isAvailable.Value` SHALL remain false

### Requirement: LootBox grants weapon to player inventory on pickup
When a weapon-type LootBox is successfully picked up, the player's WeaponInventory SHALL receive the weapon via `AddWeapon`.

#### Scenario: Weapon loot box grants weapon
- **WHEN** a LootBox with `assignedItemType` set to Shotgun/SMG/Rifle is available
- **WHEN** a valid player within interaction range calls `RequestPickupServerRpc`
- **THEN** the player's WeaponInventory SHALL contain the weapon in a non-pistol slot
- **THEN** `isAvailable.Value` SHALL become false

### Requirement: LootBox grants health on HealthPack pickup
When a HealthPack-type LootBox is picked up, the player's PlayerHealth SHALL be healed by the configured `healAmount`.

#### Scenario: Health pack heals player
- **WHEN** a LootBox with `assignedItemType` set to HealthPack is available
- **WHEN** a player with less than max health calls `RequestPickupServerRpc` within range
- **THEN** the player's `CurrentHealth.Value` SHALL increase by the heal amount (clamped to max)

### Requirement: LootBox race condition handling rejects second caller
When two `RequestPickupServerRpc` calls arrive for the same LootBox in the same frame, only the first SHALL succeed. The second SHALL be rejected because `isAvailable` is already false.

#### Scenario: Two simultaneous pickup requests
- **WHEN** two different players call `RequestPickupServerRpc` on the same available LootBox
- **THEN** exactly one player SHALL receive the loot item
- **THEN** the second call SHALL have no effect

### Requirement: LootBox validates distance before granting item
The ServerRpc SHALL check that the requesting player is within `interactionRange + 1f` units. Requests from players too far away SHALL be rejected.

#### Scenario: Player too far away is rejected
- **WHEN** a player calls `RequestPickupServerRpc` from beyond `interactionRange + 1f` units
- **THEN** no item SHALL be granted
- **THEN** `isAvailable.Value` SHALL remain true

### Requirement: WeaponInventory starts with pistol in slot 0
On network spawn, slot 0 SHALL contain the pistol WeaponData with infinite ammo (-1). Slots 1 and 2 SHALL be empty.

#### Scenario: Initial inventory state
- **WHEN** a player's WeaponInventory is network-spawned
- **THEN** `GetWeaponInSlot(0)` SHALL return the pistol WeaponData
- **THEN** `GetAmmoInSlot(0)` SHALL equal -1
- **THEN** `GetWeaponInSlot(1)` SHALL return null
- **THEN** `GetWeaponInSlot(2)` SHALL return null

### Requirement: WeaponInventory switching changes equipped slot
Calling `SwitchToSlot(slot)` SHALL update `equippedSlot.Value` when the slot contains a weapon. Switching to an empty slot SHALL be ignored.

#### Scenario: Switch to occupied slot succeeds
- **WHEN** slot 1 contains a weapon
- **WHEN** `SwitchToSlot(1)` is called
- **THEN** `equippedSlot.Value` SHALL equal 1

#### Scenario: Switch to empty slot is ignored
- **WHEN** slot 2 is empty (null)
- **WHEN** `SwitchToSlot(2)` is called
- **THEN** `equippedSlot.Value` SHALL remain unchanged

### Requirement: WeaponInventory CycleWeapon skips empty slots
`CycleWeapon(direction)` SHALL advance to the next occupied slot, skipping empty slots.

#### Scenario: Cycle forward skips empty slot
- **WHEN** slot 0 has pistol, slot 1 is empty, slot 2 has a weapon
- **WHEN** `CycleWeapon(1)` is called from slot 0
- **THEN** `equippedSlot.Value` SHALL equal 2

### Requirement: WeaponInventory ConsumeAmmo decrements and reports empty
`ConsumeAmmo()` SHALL decrement ammo for the equipped non-pistol slot and return true. When ammo reaches 0, it SHALL return false on the next call. Pistol slot (0) SHALL always return true without decrementing.

#### Scenario: Pistol never runs out
- **WHEN** equipped slot is 0 (pistol)
- **THEN** `ConsumeAmmo()` SHALL return true
- **THEN** `GetAmmoInSlot(0)` SHALL remain -1

#### Scenario: Non-pistol ammo decrements
- **WHEN** equipped slot is 1 with 5 ammo
- **WHEN** `ConsumeAmmo()` is called
- **THEN** `GetAmmoInSlot(1)` SHALL equal 4
- **THEN** return value SHALL be true

#### Scenario: Empty weapon fails to consume
- **WHEN** equipped slot is 1 with 0 ammo
- **THEN** `ConsumeAmmo()` SHALL return false

### Requirement: WeaponInventory AddWeapon handles duplicate by adding ammo
When `AddWeapon` is called with a weapon whose `weaponName` matches an existing slot, ammo SHALL be added to that slot instead of creating a new entry.

#### Scenario: Duplicate weapon adds ammo
- **WHEN** slot 1 has a Shotgun with 4 ammo
- **WHEN** `AddWeapon(shotgunData, LootItemType.Shotgun)` is called
- **THEN** slot 1 ammo SHALL increase by the weapon's `ammoCapacity`
- **THEN** no new slot SHALL be occupied

### Requirement: PlayerCombat single-shot mode fires once per press
In `FireMode.Single` mode, pulling the trigger SHALL fire exactly one ray per press, regardless of how long the button is held. A subsequent press SHALL be allowed only after `1/fireRate` seconds.

#### Scenario: Single-shot fires one ray
- **WHEN** the equipped weapon has `fireMode == FireMode.Single` and `pelletCount == 1`
- **WHEN** the fire action is pressed once
- **THEN** exactly one raycast SHALL be performed

### Requirement: PlayerCombat automatic mode fires continuously while held
In `FireMode.Automatic` mode, holding the fire button SHALL fire at the weapon's `fireRate` interval continuously.

#### Scenario: Automatic fires on sustained hold
- **WHEN** the equipped weapon has `fireMode == FireMode.Automatic`
- **WHEN** the fire button is held for a duration exceeding `2 / fireRate`
- **THEN** at least 2 raycasts SHALL be performed

### Requirement: PlayerCombat multi-ray shotgun fires pelletCount rays
When a weapon has `pelletCount > 1`, firing SHALL cast `pelletCount` rays, each with spread applied.

#### Scenario: Shotgun fires multiple pellets
- **WHEN** the equipped weapon has `pelletCount == 6`
- **WHEN** the weapon fires
- **THEN** 6 raycasts SHALL be performed, each with spread applied from `spreadAngle`

### Requirement: PlayerHealth TakeDamage reduces health and fires event
`TakeDamage(amount)` SHALL reduce `CurrentHealth.Value` by `amount` (clamped to 0 minimum) and fire the `OnDamaged` event with the damage amount and remaining health.

#### Scenario: Damage reduces health
- **WHEN** `CurrentHealth.Value` is 100
- **WHEN** `TakeDamage(30)` is called
- **THEN** `CurrentHealth.Value` SHALL equal 70
- **THEN** `OnDamaged` SHALL be invoked with (30, 70)

### Requirement: PlayerHealth TakeDamage clamps to zero and eliminates
When damage reduces health to 0 or below, `CurrentHealth.Value` SHALL be 0, `IsEliminated` SHALL be true, and `OnDied` SHALL fire.

#### Scenario: Lethal damage eliminates player
- **WHEN** `CurrentHealth.Value` is 20
- **WHEN** `TakeDamage(50)` is called
- **THEN** `CurrentHealth.Value` SHALL equal 0
- **THEN** `IsEliminated` SHALL be true
- **THEN** `OnDied` SHALL be invoked

### Requirement: PlayerHealth Heal increases health clamped to max
`Heal(amount)` SHALL increase `CurrentHealth.Value` by `amount`, clamped to `maxHealth`. It SHALL not heal eliminated players.

#### Scenario: Heal clamps to max health
- **WHEN** `CurrentHealth.Value` is 80 and `maxHealth` is 100
- **WHEN** `Heal(50)` is called
- **THEN** `CurrentHealth.Value` SHALL equal 100

#### Scenario: Heal does not revive eliminated player
- **WHEN** `IsEliminated` is true
- **WHEN** `Heal(50)` is called
- **THEN** `CurrentHealth.Value` SHALL remain 0

### Requirement: PlayerHealth TakeDamage notifies ExitGateway for escape interruption
When a non-lethal `TakeDamage` call occurs, PlayerHealth SHALL call `ExitGateway.Instance.OnKeyHolderDamaged(OwnerClientId)` so the exit can cancel an in-progress escape.

#### Scenario: Damage during escape interrupts via ExitGateway
- **WHEN** a player is escaping through ExitGateway
- **WHEN** `TakeDamage(10)` is called on that player (non-lethal)
- **THEN** `ExitGateway.EscapeProgress.Value` SHALL be reset to 0

### Requirement: PlayerHealth TakeDamage notifies MatchManager on elimination
When `TakeDamage` causes elimination, PlayerHealth SHALL call `MatchManager.Instance.OnPlayerEliminated(OwnerClientId)`.

#### Scenario: Elimination notifies MatchManager
- **WHEN** `TakeDamage` causes `CurrentHealth.Value` to reach 0
- **THEN** `MatchManager.OnPlayerEliminated` SHALL be called with the player's `OwnerClientId`
