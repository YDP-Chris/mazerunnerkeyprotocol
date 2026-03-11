## ADDED Requirements

### Requirement: Host session creation
The system SHALL start an NGO host session using Unity Transport with a configurable listen port (default 7777).

#### Scenario: Host starts successfully
- **WHEN** the player chooses to host
- **THEN** NetworkManager.StartHost() is called and the host begins listening for connections

#### Scenario: Host fails to bind port
- **WHEN** the listen port is already in use
- **THEN** the system displays an error and returns to the main menu

### Requirement: Client connection
The system SHALL connect a client to a host by setting Unity Transport's connection address and port, then calling NetworkManager.StartClient().

#### Scenario: Client connects to host on LAN
- **WHEN** client provides a valid LAN IP and port
- **THEN** the client connects and receives connection approval from the host

#### Scenario: Client connection timeout
- **WHEN** the client cannot reach the host within 5 seconds
- **THEN** the connection attempt is aborted and the client is notified

### Requirement: Connection approval
The host SHALL validate incoming connections using NGO's connection approval callback. The host SHALL reject connections when the match has already started or the player count has reached the maximum (8).

#### Scenario: Client joins before match starts
- **WHEN** a client requests connection and the match has not started and player count is below 8
- **THEN** the connection is approved

#### Scenario: Client joins after match started
- **WHEN** a client requests connection and the match has already started
- **THEN** the connection is rejected with a reason message

#### Scenario: Client joins when lobby is full
- **WHEN** a client requests connection and 8 players are already connected
- **THEN** the connection is rejected with a reason message

### Requirement: Disconnect handling
The system SHALL handle player disconnects gracefully in both lobby and match states.

#### Scenario: Client disconnects during lobby
- **WHEN** a connected client disconnects while in the lobby
- **THEN** the client is removed from the player list and other players are notified

#### Scenario: Client disconnects during match
- **WHEN** a connected client disconnects during an active match
- **THEN** the client's player object is despawned and the match continues for remaining players

#### Scenario: Host disconnects
- **WHEN** the host disconnects or closes the application
- **THEN** all clients are disconnected and returned to the main menu with a "Host disconnected" message

### Requirement: Networked scene transition
The system SHALL use NGO's NetworkSceneManager to transition all connected clients from the MainMenu scene to the Game scene simultaneously.

#### Scenario: Scene transition on match start
- **WHEN** the host starts the match
- **THEN** NetworkManager.SceneManager.LoadScene is called and all clients load the game scene

#### Scenario: All clients finish loading
- **WHEN** all clients have finished loading the game scene
- **THEN** the match initialization begins (maze generation, spawning, etc.)

### Requirement: Player data tracking
The system SHALL maintain a NetworkList of player lobby data (client ID, display name) synchronized to all clients.

#### Scenario: New player data added on connect
- **WHEN** a client's connection is approved
- **THEN** a PlayerLobbyData entry is added to the NetworkList with the client's ID and display name

#### Scenario: Player data removed on disconnect
- **WHEN** a client disconnects
- **THEN** the corresponding PlayerLobbyData entry is removed from the NetworkList
