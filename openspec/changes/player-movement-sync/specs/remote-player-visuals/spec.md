## ADDED Requirements

### Requirement: Visible player body for remote players
Each player object SHALL have a visible capsule mesh that is shown to remote clients and hidden from the owning client.

#### Scenario: Remote player is visible as capsule
- **WHEN** two players are in the same maze
- **THEN** each player sees the other as a visible capsule at the correct position

#### Scenario: Owner does not see own body
- **WHEN** a player looks down or around in first-person view
- **THEN** their own capsule body mesh is not visible (MeshRenderer disabled on owner)

### Requirement: Player nameplate
Each player object SHALL display a nameplate above the capsule showing the player's identifier (e.g., "Player 1"). The nameplate SHALL be hidden on the owner's instance.

#### Scenario: Remote player has visible nameplate
- **WHEN** a player looks at another player
- **THEN** a text label showing the remote player's identifier is visible above their capsule

#### Scenario: Nameplate faces camera
- **WHEN** the local player moves around a remote player
- **THEN** the remote player's nameplate always faces toward the local player's camera (billboard behavior)

#### Scenario: Owner nameplate hidden
- **WHEN** a player is controlling their own character
- **THEN** no nameplate is visible for their own player object

### Requirement: Distinct player colors
Each player's capsule SHALL use a distinct color based on their player index to differentiate players visually.

#### Scenario: Two players have different colors
- **WHEN** two players are in the same match
- **THEN** their capsules are rendered in different colors (e.g., blue for player 1, red for player 2)
