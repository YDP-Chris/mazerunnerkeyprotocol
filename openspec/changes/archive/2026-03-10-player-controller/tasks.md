## 1. Project Setup and Input System

- [x] 1.1 Install and verify required packages: Netcode for GameObjects, Cinemachine, Input System. Confirm Active Input Handling is set to "Input System Package (New)" in Player Settings.
- [x] 1.2 Create the Input Actions asset at `Assets/Input/PlayerInputActions.inputactions` with a "Player" action map containing: Move (Vector2, WASD composite), Look (Vector2, Mouse Delta), Sprint (Button, Left Shift), Fire (Button, Left Mouse Button). Enable "Generate C# Class".
- [x] 1.3 Create a NetworkManager GameObject in the test scene with a NetworkManager component configured for host mode (single-player prototype).
- [x] 1.4 Create a Player prefab with CharacterController (height 2.0, radius 0.5, skinWidth 0.08), a capsule mesh as placeholder model, and register it as the NetworkManager's player prefab.

## 2. Player Movement

- [x] 2.1 Create `PlayerMovement.cs` inheriting from `NetworkBehaviour`. Add serialized fields for walkSpeed (5.0), sprintSpeed (8.0), rotationSpeed (10.0), and gravity (9.81). Gate all input processing behind `IsOwner` check.
- [x] 2.2 Implement WASD movement: read Move action as Vector2, compute camera-relative direction on the XZ plane, normalize diagonal input, apply to `CharacterController.Move()` each frame.
- [x] 2.3 Implement sprint: read Sprint action, apply sprintSpeed instead of walkSpeed only when there is positive forward input component (move input Y > 0).
- [x] 2.4 Implement gravity: track vertical velocity, apply -9.81 acceleration when not grounded, apply -1.0 stick force when grounded, include vertical velocity in the `CharacterController.Move()` call.
- [x] 2.5 Implement player rotation: rotate the player transform to face movement direction using `Quaternion.Slerp` with configurable rotation speed. Maintain last facing direction when stationary.
- [x] 2.6 Add a movement state enum (Idle, Moving, Sprinting, Dead) and update it each frame based on input and health state. This will be used by the animator system in Phase 5.
- [x] 2.7 Add elimination check: subscribe to `PlayerHealth.OnDied` event. When eliminated, set state to Dead and skip all input processing and movement.

## 3. Camera System

- [x] 3.1 Add a Cinemachine `CinemachineCamera` to the scene with 3rd Person Follow body. Configure shoulder offset (X=0.5, Y=1.5) and camera distance (4.0). Set the Follow and LookAt targets to the player prefab's transform.
- [x] 3.2 Create `PlayerCameraController.cs` (NetworkBehaviour). Read Look action input and apply horizontal/vertical rotation to a camera orbit target transform. Clamp vertical pitch between -30 and 70 degrees.
- [x] 3.3 Configure Cinemachine collision handling: enable the Cinemachine Deoccluder (or CinemachineCollider) component with appropriate damping so the camera pulls in when maze walls obstruct it.
- [x] 3.4 Implement cursor lock: lock cursor to screen center and hide it on spawn (`Cursor.lockState = CursorLockMode.Locked`). Unlock on elimination or when the game is paused.
- [x] 3.5 Ensure camera is only active for the local owner: in `OnNetworkSpawn`, enable the Cinemachine camera only if `IsOwner` is true, disable it otherwise.

## 4. Player Health

- [x] 4.1 Create `PlayerHealth.cs` inheriting from `NetworkBehaviour`. Declare `NetworkVariable<int>` for current health with server write permission. Add serialized field for maxHealth (default 100).
- [x] 4.2 Initialize health to maxHealth in `OnNetworkSpawn`. Register `OnValueChanged` callback on the health NetworkVariable.
- [x] 4.3 Implement `TakeDamage(int amount)`: guard with `IsServer` check and `IsEliminated` check. Reduce health, clamp to 0. Fire `OnDamaged(int damageAmount, int currentHealth)` event. Skip if amount is 0.
- [x] 4.4 Implement death/elimination: when health reaches 0, set `IsEliminated` to true, fire `OnDied` event, deactivate the GameObject after a 0.5 second delay via coroutine.
- [x] 4.5 Implement `Heal(int amount)`: guard with `IsServer` and `IsEliminated` checks. Add amount to health, clamp to maxHealth. Fire `OnHealed(int healAmount, int currentHealth)` event.
- [x] 4.6 Expose public `IsEliminated` bool property (default false at spawn). Ensure `TakeDamage` and `Heal` both early-return when eliminated.

## 5. Player Combat

