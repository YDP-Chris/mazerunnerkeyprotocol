## Context

Maze Runner: Key Protocol is a 3rd-person multiplayer maze shooter where players compete to find a hidden key and escape the maze. The key-exit system is the central mechanic that distinguishes the game from conventional shooters: kills are only meaningful insofar as they help a player secure the key or prevent an opponent from escaping. This change defines the complete key lifecycle, exit gate behavior, win/loss resolution, and key-holder tracking system that together form the core game loop.

The game runs on Unity 6.3 URP with Netcode for GameObjects (NGO). All game-critical scripts use `NetworkBehaviour` as their base class. The host is authoritative for key state, exit lock state, and player elimination. The maze is procedurally generated from a shared seed, and the key, exit, and spawn points are placed via a post-generation pipeline.

This design covers four capabilities:
- **key-mechanics** -- spawning, proximity detection, pickup, drop-on-death, and visual feedback for the key
- **exit-mechanics** -- exit gate placement, lock/unlock lifecycle, escape animation, and interruption
- **win-condition** -- match resolution rules for escape, total elimination, and key-holder death
- **key-holder-tracking** -- directional indicator shown to all non-holder players once the key is picked up

## Goals / Non-Goals

**Goals:**
- Define deterministic, host-authoritative rules for key pickup, key drop, exit unlock, and match resolution
- Ensure all key and exit state is synchronized across clients via NetworkVariables and RPCs
- Provide clear visual and UI feedback so every player knows: (a) whether the key has been picked up, (b) who holds it, (c) where the exit is once unlocked
- Make the escape interruptible so combat remains relevant in the endgame
- Keep the system extensible for future additions (multiple keys, match timers, Hunter enemy type)

**Non-Goals:**
- Multiplayer networking implementation details (covered by the multiplayer-sync change)
- Enemy AI behavior when the key is picked up (covered by enemy-ai change)
- Loot box spawning or weapon mechanics
- Lobby, matchmaking, or session management
- Match timer or sudden-death mechanics (open question, deferred)
- Multiple keys per match (open question, deferred)

## Decisions

1. **Key pickup is automatic on contact.** The key is picked up the instant a player's collider overlaps the key's trigger collider. No deliberate input is required. This reduces friction and creates moments of accidental pickup that add tension.

2. **Key drops in place on holder death.** When the key holder is eliminated, the key drops at the holder's last position and becomes available for any surviving player to pick up. This is the simplest initial implementation and keeps the key physically grounded in the maze, rewarding players who are nearby during the fight.

3. **One key, one exit per match.** The current design targets 2-8 players. A single key and single exit create a natural convergence point. Multi-key variants are deferred.

4. **Exit is locked until key pickup.** The exit gate exists in the maze from generation time but cannot be interacted with until the key is picked up. Once unlocked, the exit becomes visible on the minimap and glows to signal its location.

5. **Escape requires a 2-3 second channeled animation.** The key holder must remain in the exit zone for a full escape animation to complete. Taking damage interrupts the animation and resets the timer. The holder must re-enter the zone or survive long enough to channel again. This window gives pursuers a meaningful chance to intervene.

6. **Host is authoritative for all key-exit state.** Key ownership, exit lock status, escape progress, and match result are all owned by the host and replicated via NetworkVariables. Client inputs (entering exit zone, colliding with key) are validated server-side.

7. **Directional indicator, not a minimap icon, for key holder.** All non-holder players receive a HUD-based directional arrow pointing toward the key holder. This gives positional awareness without revealing exact location, preserving maze navigation as a skill.

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| Key dropping in place may create camping incentives near a key-holder fight | Monitor in playtesting. Fallback option: short delay before dropped key becomes pickable, or teleport to a new random location. |
| Auto-pickup may frustrate players who stumble onto the key unprepared | Accepted trade-off. The surprise factor is intentional and creates emergent gameplay moments. |
| 2-3 second escape animation may feel too long or too short depending on maze size | Make the duration a configurable parameter. Tune during playtesting. |
| Directional indicator toward key holder may make escape too difficult in small mazes | Indicator shows direction only, not distance. Maze walls still obstruct pursuit. Tune indicator precision per maze size if needed. |
| Host authority creates single point of failure if host disconnects | Out of scope for MVP. Host migration is a Phase 5+ concern. |
| Escape animation interruption on any damage may be too punishing | Start with any-damage-interrupts. Consider a damage threshold or partial progress retention if playtesting shows escape is nearly impossible. |
