## 1. Key Spawning and Placement

- [x] 1.1 Create `KeyManager.cs` (NetworkBehaviour) on a scene GameObject owned by the host. Add NetworkVariables for key world position (`NetworkVariable<Vector3>`), key holder client ID (`NetworkVariable<ulong>` using `ulong.MaxValue` for "no holder"), and key state enum (`Uncollected`, `Held`, `Dropped`).
- [x] 1.2 Implement `PlaceKey()` host-only method: iterate up to 50 random navigable floor tiles, reject any within the configurable minimum distance of every player spawn point and the exit, select the first valid tile. If no valid tile is found in 50 attempts, fall back to the navigable tile with the greatest minimum distance from all spawns and the exit, and log a warning.
- [x] 1.3 Call `PlaceKey()` from the maze generation pipeline after exit and player spawn placement are complete. Write the result to the key position NetworkVariable so all clients instantiate the key at the same world position.
- [x] 1.4 Create the key prefab: a GameObject with a trigger SphereCollider (pickup radius), a MeshRenderer for the visual, and a NetworkObject component. Register it in the NetworkManager prefab list.

## 2. Key Proximity Feedback

- [x] 2.1 Add a client-side `KeyProximityFeedback.cs` MonoBehaviour on the key prefab. Each frame, compute the local player's distance to the key world position.
- [x] 2.2 When the local player is within the detection radius (default 8 units), enable a pulsing emissive glow on the key material. Scale pulse intensity inversely with distance (closer = stronger).
- [x] 2.3 When the local player is within the minimap-reveal radius (default 5 units), send an event to the minimap system to show the key icon. Remove the icon when the player exits the radius.
- [x] 2.4 Expose `detectionRadius` and `minimapRevealRadius` as serialized fields for tuning.

## 3. Key Pickup

- [x] 3.1 In `KeyManager.cs`, implement `OnTriggerEnter` on the key's trigger collider. When a player collider enters, if this is the local player, send a ServerRpc pickup request (`RequestPickupServerRpc(ulong clientId)`).
- [x] 3.2 In the ServerRpc handler (host-only), validate that the key state is `Uncollected` or `Dropped` and no other player holds the key. If valid, set the key holder NetworkVariable to the requesting client ID, set key state to `Held`, and invoke a `NotifyKeyPickupClientRpc(ulong holderClientId)`.
- [x] 3.3 On the ClientRpc callback, hide the key world GameObject on all clients, trigger the exit unlock sequence, and activate the key-holder tracking indicator for non-holder players.
- [ ] 3.4 On the key holder's client, attach a key visual to the player model (belt or back attachment point) so other players can visually identify the holder in 3rd-person view.

## 4. Key Drop on Death

- [x] 4.1 In the player health/elimination system, when the host confirms a player is eliminated, check if that player is the current key holder (compare against `KeyManager` holder NetworkVariable).
- [x] 4.2 If the eliminated player holds the key, call `DropKey(Vector3 deathPosition)` on `KeyManager`. This method sets key state to `Dropped`, clears the holder NetworkVariable, snaps the death position to the nearest navigable point on the NavMesh, writes the new position to the key position NetworkVariable, and fires a `NotifyKeyDropClientRpc`.
- [x] 4.3 On the ClientRpc callback, re-enable the key world GameObject at the drop position, restore idle animation and proximity feedback, remove the key attachment from the dead player's model, and deactivate the key-holder tracking indicator on all HUDs.
- [x] 4.4 Re-enable the key's trigger collider so surviving players can pick it up immediately under standard pickup rules.

## 5. Key Visual States

- [x] 5.1 Create an idle animation on the key prefab: slow Y-axis rotation and subtle emissive shimmer using a shader or animation clip. Active when key state is `Uncollected` or `Dropped`.
- [x] 5.2 When key state transitions to `Held`, disable the key world MeshRenderer and idle animation. Enable the player-attached key visual on the holder's character model.
- [x] 5.3 When key state transitions to `Dropped`, re-enable the key world MeshRenderer and idle animation at the new drop position. Disable the player-attached key visual.

## 6. Exit Placement

- [x] 6.1 Create `ExitGateway.cs` (NetworkBehaviour) with a NetworkVariable for lock state (`NetworkVariable<bool>`, default locked/false).
- [x] 6.2 Implement exit placement in the maze generation pipeline: select a position on the outer edge of the maze grid, maximizing distance from the nearest player spawn. Placement must be deterministic given the maze seed so all clients produce the same result.
- [x] 6.3 Create the exit gateway prefab: a GameObject with a trigger BoxCollider (escape zone), a visual door/barrier mesh, a NetworkObject component, and a point light for the beacon effect. Register it in the NetworkManager prefab list.

## 7. Exit Lock State and Visibility

- [x] 7.1 At match start, set the exit lock NetworkVariable to locked. Render the exit with a closed/inactive visual (no glow, no beacon). Do not show the exit on any player's minimap.
- [x] 7.2 When `KeyManager` fires the key pickup notification, set the exit lock NetworkVariable to unlocked on the host. On all clients, transition the exit visual to its unlocked state: enable glow, pulsing light beacon, and open door animation.
- [x] 7.3 When the exit unlocks, add the exit icon to every player's minimap. The exit remains visible on the minimap for the rest of the match regardless of key drops or holder changes.
- [x] 7.4 Ensure the exit does NOT re-lock if the key is dropped. The lock state only transitions once (locked -> unlocked) per match.