- [x] 5.1 Create `PlayerCombat.cs` inheriting from `NetworkBehaviour`. Add serialized fields for damage (15), fireRate (0.3s), range (50), muzzle Transform reference, and LayerMask for damageable targets. Gate input behind `IsOwner` and elimination check.
- [x] 5.2 Implement fire rate limiting: track `nextFireTime` using `Time.time`. On Fire action pressed, only proceed if `Time.time >= nextFireTime`, then set `nextFireTime = Time.time + fireRate`.
- [x] 5.3 Implement hitscan raycast: cast a ray from `Camera.main` screen center (`ViewportPointToRay(0.5, 0.5)`) forward up to the configured range. Check hit against the LayerMask.
- [x] 5.4 Implement ServerRpc damage pattern: create `[ServerRpc] DealDamageServerRpc(ulong targetNetworkObjectId, int damage)`. On hit detection, call this RPC. In the RPC, look up the target NetworkObject and call `TakeDamage` on its `PlayerHealth`.
- [x] 5.5 Implement muzzle flash: add a child GameObject at the muzzle point with a particle system or sprite. On fire, activate it for 0.05-0.1 seconds then deactivate.
- [x] 5.6 Implement hit impact effects: spawn a hit marker prefab at the raycast hit point, rotated to the surface normal. Use a different prefab for damageable vs non-damageable hits. Destroy impact effects after 2 seconds.
- [x] 5.7 Implement shoot audio: add an AudioSource to the player or weapon. On fire, call `PlayOneShot()` with the pistol fire clip. Ensure no audio plays when eliminated.

## 6. Player UI - HUD Setup

- [x] 6.1 Create `PlayerUI.cs` inheriting from `NetworkBehaviour`. In `OnNetworkSpawn`, disable the HUD Canvas if `IsOwner` is false.
- [x] 6.2 Create the HUD Canvas: screen-space overlay, CanvasScaler set to "Scale With Screen Size" at 1920x1080 reference resolution. Add it as a child of the player prefab (or instantiate at spawn).
- [x] 6.3 Implement event subscription: subscribe to `PlayerHealth.OnDamaged`, `PlayerHealth.OnHealed`, and `PlayerHealth.OnDied` in `OnNetworkSpawn`. Unsubscribe in `OnNetworkDespawn`.

## 7. Player UI - Health Bar

- [x] 7.1 Create the health bar UI: a background Image and a fill Image (using Image fill amount or width scaling) anchored to the lower-left of the screen.
- [x] 7.2 Implement health bar fill update: on `OnDamaged` and `OnHealed` events and on `NetworkVariable.OnValueChanged`, set the fill to `currentHealth / maxHealth`.
- [x] 7.3 Implement health bar color coding: set fill color to green when health > 60%, yellow when 30-60%, red when < 30%. Update color on every health change.
- [x] 7.4 Add a TextMeshPro health text element next to the health bar displaying "current / max" format. Update on every health change.

## 8. Player UI - Crosshair and Effects

- [x] 8.1 Create a crosshair Image at the exact center of the HUD Canvas (anchored center, pivot center). Use a simple cross or dot sprite. Set sort order highest among HUD elements.
- [x] 8.2 Implement crosshair hide on death: on `OnDied` event, disable the crosshair Image.
- [x] 8.3 Create a damage flash overlay: a full-screen red-tinted Image (low alpha) behind the crosshair. Default state is transparent/disabled.
- [x] 8.4 Implement damage flash logic: on `OnDamaged` event, set the flash image alpha to a visible value and fade to 0 over 0.2-0.4 seconds using a coroutine. Restart the coroutine if damage is received while already flashing.

## 9. Player UI - Elimination Screen

- [x] 9.1 Create an elimination panel: a full-screen semi-transparent overlay with "ELIMINATED" text (large, centered, TextMeshPro). Default state is disabled.
- [x] 9.2 Implement elimination display: on `OnDied` event, wait 0.5 seconds (matching the death delay), then enable the elimination panel and hide the health bar, health text, and crosshair.

## 10. Integration and Test Scene

- [x] 10.1 Build a static test maze: a simple enclosed area with corridors 3-4 units wide, walls, and a floor. Use primitive cubes or ProBuilder. This is for testing movement and camera behavior in confined spaces.
- [x] 10.2 Assemble the full player prefab: attach PlayerMovement, PlayerHealth, PlayerCombat, PlayerUI, CharacterController, NetworkObject, and all required child objects (muzzle point, HUD canvas). Register as the NetworkManager player prefab.
- [x] 10.3 Add a test enemy: place a simple capsule with a Collider on the damageable layer and a `PlayerHealth` component. Verify shooting it reduces its health and triggers elimination.
- [x] 10.4 Playtest the full loop: start as host, verify movement in corridors, sprint, camera collision with walls, shooting with muzzle flash and impact effects, taking damage (via a debug key or test trigger), health bar updating, and elimination screen on death.
