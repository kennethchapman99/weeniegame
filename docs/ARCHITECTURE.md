# Architecture

> **Status: ACTIVE UNITY ARCHITECTURE.** The TypeScript architecture remains below as historical
> reference only. All implementation work belongs in `unity/CheddarAndCocoa/`.

## Current pressure

As of 2026-06-20, `GameManager.cs` is 7,954 lines and declares 21 mission variants. This volatile,
date-stamped measurement explains the pivot; it is not a completion target. The problem is unclear
ownership: mission-specific lifecycle and state are mixed with shared session orchestration.

## Target Unity ownership

### `GameManager`

`GameManager` ultimately owns only:

- orchestration of the active mission controller;
- mission selection and transition requests;
- session flow, replay/next/select, and session summary;
- shared-service construction and wiring;
- forwarding Unity lifecycle/input events into the active controller where appropriate.

It must not own mission-specific setup, mutable state, ticking branches, input interpretation,
cleanup, clear/fail rules, or mission snapshots.

### `IMissionController`

Each controller owns one mission's:

- setup and initial state;
- mission-specific mutable state;
- ticking and time advancement;
- input handling and role locks;
- world actor/prop lifecycle and cleanup;
- outcome and clear/fail reasoning;
- deterministic test hooks;
- runtime snapshot used by tests, HUD, challenges, and result summaries.

The exact interface should be the narrowest shape that supports the existing Kitchen behavior and
tests. Expected capabilities include initialize/start, tick, input, cleanup, outcome, snapshot,
and explicit deterministic test advancement. Do not expose all of `GameManager` to controllers.

### `MissionContext`

`MissionContext` is a narrow dependency bundle supplied to controllers. It may expose stable shared
services such as dog access, scoring, HUD/feedback, audio/rumble, seeded randomness, arena/world
helpers, and session-safe callbacks. It must not become a second god object or a back door to all
`GameManager` fields.

Prefer small interfaces in the context over concrete manager access. A controller should be
testable with a deliberately small context fixture.

### Definitions and registration

Mission definitions and the mapping from mission id/variant to controller factory live outside
`GameManager`. The registry creates the matching controller and definition; `GameManager` selects
and runs them. Data describes identity and common presentation. Controllers own behavior.

```text
Mission catalog/definitions ──┐
Controller registry/factories ├──> GameManager ──> active IMissionController
Shared services ──────────────┘                         │
                                                       └── narrow MissionContext
```

## Incremental migration plan

The migration is not a big-bang rewrite:

1. Run the baseline two-player couch playtest and fix critical findings.
2. Define `IMissionController`, narrow `MissionContext`, external mission definitions, and external
   controller registration only to the extent needed for the first extraction.
3. Extract the existing Kitchen mission first without changing its gameplay or public test hooks.
4. Run the full PlayMode suite and proceed only when green.
5. Build Operation Pee Break entirely as a controller; do not add fallback mission branches to
   `GameManager`.
6. Run the second couch playtest as the acceptance gate.
7. Extract other existing missions one at a time only when prioritized, with a green suite after
   every extraction.

Done means the ownership boundary is true and behavior remains verified. A specific line count is
not an architecture acceptance criterion.

## Implemented boundary — 2026-06-21

- `IMissionController` now defines lifecycle, tick/input forwarding, cleanup, objective targeting,
  entry staging, outcome state, and controller-produced snapshots.
- `MissionContext` supplies only the dogs, arena bounds/sprites, scoring, feedback, event logging,
  deterministic randomness, and world-label helpers required by the first controller.
- `MissionControllerRegistry` owns controller factories and `MissionCatalog` owns the extracted
  Kitchen definition outside `GameManager`.
- Controller registration is atomic: each migrated mission registers its controller factory and
  presentation-definition factory together. Runtime guards reject a controller or definition whose
  declared variant does not match its registration.
- `MissionContext` dependencies are constructor-supplied, immutable, and validated before a
  controller can initialize; controllers cannot replace shared services after wiring.
- `KitchenFoodFrenzyMissionController` owns Kitchen setup/reset, mutable state, generated actors,
  ticking, bark role rules, feedback, objective copy, arrow targets, cleanup, deterministic hooks,
  and runtime snapshot construction.
- `GameManager` retains thin compatibility accessors/hooks for existing PlayMode tests while
  forwarding runtime work through the active controller.
