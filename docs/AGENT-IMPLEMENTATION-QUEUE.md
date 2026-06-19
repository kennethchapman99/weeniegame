# Agent Implementation Queue

This is the execution queue for local Codex/Claude agents.

## Rule

Do not skip ahead to new levels until the current queue item is playable and tested.

Every new or expanded mission must now include a named **Co-op Puzzle Beat** per `docs/COOP-PUZZLE-DESIGN.md`: player roles, lock/key dependency, readable hints, funny fail, world-state change, and deterministic test hooks where feasible.

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

The arena was subsequently grown to a 120x68 scrolling yard with a dynamic clamped follow-cam, landmark districts, an explicit <=2% dog-to-property scale contract, and full-yard spatial geometry for the threat missions (Eagle cover zones, Coyote fence gaps).

## Queue 5: Launch Demo Hardening

Automated hardening complete:
- isolated P1/P2 controller ownership plus full two-player keyboard interactions;
- authored Cheddar/Cocoa pose-atlas runtime art with fallback slots;
- pause/resume/select/quit flow;
- event audio/rumble slots plus looped backyard music;
- compressed release build, metadata verification, and packaged startup smoke;
- `87/87` PlayMode tests green.

Remaining gates require a two-player blind playtest, final audio/art approval, and public signing/
distribution credentials. See `docs/PRODUCTION-READINESS.md`.

## Queue 6: Co-op Puzzle Quality Pass — NEXT

Goal: upgrade the current mission set from straightforward objective loops into memorable co-op puzzle beats before adding many more levels.

Read:
- `docs/COOP-PUZZLE-DESIGN.md`
- `docs/GAME-DESIGN-BIBLE.md` / **Co-op puzzle magic standard**
- `docs/MISSION-SYSTEM.md` / **Co-op Puzzle Beat Requirement**
- `docs/PRODUCTION-BACKLOG.md` / **Phase 0: Co-op Puzzle Quality Pass**
- `docs/PUZZLE-BEAT-DISHWASHER-CATAPULT.md` as a concrete example of the desired lock/key, gross comedy, and dog-specific co-op dependency for future Kitchen / House Chaos work.

First targets:
1. **Backyard Rescue** — add a squirrel trap / redirected escape beat.
2. **Snack Heist** — add a distract-and-sneak food theft beat.
3. **Sock Panic** — add a laundry basket / hidden sock beat.

Future Kitchen candidate:
- **Dishwasher Catapult / Barf Pressure Plate** — Cheddar eats people food to charge a Barf Meter and activate a gross pressure sensor, while Cocoa/partner pins the springy dishwasher door so the dogs can reach the food without being launched.

Acceptance:
- Each upgraded mission has a named Co-op Puzzle Beat in docs and code comments/test names where useful.
- Each beat includes a role split, lock/key dependency, readable hinting, funny fail, and visible world-state change.
- Each beat has deterministic PlayMode coverage where feasible.
- Existing mission select, replay, next mission, pause, audio/rumble toggles, and release validation keep working.
- The missions become more surprising without becoming harder to understand.

## Agent Reporting Format

Every agent result should report:
- files changed;
- tests run;
- pass/fail;
- known limitations;
- next recommended queue item.
