## 1. Scene and Project Setup

- [x] 1.1 Create MainMenu scene with a basic Canvas and EventSystem
- [x] 1.2 Add MainMenu and existing Game scene to Build Settings in correct order
- [x] 1.3 Create `Assets/Scripts/Network/ConnectionManager.cs` (NetworkBehaviour) with host/join/disconnect lifecycle methods

## 2. Connection Manager

- [x] 2.1 Implement `HostGame()`: configure Unity Transport listen port, call `NetworkManager.StartHost()`, set up connection approval callback
- [x] 2.2 Implement `JoinGame(string ip, ushort port)`: set Unity Transport connection data, call `NetworkManager.StartClient()`, handle timeout (5s)
- [x] 2.3 Implement connection approval: reject if match started or player count >= 8, approve otherwise and add PlayerLobbyData to NetworkList
- [x] 2.4 Define `PlayerLobbyData` struct implementing `INetworkSerializable` and `IEquatable<PlayerLobbyData>` (clientId, displayName)
- [x] 2.5 Implement disconnect handling: `OnClientDisconnectCallback` removes player from NetworkList, host disconnect returns all clients to MainMenu

## 3. Lobby UI

- [x] 3.1 Build MainMenu UI: "Host Game" and "Join Game" buttons on the initial view
- [x] 3.2 Build Join view: IP input field, port input field, "Connect" button, "Back" button, error text
- [x] 3.3 Build Lobby view: connection info display (host IP:port), player list, "Start Match" button (host only), "Leave" button
- [x] 3.4 Create `Assets/Scripts/UI/LobbyUI.cs` to wire up all UI panels and buttons to ConnectionManager methods
- [x] 3.5 Implement player list UI that subscribes to NetworkList changes and updates dynamically
- [x] 3.6 Display host's local IP address by querying network interfaces

## 4. Networked Scene Transition

- [x] 4.1 Implement `StartMatch()` on ConnectionManager: call `NetworkManager.SceneManager.LoadScene("Game", LoadSceneMode.Single)` (host only)
- [x] 4.2 Subscribe to `OnLoadEventCompleted` to detect when all clients have loaded the game scene
- [x] 4.3 Gate match initialization (maze gen, spawning) until all clients report scene load complete

## 5. Player Spawn Synchronization

- [x] 5.1 Refactor spawn point assignment from AutoStartHost into ConnectionManager: assign by join order index from PlayerLobbyData list
- [x] 5.2 Ensure player prefab spawns with correct ownership (each client owns their player object)
- [x] 5.3 Verify owner-only scripts (PlayerMovement, PlayerCameraController, PlayerCombat) disable correctly on non-owner clients

## 6. Dev Testing Support

- [x] 6.1 Add a `--auto-host` flag or dev bypass that preserves AutoStartHost behavior for quick single-player testing
- [ ] 6.2 Test two instances on same machine: host on one, join via 127.0.0.1 on the other
- [ ] 6.3 Verify two players can move independently in the same maze and see each other
