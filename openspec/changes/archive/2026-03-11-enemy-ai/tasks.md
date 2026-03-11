## 1. Enemy Configuration Data

- [x] 1.1 Create `EnemyConfig` ScriptableObject with fields: health (float), patrol speed, alert speed, chase speed, attack speed, attack range, attack damage, attack interval, sight range, sight angle, sound detection radius, search duration (float, default 10s), memory duration (float, default 5s), waypoint arrival threshold (float, default 0.5)
- [x] 1.2 Create a Grunt `EnemyConfig` asset with low health, medium damage, standard sight range/angle, and patrol-based movement speeds
- [x] 1.3 Create a Guard `EnemyConfig` asset with higher health than Grunt, same or wider sight range, and a flag or field indicating stationary behavior (no patrol route)
- [x] 1.4 Add a `patrolBehavior` enum field to `EnemyConfig` with values `Patrol` and `Stationary` to distinguish Grunt vs Guard idle behavior without subclassing

## 2. EnemyPerception Component

- [x] 2.1 Create `EnemyPerception` class extending `NetworkBehaviour` with references to `EnemyConfig` for sight range, sight angle, and memory duration
- [x] 2.2 Implement cone-based line-of-sight detection: check distance, then angle from forward direction, then raycast against wall layer mask. Return the closest visible player (or null)
- [x] 2.3 Implement wall occlusion raycast helper method using a serialized `LayerMask` field for the wall layer. Raycast from enemy eye position (configurable offset) to target position
- [x] 2.4 Implement last-known position memory: store `Vector3` last-known position, a `float` memory timer, and a `bool` hasMemory flag. Update position on sight/sound detection; clear after configured memory duration with no new detections
- [x] 2.5 Implement perception update throttling: accept a throttle interval parameter, track elapsed time since last sight check, skip checks when interval has not elapsed. CHASE/ATTACK pass 0 (every frame), PATROL passes 0.3-0.5s
- [x] 2.6 Implement `OnSoundEvent(Vector3 origin, float radius, SoundType type)` method: check distance against radius, raycast for wall occlusion, update last-known position if sound is valid and no player currently visible. Process immediately regardless of throttle
- [x] 2.7 Add `IsServer` guard in Update so perception logic only runs on host. Clients should not execute sight checks or process sound events
- [x] 2.8 Expose a public `GetVisiblePlayer()` method and a public `GetLastKnownPosition(out Vector3 position)` method for the state machine to query each frame/tick

## 3. Sound Event Broadcast System

- [x] 3.1 Create a `SoundType` enum with values `Gunshot` and `Footstep`
- [x] 3.2 Create a static `SoundEventSystem` class (or singleton MonoBehaviour) with a `BroadcastSound(Vector3 origin, float radius, SoundType type)` method and a C# event/delegate that enemies subscribe to
- [x] 3.3 Subscribe each `EnemyPerception` instance to `SoundEventSystem` on `OnNetworkSpawn` (host only) and unsubscribe on `OnNetworkDespawn`
- [x] 3.4 Integrate gunshot sound emission: call `SoundEventSystem.BroadcastSound()` from the weapon fire logic on the host with configurable radius (default 15-20 tiles)
- [x] 3.5 Integrate footstep sound emission: call `SoundEventSystem.BroadcastSound()` from player movement logic on the host when velocity exceeds a configurable threshold, at a configurable interval, with radius 3-5 tiles

## 4. EnemyStateMachine Component

- [x] 4.1 Create `EnemyState` enum with values: `PATROL`, `INVESTIGATE`, `CHASE`, `ATTACK`, `SEARCH`
- [x] 4.2 Create `EnemyStateMachine` class extending `NetworkBehaviour` with a `NetworkVariable<EnemyState>` for syncing current state to clients
- [x] 4.3 Add references to `EnemyPerception`, `NavMeshAgent`, and `EnemyConfig` via `[SerializeField]` or `GetComponent` in `OnNetworkSpawn`
- [x] 4.4 Implement `IsServer` guard in `Update` so state transition logic and behavior updates only execute on the host
- [x] 4.5 Implement state entry/exit pattern: when transitioning states, call `ExitState(oldState)` then `EnterState(newState)`. On entry, set NavMeshAgent speed from `EnemyConfig` per-state speed values
- [x] 4.6 Wire up `NetworkVariable<EnemyState>.OnValueChanged` callback so clients can react to state changes for animations and visual indicators

