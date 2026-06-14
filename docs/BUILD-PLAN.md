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

## M7 — The House round
🎯 Three connected rooms modeled on the real house.
- `scenes/house/rooms.ts` room graph (foyer/family/rec), doors, **stairs with Cheddar-faster traversal**, furniture obstacles + `pushOut` collision.
- Per-room background painters (stone fireplace, gallery wall, brick wall, the **dog couch**, etc.).
- `systems/couch.ts` (premium hold spot, +5, cooldown), `systems/squish.ts` (squishmallow naps), room-aware toys/sunbeam.
- HUD sibling-locator pill; room-filtered rendering + transit hiding.
- AI cross-room navigation via door waypoints.
✅ House round plays across all three rooms; stairs, couch, squishmallows, doors all work; AI roams and scores room-to-room.
⚠️ Rendering must only draw the player's current room (or transit target); entities are room-tagged. Port the filtering carefully.

## M8 — Predators + ambient events (the co-op centerpiece)
🎯 Backyard danger that demands teamwork, plus the bonus events.
- `systems/predators.ts`: coyote (charge) + eagle (circle→dive→carry) FSMs; lone-dog targeting; **united-front** huddle defense (bark-off); grab/drag/carry + sibling rescue; jump/zoom dodge; score penalty on carry-off.
- `systems/events.ts`: squirrel chase (+3), treat drop with telegraph (+2), belly-rub power-up (3s wrestle immunity).
- AI predator response (flee to sibling, dodge, rescue) + event targeting.
✅ Predators threaten lone dogs and are repelled by huddling; AI cooperates in 1P; all events fire and resolve. Validate with the predator + event sims.
⚠️ Predators are backyard-only; ensure they never spawn/persist in pool/house. Reset on scene change.

---

## Post-M8 stretch (not required for parity)

- **2P (same-screen):** dogs are already symmetric. Add a second input source (split-touch left/right, or keyboard+touch). For the house round, constrain both dogs to one room view in v1 (split rooms = split screen, a bigger lift). Decide co-op vs. versus scoring before building.
- **Settings:** mute toggle, round-length options, difficulty (AI speed factor).
- **Persistence:** high scores via the artifact storage API or localStorage (note: localStorage is blocked in the chat artifact sandbox but fine in a real deploy).
- **Polish:** screen shake, win celebration, more squishmallow variety, weather/time-of-day on the yard.

## Definition of project-done

All of M0–M8 shipped and green; the built game matches the prototype across all three rounds in a side-by-side playtest; `npm run verify` (typecheck + lint + unit + full-game sim) passes; bundle is a single self-contained artifact with zero runtime network requests; prototype archived for reference.
