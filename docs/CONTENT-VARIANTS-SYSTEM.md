# Content Variants System

> **Status: DEFERRED.** Do not add content variants or mission breadth until Operation Pee Break
> passes its second couch-playtest gate.

Goal: make missions replayable without changing core rules every time.

## Variant Types

### Layout Variants

Examples:
- different squirrel route order;
- different fence gap order;
- different cover positions;
- different food drop patterns.

### Objective Variants

Examples:
- rescue toy before timer;
- avoid fake-outs;
- no dog gets grabbed;
- clear with three united barks.

### Comedy Variants

Examples:
- fake snack appears;
- squirrel carries absurd object;
- human sits in the worst possible spot;
- one toy suddenly becomes important.

### Modifier Variants

Examples:
- Zoomies Surge
- Squirrel Trouble
- Pancake Panic
- Wet Floors
- Extra Vacuum

## Rules

Variants should:
- be readable;
- be deterministic under tests;
- not break mission objectives;
- not require new art every time.

## Test Requirement

Each variant family should expose seedable behavior so PlayMode tests can confirm expected state.
