# Next Production Slice

> **Status: ACTIVE.** This document is aligned with the depth-first pivot and is not superseded.

## Current status

Kitchen Falling Food Frenzy is implemented and playtest-ready. Its behavior was extracted behind
`IMissionController` on 2026-06-21 with the full 333-test PlayMode suite green. The baseline
readability fix verification remains deferred—not passed—because the owner was unavailable for a
couch session and explicitly authorized architecture work to continue. Operation Pee Break remains
the active authored deep slice and uses the controller boundary. Its first controller-owned
implementation now includes the four exact-combo beats, split-role position stations, recoverable
Teenager misreads, bladder/phone pressure, united-bark door climax, replay reset, and deterministic
PlayMode coverage. Placeholder presentation still needs the second two-player couch acceptance pass.

As of 2026-06-20, `GameManager.cs` is nearly 8,000 lines and declares 21 mission variants. Treat
that as a date-stamped warning, not a permanent metric or a line-count target.

## Canonical work sequence

1. Run a baseline two-player couch playtest of the existing slices. Use two physical controllers
   and include Backyard Rescue, Blanket Catch, and Kitchen Falling Food Frenzy.
2. Address critical playtest findings before architecture or content work.
3. Define `IMissionController` and a narrow `MissionContext` using the ownership boundary in
   `ARCHITECTURE.md`.
4. Extract the existing Kitchen mission first. Preserve behavior and keep the full PlayMode suite
   green before proceeding.
5. Build Operation Pee Break entirely through the new controller structure described in
   `DEEP-SLICE-OPERATION-PEE-BREAK.md`.
6. Run a second two-player couch playtest. This is the deep-slice acceptance gate; automated tests
   cannot substitute for it.
7. Keep the mission roster frozen until that gate passes.

## Architecture guardrails

- `GameManager` owns orchestration, mission selection, session flow, and shared-service wiring.
- Controllers own mission-specific setup, state, ticking, input handling, cleanup, outcome, and
  snapshots.
- Mission definitions and controller registration live outside `GameManager`.
- Migration is one mission at a time and test-green after every extraction.
- Completion is defined by ownership and behavior, not an arbitrary line-count target.

Broad roadmaps, backlog items, progression work, and additional mission ideas are deferred until
the second couch-playtest gate passes.
