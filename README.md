# Cheddar & Cocoa 🐶🐶

A 2D arcade game starring two long-haired mini dachshunds — **Cheddar** (golden, 1.5yr, chaos puppy) and **Cocoa** (chocolate, 4yr, reigning spot queen) — competing across the backyard, pool, and a three-room model of their actual house. They fight over toys, cuddle spots, sunbeams, and the sacred dog couch; wrestle and flip each other; play tug-of-war; and have to **work together** to survive coyotes and eagles in the backyard.

This repository contains a **working single-file prototype** (`prototype/cheddar-and-cocoa.prototype.html`), a **staged TypeScript/Canvas port** (`src/`, M0–M14), and — as of 2026-06-15 — a **Unity rebuild** (`unity/CheddarAndCocoa/`).

> 🔱 **Direction shift (2026-06-15): the product is being rebuilt in Unity** (C#, local couch
> co-op for desktop→TV, later Apple TV). The TypeScript/Canvas build (`src/`), the `prototype/`,
> and `docs/` are **preserved as the design / balance / behavior spec and a runnable test
> oracle** — not deleted, not expanded as the long-term product. See
> **`docs/UNITY-PIVOT-PLAN.md`** for the why, the platform plan, and the migration phases.

## Why this exists

The prototype was built iteratively in a chat session and grew to ~2,300 lines of vanilla JS in one HTML file. Every mechanic works and is validated, but the single-file structure has hit its ceiling: no module boundaries, no tests in the repo, hand-rolled state, and rendering/logic interleaved. This package is the bridge from "impressive prototype" to "buildable codebase."

## Start here

1. Read `CLAUDE.md` — the operating contract for Claude Code on this repo.
2. Read **`docs/UNITY-PIVOT-PLAN.md`** — the current direction: why Unity, what's preserved, platforms, packages, systems, vertical slice, phases, risks, non-goals.
3. Read `docs/BUILD-PLAN.md` — the TS milestones (M0–M14, the spec) **plus** the Unity rebuild roadmap at the bottom.
4. Read `docs/MECHANICS.md` — the **balance bible** (every tuning constant + rationale). The Unity build ports these; it does not re-derive them.
5. Open `prototype/cheddar-and-cocoa.prototype.html` in a browser to see the target behavior. **It is the spec.** When in doubt about how something should feel, run the prototype.
6. To work in the rebuild: `unity/CheddarAndCocoa/README.md` (how to open in Unity).

## Quick facts

- **Engine:** **Unity 6 LTS (C#)** going forward — local couch co-op, URP 2D, Unity Input System, two controllers. The original build is dependency-light vanilla Canvas 2D + Web Audio (Vite + TypeScript), kept as the spec/oracle.
- **Target platforms (Unity):** macOS/Windows desktop → big TV via HDMI with two controllers first; tvOS / Apple TV later; iOS optional. No public store/distribution assumed.
- **Levels:** vertical slice = the Backyard (two dogs, two controllers, move/bark/grab/tug, squirrel chase, one predator co-op moment, result screen). Then pool, house, and the co-op mission campaign port over.
- **Players:** local **two-player couch co-op** is the headline (two humans, two controllers, one shared-camera TV). The TS build's AI sibling becomes a post-slice solo fallback.
- **Hard rule carried over:** the original webview build is **zero runtime network requests**; that constraint no longer binds the native Unity build, but the *no-dependencies-on-the-cloud* spirit stays.
