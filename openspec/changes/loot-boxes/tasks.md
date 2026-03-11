## 1. WeaponData ScriptableObject

- [ ] 1.1 Create `WeaponData` ScriptableObject class with serialized fields: weaponName (string), damage (float), fireRate (float, shots/sec), ammoCapacity (int, magazine size), maxAmmo (int, total carrying capacity, -1 for infinite), spreadAngle (float, degrees), effectiveRange (float, Unity units), fireMode (enum: Single, Automatic), reloadTime (float, seconds), pelletCount (int, default 1 for non-shotgun weapons)
- [ ] 1.2 Create `WeaponData` asset for Pistol: low damage, moderate fire rate, single-shot, maxAmmo = -1 (unlimited), zero spread, medium range, pelletCount = 1
- [ ] 1.3 Create `WeaponData` asset for Shotgun: high per-pellet damage, low fire rate, single-shot, limited ammo, wide spread angle, short range, pelletCount = 6-8
- [ ] 1.4 Create `WeaponData` asset for SMG: low damage, high fire rate, automatic fire mode, limited ammo, small spread angle, medium range, pelletCount = 1
- [ ] 1.5 Create `WeaponData` asset for Rifle: high damage, low fire rate, single-shot, limited ammo, zero spread, long range, pelletCount = 1
- [ ] 1.6 Store all WeaponData assets in `Assets/ScriptableObjects/Weapons/` and verify each is editable in the Inspector without code changes

## 2. Weapon Inventory System

- [ ] 2.1 Create `WeaponInventory` class inheriting from `NetworkBehaviour` with 3 slots: slot 1 (pistol, permanent), slots 2-3 (looted weapons, initially empty)
- [ ] 2.2 Add `NetworkVariable<int>` for the currently equipped weapon slot index, writable by owner, readable by all
- [ ] 2.3 Implement `AddWeapon(WeaponData)` method that places weapon in first empty slot (2, then 3), or swaps with currently equipped looted weapon if both slots full (destroy dropped weapon in Phase 1)
- [ ] 2.4 Implement weapon switching via number keys (1, 2, 3) -- only switch if the target slot contains a weapon
- [ ] 2.5 Implement weapon switching via scroll wheel, cycling through occupied slots only, skipping empty slots
- [ ] 2.6 Track per-weapon ammo as an int array synced via `NetworkVariable` or `NetworkList`, with pistol slot exempt from ammo tracking
- [ ] 2.7 Implement `AddAmmo(int amount)` for the currently equipped weapon (or first non-pistol weapon if pistol is equipped), clamping at maxAmmo from WeaponData
- [ ] 2.8 Implement duplicate weapon pickup logic: if player already owns the weapon type, add its default starting ammo to existing ammo count (capped at maxAmmo)
- [ ] 2.9 Reject any attempt to remove or drop the pistol from slot 1

## 3. Weapon Firing Mechanics

- [ ] 3.1 Refactor existing combat/fire logic to read damage, fireRate, spreadAngle, effectiveRange, and fireMode from the currently equipped weapon's `WeaponData` instead of hardcoded pistol values
- [ ] 3.2 Implement fire rate cooldown: track `nextFireTime` per weapon, ignore fire inputs before cooldown elapses, cancel previous weapon cooldown on weapon switch
- [ ] 3.3 Implement single-shot fire mode: fire once on `GetMouseButtonDown(0)`, do not repeat while held
- [ ] 3.4 Implement automatic fire mode: fire continuously on `GetMouseButton(0)` at the configured fire rate, stop on button release or ammo depletion
- [ ] 3.5 Implement single-ray hitscan for pistol, SMG, and rifle: one raycast from camera center forward, limited to effectiveRange, applying spread angle as random deviation per shot
- [ ] 3.6 Implement multi-ray hitscan for shotgun: cast `pelletCount` rays within the spread cone, each ray independently checking for hits, applying per-pellet damage to each hit target
- [ ] 3.7 Ensure maze walls (and all colliders on the wall layer) block all weapon raycasts -- no damage through walls
- [ ] 3.8 Apply damage to hit targets using the weapon's damage value per ray hit; hits beyond effectiveRange register no damage

## 4. Ammo Management

- [ ] 4.1 Check ammo availability before each shot: block firing and trigger empty-weapon audio/visual cue if looted weapon has zero ammo
- [ ] 4.2 Decrement ammo by 1 per shot for single-ray weapons; decrement by 1 per shot (not per pellet) for shotgun
- [ ] 4.3 Skip ammo check entirely for the pistol (slot 1) -- always allow firing
- [ ] 4.4 Stop automatic fire immediately when ammo reaches zero during sustained fire, trigger empty-weapon cue
- [ ] 4.5 Implement reserve ammo storage: if player picks up ammo with no non-pistol weapons, store reserve ammo to be applied to the next weapon picked up

## 5. Loot Table and Item Definitions

- [ ] 5.1 Create `LootItemType` enum with values: Shotgun, SMG, Rifle, AmmoPack, HealthPack
- [ ] 5.2 Create `LootTableEntry` serializable struct with fields: LootItemType, WeaponData reference (null for non-weapons), weight (int)
- [ ] 5.3 Create `LootTable` ScriptableObject containing a list of `LootTableEntry` items and a method `Roll(System.Random rng)` that returns a `LootTableEntry` based on weighted random selection
- [ ] 5.4 Create default `LootTable` asset with weights producing approximately 60% supplies (ammo + health) and 40% weapons, stored in `Assets/ScriptableObjects/`
- [ ] 5.5 Create `AmmoPickupData` ScriptableObject with configurable ammo amount per pickup
- [ ] 5.6 Create `HealthPackData` ScriptableObject with configurable heal amount per pickup
- [ ] 5.7 Verify the loot table contains entries for all five required item types: shotgun, SMG, rifle, ammo pickup, health pack

