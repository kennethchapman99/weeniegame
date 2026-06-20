# Arena Mission System

The ArenaScene mission spike now proves that the existing Cheddar/Cocoa arena loop can run a broad set of dog-game objectives without needing bespoke scenes for each mission. It is still the gameplay proving ground, but campaign ownership is moving to the Adventure Progression layer in `docs/PROGRESSION-SYSTEM.md`.

## Runtime Shape

`ArenaBootstrap` still builds the same scene and creates one `GameManager`. `GameManager` owns:

- `FlowState` - the lightweight local flow: `MissionSelect`, `Playing`, `EndScreen`, and `SessionSummary`.
- `MissionVariant` - the selectable variant id.
- `MissionDefinition` - serializable data for names, objective copy, collectible counts, enabled pressure systems, scoring labels, and end reasons.
- `StartMission(MissionVariant variant)` - code/test/manual entry point that swaps the definition and restarts the round.
- Session counters - local-only missions played, total score, stars, unique missions finished, and rank labels.

Manual selection now has two paths:

- Cold start opens the generated two-column IMGUI mission picker. Arrow keys or D-pad move in the visible grid; Enter/Space/Start/South starts the selected mission.
- Number keys `1-9` and `0` directly start the first ten missions for fast testing; arrow/grid select covers the full 12-mission rotation (Walkies and Car Ride included).

After a mission ends, the same local flow exposes Replay, Next Mission, and Mission Select. Session Summary remains a local run recap. Persistent stars, best ranks, location unlocks, and saved progress now belong to the Adventure Progression layer rather than this arena-local session loop.

## Foundation And First Production Variant

| Variant | Objective | Enabled systems | Unique scoring |
| --- | --- | --- | --- |
| Backyard Rescue | Save weenies, stop squirrel pressure, resolve predator, complete tug | Collectibles, squirrel, predator, tug | `WEENIE SAVED`, `SQUIRREL SCARED`, `PREDATOR YEETED`, `PARTNER RESCUE`, `TUG COMPLETE` |
| Snack Heist | Stash snacks before squirrel steals too many | Collectibles, squirrel | `SNACK STASHED`, `SNACK GUARD BARK`, `SNACK THIEF` |
| Sock Panic | Return scattered socks before time expires | Collectibles, timer | `SOCK RESCUED` |
| Squirrel Conspiracy | Herd the squirrel, hold route cutoffs, reveal and find its stash | Herding route, cutoff zones, bark timing, stash | `GOOD HERD`, `CUTOFF`, `FAKE OUT`, `STASH FOUND`, `CONSPIRACY CRACKED` |
| Eagle Shadow Panic | Hide from sweeping eagle danger, rescue the toy, form a united front | Threat sweep, cover, rescue, united bark | `SAFE HIDE`, `SHADOW DISTRACTED`, `TOY RESCUED`, `UNITED FRONT` |
| Coyotes at the Fence | Bark-pin the coyote and repair weak fence gaps | Patrol defense, repair, lure, united block | `FENCE HELD`, `DIRT FILLED`, `COYOTE BLOCKED`, `YARD DEFENDED` |
| Weenie Roundup | Pick up loose weenies and deliver them to the bowl | Carry, pickup, drop/fumble, bowl delivery | `PICKUP`, `DELIVERED`, `FUMBLE`, `ROUNDUP CLEAR` |
| Scent Search | Sniff/dig the right mound and avoid cold digs | Scent reads, dig spots, hot/cold feedback | `WARM SNIFF`, `BONE DUG UP`, `COLD DIG` |
| Thunderstorm Comfort | Huddle to calm panic through thunderclaps | Panic meter, storm pulses, comfort proximity | `COMFORT`, `CLAP SURVIVED`, `PANIC FAIL` |
| Mark the Yard | Claim and defend territory zones | Territory zones, reclaim pressure | `ZONE MARKED`, `RECLAIMED`, `YARD CLAIMED` |
| Walkies on the Leash | Reach checkpoints together while managing leash snaps | Leash physics, checkpoints, snap penalties | `CHECKPOINT`, `LEASH SNAP`, `WALK COMPLETE` |
| Car Ride Balance | Balance against car lurches and avoid spills | Vehicle balance, lurches, spill pressure | `STEADIED`, `SPILL`, `RIDE COMPLETE` |

## Co-op Puzzle Beat Requirement

The current variants prove mechanic coverage, but future work should raise the bar from "objective loop" to "co-op puzzle beat." Use `docs/COOP-PUZZLE-DESIGN.md` as the standard.

