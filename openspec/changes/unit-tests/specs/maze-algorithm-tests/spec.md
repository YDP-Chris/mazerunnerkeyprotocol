## ADDED Requirements

### Requirement: MazeGrid extraction
A `MazeGrid` class SHALL be extracted from `MazeGenerator` containing the pure Recursive Backtracker algorithm, operating on wall arrays without any Unity dependencies.

#### Scenario: MazeGrid generates without Unity
- **WHEN** `MazeGrid.Generate(width, height, seed)` is called
- **THEN** it SHALL return a valid grid with horizontalWalls and verticalWalls arrays without requiring any MonoBehaviour or Unity scene

### Requirement: All cells visited
Tests SHALL verify that the Recursive Backtracker visits every cell in the grid.

#### Scenario: Small grid fully visited
- **WHEN** a 5x5 maze is generated
- **THEN** all 25 cells SHALL be marked as visited

#### Scenario: Large grid fully visited
- **WHEN** a 20x20 maze is generated
- **THEN** all 400 cells SHALL be marked as visited

#### Scenario: Non-square grid fully visited
- **WHEN** a 10x15 maze is generated
- **THEN** all 150 cells SHALL be marked as visited

### Requirement: Full connectivity
Tests SHALL verify that every cell is reachable from every other cell (the maze is a spanning tree).

#### Scenario: Any two cells are connected
- **WHEN** a maze is generated and a flood-fill is started from cell (0,0)
- **THEN** the flood-fill SHALL reach all cells in the grid (no isolated regions)

#### Scenario: Connectivity with different seeds
- **WHEN** mazes are generated with 5 different seeds
- **THEN** all 5 mazes SHALL pass the connectivity test

### Requirement: Dead-end detection
Tests SHALL verify that dead-end cells (exactly one opening) are correctly identified.

#### Scenario: Dead-ends exist in generated maze
- **WHEN** a 10x10 maze is generated
- **THEN** the dead-end count SHALL be greater than 0

#### Scenario: Dead-end has exactly one opening
- **WHEN** a cell is identified as a dead-end
- **THEN** it SHALL have exactly 1 wall removed (1 passage to an adjacent cell)

### Requirement: Seed determinism
Tests SHALL verify that the same seed produces the identical maze layout.

#### Scenario: Same seed same walls
- **WHEN** a maze is generated twice with seed 12345
- **THEN** the horizontalWalls and verticalWalls arrays SHALL be identical

#### Scenario: Different seeds produce different mazes
- **WHEN** two mazes are generated with different seeds
- **THEN** the wall arrays SHALL differ in at least one position
