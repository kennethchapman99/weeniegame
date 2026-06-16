# CLAUDE.md — Operating contract for Cheddar & Cocoa

This file governs how you (Claude Code) work in this repo. Read it fully before the first task.

## Required first reads

Before modifying gameplay, read these in order:

1. `docs/GAME-DESIGN-BIBLE.md` — creative source of truth, level ideas, mechanics, running gags, and build priorities.
2. `AGENTS.md` — coding-agent guardrails and current recommended next move.
3. `docs/ARENA-PLAYABLE.md` — current Unity Backyard Mission vertical slice and acceptance checks.
4. `docs/UNITY-FIRST-PLAYABLE.md` — Unity setup, test, and runtime proof history.
5. `prototype/cheddar-and-cocoa.prototype.html` and legacy docs when porting or preserving prototype behavior.

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
8. **Small playable vertical slices beat broad architecture.** Do not build a large framework before the current level is fun.

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

## Current recommended priority

The current Backyard Mission has the right gameplay structure: breakfast/weenie recovery, squirrel pressure, predator warning/rescue, rope/tug, united bark, scoring, stars, modifiers, and test coverage.

Next best work:

1. dog identity/art/animation readability pass;
2. bark/tug/squirrel/predator feedback and game feel;
3. manual two-player playtest/tuning;
4. then build Kitchen Falling Food Frenzy as the next compact level prototype.

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
