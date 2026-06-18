# Agent PR Checklist

Use this before merging any agent-created change.

## Scope

- Change stays in Unity and docs unless explicitly requested.
- Frozen TS/Canvas code remains untouched.
- No unrelated refactors.
- No new package dependency without justification.

## Gameplay

- Objective is readable.
- Both dogs have useful roles.
- Bark/interact has meaningful timing or positioning.
- Clear path works.
- Fail path works.
- Replay resets all new state.

## Tests

Required:
- relevant PlayMode tests pass;
- new mission/system has deterministic tests;
- existing missions still pass.

Command:

```sh
./unity/run-playmode-tests.sh
```

## UX

- Mission select copy is clear.
- Score labels explain cause/effect.
- End summary explains outcome.
- No debug-only instruction required for normal play.

## Risk Review

Call out:
- soft-lock risk;
- state reset risk;
- controller risk;
- readability risk;
- placeholder art/audio risk.

## Merge Rule

Do not merge if tests are not run or if replay/reset is untested.
