## ADDED Requirements

### Requirement: Main menu with host and join options
The system SHALL display a main menu scene with two primary actions: "Host Game" and "Join Game".

#### Scenario: Player chooses to host
- **WHEN** player clicks "Host Game"
- **THEN** the system starts an NGO host session and transitions to the lobby view displaying the host's IP address and port

#### Scenario: Player chooses to join
- **WHEN** player clicks "Join Game"
- **THEN** the system displays an input field for IP:port and a "Connect" button

### Requirement: Join game input and connection
The system SHALL allow a client to enter an IP address and port to connect to a host.

#### Scenario: Client enters valid IP and connects
- **WHEN** client enters a valid IP:port and clicks "Connect"
- **THEN** the system attempts to connect via Unity Transport and transitions to the lobby view on success

#### Scenario: Connection fails
- **WHEN** client enters an IP:port and the connection fails or times out (5 seconds)
- **THEN** the system displays an error message and returns to the join input view

### Requirement: Lobby player list
The system SHALL display a list of all connected players in the lobby, updated in real time.

#### Scenario: Player joins lobby
- **WHEN** a new client connects and is approved
- **THEN** all players in the lobby see the new player added to the player list

#### Scenario: Player disconnects from lobby
- **WHEN** a connected player disconnects
- **THEN** the player is removed from the list on all remaining clients

### Requirement: Host match start control
The system SHALL allow only the host to start the match. The start button SHALL be visible only to the host.

#### Scenario: Host starts match with minimum players
- **WHEN** host clicks "Start Match" and at least 1 client is connected (2+ total players)
- **THEN** the system initiates networked scene transition to the game scene for all connected players

#### Scenario: Host starts match solo (dev mode)
- **WHEN** host clicks "Start Match" with no clients connected
- **THEN** the system starts the match with only the host (for testing purposes)

### Requirement: Connection code display
The system SHALL display the host's local IP address and port prominently in the lobby so it can be shared with other players.

#### Scenario: Host sees connection info
- **WHEN** a player hosts a game and enters the lobby
- **THEN** the lobby displays the host machine's local network IP and the Unity Transport listen port
