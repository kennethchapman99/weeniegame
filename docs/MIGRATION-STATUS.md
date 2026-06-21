# Mission Controller Migration Status

> Living ledger for the `IMissionController` migration. Update after every extraction.

## Current state

- PlayMode suite: **343 passed / 0 failed / 0 skipped** (2026-06-21).
- Tree is compile-clean and out of Safe Mode.

## Extracted (controller-owned)

| Mission | Controller | Notes |
| --- | --- | --- |
| Kitchen Falling Food Frenzy | `KitchenFoodFrenzyMissionController` | First extraction; established the boundary. |
| Operation Pee Break | `PeeBreakMissionController` | First controller-native deep slice. |
| Mark the Yard | `MarkTheYardMissionController` | First previously-in-`GameManager` mission extracted. Added `IMissionController.OutcomeSummary` and `MissionContext.CreditDog`. |

## Remaining in `GameManager` (not yet extracted)

BackyardRescue, SnackHeist, SockPanic, SquirrelConspiracy, EagleShadowPanic, CoyotesFence,
WeenieRoundup, ScentSearch, ThunderstormComfort, LeashWalk, CarRide, GateCrash, TableStealth,
SquirrelSwitcheroo, WalkCampaign, BoneRelay, GreatEscape, ChaosMachine, BlanketCatch.

## Contract additions so far

- `IMissionController.OutcomeSummary` — controller-owned end-of-round summary phrase; return null to
  use the shared `Outcome.ToString()` default.
- `MissionContext.CreditDog` — lets a controller drive the MVP/contribution tally.

## Known contract gaps for upcoming extractions

- **Non-timeout failure.** Almost every remaining mission fails early (e.g. too many wasted digs,
  breaches, snaps, exposures, panic maxed). The interface currently signals only clear via
  `IsComplete`. The next extraction of an early-fail mission must add a controller fail signal
  (e.g. `bool IsFailed` + optional `FailReason`) wired into `CheckClear`/`EndReasonFor`.
- **Interact/dig/collect input.** Several missions route gameplay through `OnDogInteracted`/treat
  collection rather than bark+position. Those need an input-forwarding hook on the controller.
- **Shared services.** ThunderstormComfort uses the shared `PanicMeter`; squirrel/predator-driven
  missions use the shared actors. Decide per mission whether to own a controller-local actor (as
  Mark the Yard does for its reclaimer) or expose the service via `MissionContext`.

## Next step

Pick the next un-migrated mission. A good candidate with the current interface plus a fail signal is
**ThunderstormComfort** (tick + position only, no interact/collect; fail = panic maxed) — extracting
it would establish the controller fail-signal pattern. Alternatively pick a position/bark-driven
mission to defer the interact-input work.
