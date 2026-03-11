## Why

The game needs a 3rd-person player controller as the foundational gameplay element. Without player movement, camera, health, and a starting weapon, no other systems (combat, key pickup, maze navigation) can function. This is the first Phase 1 deliverable.

## What Changes

- Add a 3rd-person character controller with WASD movement and mouse-look camera
- Implement a health bar system with damage/death handling (no respawn per match)
- Add a starting pistol with unlimited ammo and low damage
- Implement cover mechanics using maze walls as natural cover
- Set up player prefab using `NetworkBehaviour` for future multiplayer readiness
- Add basic player UI (health bar, crosshair)

## Capabilities

### New Capabilities
- `player-movement`: 3rd-person character controller with WASD movement, sprint, and mouse-look camera following behind the player
- `player-health`: Health bar system with damage intake, death/elimination handling, and no in-match respawn
- `player-combat`: Starting pistol with unlimited ammo, shooting mechanics, hit detection, and damage dealing
- `player-ui`: HUD elements including health bar display and aiming crosshair

### Modified Capabilities

## Impact

- New scripts in `Assets/Scripts/Player/`
- New player prefab in `Assets/Prefabs/`
- New UI canvas and HUD prefab
- Requires Unity Input System package
- All scripts must use `NetworkBehaviour` base class per CLAUDE.md multiplayer requirement
