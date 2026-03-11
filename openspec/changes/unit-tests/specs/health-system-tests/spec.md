## ADDED Requirements

### Requirement: Player health clamping
Tests SHALL verify that player health is clamped between 0 and maxHealth.

#### Scenario: Damage does not go below zero
- **WHEN** TakeDamage is called with an amount greater than current health
- **THEN** health SHALL be set to 0, not a negative value

#### Scenario: Heal does not exceed maxHealth
- **WHEN** Heal is called with an amount that would push health above maxHealth
- **THEN** health SHALL be set to maxHealth, not above it

#### Scenario: Damage at exactly current health reaches zero
- **WHEN** TakeDamage is called with an amount equal to current health
- **THEN** health SHALL be set to exactly 0

### Requirement: Damage rejection
Tests SHALL verify that invalid damage values are ignored.

#### Scenario: Zero damage is ignored
- **WHEN** TakeDamage is called with amount 0
- **THEN** health SHALL not change

#### Scenario: Negative damage is ignored
- **WHEN** TakeDamage is called with a negative amount
- **THEN** health SHALL not change

#### Scenario: Damage after elimination is ignored
- **WHEN** TakeDamage is called after the entity is already eliminated (health == 0)
- **THEN** health SHALL remain 0 and no error SHALL be thrown

### Requirement: Heal rejection
Tests SHALL verify that invalid heal values are ignored.

#### Scenario: Zero heal is ignored
- **WHEN** Heal is called with amount 0
- **THEN** health SHALL not change

#### Scenario: Negative heal is ignored
- **WHEN** Heal is called with a negative amount
- **THEN** health SHALL not change

### Requirement: Elimination flag
Tests SHALL verify that IsEliminated is set correctly on death.

#### Scenario: IsEliminated set on death
- **WHEN** health reaches 0 via TakeDamage
- **THEN** IsEliminated SHALL be true

#### Scenario: IsEliminated false while alive
- **WHEN** health is above 0
- **THEN** IsEliminated SHALL be false

### Requirement: Enemy health float precision
Tests SHALL verify that EnemyHealth handles float-based damage correctly.

#### Scenario: Fractional damage accumulates correctly
- **WHEN** TakeDamage is called multiple times with fractional values (e.g., 7.5f three times on 50 HP enemy)
- **THEN** health SHALL reflect the correct accumulated damage (50 - 22.5 = 27.5)

#### Scenario: Enemy eliminated at exactly zero
- **WHEN** damage brings enemy health to exactly 0.0f
- **THEN** IsEliminated SHALL be true
