# Production Backlog

This backlog organizes the game into reusable mechanic slices. Do not build isolated one-off levels unless they create reusable systems.

## Current Foundation

Playable (all shipped on `main`, deterministic PlayMode tests, selectable from the in-scene mission select via keys 1-9/0):
- Backyard Rescue (collect + squirrel + predator + tug)
- Snack Heist (protect-and-collect)
- Sock Panic (timed collect)
- Squirrel Conspiracy (chase / herding — `HerdingMissionState`)
- Eagle Shadow Panic (threat-sweep hide + rescue + united bark — `ThreatSweepMissionState`, real cover zones + sweeping shadow)
- Coyotes at the Fence (patrol defense — `PatrolDefenseMissionState`, real fence gaps + prowling coyote)
- Weenie Roundup (carry-to-bowl — `CarryRoundupMissionState`)
- Scent Search (sniff hot/cold + dig — `ScentSearchMissionState`)
- Thunderstorm Comfort (panic co-regulation / huddle — `ThunderstormMissionState` + `PanicMeter`)
- Mark the Yard (territory control — `TerritoryMissionState`, prowling squirrel rival)
- Walkies on the Leash (leash physics — `LeashWalkMissionState`, tethered paired checkpoints)
- Car Ride Balance (vehicle balance — `CarBalanceMissionState`, counter-lean the tilt)

**Mechanic-module coverage complete:** all nine `ProductionMechanicModule` values (Herding, ThreatSweep, PatrolDefense, SharedObject, TerritoryControl, ScentSearch, RhythmPanic, VehicleBalance, LeashPhysics) now have at least one playable, tested mission.

The arena is now a 120x68 scrolling yard with an explicit <=2% dog-to-property width contract, a dynamic local/strategic shared camera, landmark districts, and full-yard spatial geometry for the threat/territory/leash missions. Dog-verb coverage: chase, rescue, steal, distract, defend, comfort, carry, tug, hide, sniff, dig, bark, mark, balance. Meta: per-mission session-best, flawless bonus + tally, end-card MVP, and a New Session reset. Deterministic PlayMode coverage validates the scale contract and mission geometry.

Reusable now:
- mission select
- replay / next / session summary
- collectibles
- squirrel pressure
- predator warning and rescue
- bark and united bark
- tug objective
- score events, ranks, stars
- objective labels and PlayMode tests

## Phase 0: Co-op Puzzle Quality Pass

Before adding many more missions, upgrade the most straightforward existing loops with authored co-op puzzle beats. The goal is not more counters or harder timers; the goal is the premium co-op feeling where one player creates an opening and the other player converts it into progress.

Read first:
- `docs/COOP-PUZZLE-DESIGN.md`
- `docs/GAME-DESIGN-BIBLE.md` / **Co-op puzzle magic standard**
- `docs/MISSION-SYSTEM.md` / **Co-op Puzzle Beat Requirement**

Priority upgrades:

### Backyard Rescue puzzle pass

Add a squirrel trap / redirected escape beat.

- One dog baits or scares the squirrel into a route.
- The partner blocks, holds, or opens the route that turns the chase into progress.
- Success visibly drops/recovers food or reveals the next objective.
- Failure shows the squirrel using the unblocked route and teaches what was missing.

### Snack Heist puzzle pass

Add a distract-and-sneak food theft beat.

- One dog distracts the human/table watcher/squirrel pressure.
- The other dog moves through a temporary safe lane to stash the snack.
- Cheddar food temptation can become a soft failure or funny complication.

### Sock Panic puzzle pass

Implemented as **Tip and Dive**.

- One dog tips the generated laundry basket and creates a six-second opening.
- Only the partner can grab the exposed sock and turn that opening into progress.
- A same-dog grab or timeout creates a scored decoy/fumble, visibly closes the basket, and immediately allows recovery.
- Objective arrows split into `TIP BASKET`, `HOLD BASKET`, and `DIVE FOR SOCK`; deterministic hooks cover tip, collection, timeout, clear/fail, and reset.

### Weenie Roundup puzzle pass

Add a heavy-object or gate-routing beat.

- Jumbo weenie needs both dogs, or one dog carries while the other steadies/opens a route.
- Fumbles should be comic and teach spacing/timing.

### Mark the Yard puzzle pass

Add zone setup instead of only simultaneous zone occupation.

- Some zones require one dog to prime scent/hold attention while the other claims.
- Squirrel re-marking can be baited with a decoy claim.

Acceptance:
- each upgraded mission has a named **Co-op Puzzle Beat** in docs;
- each beat has role split, lock/key dependency, readable hints, funny fail, world-state change, and test hooks;
- PlayMode tests still pass and cover the new state change where feasible;
- the mission is more surprising without becoming unclear.

## Phase 1: Backyard Expansion

### 1. Great Backyard Squirrel Conspiracy

Purpose: chase and herding.

New system:
- squirrel route nodes
- cutoff zones
- bark timing fake-out
- stash reveal

Puzzle beat expectation:
- one dog baits/pressures the squirrel while the other occupies or closes the real escape lane;
- stash reveal should require controlling both the apparent route and the hidden route, not only filling a counter.

Reuses:
- squirrel actor
- bark
- score events
- objective labels
- replay flow