- Full PlayMode result after extraction: 333 passed, 0 failed, 0 skipped.
- `PeeBreakMissionController` is the first new controller-native mission. It owns four-beat exact
  social-signal progression, role locks, bladder/phone pressure, misreads, generated world stations,
  united-bark climax, cleanup, snapshots, and deterministic advancement. `GameManager` only
  registers, forwards bark/tick work, and retains one compatibility test hook.
- Pee Break pressure presentation is controller-owned: it updates world-space bladder, phone, and
  Teenager-state labels, drains the phone only from Cocoa's active unplug stimulus, and visibly
  powers the phone down when the charger gambit succeeds.
- Pee Break lifecycle coverage verifies mission switches deactivate every owned actor, reuse the
  cached controller without duplicate world objects, and reset mission state on re-entry.
- `MarkTheYardMissionController` is the first extraction of a previously-in-`GameManager` mission. It
  owns zone geometry (the single `ComputeZones` source the `GameManager.TerritoryZones` compat
  accessor delegates to), claim-by-proximity ticking, a controller-owned squirrel reclaimer with the
  legacy speed/interval timing, scoring/credit/feedback, objective copy, arrow targets, entry
  staging, cleanup, snapshot, and the deterministic `ForceClaimZone`/`ForceSquirrelReclaim` hooks.
  `GameManager` keeps only thin forwarding hooks and an empty-state fallback accessor for the period
  when Mark the Yard is not the active controller.
- `IMissionController` gained `OutcomeSummary` (controller-owned end-of-round phrase, null to defer
  to the shared default) and `MissionContext` gained `CreditDog` so controllers can drive the MVP
  tally without reaching into `GameManager`.
- `GateCrashMissionController` is the first extracted mission with a non-timeout failure. The
  contract gained `IsFailed` and `FailReason`: `CheckClear` now ends the round on either
  `IsComplete` or `IsFailed`, and `EndReasonFor` prefers a non-empty controller `FailReason` (with
  `FailReason` returning null on a plain timeout so the shared `TimeFailReason` still applies). The
  controller owns its gate/toy markers, hold/squeeze proximity ticking, snap handling, objective
  copy, arrow targets, snapshot, and the `ForceGateHold`/`ForceGateCross` hooks.
- `TableStealthMissionController` reuses the same pattern for the human-distraction puzzle: it owns
  the human/steak markers, flop+sneak proximity ticking, exposure handling (its `IsFailed`/
  `FailReason` fire at the exposure cap), objective copy, snapshot, and the `ForceTableFlop`/
  `ForceTableBurp`/`ForceTableSneak` hooks.
- `SquirrelSwitcherooMissionController` owns the bait/raid puzzle: decoy/stash markers, the
  feint-commitment + one-strike-per-window ticking, raid/backfire handling (backfire-cap fail via
  `IsFailed`/`FailReason`), objective copy, snapshot, and the `ForceSwitcherooBait`/
  `ForceSwitcherooStrike` hooks.
- Full PlayMode result after the latest controller increment: 346 passed, 0 failed, 0 skipped.

## Test and reset contract

- Preserve existing public test entry points while moving their implementation behind controllers.
- Every controller must reset all owned state on replay and cleanup all owned actors/resources.
- Snapshots must be controller-produced; `GameManager` may wrap them with shared session data.
- Deterministic test hooks must advance the same state machine used by live play.
- Registry contract tests must keep controller factories, definitions, and variant identities in
  sync, and must explicitly preserve the set of migrated missions while the roster is frozen.
- Never move to a second extraction while the first leaves the PlayMode suite red.

---

## Historical TypeScript architecture (frozen reference)

> **HISTORICAL — DO NOT IMPLEMENT.** This section records the pre-Unity TypeScript/Canvas target.
> `src/`, `tests/`, and `prototype/` remain read-only behavior and tuning references.

The frozen TypeScript target used strict TypeScript, Vite, Canvas 2D, Web Audio, Vitest, a typed
`GameState`, deterministic fixed-step simulation, and seedable randomness. Its useful lessons still
apply in Unity:

- keep logic deterministic and independently testable;
- centralize shared score mutations;
- reset every scene/mission-owned state explicitly;
- model mutually exclusive dog movement states explicitly;
- separate input intent, gameplay state, and presentation;
- use the prototype and simulation tests as behavior references when porting older mechanics.

The former `src/core`, `src/state`, `src/systems`, `src/scenes`, `src/render`, and `tests/sim`
directory plan is historical. It must not be resumed, extended, or treated as the active product
architecture.
