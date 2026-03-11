## ADDED Requirements

### Requirement: Density scaling formula
Tests SHALL verify the loot box count formula: `baseLootCount + max(0, playerCount - 2) * additionalLootPerPlayer`.

#### Scenario: Solo player gets base count
- **WHEN** playerCount is 1, baseLootCount is 8, additionalLootPerPlayer is 3
- **THEN** loot count SHALL be 8

#### Scenario: Two players get base count
- **WHEN** playerCount is 2
- **THEN** loot count SHALL be 8 (no additional scaling at 2 players)

#### Scenario: Four players get scaled count
- **WHEN** playerCount is 4, baseLootCount is 8, additionalLootPerPlayer is 3
- **THEN** loot count SHALL be 14 (8 + 2*3)

#### Scenario: Eight players get scaled count
- **WHEN** playerCount is 8, baseLootCount is 8, additionalLootPerPlayer is 3
- **THEN** loot count SHALL be 26 (8 + 6*3)

### Requirement: Max cap based on maze area
Tests SHALL verify the area-based maximum cap: `max(8, mazeAreaInCells / 15)`.

#### Scenario: Small maze limits loot count
- **WHEN** maze area is 100 cells (10x10) and calculated loot count is 20
- **THEN** the capped count SHALL be min(20, max(8, 100/15)) = min(20, 8) = 8

#### Scenario: Large maze allows more loot
- **WHEN** maze area is 400 cells (20x20) and calculated loot count is 14
- **THEN** the capped count SHALL be min(14, max(8, 400/15)) = min(14, 26) = 14

### Requirement: 50-attempt placement guard
Tests SHALL verify that the placement loop terminates after maxAttempts and falls back.

#### Scenario: All attempts fail triggers fallback
- **WHEN** placement is attempted with constraints that make all random positions invalid (e.g., minSeparation larger than maze area)
- **THEN** the loop SHALL terminate after exactly maxAttempts iterations and use a fallback position

#### Scenario: Valid position found within attempts
- **WHEN** placement is attempted with reasonable constraints
- **THEN** a valid position SHALL be found within maxAttempts iterations

### Requirement: Placement distance validation (extracted static helper)
Tests SHALL verify the distance-based placement validation logic independent of Physics.Raycast.

#### Scenario: Too close to existing loot box rejected
- **WHEN** a candidate position is within minSeparation of an already-placed loot box
- **THEN** the position SHALL be rejected

#### Scenario: Too close to player spawn rejected
- **WHEN** a candidate position is within minDistFromSpawns of any player spawn
- **THEN** the position SHALL be rejected

#### Scenario: Too close to key rejected
- **WHEN** a candidate position is within minDistFromKey of the key position
- **THEN** the position SHALL be rejected

#### Scenario: Valid position accepted
- **WHEN** a candidate position is farther than all minimum distances from all exclusion zones
- **THEN** the position SHALL be accepted

### Requirement: Fallback positions
Tests SHALL verify that fallback positions are generated correctly.

#### Scenario: Four fallback positions in quadrants
- **WHEN** BuildFallbackPositions is called with known maze bounds
- **THEN** exactly 4 positions SHALL be generated, one per quadrant of the maze
