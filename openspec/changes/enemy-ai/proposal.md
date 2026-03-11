## Why

Maze-dwelling NPC enemies add danger, slow exploration, and create tactical pressure. They make the maze feel alive and dangerous even before encountering other players. The AI must be a proper 5-state machine from the start (not simple patrol/chase conditionals) to support the complexity needed for the Grunt, Guard, and future Hunter enemy types.

## What Changes

- Implement a 5-state machine: PATROL, INVESTIGATE, CHASE, ATTACK, SEARCH
- Add cone-based line-of-sight perception with wall obstruction
- Add radius-based sound perception (gunshots 15-20 tiles, footsteps 3-5 tiles)
- Implement 5-second memory of last-known player position in SEARCH state
- Use Unity NavMesh Agent for all pathfinding with obstacle avoidance
- Create Grunt enemy type (basic patrol, low health, medium damage) for MVP
- Create Guard enemy type (stationary near key spawn, higher health) for MVP
- Set up patrol route waypoint system
- Enemy AI runs on host only with state synced to clients (`NetworkBehaviour`)

## Capabilities

### New Capabilities
- `enemy-state-machine`: 5-state AI state machine (PATROL, INVESTIGATE, CHASE, ATTACK, SEARCH) with configurable transition parameters
- `enemy-perception`: Cone-based sight and radius-based sound detection, blocked by maze walls, with 5-second position memory
- `enemy-types`: Grunt (patrol, low health) and Guard (stationary near key, higher health) enemy configurations
- `enemy-navigation`: NavMesh Agent-based pathfinding with patrol waypoint routes and obstacle avoidance

### Modified Capabilities

## Impact

- New scripts in `Assets/Scripts/Enemy/`
- New enemy prefabs in `Assets/Prefabs/Enemy/`
- Requires NavMesh to be baked (dependency on static-maze)
- Requires spawn point system for enemy placement and patrol waypoints
- All scripts use `NetworkBehaviour` for multiplayer readiness
