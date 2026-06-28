# Gameplay-First Greybox Pivot

> **Status: ACTIVE EXECUTION DECISION.** Use generated, readable Unity primitives to prove level
> design, co-op roles, and mission fun before spending more time on realistic backgrounds or
> photo-derived scenes.

## Decision

The next playable work should optimize for couch-playtest clarity and fun:

- clear level shapes;
- obvious dog-specific jobs;
- wired mechanics;
- readable failure and recovery;
- fast iteration through generated Unity objects.

Realistic backyard/photo replacement is deferred until a generated version of the beat is fun with
two players. Photo references remain useful for later art direction, but they should not block
mission layout, role tuning, or PlayMode coverage.

## Unity artifact

Use the editor menu:

```text
Cheddar & Cocoa > Gameplay First > Build Generated Playtest Lab
```

It creates:

```text
Assets/Scenes/GameplayFirstPlaytestLab.unity
Assets/Generated/GameplayFirst/Materials/
```

The generated lab contains:

- a primitive readable-arena scale blockout;
- an Operation Pee Break room blockout with role pads for all four beats;
- playable-component set pieces for current levels, including Kitchen falling-food lanes, Sock Panic
  tip/dive roles, Eagle Shadow cover/sweep, and Coyotes pin/fill roles;
- Pee Break pressure props for the attention cone, charger cord, hallway choke, bladder/phone
  meters, misread reset spot, and door-sunbeam payoff preview;
- generic co-op beat lanes for hold/release, distract/sneak, smell/act, and rescue-as-puzzle;
- a couch-playtest acceptance wall with the pass criteria that matter before art replacement.

This scene is an authoring artifact. It does not add a mission, does not change `ArenaScene`, and
does not authorize roster expansion before the Operation Pee Break couch gate.

## Working rule

For every gameplay beat, build the shortest generated version first:

1. Put the current objective in the world with a large label or pad.
2. Give Cheddar and Cocoa distinct jobs.
3. Make one dog create an opening and the other dog turn it into progress.
4. Add a funny, recoverable failure.
5. Add a visible world-state change on success.
6. Add deterministic PlayMode coverage for the state machine.
7. Only then replace generated objects with better art.

Generated assets are not embarrassing placeholders; they are the production tool for finding the fun.

## Current priority

Operation Pee Break remains the active deep slice. The next pass should use generated objects to
answer couch-playtest questions:

- Does each player know where to go in each beat?
- Does Beat 2 communicate that one signal is not enough?
- Does Beat 3 feel like a role flip under pressure instead of a chore?
- Is the misread funny and instantly recoverable?
- Does the united-bark door payoff feel like the climax?

If any answer is no, fix layout, labels, timing, and feedback before touching realistic art.
