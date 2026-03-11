## ADDED Requirements

### Requirement: Slot occupancy tracking
Tests SHALL verify that WeaponInventory correctly tracks which slots contain weapons.

#### Scenario: Initial state has only pistol
- **WHEN** a WeaponInventory is initialized with a pistol in slot 0
- **THEN** slot 0 SHALL be occupied and slots 1-2 SHALL be empty

#### Scenario: Adding a weapon fills first empty slot
- **WHEN** AddWeapon is called with a new weapon and slot 1 is empty
- **THEN** slot 1 SHALL become occupied with that weapon

#### Scenario: Adding a second weapon fills slot 2
- **WHEN** AddWeapon is called with a new weapon and slot 1 is occupied but slot 2 is empty
- **THEN** slot 2 SHALL become occupied with that weapon

### Requirement: Duplicate weapon handling
Tests SHALL verify that picking up a duplicate weapon adds ammo instead of consuming a slot.

#### Scenario: Duplicate weapon adds ammo
- **WHEN** AddWeapon is called with a weapon whose weaponName matches an existing slot
- **THEN** no new slot SHALL be consumed and the matching slot's ammo SHALL increase by the weapon's ammoCapacity

#### Scenario: Duplicate ammo capped at maxAmmo
- **WHEN** a duplicate weapon is picked up and the existing ammo + ammoCapacity exceeds maxAmmo
- **THEN** the slot's ammo SHALL be capped at maxAmmo

### Requirement: Slot swap when full
Tests SHALL verify that adding a weapon when both loot slots are occupied swaps the equipped weapon.

#### Scenario: Both slots full swaps equipped slot
- **WHEN** AddWeapon is called with both slots 1 and 2 occupied and the player has a loot weapon equipped
- **THEN** the equipped slot SHALL be replaced with the new weapon

#### Scenario: Pistol equipped defaults to slot 1 swap
- **WHEN** AddWeapon is called with both slots full and slot 0 (pistol) is equipped
- **THEN** slot 1 SHALL be replaced with the new weapon (cannot swap pistol)

### Requirement: Ammo consumption
Tests SHALL verify that ConsumeAmmo correctly decrements ammo and returns the appropriate boolean.

#### Scenario: Pistol always fires
- **WHEN** ConsumeAmmo is called while slot 0 (pistol) is equipped
- **THEN** it SHALL return true without decrementing any ammo counter

#### Scenario: Non-pistol consumes one round
- **WHEN** ConsumeAmmo is called while a loot weapon with ammo > 0 is equipped
- **THEN** ammo SHALL decrease by 1 and the method SHALL return true

#### Scenario: Empty weapon cannot fire
- **WHEN** ConsumeAmmo is called while a loot weapon with ammo == 0 is equipped
- **THEN** the method SHALL return false and ammo SHALL remain 0

### Requirement: Ammo capping
Tests SHALL verify ammo is capped at the weapon's maxAmmo value.

#### Scenario: AddAmmo caps at maxAmmo
- **WHEN** AddAmmo is called and current ammo + amount exceeds maxAmmo
- **THEN** ammo SHALL be set to maxAmmo, not current + amount

#### Scenario: Infinite maxAmmo weapon has no cap
- **WHEN** a weapon has maxAmmo == -1 (pistol)
- **THEN** ammo additions SHALL not be capped

### Requirement: Reserve ammo storage
Tests SHALL verify that ammo picked up without a non-pistol weapon is stored as reserve.

#### Scenario: Ammo stored as reserve when only pistol
- **WHEN** AddAmmo is called and only slot 0 (pistol) is occupied
- **THEN** the amount SHALL be stored as reserve ammo

#### Scenario: Reserve transfers on weapon pickup
- **WHEN** a weapon is picked up and reserve ammo > 0
- **THEN** the new weapon's starting ammo SHALL include the reserve amount (capped at maxAmmo) and reserve SHALL reset to 0
