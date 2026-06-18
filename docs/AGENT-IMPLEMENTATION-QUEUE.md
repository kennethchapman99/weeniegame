# Agent Implementation Queue

This is the execution queue for local Codex/Claude agents.

## Rule

Do not skip ahead to new levels until the current queue item is playable and tested.

## Queue 1: Wire Production Systems

Goal: stop the new production types from being passive scaffolding.

Tasks:
- Replace any duplicated rank/star logic in `GameManager` with `MissionRankCalculator`.
- Use `ScoreEventCatalog` labels when adding new mission score events.
- Use `MissionSeedGenerator` for deterministic mission variant seeds.
- Add tests for the wired behavior.

Acceptance:
- Existing Arena PlayMode tests pass.
- `ProductionSystemsPlayModeTests` pass.
- No behavior regression in Backyard Rescue, Snack Heist, Sock Panic.

## Queue 2: Squirrel Conspiracy

Goal: first new production mission.

Read:
- `docs/CODEX-GOAL-SQUIRREL-CONSPIRACY.md`
- `docs/MECHANIC-MODULES.md`
- `docs/PRODUCTION-BACKLOG.md`

Tasks:
- Add mission variant.
- Add route/cutoff/stash state.
- Add score events.
- Add clear/fail/replay tests.

Acceptance:
- Mission is playable from mission select.
- It can clear and fail.
- Replay resets all new state.

## Queue 3: Eagle Shadow Panic — DONE

Implemented as a selectable `EagleShadowPanic` mission variant wired into `GameManager` on top of `ThreatSweepMissionState`: shadow-sweep hide phase (`SAFE HIDE` / `EAGLE SPOOK` exposure), a split-role toy rescue (`SHADOW DISTRACTED` / `TOY RESCUED`), and a final united-front bark circle (`UNITED FRONT` / `SHADOW PANIC CLEAR`). Three exposures fail the run. Covered by `EagleShadowPanicPlayModeTests` (select rotation, clear path, exposure fail, replay reset); full PlayMode suite green at 39/39.

## Queue 4: Coyotes at the Fence — DONE

Implemented as a selectable `CoyotesFence` mission variant wired into `GameManager` on top of `PatrolDefenseMissionState`: a coyote tests fence gaps, one dog bark-pins it (`FENCE HELD`) while the partner fills the weak spot (`DIRT FILLED`) — repairs only progress while bark pressure is held. A late fake-snack lure (`FAKE SNACK BAIT`, Cheddar-specific gag) can be resolved by barking instead of taking the bait. After enough repairs the final push is blocked with a united bark (`YARD DEFENDED`). Three breaches fail the run. Covered by `CoyotesFencePlayModeTests` (select rotation, bark-gated repair + clear, fake-snack lure, breach fail, replay reset); full PlayMode suite green.

## Bonus missions — DONE (beyond the original queue)

Three more missions were added on top of the queue to broaden the dog-verb coverage and make the large yard feel like a real game. All are selectable from mission select, deterministically tested, and shipped on `main`:

- **Weenie Roundup** (`CarryRoundupMissionState`) — the **carry** verb: pick up scattered weenies and carry them to the HOME BOWL; both dogs can carry in parallel; fumbles bounce the weenie away. `WeenieRoundupPlayModeTests`.
- **Scent Search** (`ScentSearchMissionState`) — the **sniff + dig** verbs: bark to read HOT/COLD on a buried bone, interact to dig the right mound; cold digs are penalized. `ScentSearchPlayModeTests`.
- **Thunderstorm Comfort** (`ThunderstormMissionState` + `PanicMeter`) — the **comfort** verb: huddle to co-regulate panic and ride out timed thunderclaps; leans into Cheddar (spooks harder) vs Cocoa (steadier). `ThunderstormComfortPlayModeTests`.

The arena was also grown to a real 48x28 yard with a dynamic clamped follow-cam, decorative backyard dressing, and spatial geometry for the threat missions (Eagle cover zones, Coyote fence gaps). Full PlayMode suite green at 59/59.

## Queue 5: Launch Demo Hardening

Tasks:
- controller pass;
- readability pass;
- art replacement slots;
- audio pass;
- Steam demo checklist.

## Agent Reporting Format

Every agent result should report:
- files changed;
- tests run;
- pass/fail;
- known limitations;
- next recommended queue item.
