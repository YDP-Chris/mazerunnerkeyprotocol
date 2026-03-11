## Why

Phase 3's exit criteria is "two players move in same maze." The lobby-connection change gets players connected, but player movement must actually be synchronized — each player needs to see the other player's position, rotation, and basic visual state in real time. The current PlayerMovement uses a CharacterController locally with no NetworkTransform, so remote players are invisible/stationary.

## What Changes

- Add NetworkTransform to the Player prefab for position and rotation sync
- Ensure PlayerMovement works correctly in a multi-player context: owner processes input, non-owners receive transform updates
- Add a visible player model/capsule so players can see each other (currently first-person only)
- Sync player look direction (yaw rotation) so remote players face the correct direction
- Handle Network Transform interpolation settings for smooth remote player movement
- Add player nameplate or indicator above remote players for identification

## Capabilities

### New Capabilities
- `transform-sync`: NetworkTransform configuration for smooth position/rotation replication of player objects across host and clients
- `remote-player-visuals`: Visual representation of remote players (model, nameplate) so connected players can see each other in the maze

### Modified Capabilities
- None — PlayerMovement and PlayerCameraController already have owner-only guards. This change adds the network layer that makes those guards meaningful.

## Impact

- **Prefabs**: Player prefab gets NetworkTransform component, visible body mesh (capsule or placeholder model), nameplate UI
- **Scripts**: Minor adjustments to PlayerMovement to ensure CharacterController and NetworkTransform coexist correctly. New RemotePlayerVisuals script for nameplate
- **Performance**: NetworkTransform adds bandwidth per player (~50-100 bytes/tick at 30Hz). Negligible for 2-8 players on LAN
- **Dependencies**: No new packages
