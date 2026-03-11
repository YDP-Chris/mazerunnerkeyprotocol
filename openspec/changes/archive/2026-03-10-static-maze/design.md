## Context

Phase 1 of Maze Runner: Key Protocol requires a playable maze environment to develop and test all core systems: player movement, combat, key/exit mechanics, enemy AI pathfinding, and loot box placement. Procedural maze generation is scoped for Phase 2, so Phase 1 needs a hand-built static maze that establishes the spatial, visual, and navigational standards the procedural system must later replicate.

The maze must support 3rd-person shooter gameplay with natural cover, chokepoints for ambushes, dead-ends for tension, and open areas for combat encounters. It must accommodate 2-8 players spawning at maximum distance from each other, a key spawn point isolated from both player spawns and the exit, loot box positions distributed throughout, and enemy patrol waypoints connected by walkable NavMesh paths.

The project uses Unity 6.3 LTS with the Universal Render Pipeline (URP). All maze geometry, spawn markers, and NavMesh data live in a single test scene.

## Goals / Non-Goals

**Goals:**
- Build a static test maze scene that validates all Phase 1 gameplay systems end-to-end
- Establish corridor width (3-4 Unity units), wall height, and tile dimensions as spatial standards for the future procedural generator
- Provide tactical variety through a mix of corridors, chokepoints, dead-ends, and open areas
- Place configurable spawn points for all game entities: players, key, exit, loot boxes, and enemy waypoints
- Bake a NavMesh that enemy AI can navigate without getting stuck on corners or walls
- Set up basic URP materials and lighting so the maze is visually readable during development
- Create reusable wall and floor prefabs that the procedural generator can adopt in Phase 2

**Non-Goals:**
- Procedural maze generation (Phase 2)
- Final art, textures, or visual polish
- Multiplayer networking or syncing (Phase 3+)
- Sound design or audio
- Minimap or HUD elements
- Runtime maze modification or destruction

## Decisions

1. **Grid-based layout on a 20x20 cell grid.** Each cell is 4x4 Unity units. This gives corridors a consistent 4-unit width, meeting the minimum for 3rd-person character movement with camera clearance. The total maze footprint is 80x80 Unity units.

2. **Wall height of 4 Unity units.** Walls match corridor width for proportional aesthetics. Tall enough to block the 3rd-person camera from seeing over them, preserving exploration tension. Low enough that lighting fills corridors without deep shadow artifacts.

3. **Prefab-based construction.** Maze walls and floors are built from a small set of prefabs (straight wall, corner wall, floor tile, pillar). This ensures consistent dimensions, simplifies the static build, and provides the exact prefab library the procedural generator will instantiate in Phase 2.

4. **Spawn points as empty GameObjects with custom editor gizmos.** Each spawn type (player, key, exit, loot box, enemy waypoint) uses a dedicated MonoBehaviour component with a gizmo color and icon. This keeps spawn data in the scene hierarchy, visible in the editor, and queryable at runtime without extra configuration files.

5. **Single scene architecture.** All maze geometry, spawn points, lighting, and NavMesh data live in `Assets/Scenes/TestMaze.unity`. No additive scene loading for Phase 1.

6. **NavMesh baked at edit time, not runtime.** The static maze does not change, so a pre-baked NavMesh is simpler and faster than runtime baking. Runtime NavMesh baking will be introduced in Phase 2 when procedural generation requires it.

7. **URP Lit material with flat colors.** Walls and floors use a single URP Lit shader with distinct flat colors (gray walls, darker floor). No textures in Phase 1 -- keeps the project lightweight and avoids asset pipeline complexity.

## Risks / Trade-offs

1. **Static maze does not test procedural generation edge cases.** The hand-built maze will have well-formed corridors and intentional dead-ends. The procedural generator in Phase 2 may produce degenerate layouts (unreachable areas, too-narrow corridors) that the static maze never exercises. Mitigation: enforce the same spatial constraints (cell size, corridor width) in both the static build and the procedural algorithm.

2. **Single maze layout limits replayability testing.** Testers will memorize the layout quickly. This is acceptable for Phase 1 since the goal is system validation, not session variety. The procedural generator in Phase 2 resolves this.

3. **Prefab dimensions are a commitment.** Changing the 4x4 cell size or 4-unit wall height later would require rebuilding the static maze and updating the procedural generator. Risk is low because 4 units provides comfortable 3rd-person movement clearance and is a clean integer for grid math.

4. **Edit-time NavMesh bake must be redone if maze geometry changes.** Any wall or floor adjustment requires a re-bake. This is a minor workflow friction for Phase 1 iteration. Mitigation: document the re-bake step in the development workflow.

5. **No vertical gameplay.** The static maze is single-level with uniform wall height. Ramps, elevated platforms, or multi-story sections are out of scope. If vertical gameplay proves desirable, it would require significant rework of both the static maze and the procedural generator.