## 6. Loot Box Prefab

- [ ] 6.1 Create `LootBox` class inheriting from `NetworkBehaviour` with `NetworkVariable<bool>` for availability state (default true), writable by server only
- [ ] 6.2 Add a `NetworkVariable<int>` (or serialized field set by host) storing the assigned `LootItemType` determined at spawn time
- [ ] 6.3 Add a trigger collider (SphereCollider or BoxCollider, isTrigger = true) defining the interaction range
- [ ] 6.4 Implement `OnTriggerEnter`/`OnTriggerStay` to detect player presence, and listen for interaction key (E) press to initiate loot request
- [ ] 6.5 Implement `RequestPickupServerRpc` that sends a pickup request from the client to the host
- [ ] 6.6 On the host, validate pickup request: check box availability (`NetworkVariable<bool>` is true), verify player is within interaction range (distance check), then grant item and set availability to false
- [ ] 6.7 After successful pickup on host: grant item to player (call into WeaponInventory.AddWeapon, AddAmmo, or heal player depending on LootItemType), then despawn the loot box `NetworkObject`
- [ ] 6.8 Reject second pickup if two requests arrive near-simultaneously: host processes sequentially, second request finds availability = false and is denied
- [ ] 6.9 Add a pulsing glow visual effect (emissive material or point light) on the loot box prefab, visible when availability is true, hidden when looted
- [ ] 6.10 Create the loot box prefab in `Assets/Prefabs/` with the `LootBox` script, trigger collider, visual mesh (placeholder cube/crate), and glow effect

## 7. Loot Box Spawn System

- [ ] 7.1 Create `LootBoxSpawner` class inheriting from `NetworkBehaviour`, responsible for placing loot boxes at match start (runs on host only)
- [ ] 7.2 Implement loot count formula: `lootCount = baseLootCount + (playerCount - 2) * additionalLootPerPlayer` with configurable `baseLootCount` (default 8) and `additionalLootPerPlayer` (default 3); clamp minimum to baseLootCount for 1-player matches
- [ ] 7.3 Implement maximum cap: clamp lootCount to `mazeWidth * mazeHeight / 15` (rounded down) to prevent over-saturation
- [ ] 7.4 Implement the valid tile check function: candidate position must be a walkable floor tile, not within 2-tile separation distance of any already-placed loot box, not within configurable min distance of any player spawn (default 3 tiles), not within configurable min distance of key spawn (default 2 tiles)
- [ ] 7.5 Implement the 50-attempt placement loop: for each loot box, try up to 50 random valid floor tile positions; if valid, place and break; if 50 attempts exhausted, use fallback
- [ ] 7.6 At maze generation time, build a fallback position list of at least 4 pre-validated walkable positions distributed across the 4 quadrants of the maze
- [ ] 7.7 When fallback is needed, consume the next fallback position from the list (remove it so no two boxes share a fallback)
- [ ] 7.8 Initialize the spawner's `System.Random` RNG with the match seed so loot placements are deterministic and reproducible for debugging
- [ ] 7.9 For each placed loot box, roll the `LootTable` using the seeded RNG to determine contents, then spawn the loot box `NetworkObject` at the position with contents assigned
- [ ] 7.10 Ensure clients receive loot box positions and states via NetworkObject synchronization (clients do NOT run placement logic)

## 8. Health Pack and Ammo Pickup Logic

- [ ] 8.1 Implement health pack application in `LootBox` pickup handler: increase player health by the configured heal amount, clamp to max health, consume the box even if player is at full health
- [ ] 8.2 Implement ammo pickup application: add configured ammo amount to the currently equipped non-pistol weapon, clamp at weapon's maxAmmo
- [ ] 8.3 Handle ammo pickup when only pistol is equipped: apply ammo to first non-pistol weapon in inventory, or store as reserve if no non-pistol weapons exist

## 9. Combat HUD Updates

- [ ] 9.1 Display the currently equipped weapon name (from WeaponData.weaponName) on the HUD, updating immediately on weapon switch
- [ ] 9.2 Display ammo as "current / max" for looted weapons, updating on every shot fired and ammo pickup
- [ ] 9.3 Display infinity symbol or "INF" for ammo when the pistol is equipped
- [ ] 9.4 Implement crosshair that adapts to weapon spread: wider crosshair for high-spread weapons (shotgun), tighter for low/zero-spread weapons (pistol, rifle)
- [ ] 9.5 Display weapon slot indicators (1, 2, 3) showing which slots are occupied and which is currently active

## 10. Integration and Validation

- [ ] 10.1 Wire up the `LootBoxSpawner` to run after maze generation completes and after player spawns, key, and exit have been placed
- [ ] 10.2 Verify full loot loop in single-player: spawn maze, place loot boxes, approach box, press E, receive weapon/ammo/health, box destroyed
- [ ] 10.3 Verify weapon swap flow: pick up weapon with full inventory, currently equipped weapon is destroyed, new weapon takes its slot
- [ ] 10.4 Verify each weapon type fires correctly: pistol (single-ray, unlimited ammo), shotgun (multi-ray spread, limited ammo), SMG (automatic fire, limited ammo), rifle (single-ray, high damage, limited ammo)
- [ ] 10.5 Verify 50-attempt guard: in a constrained maze (small size, many exclusion zones), confirm loot boxes use fallback positions and no infinite loop occurs
- [ ] 10.6 Verify density scaling: confirm correct box counts for 1, 2, 4, and 8 player counts, and that the maze-size cap is enforced
- [ ] 10.7 Verify deterministic placement: run two matches with the same seed and player count, confirm identical loot box positions and contents
