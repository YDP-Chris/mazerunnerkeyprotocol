## ADDED Requirements

### Requirement: Exit placement on maze edge
The system SHALL place exactly one exit on the maze edge by opening a wall on the boundary and creating an ExitSpawnPoint GameObject with the ExitSpawnPoint component.

#### Scenario: Exit at maze boundary
- **WHEN** the placement pipeline runs
- **THEN** one ExitSpawnPoint is created at a maze edge cell with the boundary wall removed to create a passage out

#### Scenario: Exit accessible
- **WHEN** exit is placed
- **THEN** the exit position is reachable from any cell in the maze via the carved corridors

### Requirement: Player spawn placement in dead-ends
The system SHALL place player spawn points in dead-end cells (cells with exactly one opening), maximizing distance between spawn points. Each spawn point SHALL have a PlayerSpawnPoint component.

#### Scenario: Spawn point count
- **WHEN** the placement pipeline runs on a 20x20 maze
- **THEN** at least 4 and up to 8 PlayerSpawnPoint GameObjects are created in dead-end cells

#### Scenario: Spawn point separation
- **WHEN** multiple spawn points are placed
- **THEN** each spawn point is placed using a greedy max-distance algorithm, choosing the dead-end farthest from all previously placed spawn points

#### Scenario: Spawn point orientation
- **WHEN** a spawn point is placed in a dead-end
- **THEN** the spawn point's forward direction faces toward the corridor opening

### Requirement: Pipeline execution order
The placement pipeline SHALL execute in a fixed order: (1) exit placement, (2) player spawn placement. Key, enemy, and loot placement are handled by existing runtime systems after NavMesh is baked.

#### Scenario: Order guarantee
- **WHEN** maze generation completes
- **THEN** exit is placed before player spawns, and both complete before NavMesh baking begins
