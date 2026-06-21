# Production Readiness

> **Status as of 2026-06-20: TECHNICAL READINESS EVIDENCE, NOT FUN READINESS.** Automated checks and
> packaging can establish that the build runs consistently. They cannot establish that the game is
> clear, funny, or worth replaying on the couch.

## Technical readiness already demonstrated

- The Unity 6 project has compiled outside Safe Mode.
- A date-stamped prior run reported 87/87 PlayMode tests passing with no failures or skips.
- Mission select, input ownership, replay/next/session flow, pause, and deterministic mission smoke
  coverage have automated checks.
- A macOS release build, metadata validation, and packaged startup smoke path exist through
  `./unity/validate-release.sh`.
- Art/audio fallbacks and release identifiers are wired sufficiently for internal technical tests.

These claims are snapshots, not promises that the current checkout is green. Re-run the commands
before relying on them.

## Fun readiness gate

Fun readiness is blocked on human couch playtesting:

1. Run a baseline two-player session with two physical controllers across the existing slices.
2. Record confusion, role ownership, camera/readability failures, recovery friction, laughter, and
   dead time using `ARENA-PLAYABLE.md`.
3. Address critical findings.
4. Complete the Kitchen-first controller extraction and build Operation Pee Break through it.
5. Run a second two-player couch session focused on the deep slice.

Operation Pee Break is not accepted because tests pass. The deep-slice gate passes only when the
human session validates its teach/explore/twist/climax flow, funny failures, role reversal, and
cathartic payoff. Keep the mission roster frozen until then.

## Public distribution readiness

Public distribution remains a separate later gate. It may require final visual/audio approval,
the intended signing/notarization identity, distribution credentials, and platform install/launch/
controller testing. None of those tasks should displace the active depth-first sequence.

## Verification commands

```sh
./unity/run-playmode-tests.sh
./unity/validate-demo.sh
./unity/validate-release.sh
```
