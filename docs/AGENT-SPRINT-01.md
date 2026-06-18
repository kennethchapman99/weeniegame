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