A mission definition should identify at least one beat where:

- Dog A creates the opening through a dog-authentic action: bark, bait, hold, sniff, distract, anchor, tug, block, or survive.
- Dog B converts that opening into progress through a different action.
- The world changes visibly after success.
- Failure is funny and teaches the missing dependency.
- The beat can be checked manually and, where feasible, through deterministic PlayMode hooks.

For the current ArenaScene set, the weakest puzzle candidates are the most direct objective loops: Backyard Rescue, Snack Heist, Sock Panic, Weenie Roundup, and Mark the Yard. They are playable and useful, but future polish should add specific co-op locks rather than simply increasing counts, speed, or timers.

## Adding The Next Mission

Do not add the next mission yet. The current priority is the AdventureMap/progression spine. Once the progression layer is playable and tested, use this sequence for future mission work:

1. Decide which location owns the mission in `AdventureLocationCatalog` or future location assets.
2. Name the **Co-op Puzzle Beat** before adding code: player roles, lock/key relationship, readable hints, funny fail, world-state change, and test hooks.
3. Add a new value to `GameManager.MissionVariant`.
4. Add the value to `GameManager.MissionOrder` only if it should appear in the generated arena picker and local session loop.
5. Add a new `MissionDefinition` branch in `BuildMissionDefinition`.
6. Decide which existing systems are enabled:
   - `UsesSquirrel`
   - `RequiresPredator`
   - `RequiresTug`
7. Give the mission unique objective text, score labels, clear banner, replay prompt, and fail reasons.
8. Add readable placeholder collectible art in `BuildCollectibleArt` if the existing shapes do not fit.
9. Add deterministic PlayMode coverage that starts the mission from mission select, checks objective copy, scores one unique event, reaches clear/fail, verifies the co-op puzzle beat state change, and verifies replay/next/mission-select state.
10. Update `docs/ARENA-PLAYABLE.md`, `docs/COOP-PUZZLE-DESIGN.md`, and `docs/PROGRESSION-SYSTEM.md` if the mission affects location unlocks, saved progress, or the reusable puzzle vocabulary.

## Guardrails

- Do not create a new scene for a small mission variant.
- Do not bypass `AddScore`, `EndRound`, or `StartMission`; tests rely on those single mutation paths.
- Keep Cheddar/Cocoa art and identity generated by the existing dog feedback components until final sprite/prefab slots are ready.
- Bark should remain useful in at least some active mission pressure. If a mission disables squirrel/predator, document that it is a collect/timer proof rather than the new default loop.
- Prefer one or two meaningful objective differences over many shallow variants.
- Do not add more mission variants before AdventureMap, save/load, and unlock tests are in place.
- Do not approve a new mission whose only co-op requirement is parallel collection, simultaneous standing in a circle, or shared survival without a lock/key puzzle beat.

## Warning Status

The previous `LevelObjective.surviveSeconds` warning was low-risk and related to objective data. It is now exposed through `LevelObjective.SurviveSeconds`, matching the existing read-only `Kind` and `Label` properties. The `LevelObjective` manager is still a future stub; ArenaScene mission variants currently run through `GameManager.MissionDefinition`.

## Backyard Pack: Squirrel Conspiracy (2026-06-18)

- Added `GameManager.MissionVariant.SquirrelConspiracy` / **The Great Backyard Squirrel Conspiracy** to the Arena mission select and mission order.
- The mission uses `HerdingMissionState` for deterministic route progress, herd/cutoff counts, fake-outs, taunts, stash reveal, and stash found clear state.
- Gameplay loop: the nearest dog follows **BARK HERD** guidance while the partner holds the active generated **HOLD CUTOFF** zone. A nearby bark scores `GOOD HERD`; a nearby bark while the partner occupies that route's zone scores `CUTOFF`; an early/far bark scores a visible `FAKE OUT` penalty. Four controls reveal the stash with `DOUBLE BARK BLOCK`, then interaction awards `STASH FOUND` and `CONSPIRACY CRACKED`.
- Fail path: repeated squirrel taunts or timer expiry fails the mission; replay resets route, stash, score, and outcome state.
- Production helpers are now part of the live path where relevant: `MissionRankCalculator`, `ScoreEventCatalog`, `MissionRuntimeSnapshot`, `MissionSeedGenerator`, and `MissionOutcomeSummaryBuilder`.
- A replay reuses the exact `MissionSeedGenerator` seed, preserving the round modifier and deterministic layout instead of advancing with session counters.
- `DemoReadinessGate` now reports its backyard acceptance status in the F1 playtest overlay.
