## ADDED Requirements

### Requirement: Escape Victory

A player SHALL win the match by holding the key and completing the escape animation at the exit. This is the only way to achieve a victory in a match.

#### Scenario: Key holder completes escape

- **WHEN** the key holder's escape animation timer reaches zero without interruption
- **THEN** the host SHALL declare that player as the match winner, broadcast a match-end RPC to all clients with the winner's player ID, and transition all clients to the match results screen

#### Scenario: Winner is recorded

- **WHEN** a player wins by escaping
- **THEN** the system SHALL record the escape as a win for that player in the session results, incrementing their escape count (the only tracked competitive metric)

#### Scenario: Non-holders cannot win

- **WHEN** a player who does not hold the key attempts to interact with the exit
- **THEN** no escape animation SHALL begin and no win condition SHALL be evaluated for that player

---

### Requirement: Draw by Total Elimination

If all players are eliminated before anyone escapes, the match SHALL end in a draw with no winner.

#### Scenario: All players eliminated, no key holder

- **WHEN** all players in the match have been eliminated and no player held the key at the time the last player was eliminated
- **THEN** the host SHALL declare the match a draw, broadcast a match-end RPC with no winner, and transition all clients to the match results screen indicating "No Escape - Draw"

#### Scenario: Key holder eliminated as last player

- **WHEN** the key holder is the last surviving player and is eliminated (e.g., by enemy AI)
- **THEN** the host SHALL declare the match a draw, as there are no remaining players to pick up the key and escape

#### Scenario: Last two players eliminate each other simultaneously

- **WHEN** the final two players deal lethal damage to each other on the same server tick
- **THEN** the host SHALL declare the match a draw, regardless of which player held the key

---

### Requirement: Match-End Sequence

When a match ends (by escape or total elimination), the system SHALL execute a consistent match-end sequence that stops gameplay and presents results.

#### Scenario: Match ends by escape

- **WHEN** the host declares an escape victory
- **THEN** the system SHALL freeze all player input within 0.5 seconds, display the winner's escape animation to all clients, show the match results screen with the winner identified, and disable all combat and movement for all players

#### Scenario: Match ends by draw

- **WHEN** the host declares a draw
- **THEN** the system SHALL freeze all player input within 0.5 seconds, display a "No Escape" message to all clients, show the match results screen indicating no winner, and disable all combat and movement

#### Scenario: Enemy AI stops on match end

- **WHEN** the match-end sequence begins (regardless of outcome)
- **THEN** all enemy AI agents SHALL transition to an idle state and stop pursuing, attacking, or patrolling

#### Scenario: Match results are displayed to all clients

- **WHEN** the match-end sequence completes
- **THEN** every connected client SHALL see a results screen showing: match outcome (win or draw), the winning player's name (if applicable), and match duration

#### Scenario: Match-end is host-authoritative

- **WHEN** a potential match-end condition is detected
- **THEN** only the host SHALL evaluate and declare the match result; clients SHALL NOT independently determine match outcomes

---

### Requirement: Key Holder Elimination During Match

When the key holder is eliminated but other players survive, the match SHALL continue with the key dropped.

#### Scenario: Key holder eliminated with survivors remaining

- **WHEN** the key holder is eliminated and at least one other player is still alive
- **THEN** the match SHALL NOT end; the key SHALL drop at the holder's position, and the match SHALL continue until a player escapes or all remaining players are eliminated

#### Scenario: Key holder eliminated, one survivor remains

- **WHEN** the key holder is eliminated and exactly one other player remains alive
- **THEN** the match SHALL continue; the surviving player can pick up the dropped key and attempt to escape; the match does NOT auto-award victory to the last survivor

#### Scenario: Multiple key holder eliminations in one match

- **WHEN** a second (or subsequent) key holder is eliminated during the same match
- **THEN** the key SHALL drop again following the same drop rules, and the match SHALL continue as long as at least one player survives
