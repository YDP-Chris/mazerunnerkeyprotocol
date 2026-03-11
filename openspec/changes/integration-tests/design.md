## Context

Phase 1 gameplay systems are implemented across Enemy, GameState, Player, and Loot scripts. All game-critical scripts use `NetworkBehaviour` and rely on Netcode for GameObjects (NGO). There are currently no automated tests. The systems have tight interdependencies (e.g., PlayerHealth notifies MatchManager on elimination, which notifies KeyManager to drop the key, which notifies ExitGateway to cancel escape). These cross-system interactions are prime candidates for integration tests.

Unity Test Framework supports Play Mode tests that run in a live Unity runtime with physics, coroutines, and scene loading. NGO supports host-mode testing where a single process acts as both server and client, which is sufficient for validating NetworkBehaviour logic without a separate client process.

## Goals / Non-Goals

**Goals:**
- Establish Play Mode test infrastructure that bootstraps a NetworkManager in host mode for testing NetworkBehaviour components
- Cover all 5-state enemy AI transitions with proper enter/exit verification
- Cover EnemyPerception cone detection, wall occlusion, and SoundEventSystem integration
- Cover the full key lifecycle: pickup, holder tracking, drop on elimination
- Cover ExitGateway escape sequence including damage-based interruption
- Cover MatchManager win and draw outcomes
- Cover LootBox ServerRpc validation including race condition (double-pickup) handling
- Cover WeaponInventory slot management, switching, and ammo consumption
- Cover PlayerCombat fire mode differentiation (single vs automatic, single-ray vs multi-ray)
- Cover PlayerHealth damage, heal clamping, and elimination flow

**Non-Goals:**
- Edit Mode (pure unit) tests for data-only classes (can be added separately)
- Multiplayer tests with separate host and client processes
- Performance/load testing
- UI testing (MatchResultsUI, WeaponHUD, etc.)
- Visual regression testing
- Testing MazeGenerator procedural generation output

## Decisions

### 1. Host-mode testing over mock NetworkManager

Tests will start a real NetworkManager as host (server + client in one process). This exercises actual NetworkVariable sync, ServerRpc dispatch, and ClientRpc callbacks without requiring a second process.

**Alternative considered:** Mocking NetworkBehaviour methods. Rejected because it would miss real NGO initialization (OnNetworkSpawn, NetworkVariable change callbacks) which is where most bugs live.

### 2. Dedicated test scene per test fixture

Each test class will create a minimal scene with only the GameObjects needed for that fixture. A shared `TestSceneHelper` utility will handle:
- Creating and configuring a NetworkManager GameObject
- Starting host mode and waiting for connection
- Spawning NetworkObjects with required components
- Cleaning up after each test (stopping host, destroying objects)

**Alternative considered:** One shared test scene loaded from disk. Rejected because it couples tests to a scene asset and makes it harder to control exactly which components are present.

### 3. Test file organization mirrors source structure

```
Assets/Tests/PlayMode/
  PlayModeTests.asmdef
  Helpers/
    TestSceneHelper.cs
  Enemy/
    EnemyStateMachineTests.cs
    EnemyPerceptionTests.cs
    SoundEventSystemTests.cs
  GameState/
    KeyManagerTests.cs
    ExitGatewayTests.cs
    MatchManagerTests.cs
  Loot/
    LootBoxTests.cs
    WeaponInventoryTests.cs
  Player/
    PlayerCombatTests.cs
    PlayerHealthTests.cs
```

### 4. Use UnityTest (coroutine) over async Test

Play Mode tests that need to wait for physics frames, NetworkVariable propagation, or coroutine completion will use `[UnityTest]` with `yield return null` (wait one frame) or `yield return new WaitForSeconds(...)`. This is the standard Unity Test Framework pattern and avoids async/await compatibility issues with older Unity test runner versions.

### 5. EnemyStateMachine testing via direct method calls and perception injection

The EnemyStateMachine reads from EnemyPerception. Tests will:
- Create a real EnemyStateMachine + EnemyPerception + NavMeshAgent on a NavMesh surface
- Position mock "player" GameObjects within/outside perception range to trigger state transitions
- Assert `CurrentState.Value` after each transition

This tests the full perception-to-state-machine pipeline rather than testing the state machine in isolation.

### 6. LootBox race condition test via double ServerRpc invocation

To test the race condition guard (`if (!isAvailable.Value) return`), the test will call `RequestPickupServerRpc` twice in rapid succession from the same frame. Only the first call should succeed; the second should be rejected because `isAvailable` is already false.

## Risks / Trade-offs

**[Risk] NavMesh required for enemy tests** -- Enemy tests need a baked NavMesh surface. Tests will create a flat plane GameObject with a NavMeshSurface component and bake at runtime before spawning enemies.

**[Risk] Host-mode tests are slower than pure unit tests** -- Each test starts/stops NetworkManager. Mitigated by keeping test scenes minimal and using `[OneTimeSetUp]`/`[OneTimeTearDown]` at the fixture level where possible.

**[Risk] Singleton coupling (KeyManager.Instance, MatchManager.Instance, ExitGateway.Instance)** -- Tests must manage singleton lifecycle carefully. TestSceneHelper will ensure singletons from previous tests are cleaned up before new tests run.

**[Risk] EnemyPerception depends on NetworkManager.Singleton.SpawnManager** -- The CheckSight method iterates SpawnedObjects. Tests must ensure player objects are network-spawned (not just Instantiated) before perception checks.

**[Trade-off] No separate client validation** -- Host-mode tests validate server logic but not client-only code paths (e.g., BeginEscapeClientRpc disabling movement on the local player). Accepted for Phase 1; true multiplayer tests are a Phase 3+ concern.
