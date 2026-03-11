## ADDED Requirements

### Requirement: Maze grid generation using Recursive Backtracker
The system SHALL generate a maze grid using the Recursive Backtracker (DFS) algorithm with an iterative stack-based implementation. The algorithm SHALL produce a perfect maze (exactly one path between any two cells).

#### Scenario: Default maze generation
- **WHEN** a match starts
- **THEN** the system generates a 20x20 cell maze grid with walls between cells carved by the Recursive Backtracker algorithm

#### Scenario: All cells reachable
- **WHEN** maze generation completes
- **THEN** every cell in the grid is reachable from every other cell (perfect maze property)

### Requirement: Configurable maze parameters
The system SHALL support configurable maze parameters: grid width, grid height, corridor width (in Unity units), and wall height.

#### Scenario: Custom grid size
- **WHEN** grid size is set to 15x25
- **THEN** the generated maze has 15 columns and 25 rows of cells

#### Scenario: Default parameters
- **WHEN** no custom parameters are specified
- **THEN** the maze uses defaults: 20x20 grid, 4 units per cell, 4 unit wall height

### Requirement: Seeded random generation
The system SHALL accept an integer seed for the random number generator. The same seed SHALL always produce the same maze layout.

#### Scenario: Reproducible maze from seed
- **WHEN** two mazes are generated with seed 12345 and identical parameters
- **THEN** both mazes have identical wall configurations

#### Scenario: Random seed when none specified
- **WHEN** no seed is provided
- **THEN** the system generates a random seed and logs it to the console for debug reproducibility

### Requirement: Runtime geometry instantiation
The system SHALL instantiate wall, floor, and pillar prefabs at runtime to create the physical maze. All geometry SHALL be organized under a parent "Maze" GameObject.

#### Scenario: Geometry matches grid
- **WHEN** maze generation completes
- **THEN** wall GameObjects exist at every position where the logical grid has a wall, floor tiles cover all cells, and corner pillars exist at wall intersections

#### Scenario: Clean regeneration
- **WHEN** a new maze is generated while an existing maze exists
- **THEN** the old maze geometry is destroyed before new geometry is created

### Requirement: Maze world bounds exposure
The system SHALL expose the maze's world-space bounds (min/max X and Z coordinates) as static properties accessible by other systems.

#### Scenario: Spawner reads bounds
- **WHEN** EnemySpawner or LootBoxSpawner queries maze bounds after generation
- **THEN** it receives correct world-space min/max values based on grid size and cell size
