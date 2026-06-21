# Codex Goal: Squirrel Conspiracy

> **Status: COMPLETED / HISTORICAL.** The mission is implemented. Do not execute these instructions
> or add their branches to `GameManager`; see `NEXT-PRODUCTION-SLICE.md` for active work.

Implement the first production-style expansion mission in Unity only.

Repo guardrails:
- Work only under `unity/CheddarAndCocoa/` and `docs/`.
- Do not edit frozen TS/Canvas code under `src/` or `tests/`.
- Keep the first pass inside `ArenaScene` and `GameManager` mission flow.
- Keep PlayMode tests deterministic.

Goal:
Add a selectable mission variant named `SquirrelConspiracy` / `The Great Backyard Squirrel Conspiracy`.

Gameplay:
- This is a chase/herding mission, not a food-steal mission.
- One dog pressures the squirrel with bark/proximity.
- The other dog gets rewarded for standing in cutoff zones.
- Barking too early causes a fake-out penalty.
- Enough successful herds/cutoffs reveals a hidden stash.
- Finding the stash clears the mission.
- Repeated taunt-branch escapes or timer expiry fails it.

Implementation target:
- Extend `GameManager.MissionVariant` and `MissionOrder`.
- Add `BuildMissionDefinition` branch with unique copy, timing, score labels, clear/fail reasons.
- Add lightweight state fields for route index, herds, cutoffs, fake-outs, stash reveal/found, taunts.
- Add generated placeholder route/cutoff/stash objects if needed.
- Reuse existing actor labels, score pops, audio/rumble cue requests, objective arrows, and event log.
- Add direct test hooks only if needed, such as `ForceSquirrelConspiracyCutoff`, `ForceSquirrelFakeOut`, or `ForceStashReveal`.

Score labels:
- `GOOD HERD`
- `CUTOFF`
- `DOUBLE BARK BLOCK`
- `FAKE OUT`
- `STASH FOUND`
- `CONSPIRACY CRACKED`

Tests:
- mission select count increases and includes the new mission;
- starting the mission sets the expected name and objective;
- cutoff success changes score and event log;
- early bark/fake-out changes score and event log;
- stash reveal changes objective;
- stash found clears the mission;
- replay resets score, outcome, route/stash state.

Validation:
Run `./unity/run-playmode-tests.sh`.
