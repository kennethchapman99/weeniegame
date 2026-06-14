# Testing

The prototype was kept honest by a **headless simulation harness**: it stubs the browser, drives the real game `update()` at a fixed `dt`, and asserts on resulting state. Every regression in the build history was caught this way before shipping. Port this approach as the project's primary test layer — it tests behavior and feel-adjacent logic, not just units.

## Layers

1. **Headless sim (primary).** Stub `document`, a Canvas2D context (Proxy returning no-ops + gradient stubs), and `AudioContext`. Import the game's systems, build a known `GameState`, run N fixed steps, assert. This is where predator FSMs, pool routing, tug resolution, scoring, and scene flow are verified.
2. **Pure unit tests.** `math.ts`, `rng.ts`, geometry helpers (`inRect`, `pushOut`, deck-band sampler, room-graph waypoints).
3. **Full-game smoke sim.** Play yard→pool→house to `state==='end'` without throwing. Must stay green at every milestone. Run it with a fixed RNG seed and also a few random seeds.

## Harness sketch (port from the prototype's external harness)

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

The prototype harness already exists in spirit in the build history; lift its stubs verbatim — they're proven to let the real code run unmodified.

## Tests that must exist (mirror the prototype's validation suite)

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

## Run targets

- `npm run verify` → `tsc --noEmit && eslint . && vitest run` (the gate for "milestone done").
- `npm run sim` → just the full-game smoke sim, for quick iteration.

## Determinism

All gameplay randomness goes through an injected seedable `rng`. Tests set the seed; a failing run is reproducible by re-seeding. No bare `Math.random()` in `src/` logic (renderer-only cosmetic jitter is the lone exception, and even that is better seeded).
