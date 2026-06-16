# Agent Instructions for Weeniegame

This repository is a personal couch co-op game starring Cheddar and Cocoa. Before implementing gameplay, read:

1. `docs/GAME-DESIGN-BIBLE.md` — creative north star, level ideas, mechanics, build priorities.
2. `docs/ARENA-PLAYABLE.md` — current Unity vertical slice and acceptance checks.
3. `docs/UNITY-FIRST-PLAYABLE.md` — Unity setup/runtime proof history.

## Non-negotiable project intent

Build a joyful, personal, replayable couch co-op game for Ken and Sue to play as Cheddar and Cocoa. The game should feel like exaggerated dog-life adventures, not a generic platformer with dog sprites.

## Design guardrails

- Preserve Cheddar/Cocoa identity and asymmetry.
- Prefer playable vertical slices over broad unplayable architecture.
- Every new mechanic should map to dog-authentic verbs: chase, rescue, steal, distract, defend, comfort, carry, tug, hide, sniff, bark.
- Bark must remain gameplay-relevant, not only cosmetic.
- New levels should create co-op communication, readable chaos, and funny failure/recovery.
- Do not discard the working Unity prototype, tests, or docs.

## Engineering guardrails

- Keep the Unity project compile-clean and out of Safe Mode.
- Preserve existing PlayMode tests and add deterministic coverage for new mission logic where feasible.
- Keep placeholder/generated assets acceptable until a deliberate art pipeline is introduced.
- Update docs when adding mechanics, levels, controls, or acceptance criteria.
- Use small commits with clear messages.

## Current recommended next move

Improve the current Backyard Mission vertical slice before starting many new levels:

1. dog identity/art/animation readability pass;
2. bark/tug/squirrel/predator feedback and game feel;
3. manual playtest with two players;
4. then build Kitchen Falling Food Frenzy as the next compact vertical slice.
