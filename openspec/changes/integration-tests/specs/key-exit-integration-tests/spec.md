## ADDED Requirements

### Requirement: KeyManager pickup sets key state to Held
When a player requests key pickup via `RequestPickupServerRpc`, the KeyManager SHALL set `CurrentKeyState` to `Held` and `KeyHolderClientId` to the requesting client's ID.

#### Scenario: First pickup request succeeds
- **WHEN** `CurrentKeyState` is `Uncollected`
- **WHEN** `RequestPickupServerRpc(clientId)` is called
- **THEN** `CurrentKeyState.Value` SHALL equal `KeyState.Held`
- **THEN** `KeyHolderClientId.Value` SHALL equal the requesting client ID

### Requirement: KeyManager rejects pickup when key is already held
When the key is already in `Held` state, subsequent `RequestPickupServerRpc` calls SHALL be ignored.

#### Scenario: Second pickup request is rejected
- **WHEN** `CurrentKeyState` is `Held` by client A
- **WHEN** `RequestPickupServerRpc(clientB)` is called
- **THEN** `CurrentKeyState.Value` SHALL remain `Held`
- **THEN** `KeyHolderClientId.Value` SHALL remain client A's ID

### Requirement: KeyManager fires OnKeyPickedUp event on pickup
When the key is picked up, the `OnKeyPickedUp` event SHALL fire with the holder's client ID.

#### Scenario: Event fires with correct client ID
- **WHEN** `RequestPickupServerRpc(clientId)` is called and succeeds
- **THEN** `OnKeyPickedUp` SHALL be invoked with the holder's client ID

### Requirement: KeyManager drops key at death position
When `DropKey(deathPosition)` is called on the server, the key SHALL transition to `Dropped` state, clear the holder, and update the world position.

#### Scenario: Key dropped on holder elimination
- **WHEN** `CurrentKeyState` is `Held` by a client
- **WHEN** `DropKey(deathPosition)` is called on the server
- **THEN** `CurrentKeyState.Value` SHALL equal `KeyState.Dropped`
- **THEN** `KeyHolderClientId.Value` SHALL equal `ulong.MaxValue`
- **THEN** `KeyWorldPosition.Value` SHALL be near the death position

### Requirement: KeyManager fires OnKeyDropped event on drop
When the key is dropped, the `OnKeyDropped` event SHALL fire with the drop position.

#### Scenario: Drop event fires with position
- **WHEN** `DropKey(position)` is called
- **THEN** `OnKeyDropped` SHALL be invoked with the drop position

### Requirement: KeyManager IsKeyHolder returns correct status
`IsKeyHolder(clientId)` SHALL return true only when the key is in `Held` state and the given client ID matches `KeyHolderClientId`.

#### Scenario: Holder query returns true for correct client
- **WHEN** client 1 holds the key
- **THEN** `IsKeyHolder(1)` SHALL return true
- **THEN** `IsKeyHolder(2)` SHALL return false

#### Scenario: Holder query returns false when key not held
- **WHEN** `CurrentKeyState` is `Uncollected`
- **THEN** `IsKeyHolder(anyClientId)` SHALL return false

### Requirement: ExitGateway unlocks when key is picked up
ExitGateway SHALL set `IsUnlocked` to true when it receives the `OnKeyPickedUp` event from KeyManager.

#### Scenario: Key pickup unlocks exit
- **WHEN** a KeyManager fires `OnKeyPickedUp`
- **THEN** `ExitGateway.IsUnlocked.Value` SHALL be true

### Requirement: ExitGateway escape progress increments over time
When a key holder enters the exit trigger zone, `EscapeProgress` SHALL increment from 0 toward 1 over `escapeDuration` seconds.

#### Scenario: Escape progress increases each frame
- **WHEN** escape has been initiated for the key holder
- **WHEN** multiple frames elapse
- **THEN** `EscapeProgress.Value` SHALL be greater than 0 and increasing

### Requirement: ExitGateway escape completes and fires event
When `EscapeProgress` reaches 1.0, the `OnEscapeComplete` event SHALL fire with the escaping client's ID.

#### Scenario: Full escape duration triggers completion
- **WHEN** escape is in progress
- **WHEN** enough time elapses for `EscapeProgress` to reach 1.0
- **THEN** `OnEscapeComplete` SHALL be invoked with the escaping client ID

### Requirement: ExitGateway cancels escape on key holder damage
When `OnKeyHolderDamaged(clientId)` is called with the escaping client's ID, the escape SHALL be cancelled and `EscapeProgress` reset to 0.

#### Scenario: Damage during escape cancels it
- **WHEN** escape is in progress for a client
- **WHEN** `OnKeyHolderDamaged(escapingClientId)` is called
- **THEN** `EscapeProgress.Value` SHALL be 0
- **THEN** the escape SHALL no longer be in progress

#### Scenario: Damage to non-escaping client does not cancel escape
- **WHEN** escape is in progress for client A
- **WHEN** `OnKeyHolderDamaged(clientB)` is called (different client)
- **THEN** escape SHALL continue and `EscapeProgress.Value` SHALL not be reset

### Requirement: MatchManager reports Win on escape completion
When `OnEscapeComplete` fires from ExitGateway, MatchManager SHALL set `CurrentOutcome` to `Win` and `WinnerClientId` to the escaping player's client ID.

#### Scenario: Escape triggers win condition
- **WHEN** `ExitGateway.OnEscapeComplete` fires with a client ID
- **THEN** `MatchManager.CurrentOutcome.Value` SHALL equal `MatchOutcome.Win`
- **THEN** `MatchManager.WinnerClientId.Value` SHALL equal the escaping client ID

### Requirement: MatchManager reports Draw when all players eliminated
When `OnPlayerEliminated` is called enough times to reduce alive player count to 0, MatchManager SHALL set `CurrentOutcome` to `Draw`.

#### Scenario: All players eliminated triggers draw
- **WHEN** the match starts with N players
- **WHEN** `OnPlayerEliminated` is called N times
- **THEN** `MatchManager.CurrentOutcome.Value` SHALL equal `MatchOutcome.Draw`
- **THEN** `MatchManager.WinnerClientId.Value` SHALL equal `ulong.MaxValue`

### Requirement: MatchManager fires OnMatchEnded event
When the match ends (win or draw), the `OnMatchEnded` event SHALL fire with the outcome and winner client ID.

#### Scenario: Win event fires
- **WHEN** a player escapes
- **THEN** `OnMatchEnded` SHALL be invoked with `(MatchOutcome.Win, winnerClientId)`

#### Scenario: Draw event fires
- **WHEN** all players are eliminated
- **THEN** `OnMatchEnded` SHALL be invoked with `(MatchOutcome.Draw, ulong.MaxValue)`

### Requirement: MatchManager drops key when key holder is eliminated
When `OnPlayerEliminated` is called for a client that holds the key, MatchManager SHALL call `KeyManager.DropKey` with the eliminated player's position.

#### Scenario: Key holder elimination drops key
- **WHEN** the key holder is eliminated
- **THEN** `KeyManager.CurrentKeyState.Value` SHALL transition to `KeyState.Dropped`
- **THEN** `KeyManager.KeyHolderClientId.Value` SHALL equal `ulong.MaxValue`
