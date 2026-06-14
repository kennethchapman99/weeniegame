# Cheddar & Cocoa 🐶🐶

A 2D arcade game starring two long-haired mini dachshunds — **Cheddar** (golden, 1.5yr, chaos puppy) and **Cocoa** (chocolate, 4yr, reigning spot queen) — competing across the backyard, pool, and a three-room model of their actual house. They fight over toys, cuddle spots, sunbeams, and the sacred dog couch; wrestle and flip each other; play tug-of-war; and have to **work together** to survive coyotes and eagles in the backyard.

This repository contains a **working single-file prototype** (`prototype/cheddar-and-cocoa.prototype.html`) and a **multi-stage build plan** to turn it into a proper, maintainable project.

## Why this exists

The prototype was built iteratively in a chat session and grew to ~2,300 lines of vanilla JS in one HTML file. Every mechanic works and is validated, but the single-file structure has hit its ceiling: no module boundaries, no tests in the repo, hand-rolled state, and rendering/logic interleaved. This package is the bridge from "impressive prototype" to "buildable codebase."

## Start here

1. Read `CLAUDE.md` — the operating contract for Claude Code on this repo.
2. Read `docs/ARCHITECTURE.md` — target structure and the systems that already exist in the prototype.
3. Read `docs/BUILD-PLAN.md` — the staged milestones (M0–M8), each independently shippable.
4. Open `prototype/cheddar-and-cocoa.prototype.html` in a browser to see the target behavior. **It is the spec.** When in doubt about how something should feel, run the prototype.

## Quick facts

- **Engine:** none yet. Prototype is vanilla Canvas 2D + Web Audio. The plan keeps it dependency-light (Vite + TypeScript, no game framework) unless a milestone justifies otherwise.
- **Target platforms:** mobile web first (portrait, touch-drag), desktop second (WASD/arrows + buttons). Renders in constrained in-app webviews, so **zero external network requests at runtime** is a hard rule (fonts, audio, everything inlined or bundled).
- **Rounds:** Backyard (45s) → Pool (45s) → House (75s). Most-points-across-rounds wins.
- **Players:** 1P now (you vs. an AI sibling). 2P co-op/versus is a planned milestone; the dog entities are already symmetric to make it cheap.
