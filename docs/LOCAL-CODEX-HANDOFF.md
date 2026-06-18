# Local Codex Handoff

Use this when running Codex locally against the repo.

## Always Read First

1. `README.md`
2. `AGENTS.md`
3. `CLAUDE.md`
4. `docs/GAME-DESIGN-BIBLE.md`
5. `docs/ARENA-PLAYABLE.md`
6. `docs/MISSION-SYSTEM.md`
7. The active goal file under `docs/CODEX-GOAL-*.md`

## Hard Rules

- Build only in Unity: `unity/CheddarAndCocoa/`.
- Do not edit frozen TS/Canvas code under `src/` or `tests/`.
- Keep compact mission variants inside `ArenaScene` until a real authored level pipeline exists.
- Do not introduce save/progression/campaign systems until the mission mechanics are proven.
- Every new mission needs deterministic PlayMode tests.
- Keep placeholder art/audio replaceable and clearly named.

## Prompt Shape

Use one goal per Codex run.

Template:

```text
Read the repo guardrails and implement docs/CODEX-GOAL-SQUIRREL-CONSPIRACY.md.

Constraints:
- Unity only.
- Keep first pass inside ArenaScene/GameManager mission flow.
- Add deterministic PlayMode tests.
- Do not touch frozen TS/Canvas code.
- Run ./unity/run-playmode-tests.sh and report results.

Commit when green.
```

## Recommended Order

1. `docs/CODEX-GOAL-SQUIRREL-CONSPIRACY.md`
2. `docs/CODEX-GOAL-EAGLE-SHADOW.md`
3. `docs/CODEX-GOAL-COYOTES-FENCE.md`

These three form the Backyard Expansion Pack and should land before pool, house, car, vet, or walkies systems.

## Review Checklist

Before accepting a Codex commit:

- Mission starts from mission select.
- Objective copy is readable.
- Both dogs have useful jobs.
- Bark/interact cannot be solved by spam.
- Clear/fail/replay work.
- Score events explain cause and effect.
- Session summary still works.
- Existing three missions still pass tests.
- New tests cover new state and reset paths.