## 5. PATROL State

- [x] 5.1 Implement PATROL behavior: move toward current patrol waypoint using `NavMeshAgent.SetDestination()` at configured patrol speed
- [x] 5.2 Implement waypoint cycling: when within arrival threshold of current waypoint, advance index to next waypoint. Wrap to first waypoint after reaching the last
- [x] 5.3 Implement Guard stationary variant: when `EnemyConfig.patrolBehavior == Stationary`, PATROL state keeps enemy idle at spawn position with no waypoint movement
- [x] 5.4 Add PATROL exit transitions: if `EnemyPerception.GetVisiblePlayer()` returns a player, transition to CHASE. If `EnemyPerception` reports a sound event, transition to INVESTIGATE with the sound origin as target

## 6. INVESTIGATE State

- [x] 6.1 Implement INVESTIGATE entry: store investigation target position, set NavMeshAgent destination to that position, set agent speed to alert speed
- [x] 6.2 Implement INVESTIGATE behavior: navigate toward investigation target each frame
- [x] 6.3 Add INVESTIGATE-to-CHASE transition: if `EnemyPerception.GetVisiblePlayer()` returns a player during investigation, transition to CHASE with that player as target
- [x] 6.4 Add INVESTIGATE-to-PATROL transition: when enemy reaches investigation target (within arrival threshold) and no player is visible, transition back to PATROL
- [x] 6.5 Implement investigation target update: if a new sound event is received while investigating and the new sound origin is closer than the current target, update the investigation target position

## 7. CHASE State

- [x] 7.1 Implement CHASE entry: store target player reference, set agent speed to chase speed
- [x] 7.2 Implement CHASE behavior: continuously update `NavMeshAgent.SetDestination()` to target player's current position each frame
- [x] 7.3 Add CHASE-to-ATTACK transition: when distance to target player is within configured attack range AND line-of-sight is confirmed, transition to ATTACK
- [x] 7.4 Implement line-of-sight loss tracking: start a 5-second timer when LOS is lost; continue navigating to last-known position. If LOS regained within 5s, reset timer and stay in CHASE
- [x] 7.5 Add CHASE-to-SEARCH transition: when LOS loss timer exceeds 5 seconds, transition to SEARCH with last-known position stored
- [x] 7.6 Add CHASE-to-PATROL transition: if target player is eliminated (health zero or object destroyed), transition to PATROL

## 8. ATTACK State

- [x] 8.1 Implement ATTACK entry: set agent speed to attack movement speed, initialize attack cooldown timer
- [x] 8.2 Implement ATTACK behavior: deal configured damage to target player at configured attack interval. Rotate enemy to face target player continuously
- [x] 8.3 Implement damage application: reduce target player's health `NetworkVariable` on the host by configured damage amount per attack tick
- [x] 8.4 Add ATTACK-to-CHASE transition: when target player moves out of attack range OR line-of-sight is lost, transition to CHASE
- [x] 8.5 Add ATTACK-to-PATROL transition: when target player is eliminated (health reaches zero), transition to PATROL and resume patrol route

## 9. SEARCH State

- [x] 9.1 Implement SEARCH entry: read last-known position from `EnemyPerception`, set agent destination to that position, set speed to alert speed, start search timer
- [x] 9.2 Implement SEARCH behavior: navigate to last-known position. Upon arrival, investigate up to 3 nearby patrol waypoints (within configurable search radius) sequentially
- [x] 9.3 Add SEARCH-to-CHASE transition: if `EnemyPerception.GetVisiblePlayer()` returns a player at any point during search, transition to CHASE
- [x] 9.4 Add SEARCH-to-PATROL transition: when search timer exceeds configured search duration (default 10s) without finding a player, transition to PATROL. Resume from nearest waypoint
- [x] 9.5 Implement Guard search return: when Guard's search expires, return to original spawn position near key spawn instead of resuming a patrol route

## 10. Enemy Health and Elimination

