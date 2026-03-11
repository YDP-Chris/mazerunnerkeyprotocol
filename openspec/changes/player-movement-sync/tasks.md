## 1. NetworkTransform Setup

- [x] 1.1 Add NetworkTransform component to the Player prefab via editor script
- [x] 1.2 Configure NetworkTransform: owner-authoritative, interpolation enabled, ~30Hz sync rate
- [x] 1.3 In PlayerMovement.OnNetworkSpawn, disable CharacterController on non-owner instances so NetworkTransform can drive position

## 2. Player Body Visuals

- [x] 2.1 Add a capsule MeshRenderer child object to the Player prefab (height ~2, radius ~0.5)
- [x] 2.2 Create 4-8 player color materials (blue, red, green, yellow, etc.) in Assets/Materials/Players/
- [x] 2.3 On spawn, assign color material based on player index; disable MeshRenderer on owner instance

## 3. Player Nameplate

- [x] 3.1 Add a World Space Canvas child to the Player prefab positioned above the capsule (Y offset ~2.5)
- [x] 3.2 Add TextMeshPro text displaying "Player {index}" on the canvas
- [x] 3.3 Add billboard script that rotates canvas to always face the local camera
- [x] 3.4 Disable nameplate canvas on owner instance

## 4. Integration and Verification

- [ ] 4.1 Verify owner movement still feels responsive (no input lag from NetworkTransform)
- [ ] 4.2 Verify remote player movement is smooth (interpolation working)
- [ ] 4.3 Verify first-person camera does not see own capsule body
- [ ] 4.4 Test with two instances: both players visible and moving independently in the maze
