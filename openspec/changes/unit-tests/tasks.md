## 1. Test Framework Setup

- [x] 1.1 Create `Assets/Tests/EditMode/` folder
- [x] 1.2 Create `Assets/Tests/EditMode/EditModeTests.asmdef` with references to game assembly, `com.unity.test-framework`, and Editor platform only
- [x] 1.3 Verify Unity Test Runner discovers the assembly in Edit Mode tab

## 2. Extract Testable Helpers

- [x] 2.1 Extract `MazeGrid` class from `MazeGenerator` — pure C# class with `Generate(int width, int height, int seed)`, returning wall arrays and visited state. No Unity dependencies.
- [x] 2.2 Add `GetDeadEnds()` method to `MazeGrid` that returns list of cells with exactly 1 opening
- [x] 2.3 Add `FloodFill(int startRow, int startCol)` method to `MazeGrid` that returns count of reachable cells
- [x] 2.4 Update `MazeGenerator.GenerateMazeGrid()` to delegate to `MazeGrid` internally
- [x] 2.5 Extract static `LootPlacementHelper.CalculateLootCount(int baseLootCount, int additionalPerPlayer, int playerCount, float mazeAreaInCells)` from `LootBoxSpawner`
- [x] 2.6 Extract static `LootPlacementHelper.IsValidPlacement(Vector3 candidate, List<Vector3> placed, List<Vector3> spawns, Vector3 keyPos, float minSep, float minSpawnDist, float minKeyDist)` from `LootBoxSpawner` (distance checks only, no Physics.Raycast)
- [x] 2.7 Extract static `LootPlacementHelper.BuildFallbackPositions(float minX, float maxX, float minZ, float maxZ)` from `LootBoxSpawner`
- [x] 2.8 Update `LootBoxSpawner` to delegate to `LootPlacementHelper` methods
- [x] 2.9 Extract static `HealthHelper.ClampDamage(int currentHealth, int damage)` and `HealthHelper.ClampHeal(int currentHealth, int maxHealth, int healAmount)` as pure math helpers
- [x] 2.10 Verify game still runs correctly after all extractions (manual smoke test)

## 3. Loot Table Tests

- [x] 3.1 Create `Assets/Tests/EditMode/LootTableTests.cs`
- [x] 3.2 Test: even weights produce even distribution (3 entries, weight 10 each, 10k rolls, 33% +/- 5%)
- [x] 3.3 Test: skewed weights favor heavy entries (weights [80,10,10], 10k rolls, 75-85% for heavy)
- [x] 3.4 Test: single-entry table always returns that entry
- [x] 3.5 Test: all weights zero returns first entry without exception
- [x] 3.6 Test: zero-weight entries never returned when mixed with positive weights
- [x] 3.7 Test: same seed produces identical roll sequences

## 4. Weapon Inventory Tests

- [x] 4.1 Create `Assets/Tests/EditMode/WeaponInventoryTests.cs`
- [x] 4.2 Test: initial state — slot 0 occupied (pistol), slots 1-2 empty
- [x] 4.3 Test: AddWeapon fills first empty slot (slot 1)
- [x] 4.4 Test: AddWeapon fills slot 2 when slot 1 is occupied
- [x] 4.5 Test: duplicate weapon adds ammo instead of consuming slot
- [x] 4.6 Test: duplicate ammo capped at maxAmmo
- [x] 4.7 Test: both slots full — swap replaces equipped loot slot
- [x] 4.8 Test: both slots full with pistol equipped — swaps slot 1
- [x] 4.9 Test: ConsumeAmmo returns true for pistol (always fires)
- [x] 4.10 Test: ConsumeAmmo decrements ammo and returns true for non-pistol with ammo > 0
- [x] 4.11 Test: ConsumeAmmo returns false for non-pistol with ammo == 0
- [x] 4.12 Test: AddAmmo caps at maxAmmo
- [x] 4.13 Test: AddAmmo with no non-pistol weapons stores as reserve
- [x] 4.14 Test: reserve ammo transfers to newly picked up weapon
- [x] 4.15 Test: reserve + ammoCapacity capped at maxAmmo on weapon pickup

