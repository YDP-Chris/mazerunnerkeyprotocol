## Context

The Player prefab currently has NetworkObject but no NetworkTransform. PlayerMovement uses a CharacterController for local movement and already guards input processing behind `IsOwner` checks. PlayerCameraController similarly disables on non-owners. However, without NetworkTransform, remote players' positions never update — they stay at their spawn point from other clients' perspectives.

The game uses first-person camera (Cinemachine with CameraDistance=0.1), so the local player doesn't need a visible body, but remote players must have a visible representation.

## Goals / Non-Goals

**Goals:**
- Remote players' positions and rotations replicate smoothly to all clients
- Players can see each other as visible characters in the maze
- Movement feels responsive for the owner (no input delay from networking)
- Basic identification of remote players (nameplate)

**Non-Goals:**
- Player animation (walk/run/idle) — placeholder capsule is sufficient for Phase 3
- Combat sync (damage, weapons visible on other players) — that's Phase 4
- Client-side prediction or lag compensation — LAN latency is negligible
- Character customization or player models

## Decisions

### 1. Owner-authoritative movement with NetworkTransform

**Decision:** Use NGO's `NetworkTransform` component on the Player prefab with owner-authoritative mode. The owner moves via CharacterController; NetworkTransform replicates position/rotation to other clients.

**Why:** CharacterController is already the movement system. Owner-authoritative is the NGO default and simplest model. Server-authoritative would require rewriting movement to run on host, which is unnecessary for Phase 3.

**Alternatives:**
- Server-authoritative with client prediction: more cheat-resistant but significantly more complex. Defer to Phase 5 polish if needed.
- Custom position sync via NetworkVariable: reinventing the wheel when NetworkTransform exists.

### 2. Interpolation for smooth remote movement

**Decision:** Enable NetworkTransform interpolation (default in NGO). Use 30Hz sync rate to balance smoothness and bandwidth.

**Why:** Without interpolation, remote players appear to teleport between sync ticks. NGO's built-in interpolation smooths this automatically.

### 3. Capsule placeholder for player body

**Decision:** Add a capsule mesh renderer to the Player prefab as the visible body. Hide it on the owner (first-person camera would see inside it). Show it on remote clients.

**Why:** The game is first-person for the local player but needs third-person visibility for others. A capsule is the simplest visual that communicates player position and size. Proper character models can replace it later.

**Alternatives:**
- No body (just nameplate): hard to see players, no spatial reference
- Full character model: art dependency, not needed for Phase 3 validation

### 4. Yaw sync only for look direction

**Decision:** Sync the player's Y-axis rotation (yaw) via NetworkTransform. Do NOT sync pitch (vertical look angle) — it's only relevant for the owner's camera.

**Why:** Remote players need to face the direction they're moving/looking for visual coherence. Pitch doesn't affect the capsule's visual orientation and adds unnecessary bandwidth.

### 5. World-space nameplate canvas

**Decision:** Attach a small World Space Canvas above each player with a TextMeshPro label showing "Player {clientId}". Hide on owner. Billboard toward camera.

**Why:** With identical capsules, players need identification. ClientId is available immediately without extra sync. Can be enhanced with actual names later.

## Risks / Trade-offs

- **[Owner-authoritative cheating]** → Acceptable for LAN play. Can add server validation in Phase 5 if needed.
- **[CharacterController + NetworkTransform conflict]** → On non-owner clients, CharacterController must be disabled so NetworkTransform can set position directly. Verified: PlayerMovement already disables input on non-owners, but CharacterController component itself may need to be disabled to avoid interference.
- **[Capsule clipping in first-person]** → Must set capsule's layer to a "LocalPlayerHidden" layer and exclude from owner's camera culling mask, OR simply disable the MeshRenderer on owner.
