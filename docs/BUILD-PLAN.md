# Build Plan

Each milestone is independently shippable and must end green (typecheck + lint + tests + full-game sim). Ship in order — later milestones assume the scaffolding of earlier ones. **The prototype is the acceptance reference for every milestone.**

Legend: 🎯 goal · ✅ done-when · ⚠️ watch-out

---

## M0 — Project scaffold & guardrails
🎯 Stand up the toolchain and the safety net before porting any gameplay.
- Vite + TypeScript (strict), ESLint + Prettier.
- `index.html` thin host + a placeholder `main.ts` that opens a 960×600 DPR-scaled letterboxed canvas and renders a static frame.
- `core/rng.ts` (seedable), `core/math.ts` (port `rounded/shade/dist/clamp/rnd`), `core/camera.ts` (world↔screen transform).
- `tests/sim/harness.ts`: DOM/Canvas/AudioContext stubs (lift from the prototype's external harness).
- `npm run verify` = typecheck + lint + test. Wire CI-style local check.
✅ Blank canvas renders crisply on mobile + desktop; `npm run verify` passes with one trivial test.
⚠️ Get the DPR/letterbox transform and pointer→world conversion correct here; everything downstream depends on it.

## M1 — Core loop, one dog, movement
🎯 A controllable dog on an empty backyard.
- Fixed-timestep loop (`core/loop.ts`) with update/render split.
- `state/dog.ts` with the `MovementMode` model (see ARCHITECTURE). Port `moveDog` steering + **arrival easing** (the anti-jitter fix) + facing threshold.
- `core/input.ts`: touch-drag (primary) + WASD/arrows; world-space target marker.
- `render/dog.ts`: port the dachshund renderer — body/ears/tail/gait/bob. (Wet/swim/flip/jump/trail variants come later; stub them.)
- `scenes/yard.ts`: backyard background painter (fence, maple, lawn, flowers, patio).
✅ Drag the dog around the yard at correct speed with no on-spot frazzle; renders identically to prototype yard.
⚠️ Movement uses `dt*60` frame-scaling, NOT a divided-down value — the prototype had a units bug that made movement ~17× too slow. Port the corrected math.

## M2 — Scoring spine: toys, cuddle spot, HUD, scene flow
🎯 The minimal game loop: grab toys, hold a spot, see a score, rounds end.
- `state/sceneManager.ts`: round flow (yard→pool→house configs), per-scene init/reset, timers, interstitials, end screen. **One explicit reset function per scene entry.**
- `systems/toys.ts` (spawn + pickup; rope flag stubbed), `systems/spot.ts` (place/hold/steal), `state/gameState.ts` (central `addScore`).
- `render/hud.ts` (score pills, timer, scene name), `render/overlays.ts` (title/interstitial/end + dog-picker).
- `systems/particles.ts` (bursts/hearts/popups).
✅ A full single-round game is playable and scores correctly; round timer advances to the end screen.
⚠️ `addScore` is the single mutation point (M5 zoomies hooks into it). Don't scatter `score++`.

## M3 — The AI sibling (1P opponent)
🎯 The second dog plays on its own.
- `ai/sibling.ts`: target selection (nearest toy vs. spot), wander fallback, and dog-vs-dog collision/separation. (The **0.88× speed factor** is inert in the prototype — normalised away in `moveDog` — so the AI runs at full speed; matched per owner decision. See MECHANICS.md.)
- AI contests the cuddle spot.
✅ In 1P the AI dog competes for toys and the spot and feels like a sibling, matching prototype behavior.
⚠️ Keep AI logic out of render. AI reads state, returns an intent vector; movement system applies it.

## M4 — Wrestle
🎯 Flip your sibling.
- `systems/wrestle.ts`: range check + lunge, reversal odds (Cocoa ~0.78 / Cheddar ~0.70), knockback, stun, spot-steal-on-win, cooldown. Flipped-on-back render variant + dust VFX.
- HUD WRESTLE button + space/E; AI initiates wrestles.
✅ Both dogs can flip each other; immunity/tug interactions stubbed but wired.
⚠️ Wrestle eligibility gates (busy states) must be centralized; later systems (tug, belly-rub immunity, pool dunk) extend it.

## M5 — Pool round: floaters, swim/shake, water routing
🎯 The water round with real fall-in stakes.
- `scenes/pool.ts`: deck/coping/water rect, drifting floaters, caustics.
- Swim state (clipped underwater render), `splashIn`, `startShake` (wet/dry timers + drip/sheen render variants), `nearestDeck`.
- Wrestle-into-pool dunk; wet coat palette (derived in `config/dogs.ts`).
- AI pool smarts: **corner-waypoint routing** around the deck, short-hop-only floater judgment, post-shake `dryT` cooldown.
✅ Falling off a floater forces swim→edge→shake→rejoin; AI doesn't dunk-loop (validate with the pool sim, ~1–4 organic dunks/round).
⚠️ Spot/toy placement must sample the **deck bands**, never inside the water rect (prototype had a 42%-in-water placement bug). Port the band sampler.

## M6 — Zoomies, jump, tug-of-war
🎯 Movement flair + the two-dog rope minigame with growls.
- Zoomies: 3 quick scores → turbo + after-image trail (hooks `addScore`).
- Jump: arc + shadow shrink; JUMP button + J.
- `systems/tug.ts`: two dogs on a rope lock into tug; mash WRESTLE to pull; growl bed + popups; winner +3, loser tumbles; stalemate timeout. Rope-toy spawning (~30% of land toys). `core/audio.ts` growl/yip.
✅ Tug starts when both reach a rope and resolves to a winner or stalemate; zoomies + jump feel right.
⚠️ Tug locks both dogs (a `tug` mode) — make sure scene reset clears it and `busy()` includes it.

> **Order note (owner decision, 2026-06-14):** M7 and M8 were **swapped**. Predators + events
> are built first (no floor-plan dependency); the House is deferred to last so the owner can
> supply real photos/floor-plans for the data-driven room schema. Numbering below reflects the
> swap.

## M7 — Predators + ambient events (the co-op centerpiece)
🎯 Backyard danger that demands teamwork, plus the bonus events.
- `systems/predators.ts`: coyote (charge) + eagle (circle→dive→carry) FSMs; lone-dog targeting; **united-front** huddle defense (bark-off); grab/drag/carry + sibling rescue; jump/zoom dodge; score penalty on carry-off.
- **Hilarious bark animation** (owner ask): the united-front bark-off should read as comically over-the-top — exaggerated lunge/recoil, big "WOOF!" speech bursts, spit specks, ear/tail flail.
- `systems/events.ts`: squirrel chase (+3), treat drop with telegraph (+2), belly-rub power-up (3s wrestle immunity).
- AI predator response (flee to sibling, dodge, rescue) + event targeting.
✅ Predators threaten lone dogs and are repelled by huddling; AI cooperates in 1P; all events fire and resolve. Validate with the predator + event sims.
⚠️ Predators are backyard-only; ensure they never spawn/persist in pool/house. Reset on scene change.

## M8 — The House round
🎯 Three connected rooms modeled on the real house.
- `scenes/house/rooms.ts` room graph (foyer/family/rec), doors, **stairs with Cheddar-faster traversal**, furniture obstacles + `pushOut` collision.
- Per-room background painters (stone fireplace, gallery wall, brick wall, the **dog couch**, etc.).
- `systems/couch.ts` (premium hold spot, +5, cooldown), `systems/squish.ts` (squishmallow naps), room-aware toys/sunbeam.
- HUD sibling-locator pill; room-filtered rendering + transit hiding.
- AI cross-room navigation via door waypoints.
- **Data-driven room schema + `docs/ROOM-SCHEMA.md`** so the owner's photos/floor-plans drop in as map data (the art/map-data separation seam).
✅ House round plays across all three rooms; stairs, couch, squishmallows, doors all work; AI roams and scores room-to-room.
⚠️ Rendering must only draw the player's current room (or transit target); entities are room-tagged. Port the filtering carefully.

---

## Post-M8 stretch (not required for parity)

- **2P (same-screen):** dogs are already symmetric. Add a second input source (split-touch left/right, or keyboard+touch). For the house round, constrain both dogs to one room view in v1 (split rooms = split screen, a bigger lift). Decide co-op vs. versus scoring before building.
- **Settings:** mute toggle, round-length options, difficulty (AI speed factor).
- **Persistence:** high scores via the artifact storage API or localStorage (note: localStorage is blocked in the chat artifact sandbox but fine in a real deploy).
- **Polish:** screen shake, win celebration, more squishmallow variety, weather/time-of-day on the yard.
- **New levels:** see `docs/LEVEL-IDEAS.md` (e.g. the 🍗 Kitchen "food drop" round with asymmetric dog abilities — Cheddar eats fast but barfs + can chair-leap; Cocoa chews slow but safe). Queued, owner to promote.

## Definition of project-done (parity)

All of M0–M8 shipped and green; the built game matches the prototype across all three rounds in a side-by-side playtest; `npm run verify` (typecheck + lint + unit + full-game sim) passes; bundle is a single self-contained artifact with zero runtime network requests; prototype archived for reference.

---

# Co-op track (M9+) — the couch game

> Owner decision, 2026-06-15: evolve the parity build into a **local two-player cooperative**
> game for TV + iOS, keeping the TypeScript/Canvas stack (no engine port, no console dev-program
> gate). See `docs/COOP-VISION.md` for the why and the platform reality. Same rules apply: each
> milestone is independently shippable and ends green; **zero runtime network requests** holds
> for the desktop/iOS wrappers too.

## Phase 1 — Couch-playable

## M9 — Gamepad input
🎯 Play the existing game with a controller on a TV.
- New `GamepadSource` adapter feeding the same `Intent` shape `core/input.ts` already exposes
  (analog stick → `ax/ay/arrive`; A/B → `queueWrestle`/`queueJump`; D-pad fallback).
- Input abstraction: a player reads from *an* input source (touch | keyboard | gamepad), chosen
  per device. Touch stays the iOS-solo default.
- HUD: when a gamepad is active, hide the on-screen WRESTLE/JUMP buttons (controller-driven).
✅ One player completes a full game on a gamepad in landscape on a TV; touch/keyboard unchanged.
⚠️ Gamepad polling is per-frame (no events) — read it in the input phase of the fixed-step loop,
   not in render. Keep `computeIntent` pure; the gamepad adapter just produces its arguments.

## M10 — Local two-player ✅ BUILT (2026-06-15)
🎯 Two humans, two dogs, same screen.
- Player 2 drives the second `Dog` via a second input source; `ai/sibling.ts` becomes the
  **solo fallback** (a `partner: 'human' | 'ai'` choice at start).
- Controller assignment (P1/P2) + a lobby/"press a button to join" on the title overlay.
- Both dogs are player-eligible for every interaction (wrestle/tug/jump already symmetric).
✅ Two controllers play all existing rounds couch co-op; solo still works with the AI partner.
⚠️ Don't fork the dog update path per-controller — both dogs run the same systems; only the
   *intent source* differs. Scene reset must reassign inputs cleanly (the per-scene-reset rule).

> **Shipped.** `partner: 'human' | 'ai'` on `GameState` is the source of truth (default `'ai'`).
> `updateGame` takes an optional `p2?: DogCmd` (6th arg, zero churn to existing sims): in co-op
> the sibling is steered by P2 and runs the *same* movement/action systems as P1; in solo it
> falls back to `aiThink` + `maybeAiWrestle`. Human-only affordances (wrestle lunge/immune
> popup) gate on `isHuman(s,id)`; tug auto-mash is disabled in co-op (each dog mashes its own
> side). Input: `core/gamepad.ts` `GamepadSource(slot)` (P1=pad0, P2=pad1); `core/input.ts`
> `p1Command`/`p2Command` + keyboard split (P1 WASD, P2 arrows + `/`,`.`). Title lobby: 1P/2P
> toggle + press-Ⓐ-on-pad-2 to join; HUD/title show P1/P2/CPU tags. Competitive scoring kept
> (versus mode); combined co-op scoring is M12. Tests: `tests/sim/coop.test.ts`,
> `tests/core/input.test.ts` (79 green total).

## M11 — Wrappers: desktop (TV) + iOS — 🟡 SCAFFOLDED (2026-06-15)
🎯 Install and play off the web — on the TV and on the iPad.
- **Tauri v2 for both desktop AND iOS** (Tauri v2 added native iOS — supersedes the original
  Tauri-desktop + Capacitor-iOS split; one shell, one CSP). Verify two gamepads.
- iOS via `tauri ios` (built from the same crate); install via Xcode; verify touch + controllers.
- Scripts: `npm run build:desktop` (`tauri build`), `npm run desktop:dev`; see `docs/WRAPPERS.md`.
✅ A double-clickable desktop app and an installable iOS build run the M10 game; **zero network
   requests** confirmed in both (offline test).
⚠️ Re-verify the hard rule in each shell — wrappers can silently reintroduce remote assets.
   Everything stays bundled/inlined, same as the web artifact.

> **Scaffolded & validated, build pending toolchain.** `src-tauri/` (Cargo.toml, tauri.conf.json,
> build.rs, src/{main,lib}.rs, capabilities, generated icons) + npm scripts are committed; the web
> payload is `dist/index.html` (zero-network, already audited) and the CSP allows no http(s)
> origin. `npm run tauri info` parses the config and detects the Vite bundler. **Remaining (owner
> machine):** one-time `rustup` install (no Rust in CI/sandbox) to produce the desktop binary,
> and full Xcode + devices for the iOS build + offline on-device verification. Steps in
> `docs/WRAPPERS.md`.

> **End of Phase 1: the owner and his wife can play the current mechanics on the couch.**

## Phase 2 — The co-op design turn

## M12 — Mission framework + "needs both dogs" primitives ✅ BUILT (2026-06-15)
🎯 The structural shift from competitive rounds to cooperative missions.
- `systems/mission.ts`: objectives (reach/collect/survive/escort), success/fail, checkpoint
  + retry, combined score, optional 1–3 star rating. Data-driven, layered on `SceneDef`.
- Reusable interdependence gates: `bothOnSpots`, `boostJump` (one dog launches the other),
  `distract+grab`, paired pressure pads. Each is a small system with its own sim test.
- A **co-op mode toggle** on the title; competitive M2–M8 rounds kept as "versus / chaos" mode.
✅ A minimal co-op mission (reach the goal with both dogs through one gate) plays and resolves
   to success/fail; combined score correct; sim covers each gate primitive.
⚠️ Objectives mutate via a single point like `addScore` did — don't scatter completion flags.
   Gates must be testable headless (drive both dogs in the sim, assert the gate opens).

> **Shipped.** `mode: 'versus' | 'coop'` on `GameState` switches the scene registry (versus rounds
> kept intact). `systems/mission.ts` owns the framework: objectives with a single mutation point
> (`completeObjective`/`addCombined`/`setProgress`), success-when-all-done + time-bonus + 1–3★,
> fail-on-timeout, replay/retry. `systems/gates.ts` has the interdependence primitives — pressure
> pads (`updatePads`/`allPadsPressed`, one dog can't cover two), boost-jump (`canBoost`/
> `boostLaunch`), distract+grab (`isDistracted`). Minimal mission `scenes/missions/gate.ts`
> ("Through the Gate"): two pads latch a wall open, both pups reach the den. Title VERSUS/CO-OP
> toggle; co-op HUD (objective checklist + combined score) + mission success/fail screen with
> stars. Tests: `tests/sim/gates.test.ts`, `tests/sim/mission.test.ts` (95 green). The other
> three gate primitives are built + tested but not yet placed in a mission — that's M13 content.

## M13+ — Missions as content
🎯 Build the actual missions, one shippable mission per milestone.
- **Kitchen "food drop"** (promote `docs/LEVEL-IDEAS.md`): asymmetric abilities — Cheddar
  chair-leaps + barfs, Cocoa steady; co-op angle (Cheddar knocks table food down for Cocoa).
- **Car trip**, **house rescue**, **yard predator escort**, … (owner to prioritize).
- Each mission = `SceneDef` + painter + objective config + any net-new gate system + sim test.
✅ Per mission: plays start→success/fail with both dogs, needs genuine teamwork, sim-green.
⚠️ Asymmetric abilities are a balance shift — budget a sim + playtest pass per new ability,
   the way AI speed / wrestle odds were tuned. New constants go in `config/balance.ts`.

## Co-op definition-of-done

Phase 1 shipped and green (two controllers, TV + iOS, zero-network in every shell); the mission
framework + gate primitives are tested headless; at least the first co-op mission plays
start-to-finish requiring both dogs; `npm run verify` stays green throughout; competitive mode
preserved.
