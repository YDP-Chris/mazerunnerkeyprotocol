# Maze Runner: Key Protocol
### Product Requirements Document
**Version:** 0.2 — Draft  
**Audience:** Developer, Chris, Claude  
**Repo:** https://github.com/YDP-Chris/mazerunnerkeyprotocol

---

## 1. Game Overview

### 1.1 Elevator Pitch
A 3rd-person multiplayer shooter set inside a procedurally generated maze. Players compete to find a hidden key, then fight their way to the exit. The winner is not the best killer — it's the player who escapes. Every kill has strategic purpose; kill counts mean nothing.

### 1.2 What Makes It Different
- **Kill-with-purpose:** eliminating players only matters if it helps you secure the key or block the escape
- **No XP for kills:** the scoreboard tracks only escapes, creating a fundamentally different incentive structure than COD or Fortnite
- **The maze changes:** procedural generation means no two sessions play the same
- **Asymmetric endgame:** once the key is picked up, the balance of power shifts — everyone becomes a hunter

### 1.3 Core Pillars

| Pillar | What It Means in Practice |
|--------|--------------------------|
| Objective over combat | Escape wins, not kills. Players weigh risk vs reward on every engagement. |
| Tension over chaos | The maze creates natural chokepoints, ambushes, and paranoia. |
| Replayability | Procedural maze + random loot spawns keep sessions fresh. |
| Accessible multiplayer | Low barrier to join a match; no ranked system required at launch. |

---

## 2. Core Mechanics & Rules

### 2.1 Match Flow
1. All players spawn at random, separated starting points inside the maze
2. One key spawns at a random location (not near any exit or spawn)
3. Players explore, collect loot, and fight — all in pursuit of the key
4. The player who picks up the key becomes the target — all other players are notified
5. Key holder must reach the exit to win. Exit location is revealed to all players when the key is picked up
6. Match ends when a player escapes OR all players are eliminated

### 2.2 The Key

| Rule | Detail |
|------|--------|
| Spawn location | Random maze position. Not near spawn points or exit. Min distance from any player start. |
| Visibility | Glows/pulses on approach. Not visible on minimap until within range. |
| Key holder indicator | All players see a directional indicator toward the key holder once key is picked up. |
| Key on death | **OPEN QUESTION** — see Section 7. |
| One key per match | Single key per session. Auto-picked up on contact. |

### 2.3 The Exit
- One exit per maze. Location hidden until key is picked up
- Exit is locked until key is collected — cannot be used early
- Exit glows/pulses once unlocked. Visible on minimap
- Entering the exit zone while holding the key triggers a 2-3 second escape animation (interruptible by damage)

### 2.4 Combat
- 3rd-person shooter with cover mechanics (maze walls = natural cover)
- Players have health bars. No respawn within a match — elimination is permanent per round
- Friendly fire: OFF by default (open question)
- Starting weapon: pistol (unlimited ammo, low damage)
- Additional weapons available from loot boxes

### 2.5 Loot Boxes
- Spawn randomly throughout the maze at match start
- Contents: weapons (shotgun, SMG, rifle), ammo, health packs, grenades (TBD)
- Destroyed after looting — one use only
- **Design constraint:** Spawn logic must use a max-attempts guard (50 attempts max) with a known-safe fallback position. Build this in from the start — do not implement an infinite loop.
- Loot density scales with player count

### 2.6 Win & Loss Conditions

| Condition | Result |
|-----------|--------|
| Player escapes with key | That player wins. Session ends. |
| Key holder is eliminated | Key drops/resets per key-drop rule (Open Question) |
| All players eliminated | Match ends — draw, no winner |
| Timer expires (optional) | Open Question — force key reveal or sudden death |

---

## 3. Enemy AI Specification

### 3.1 Enemy Role
Enemies are maze-dwelling NPCs, not allied with any player. Their purpose is to add danger, slow exploration, and create tactical pressure — especially on the key holder.

