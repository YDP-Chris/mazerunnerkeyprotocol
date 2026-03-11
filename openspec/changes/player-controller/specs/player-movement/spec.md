## ADDED Requirements

### Requirement: Character Controller Setup

The player prefab SHALL use a Unity `CharacterController` component for collision and movement. The player script SHALL inherit from `NetworkBehaviour`. The `CharacterController` MUST have a capsule collider shape with a height of 2.0 units and radius of 0.5 units. The `skinWidth` MUST be set to 0.08 or less to prevent wall-clipping in narrow maze corridors.

#### Scenario: Player prefab instantiation

- **WHEN** the player prefab is instantiated in the scene
- **THEN** it SHALL have a `CharacterController` component attached with height 2.0 and radius 0.5

#### Scenario: Player placed in maze corridor

- **WHEN** the player is placed in a corridor that is 3 Unity units wide
- **THEN** the player MUST be able to move through the corridor without getting stuck on walls

#### Scenario: NetworkBehaviour base class

- **WHEN** the `PlayerMovement` script is inspected
- **THEN** it SHALL inherit from `Unity.Netcode.NetworkBehaviour`, not `MonoBehaviour`

---

### Requirement: WASD Movement

The player SHALL move in the horizontal plane using WASD keys (or equivalent Input System bindings for Move action). Movement direction SHALL be relative to the camera's forward direction projected onto the horizontal plane. The player SHALL have a default walk speed of 5.0 units per second, configurable via a serialized field.

#### Scenario: Forward movement

- **WHEN** the player presses the W key
- **THEN** the player SHALL move in the camera's forward direction (projected onto the XZ plane) at 5.0 units per second

#### Scenario: Strafing

- **WHEN** the player presses the A or D key
- **THEN** the player SHALL move perpendicular to the camera's forward direction (left or right respectively) at 5.0 units per second

#### Scenario: Diagonal movement normalization

- **WHEN** the player presses W and D simultaneously
- **THEN** the movement vector SHALL be normalized so the player does not exceed 5.0 units per second diagonally

#### Scenario: No input

- **WHEN** no movement keys are pressed
- **THEN** the player SHALL remain stationary and not drift

#### Scenario: Camera-relative direction

- **WHEN** the player rotates the camera 90 degrees to the right and presses W
- **THEN** the player SHALL move in the new camera forward direction, not the original world forward

---

### Requirement: Sprint

The player SHALL be able to sprint by holding the Left Shift key (bound to the Sprint action in the Input System). Sprint speed SHALL be 8.0 units per second, configurable via a serialized field. Sprint SHALL only apply when the player is moving forward (positive forward input component).

#### Scenario: Sprint activation

- **WHEN** the player holds Left Shift while pressing W
- **THEN** the player's movement speed SHALL increase to 8.0 units per second

#### Scenario: Sprint requires forward input

- **WHEN** the player holds Left Shift but only presses A (strafe left) with no forward input
- **THEN** the player SHALL move at normal walk speed (5.0 units per second), not sprint speed

#### Scenario: Sprint release

- **WHEN** the player releases Left Shift while moving
- **THEN** the player's speed SHALL immediately return to 5.0 units per second

#### Scenario: Sprint while stationary

- **WHEN** the player holds Left Shift but presses no movement keys
- **THEN** the player SHALL remain stationary

---

### Requirement: Gravity

The player SHALL be affected by gravity when not grounded. Gravity SHALL be applied at 9.81 units per second squared downward. The `CharacterController.isGrounded` property SHALL be used to determine grounding state. A small downward velocity (-1.0) SHALL be applied even when grounded to maintain ground contact.

#### Scenario: Player on flat ground

- **WHEN** the player stands on a flat floor surface
- **THEN** `CharacterController.isGrounded` SHALL return true and the player SHALL not fall

#### Scenario: Player walks off edge

- **WHEN** the player walks off a ledge or elevated surface
- **THEN** the player SHALL fall downward at an accelerating rate due to gravity until grounded again

#### Scenario: Grounding stick force

- **WHEN** the player is grounded and moving along a surface
- **THEN** a small downward velocity SHALL be applied each frame to prevent the player from "floating" over minor surface irregularities

---

### Requirement: Player Rotation