### 2. Eagle Shadow Panic

Purpose: hide, survive, rescue.

New system:
- sweeping shadow hazard
- safe cover zones
- hide state
- final united bark circle

Puzzle beat expectation:
- at least one dynamic-cover moment where one dog creates/holds shade while the other rescues or crosses;
- the final united bark should be earned by solving positioning, not only standing together.

Reuses:
- predator timing
- rescue
- united bark
- objective arrows

### 3. Coyotes at the Fence

Purpose: defensive patrol.

New system:
- fence weak spots
- repair/fill interaction
- isolated dog targeting
- fake snack lure

Puzzle beat expectation:
- bark-pin + repair should escalate into a multi-step lock: pressure, fill, then move/brace a physical blocker;
- fake snack lure should be a readable bait-and-switch puzzle, not only a penalty event.

Reuses:
- predator warning
- bark
- rescue
- score/fail pressure

## Phase 2: Pool Pack

### 4. Pool Floaty Disaster

New system:
- floaty surfaces
- water fall state
- swim slowdown
- shake recovery
- wet timer

Puzzle beat expectation:
- one dog anchors or moves a floaty/noodle route while the other crosses;
- wet/shake state should become a useful puzzle tool at least once, not only a debuff.

### 5. Forbidden Pool Toy

New system:
- movable pool noodle bridge
- anchor/push roles
- optional pool robot patrol

Puzzle beat expectation:
- use anchor/push/rescue roles as a sequence: open route, retrieve toy, escape while pool robot or water state changes the route.

## Phase 3: House Chaos Pack

### 6. Cleaning Day Invasion

New system:
- human/cleaner route
- vacuum safe zones
- toy rescue before put-away
- mud/pawprint risk

Puzzle beat expectation:
- prove distract-and-sneak: one dog manipulates cleaner/vacuum attention while the other rescues toys or moves through a blind spot.

### 7. Operation Pee Break

New system:
- human attention meter
- leash/shoe/charger interactions
- bladder emergency meter

Puzzle beat expectation:
- prove social manipulation: the dogs combine multiple signals so the teenager finally understands "outside now";
- one dog should assemble physical evidence at the door while the other interrupts the phone/charger attention loop.

### 8. Kitchen Falling Food Frenzy

New system:
- falling object spawner
- good/bad food tags
- warning circles
- catch combo

Puzzle beat expectation:
- avoid pure arcade collection: one dog should trigger/redirect drops while the other catches, blocks, or filters good/bad food.

### 9. Dinner Table Begging Boss Fight

New system:
- line of sight
- soft/strict humans
- begging pose meter

Puzzle beat expectation:
- use social stealth: Cocoa locks pity attention, Cheddar redirects dropped food or creates under-table chaos without exposing the team.

### 10. Dog Couch War

New system:
- territory hold zones
- moving sunbeam
- toy gathering
- human interruption

Puzzle beat expectation:
- one dog manipulates a human/sunbeam/cushion position while the other claims or defends the sacred couch zone.

### 11. Great Toy Search

New system:
- scent trails
- decoy toys
- scent confusion zones

Puzzle beat expectation:
- split scent information: Cheddar and Cocoa should receive different scent certainty, forcing communication before choosing which pile/path to trust.

## Phase 4: Specialty Pack

### 12. Vet Appointment

New system:
- multi-stage mission chain
- panic/calm meter
- held dog limited controls

Puzzle beat expectation:
- held dog must still participate through wiggle/lean/timing clues while the free dog distracts, drags, knocks, or rescues.

### 13. Nail Clip Nightmare

New system:
- boss phases
- rhythm distraction
- held paw timing

Puzzle beat expectation:
- free dog distraction should affect which held-paw timing window opens; wrong distraction protects the wrong paw or starts the "just one more nail" escalation.

### 14. Car Ride Chaos

New system:
- side balance
- turn sliding
- smell data meter

Puzzle beat expectation:
- one dog gets early lurch/smell information while the other must counterweight or block sliding temptation.

### 15. Walkies

New system:
- leash tether
- wrap/unwrap
- tension annoyance
- synchronized trot bonus

Puzzle beat expectation:
- leash wrapping must become a puzzle tool: pull open a gate, redirect human motion, or move an object, then safely unwrap before the tension fail.

## 1v1 Minigame Entry Point

Do not build all minigames yet. Build the framework only after one of these systems exists:

- Tug-of-War Supreme after shared-object work improves.
- Sunbeam King/Queen after couch territory works.
- Treat Drop Duel after kitchen drops work.
- Best Bark Battle after rhythm/panic work exists.

## Production Readiness Checklist

A mission is production-ready only when:

- mission select starts it cleanly;
- objective is readable quickly;
- both dogs have meaningful roles;
- it includes at least one authored co-op puzzle beat where one dog creates an opening and the other turns it into progress;
- the co-op beat has a readable lock/key dependency, visible world-state change, and funny fail/recovery path;
- bark/interact cannot be solved by spam;
- clear and fail states are funny and legible;
- replay resets the mission fully;
- score events explain what happened;
- keyboard and controller paths work;
- PlayMode tests cover clear, fail, replay, session state, and the puzzle beat state change where feasible;
- placeholder visuals have replacement slots.
