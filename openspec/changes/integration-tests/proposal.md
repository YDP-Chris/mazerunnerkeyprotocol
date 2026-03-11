## Why

Phase 1 gameplay systems (enemy AI, key/exit flow, combat, loot) are implemented but have no automated test coverage. Manual playtesting cannot reliably catch regressions in state machine transitions, network-synced pickup logic, or race conditions in loot box claiming. Play Mode integration tests are needed now, before Phase 2 multiplayer work compounds the cost of finding bugs.

## What Changes

- Add Unity Test Framework Play Mode test assemblies and infrastructure under `Assets/Tests/PlayMode/`
- Create a reusable test scene setup utility that bootstraps a NetworkManager in host mode for testing NetworkBehaviour components
- Write integration tests covering all core gameplay systems:
  - **Enemy AI:** EnemyStateMachine full 5-state transition cycle (PATROL, INVESTIGATE, CHASE, ATTACK, SEARCH) with proper enter/exit callbacks
  - **Enemy Perception:** EnemyPerception cone-based detection, range falloff, and wall occlusion via raycasts
  - **Sound System:** SoundEventSystem broadcast/subscription, radius-based filtering, maze wall blocking
  - **Key Flow:** KeyManager pickup trigger, key holder tracking, key drop on elimination
  - **Exit Sequence:** ExitGateway escape animation, 2-3 second timer, damage interruption canceling escape
  - **Match State:** MatchManager win condition (escape with key), draw condition (all eliminated), match end broadcast
  - **Loot Boxes:** LootBox ServerRpc pickup validation, single-use enforcement, race condition handling when two players contact simultaneously
  - **Weapons:** WeaponInventory switching, ammo tracking and sync, weapon slot management
  - **Combat:** PlayerCombat multi-weapon firing modes (single-shot vs automatic rate limiting, single-ray vs multi-ray shotgun spread)
  - **Player Health:** PlayerHealth damage application, heal clamping to max, elimination threshold and callback

## Capabilities

### New Capabilities
- `play-mode-test-infrastructure`: Reusable test scene setup, NetworkManager host-mode bootstrap, test helper utilities, assembly definition for Play Mode tests
- `enemy-integration-tests`: Tests for EnemyStateMachine transitions, EnemyPerception detection/occlusion, and SoundEventSystem broadcast
- `key-exit-integration-tests`: Tests for KeyManager pickup/drop flow, ExitGateway escape sequence with damage interruption, MatchManager win/draw conditions
- `combat-loot-integration-tests`: Tests for LootBox pickup validation and race conditions, WeaponInventory switching and ammo, PlayerCombat firing modes, PlayerHealth damage/heal/elimination

### Modified Capabilities

## Impact

- **New files:** Test scripts in `Assets/Tests/PlayMode/`, assembly definition file, test scene(s)
- **Dependencies:** Unity Test Framework package (likely already included with Unity 6.3), Netcode for GameObjects (already in project)
- **Build:** Test assemblies are editor/test-only; no impact on game builds
- **Existing code:** No modifications to production scripts; tests exercise existing public APIs only
