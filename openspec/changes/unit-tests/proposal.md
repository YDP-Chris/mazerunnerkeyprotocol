## Why

The codebase has grown to include loot tables, weapon inventory, damage/health systems, maze generation, enemy configs, and placement logic -- all with pure game logic that can be validated without running a scene. Unit tests will catch regressions early, document expected behavior, and provide confidence for future refactoring (especially ahead of multiplayer sync work). Adding tests now, while the systems are fresh and the contracts are clear, is cheaper than retrofitting later.

## What Changes

- Add Edit Mode unit tests covering all pure game logic that can be tested without scene loading:
  - **LootTable**: weighted random roll distribution, zero/negative weight edge cases, single-entry tables
  - **WeaponData**: ScriptableObject field validation (damage > 0, fireRate > 0, ammoCapacity >= 0, pelletCount >= 1)
  - **WeaponInventory**: slot add/swap/duplicate handling, ammo consumption, ammo capping to maxAmmo, reserve ammo storage and transfer, ConsumeAmmo returning true/false, pistol slot immutability
  - **PlayerHealth**: health clamping between 0 and maxHealth, TakeDamage with zero/negative amounts ignored, Heal clamping, IsEliminated flag on death
  - **EnemyHealth**: damage clamping, elimination at zero, float-based health values
  - **EnemyConfig**: ScriptableObject data integrity (positive speeds, ranges, durations)
  - **MazeGenerator algorithm validation**: all cells visited after generation, no isolated regions, dead-end detection correctness
  - **LootBoxSpawner density formula**: baseLootCount + additionalLootPerPlayer scaling, maxCap calculation
  - **50-attempt placement guard**: verify loop terminates and falls back correctly
- Set up Unity Test Framework (NUnit) assembly definition for `Assets/Tests/EditMode/`
- All tests use `ScriptableObject.CreateInstance<T>()` and direct method calls -- no MonoBehaviour instantiation, no scene loading

## Capabilities

### New Capabilities
- `unit-test-framework`: Edit Mode test project setup (assembly definition, NUnit references, folder structure under Assets/Tests/EditMode/)
- `loot-logic-tests`: Tests for LootTable.Roll weighted random distribution and edge cases
- `weapon-inventory-tests`: Tests for WeaponInventory slot management, ammo consumption, capping, reserve storage, and duplicate weapon handling
- `health-system-tests`: Tests for PlayerHealth and EnemyHealth clamping, damage, healing, and elimination logic
- `enemy-config-tests`: Tests for EnemyConfig ScriptableObject data integrity and field validation
- `maze-algorithm-tests`: Tests for MazeGenerator grid algorithm (all cells visited, connectivity, dead-end count)
- `placement-guard-tests`: Tests for 50-attempt placement loop termination and density scaling formula

### Modified Capabilities

## Impact

- **New files**: Assembly definition at `Assets/Tests/EditMode/EditModeTests.asmdef`, test scripts in `Assets/Tests/EditMode/`
- **Dependencies**: Unity Test Framework package (com.unity.test-framework) -- already included by default in Unity 6.3
- **Existing code**: Some methods in WeaponInventory and LootBoxSpawner may need minor refactoring to extract pure logic into static/testable helpers (e.g., density calculation, placement validation without Physics.Raycast). These extractions will be minimal and non-breaking.
- **No runtime impact**: Edit Mode tests do not ship in builds and do not affect game performance