### 3.2 State Machine
**Implement as a proper 5-state machine from the start. Do not use simple patrol/chase conditionals.**

| State | Trigger In | Behavior | Trigger Out |
|-------|-----------|----------|-------------|
| PATROL | Default / lost target | Follows patrol path. Slow speed. | Sees or hears player |
| INVESTIGATE | Heard sound (gunshot, footstep) | Moves to last-known sound position. Alert posture. | Finds player → CHASE. Nothing found → PATROL |
| CHASE | Has line-of-sight to player | Pursues via NavMesh. Faster speed. | Loses sight 5s → SEARCH. In range → ATTACK |
| ATTACK | Within attack range | Fires/strikes at player. Reduced movement. | Player leaves range → CHASE. Player dead → PATROL |
| SEARCH | Lost sight of player | Checks last-known position + nearby waypoints. | Finds player → CHASE. Timer expires → PATROL |

### 3.3 Perception
- **Sight:** cone-based line-of-sight. Range and angle configurable per enemy type
- **Sound:** radius-based. Gunshots trigger INVESTIGATE within 15-20 tiles. Footsteps within 3-5 tiles
- **Memory:** remembers last-known player position for 5 seconds in SEARCH state
- **Obstruction:** maze walls block both sight and sound

### 3.4 NavMesh & Movement
- Unity NavMesh Agent for all pathfinding. Baked per maze (rebaked on procedural gen)
- Patrol routes defined as waypoint lists, randomly assigned at spawn
- Must not get stuck on maze corners — use NavMesh obstacle avoidance

### 3.5 Enemy Types

| Type | Description | Phase |
|------|-------------|-------|
| Grunt | Basic patrol. Low health, medium damage. | MVP |
| Guard | Stationary near key spawn until alerted. Higher health. | MVP |
| Hunter | Spawns when key is picked up. Targets key holder specifically. Fast. | Phase 2 |

---

## 4. Maze Generation

### 4.1 Algorithm
Recommended: **Recursive Backtracker** (depth-first search). Produces long corridors, good playability for a shooter, simpler to implement and debug than alternatives.

### 4.2 Parameters

| Parameter | Value / Rule |
|-----------|-------------|
| Grid size | Configurable. Default: 20x20 cells |
| Corridor width | Min 3-4 Unity units for 3rd-person movement |
| Seed | Random per match. Logged for debug reproducibility. |
| Key placement | Post-generation. Min distance from all spawns and exit. |
| Exit placement | Fixed to maze edge. One per maze. |
| Player spawns | Dead-ends, max distance from each other. |
| Loot box placement | Random open floor tiles. Max 50 attempts. Density scales with player count. |

### 4.3 Generation Pipeline
1. Generate grid → wall/floor map
2. Place exit on maze edge
3. Place player spawn points (dead-ends, max distance from each other)
4. Place key (random, min distance from spawns and exit)
5. Scatter loot boxes (max-attempt loop with fallback)
6. Place enemy patrol waypoints
7. Bake NavMesh

---

## 5. Multiplayer Architecture

### 5.1 Player Count
Target: 2-8 players. Optimal: 4 players.

### 5.2 Networking Framework
**Netcode for GameObjects (NGO) + Unity Relay**
- NGO: object sync, RPCs, host/client architecture
- Unity Relay: NAT punchthrough — no port forwarding or dedicated servers needed

### 5.3 Session Model
- Host/client. One player hosts; others connect via Relay code
- Host is authoritative for: key state, exit lock, player elimination
- Enemy AI runs on host only, state synced to clients
- Maze seed generated by host, distributed to all clients at match start — everyone builds the same maze locally

### 5.4 Critical Synced State

| State | Owner | Sync Method |
|-------|-------|-------------|
| Player position/rotation | Client (owner) | Network Transform |
| Player health | Host | Network Variable |
| Key pickup & holder | Host | Network Variable + RPC |
| Exit unlock | Host | Network Variable |
| Enemy position/state | Host | Network Transform + Network Variable |
| Loot box state | Host | Network Variable |
| Player elimination | Host | RPC broadcast |

