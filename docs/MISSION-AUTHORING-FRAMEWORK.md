# Mission Authoring Framework

Goal: missions should become data-driven over time.

## Current State

ArenaScene mission variants are currently code-driven through `GameManager.MissionDefinition`.

That is acceptable for early vertical slices.

## Target State

A mission should be describable by:

- id
- title
- pack
- intro copy
- objective copy
- clear condition
- fail condition
- enabled mechanic modules
- score events
- actor slots
- prop slots
- test hooks

## Authoring Rule

Do not create a new scene for every small mission.

Create new scenes only when:
- camera layout is fundamentally different;
- environment is fundamentally different;
- mission needs authored geometry that cannot be generated.

## Mission Definition Fields

Required:
- Mission id
- Display name
- Mission pack
- Timer
- Primary objective
- Secondary challenge
- Clear banner
- Fail banner
- Replay prompt
- Score event labels

Optional:
- Hazard actor
- NPC actor
- hidden collectible
- co-op object
- minigame handoff

## Testing Requirement

Every mission definition needs tests for:

- start state
- objective text
- one success event
- one fail event
- clear path
- replay reset
- session summary state