## 5. Health System Tests

- [x] 5.1 Create `Assets/Tests/EditMode/HealthHelperTests.cs`
- [x] 5.2 Test: damage does not produce negative health (ClampDamage with amount > current)
- [x] 5.3 Test: damage equal to current health produces exactly 0
- [x] 5.4 Test: heal does not exceed maxHealth (ClampHeal with amount that overshoots)
- [x] 5.5 Test: zero damage returns unchanged health
- [x] 5.6 Test: negative damage returns unchanged health
- [x] 5.7 Test: zero heal returns unchanged health
- [x] 5.8 Test: negative heal returns unchanged health
- [x] 5.9 Test: fractional enemy damage accumulates correctly (float-based helper variant)
- [x] 5.10 Test: elimination flag logic — health reaching 0 means eliminated

## 6. Enemy Config Tests

- [x] 6.1 Create `Assets/Tests/EditMode/EnemyConfigTests.cs`
- [x] 6.2 Test: Grunt config — positive health, positive speeds, Patrol behavior
- [x] 6.3 Test: Guard config — positive health, Stationary behavior, health >= Grunt health
- [x] 6.4 Test: all speeds positive (patrolSpeed, alertSpeed, chaseSpeed, attackSpeed)
- [x] 6.5 Test: chase speed >= patrol speed
- [x] 6.6 Test: perception ranges positive (sightRange, sightAngle, soundDetectionRadius)
- [x] 6.7 Test: timer durations positive (searchDuration, memoryDuration)
- [x] 6.8 Test: combat values positive (attackRange, attackDamage, attackInterval)

## 7. Maze Algorithm Tests

- [x] 7.1 Create `Assets/Tests/EditMode/MazeGridTests.cs`
- [x] 7.2 Test: 5x5 maze — all 25 cells visited
- [x] 7.3 Test: 20x20 maze — all 400 cells visited
- [x] 7.4 Test: 10x15 non-square maze — all 150 cells visited
- [x] 7.5 Test: flood-fill from (0,0) reaches all cells (full connectivity)
- [x] 7.6 Test: connectivity holds for 5 different seeds
- [x] 7.7 Test: dead-end count > 0 in 10x10 maze
- [x] 7.8 Test: each dead-end has exactly 1 opening
- [x] 7.9 Test: same seed produces identical wall arrays
- [x] 7.10 Test: different seeds produce different wall arrays

## 8. Placement Guard Tests

- [x] 8.1 Create `Assets/Tests/EditMode/LootPlacementTests.cs`
- [x] 8.2 Test: CalculateLootCount — 1 player returns baseLootCount (8)
- [x] 8.3 Test: CalculateLootCount — 2 players returns baseLootCount (8)
- [x] 8.4 Test: CalculateLootCount — 4 players returns 14 (8 + 2*3)
- [x] 8.5 Test: CalculateLootCount — 8 players returns 26 (8 + 6*3)
- [x] 8.6 Test: max cap — small maze (100 cells) caps at 8
- [x] 8.7 Test: max cap — large maze (400 cells) does not cap 14
- [x] 8.8 Test: IsValidPlacement rejects position too close to existing loot box
- [x] 8.9 Test: IsValidPlacement rejects position too close to player spawn
- [x] 8.10 Test: IsValidPlacement rejects position too close to key
- [x] 8.11 Test: IsValidPlacement accepts valid position (all distances satisfied)
- [x] 8.12 Test: BuildFallbackPositions returns exactly 4 positions
- [x] 8.13 Test: BuildFallbackPositions positions are in different quadrants
- [x] 8.14 Test: 50-attempt guard — impossible constraints terminate after maxAttempts and use fallback
- [x] 8.15 Test: 50-attempt guard — reasonable constraints find valid position within maxAttempts
