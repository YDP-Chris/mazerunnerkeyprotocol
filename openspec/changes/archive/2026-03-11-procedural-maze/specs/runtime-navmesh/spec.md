## ADDED Requirements

### Requirement: Runtime NavMesh baking
The system SHALL bake a NavMesh at runtime after all maze geometry and spawn points are placed, using Unity's NavMeshSurface component.

#### Scenario: NavMesh covers walkable area
- **WHEN** NavMesh baking completes
- **THEN** all floor tiles in the maze are covered by the NavMesh, and wall positions are excluded

#### Scenario: NavMesh validity for AI
- **WHEN** EnemySpawner samples the NavMesh after baking
- **THEN** NavMesh.SamplePosition returns true for positions on maze corridors

### Requirement: Maze ready event
The system SHALL fire an event or callback when the full pipeline completes (geometry + placement + NavMesh bake), signaling that spawners can proceed.

#### Scenario: Spawners wait for maze ready
- **WHEN** the maze ready event fires
- **THEN** KeyManager, EnemySpawner, and LootBoxSpawner can begin their placement logic with a valid NavMesh and spawn point references available

#### Scenario: Spawners do not run early
- **WHEN** NavMesh has not yet been baked
- **THEN** spawners do not attempt placement (no NavMesh sampling failures)

### Requirement: NavMesh bake performance
The NavMesh bake SHALL complete within 2 seconds for a 20x20 maze on target hardware.

#### Scenario: Acceptable generation time
- **WHEN** a 20x20 maze is generated and NavMesh is baked
- **THEN** the total time from generation start to maze ready event is under 2 seconds
