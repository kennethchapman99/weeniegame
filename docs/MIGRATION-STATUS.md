# Mission Controller Migration Status

> Living ledger for the `IMissionController` migration. Update after every extraction.

## Current state

- PlayMode suite: **357 passed / 0 failed / 0 skipped** (2026-06-21).
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
| Walkies on the Leash | `LeashWalkMissionController` | Owns checkpoint route, tether ticking, snaps, snap-cap failure, entry staging, and reset. |
| Sock Panic | `SockPanicMissionController` | Owns basket timing, partner-only sock dives, collectible interpretation, and fumble recovery. |
| Car Ride Balance | `CarRideMissionController` | Owns the car actor, balance/counter-lean ticking, lurch timing, and spill-cap failure. |
| Scent Search | `ScentSearchMissionController` | Owns seeded dig spots, sniff/dig input, objective targeting, and cold-dig failure. |
| Weenie Roundup | `WeenieRoundupMissionController` | Owns loose/carry actors, pickup/delivery ticking, cargo state, and fumble recovery. |
| Squirrel Conspiracy | `SquirrelConspiracyMissionController` | Owns route/cutoff geometry, herding state, taunts, stash interaction, markers, failure, and snapshots; temporarily consumes the shared squirrel actor through `MissionContext`. |
| Snack Heist | `SnackHeistMissionController` | Owns recovery/steal state, squirrel targeting/timing, bark defense, collectible interpretation, failure, and snapshots; consumes the shared squirrel and treat pool through narrow context services. |

## Remaining in `GameManager` (not yet extracted)

BackyardRescue, EagleShadowPanic, CoyotesFence.

## Contract additions so far

- `IMissionController.OutcomeSummary` — controller-owned end-of-round summary phrase; return null to
  use the shared `Outcome.ToString()` default.
- `MissionContext.CreditDog` — lets a controller drive the MVP/contribution tally.
- `IMissionController.IsFailed` / `FailReason` — controller-owned non-timeout failure. `CheckClear`
  ends the round on clear or fail; `EndReasonFor` prefers a non-empty `FailReason`. Return null from
  `FailReason` on a plain timeout so the shared `TimeFailReason` still applies.

## Known contract gaps for upcoming extractions

- **Shared legacy actors.** BackyardRescue still uses the shared squirrel/treat loop and trap state;
  EagleShadowPanic and CoyotesFence repurpose both squirrel and predator actors. Decide per mission
  whether to own controller-local actors or expose the minimum shared references through
  `MissionContext`.

## Next step

Snack Heist extracted. Remaining missions are the squirrel/predator/legacy-actor cluster.
**Backyard Rescue** can now reuse the controller-owned squirrel/treat path while adding its trap-role
state. Eagle Shadow Panic and Coyotes at the Fence remain larger two-actor extractions.
