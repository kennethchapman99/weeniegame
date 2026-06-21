# GameManager Wiring Guide

> **Status: ACTIVE MIGRATION GUIDE.** Do not add mission-specific fields, switches, update branches,
> input branches, helpers, outcomes, or snapshot construction directly to `GameManager`.

## Ownership boundary

`GameManager` wires shared services, selects a mission definition/controller pair, starts the
controller, forwards lifecycle events, and manages session flow. It may wrap a controller snapshot
with session-level information but does not inspect mission-specific state to build one.

Controllers receive a narrow `MissionContext` and own their complete mission lifecycle.
Definitions and registration live outside `GameManager`.

## Shared service wiring

The context may expose narrow interfaces for:

- dog state/input access;
- scoring via the existing single mutation path;
- HUD, objective, cue, event-log, and world-pop presentation;
- audio and rumble requests;
- deterministic seed/random access;
- arena actor creation/cleanup;
- shared session-safe clear/fail callbacks.

Add only dependencies required by the extracted controller. Do not mirror every `GameManager`
method or field into `MissionContext`.

## Existing production helpers

- Rank calculation and session-best bookkeeping remain shared orchestration concerns unless a
  controller-specific rule proves otherwise.
- Controllers emit score events using `ScoreEventCatalog` rather than raw duplicated labels.
- Controllers produce `MissionRuntimeSnapshot` data from their own state.
- Challenge evaluation consumes controller snapshots at the boundary; it does not read private
  controller fields from `GameManager`.
- Stable mission seeds are supplied through context and preserved on replay.

## Kitchen-first extraction

1. Preserve the existing Kitchen definition, behavior, public test hooks, and seed/replay semantics.
2. Move Kitchen setup, state, tick/input logic, cleanup, outcome, and snapshot into its controller.
3. Register its definition and controller outside `GameManager`.
4. Route existing selection/session flow to the controller without changing other missions.
5. Run `./unity/run-playmode-tests.sh`; do not begin Pee Break until the full suite is green.

## Acceptance

- Kitchen plays identically through the controller.
- No new Kitchen-specific `GameManager` branch or state is introduced.
- Replay and cleanup reset all controller-owned state.
- Existing score, HUD, audio/rumble, session, and snapshot behavior remains stable.
- Full PlayMode suite passes after the extraction and after every later mission migration.
- Architecture completion is based on correct ownership, not a line-count target.