### 5.5 Multiplayer Implementation Order
1. **Phase 1:** Single-player prototype — all mechanics working locally
2. **Phase 2:** Add NGO. Sync player movement only
3. **Phase 3:** Sync key, exit, win condition
4. **Phase 4:** Sync enemies and loot
5. **Phase 5:** Unity Relay for online play
6. **Phase 6:** Lobby/matchmaking (optional)

> ⚠️ **Critical:** Use `NetworkBehaviour` instead of `MonoBehaviour` for all game-critical scripts from Phase 1, even before NGO is added. Retrofitting single-player code for multiplayer is the most common cause of full rewrites.

---

## 6. Technical Stack

| Layer | Tool | Notes |
|-------|------|-------|
| Engine | Unity 6.3 LTS (6000.3.10f1) | Current install |
| Render Pipeline | URP | Set at project creation |
| Multiplayer | Netcode for GameObjects + Unity Relay | Unity official stack |
| Pathfinding | Unity NavMesh | Built-in. Rebake after maze gen. |
| Version Control | GitHub | https://github.com/YDP-Chris/mazerunnerkeyprotocol |
| AI Integration | Claude Code + IvanMurzak Unity MCP | Live Editor connection — confirmed working |
| IDE | VS Code + C# Dev Kit | |

---

## 7. Open Questions
> These must be answered before or during development. They directly affect architecture and code decisions.

### 7.1 Key Mechanics
1. What happens to the key when the key holder is eliminated? *(Drop in place / teleport to new random location / brief timer then teleport)*
2. Is key pickup automatic on contact, or does it require a deliberate action?
3. Is there only ever one key per match, or multiple keys in larger lobbies?

### 7.2 Match Structure
4. Is there a match timer? What happens when it expires?
5. Are matches one-round or best-of-N?
6. What is the target match length? *(drives maze size and loot density)*

### 7.3 Combat & Player
7. Friendly fire on or off?
8. Can players see each other on a minimap?
9. Does the key holder get an arrow/indicator pointing to the exit on their own screen?

### 7.4 Enemies
10. Does enemy count/difficulty scale with player count?
11. Do enemies respawn during a match or are they fixed at match start?
12. Confirm Phase 2 Hunter enemy that targets key holder specifically?

### 7.5 Technical
13. Target platform(s)? *(PC only for now?)*
14. Will we add a settings/options menu in MVP or post-launch?

---

## 8. Development Phases

| Phase | Scope | Exit Criteria |
|-------|-------|--------------|
| **Phase 0: Setup** ✅ | GitHub, Unity 6.3, Unity MCP, Claude Code connected | Claude can read/write Unity scripts live |
| **Phase 1: Core Loop** | Player controller. Static maze with key + exit. Enemy AI (5-state machine). Loot boxes with 50-attempt guard. Win condition. | One player completes a full match locally |
| **Phase 2: Maze Generation** | Procedural maze. Key/exit/spawn placement pipeline. NavMesh bake. | Each session has a unique maze |
| **Phase 3: Multiplayer Foundation** | NGO integrated. Player movement synced. Host/client architecture. | Two players move in same maze |
| **Phase 4: Full Multiplayer** | Key, exit, health, enemies, loot all synced. Win condition networked. | Full match with 2-4 players online |
| **Phase 5: Polish** | Unity Relay, lobby, UI, sound, balance tuning | Shareable build for external playtesting |

---

## 9. Current Session Priorities
> Update this section at the start of each working session.

**Current phase: Phase 0 → Phase 1 transition**

- [x] Unity 6.3 URP project created
- [x] Unity MCP connected (IvanMurzak — confirmed working)
- [ ] First commit pushed to GitHub
- [ ] Begin Phase 1: player controller, static maze, key + exit placement, win condition

---

*This is a living document. Update Section 9 at the start of each session. Version alongside the project in GitHub.*
