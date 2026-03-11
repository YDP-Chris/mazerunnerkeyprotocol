## 1. Test Infrastructure Setup

- [ ] 1.1 Create `Assets/Tests/PlayMode/` directory and `PlayModeTests.asmdef` assembly definition referencing game assemblies, Unity Test Framework, Netcode for GameObjects, and Unity.AI.Navigation
- [ ] 1.2 Create `Assets/Tests/PlayMode/Helpers/TestSceneHelper.cs` with `StartHost()` method that creates a NetworkManager GameObject, configures NetworkConfig with a UnityTransport, starts host mode, and waits until `IsListening` is true
- [ ] 1.3 Add `Shutdown()` method to TestSceneHelper that stops the host, destroys all spawned NetworkObjects, clears singleton references (KeyManager.Instance, MatchManager.Instance, ExitGateway.Instance), and destroys the NetworkManager
- [ ] 1.4 Add `SpawnPlayer(ulong clientId)` method to TestSceneHelper that creates a GameObject with NetworkObject, PlayerHealth, WeaponInventory, and PlayerCombat components, then spawns it as a NetworkObject with the given owner
- [ ] 1.5 Add `SpawnEnemy(EnemyConfig config, List<Vector3> waypoints)` method to TestSceneHelper that creates a GameObject with NetworkObject, EnemyStateMachine, EnemyPerception, EnemyHealth, NavMeshAgent, and CapsuleCollider, then spawns it and sets config/waypoints
- [ ] 1.6 Add `CreateNavMeshSurface()` method to TestSceneHelper that creates a flat plane with NavMeshSurface component and bakes a NavMesh at runtime
- [ ] 1.7 Add helper methods: `SpawnKeyManager()`, `SpawnExitGateway()`, `SpawnMatchManager()` for creating game state manager NetworkObjects
- [ ] 1.8 Add `CreateWall(Vector3 position, Vector3 scale, int layer)` helper to create wall colliders for occlusion tests

## 2. Enemy State Machine Tests

- [ ] 2.1 Create `Assets/Tests/PlayMode/Enemy/EnemyStateMachineTests.cs` test fixture with `[OneTimeSetUp]` that calls TestSceneHelper.StartHost and CreateNavMeshSurface, and `[OneTimeTearDown]` that calls Shutdown
- [ ] 2.2 Create a test EnemyConfig ScriptableObject asset or instantiate one at runtime with known values (sightRange, sightAngle, attackRange, patrol/chase/alert speeds, attackInterval, attackDamage, searchDuration)
- [ ] 2.3 Write test: enemy starts in PATROL state after network spawn
- [ ] 2.4 Write test: PATROL transitions to INVESTIGATE when SoundEventSystem broadcasts a gunshot within hearing radius (no wall)
- [ ] 2.5 Write test: PATROL transitions to CHASE when a spawned player is positioned within sight cone and range
- [ ] 2.6 Write test: INVESTIGATE transitions to CHASE when player becomes visible during investigation
- [ ] 2.7 Write test: INVESTIGATE transitions to PATROL when enemy reaches investigation target with no visible player
- [ ] 2.8 Write test: CHASE transitions to ATTACK when player is within attackRange and visible
- [ ] 2.9 Write test: CHASE transitions to SEARCH after 5 seconds without line-of-sight (move player behind wall or out of range, wait 5s)
- [ ] 2.10 Write test: ATTACK deals damage to target PlayerHealth after attackInterval elapses
- [ ] 2.11 Write test: ATTACK transitions to CHASE when target moves beyond attackRange
- [ ] 2.12 Write test: ATTACK transitions to PATROL when target is eliminated (PlayerHealth.IsEliminated == true)
- [ ] 2.13 Write test: SEARCH transitions to CHASE when player becomes visible during search
- [ ] 2.14 Write test: SEARCH transitions to PATROL after searchDuration expires with no player found

## 3. Enemy Perception Tests

- [ ] 3.1 Create `Assets/Tests/PlayMode/Enemy/EnemyPerceptionTests.cs` test fixture with host-mode setup and NavMesh surface
- [ ] 3.2 Write test: player inside sight cone and range is detected by GetVisiblePlayer after UpdatePerception(0)
- [ ] 3.3 Write test: player outside cone angle (but within range) returns null from GetVisiblePlayer
- [ ] 3.4 Write test: player beyond sightRange (but within cone) returns null from GetVisiblePlayer
- [ ] 3.5 Write test: wall collider between enemy and player blocks detection (GetVisiblePlayer returns null)
- [ ] 3.6 Write test: GetLastKnownPosition returns true with stored position after a player was seen then hidden

## 4. Sound Event System Tests

- [ ] 4.1 Create `Assets/Tests/PlayMode/Enemy/SoundEventSystemTests.cs` test fixture
- [ ] 4.2 Write test: BroadcastSound delivers event to a subscribed Action listener with correct origin, radius, and SoundType
- [ ] 4.3 Write test: EnemyPerception ignores sound broadcast when origin is beyond the broadcast radius
- [ ] 4.4 Write test: EnemyPerception ignores sound broadcast when wall blocks path between enemy and sound origin

## 5. Key Manager Tests