- [x] 10.1 Create `EnemyHealth` class extending `NetworkBehaviour` with a `NetworkVariable<float>` for health, initialized from `EnemyConfig.health` on spawn
- [x] 10.2 Implement `TakeDamage(float amount)` method that only executes on host (`IsServer` guard), decrements health NetworkVariable
- [x] 10.3 Implement elimination: when health reaches zero on host, deactivate the enemy GameObject (or despawn the NetworkObject). Enemy does not respawn during the match
- [x] 10.4 Add `NetworkVariable<float>.OnValueChanged` callback on clients for health bar UI updates and death visual effects
- [x] 10.5 Notify the `EnemyStateMachine` of elimination so it can clean up state (stop NavMeshAgent, cancel any pending transitions)

## 11. NavMesh Runtime Bake

- [ ] 11.1 Add a `NavMeshSurface` component to the maze floor parent object. Configure agent radius and height to match enemy dimensions so corridors (3-4 units wide) are fully walkable
- [ ] 11.2 Implement runtime NavMesh bake call after maze geometry is fully placed: invoke `NavMeshSurface.BuildNavMesh()` at the end of the maze generation pipeline (after step 5, before step 6)
- [x] 11.3 Ensure enemy spawning and waypoint assignment are deferred until NavMesh bake completes. Use a callback, coroutine, or async pattern to gate enemy instantiation on bake completion
- [x] 11.4 Validate NavMesh coverage: after bake, spot-check that representative floor positions return true from `NavMesh.SamplePosition()`. Log warnings for uncovered areas

## 12. Patrol Waypoint Generation

- [x] 12.1 Implement waypoint generation in the maze generation pipeline (step 6): iterate over open floor tiles, prefer corridor intersections and junctions over dead-ends, validate each with `NavMesh.SamplePosition()`
- [x] 12.2 Create patrol routes as ordered lists of 4-8 waypoints. Distribute routes across different maze regions to ensure coverage
- [x] 12.3 Assign one patrol route to each Grunt at spawn time. Guards receive no patrol route
- [x] 12.4 Store waypoints as `List<Vector3>` on the enemy or in a shared waypoint data structure accessible by the state machine

## 13. Enemy Spawning

- [x] 13.1 Implement Grunt spawn placement: distribute Grunts across the maze at valid NavMesh positions with minimum spacing between them to avoid clustering. Use `NavMesh.SamplePosition()` to validate each position
- [x] 13.2 Implement Guard spawn placement: place Guards within a configurable radius of the key spawn position at valid NavMesh positions
- [x] 13.3 Implement spawn position fallback: if a calculated spawn position is not on the NavMesh, sample the nearest valid position within a search radius. If no valid position is found, skip spawning that enemy (no infinite loop)
- [x] 13.4 Instantiate enemies as NetworkObjects on the host. Attach `EnemyStateMachine`, `EnemyPerception`, `EnemyHealth`, `NavMeshAgent`, and `NetworkTransform` components via prefab
- [x] 13.5 Disable `NavMeshAgent` on clients in `OnNetworkSpawn` when `IsServer` is false. Only the host drives NavMesh Agent movement

## 14. Enemy Prefabs and NetworkObject Setup

- [x] 14.1 Create a Grunt enemy prefab with: `NetworkObject`, `NetworkTransform`, `NavMeshAgent`, `EnemyStateMachine`, `EnemyPerception`, `EnemyHealth`, a capsule or placeholder mesh, and a collider
- [x] 14.2 Create a Guard enemy prefab with the same components as Grunt but referencing the Guard `EnemyConfig` asset
- [x] 14.3 Configure `NavMeshAgent` on prefabs: set appropriate agent radius (small enough for two enemies to pass in a 3-4 unit corridor), obstacle avoidance priority, and auto-braking
- [x] 14.4 Configure `NetworkTransform` on prefabs for position and rotation sync with interpolation enabled
- [x] 14.5 Register both enemy prefabs in the `NetworkManager`'s network prefab list so they can be spawned over the network

## 15. Stuck Detection and Recovery

- [x] 15.1 Implement stuck detection in `EnemyStateMachine`: track enemy position over time. If the enemy has not moved more than a configurable threshold (e.g., 0.1 units) toward its destination for a configurable timeout (e.g., 3 seconds), flag as stuck
- [x] 15.2 Implement stuck recovery: when stuck is detected, call `NavMeshAgent.ResetPath()` and recalculate path. If still stuck after a second attempt, set destination to the nearest patrol waypoint and transition to PATROL
- [x] 15.3 Log a warning when stuck recovery triggers, including enemy position and intended destination, for debugging maze geometry issues
