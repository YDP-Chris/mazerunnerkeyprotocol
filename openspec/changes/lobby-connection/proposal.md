## Why

Phase 1 uses AutoStartHost to immediately start a single-player host session. Phase 3's exit criteria is "two players move in same maze," which requires a proper connection flow where one player hosts and another joins. Without a lobby system, there's no way for a second player to connect.

## What Changes

- Replace AutoStartHost with a lobby/connection flow: main menu scene with Host and Join options
- Host creates a session and sees a connection code (IP:port); clients enter this code to join
- Connection approval validates incoming clients (player cap, match state)
- Lobby UI shows connected players list with ready status
- Host initiates match start, which transitions all connected clients to the game scene together
- Player spawn assignment distributes players across spawn points deterministically
- Disconnect handling: graceful cleanup when players leave mid-lobby or mid-match
- Uses Unity Transport direct connect (not Relay — that's Phase 5)

## Capabilities

### New Capabilities
- `lobby-ui`: Main menu scene with Host/Join buttons, connection code input, connected player list, and host-controlled match start
- `connection-manager`: Session lifecycle management — hosting, joining via IP:port, connection approval, disconnect handling, and scene transition for all clients
- `player-spawn-sync`: Synchronized player spawning across host and clients with deterministic spawn point assignment at match start

### Modified Capabilities
- None — existing Phase 1 systems (player-controller, key-exit, enemy-ai, loot-boxes) remain unchanged. AutoStartHost is removed/disabled but no spec-level behavior changes to existing capabilities.

## Impact

- **Scripts**: New `LobbyManager`, `ConnectionManager`, `LobbyUI` scripts. AutoStartHost disabled or wrapped with a bypass flag for dev testing
- **Scenes**: New `MainMenu` scene with lobby UI. Existing game scene loaded via networked scene management
- **Prefabs**: Lobby UI canvas prefab
- **Dependencies**: No new packages — Unity Transport and NGO already installed
- **Build Settings**: Two scenes in build order (MainMenu → Game)
