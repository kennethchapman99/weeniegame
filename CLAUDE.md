# CLAUDE.md — Operating contract for Cheddar & Cocoa

> 🛑 **STOP — READ THIS FIRST. The ONLY active codebase is the Unity project
> (`unity/CheddarAndCocoa/`).** The TypeScript/Canvas build (`src/`, `tests/`) and the
> `prototype/` are **FROZEN, read-only reference material** for porting behavior and balance —
> they are NOT the product and must NOT be extended. **Do not add, edit, or "improve" anything in
> `src/` or `tests/`.** Every new level, mechanic, fix, and test belongs in the Unity project. If a
> task seems to call for TS/Canvas work, do it in Unity instead. Treat any request to "make
> progress" or "build a level" as a request to build it **in Unity**.

This file governs how you (Claude Code) work in this repo. Read it fully before the first task.

## Required first reads

Before modifying gameplay, read these in order:

1. `docs/README.md` — status index separating active instructions from reference/history.
2. `docs/NEXT-PRODUCTION-SLICE.md` — current depth-first sequence and acceptance gate.
3. `AGENTS.md` — coding-agent guardrails.
4. `docs/GAME-DESIGN-BIBLE.md` — creative source of truth and level idea bank.
5. `docs/DEEP-SLICE-OPERATION-PEE-BREAK.md` — active deep-slice specification.
6. `docs/ARCHITECTURE.md` — active Unity mission-controller boundary.
7. `docs/ARENA-PLAYABLE.md` — current Unity slices and manual acceptance checks.
8. The prototype and TypeScript build **as read-only reference only** when porting older behavior.

## Prime directive

**The game fantasy is the spec.** Build a joyful, personal, replayable couch co-op game for Ken and Sue where they play as Cheddar and Cocoa through exaggerated dog-life adventures.

The prototype remains important as a validated reference for original mechanics and feel. The current Unity work is now the active playable direction. Do not drift into generic arena mechanics; use dog-life fantasies and inside jokes as mechanics.

## Hard rules (non-negotiable)

1. **Preserve the working Unity project and tests.** Keep the Unity project compile-clean and out of Safe Mode. Do not regress PlayMode tests.
2. **Every new gameplay slice should be shippable and tested.** Add deterministic PlayMode coverage for mission logic where feasible.
3. **Prototype remains preserved.** Do not delete or casually rewrite `prototype/cheddar-and-cocoa.prototype.html`; it remains a living reference for mechanics/feel until explicitly retired.
4. **Cheddar and Cocoa must feel distinct.** Cheddar is chaos puppy energy; Cocoa is veteran/queen/territory-control energy. This should show up in mechanics, animation, tuning, and comedy.
5. **Co-op first.** New ideas should force communication, rescue, role split, synchronized timing, or shared-object interaction.
6. **Bark stays gameplay-relevant.** It should affect squirrel, predator, rescue, human distraction, rhythm, panic/calm, or puzzle state — not merely play a cosmetic effect.
7. **Readable chaos.** Add clear labels, pings, animation, camera/audio cues, HUD copy, and manual acceptance notes until final art can carry clarity.
8. **Small playable vertical slices beat broad architecture.** Implement only the narrow controller
   boundary needed to extract Kitchen and build the accepted deep slice.
9. **Do not grow the god class.** Mission-specific setup, state, ticking, input, cleanup, outcomes,
   and snapshots belong to `IMissionController` implementations, not new `GameManager` branches.

## Owner preferences

- Full code for changed files when reviewing in chat; confident brevity in prose.
- Flag risks and tradeoffs directly — no yes-man.
- Comparisons as tables.
- Preserve docs and explain what changed.

## Build philosophy

Every meaningful mechanic should answer:

- What dog fantasy is this delivering?
- What are Cheddar and Cocoa each doing differently?
- What makes players communicate?
- What can go wrong in a funny way?
- What is the simplest tested version?

Core dog verbs to prefer: chase, rescue, steal, distract, defend, comfort, carry, tug, hide, sniff, bark.

## Canonical work sequence

Do not skip or reorder these gates:

1. Run a baseline two-player couch playtest of the existing slices.
2. Address critical playtest findings.
3. Define `IMissionController` and a narrow `MissionContext`.
4. Extract the existing Kitchen mission first, keeping all PlayMode tests green.
5. Build Operation Pee Break entirely through the new controller structure.
6. Run a second couch playtest as the deep-slice acceptance gate.
7. Keep the mission roster frozen until that gate passes.

The mission-controller migration is incremental and test-green after every extracted mission.
`GameManager` ultimately owns orchestration, mission selection, session flow, and shared-service
wiring. Do not use a target line count as the definition of done.

## What "done" looks like per task

- Code compiles/imports cleanly in Unity.
- Existing PlayMode tests stay green.
- New/changed behavior has a test that would fail without the change where feasible.
- Manual acceptance checklist is updated for visual/feel changes.
- Behavior aligns with `docs/GAME-DESIGN-BIBLE.md` and preserves Cheddar/Cocoa identity.
- Docs are updated when adding mechanics, levels, controls, or acceptance criteria.

## Things that will bite you

- **Brace/scope drift in large edits.** Use small changes and run checks often.
- **Scene state resets.** Zoomies, tug, predator, wet timers, and score state should reset explicitly on restart/scene entry.
- **Placeholder readability.** Generated shapes are acceptable, but players must instantly know what is food, squirrel, predator, rope, Cheddar, Cocoa, danger, and objective.
- **Overbuilding.** This project needs funny playable proof more than systems architecture.
- **Generic co-op drift.** If an idea could star any two characters, rewrite it until it feels dog-specific.
