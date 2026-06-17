# Cheddar & Cocoa 🐶🐶

A 2D arcade game starring two long-haired mini dachshunds — **Cheddar** (golden, chaos puppy) and **Cocoa** (chocolate, reigning spot queen) — competing and cooperating across the backyard, pool, and a three-room model of their actual house. They fight over toys, cuddle spots, sunbeams, and the sacred dog couch; wrestle and flip each other; play tug-of-war; chase food and squirrels; and have to **work together** to survive coyotes, eagles, humans, cleaning day, vet betrayal, and other dog-life emergencies.

This repository contains a **working single-file prototype** (`prototype/cheddar-and-cocoa.prototype.html`), a **frozen, reference-only TypeScript/Canvas port** (`src/`, M0–M14 — see the freeze notice below), and the **active Unity build** (`unity/CheddarAndCocoa/`), which is where all current work happens.

> 🛑 **THE TYPESCRIPT / CANVAS BUILD (`src/`, `tests/`) IS FROZEN — DO NOT ADD OR EDIT IT.**
> The product is built **only in Unity** (`unity/CheddarAndCocoa/`). The TS/Canvas build and the
> `prototype/` are kept **read-only**, purely as a behavior/balance **reference** for porting —
> not as a thing to extend. **All new gameplay, levels, mechanics, and tests go in the Unity
> project.** Any work added to `src/` or `tests/` is wasted effort and will be reverted. If you are
> an agent: the answer to "where do I build this?" is always Unity. See
> **`docs/UNITY-PIVOT-PLAN.md`** and **`docs/UNITY-FIRST-PLAYABLE.md`**.

> 🎮 **Creative source of truth:** `docs/GAME-DESIGN-BIBLE.md` captures the current game vision,
> mechanic library, level idea bank, running gags, and near-term build priorities. Claude/Codex
> agents should read it before proposing or implementing gameplay.

## Why this exists

The prototype was built iteratively in a chat session and grew to ~2,300 lines of vanilla JS in one HTML file. Every mechanic works and is validated, but the single-file structure has hit its ceiling: no module boundaries, no tests in the repo, hand-rolled state, and rendering/logic interleaved. This package is the bridge from "impressive prototype" to "buildable codebase."

The Unity rebuild is now the active playable direction. The goal is not a generic co-op platformer with dog sprites; it is a personal, funny, replayable couch co-op game where Ken and Sue play as Cheddar and Cocoa through exaggerated dog-life adventures.

## Start here

1. Read `docs/GAME-DESIGN-BIBLE.md` — the creative north star, mechanics library, level idea bank, running gags, and recommended next build priorities.
2. Read `AGENTS.md` and `CLAUDE.md` — coding-agent guardrails for Claude/Codex and future assistant sessions.
3. Read **`docs/UNITY-PIVOT-PLAN.md`** — the current direction: why Unity, what's preserved, platforms, packages, systems, vertical slice, phases, risks, non-goals.
4. Read `docs/ARENA-PLAYABLE.md` — the current Backyard Mission vertical slice: objective, controls, squirrel pressure, predator rescue, rope/tug, scoring, modifiers, and tests.
5. Read `docs/BUILD-PLAN.md` — the TS milestones (M0–M14, the spec) **plus** the Unity rebuild roadmap at the bottom.
6. Read `docs/MECHANICS.md` — the **balance bible** (every tuning constant + rationale). The Unity build ports these; it does not re-derive them.
7. Open `prototype/cheddar-and-cocoa.prototype.html` in a browser to see the original target behavior. When in doubt about how an older mechanic should feel, run the prototype.
8. To work in the rebuild: `unity/CheddarAndCocoa/README.md` (how to open in Unity).

## Quick facts

- **Engine:** **Unity 6 LTS (C#)** going forward — local couch co-op, URP 2D, Unity Input System, two controllers. The original build is dependency-light vanilla Canvas 2D + Web Audio (Vite + TypeScript), kept as the spec/oracle.
- **Target platforms (Unity):** macOS/Windows desktop → big TV via HDMI with two controllers first; tvOS / Apple TV later; iOS optional. No public store/distribution assumed.
- **Current playable slice:** Backyard Mission — two dogs, two controllers, move/bark/interact, breakfast/weenie recovery, squirrel pressure, predator warning/attack/rescue, rope/tug objective, scoring, stars, modifiers, result/restart flow.
- **Next likely work:** dog identity/art/animation and game-feel pass on the current vertical slice before adding the next compact level, likely Kitchen Falling Food Frenzy.
- **Players:** local **two-player couch co-op** is the headline (two humans, two controllers, one shared-camera TV). The TS build's AI sibling becomes a post-slice solo fallback.
- **Hard rule carried over:** the original webview build is **zero runtime network requests**; that constraint no longer binds the native Unity build, but the *no-dependencies-on-the-cloud* spirit stays.
