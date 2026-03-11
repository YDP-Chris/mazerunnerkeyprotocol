## ADDED Requirements

### Requirement: Maze Grid Dimensions

The static maze SHALL be constructed on a 20x20 cell grid where each cell measures 4x4 Unity units, producing a total maze footprint of 80x80 Unity units. All walls and floors MUST align to this grid without fractional offsets.

#### Scenario: Grid alignment verification

- **WHEN** the maze scene is loaded in the Unity Editor
- **THEN** every wall segment and floor tile position SHALL have X and Z coordinates that are multiples of 4.0 Unity units

#### Scenario: Total maze bounds

- **WHEN** the maze geometry bounding box is measured
- **THEN** the maze SHALL fit within an 80x80 Unity unit footprint on the XZ plane

#### Scenario: Cell count

- **WHEN** the maze layout is inspected
- **THEN** the grid SHALL contain exactly 20 columns and 20 rows of cells, totaling 400 cells

---

### Requirement: Corridor Width

All navigable corridors in the maze MUST have a minimum clear width of 4 Unity units to accommodate 3rd-person character movement and camera positioning. No corridor SHALL be narrower than 3 Unity units at any point.

#### Scenario: Standard corridor passage

- **WHEN** a player character (approximately 1 Unity unit wide) moves through any corridor
- **THEN** there SHALL be at least 1.5 Unity units of clearance on each side of the character

#### Scenario: Corridor width at chokepoints

- **WHEN** a corridor narrows to form a chokepoint
- **THEN** the narrowest passable width SHALL be no less than 3 Unity units

#### Scenario: Camera clearance in corridors

- **WHEN** the 3rd-person camera follows the player through any corridor
- **THEN** the camera SHALL not clip through walls due to insufficient corridor width

---

### Requirement: Wall Height

All maze walls MUST have a uniform height of 4 Unity units. Walls SHALL extend from the floor plane (Y=0) to Y=4.

#### Scenario: Wall occlusion

- **WHEN** the 3rd-person camera is positioned at its default follow distance and angle behind the player
- **THEN** the camera SHALL NOT be able to see over maze walls into adjacent corridors

#### Scenario: Consistent wall height

- **WHEN** any two wall segments in the maze are compared
- **THEN** both walls SHALL have identical height of 4 Unity units

---

### Requirement: Maze Layout Variety

The static maze MUST include a mix of tactical environments: corridors, chokepoints, dead-ends, and open areas. The layout SHALL create opportunities for ambush, cover-based combat, and exploration tension.

#### Scenario: Dead-end count

- **WHEN** the maze layout is analyzed
- **THEN** the maze SHALL contain at least 8 dead-end corridors suitable for player spawn placement and ambush scenarios

#### Scenario: Chokepoint presence

- **WHEN** the maze layout is analyzed
- **THEN** the maze SHALL contain at least 4 chokepoints where corridors narrow or intersect, creating natural defensive positions

#### Scenario: Open area presence

- **WHEN** the maze layout is analyzed
- **THEN** the maze SHALL contain at least 2 open areas spanning 2x2 cells (8x8 Unity units) or larger, suitable for multi-player combat encounters

#### Scenario: No isolated sections

- **WHEN** any two walkable cells in the maze are selected
- **THEN** there SHALL exist a navigable path between them (the maze MUST be fully connected)

---

### Requirement: Maze Prefabs

The maze MUST be constructed from a defined set of reusable prefabs stored in `Assets/Prefabs/Maze/`. Each prefab SHALL have consistent dimensions aligned to the 4-unit grid.

#### Scenario: Wall prefab dimensions

- **WHEN** a straight wall prefab is instantiated
- **THEN** it SHALL measure 4 Unity units in length, 4 Unity units in height, and between 0.2 and 0.5 Unity units in thickness

#### Scenario: Floor tile prefab dimensions

- **WHEN** a floor tile prefab is instantiated
- **THEN** it SHALL measure 4x4 Unity units on the XZ plane with a thickness between 0.1 and 0.3 Unity units

#### Scenario: Prefab reuse

- **WHEN** the maze scene hierarchy is inspected
- **THEN** all maze geometry SHALL be instances of prefabs from `Assets/Prefabs/Maze/`, not loose primitives or one-off meshes

#### Scenario: Corner wall prefab

- **WHEN** two walls meet at a 90-degree angle
- **THEN** a corner wall or pillar prefab SHALL fill the junction to prevent visual gaps between wall segments

---

### Requirement: URP Materials

All maze surfaces MUST use URP-compatible materials with the Universal Render Pipeline Lit shader. Materials SHALL use flat solid colors without textures for Phase 1.

#### Scenario: Wall material

- **WHEN** a wall prefab is rendered
- **THEN** it SHALL use a URP Lit material with a flat gray base color (approximate RGB range: 0.5-0.7 per channel) and no texture maps assigned

#### Scenario: Floor material

- **WHEN** a floor tile prefab is rendered
- **THEN** it SHALL use a URP Lit material with a flat color visually distinct from walls (darker gray or muted tone), enabling players to distinguish floor from walls at a glance

#### Scenario: Material rendering in URP

- **WHEN** the maze scene is rendered using the URP pipeline
- **THEN** all materials SHALL render without pink/magenta error shading, confirming URP shader compatibility

---

### Requirement: Maze Lighting

The maze scene MUST include lighting sufficient for gameplay visibility in all navigable areas. Lighting SHALL use URP-compatible light sources.

#### Scenario: Corridor illumination

- **WHEN** a player is in any corridor of the maze
- **THEN** the corridor SHALL be lit well enough to identify other players, enemies, and loot boxes at a distance of at least 8 Unity units (2 cells)

#### Scenario: No fully dark areas

- **WHEN** any navigable area of the maze is inspected
- **THEN** there SHALL be no area with zero illumination -- ambient light or placed lights MUST provide minimum visibility everywhere

#### Scenario: URP light compatibility

- **WHEN** the scene lighting is configured
- **THEN** all light sources SHALL be compatible with URP (no legacy light modes) and the scene SHALL use a URP-compatible lighting setup (e.g., directional light with URP shadow settings)

---

### Requirement: Scene Structure

The static maze SHALL be contained in a single Unity scene at `Assets/Scenes/TestMaze.unity`. The scene hierarchy MUST be organized with clearly named parent GameObjects for each category of content.

#### Scenario: Scene file location

- **WHEN** the project is opened in Unity
- **THEN** a scene file SHALL exist at `Assets/Scenes/TestMaze.unity`

#### Scenario: Hierarchy organization

- **WHEN** the scene hierarchy is inspected
- **THEN** maze geometry SHALL be grouped under a parent GameObject named "Maze" or "MazeGeometry"
- **THEN** spawn points SHALL be grouped under a parent GameObject named "SpawnPoints"
- **THEN** lighting objects SHALL be grouped under a parent GameObject named "Lighting"

#### Scenario: No loose objects

- **WHEN** the scene hierarchy root is inspected
- **THEN** there SHALL be no ungrouped game objects at the root level other than designated parent containers and the Main Camera
