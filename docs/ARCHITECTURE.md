# Architecture

## Target stack

| Concern | Choice | Why |
|---|---|---|
| Language | TypeScript (strict) | Catches the brace/scope/typo class of bug that hit the prototype |
| Build | Vite | Fast dev server, single-file bundle output for the webview constraint |
| Rendering | Canvas 2D (keep) | Prototype's painterly look is hand-drawn 2D; no need for WebGL yet |
| Audio | Web Audio synth (keep) | Already file-free; wrap in a small `AudioBus` module |
| Tests | Vitest + a headless sim harness | Port the proven external harness into the repo |
| State | Plain modules + a typed `GameState` object | No framework; deterministic and testable |

**No game engine.** The prototype proves vanilla Canvas is enough. Adding Phaser/Pixi now would be a rewrite tax for little gain. Revisit only if a milestone (e.g. heavy particles, 2P split-view) actually demands it — and call that out before adopting it.

## Target directory layout

```
cheddar-and-cocoa/
  index.html                # thin host; mounts the game, no logic
  src/
    main.ts                 # bootstrap: canvas, loop, scene manager
    core/
      loop.ts               # fixed-timestep update + render split
      rng.ts                # seedable RNG (injected everywhere logic randomizes)
      input.ts              # touch-drag + keyboard → intent vector; world-space conversion
      audio.ts              # AudioBus: growl/bark/yip/screech/splash
      math.ts               # rounded(), shade(), dist(), clamp(), helpers
      camera.ts             # 960x600 world → DPR-scaled letterboxed canvas
    state/
      gameState.ts          # typed top-level state; scene + score + entities
      dog.ts                # Dog entity: movement states, wet/swim/zoom/tug/immune flags
      sceneManager.ts       # round flow, per-scene init/reset, timers
    systems/                # each: pure-ish update(state, dt) + optional draw(ctx, state)
      movement.ts           # steering, arrival easing, zoomies, jump arc, pushOut
      wrestle.ts            # flip/reversal, knockback, immunity, pool-dunk
      tug.ts                # two-dog rope tug-of-war + growl bed
      toys.ts               # spawn (deck/floater/room aware), pickup, rope-toy flagging
      spot.ts               # cuddle spot place/hold/steal
      couch.ts              # dog couch (house) hold + cooldown
      squish.ts             # squishmallow naps (house)
      sunbeam.ts            # bask scoring + standoff + relocation
      predators.ts          # coyote + eagle FSMs, grab/carry, united-front defense
      events.ts             # squirrel, treat drop, belly-rub power-up
      particles.ts          # bursts, hearts, drips, dust, motes
    ai/
      sibling.ts            # the AI dog brain: target selection, pool routing, predator response
    scenes/
      yard.ts               # backyard background painter + scene config
      pool.ts               # pool background, floaters, water rect, swim/shake loop
      house/
        rooms.ts            # room graph: foyer/family/rec, doors, stairs, obstacles
        foyer.ts family.ts rec.ts   # per-room background painters
    render/
      dog.ts                # the dachshund renderer (wet/swim/flip/jump/trail variants)
      hud.ts                # score pills, timer, scene name, sibling locator
      overlays.ts           # title, interstitial, end screens
    config/
      balance.ts            # ALL tuning constants (see docs/MECHANICS.md)
      dogs.ts               # palettes incl. derived "wet" variants
  tests/
    sim/harness.ts          # headless DOM/canvas/audio stubs + driver
    *.test.ts               # per-system behavioral tests + full-game sim
  docs/                     # this folder (carried over)
  prototype/                # the reference single-file build (do not delete)
```

## How the prototype maps onto this

The prototype is one file but is already loosely sectioned by comment banners. Use these as extraction seams:

| Prototype section (comment banner) | Target module |
|---|---|
| `DOG RENDERER` | `render/dog.ts` |
| pre-rendered paper grain | `render/` util |
| `tiny synth audio` | `core/audio.ts` |
| `GAME STATE` + `mkDog` | `state/gameState.ts`, `state/dog.ts` |
| input listeners (`pointerdown`, keys) | `core/input.ts` |
| `toys` + `spawnToy` + rope flag | `systems/toys.ts` |
| `cuddle spot` (`placeSpot`, draw) | `systems/spot.ts` |
| floaters + swim/shake (`splashIn`, `startShake`, `nearestDeck`) | `scenes/pool.ts` + `systems/movement.ts` |
| `SUNBEAMS` | `systems/sunbeam.ts` |
| `THE HOUSE` room graph + bg painters | `scenes/house/*` |
| `TUG OF WAR` | `systems/tug.ts` |
| `PREDATORS` | `systems/predators.ts` |
| `YARD/HOUSE EVENTS` | `systems/events.ts` |
| `moveDog`, `jumpHeight`, `addScore` | `systems/movement.ts`, `state/dog.ts` |
| `aiThink` | `ai/sibling.ts` |
| `update(dt)` | `core/loop.ts` orchestrating system `update()`s |
| `render()` | `core/loop.ts` orchestrating system/render `draw()`s |
| `doWrestle`, `dust`, `syncBtn` | `systems/wrestle.ts` + `render/hud.ts` |

## The Dog entity (most important type)

A dog carries a lot of mutually-exclusive movement states. In the port, model this explicitly rather than as a bag of booleans. Current flags (from `mkDog`): position/velocity/facing, `score`, and state: `onFloater`, `stun`, `swim`, `shake`, `dryT` (wet timer), `transit` (door/stair), `carry`, `jumpT`, `inTug`, `zoom`, `immune`, plus cooldowns (`bumpCD`, `wrestleCD`) and AI scratch (`aiTx/aiTy/aiWanderT`, `hist`, `trail`).

Recommended: a small discriminated `MovementMode` (`'free' | 'stunned' | 'swimming' | 'shaking' | 'transit' | 'tug'`) for the exclusive states, with timers/effects (`zoom`, `immune`, `dryT`, `jumpT`) as overlays. `busy(dog)` becomes "mode is not free." This kills a whole class of "two states at once" bugs.

## Determinism & the test harness

The prototype was validated by an external Node harness that stubs `document`, a Canvas2D context, and `AudioContext`, then drives `update(1/60)` thousands of times and asserts on state. Port this into `tests/sim/harness.ts`. Every system test should: build a known state, run N fixed steps, assert. Seed the RNG so failures reproduce. The full-game sim (play all three rounds to `state==='end'` without throwing) is the smoke test that must stay green.
