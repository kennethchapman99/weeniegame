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

## Queue 3: Eagle Shadow Panic

Only start after Queue 2 is green.

## Queue 4: Coyotes at the Fence

Only start after Queue 3 is green.

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
