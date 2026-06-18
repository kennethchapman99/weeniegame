# Production Readiness

Status as of 2026-06-18: **local macOS release candidate ready; public release approval blocked on
human validation and distribution credentials.**

## Verified automatically

- Unity 6 project compiles outside Safe Mode.
- `87/87` PlayMode tests pass with no failures or skips.
- All 12 missions start from mission select and have deterministic smoke/replay coverage.
- P1/P2 controller ownership is isolated; one controller cannot drive both dogs.
- Both dogs have complete keyboard move, bark, and interact paths.
- Mission select, replay, next mission, session summary, New Session, and pause flows are covered.
- Eight authored pose-atlas states are wired for both Cheddar and Cocoa, with generated fallback art.
- Event SFX slots, rumble requests, and a looped backyard music bed follow the audio toggle.
- Release build uses `com.kennethchapman.cheddarandcocoa`, version `0.1.0`, and a release player
  rather than Unity development/debug options.
- `./unity/validate-release.sh` runs tests, creates the release app, validates metadata, and boots the
  packaged player for a startup smoke test.
- Current release app is a universal `x86_64`/`arm64` macOS bundle, approximately `116 MB` on disk.

## Required before a public demo

These gates cannot be truthfully replaced by more headless automation:

1. Two new players complete a 20-minute blind couch session with two physical controllers.
2. Record observations against the protocol in `docs/ARENA-PLAYABLE.md`, especially objective
   comprehension, dog identity, bark usefulness, tug communication, and end-flow navigation.
3. Visually approve every cropped dog pose in the packaged build at gameplay zoom; replace any
   keyed-background halo with a native-alpha export.
4. Replace or explicitly approve the generated event tones with recorded bark/UI/warning/success/
   fail audio, then perform a speaker/headphone mix pass.
5. Sign with the intended Developer ID/Steam identity and notarize as required. The local candidate
   is structurally valid but ad-hoc signed (`TeamIdentifier` is absent).
6. Upload through the intended distribution account and run the platform-specific install/launch/
   controller test. Steam app/depot credentials are not stored in this repository.

## Commands

```sh
./unity/run-playmode-tests.sh
./unity/validate-demo.sh
./unity/validate-release.sh
```

Release output:

```text
unity/builds/release/CheddarAndCocoa-Demo.app
```
