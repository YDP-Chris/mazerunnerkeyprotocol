## ADDED Requirements

### Requirement: Edit Mode test assembly definition
The project SHALL have an assembly definition file at `Assets/Tests/EditMode/EditModeTests.asmdef` that references the game's main assembly and Unity Test Framework (NUnit).

#### Scenario: Assembly definition is valid
- **WHEN** the assembly definition is loaded by Unity
- **THEN** it SHALL reference `com.unity.test-framework` and the game assembly, and SHALL be configured for Editor platform only

#### Scenario: Test discovery
- **WHEN** test scripts are placed in `Assets/Tests/EditMode/`
- **THEN** Unity Test Runner SHALL discover and list all `[Test]` and `[TestFixture]` methods in the Edit Mode tab

### Requirement: Folder structure
The test project SHALL organize tests in `Assets/Tests/EditMode/` with one test file per system under test.

#### Scenario: Folder exists
- **WHEN** the project is opened in Unity
- **THEN** `Assets/Tests/EditMode/` SHALL exist and contain test scripts

### Requirement: No scene dependencies
All Edit Mode tests SHALL run without loading any scene or requiring Play Mode.

#### Scenario: Tests run in Edit Mode
- **WHEN** tests are executed via Unity Test Runner in Edit Mode
- **THEN** all tests SHALL pass without any scene being loaded and without entering Play Mode
