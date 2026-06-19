# Adventure Progression System

Status: foundation started. The game still launches through the existing ArenaScene mission carousel until the AdventureMap scene is wired.

## Why This Exists

The project now has enough proven missions to stop growing as a single-scene sandbox. The progression layer turns the existing 12 mission variants into a durable game shell with locations, unlocks, and saved best results.

The first implementation is intentionally boring:

- no new missions;
- no new art dependency;
- no deep UI redesign;
- no rewrite of `GameManager`;
- additive service/catalog code that can be tested before any scene-flow changes.

## Runtime Pieces

`unity/CheddarAndCocoa/Assets/Scripts/Game/AdventureProgression.cs` introduces:

- `AdventureLocationDefinition` - Unity-serializable location data: id, display name, description, required stars, thumbnail key, color, and mission list.
- `AdventureLocationCatalog` - default data for the first map nodes.
- `AdventureProgressSnapshot` - JSON-serializable save payload.
- `MissionProgressRecord` - attempts, clears, best score, best stars, and best rank per mission.
- `AdventureProgressService` - load/save, corrupt-save fallback, mission-result recording, star totals, unlock recalculation, and test-only in-memory construction.

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

## Integration Contract

The next implementation pass should add thin adapters rather than a `GameManager` rewrite:

1. Add an `AdventureMap` scene or bootstrap.
2. Load `AdventureProgressService.LoadDefault()` at map start.
3. Render location nodes from `AdventureLocationCatalog.CreateDefault()`.
4. Selecting an unlocked location shows its mission list.
5. Locked nodes show required stars and current total stars.
6. Starting a mission still routes to the existing ArenaScene/GameManager path.
7. When a mission ends, record the result with:

```csharp
progress.RecordMissionResult(game.ActiveMissionVariant, game.Score, game.StarRating, game.EndRank, game.Outcome == GameManager.MissionOutcome.Clear);
progress.Save();
```

8. Mission select tiles should read best score/stars/rank from the progression service once the map owns the flow.

## Test Requirements For Next Pass

Add PlayMode coverage for:

- fresh player defaults: only Backyard unlocked, zero stars, empty mission records;
- recording a clear updates attempts, clears, best score, best stars, best rank, and total stars;
- weaker replay result does not lower best score/stars;
- save/load round-trip persists mission records and unlocks;
- corrupt save falls back to fresh Backyard-only state;
- reaching 6/9/18 total stars unlocks Front Yard, House Interior, and Neighborhood Park;
- AdventureMap displays locked/unlocked state and mission bests;
- existing `AllMissionsSmokePlayModeTests` still pass.

## Guardrails

- Do not add more mission variants until the map/save layer works.
- Do not move mission scoring into the save layer; save only records results.
- Do not make location unlocks depend on session counters; use persisted best stars.
- Keep corrupt-save recovery silent and safe for families/non-technical players.
- Keep the map UI generated/minimal until the data and scene flow are tested.
