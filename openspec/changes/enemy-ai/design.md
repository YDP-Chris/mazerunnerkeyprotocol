## Context

The Maze Runner: Key Protocol maze needs to feel dangerous even before players encounter each other. Enemy NPCs serve as environmental hazards that slow exploration, create tactical pressure, and punish reckless movement. The PRD mandates a proper 5-state machine (PATROL, INVESTIGATE, CHASE, ATTACK, SEARCH) from the start -- not simple patrol/chase conditionals -- because the enemy behavior needs to support multiple enemy types with distinct roles and the future addition of the Hunter type in Phase 2.

Enemy AI runs exclusively on the host in the multiplayer architecture. All enemy state is synced to clients via NetworkVariables and NetworkTransform. Every enemy script must use `NetworkBehaviour` from the start to avoid costly retrofitting when multiplayer is integrated.

The maze is procedurally generated, so enemies cannot rely on hand-placed waypoints or baked-in patrol routes. Patrol paths, spawn positions, and NavMesh data are all generated at runtime after the maze is built.

## Goals / Non-Goals

**Goals:**

- Implement a clean, extensible 5-state machine that all enemy types share as a base
- Create a perception system (sight + sound) that respects maze wall occlusion for both senses
- Ship two MVP enemy types: Grunt (patrolling) and Guard (stationary near key spawn)
- Use Unity NavMesh Agent for all pathfinding with proper obstacle avoidance so enemies never get stuck on maze corners
- Design all components so they are configurable per enemy type (sight range, speed, health, damage, patrol behavior)
- Ensure all scripts use `NetworkBehaviour` and are structured for host-authoritative AI with client-side state sync

**Non-Goals:**

- Hunter enemy type (Phase 2 -- not implemented now, but the architecture must not prevent it)
- Enemy respawning during a match (enemies are fixed at match start for MVP)
- Enemy-to-enemy communication or coordination (e.g., flanking, calling for backup)
- Complex behavior trees or utility AI -- the 5-state machine is the chosen architecture
- Client-side AI prediction or interpolation beyond what NetworkTransform provides
- Enemy loot drops on death
- Difficulty scaling based on player count (open question in PRD, deferred)

## Decisions

1. **State machine pattern over behavior trees.** The PRD explicitly requires a 5-state machine. This is simpler to implement, debug, and reason about than a behavior tree. Each state is a discrete class or method with clearly defined entry/exit conditions and no ambiguous fallthrough.

2. **Shared base, configured per type.** All enemy types use the same `EnemyStateMachine` component. Behavioral differences (Grunt patrols vs. Guard stands still) are driven by configuration data (ScriptableObject or serialized fields), not by subclassing the state machine itself. This keeps the codebase small and makes adding the Hunter type later a configuration task, not an architecture task.

3. **Perception as a separate component.** Sight and sound detection are handled by a dedicated `EnemyPerception` component, not embedded in the state machine. This allows perception parameters to be tuned independently per enemy type and makes testing perception logic in isolation straightforward.

4. **Sound events are broadcast, not continuously polled.** When a player fires a weapon or moves, a sound event is emitted with a position and radius. Enemies within range (and not blocked by walls) receive the event. This is more performant than having every enemy continuously raycasting for sound sources every frame.

5. **Wall occlusion uses raycasting.** Both sight and sound occlusion are determined by raycasting against maze wall colliders. This is simpler and more reliable than analytical approaches given the procedural maze geometry.

6. **NavMesh rebake after maze generation.** The NavMesh is baked at runtime after the maze is generated. Patrol waypoints are assigned from a pool of valid NavMesh positions generated during the maze placement pipeline (step 6 in the PRD generation pipeline).

7. **Host-only AI execution.** The `EnemyStateMachine` and `EnemyPerception` components only run their update logic on the host (`IsServer` check). Position is synced via `NetworkTransform`. Current state and target information are synced via `NetworkVariable` so clients can play appropriate animations.

## Risks / Trade-offs

1. **NavMesh bake time on large mazes.** Runtime NavMesh baking on a 20x20 grid with 3-4 unit corridors should be fast, but larger maze sizes could introduce noticeable load times. Mitigation: profile early; if needed, bake asynchronously or use NavMesh surface components that support incremental baking.

2. **Raycast cost for perception.** Each enemy performs raycasts for sight (every frame in CHASE/ATTACK, every 0.2-0.5s otherwise) and for sound occlusion (on each sound event). With many enemies and players, this could become expensive. Mitigation: use layer masks to limit raycast targets; throttle perception checks in PATROL state; cap enemy count per match.

3. **State machine rigidity.** A 5-state machine is simple but less flexible than a behavior tree. If future enemy types need behaviors that don't map cleanly to these five states, the architecture may need extension. Mitigation: design state transitions as data-driven (configurable per type) so new states can be added without rewriting existing ones.

4. **Guard type depends on key spawn position.** The Guard is stationary near the key spawn, which means Guard placement is coupled to the key placement step in the maze generation pipeline. If key placement logic changes, Guard placement must update accordingly. Mitigation: Guards reference the key spawn position at runtime rather than storing it at generation time.

5. **No enemy respawning limits match tension over time.** Once all enemies in an area are cleared, that area becomes permanently safe. This could reduce tension in longer matches. Accepted for MVP; respawning can be added later if playtesting shows it is needed.

6. **NetworkBehaviour overhead in single-player.** Using `NetworkBehaviour` for all scripts adds slight overhead and complexity even during Phase 1 single-player development. This is an intentional trade-off per the PRD directive to avoid multiplayer retrofitting later.
