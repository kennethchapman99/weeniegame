# Local Codex Handoff

> **Status: ACTIVE.** Use this handoff for local agent work. `README.md`, `AGENTS.md`, `CLAUDE.md`,
> and this file must prescribe the same sequence.

## Always read first

1. `README.md`
2. `AGENTS.md`
3. `CLAUDE.md`
4. `docs/README.md`
5. `docs/NEXT-PRODUCTION-SLICE.md`
6. `docs/GAME-DESIGN-BIBLE.md`
7. `docs/DEEP-SLICE-OPERATION-PEE-BREAK.md`
8. `docs/ARCHITECTURE.md`
9. `docs/ARENA-PLAYABLE.md`

Do not select a `CODEX-GOAL-*.md` file as active work; those files record completed historical
missions.

## Canonical work sequence

1. Run a baseline two-player couch playtest of the existing slices.
2. Address critical playtest findings.
3. Define `IMissionController` and a narrow `MissionContext`.
4. Extract the existing Kitchen mission first, keeping all PlayMode tests green.
5. Build Operation Pee Break entirely through the new controller structure.
6. Run a second couch playtest as the deep-slice acceptance gate.
7. Keep the mission roster frozen until that gate passes.

## Hard rules

- Build only in Unity: `unity/CheddarAndCocoa/`.
- Do not edit frozen TS/Canvas code under `src/`, `tests/`, or `prototype/`.
- Do not add mission-specific state or behavior branches directly to `GameManager`.
- Keep each controller extraction behavior-preserving and PlayMode-test-green before continuing.
- Keep mission definitions and controller registration outside `GameManager`.
- Do not use an arbitrary `GameManager` line count as the definition of done.
- Do not add missions or resume deferred roadmap work before the deep-slice couch gate passes.

## Prompt shape

Use one gate-sized goal per run. Example for the first coding step after playtest fixes:

```text
Read the active depth-pivot docs. Define IMissionController and a narrow MissionContext, then
extract the existing Kitchen mission without changing behavior.

Constraints:
- Unity only.
- No new gameplay scope or mission variants.
- Mission-specific setup, state, ticking, input, cleanup, outcome, and snapshots belong to the
  Kitchen controller.
- Keep GameManager limited to orchestration, selection, session flow, and shared-service wiring.
- Run ./unity/run-playmode-tests.sh and stop if the extraction is not green.

Commit when green.
```

## Review checklist

- The current sequence gate was respected.
- No mission variant or gameplay scope was added prematurely.
- Mission behavior is owned by a controller rather than new `GameManager` branches.
- Replay/reset, clear/fail, objective copy, score events, and snapshots remain covered.
- Existing PlayMode tests pass after the extraction.
- Manual couch-playtest findings and acceptance evidence are recorded where required.
