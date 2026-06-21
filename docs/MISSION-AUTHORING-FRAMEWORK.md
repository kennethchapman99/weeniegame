# Mission Authoring Framework

> **Status: ACTIVE ARCHITECTURE REFERENCE.** New mission authoring is frozen until the deep-slice
> couch-playtest gate passes. This document defines the structure used by the Kitchen extraction
> and Operation Pee Break.

## Authoring model

A mission consists of three external pieces:

1. A definition in a mission catalog.
2. An `IMissionController` implementation.
3. A controller factory/registration entry.

`GameManager` selects and orchestrates that pair but does not contain mission-specific state or
behavior branches.

## Mission definition

Definitions hold common descriptive/configuration data:

- stable id and display title;
- pack/location metadata if already required by the current flow;
- intro and objective copy;
- presentation timer and score-event labels;
- clear/fail/replay copy;
- shared actor/prop identifiers where genuinely data-driven.

Definitions must not become a giant set of optional fields that encode controller behavior.

## Controller ownership

The controller owns:

- setup, initial state, and owned world objects;
- per-frame ticking and mission time;
- input interpretation and Cheddar/Cocoa role locks;
- phase/state transitions and funny recovery;
- clear/fail outcome and reason;
- cleanup and replay reset;
- runtime snapshots and deterministic test hooks.

Use only a narrow `MissionContext`; never pass the complete `GameManager` as a service locator.

## Scene rule

Do not create a new scene for every mission. A separate scene is justified when camera/layout or
authored environment geometry is fundamentally different. Scene reuse does not justify putting
mission behavior back into `GameManager`.

## Co-op Puzzle Beat block

Every substantial mission spec must name:

- beat and fantasy;
- Cheddar role and Cocoa role;
- opening/action lock-key dependency;
- readable hints;
- funny, recoverable failure;
- visible world-state change;
- role reversal or escalation;
- deterministic test hooks.

## Testing requirement

Each controller needs deterministic coverage for start state, one success event, recoverable
failure, clear/fail paths, cleanup, replay reset, snapshot state, and session-flow integration.
Migration is one mission at a time; run the full PlayMode suite and restore green before continuing.

Kitchen is the first behavior-preserving extraction. Operation Pee Break is the first new deep
slice authored entirely through the controller structure. No other mission work begins before its
second human couch-playtest gate passes.
