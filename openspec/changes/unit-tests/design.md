## Context

The Maze Runner Key Protocol codebase has completed Phase 1 implementation with core systems: loot tables with weighted random rolls, weapon inventory with slot management, player/enemy health with clamping, maze generation via Recursive Backtracker, enemy configs as ScriptableObjects, and a 50-attempt placement guard for loot box spawning. All of these contain pure logic that can be validated without loading a Unity scene.

Currently there are zero automated tests. As the project moves into Phase 2 (procedural maze) and Phase 3 (multiplayer sync), regressions in core logic will become harder to diagnose. Edit Mode tests provide the fastest feedback loop -- they run in the Unity Editor without Play Mode, taking seconds instead of minutes.

Key constraint: many game systems inherit from `NetworkBehaviour`, which requires a running `NetworkManager` to function. Tests must avoid instantiating NetworkBehaviour components. Instead, tests target ScriptableObject data, static/pure methods, and extracted helper logic.

## Goals / Non-Goals

**Goals:**
- Test all pure game logic that does not require scene loading or NetworkManager
- Validate ScriptableObject data contracts (WeaponData, EnemyConfig, LootTable, AmmoPickupData, HealthPackData)
- Verify mathematical correctness of weighted random rolls, health clamping, ammo capping, and density scaling
- Confirm maze generation algorithm properties (full connectivity, dead-end detection)
- Ensure 50-attempt placement guard terminates and uses fallback positions
- Extract testable static helpers from NetworkBehaviour classes where the logic is pure
- Establish test infrastructure (assembly definition, folder structure) for future test expansion

**Non-Goals:**
- Play Mode tests (require scene loading, NetworkManager, physics)
- Integration tests across multiple systems (covered by a separate `integration-tests` change)
- Testing MonoBehaviour lifecycle methods (Awake, Start, Update)
- Testing NetworkVariable sync, RPCs, or host/client authority
- Testing Unity Input System bindings
- UI testing (PlayerUI, WeaponHUD, MatchResultsUI)
- Performance benchmarking

## Decisions

### 1. Edit Mode over Play Mode tests
**Decision:** Use Edit Mode (NUnit) tests exclusively.
**Rationale:** Pure logic tests don't need a running game loop. Edit Mode tests execute 10-50x faster than Play Mode, have no scene dependencies, and can run in CI without a GPU. Play Mode tests will be added in the separate `integration-tests` change for systems that require physics or networking.
**Alternative considered:** Play Mode tests for everything -- rejected because they are slow, brittle (depend on scene setup), and overkill for validating math and data.

### 2. ScriptableObject.CreateInstance for test data
**Decision:** Create WeaponData, EnemyConfig, LootTable, etc. via `ScriptableObject.CreateInstance<T>()` in test setup, then set fields directly.
**Rationale:** Avoids dependency on specific asset files in the project. Tests are self-contained and won't break when assets are renamed or rebalanced. Unity supports `CreateInstance` in Edit Mode without issue.
**Alternative considered:** Loading assets from a test fixtures folder via `AssetDatabase.LoadAssetAtPath` -- rejected because it couples tests to asset paths and requires maintaining duplicate test assets.

### 3. Extract static helpers for NetworkBehaviour logic
**Decision:** Extract pure logic from NetworkBehaviour classes into static helper methods that can be tested directly. Specifically:
- `LootBoxSpawner.CalculateLootCount(int baseLootCount, int additionalPerPlayer, int playerCount, float mazeAreaInCells)` -- static method
- `LootBoxSpawner.IsValidPlacement(Vector3 candidate, List<Vector3> placed, List<Vector3> spawns, Vector3 keyPos, float minSep, float minSpawnDist, float minKeyDist)` -- static method (no Physics.Raycast, that check stays in the caller)
- `PlayerHealth` clamping logic is already simple enough to test via the public API if we mock the NetworkVariable, but since we can't instantiate NetworkBehaviour in Edit Mode, we'll test the math patterns directly (Mathf.Max/Min clamping) via a thin static helper.
**Rationale:** Keeps NetworkBehaviour classes clean while making logic independently testable. The extraction is a single-line delegate to the static method, so existing behavior is preserved.
**Alternative considered:** Using reflection to call private methods -- rejected because it's fragile and doesn't survive refactoring.

### 4. Seeded RNG for deterministic loot table tests
**Decision:** All loot table distribution tests use `System.Random` with a fixed seed, matching the existing `LootTable.Roll(System.Random rng)` API.
**Rationale:** The LootTable already accepts an injectable RNG. Tests can roll 10,000 times with a fixed seed and assert distribution percentages within a tolerance (e.g., +/- 5%). This is both deterministic and statistically meaningful.

### 5. Maze algorithm tested via extracted grid logic
**Decision:** Extract the maze generation grid logic (wall arrays, visited tracking, neighbor finding) into a testable form. The `GenerateMazeGrid` method is private, so tests will either:
  - (a) Make the grid state (`horizontalWalls`, `verticalWalls`, `visited`) accessible via internal + `InternalsVisibleTo`, or
  - (b) Extract a static `MazeGrid` helper class that holds the pure algorithm.
**Preferred approach:** Option (b) -- extract `MazeGrid` as a plain C# class with no Unity dependencies. MazeGenerator delegates to it.
**Rationale:** The Recursive Backtracker algorithm is pure logic (array manipulation + RNG). Testing it through MazeGenerator would require instantiating a NetworkBehaviour. A separate `MazeGrid` class is cleaner and reusable.

## Risks / Trade-offs

- **[Risk] Extracting static helpers changes existing code** -> Mitigation: Each extraction is a one-line delegation. Run the game after extraction to verify no behavioral change. Keep original method signatures identical.
- **[Risk] Weighted distribution tests may be flaky** -> Mitigation: Use fixed seeds and generous tolerance bands (5%). Run 10,000 rolls per test. With these parameters, false failures are statistically negligible.
- **[Risk] MazeGrid extraction may miss Unity-specific edge cases** -> Mitigation: The algorithm is purely array-based. Unity-specific behavior (Instantiate, NavMesh) stays in MazeGenerator. Only the grid logic (walls, visited, neighbors) moves to MazeGrid.
- **[Trade-off] Cannot test health/damage through actual PlayerHealth/EnemyHealth** -> Accepted. NetworkBehaviour requires NetworkManager. We test the clamping math directly and rely on integration tests (separate change) for the full flow.
- **[Trade-off] Placement validation tests skip Physics.Raycast check** -> Accepted. The distance-based checks (separation, spawn distance, key distance) are the testable part. Raycast-based floor detection is a runtime concern tested in integration tests.
