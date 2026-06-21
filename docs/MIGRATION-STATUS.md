# Mission Controller Migration Status

> Living ledger for the `IMissionController` migration. Update after every extraction.

## Current state

- PlayMode suite: **351 passed / 0 failed / 0 skipped** (2026-06-21).
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

## Remaining in `GameManager` (not yet extracted)

BackyardRescue, SnackHeist, SockPanic, SquirrelConspiracy, EagleShadowPanic, CoyotesFence,
WeenieRoundup, ScentSearch, ThunderstormComfort, LeashWalk, CarRide.

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
- **Shared services.** ThunderstormComfort uses the shared `PanicMeter` (a `MonoBehaviour` exposed
  via `game.Panic`, asserted reset across mission switches); squirrel/predator-driven missions use
  the shared actors. Decide per mission whether to own a controller-local actor (as Mark the Yard
  and Gate Crash do) or expose the service via `MissionContext`.

## Next step

All three puzzle-driven siblings (ChaosMachine, BlanketCatch, BoneRelay) are extracted.
Remaining missions are the squirrel/predator/legacy-actor cluster. ThunderstormComfort still needs
a `game.Panic`/`PanicMeter` hosting decision. Good next candidate: **ThunderstormComfort**
(resolve PanicMeter hosting, then extract) or **SockPanic** (position-tick + basket-tip state,
no shared actors).
