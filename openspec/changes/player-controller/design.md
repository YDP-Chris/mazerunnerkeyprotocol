## Context

The Maze Runner: Key Protocol game requires a 3rd-person player controller as the foundational gameplay element for Phase 1. This controller must support movement through tight maze corridors, a camera system that works in confined spaces, health management with permanent elimination, and a starting pistol for combat. Every script must use `NetworkBehaviour` as its base class to ensure multiplayer readiness even during the single-player prototype phase.

The player controller is the first Phase 1 deliverable. No other system (key pickup, maze navigation, enemy AI, loot) can function without it. The controller must feel responsive in narrow corridors (3-4 Unity units wide) and support natural cover mechanics using maze walls.

## Goals / Non-Goals

**Goals:**
- Implement a 3rd-person character controller with WASD movement, sprint, and mouse-look camera
- Implement a health system with damage intake, elimination (death), and no in-match respawn
- Implement a starting pistol with unlimited ammo, low damage, and hit detection
- Implement a HUD with health bar display and aiming crosshair
- Use Unity's new Input System for all player input
- Use Cinemachine for the 3rd-person follow camera
- Use `NetworkBehaviour` as the base class for all player scripts
- Ensure the controller works in tight maze corridors (minimum 3-4 Unity units wide)
- Structure all code for future Netcode for GameObjects (NGO) multiplayer integration

**Non-Goals:**
- Networked multiplayer synchronization (Phase 3+)
- Additional weapons beyond the starting pistol (handled by loot-boxes change)
- Key pickup or carrying mechanics (handled by key-exit-system change)
- Enemy interaction or AI aggro (handled by enemy-ai change)
- Player animations or character model (Phase 5 polish)
- Matchmaking, lobby, or session management
- Settings/options menu
- Gamepad or controller input support (PC keyboard/mouse only for Phase 1)

## Decisions

1. **CharacterController over Rigidbody**: Use Unity's `CharacterController` component for player movement. It provides built-in grounding, slope handling, and collision without physics jitter. Maze corridors are tight, and physics-based movement adds unnecessary complexity for a ground-based shooter. Gravity will be applied manually via `CharacterController.Move()`.

2. **Cinemachine 3rd-Person Follow**: Use Cinemachine's 3rd Person Follow camera body with a `CinemachineCamera` component. This handles camera collision with maze walls automatically (preventing the camera from clipping through walls). The camera will orbit behind the player and tighten in narrow corridors.

3. **Unity Input System (Actions asset)**: Define an Input Actions asset (`PlayerInputActions`) with action maps for Player (Move, Look, Sprint, Fire). This decouples input from code and allows rebinding later. Use the generated C# class for type-safe access.

4. **NetworkBehaviour from Day One**: All player scripts inherit from `Unity.Netcode.NetworkBehaviour` instead of `MonoBehaviour`. Health is stored as a `NetworkVariable<int>`. Owner checks (`IsOwner`) gate input processing even in single-player to establish the pattern. This avoids a full rewrite when multiplayer is added in Phase 3.

5. **Hitscan for Pistol**: The starting pistol uses raycasting (hitscan) rather than projectile physics. A ray is cast from the camera center forward, checked against a `LayerMask` for damageable targets. This is simpler, deterministic, and easier to network later.

6. **Single Health Script with Events**: `PlayerHealth` manages HP as a `NetworkVariable` and exposes C# events (`OnDamaged`, `OnDied`). Other systems subscribe to these events rather than polling. Death triggers elimination state -- the player GameObject is deactivated, not destroyed, to preserve network identity.

7. **World-Space Health Bar + Screen-Space Crosshair**: The player's own health bar is rendered as a screen-space UI element on the HUD canvas. The crosshair is a fixed screen-space image at screen center. No world-space health bars on other players in Phase 1 (added with multiplayer).

## Risks / Trade-offs

1. **CharacterController wall sliding in narrow corridors**: `CharacterController` can slide along walls in ways that feel imprecise in tight spaces. Mitigation: tune `skinWidth` and `radius` to match corridor width. Playtest early in the static maze.

2. **Cinemachine camera clipping in maze corners**: Even with collision handling, extreme corner cases (literal corners) may cause the camera to snap or jitter. Mitigation: set Cinemachine's collision damping and use a minimum camera distance to prevent the camera from entering the player model.

3. **NetworkBehaviour overhead in single-player**: Using `NetworkBehaviour` before multiplayer is active adds a dependency on the Netcode for GameObjects package and requires a `NetworkManager` in the scene even for solo play. Mitigation: this is an explicit PRD requirement (Section 5.5) to avoid rewrite risk. The overhead is minimal.

4. **Hitscan vs. projectile for future weapons**: The pistol uses hitscan, but future weapons (shotgun, SMG) may need spread patterns or different fire rates. Mitigation: abstract the firing interface behind a base weapon class or interface so the pistol implementation does not lock in the approach for all weapons.

5. **No animation system in Phase 1**: The player will use a capsule or placeholder model with no animations. This means no animation-driven state (aiming, running, dying) which will need to be retrofitted. Mitigation: use a state enum (`Idle`, `Moving`, `Sprinting`, `Dead`) on the controller that an animator can hook into later.

6. **Input System package version**: Unity 6.3 ships with Input System 1.x. Ensure the package is installed and the Active Input Handling is set to "Input System Package (New)" in Player Settings, or "Both" during transition. Failing to set this will cause silent input failures.
