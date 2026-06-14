# CLAUDE.md — Operating contract for Cheddar & Cocoa

This file governs how you (Claude Code) work in this repo. Read it fully before the first task.

## Prime directive

**The prototype is the spec.** `prototype/cheddar-and-cocoa.prototype.html` is a working, validated reference implementation of every mechanic. Your job is to re-implement it as a clean, staged, tested TypeScript project **without regressing behavior or feel**. When a question about intended behavior arises, open the prototype and observe — do not invent.

## Hard rules (non-negotiable)

1. **Zero runtime network requests.** The game must run from `file://` and inside constrained in-app webviews. No CDN fonts, no remote audio, no analytics. Bundle/inline everything. The prototype already proved this; don't reintroduce a `<link href="https://fonts...">`.
2. **Mobile-first.** Portrait orientation, touch-drag movement is the primary input. The logical world is **960×600**, letterboxed to the screen and scaled by `devicePixelRatio` (capped at 3) for crisp rendering. Preserve this.
3. **Deterministic core.** Game logic must be driven by a fixed timestep and a seedable RNG so the headless test harness can reproduce runs. No `Math.random()` calls scattered through logic — route through an injectable `rng`.
4. **Logic/render separation.** Update (simulation) and render (drawing) must be separate. No score mutations or state transitions inside render functions. The prototype violates this in places; fixing it is part of the work.
5. **Every milestone is shippable and tested** before moving on. See `docs/BUILD-PLAN.md`. Do not start milestone N+1 with milestone N's tests red.
6. **Don't delete the prototype.** It stays as the living spec until M8 ships and is signed off.

## Working style

- **Plan before editing.** For any non-trivial task, write a short plan (files touched, functions added, how you'll test) and confirm scope before large changes.
- **Small, reviewable commits**, one concern each. Conventional commit messages (`feat:`, `fix:`, `refactor:`, `test:`, `chore:`).
- **Tests live in the repo.** The prototype was validated by an external headless harness; port that approach into `tests/` (see ARCHITECTURE). Each system gets at least one behavioral test that drives `update()` and asserts on state.
- **Run the full check before declaring a milestone done:** typecheck, lint, unit tests, and the headless full-game sim. Wire these into one `npm run verify` script early (M0).
- **Preserve tuning constants.** Numbers like round times, wrestle reversal odds, sunbeam bask rate, predator dodge windows, and AI speed (0.88×) were balanced by playtesting + simulation. Extract them into a single `config/balance.ts` rather than re-deriving them. The values are catalogued in `docs/MECHANICS.md`.

## Owner preferences (from the project owner)

- Full code for changed files when reviewing in chat; confident brevity in prose.
- Flag risks and tradeoffs directly — no yes-man. If a milestone's approach has a downside, say so up front.
- Comparisons as tables.

## What "done" looks like per task

- Code typechecks and lints clean.
- New/changed behavior has a test that would fail without the change.
- `npm run verify` is green.
- The behavior matches the prototype (spot-check in browser for anything visual/feel-related).
- Tuning constants centralized, not inlined.

## Things that will bite you (learned from the prototype build)

- **Brace/scope drift in large edits.** The prototype broke once from a dropped `if(){` wrapper during an inline edit. Modular files + typecheck on every save prevents this class of bug — lean on it.
- **Coordinate spaces.** World units (960×600) vs. device pixels vs. CSS pixels. Pointer events must be converted world-space before use. Get the transform helpers right once in the renderer/input layer.
- **Per-scene state resets.** Several bugs came from state leaking between rounds (zoomies, tug, predator, wet timers). A scene should fully (re)initialize its entity state on entry. Make this a single explicit function, not scattered resets.
- **AI water/predator routing.** The pool AI uses corner-waypoint routing to avoid swimming; the predator AI uses huddle/dodge logic. These are subtle — port them faithfully and keep the sims that validate them.