The player model SHALL rotate to face the direction of movement. Rotation SHALL be smoothed using `Quaternion.Slerp` or equivalent with a configurable rotation speed (default 10.0). When the player is stationary, rotation SHALL remain at the last facing direction.

#### Scenario: Player moves forward

- **WHEN** the player moves in any direction
- **THEN** the player model SHALL smoothly rotate to face that movement direction

#### Scenario: Player stops moving

- **WHEN** the player releases all movement keys
- **THEN** the player model SHALL maintain its current rotation and not snap to a default direction

#### Scenario: Rapid direction change

- **WHEN** the player reverses movement direction (e.g., from W to S)
- **THEN** the player model SHALL smoothly rotate 180 degrees rather than instantly snapping

---

### Requirement: Cinemachine 3rd-Person Camera

The scene SHALL use a Cinemachine `CinemachineCamera` component with 3rd Person Follow body to follow the player. The camera SHALL orbit behind and above the player. Mouse input (Look action) SHALL control camera orbit. The camera SHALL handle collision with maze walls to prevent clipping through geometry.

#### Scenario: Camera follows player

- **WHEN** the player moves through the maze
- **THEN** the camera SHALL follow behind the player at a configurable offset (default: shoulder offset X=0.5, Y=1.5, camera distance 4.0)

#### Scenario: Mouse look horizontal

- **WHEN** the player moves the mouse horizontally
- **THEN** the camera SHALL orbit around the player on the horizontal axis

#### Scenario: Mouse look vertical

- **WHEN** the player moves the mouse vertically
- **THEN** the camera SHALL pitch up and down, clamped between -30 and 70 degrees to prevent over-rotation

#### Scenario: Camera wall collision

- **WHEN** a maze wall is between the camera's desired position and the player
- **THEN** the camera SHALL move closer to the player to avoid clipping through the wall

#### Scenario: Camera in narrow corridor

- **WHEN** the player enters a corridor that is 3-4 Unity units wide
- **THEN** the camera SHALL adjust its distance to remain outside walls while keeping the player visible

#### Scenario: Cursor lock

- **WHEN** the game is running and the player is alive
- **THEN** the cursor SHALL be locked to the center of the screen and hidden

---

### Requirement: Input System Configuration

All player input SHALL be defined in a Unity Input Actions asset (`PlayerInputActions`). The asset SHALL define a "Player" action map containing at minimum: Move (Vector2), Look (Vector2), Sprint (Button), and Fire (Button). The generated C# class SHALL be used for type-safe input access.

#### Scenario: Input Actions asset exists

- **WHEN** the project is opened in Unity
- **THEN** an Input Actions asset SHALL exist at `Assets/Input/PlayerInputActions.inputactions`

#### Scenario: Move action binding

- **WHEN** the Move action is inspected
- **THEN** it SHALL be bound to WASD keys as a 2D composite and SHALL output a `Vector2`

#### Scenario: Look action binding

- **WHEN** the Look action is inspected
- **THEN** it SHALL be bound to Mouse Delta and SHALL output a `Vector2`

#### Scenario: Fire action binding

- **WHEN** the Fire action is inspected
- **THEN** it SHALL be bound to Left Mouse Button

#### Scenario: Sprint action binding

- **WHEN** the Sprint action is inspected
- **THEN** it SHALL be bound to Left Shift

---

### Requirement: Owner Authority Check

The `PlayerMovement` script SHALL check `IsOwner` before processing any input. If `IsOwner` is false, the script SHALL skip input reading and movement application. This ensures correct behavior when multiplayer is enabled in later phases.

#### Scenario: Local player input

- **WHEN** `IsOwner` is true for the player instance
- **THEN** input SHALL be read and movement SHALL be applied

#### Scenario: Remote player instance

- **WHEN** `IsOwner` is false for a player instance
- **THEN** no input SHALL be read and no local movement SHALL be applied to that instance

---

### Requirement: Elimination Stops Movement

When the player is eliminated (health reaches zero), all movement input SHALL be disabled. The player SHALL not be able to move, sprint, or rotate after elimination.

#### Scenario: Player eliminated

- **WHEN** the player's health reaches zero
- **THEN** all movement input SHALL be ignored and the player SHALL stop moving immediately

#### Scenario: Movement input after death

- **WHEN** an eliminated player presses WASD or Sprint keys
- **THEN** no movement SHALL occur
