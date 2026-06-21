# Agent Instructions for Weeniegame

> 🛑 **THE UNITY PROJECT (`unity/CheddarAndCocoa/`) IS THE ONLY ACTIVE CODEBASE.** The TypeScript/
> Canvas build (`src/`, `tests/`) and the `prototype/` are **FROZEN reference material** — read them
> to understand how a mechanic behaves and how it's tuned, then **build it in Unity**. Do **not** add
> to or edit `src/` or `tests/`; that work is wasted and will be reverted. All new levels, mechanics,
> fixes, and tests belong in the Unity project.

This repository is a personal couch co-op game starring Cheddar and Cocoa. Start with
`docs/README.md` for document status. Before implementing gameplay, read:

1. `docs/NEXT-PRODUCTION-SLICE.md` — current depth-first sequence and gate.
2. `docs/GAME-DESIGN-BIBLE.md` — creative north star and level idea bank.
3. `docs/DEEP-SLICE-OPERATION-PEE-BREAK.md` — the active deep-slice specification.
4. `docs/ARCHITECTURE.md` — active Unity mission-controller boundary.
5. `docs/COOP-PUZZLE-DESIGN.md` — co-op puzzle doctrine.
6. `docs/ARENA-PLAYABLE.md` — current Unity slices and manual acceptance checks.

## Non-negotiable project intent

Build a joyful, personal, replayable couch co-op game for Ken and Sue to play as Cheddar and Cocoa. The game should feel like exaggerated dog-life adventures, not a generic platformer with dog sprites.

## Design guardrails

- Preserve Cheddar/Cocoa identity and asymmetry.
- Prefer playable vertical slices over broad unplayable architecture.
- Every new mechanic should map to dog-authentic verbs: chase, rescue, steal, distract, defend, comfort, carry, tug, hide, sniff, bark.
- Bark must remain gameplay-relevant, not only cosmetic.
- New levels should create co-op communication, readable chaos, and funny failure/recovery.
- Every substantial mission should include at least one authored co-op puzzle beat where one dog creates the opening and the other turns it into progress.
- Do not discard the working Unity prototype, tests, or docs.

## Engineering guardrails

- Keep the Unity project compile-clean and out of Safe Mode.
- Preserve existing PlayMode tests and add deterministic coverage for new mission logic where feasible.
- Do not add mission-specific state, ticking, input branches, setup, cleanup, outcome logic, or
  snapshots directly to `GameManager`; use the mission-controller boundary described in
  `docs/ARCHITECTURE.md`.
- Keep placeholder/generated assets acceptable until a deliberate art pipeline is introduced.
- Update docs when adding mechanics, levels, controls, or acceptance criteria.
- Use small commits with clear messages.

## Canonical work sequence

Do not skip or reorder these gates:

1. Run a baseline two-player couch playtest of the existing slices.
2. Address critical playtest findings.
3. Define `IMissionController` and a narrow `MissionContext`.
4. Extract the existing Kitchen mission first, keeping all PlayMode tests green.
5. Build Operation Pee Break entirely through the new controller structure.
6. Run a second couch playtest as the deep-slice acceptance gate.
7. Keep the mission roster frozen until that gate passes.

The controller migration is incremental and must remain test-green after every extraction. Its goal
is ownership clarity, not an arbitrary `GameManager` line count. Broad roadmaps and mission ideas are
deferred inventory until the second couch-playtest gate passes.
