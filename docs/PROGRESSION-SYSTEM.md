# Adventure Progression System

Status: foundation plus first generated map/bridge pass are committed. Unity still needs to be opened locally to import the new scripts and run the full PlayMode suite before this should be treated as green.

## Why This Exists

The project now has enough proven missions to stop growing as a single-scene sandbox. The progression layer turns the existing 12 mission variants into a durable game shell with locations, unlocks, and saved best results.

The implementation is intentionally boring:

- no new missions;
- no new art dependency;
- no deep UI redesign;
- no rewrite of `GameManager`;
- additive services/controllers that can be tested before real menu art/prefabs exist.

## Runtime Pieces

`unity/CheddarAndCocoa/Assets/Scripts/Game/AdventureProgression.cs` introduces:

- `AdventureLocationDefinition` - Unity-serializable location data: id, display name, description, required stars, thumbnail key, color, and mission list.
- `AdventureLocationCatalog` - default data for the first map nodes.
- `AdventureProgressSnapshot` - JSON-serializable save payload.
- `MissionProgressRecord` - attempts, clears, best score, best stars, and best rank per mission.
- `AdventureProgressService` - load/save, corrupt-save fallback, mission-result recording, star totals, unlock recalculation, and test-only in-memory construction.

`unity/CheddarAndCocoa/Assets/Scripts/Game/AdventureMapController.cs` introduces UI-agnostic map state:

- selected location;
- selected mission within that location;
- locked/unlocked labels;
- mission best-score/star/rank rows;
- launch gating so locked nodes cannot start missions.

`unity/CheddarAndCocoa/Assets/Scripts/Game/AdventureMissionLaunch.cs` is the tiny handoff between map and arena. It queues a selected mission/location before loading ArenaScene.

`unity/CheddarAndCocoa/Assets/Scripts/Game/AdventureArenaProgressBridge.cs` watches ArenaScene only when a mission was launched from AdventureMap. It consumes the queued mission, starts it through existing `GameManager.StartMission`, watches for the end screen, records the result in `AdventureProgressService`, and saves.

`unity/CheddarAndCocoa/Assets/Scripts/Game/AdventureMapHud.cs` is a generated IMGUI map HUD for early testing. It shows location nodes, locked state, mission rows, bests, and launches unlocked missions. It is intentionally not final UI.

`unity/CheddarAndCocoa/Assets/Scripts/Game/AdventureMapBootstrap.cs` can be placed into a future empty AdventureMap scene to create the generated HUD.

This is a code-first bridge. A later pass can move the location catalog into authored ScriptableObject assets without changing the save format.

## Default Location Nodes

| Location | Unlock | Missions |
| --- | ---: | --- |
| Backyard | 0 stars | Backyard Rescue, Squirrel Conspiracy, Weenie Roundup, Scent Search, Thunderstorm Comfort, Mark the Yard |
| Front Yard | 6 stars | Coyotes at the Fence, Walkies on the Leash, Car Ride Balance |
| House Interior | 9 stars | Snack Heist, Sock Panic |
| Neighborhood Park | 18 stars | Eagle Shadow Panic for now; future park/social missions should land here |

The mapping is a starting point, not final world design. Keep all existing ArenaScene missions playable during the transition.

## Save Format

Default path:

`Application.persistentDataPath/adventure-progress.json`

Current payload shape:

```json
{
  "Version": 1,
  "TotalStars": 0,
  "UnlockedLocationIds": ["backyard"],
  "Missions": [
    {
      "MissionId": "BackyardRescue",
      "Attempts": 1,
      "Clears": 1,
      "BestScore": 1500,
      "BestStars": 3,
      "BestRank": "Pawfect Yard"
    }
  ]
}
```

Rules:

- Backyard is always unlocked.
- Total stars are recalculated from best mission stars, not trusted blindly from the JSON.
- Location unlocks are recalculated from total stars after load and after mission result recording.
- Missing, empty, unreadable, or corrupt saves fall back to a fresh snapshot.
- Tests should use `AdventureProgressService.CreateInMemoryForTests()` or an isolated temp path, then `ClearSave()`.

## Current Map Flow

1. A future AdventureMap scene should contain `AdventureMapBootstrap` or directly add `AdventureMapHud`.
2. `AdventureMapHud` loads progress with `AdventureProgressService.LoadDefault()`.
3. `AdventureMapController` renders/gates locations and mission rows.
4. Starting an unlocked mission calls `AdventureMissionLaunch.QueueMission(...)` and loads `ArenaScene`.
5. `AdventureArenaProgressBridge` sees the queued launch when ArenaScene loads, starts the selected mission, and waits for the end screen.
6. On clear/fail, it records:

```csharp
progress.RecordMissionResult(game.ActiveMissionVariant, game.Score, game.StarRating, game.EndRank, game.Outcome == GameManager.MissionOutcome.Clear);
progress.Save();
```

7. Returning to the map is not implemented yet; current flow proves launch + record. The next pass should add an explicit Map/Continue action on the end screen or a separate return mechanism.

## Tests Added

`AdventureProgressionPlayModeTests` covers:

- fresh player defaults;
- star thresholds unlocking locations;
- weaker replay not lowering best result;
- save/load round trip;
- corrupt save fallback;
- 18-star park unlock.

`AdventureMapControllerPlayModeTests` covers:

- fresh map selecting unlocked Backyard;
- locked Front Yard refusing launch;
- unlocked Front Yard queuing selected mission;
- mission rows showing persisted best result;
- location selection wrapping and mission selection reset.

## Remaining Validation / Next Pass

1. Open Unity so the new scripts and `.meta` files import.
2. Run the full PlayMode suite.
3. Fix compile/import issues from the new files if any.
4. Create an actual `AdventureMap` scene with `AdventureMapBootstrap`.
5. Add it to build settings ahead of ArenaScene if that is the desired launch order.
6. Add a return-to-map path from ArenaScene end screen.
7. Decide whether direct ArenaScene play should remain local-only or also persist results.
8. Update `docs/ARENA-PLAYABLE.md` once scene flow is verified in Unity.

## Guardrails

- Do not add more mission variants until the map/save layer works end-to-end.
- Do not move mission scoring into the save layer; save only records results.
- Do not make location unlocks depend on session counters; use persisted best stars.
- Keep corrupt-save recovery silent and safe for families/non-technical players.
- Keep the map UI generated/minimal until the data and scene flow are tested.
