## ADDED Requirements

### Requirement: Weighted random roll distribution
Tests SHALL verify that `LootTable.Roll()` returns items proportional to their configured weights over a large sample.

#### Scenario: Even weights produce even distribution
- **WHEN** a LootTable has 3 entries each with weight 10 and Roll is called 10,000 times with a fixed seed
- **THEN** each entry SHALL appear between 28% and 38% of the time (33% +/- 5%)

#### Scenario: Skewed weights favor heavy entries
- **WHEN** a LootTable has entries with weights [80, 10, 10] and Roll is called 10,000 times with a fixed seed
- **THEN** the weight-80 entry SHALL appear between 75% and 85% of the time

#### Scenario: Single-entry table always returns that entry
- **WHEN** a LootTable has exactly one entry
- **THEN** Roll SHALL always return that entry regardless of RNG seed

### Requirement: Zero and negative weight handling
Tests SHALL verify LootTable behavior with edge-case weight values.

#### Scenario: All weights zero
- **WHEN** all entries have weight 0 (totalWeight <= 0)
- **THEN** Roll SHALL return the first entry (entries[0]) without throwing an exception

#### Scenario: Mixed zero and positive weights
- **WHEN** some entries have weight 0 and others have positive weights
- **THEN** zero-weight entries SHALL never be returned by Roll

### Requirement: Roll determinism with seeded RNG
Tests SHALL verify that the same seed produces the same roll sequence.

#### Scenario: Same seed same results
- **WHEN** Roll is called N times with `System.Random(42)` and then N times again with a fresh `System.Random(42)`
- **THEN** both sequences SHALL produce identical results
