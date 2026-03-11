## Why

Loot boxes provide the weapons, ammo, and health packs that make combat viable beyond the starting pistol. They create exploration incentives and risk/reward decisions — do you loot more or rush for the key? Without loot, combat is one-dimensional and the maze has less reason to be explored.

## What Changes

- Implement loot box prefab with visual indicator and interaction
- Add spawn system using max 50 attempts with known-safe fallback position (design constraint from CLAUDE.md)
- Create loot table: shotgun, SMG, rifle, ammo, health packs
- Implement weapon pickup and weapon switching for the player
- Loot boxes are single-use (destroyed after looting)
- Loot density scales with player count
- All loot box state managed via `NetworkVariable` for multiplayer readiness

## Capabilities

### New Capabilities
- `loot-spawning`: Loot box placement system with 50-attempt max guard and safe fallback, density scaling with player count
- `loot-contents`: Loot table with weapons (shotgun, SMG, rifle), ammo, and health packs, with randomized drops
- `weapon-system`: Weapon pickup, switching, and firing mechanics for looted weapons beyond the starting pistol

### Modified Capabilities
- `player-combat`: Extended to support multiple weapon types with different damage, fire rate, and ammo counts

## Impact

- New scripts in `Assets/Scripts/Loot/`
- New loot box and weapon prefabs in `Assets/Prefabs/`
- Modifies player combat system to support weapon inventory
- Requires spawn point system from static-maze
- All scripts use `NetworkBehaviour` for multiplayer readiness
