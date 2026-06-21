# Mission Controller Migration Status

> Living ledger for the `IMissionController` migration. Update after every extraction.

## Current state

- PlayMode suite: **352 passed / 0 failed / 0 skipped** (2026-06-21).
- Tree is compile-clean and out of Safe Mode.

## Extracted (controller-owned)

| Mission | Controller | Notes |
| --- | --- | --- |
| Kitchen Falling Food Frenzy | `KitchenFoodFrenzyMissionController` | First extraction; established the boundary. |
| Operation Pee Break | `PeeBreakMissionController` | First controller-native deep slice. |
| Mark the Yard | `MarkTheYardMissionController` | First previously-in-`GameManager` mission extracted. Added `IMissionController.OutcomeSummary` and `MissionContext.CreditDog`. |
| Gate Crash | `GateCrashMissionController` | First extracted mission with a non-timeout fail. Added `IMissionController.IsFailed`/`FailReason`. |
| Table Stealth | `TableStealthMissionController` | Human-distraction hold/sneak puzzle; exposure-cap fail via `IsFailed`/`FailReason`. |
| The Ol' Switcheroo | `SquirrelSwitcherooMissionController` | Bait/raid puzzle; backfire-cap fail. |
| The Walk Campaign | `WalkCampaignMissionController` | Two-station social-manipulation puzzle; misread-cap fail. |
| The Great Escape | `GreatEscapeMissionController` | Alternating-owner sequence-chain contraption puzzle; botch-cap fail. |
| Chaos Machine | `ChaosMachineMissionController` | Time-pressure conveyor puzzle; own `CoopChaosMachinePuzzle` actor. |
| The Blanket Catch | `BlanketCatchMissionController` | Stretch-span co-op puzzle; `CoopStretchSpanPuzzle`; rip-cap fail. |
| The Bone Detail | `BoneRelayMissionController` | Split-information relay; `CoopScentRelayPuzzle`; own scent post + 4 mounds (no shared SquirrelObject). Stage Cocoa 5u from post (outside ScentRange=3.5f) so arrow shows and auto-reveal is suppressed on first tick. |
| Thunderstorm Comfort | `ThunderstormComfortMissionController` | Panic co-regulation; shares `PanicMeter` via `MissionContext.PanicMeter` (GameManager still owns the MB; `game.Panic` accessor unchanged). `_cleared` flag gates `IsComplete` so `StormCleared` score fires exactly once inside `ApplyThunderclap`. `TickThunderstorm`/`ThunderClap` removed from GameManager. |

## Remaining in `GameManager` (not yet extracted)

BackyardRescue, SnackHeist, SockPanic, SquirrelConspiracy, EagleShadowPanic, CoyotesFence,
WeenieRoundup, ScentSearch, LeashWalk, CarRide.

## Contract additions so far

- `IMissionController.OutcomeSummary` — controller-owned end-of-round summary phrase; return null to
  use the shared `Outcome.ToString()` default.
- `MissionContext.CreditDog` — lets a controller drive the MVP/contribution tally.
- `IMissionController.IsFailed` / `FailReason` — controller-owned non-timeout failure. `CheckClear`
  ends the round on clear or fail; `EndReasonFor` prefers a non-empty `FailReason`. Return null from
  `FailReason` on a plain timeout so the shared `TimeFailReason` still applies.

## Known contract gaps for upcoming extractions

- **Interact/dig/collect input.** Several missions route gameplay through `OnDogInteracted`/treat
  collection rather than bark+position. Those need an input-forwarding hook on the controller.
- **Squirrel/predator-driven missions.** BackyardRescue, SnackHeist, SockPanic, SquirrelConspiracy,
  EagleShadowPanic, CoyotesFence, WeenieRoundup, ScentSearch use the shared squirrel actor or
  treat-collection system. Decide per mission whether to own a controller-local actor or expose via
  `MissionContext`.

## Next step

ThunderstormComfort extracted. Remaining missions are the squirrel/predator/legacy-actor cluster.
**SockPanic** is the cleanest next candidate: position-tick, basket-tip state, no shared squirrel
actor. LeashWalk and CarRide have self-contained state too. SquirrelConspiracy/EagleShadowPanic/
CoyotesFence/WeenieRoundup need their shared actors exposed or owned by the controller.
