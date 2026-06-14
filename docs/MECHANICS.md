# Mechanics & Balance Reference

These are the **playtested, simulation-validated** constants from the prototype. Port them into `config/balance.ts` as named values; do not re-derive or "improve" them without re-running the relevant sim. Values below are pulled directly from the prototype source.

## World & frame

| Constant | Value | Notes |
|---|---|---|
| World size | 960 × 600 logical units | Letterboxed, DPR-scaled (cap 3) |
| Play bounds | x∈[50, 910], y∈[215, 555] | `BOUNDS` |
| Timestep | fixed, `dt` in seconds; movement uses `pos += v * dt * 60` | **Not** divided down — the prototype's original bug |

## Movement speeds (per-frame velocity targets, pre-`dt*60`)

| State | Speed | Notes |
|---|---|---|
| Free (land) | 4.4 | base |
| On a floater | 4.9 | slightly faster than land |
| Swimming / in water | 1.6 | the penalty for falling in |
| Zoomies | base × 1.85 | turbo after a hot streak |
| AI sibling | player-equivalent × **0.88** | the "feels fair but beatable" factor |
| Arrival easing | speed scales to 0 within ~10px of target; hard stop inside | kills on-spot jitter |
| Facing flip threshold | |vx| > 0.8 | prevents left/right flicker when parked |

## Round flow

| Round | Key | Time | Notes |
|---|---|---|---|
| 1 | yard | 45s | zoomies, squirrels, **predators** |
| 2 | pool | 45s | floaters, swim/shake, water routing |
| 3 | house | 75s | 3 rooms, stairs, couch, squishmallows |

## Scoring

| Action | Points | Via |
|---|---|---|
| Toy pickup | +1 | `addScore(d,1)` |
| Rope tug — solo grab (uncontested) | +2 | |
| Rope tug — win | +3 | |
| Cuddle spot (hold 3s) | +3 | |
| Dog couch (hold 4s) | +5 | house premium; 6s cooldown |
| Squishmallow nap (~1.2s still) | +1 | house |
| Sunbeam bask | +1 per 3.5s solo | standoff if both present |
| Treat | +2 | telegraphed drop |
| Squirrel | +3 | fence sprint |
| Carried off by predator | −1 | penalty, min 0 |

## Wrestle

| Constant | Value |
|---|---|
| Win/reversal odds (attacker) | Cocoa 0.78 · Cheddar 0.70 |
| Cooldown | 2.6s (0.5s on a whiffed lunge) |
| Loser stun | 1.35s |
| Range | ≤95px (lunges if just outside) |
| Blocked by | belly-rub immunity, tug lock, swim/shake/transit, prior stun |
| Side effects | knockback; steals spot/couch on win; dunks loser if near pool water |

## Zoomies

| Constant | Value |
|---|---|
| Trigger | 3 scores within an 8s rolling window |
| Duration | 4s |
| Effect | ×1.85 speed + after-image trail; knocks sibling on contact |

## Jump

| Constant | Value |
|---|---|
| Duration | 0.5s arc (sin) |
| Use | dodge predators (jumpHeight > 0.3 at strike), leap, flair |

## Tug-of-war

| Constant | Value |
|---|---|
| Trigger | both dogs within ~40px of a rope toy, both free |
| Rope range | −1..+1; win at ±0.98 |
| AI mash strength | Cocoa 2.6 · Cheddar 2.3 |
| Stalemate | rope flies away at 14s, no score |
| Rope-toy spawn rate | ~30% of land toys (never in pool) |
| Audio | continuous growl bed (~0.5–0.85s cadence), yip on win |

## Pool

| Constant | Value |
|---|---|
| Fall-in | stepping off a floater into open water → swim |
| Recovery | swim to deck edge → shake (0.9s) → free |
| Wet timer (`dryT`) | 4.5s after shake (slick render + drips); AI stays off floaties during it |
| Spot/toy placement | sampled from 4 **deck bands** only, never the water rect |
| AI routing | corner-waypoint graph around the pool; short-hop-only floater judgment |
| Validated dunk rate | ~1–4 organic AI dunks per round |

## House

| Constant | Value |
|---|---|
| Rooms | foyer ⇄ family (hallway), foyer ⇄ rec (stairs) |
| Stair traversal | Cheddar 0.5s · **Cocoa 1.05s** (Cheddar's real-life edge) |
| Door traversal | 0.35s |
| Dog couch | hold 4s → +5, then 6s cooldown |
| Squishmallows | 2 active per room; nap ~1.2s still → +1; respawn 6s |
| Rendering | only the player's current room (or transit target) is drawn |

## Sunbeam

| Constant | Value |
|---|---|
| Bask | +1 per 3.5s when a dog is alone in the beam |
| Standoff | both dogs in beam → nobody scores, "grrr/MY sunbeam" |
| Relocation | every ~9–13s the beam moves ("the sun moved…") |
| Scope | yard + house (room-picked); none in pool |

## Predators (backyard only)

| Constant | Value |
|---|---|
| First spawn | `predatorTimer = rnd(11,17)`s; respawn `rnd(13,20)`s; none if <9s left |
| Kind | 50/50 coyote / eagle |
| Targeting | the more-alone dog gets the red reticle |
| Coyote | enter → warn 1.4s → charge (speed ~5.6) → grab or whiff (2.4s) → drag (2.0s) |
| Eagle | circle → warn 1.6s → dive (speed ~11, homes on live target, 2.6s window) → carry (2.6s) |
| United-front defense | both dogs <86px apart + both free → bark-off scares predator within 150px |
| Dodge | target jumping (height>0.3), in zoomies, or immune |
| Rescue | sibling reaches the grabbed dog (coyote <70px; eagle gets underneath) |
| Carry-off | −1 score, dog dropped/returned |

## Ambient events (scheduler: one at a time)

| Event | Timer | Reward | Scope |
|---|---|---|---|
| Squirrel | `eventTimer = rnd(6,10)`s | +3 | yard (fence run) |
| Treat | same scheduler | +2 | yard + house (telegraphed) |
| Belly-rub | same scheduler | 3s wrestle immunity | yard + house |

## Audio (Web Audio synth, no files)

| Sound | Use |
|---|---|
| growl (wobbly sawtooth + LFO) | tug-of-war bed |
| bark (double square) | predator alarm, scare-off |
| yip (rising square) | win/grab/jump |
| screech (falling sawtooth) | eagle |
| splash (triangle drop) | water |

> Audio must initialize on first user gesture (browser autoplay policy). Wrap in `AudioBus` with a `resume()` on first tap; fail silently if `AudioContext` is unavailable (the headless harness stubs it).
