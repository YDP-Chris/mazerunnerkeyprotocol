## Context

Phase 1 is complete with all game mechanics working in single-player via AutoStartHost. The project already uses NetworkBehaviour on all game-critical scripts, NetworkVariables for state sync, and ServerRpc/ClientRpc patterns. Unity Transport (UTP) is configured on the NetworkManager. However, there is no way for a second player to connect — AutoStartHost immediately starts a host session with no join flow.

The existing connection approval callback in AutoStartHost already handles spawn point assignment by client ID, which can be reused. The game scene contains all gameplay objects; we need a separate menu scene for the lobby.

## Goals / Non-Goals

**Goals:**
- Two players can connect and play in the same maze (Phase 3 exit criteria)
- Host creates a session, client joins via IP:port
- Players see each other in a lobby before the match starts
- Host controls when the match begins
- Graceful handling of disconnects in lobby and during match

**Non-Goals:**
- Unity Relay / NAT punchthrough (Phase 5)
- Matchmaking or lobby browser
- More than LAN/direct-IP connectivity
- Voice chat or text chat
- Player customization or loadouts
- Syncing combat, key, loot, or enemy state (Phase 4)

## Decisions

### 1. Two-scene architecture (MainMenu + Game)

**Decision:** Separate MainMenu scene for lobby UI, load Game scene via NGO's networked scene management (`NetworkManager.SceneManager.LoadScene`).

**Why:** Keeps lobby logic isolated from gameplay. NGO's scene management automatically syncs scene transitions to all clients. AutoStartHost can remain as a dev bypass (enabled via a flag or removed from MainMenu scene).

**Alternatives:**
- Single scene with UI toggle: simpler but mixes concerns, harder to manage lifecycle
- Three scenes (Menu/Lobby/Game): over-engineered for current needs

### 2. Direct IP:port connection (no Relay)

**Decision:** Host displays their local IP and port. Client enters IP:port to connect. Unity Transport handles the socket.

**Why:** Simplest path to two-player testing. Works on LAN immediately. Relay integration (Phase 5) only requires swapping the transport layer — no lobby logic changes.

**Alternatives:**
- Relay now: adds complexity and Relay package dependency before core gameplay is validated
- Steam networking: wrong platform target for now

### 3. Connection approval on host

**Decision:** Reuse and extend the existing connection approval pattern from AutoStartHost. Host validates: match hasn't started, player count < max (8), client version matches.

**Why:** Already have the pattern. Connection approval is the standard NGO mechanism for gatekeeping joins.

### 4. Player tracking via NetworkList

**Decision:** Use a `NetworkList<PlayerLobbyData>` on the LobbyManager to track connected players (client ID, name, ready status). Lobby UI reads this list.

**Why:** NetworkList automatically syncs additions/removals to all clients. Simpler than manual RPC-based player tracking.

**Alternatives:**
- NetworkVariable with serialized array: doesn't support dynamic resize well
- RPC broadcast on every change: more code, more error-prone

### 5. Spawn point assignment

**Decision:** Host assigns spawn points sequentially by join order (stored in PlayerLobbyData list index). Passed via connection approval response's Position/Rotation fields when the game scene loads.

**Why:** Deterministic, simple, already partially implemented in AutoStartHost.

## Risks / Trade-offs

- **[LAN only]** → Acceptable for Phase 3. Relay in Phase 5 swaps transport, no lobby changes needed.
- **[IP discovery UX]** → Players need to know host's LAN IP. Mitigated by displaying it in the lobby UI. Could add clipboard copy.
- **[Host migration not supported]** → If host disconnects, match ends. Acceptable — host migration is complex and not in PRD scope.
- **[Scene transition timing]** → All clients must finish loading before gameplay starts. Use NGO's `LoadSceneMode.Single` with `OnLoadComplete` callback to gate match start.
- **[NetworkList struct constraints]** → `PlayerLobbyData` must implement `INetworkSerializable` and `IEquatable`. Minor boilerplate.