## 8. Escape Animation and Sequence

- [x] 8.1 In `ExitGateway.cs`, implement `OnTriggerEnter`/`OnTriggerExit` for the escape zone. When the key holder enters the zone, send a ServerRpc to the host to begin the escape sequence.
- [x] 8.2 On the host, start a configurable escape timer (default 2.5 seconds, exposed as a serialized field). Track progress in a NetworkVariable (`NetworkVariable<float>` for normalized 0-1 progress) so all clients can display a progress bar.
- [x] 8.3 Lock the key holder's movement input on all clients when the escape animation begins. Play a channeled escape animation on the player model, synchronized via ClientRpc or NetworkAnimator.
- [x] 8.4 If the key holder takes any damage during the escape (detected on host via the health system), immediately cancel the escape: reset the timer NetworkVariable to 0, unlock player movement, fire a `CancelEscapeClientRpc`, and allow the player to re-trigger by remaining in or re-entering the zone.
- [x] 8.5 If the key holder leaves the exit zone trigger collider during the escape animation, cancel the escape with the same reset logic as damage interruption.
- [x] 8.6 On all non-holder clients, display a visible "Escape in Progress" indicator at the exit location when the escape timer is running, creating urgency for pursuers.
- [x] 8.7 When the escape timer reaches 1.0 (completion) on the host without interruption, invoke the win condition (see Group 9).

## 9. Win Condition: Escape Victory

- [x] 9.1 Create `MatchManager.cs` (NetworkBehaviour) to own match state. Add a NetworkVariable for match outcome enum (`InProgress`, `Win`, `Draw`) and a NetworkVariable for the winner's client ID.
- [x] 9.2 When `ExitGateway` reports escape completion to `MatchManager`, set the match outcome to `Win`, set the winner client ID, and fire `MatchEndClientRpc(outcome, winnerClientId)`.
- [x] 9.3 On the ClientRpc, freeze all player input within 0.5 seconds, disable combat and movement for all players, and display the winner's escape animation to all clients.
- [ ] 9.4 Record the escape as a win for that player in the session results (increment escape count).
- [x] 9.5 Transition all clients to the match results screen showing: match outcome, winning player's name, and match duration.

## 10. Win Condition: Draw by Total Elimination

- [x] 10.1 In `MatchManager.cs`, track the count of alive players. On each player elimination event (from the health/elimination system), decrement the alive count.
- [x] 10.2 When alive player count reaches zero, set match outcome to `Draw` with no winner. Fire `MatchEndClientRpc(Draw, none)`.
- [x] 10.3 Handle the simultaneous elimination edge case: if the last two players are eliminated on the same server tick, declare a draw regardless of key holder status.
- [x] 10.4 On draw, freeze all player input within 0.5 seconds, display "No Escape - Draw" to all clients, stop all enemy AI (transition to idle), and transition to the results screen.

## 11. Key Holder Elimination During Match

- [x] 11.1 When the key holder is eliminated and at least one other player survives, do NOT end the match. Execute the key drop sequence (Group 4) and let the match continue.
- [x] 11.2 When the key holder is eliminated and exactly one other player remains, continue the match. Do not auto-award victory to the last survivor; they must pick up the key and escape.
- [x] 11.3 Support multiple key holder eliminations in a single match: each drop follows the same rules, and the match continues as long as at least one player survives.

## 12. Match-End Sequence

- [x] 12.1 In `MatchManager.cs`, implement a `FreezeMatch()` method called on both win and draw. This method disables all player input components, disables combat systems, and sends a ClientRpc to do the same on all clients.
- [x] 12.2 On match end, fire an event that all enemy AI agents listen for to transition to idle state and stop all patrol, chase, and attack behaviors.
- [x] 12.3 Build a match results UI screen (Canvas/UI Toolkit) that displays: match outcome (win/draw), winner name (if applicable), and match duration. Show this screen on all clients after the freeze.

## 13. Key-Holder Tracking: Directional Indicator

- [x] 13.1 Create `KeyHolderIndicator.cs` (MonoBehaviour) on each player's local HUD Canvas. This component reads the key holder client ID from `KeyManager`'s NetworkVariable and the holder's position from the existing NetworkTransform.
- [x] 13.2 Each frame, compute the direction from the local player's camera to the key holder's world position. Convert to screen space and render a directional arrow at the screen edge in the direction of the key holder.
- [x] 13.3 Do NOT display the indicator on the key holder's own HUD. Only activate for non-holder players.
- [x] 13.4 When the key is dropped (holder NetworkVariable cleared), hide the indicator on all HUDs. When a new player picks up the key, reactivate and point toward the new holder.
- [x] 13.5 Style the indicator as a glowing gold arrow, visually distinct from health bars, minimap, and weapon UI. The arrow must not change size, color, or opacity based on distance.
- [x] 13.6 When the key holder is within the observing player's camera viewport, reduce the indicator opacity or hide it to avoid redundant visual clutter.

## 14. Indicator Network Synchronization

- [x] 14.1 Ensure the indicator reads the key holder's position exclusively from the existing NetworkTransform sync, with no additional network messages for tracking.
- [x] 14.2 Handle key holder disconnection: the host treats disconnection as elimination, triggers key drop, and the indicator is removed from all HUDs via the standard drop flow.
- [x] 14.3 Handle late-joining clients: on spawn, read the key holder NetworkVariable. If a holder exists, immediately activate the directional indicator pointing toward them.
