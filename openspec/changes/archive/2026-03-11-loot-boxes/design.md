## Context

The Maze Runner: Key Protocol game currently provides players with only a starting pistol (unlimited ammo, low damage). Combat is one-dimensional and the maze has little exploration incentive beyond finding the key. Loot boxes address both problems by scattering randomized weapon and supply pickups throughout the maze, creating risk/reward decisions -- do you spend time looting for a shotgun, or rush toward the key with just a pistol?

Loot boxes are a Phase 1 deliverable. They spawn at match start, are single-use (destroyed after looting), and contain weapons (shotgun, SMG, rifle), ammo, and health packs. The spawning system has a hard design constraint: placement logic MUST use a max-attempts guard of 50 attempts with a known-safe fallback position to prevent infinite loops. Loot density scales with the number of players in the match.

This change also introduces the weapon system -- the ability to pick up, carry, switch between, and fire multiple weapon types beyond the starting pistol. The existing player-combat capability must be modified to support a weapon inventory rather than a single hardcoded pistol.

All scripts use `NetworkBehaviour` as their base class for multiplayer readiness. Loot box state is managed via `NetworkVariable` so the host is authoritative over loot box contents and pickup status.

## Goals / Non-Goals

**Goals:**
- Implement a loot box prefab with visual indicator (glow/pulse), interaction trigger, and single-use destruction
- Implement a spawn system that places loot boxes on valid open floor tiles using a max 50-attempt loop with a known-safe fallback position
- Scale loot box density with player count using a configurable ratio
- Define a loot table with randomized drops: shotgun, SMG, rifle, ammo pickups, and health packs
- Implement a weapon system supporting pickup, inventory (carry up to 2 weapons + pistol), switching, and per-weapon fire behavior
- Implement weapon data as ScriptableObjects for clean configuration of damage, fire rate, ammo capacity, spread, and range per weapon type
- Modify the player combat system to support multiple weapons with different stats and ammo tracking
- Use `NetworkBehaviour` and `NetworkVariable` on all loot and weapon scripts
- Ensure host authority over loot box state (contents, pickup, destruction)

**Non-Goals:**
- Grenades or throwable items (TBD per PRD, deferred to a future change)
- Weapon attachments, upgrades, or modifications
- Weapon drop or trade between players
- Loot box respawning mid-match (single-use, match-start only)
- Visual weapon models or animations (Phase 5 polish -- placeholder representations acceptable)
- Networked multiplayer synchronization of loot interactions (Phase 4, but the architecture must support it)
- Loot rarity tiers or color-coded quality levels
- AI enemy weapon drops

## Decisions

1. **ScriptableObject weapon definitions**: Each weapon type (pistol, shotgun, SMG, rifle) is defined as a `WeaponData` ScriptableObject containing damage, fire rate, ammo capacity, max ammo, spread angle, range, and fire mode (single/auto). This allows designers to tune weapons without touching code and makes adding new weapons trivial.

2. **Max 50-attempt spawn guard with fallback**: The loot box placement algorithm attempts up to 50 random valid floor tile positions per box. If no valid position is found after 50 attempts, it uses a known-safe fallback position (the center of the maze or a pre-validated list of positions). This is a hard design constraint from the PRD and must not be bypassed.

3. **Weapon inventory model -- pistol + 2 slots**: The player always has the starting pistol (cannot be dropped, unlimited ammo). They can carry up to 2 additional looted weapons. Picking up a 3rd weapon while slots are full will swap it with the currently equipped weapon, dropping the replaced weapon. This keeps inventory simple and forces meaningful choices.

4. **Hitscan for all weapons in Phase 1**: All weapons use raycasting (hitscan) for hit detection, matching the pistol implementation. The shotgun fires multiple rays in a spread cone. The SMG and rifle fire single rays with different fire rates and damage values. Projectile physics can be added later if gameplay demands it.

5. **Loot table as weighted random**: Each loot box rolls against a weighted loot table to determine its contents. Weapons are less common than ammo and health packs. The table is defined as a ScriptableObject for easy tuning. Each loot box contains exactly one item.

6. **Proximity-based interaction**: Players loot boxes by entering a trigger collider and pressing an interaction key (E by default). The interaction requires the player to be within range and have line of sight (no looting through walls). The box plays a brief open animation/effect, grants the item, and is destroyed.

7. **Host-authoritative loot state**: The host determines loot box contents at match start (using the match seed for determinism). Pickup requests are sent as RPCs to the host, which validates and applies them. This prevents race conditions where two players try to loot the same box simultaneously.

8. **Density scaling formula**: Base loot count is 8 boxes for a 2-player match. Add 3 boxes per additional player. Formula: `lootCount = 8 + (playerCount - 2) * 3`. Capped at a maximum relative to maze size to prevent over-saturation.

## Risks / Trade-offs

1. **50-attempt guard may cluster loot boxes**: If valid floor tiles are scarce (small maze, many obstacles), the fallback positions could cluster loot near the maze center. Mitigation: maintain a distributed list of pre-validated fallback positions across different maze quadrants, not just a single center point.

2. **Weapon balance without playtesting data**: Initial weapon stats (damage, fire rate, ammo) are educated guesses. The shotgun in tight maze corridors could be overpowered. Mitigation: use ScriptableObjects so all values are tunable without code changes. Plan a balance pass during Phase 5.

3. **Two-weapon limit feels restrictive**: Players may want to carry more weapons. However, unlimited inventory removes the strategic choice of which weapons to carry and reduces the value of loot boxes as contested resources. Mitigation: the 2-slot limit is a starting point; it can be expanded based on playtesting feedback.

4. **Single item per loot box simplicity vs. variety**: One item per box is simpler to implement and network, but means players need to find more boxes. Mitigation: this keeps individual loot box interactions fast (no inventory UI for selecting items) and increases the number of exploration touchpoints in the maze.

5. **Hitscan shotgun spread at close range**: Multiple raycasts in a cone can all hit at close range, making the shotgun devastating in tight corridors. Mitigation: cap per-pellet damage so total damage at point-blank is strong but not instant-kill. Tune pellet count and cone angle via ScriptableObject.

6. **Race condition on loot pickup without networking**: In the single-player Phase 1 prototype, the host-authority pattern works naturally (the single player is the host). But the architecture must handle the multiplayer case where two RPCs arrive nearly simultaneously. Mitigation: the host processes pickups sequentially and checks box state before granting. The `NetworkVariable` for box state (available/looted) is the source of truth.
