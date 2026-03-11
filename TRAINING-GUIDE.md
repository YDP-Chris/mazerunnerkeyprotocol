# Training Guide: AI-Accelerated Unity Development with Claude Code + OpenSpec

**What this guide covers:** How to build a complete game prototype in hours instead of weeks using Claude Code (AI coding assistant), Unity MCP (live Unity Editor bridge), and OpenSpec (change management workflow).

**What you need before starting:**
- Basic Unity knowledge (you know what GameObjects, prefabs, and components are)
- A computer running Windows 10/11 (macOS works too with path adjustments)
- Node.js installed (for OpenSpec)
- Git installed and a GitHub account

**What was built using this workflow:** Maze Runner: Key Protocol -- a 3rd-person multiplayer maze shooter. Phase 1 (the complete core loop) was built in roughly 2.5 hours: 286 tasks across 5 systems (static maze, player controller, key/exit system, enemy AI, loot boxes).

---

## Table of Contents

1. [Install the Tools](#1-install-the-tools)
2. [Create the Unity Project](#2-create-the-unity-project)
3. [Connect Unity MCP to Claude Code](#3-connect-unity-mcp-to-claude-code)
4. [Write Your PRD in CLAUDE.md](#4-write-your-prd-in-claudemd)
5. [Set Up Version Control](#5-set-up-version-control)
6. [Initialize OpenSpec](#6-initialize-openspec)
7. [Plan Your Changes](#7-plan-your-changes)
8. [Implement Changes](#8-implement-changes)
9. [The Editor Setup Script Pattern](#9-the-editor-setup-script-pattern)
10. [Archive, Commit, Repeat](#10-archive-commit-repeat)
11. [Key Architecture Patterns](#11-key-architecture-patterns)
12. [Troubleshooting](#12-troubleshooting)
13. [Reference: Full Project Structure](#13-reference-full-project-structure)

---

## 1. Install the Tools

### Claude Code (CLI)

Claude Code is Anthropic's command-line AI assistant. It reads your codebase, writes code, and executes commands.

```bash
# Install Claude Code globally
npm install -g @anthropic-ai/claude-code
```

After install, run `claude` in any terminal to start a session. You will need an Anthropic API key or a Claude Pro/Max subscription.

### OpenSpec

OpenSpec is a change management workflow tool. It breaks work into structured changes with proposals, designs, specs, and task lists -- then tracks implementation.

```bash
# Install OpenSpec globally
npm install -g openspec
```

### Unity 6.3 LTS

Download Unity 6.3 LTS (6000.3.x) from Unity Hub. When creating your project, select the **Universal Render Pipeline (URP)** template. This matters -- URP is lighter weight and better suited for this kind of project.

### Required Unity Packages

Open your Unity project, then install these via **Window > Package Manager**:

| Package | Purpose |
|---------|---------|
| Netcode for GameObjects | Multiplayer networking framework |
| Cinemachine | Camera system (3rd-person follow) |
| Input System | Modern input handling (replaces legacy Input) |
| TextMesh Pro | UI text rendering |

For Netcode for GameObjects: Add it from the Unity Registry tab in Package Manager. If it does not appear, add it by name: `com.unity.netcode.gameobjects`.

### Unity MCP (IvanMurzak)

This is the bridge that lets Claude Code talk directly to your Unity Editor. It exposes Unity operations (create objects, run scripts, inspect hierarchy) as tools Claude can call.

Install via OpenUPM. In your Unity project's `Packages/manifest.json`, add the OpenUPM scoped registry and the package:

```json
{
  "scopedRegistries": [
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.ivanmurzak"
      ]
    }
  ],
  "dependencies": {
    "com.ivanmurzak.unity.mcp": "0.51.5"
  }
}
```

Save the file. Unity will download and install the package. You should see a Unity MCP panel appear (accessible via **Window > Unity MCP** or similar).

---

## 2. Create the Unity Project

1. Open Unity Hub
2. Click **New Project**
3. Select Unity 6.3 LTS
4. Choose the **Universal 3D (URP)** template
5. Name your project and choose a location
6. Click **Create project**

Once the project opens, install the packages listed in Step 1. Let Unity compile after each install.

Create this folder structure under `Assets/`:

```
Assets/
├── Input/
├── Materials/
├── Prefabs/
├── ScriptableObjects/
├── Scenes/
├── Scripts/
│   ├── Editor/
│   ├── Enemy/
│   ├── GameState/
│   ├── Loot/
│   ├── Network/
│   ├── Player/
│   ├── SpawnPoints/
│   └── UI/
└── Settings/
```

You do not need to create all of these upfront. Claude will create them as needed during implementation. But knowing the target structure helps you understand where things go.

---

## 3. Connect Unity MCP to Claude Code

This is the step most people get wrong. Read carefully.

### How it works

Unity MCP has two parts:
1. **A server executable** that Unity builds into your project's `Library/mcp-server/` folder
2. **A client connection** configured in Claude Code via `.mcp.json`

The transport is **stdio** -- Claude Code launches the server executable directly. Unity must be open and running for the tools to work.

### Configuration steps

1. Open your Unity project. The MCP package should have compiled and generated a server executable at:
   ```
   <YourProject>/Library/mcp-server/win-x64/unity-mcp-server.exe
   ```
   On macOS the path will end in `osx-x64/unity-mcp-server` (no .exe).

2. In the Unity Editor, open the Unity MCP panel. Click **Configure** (or similar button) to generate the configuration.

3. Create a `.mcp.json` file in your **project root** (the folder containing your Unity project folder, not inside the Unity project itself):

```json
{
  "mcpServers": {
    "unity": {
      "args": [
        "port=51406",
        "plugin-timeout=10000",
        "client-transport=stdio",
        "authorization=none"
      ],
      "command": "C:/path/to/your/Unity Project/Library/mcp-server/win-x64/unity-mcp-server.exe"
    }
  }
}
```

**Important:** Use forward slashes in the path, even on Windows. Replace the command path with the actual path to your server executable. If your project path has spaces, the full path in quotes works in the JSON string.

4. Restart Claude Code. When you start a new session in the project directory, Claude should now have access to `mcp__unity` tools. You can verify by asking Claude "What Unity MCP tools do you have available?"

### Verifying the connection

Ask Claude Code:
```
Can you check what's in the Unity hierarchy right now?
```

If MCP is working, Claude will call a Unity tool and return the current scene hierarchy. If it fails, check:
- Is Unity open with the project loaded?
- Is the server exe path correct in `.mcp.json`?
- Did you restart Claude Code after creating `.mcp.json`?

---

## 4. Write Your PRD in CLAUDE.md

This is the most important file in the entire workflow. `CLAUDE.md` sits at the root of your project and Claude Code reads it automatically at the start of every session. It is your product requirements document (PRD), your design bible, and the instructions Claude follows.

### Why CLAUDE.md specifically?

Claude Code has a built-in convention: it reads `CLAUDE.md` from the working directory at session start. No special configuration needed. Whatever you put in this file becomes Claude's persistent context -- every session, every command.

### What goes in it

Your CLAUDE.md should cover:

1. **Game/product overview** -- elevator pitch, what makes it unique, core pillars
2. **Core mechanics** -- detailed rules for every game system (match flow, key behavior, combat, loot, win/loss conditions)
3. **Technical specifications** -- enemy AI state machine, maze generation algorithm, placement rules
4. **Multiplayer architecture** -- networking framework, session model, what state is synced and how
5. **Tech stack** -- exact Unity version, render pipeline, packages, tools
6. **Open questions** -- things not yet decided (forces you to think about them and tells Claude not to assume)
7. **Development phases** -- what gets built when, with clear exit criteria
8. **Current session priorities** -- updated at the start of each working session

### Writing tips

- Be specific. "Enemies patrol the maze" is useless. "5-state machine: PATROL, INVESTIGATE, CHASE, ATTACK, SEARCH with specific triggers between each state" is useful.
- Include constraints. "Loot box placement uses max 50 attempts with a fallback position" prevents Claude from writing infinite loops.
- State architectural decisions upfront. "All game scripts use NetworkBehaviour, not MonoBehaviour" saves you from painful refactoring later.
- Use tables for structured data. Claude parses them well.
- Mark open questions explicitly. This prevents Claude from making assumptions you have not validated.

The CLAUDE.md for this project is roughly 300 lines. That is a reasonable size. Much shorter and Claude lacks context. Much longer and you are probably over-specifying implementation details that should be in OpenSpec specs instead.

---

## 5. Set Up Version Control

```bash
# Initialize git (if not already)
cd /path/to/your/project
git init

# Create .gitignore for Unity
# (Unity Hub usually generates one, but verify it excludes Library/, Temp/, Logs/, obj/)

# Initial commit
git add .
git commit -m "Initial Unity 6.3 URP project setup"

# Create GitHub repo and push
git remote add origin https://github.com/your-username/your-repo.git
git branch -M main
git push -u origin main
```

### Branch strategy

Use one branch per development phase:

```bash
git checkout -b phase-1
```

Commit after each completed change (static-maze, player-controller, etc.) or after logical batches of work. Descriptive commit messages matter -- they are your changelog.

---

## 6. Initialize OpenSpec

Navigate to your project root (the directory containing CLAUDE.md) and initialize OpenSpec:

```bash
cd /path/to/your/project
openspec init
```

This creates an `openspec/` directory:

```
openspec/
├── changes/          # Active changes being worked on
│   └── archive/      # Completed changes moved here
└── specs/            # Main specs (synced from completed changes)
```

Commit the openspec directory:

```bash
git add openspec/
git commit -m "Initialize OpenSpec"
```

---

## 7. Plan Your Changes

This is where the workflow starts paying off. Instead of diving into code, you break your phase into logical, ordered changes and let Claude generate detailed plans for each one.

### Identify your changes

Look at your phase and break it into systems that can be built sequentially. Dependencies flow forward -- each change can depend on the ones before it but not after.

For Phase 1 of this project, the changes were:

| Order | Change Name | What It Covers |
|-------|-------------|----------------|
| 1 | static-maze | Maze geometry, materials, floor/wall prefabs, spawn points, NavMesh |
| 2 | player-controller | Movement, camera, input, health, basic combat, HUD |
| 3 | key-exit-system | Key spawning, pickup, exit gate, win condition, match manager |
| 4 | enemy-ai | 5-state machine, perception, navigation, enemy types, configs |
| 5 | loot-boxes | Weapon data, loot tables, loot box spawning, weapon switching, combat integration |

### Generate proposals

In Claude Code, use the OpenSpec propose command for each change:

```
/opsx:propose static-maze
```

Claude reads your CLAUDE.md (the PRD), understands the full project context, and generates:

- **proposal.md** -- High-level description of what this change does and why
- **design.md** -- Architectural decisions, component relationships, data flow
- **specs/** -- Detailed specifications for each sub-system (e.g., maze-environment, spawn-points, navmesh-setup)
- **tasks.md** -- A numbered checklist of every implementation step

Each task in tasks.md is specific and actionable. Not "implement enemy AI" but "Create EnemyPerception.cs with cone-based line-of-sight check using Physics.OverlapSphere and angle comparison, configurable range and angle fields."

### Review before implementing

Read through the generated artifacts. This is your chance to catch design issues before any code is written. Check:

- Do the specs match your PRD?
- Are dependencies between changes handled correctly?
- Are there missing tasks?
- Do the architectural decisions make sense?

Adjust anything that needs fixing, then commit all proposals:

```bash
git add openspec/
git commit -m "Add all Phase 1 proposals"
```

### How many tasks to expect

The number of tasks depends on system complexity. For reference:

| Change | Task Count |
|--------|-----------|
| static-maze | 68 |
| player-controller | 37 |
| key-exit-system | 52 |
| enemy-ai | 62 |
| loot-boxes | 67 |
| **Total** | **286** |

286 tasks sounds like a lot. It is. But Claude executes them, not you. Your job is to review and playtest.

---

## 8. Implement Changes

Now the fast part. For each change, in order:

```
/opsx:apply static-maze
```

Claude reads the proposal, design, specs, and task list, then starts implementing tasks one by one. It will:

1. Create C# scripts in the appropriate `Assets/Scripts/` subdirectory
2. Create Editor setup scripts that programmatically build prefabs, materials, and ScriptableObjects
3. Execute those setup scripts via Unity MCP's `script-execute` tool
4. Wire up references between components
5. Check off tasks as they are completed

### What you do during implementation

- **Watch the output.** Claude will show you what it is creating and executing. You do not need to type code, but you should read what is being generated.
- **Keep Unity open.** Claude needs MCP access to create assets and verify things compile.
- **Playtest between changes.** After `static-maze` finishes, press Play in Unity. Walk around. Does the maze look right? Are spawn points in dead-ends? Fix issues before moving on.
- **Answer questions.** Sometimes Claude will hit an ambiguity in the PRD and ask you to decide. Have an opinion ready or tell it to pick the simpler option.

### Handling interruptions

If Claude's context gets too large (you will notice it slowing down or losing track of earlier work), just start a new Claude Code session:

```bash
# End current session (Ctrl+C or type /exit)
# Start fresh
claude
```

Claude reads CLAUDE.md again, sees the OpenSpec task list with completed checkboxes, and picks up where it left off. This is why the OpenSpec structure matters -- it is persistent state that survives across sessions.

### Implementation order matters

Build in dependency order. Each system depends on the ones before it:

```
static-maze      -- The physical environment everything else lives in
    |
player-controller -- Needs a maze to walk through
    |
key-exit-system  -- Needs a player to pick up the key and a maze to place it in
    |
enemy-ai         -- Needs a maze (NavMesh), players to chase, spawn points
    |
loot-boxes       -- Needs the weapon/combat system, spawn logic, maze floor tiles
```

If you try to build the player controller before the maze exists, Claude will have nothing to test against and you will get scripts with no scene context.

---

## 9. The Editor Setup Script Pattern

This is the single most important technical pattern in the entire workflow. Understand it well.

### The problem

Claude Code can write C# scripts all day. But Unity is not just code -- it is scenes, prefabs, materials, ScriptableObjects, component configurations, and wired-up references. Claude cannot drag-and-drop in the Unity Editor. It cannot click "Create Material" in a menu.

### The solution

**Editor setup scripts.** These are C# scripts in `Assets/Scripts/Editor/` that programmatically do what you would do manually in the Unity Editor:

- Create materials with specific shader settings and colors
- Build prefabs from primitives with correct components, colliders, and scales
- Create ScriptableObjects with pre-filled data
- Instantiate prefabs in the scene hierarchy under the right parent objects
- Add components to existing GameObjects and wire their references
- Register network prefabs with the NetworkManager

Claude writes these scripts, then executes them via Unity MCP's `script-execute` tool.

### How execution works

Unity MCP's `script-execute` runs C# code inside the Unity Editor process. But there is a catch: Editor scripts live in a separate assembly (`Assembly-CSharp-Editor`), so the MCP execution context cannot see them directly. The workaround is reflection:

```csharp
// Find the Editor assembly
var editorAssembly = System.AppDomain.CurrentDomain.GetAssemblies()
    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp-Editor");

// Get the setup class
var setupType = editorAssembly.GetType("YourSetupClassName");

// Invoke the static setup method
var method = setupType.GetMethod("RunSetup",
    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
method.Invoke(null, null);
```

Claude handles this pattern automatically. You do not need to write reflection code yourself. But understanding why it exists helps when debugging.

### Example: What a setup script does

A typical setup script like `StaticMazeBuilder.cs` might:

1. Create wall and floor materials (URP Lit shader, specific colors)
2. Save them to `Assets/Materials/Maze/`
3. Create wall, floor, and pillar prefabs from cubes with correct scale
4. Assign materials to prefab renderers
5. Save prefabs to `Assets/Prefabs/Maze/`
6. Instantiate the full 20x20 maze layout in the scene
7. Place spawn point markers at dead-ends
8. Call `AssetDatabase.SaveAssets()` and `AssetDatabase.Refresh()`

All of this runs in a single method call. The result is a fully built maze in your scene that would have taken 30+ minutes to construct by hand.

---

## 10. Archive, Commit, Repeat

After each change is fully implemented and tested:

### Archive the change

```
/opsx:archive static-maze
```

This moves the change from `openspec/changes/` to `openspec/changes/archive/` and syncs the specs to `openspec/specs/`. Archived specs become reference material for future changes.

### Commit

```bash
git add .
git commit -m "Implement static maze: 20x20 layout, materials, prefabs, spawn points, NavMesh"
```

### Move to the next change

```
/opsx:apply player-controller
```

Repeat until all changes in the phase are complete.

### End of phase

When all changes are archived and committed:

```bash
git push origin phase-1
```

Create a pull request to merge into main, or merge directly if you are working solo.

---

## 11. Key Architecture Patterns

These patterns are baked into the CLAUDE.md and enforced during implementation. Follow them from the start or pay for it later.

### NetworkBehaviour over MonoBehaviour

Every game-critical script inherits from `NetworkBehaviour`, not `MonoBehaviour`. Even in Phase 1 when there is no multiplayer. This means adding `using Unity.Netcode;` to every script and the project must have Netcode for GameObjects installed.

Why: Retrofitting MonoBehaviour scripts for multiplayer requires changing the base class, adding network variables, converting method calls to RPCs, and handling ownership. Doing this across 30+ scripts is a full rewrite. Starting with NetworkBehaviour costs nothing upfront and saves days later.

### ScriptableObject configuration

Game data lives in ScriptableObjects, not hard-coded in scripts:

- Enemy configs (health, speed, sight range, patrol speed)
- Weapon data (damage, fire rate, range, ammo)
- Loot tables (item weights, spawn chances)

This lets you tune the game without code changes. Change enemy health from 100 to 150 by editing a ScriptableObject in the Inspector -- no recompile.

### Host-authoritative architecture

Even before multiplayer is implemented, structure your code so the "host" owns all game state:

- Player health: modified only by the authority (host)
- Key state: managed by a central KeyManager
- Loot box contents: determined by a central LootManager

Clients request actions via methods that will become ServerRpcs. The host validates and applies them. This pattern maps directly to Netcode for GameObjects when you add networking.

### 50-attempt placement guards

Any system that places objects at random positions (loot boxes, enemies, the key) must use a maximum attempt loop:

```csharp
Vector3 position = fallbackPosition;
for (int i = 0; i < 50; i++)
{
    Vector3 candidate = GetRandomPosition();
    if (IsValidPosition(candidate))
    {
        position = candidate;
        break;
    }
}
// Use position -- either a valid random spot or the fallback
```

Never use `while(true)` or unbounded loops for placement. A bad maze seed or tight constraints will hang your game.

---

## 12. Troubleshooting

### "Unity MCP tools are not available"

- Is Unity open with the project loaded?
- Does the `.mcp.json` file exist in the directory where you launched Claude Code?
- Is the server exe path correct? Check for typos and use forward slashes.
- Restart Claude Code after creating or modifying `.mcp.json`.
- In Unity, check the MCP panel -- the server must be running.

### "Claude lost context / is confused about what was already done"

Start a new Claude Code session. The CLAUDE.md and OpenSpec files persist on disk. Claude will re-read them and pick up where it left off. This is normal and expected for long implementation sessions.

### "Script won't compile -- missing reference"

Check the implementation order. If Claude is trying to reference a class that has not been created yet (e.g., WeaponData before the loot-boxes change), it is working out of order. The OpenSpec task list should prevent this, but if it happens, implement the dependency first.

### "Editor setup script fails via MCP"

Common causes:
- The script has a compile error. Check Unity's Console window.
- The script references an Editor-only API but is not in an `Editor/` folder.
- The reflection path to the method is wrong. The class name or method name does not match.
- Unity needs to recompile before the new script is available. Wait for compilation to finish.

### "Prefab or material looks wrong (pink/magenta)"

The material is using a shader not compatible with URP. Editor setup scripts should use `Shader.Find("Universal Render Pipeline/Lit")`. If they used the Standard shader by mistake, recreate the material with the correct shader.

### "NavMesh agents won't move / enemies stuck"

NavMesh needs to be baked after the maze geometry is in place. In Unity: **Window > AI > Navigation** (or Navigation panel) then click **Bake**. For runtime generation (Phase 2+), you will use `NavMeshSurface.BuildNavMesh()` in code.

### "Path has spaces and something broke"

If your Unity project path has spaces (e.g., `Maze Runner Key Protocol/`), some command-line tools may struggle. The MCP `.mcp.json` handles this fine in JSON strings, but be careful with bash commands. Wrap paths in quotes.

---

## 13. Reference: Full Project Structure

After Phase 1 completion, the project looks like this:

```
mazerunnerkeyprotocol/                    # Git root / Claude Code working directory
|
+-- CLAUDE.md                             # PRD -- Claude reads this every session
+-- TRAINING-GUIDE.md                     # This file
+-- .mcp.json                             # Unity MCP connection config
+-- .gitignore
|
+-- openspec/                             # OpenSpec change management
|   +-- changes/
|   |   +-- archive/                      # Completed changes
|   |       +-- 2026-03-10-static-maze/
|   |       |   +-- proposal.md
|   |       |   +-- design.md
|   |       |   +-- specs/
|   |       |   |   +-- maze-environment/spec.md
|   |       |   |   +-- spawn-points/spec.md
|   |       |   |   +-- navmesh-setup/spec.md
|   |       |   +-- tasks.md
|   |       +-- 2026-03-10-player-controller/
|   |       +-- 2026-03-11-key-exit-system/
|   |       +-- 2026-03-11-enemy-ai/
|   |       +-- 2026-03-11-loot-boxes/
|   +-- specs/                            # Synced specs (living reference)
|       +-- maze-environment/spec.md
|       +-- spawn-points/spec.md
|       +-- navmesh-setup/spec.md
|       +-- player-movement/spec.md
|       +-- player-health/spec.md
|       +-- player-combat/spec.md
|       +-- player-ui/spec.md
|       +-- key-mechanics/spec.md
|       +-- exit-mechanics/spec.md
|       +-- win-condition/spec.md
|       +-- key-holder-tracking/spec.md
|       +-- enemy-state-machine/spec.md
|       +-- enemy-perception/spec.md
|       +-- enemy-types/spec.md
|       +-- enemy-navigation/spec.md
|
+-- Maze Runner Key Protocol/             # Unity project
    +-- Assets/
    |   +-- Input/                        # Input System action maps
    |   +-- Materials/
    |   |   +-- Maze/                     # Wall, floor materials
    |   |   +-- Key/                      # Key glow material
    |   |   +-- Exit/                     # Exit gate material
    |   |   +-- Enemy/                    # Enemy materials
    |   |   +-- Loot/                     # Loot box material
    |   +-- Prefabs/
    |   |   +-- Maze/                     # Wall, floor, pillar prefabs
    |   |   +-- Enemy/                    # Grunt, Guard prefabs
    |   |   +-- Player.prefab
    |   |   +-- Key.prefab
    |   |   +-- ExitGateway.prefab
    |   |   +-- LootBox.prefab
    |   +-- ScriptableObjects/
    |   |   +-- EnemyConfigs/             # Grunt, Guard configurations
    |   |   +-- Weapons/                  # Pistol, Shotgun, SMG, Rifle data
    |   |   +-- LootTable.asset
    |   +-- Scenes/
    |   |   +-- TestMaze.unity
    |   +-- Scripts/
    |   |   +-- Editor/                   # Setup scripts (run via MCP)
    |   |   |   +-- StaticMazeBuilder.cs
    |   |   |   +-- PlayerSetup.cs
    |   |   |   +-- KeyExitSetup.cs
    |   |   |   +-- EnemySetup.cs
    |   |   |   +-- LootSetup.cs
    |   |   +-- Enemy/
    |   |   |   +-- EnemyStateMachine.cs
    |   |   |   +-- EnemyPerception.cs
    |   |   |   +-- EnemyConfig.cs        # ScriptableObject definition
    |   |   |   +-- EnemyNavigation.cs
    |   |   +-- GameState/
    |   |   |   +-- KeyManager.cs
    |   |   |   +-- ExitGateway.cs
    |   |   |   +-- MatchManager.cs
    |   |   +-- Loot/
    |   |   |   +-- WeaponData.cs         # ScriptableObject definition
    |   |   |   +-- WeaponInventory.cs
    |   |   |   +-- LootBox.cs
    |   |   |   +-- LootTableData.cs
    |   |   |   +-- LootSpawner.cs
    |   |   +-- Network/
    |   |   |   +-- AutoStartHost.cs
    |   |   |   +-- PlayerSpawnHandler.cs
    |   |   +-- Player/
    |   |   |   +-- PlayerMovement.cs
    |   |   |   +-- PlayerCombat.cs
    |   |   |   +-- PlayerHealth.cs
    |   |   |   +-- PlayerCamera.cs
    |   |   +-- SpawnPoints/
    |   |   |   +-- PlayerSpawnPoint.cs
    |   |   |   +-- EnemySpawnPoint.cs
    |   |   |   +-- LootSpawnPoint.cs
    |   |   |   +-- KeySpawnPoint.cs
    |   |   +-- UI/
    |   |       +-- PlayerHUD.cs
    |   +-- Settings/                     # URP pipeline assets
    +-- Packages/
    +-- ProjectSettings/
```

---

## Quick Reference: Command Cheat Sheet

| What | Command |
|------|---------|
| Start Claude Code session | `claude` |
| Initialize OpenSpec | `openspec init` |
| Propose a change | `/opsx:propose <change-name>` |
| Implement a change | `/opsx:apply <change-name>` |
| Archive a change | `/opsx:archive <change-name>` |
| Create feature branch | `git checkout -b phase-1` |
| Commit work | `git add . && git commit -m "message"` |
| Push branch | `git push origin phase-1` |

---

## The Big Picture

The workflow boils down to a loop:

```
Write PRD (once) --> Break phase into changes --> For each change:
    Propose --> Review --> Implement --> Playtest --> Fix --> Archive --> Commit
```

Claude does the implementation. OpenSpec keeps it organized. Unity MCP bridges the gap between generated code and the Unity Editor. Your job is to direct, review, and playtest.

286 tasks in 2.5 hours is not magic. It is the result of front-loading the thinking (PRD + proposals) so the execution can be mechanical. Claude is very good at mechanical execution when it has clear specs. It is much worse at guessing what you want. Give it a detailed PRD and structured task lists, and it will build your game.