- [ ] 5.1 Create `Assets/Tests/PlayMode/GameState/KeyManagerTests.cs` test fixture with host-mode setup and a spawned KeyManager NetworkObject
- [ ] 5.2 Write test: RequestPickupServerRpc sets CurrentKeyState to Held and KeyHolderClientId to requesting client
- [ ] 5.3 Write test: RequestPickupServerRpc is rejected when key is already Held (state and holder unchanged)
- [ ] 5.4 Write test: OnKeyPickedUp event fires with correct client ID on successful pickup
- [ ] 5.5 Write test: DropKey sets CurrentKeyState to Dropped, clears KeyHolderClientId, updates KeyWorldPosition
- [ ] 5.6 Write test: OnKeyDropped event fires with drop position
- [ ] 5.7 Write test: IsKeyHolder returns true for holder client and false for other clients
- [ ] 5.8 Write test: IsKeyHolder returns false when key is Uncollected

## 6. Exit Gateway Tests

- [ ] 6.1 Create `Assets/Tests/PlayMode/GameState/ExitGatewayTests.cs` test fixture with host-mode setup, spawned KeyManager and ExitGateway
- [ ] 6.2 Write test: IsUnlocked becomes true when KeyManager.OnKeyPickedUp fires
- [ ] 6.3 Write test: EscapeProgress increments over time after escape begins (simulate via direct BeginEscape or trigger enter)
- [ ] 6.4 Write test: OnEscapeComplete fires when EscapeProgress reaches 1.0
- [ ] 6.5 Write test: OnKeyHolderDamaged cancels escape and resets EscapeProgress to 0 for the escaping client
- [ ] 6.6 Write test: OnKeyHolderDamaged with a different client ID does not cancel escape

## 7. Match Manager Tests

- [ ] 7.1 Create `Assets/Tests/PlayMode/GameState/MatchManagerTests.cs` test fixture with host-mode setup, spawned MatchManager and ExitGateway
- [ ] 7.2 Write test: OnEscapeComplete triggers CurrentOutcome = Win and WinnerClientId = escaping client
- [ ] 7.3 Write test: OnPlayerEliminated called N times (for N players) triggers CurrentOutcome = Draw
- [ ] 7.4 Write test: OnMatchEnded event fires with (Win, clientId) on escape
- [ ] 7.5 Write test: OnMatchEnded event fires with (Draw, ulong.MaxValue) when all eliminated
- [ ] 7.6 Write test: OnPlayerEliminated for key holder calls KeyManager.DropKey

## 8. Loot Box Tests

- [ ] 8.1 Create `Assets/Tests/PlayMode/Loot/LootBoxTests.cs` test fixture with host-mode setup
- [ ] 8.2 Write test: RequestPickupServerRpc on already-looted box (isAvailable false) has no effect
- [ ] 8.3 Write test: weapon-type LootBox grants weapon to player WeaponInventory on successful pickup
- [ ] 8.4 Write test: HealthPack-type LootBox heals player's PlayerHealth
- [ ] 8.5 Write test: two RequestPickupServerRpc calls in same frame — only first succeeds, second is rejected
- [ ] 8.6 Write test: RequestPickupServerRpc from player beyond interactionRange + 1f is rejected

## 9. Weapon Inventory Tests

- [ ] 9.1 Create `Assets/Tests/PlayMode/Loot/WeaponInventoryTests.cs` test fixture with host-mode setup
- [ ] 9.2 Write test: initial state has pistol in slot 0 with -1 ammo, slots 1-2 empty
- [ ] 9.3 Write test: SwitchToSlot to occupied slot updates equippedSlot.Value
- [ ] 9.4 Write test: SwitchToSlot to empty slot does not change equippedSlot.Value
- [ ] 9.5 Write test: CycleWeapon(1) from slot 0 skips empty slot 1 to occupied slot 2
- [ ] 9.6 Write test: ConsumeAmmo on pistol slot returns true and keeps ammo at -1
- [ ] 9.7 Write test: ConsumeAmmo on non-pistol slot decrements ammo and returns true
- [ ] 9.8 Write test: ConsumeAmmo on empty non-pistol slot returns false
- [ ] 9.9 Write test: AddWeapon with duplicate weaponName adds ammo to existing slot instead of new slot

## 10. Player Combat Tests

- [ ] 10.1 Create `Assets/Tests/PlayMode/Player/PlayerCombatTests.cs` test fixture with host-mode setup
- [ ] 10.2 Write test: single-shot fire mode performs exactly one raycast per fire press (verify via damage dealt to a target or mock)
- [ ] 10.3 Write test: automatic fire mode performs multiple raycasts when fire is held over 2/fireRate duration
- [ ] 10.4 Write test: weapon with pelletCount > 1 fires pelletCount rays per shot (verify via shotgun dealing pelletCount * damage to close target)

## 11. Player Health Tests

- [ ] 11.1 Create `Assets/Tests/PlayMode/Player/PlayerHealthTests.cs` test fixture with host-mode setup
- [ ] 11.2 Write test: TakeDamage(30) on 100 HP player sets CurrentHealth to 70 and fires OnDamaged(30, 70)
- [ ] 11.3 Write test: TakeDamage(50) on 20 HP player sets CurrentHealth to 0, IsEliminated to true, fires OnDied
- [ ] 11.4 Write test: Heal(50) on 80 HP player (max 100) clamps CurrentHealth to 100
- [ ] 11.5 Write test: Heal on eliminated player has no effect (CurrentHealth stays 0)
- [ ] 11.6 Write test: non-lethal TakeDamage calls ExitGateway.OnKeyHolderDamaged for escape interruption
- [ ] 11.7 Write test: lethal TakeDamage calls MatchManager.OnPlayerEliminated with OwnerClientId
