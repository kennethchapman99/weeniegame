# Testing

> **Status: ACTIVE — UNITY PLAYMODE.** The layers below are how the Unity project (the only active
> codebase) is actually tested today. The frozen TypeScript prototype's harness at the bottom of this
> file is historical reference only — do not run or extend `npm run verify`/`vitest` for current work.

## Layers

1. **Unity PlayMode tests (primary).** Live under `Assets/Tests/PlayMode/` in the Unity project.
   Each mission controller exposes deterministic `Force*` test hooks that drive the same state
   machine live play uses (e.g. `GateCrashMissionController`'s `ForceGateHold`/`ForceGateCross`),
   so tests don't depend on real input timing or randomness. Per `MISSION-AUTHORING-FRAMEWORK.md`,
   each controller needs coverage for start state, a success event, a recoverable failure, clear/fail
   paths, cleanup, replay reset, snapshot state, and session-flow integration.
2. **Scene-load smoke tests.** A `SceneManager.LoadSceneAsync("SceneName", ...)` test per scene in
   `ProjectSettings/EditorBuildSettings.asset`, proving the real scene bootstrap builds without error
   (distinct from testing the underlying logic via direct instantiation).
3. **Packaged-build smoke checks.** Prove the compiled player itself starts cleanly, not just the
   editor - a different code path (stripping, build config) that editor PlayMode tests don't exercise.

## Run targets

- `./unity/run-playmode-tests.sh` — headless run of the full PlayMode suite (currently 400+ tests).
  Requires a licensed Unity editor pinned to the project's `6000.0.x` version; writes results to
  `unity/playmode-results.xml`. This is the primary gate — keep it green after every change.
- `./unity/build-dev.sh` — development configuration build.
- `./unity/build-release.sh` — release configuration build (separate stripping/config path from dev).
- `./unity/smoke-player.sh /path/to/Game.app` — launches a packaged player briefly to prove clean
  runtime startup, then stops it.
- `./unity/validate-demo.sh` — deterministic local validation for the ArenaScene demo slice.
- `./unity/validate-release.sh` — full local release-candidate gate: tests, release build, metadata,
  and packaged startup, combined.

## Determinism

Mission controllers drive the exact same logic live play and tests use — a test calls a `Force*`
hook, which advances the same puzzle/state-machine primitive (`CoopHoldReleasePuzzle`,
`CoopSocialManipulationPuzzle`, etc.) that real dog positions/input would. `PlayModeGlobalTestSetup`
(an assembly-wide `[SetUpFixture]`) resets things like the round lead-in delay to zero so the legacy
suite runs at full speed; tests that need real timing (e.g. `LeadInPlayModeTests`) explicitly opt
back in via a static override.

---

## Historical: frozen TypeScript prototype test harness (do not implement)

> **HISTORICAL — DO NOT IMPLEMENT.** This section describes the pre-Unity TypeScript/Canvas
> prototype's test approach. `src/`, `tests/`, and `prototype/` remain frozen, read-only references
> for porting behavior/balance — do not add to or run this harness for current work.

The prototype was kept honest by a **headless simulation harness**: it stubs the browser, drives the
real game `update()` at a fixed `dt`, and asserts on resulting state. Every regression in the build
history was caught this way before shipping. This approach substantially informed how the Unity
PlayMode layer above is structured (deterministic hooks, fixed-step advancement, no reliance on real
input timing).

### Layers (prototype-era)

1. **Headless sim (primary).** Stub `document`, a Canvas2D context (Proxy returning no-ops + gradient stubs), and `AudioContext`. Import the game's systems, build a known `GameState`, run N fixed steps, assert. This is where predator FSMs, pool routing, tug resolution, scoring, and scene flow were verified.
2. **Pure unit tests.** `math.ts`, `rng.ts`, geometry helpers (`inRect`, `pushOut`, deck-band sampler, room-graph waypoints).
3. **Full-game smoke sim.** Play yard→pool→house to `state==='end'` without throwing. Stayed green at every milestone. Run with a fixed RNG seed and also a few random seeds.

### Harness sketch (prototype-era, frozen)

```ts
// tests/sim/harness.ts
const noop = () => {};
const grad = { addColorStop: noop };
const ctx2d = new Proxy({}, { get: (_t, p) => {
  if (p === 'createLinearGradient' || p === 'createRadialGradient') return () => grad;
  if (p === 'measureText') return () => ({ width: 50 });
  if (p === 'createImageData') return (w: number, h: number) => ({ data: new Uint8ClampedArray(w*h*4) });
  return noop;
}, set: () => true });
// stub document.getElementById, createElement('canvas'), window, AudioContext...
// then: import { newGame, update } from '../../src/...'; drive update(1/60).
```

### Tests that existed (prototype-era, mirrored the prototype's validation suite)

| Test | Asserts |
|---|---|
| full 3-round game | reaches `state==='end'` without throwing, multiple seeds |
| zoomies | 3 quick `addScore`s sets `zoom > 0` |
| squirrel | a dog on the squirrel scores it |
| treat | a dog on a landed treat picks it up |
| belly immunity | flopping grants `immune>0`; wrestling an immune dog does nothing |
| predator grab | a lone, still dog gets grabbed within a few spawns |
| united front | two huddled dogs flip the predator to `flee` |
| tug resolves | both dogs on a rope → tug starts and ends with a winner or stalemate |
| pool no dunk-loop | AI dunk rate ~1–4/round, AI still scores |
| spot placement | over many placements, none land inside the pool water rect |
| stairs | Cheddar transit < Cocoa transit |

### Prototype-era run targets (frozen, do not run for current work)

- `npm run verify` → `tsc --noEmit && eslint . && vitest run` (the prototype's "milestone done" gate).
- `npm run sim` → just the full-game smoke sim, for quick iteration.

### Prototype-era determinism

All gameplay randomness went through an injected seedable `rng`. Tests set the seed; a failing run
was reproducible by re-seeding. No bare `Math.random()` in `src/` logic (renderer-only cosmetic
jitter was the lone exception, and even that was better seeded).
