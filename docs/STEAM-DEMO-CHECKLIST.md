# Steam Demo Checklist

> **Status: DEFERRED.** Public-demo scope and distribution work do not resume until Operation Pee
> Break passes its human couch-playtest acceptance gate. Test-count claims below are historical
> snapshots and must be re-run before use.

## Gameplay

Required:
- Backyard Rescue
- Snack Heist
- Sock Panic
- Squirrel Conspiracy
- Eagle Shadow Panic
- Coyotes at the Fence

## Quality

- No blocker bugs.
- No broken mission flow.
- No missing objectives.
- No soft locks.
- No required debug actions.

## Controls

- Keyboard supported.
- Controller supported.
- Menu navigation works.
- Replay works.

## Art

- Final dog models.
- Final mission UI.
- Final score feedback.
- Placeholder props acceptable only when readable.

## Audio

- Bark set.
- UI sounds.
- Mission music.
- Warning cues.

## Performance

Target:
- Stable framerate.
- No noticeable hitches during mission flow.

## Automated Release Gate

Verified locally on 2026-06-18:

- `87/87` PlayMode tests pass.
- macOS release build succeeds without development/debug build flags.
- bundle identity is `com.kennethchapman.cheddarandcocoa`, version `0.1.0`.
- packaged-player startup smoke passes.
- release app is universal `x86_64`/`arm64` and approximately `116 MB` on disk.

Run `./unity/validate-release.sh` to reproduce the gate. See
`docs/PRODUCTION-READINESS.md` for the remaining human/signing blockers.

## Player Test

Two new players should:
- understand controls;
- understand objectives;
- complete missions;
- want to replay.

Without developer explanation.
