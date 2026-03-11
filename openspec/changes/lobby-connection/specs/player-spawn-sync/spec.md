## ADDED Requirements

### Requirement: Deterministic spawn point assignment
The system SHALL assign spawn points to players based on their join order, ensuring each player spawns at a unique location.

#### Scenario: Two players spawn at different points
- **WHEN** two players are connected and the match starts
- **THEN** player 1 (host) spawns at spawn point 0 and player 2 (first client) spawns at spawn point 1

#### Scenario: More players than spawn points
- **WHEN** the number of connected players exceeds available spawn points
- **THEN** spawn points are reused cyclically (index modulo spawn count)

### Requirement: Spawn timing after scene load
The system SHALL wait until all clients have loaded the game scene before spawning player objects.

#### Scenario: All clients loaded
- **WHEN** NetworkSceneManager reports all clients have completed scene loading
- **THEN** the host triggers player object spawning at assigned positions

#### Scenario: Slow client loading
- **WHEN** one client takes longer to load than others
- **THEN** no players are spawned until all clients report scene load complete

### Requirement: Player object ownership
Each spawned player object SHALL be owned by its respective client, allowing owner-authoritative movement input.

#### Scenario: Client controls own player
- **WHEN** a player object is spawned for a client
- **THEN** that client has ownership and can send movement input; other clients see the player but cannot control it

#### Scenario: Non-owner scripts disabled
- **WHEN** a player object is spawned on a non-owner client
- **THEN** input, camera, and local-only components are disabled on that client's instance
