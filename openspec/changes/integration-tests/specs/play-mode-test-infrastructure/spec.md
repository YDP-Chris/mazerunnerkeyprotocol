## ADDED Requirements

### Requirement: Play Mode test assembly definition
The project SHALL have an assembly definition file at `Assets/Tests/PlayMode/PlayModeTests.asmdef` that references the game assemblies, Unity Test Framework, and Netcode for GameObjects. The assembly SHALL be configured for Editor platform only with Test Runner enabled.

#### Scenario: Assembly definition enables test discovery
- **WHEN** Unity Test Runner is opened
- **THEN** all test classes under `Assets/Tests/PlayMode/` SHALL appear in the Play Mode test list

### Requirement: TestSceneHelper bootstraps NetworkManager in host mode
The `TestSceneHelper` class SHALL provide a static method to create a minimal scene with a NetworkManager configured for host mode. It SHALL start the host and wait until `NetworkManager.Singleton.IsListening` is true before returning control to the test.

#### Scenario: Host mode starts successfully
- **WHEN** a test calls `TestSceneHelper.StartHost()`
- **THEN** `NetworkManager.Singleton.IsHost` SHALL be true
- **THEN** `NetworkManager.Singleton.IsListening` SHALL be true

#### Scenario: Host mode cleanup after test
- **WHEN** a test calls `TestSceneHelper.Shutdown()`
- **THEN** the NetworkManager SHALL stop and all spawned NetworkObjects SHALL be destroyed
- **THEN** singleton references (KeyManager.Instance, MatchManager.Instance, ExitGateway.Instance) SHALL be null

### Requirement: TestSceneHelper spawns NetworkObjects with required components
The `TestSceneHelper` SHALL provide methods to spawn GameObjects as NetworkObjects with configurable component sets (e.g., `SpawnPlayer()` adds PlayerHealth, WeaponInventory, PlayerCombat; `SpawnEnemy()` adds EnemyStateMachine, EnemyPerception, EnemyHealth, NavMeshAgent).

#### Scenario: Spawn a player NetworkObject
- **WHEN** a test calls `TestSceneHelper.SpawnPlayer(clientId)`
- **THEN** a NetworkObject SHALL be spawned with PlayerHealth, WeaponInventory, and PlayerCombat components
- **THEN** the object SHALL have `OwnerClientId` set to the provided client ID

#### Scenario: Spawn an enemy NetworkObject with NavMeshAgent
- **WHEN** a test calls `TestSceneHelper.SpawnEnemy(config, waypoints)`
- **THEN** a NetworkObject SHALL be spawned with EnemyStateMachine, EnemyPerception, EnemyHealth, and NavMeshAgent components
- **THEN** the EnemyStateMachine SHALL have its config and patrol waypoints set

### Requirement: TestSceneHelper provides a runtime NavMesh surface
The `TestSceneHelper` SHALL create a flat plane with a NavMeshSurface component and bake a NavMesh at runtime so that NavMeshAgent-based tests can pathfind.

#### Scenario: NavMesh is available for enemy pathfinding
- **WHEN** `TestSceneHelper.CreateNavMeshSurface()` is called
- **THEN** a NavMesh SHALL be baked on a flat plane
- **THEN** `NavMesh.SamplePosition` at the center of the plane SHALL return true
