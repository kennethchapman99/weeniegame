# Co-op Vision — Cheddar & Cocoa, the couch game

> Status: north-star direction (owner decision, 2026-06-15). Turns the M0–M8 arcade game
> into a **local two-player cooperative** game the owner and his wife play on the couch,
> starring Cheddar & Cocoa on missions around their house, yard, car, and beyond.
> The M0–M8 contract in `CLAUDE.md` still governs *how* we build; this doc governs *what
> we're building toward*. The prototype remains the spec for ported mechanics; new co-op
> mechanics are speced here and in their milestone before they're built.

## The pitch

Two long-haired mini dachshunds, two controllers, one couch. Inspired by *It Takes Two* /
*Split Fiction*: **cooperative missions** where the two dogs must work *together* — neither
dog can finish a mission alone. Settings are the real places the dogs go: the house, the
backyard, the pool, the car, and trips beyond.

## Platform target (committed)

**TV + iOS, on the existing TypeScript / Canvas 2D stack.** No engine port, no console
dev-program gate. Reality check, kept here so it stays honest:

| Platform | Reachable? | How |
|---|---|---|
| **PC / Mac → TV** | ✅ now | Tauri (preferred) or Electron desktop build; two USB/Bluetooth gamepads. This is the primary couch experience. |
| **iOS / iPadOS** | ✅ now | Capacitor wrapper; install via Xcode on owner's devices (free account) or TestFlight ($99/yr). |
| **Apple TV (tvOS)** | ✅ later | Same Capacitor/web core; controllers pair over Bluetooth. Optional stretch. |
| **PlayStation 5 (retail)** | ❌ not on this stack | Requires Sony PlayStation Partners approval + dev kit + a console-capable engine. Out of scope unless we ever choose an engine port. |
| **Nintendo Switch (retail)** | ❌ not on this stack | Requires Nintendo dev-program approval + their SDK + dev hardware. Out of scope, same reason. |

If native PS5/Switch ever becomes a must-have, that is a **separate track**: port the
deterministic game *logic* (which is engine-agnostic by design) into Godot or Unity and
enter the platform dev programs. We are explicitly **not** doing that now.

## Why the current codebase is ready for this

The M0–M8 architecture did the expensive groundwork:

- **Symmetric dog entities** — player 2 simply drives the second `Dog` instead of `aiThink`.
- **Pure intent input** — `core/input.ts` already returns a world-space `Intent` from a
  source; a Gamepad source is a new input adapter, not a rewrite.
- **Logical world is already landscape** — 960×600 (1.6:1), aspect-correct letterbox in
  `core/camera.ts`. TV/16:9 is free; only the touch HUD buttons need a controller mapping.
- **Deterministic, logic/render-separated core** — co-op objectives and "needs both dogs"
  interactions are testable in the headless sim exactly like every existing system.

The expensive part remaining is **design and content** (the missions), not plumbing.

## Co-op design pillars

1. **Two dogs, one goal.** Co-op rounds share a single objective and a combined score.
   The competitive rounds (M2–M8) stay available as a "versus / chaos" mode, but the
   headline mode is cooperative.
2. **Interdependence — neither dog alone.** Every mission has at least one beat that
   *requires both dogs*: one boosts the other onto a ledge, one distracts a predator while
   the other sneaks the objective, a door/gate that needs two dogs holding two spots, etc.
   This is the *It Takes Two* DNA and the reason it's couch-only, not solo.
3. **Asymmetric flavor.** Lean into the dogs' real differences (already partly modeled:
   wrestle odds, stair speed). Cheddar = chaos/agility (chair-leaps, faster, but barfs);
   Cocoa = steady/safe (the veteran). The Kitchen level in `docs/LEVEL-IDEAS.md` already
   pitches this — promote it as an early co-op mission.
4. **Comedy first.** The "hilarious bark-off" energy (M7) is the tone for everything. Big,
   silly, readable animations over realism.
5. **Two humans by default; AI as a fallback.** P2 is a person on a second controller. The
   existing `ai/sibling.ts` brain becomes an optional partner for when the owner plays solo.

## Mission framework (the new core system)

Co-op needs a structure the competitive rounds didn't:

- **Objective(s):** a mission defines one or more goals (reach X with both dogs, collect N
  together, survive a wave, escort an item). Tracked in state, asserted in the sim.
- **Gates:** "needs both dogs" primitives — `bothOnSpots`, `boostJump` (one dog launches the
  other), `distract+grab`, paired pressure pads. These are reusable systems, not per-mission code.
- **Win / fail / retry:** missions end in success or failure (not just a timer). A failed
  mission is replayable from a checkpoint — couch co-op forgives.
- **Combined scoring / stars:** shared score plus an optional 1–3 star rating for replay pull.

This framework is data-driven, layered on the existing `SceneDef` + room-schema seams so a
new mission is mostly data + a painter + any net-new gate system.

## What stays, what changes

| Area | Today (M0–M8) | Co-op |
|---|---|---|
| Players | 1 human vs. AI | 2 humans (AI = solo fallback) |
| Input | Touch-drag + WASD | + **Gamepad per player**; touch stays for iOS solo |
| Display | Portrait-friendly, landscape-capable | Landscape/TV first; touch HUD → controller buttons |
| Scoring | Per-dog competition | Shared objective + combined score (versus mode kept) |
| Round end | Timer | Objective success/fail + retry |
| Distribution | Web / `file://` artifact | + Tauri (desktop/TV) + Capacitor (iOS) wrappers, still zero-network |

The hard rules in `CLAUDE.md` still hold — **zero runtime network requests** especially:
the desktop/iOS wrappers must bundle everything, same as the web artifact.

## Roadmap shape

- **Phase 1 — Couch-playable (M9–M11):** gamepad input, true local 2P, desktop + iOS
  wrappers. End state: two controllers on the couch, today's mechanics, on the TV and on
  the iPad. *This is the "we can actually play it" milestone.*
- **Phase 2 — Co-op design turn (M12+):** mission framework, the "needs both dogs" gate
  primitives, combined scoring, then missions as content (Kitchen, Car, House rescue, …).
- **Phase 3 — only if ever wanted:** native consoles via an engine port + dev programs.

See `docs/BUILD-PLAN.md` (M9 onward) for the staged, shippable, tested breakdown.
