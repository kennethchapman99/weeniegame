# Agent Sprint 01

Objective: land the first production mission.

Duration:
- 1 to 3 Codex sessions.

## Task A

Wire existing helpers into GameManager.

Required:
- MissionRankCalculator
- ScoreEventCatalog
- MissionRuntimeSnapshot
- MissionSeedGenerator

## Task B

Implement Squirrel Conspiracy.

Required runtime state:
- HerdingMissionState

Required gameplay:
- route progression
- cutoff progression
- fake-out tracking
- stash reveal
- stash found clear
- taunt fail

Required tests:
- clear path
- fail path
- replay reset

## Task C

Session Summary

Use:
- MissionOutcomeSummaryBuilder

Goal:
- funny outcome labels;
- readable replay feedback.

## Definition Of Done

- all existing tests pass;
- new tests pass;
- mission selectable;
- mission replayable;
- no manual debug actions required.

## 2026-06-18 Landing Notes

Landed in this sprint:
- Wired the live Arena mission flow to include `SquirrelConspiracy` in mission order, mission select, keyboard hotkeys, session tracking, runtime snapshots, score catalog events, seed-driven round setup, rank calculation, and squirrel outcome summaries.
- Implemented the first playable pass of The Great Backyard Squirrel Conspiracy inside `ArenaScene`/`GameManager`: deterministic route nodes, cutoff/herd scoring, fake-out bark penalties, stash reveal, stash found clear, taunt fail, and replay reset coverage.
- Added deterministic PlayMode coverage for select availability, opening objective, herd/cutoff/fake-out scoring, stash reveal/found clear, taunt fail, and replay reset.

Known limitations / next steps:
- Cutoff zones are still lightweight generated logic based on dog spacing and route pressure; next sprint should add clearer placeholder zone markers and stronger dog-role readability.
- Placeholder squirrel/stash visuals are label-driven; final art/audio remains intentionally unblocked.
