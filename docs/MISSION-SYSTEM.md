# Mission System

> **Status: ACTIVE.** The mission roster is frozen until Operation Pee Break passes its second
> two-player couch playtest.

## Runtime boundary

As of 2026-06-20, the Unity Arena exposes 21 mission variants through a large `GameManager`.
Migration is incremental; existing missions remain playable while ownership moves behind
controllers.

- `GameManager` owns orchestration, mission selection, session flow, and shared-service wiring.
- `IMissionController` implementations own mission-specific setup, state, ticking, input handling,
  cleanup, outcome, deterministic hooks, and snapshots.
- `MissionContext` exposes only the shared services controllers require.
- Mission definitions and controller registration live outside `GameManager`.

Do not add a new mission enum branch, `Tick*` branch, mission field cluster, definition builder
branch, input branch, or snapshot switch directly to `GameManager`.

## Active migration

1. Define the narrow controller/context contract after baseline playtest fixes.
2. Extract the existing Kitchen mission first without behavior changes.
3. Keep the full PlayMode suite green.
4. Build Operation Pee Break entirely as a registered controller.
5. Keep the roster frozen through the second couch-playtest gate.

See `ARCHITECTURE.md` for ownership details and `NEXT-PRODUCTION-SLICE.md` for the full sequence.

## Mission definition versus controller

A mission definition contains stable descriptive/common data such as id, title, intro/objective
copy, timer presentation, score labels, and shared feature configuration. It does not implement
mission behavior.

A controller owns the behavior that makes the mission specific: roles, phases, setup, actors,
rules, state transitions, funny failures, outcome, reset, and snapshot.

Both the catalog of definitions and the controller factory registry are separate from
`GameManager`. Selection resolves a definition/controller pair and passes a narrow context to the
controller.

## Co-op puzzle beat requirement

Every substantial mission needs at least one named beat where:

- one dog creates an opening through a dog-authentic action;
- the other dog converts that opening into progress with a different action;
- the world changes visibly after success;
- failure is funny, readable, and recoverable;
- deterministic controller hooks can verify the dependency where feasible.

Parallel collection, shared survival, or both dogs standing in one circle is not sufficient by
itself. Follow `COOP-PUZZLE-DESIGN.md`.

## Controller acceptance

Every extracted or new controller must demonstrate:

- start state and objective copy;
- both dogs' useful roles and role-lock dependency;
- success, recoverable failure, clear/fail, and replay reset;
- cleanup with no leaked actors or state;
- controller-produced runtime snapshot;
- unchanged session replay/next/select/summary flow;
- full PlayMode suite green before the next migration step.

No arbitrary `GameManager` line count is part of this acceptance.
