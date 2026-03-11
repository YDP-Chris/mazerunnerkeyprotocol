## Why

The key-and-exit mechanic is what makes Maze Runner unique — it's the core win condition that differentiates the game from standard deathmatch shooters. Without it, there's no objective-driven gameplay. The key pickup, key holder tracking, exit unlock, and escape animation form the central game loop that all other systems support.

## What Changes

- Implement key spawning at a designated spawn point with glow/pulse visual effect
- Add auto-pickup on player contact
- Broadcast key holder status to all players (directional indicator)
- Implement exit door that is locked until key is collected
- Add exit glow/pulse when unlocked, visible on minimap
- Create 2-3 second interruptible escape animation at exit zone
- Implement win condition: escaping with key ends match
- Handle key-on-death (drop in place as initial implementation)
- All state managed via `NetworkVariable` and RPCs for multiplayer readiness

## Capabilities

### New Capabilities
- `key-mechanics`: Key spawning, visual effects (glow/pulse), proximity visibility, auto-pickup, and key-on-death drop behavior
- `exit-mechanics`: Exit door with locked/unlocked states, visual feedback, and escape zone with interruptible escape animation
- `win-condition`: Match end logic triggered by successful escape, including all-eliminated draw condition
- `key-holder-tracking`: Directional indicator system showing all players where the key holder is

### Modified Capabilities

## Impact

- New scripts in `Assets/Scripts/GameState/`
- New key and exit prefabs in `Assets/Prefabs/`
- New VFX for key glow and exit glow
- Requires a game manager / match state controller
- UI updates for key holder indicator and minimap integration
- All scripts use `NetworkBehaviour` for multiplayer readiness
