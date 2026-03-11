## ADDED Requirements

### Requirement: PlayerCombat Script Setup

The `PlayerCombat` script SHALL inherit from `NetworkBehaviour`. It SHALL handle weapon firing, hit detection, and damage dealing. The script SHALL only process input when `IsOwner` is true. The script SHALL not process input when the player is eliminated.

#### Scenario: NetworkBehaviour base class

- **WHEN** the `PlayerCombat` script is inspected
- **THEN** it SHALL inherit from `Unity.Netcode.NetworkBehaviour`

#### Scenario: Owner-only input

- **WHEN** `IsOwner` is false for a player instance
- **THEN** the `PlayerCombat` script SHALL not read fire input or perform shooting logic

#### Scenario: Eliminated player cannot fire

- **WHEN** the player is eliminated (health is zero)
- **THEN** pressing the Fire button SHALL have no effect

---

### Requirement: Starting Pistol

The player SHALL spawn with a starting pistol. The pistol SHALL have unlimited ammo (no ammo counter, no reload mechanic). The pistol SHALL deal low damage (default 15 per hit, configurable via serialized field). The pistol SHALL have a fire rate limit (default 0.3 seconds between shots) to prevent spam clicking.

#### Scenario: Pistol available at spawn

- **WHEN** the player spawns at the start of a match
- **THEN** the player SHALL have the starting pistol equipped and ready to fire

#### Scenario: Unlimited ammo

- **WHEN** the player fires the pistol 100 times
- **THEN** the pistol SHALL continue to fire without running out of ammo or requiring a reload

#### Scenario: Pistol damage

- **WHEN** a pistol shot hits a damageable target
- **THEN** the target SHALL receive 15 damage (or the configured damage value)

#### Scenario: Fire rate limiting

- **WHEN** the player clicks the Fire button twice within 0.3 seconds
- **THEN** only one shot SHALL be fired; the second click SHALL be ignored until the cooldown expires

#### Scenario: Fire rate allows sequential shots

- **WHEN** the player clicks the Fire button, waits 0.3 seconds, then clicks again
- **THEN** both shots SHALL fire

---

### Requirement: Hitscan Shooting

The pistol SHALL use raycasting (hitscan) for hit detection. The ray SHALL originate from the main camera's center (screen center) and project forward into the scene. The ray SHALL have a maximum range (default 50 units, configurable). The raycast SHALL use a `LayerMask` to filter which objects can be hit.

#### Scenario: Raycast origin

- **WHEN** the player fires the pistol
- **THEN** a raycast SHALL be cast from the camera's position in the camera's forward direction

#### Scenario: Hit detection on target

- **WHEN** the raycast hits a GameObject on the damageable layer within 50 units
- **THEN** a hit SHALL be registered and damage SHALL be applied to the target

#### Scenario: Miss beyond range

- **WHEN** the raycast does not hit any damageable object within 50 units
- **THEN** no damage SHALL be applied and no hit SHALL be registered

#### Scenario: Wall blocks shot

- **WHEN** a maze wall is between the player's camera and a target
- **THEN** the raycast SHALL hit the wall and NOT pass through to the target behind it

#### Scenario: LayerMask filtering

- **WHEN** the raycast hits a non-damageable object (e.g., floor, decoration)
- **THEN** no damage SHALL be applied to that object

---

### Requirement: Damage Application via Server RPC

When a hit is detected by the local (owning) client, the client SHALL request damage application via a ServerRpc. The ServerRpc SHALL validate the hit and call `TakeDamage` on the target's `PlayerHealth` component. This pattern establishes the server-authoritative damage model needed for multiplayer.

#### Scenario: Client detects hit

- **WHEN** the owning client's raycast hits a damageable target
- **THEN** the client SHALL call a ServerRpc with the target's network identity and damage amount

#### Scenario: Server applies damage

- **WHEN** the ServerRpc is received on the server/host
- **THEN** the server SHALL call `TakeDamage` on the identified target's `PlayerHealth` component

#### Scenario: Single-player host acts as both

- **WHEN** in single-player mode (player is both host and client)
- **THEN** the ServerRpc pattern SHALL still function correctly with the player acting as both caller and handler

---

### Requirement: Muzzle Flash Visual Feedback

When the pistol fires, a brief visual effect SHALL indicate the shot. At minimum, a muzzle flash (particle effect or sprite flash) SHALL appear at the weapon's muzzle point for a short duration (0.05-0.1 seconds). The muzzle point SHALL be defined by a Transform reference on the player prefab.

#### Scenario: Muzzle flash on fire

- **WHEN** the player fires the pistol
- **THEN** a muzzle flash effect SHALL appear at the designated muzzle Transform position

#### Scenario: Muzzle flash duration

- **WHEN** the muzzle flash appears
- **THEN** it SHALL be visible for no longer than 0.1 seconds before deactivating

#### Scenario: Muzzle flash per shot

- **WHEN** the player fires multiple shots in sequence
- **THEN** each shot SHALL produce its own muzzle flash

---

### Requirement: Hit Impact Feedback

When a raycast hit is detected, a visual indicator SHALL appear at the hit point. For hits on damageable targets, a distinct hit marker effect SHALL appear. For hits on walls or non-damageable surfaces, a generic impact effect (e.g., spark or dust puff) SHALL appear. Impact effects SHALL be spawned at the raycast hit point with rotation aligned to the hit surface normal.

#### Scenario: Hit marker on damageable target

- **WHEN** the raycast hits a damageable target
- **THEN** a hit marker effect SHALL appear at the impact point

#### Scenario: Wall impact effect

- **WHEN** the raycast hits a maze wall
- **THEN** a generic impact effect SHALL appear at the hit point on the wall surface

#### Scenario: Impact effect alignment

- **WHEN** an impact effect is spawned
- **THEN** it SHALL be rotated to align with the surface normal of the hit point

#### Scenario: Impact effect cleanup

- **WHEN** an impact effect is spawned
- **THEN** it SHALL be destroyed or returned to a pool after a configurable duration (default 2 seconds)

---

### Requirement: Shoot Audio Feedback

When the pistol fires, an audio clip SHALL play. The audio source SHALL be attached to the player or weapon GameObject. The audio clip SHALL be a placeholder sound in Phase 1 but the system SHALL support swapping clips per weapon type for future weapons.

#### Scenario: Shot sound plays

- **WHEN** the player fires the pistol
- **THEN** an AudioSource SHALL play the pistol fire audio clip

#### Scenario: Rapid fire audio

- **WHEN** the player fires multiple shots in sequence at the fire rate limit
- **THEN** each shot SHALL play the audio clip independently without cutting off the previous one (use `PlayOneShot`)

#### Scenario: Audio does not play when eliminated

- **WHEN** an eliminated player somehow triggers fire logic
- **THEN** no audio SHALL play
