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

## Phase 1: Backyard Expansion

### 1. Great Backyard Squirrel Conspiracy

Purpose: chase and herding.

New system:
- squirrel route nodes
- cutoff zones
- bark timing fake-out
- stash reveal

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

### 5. Forbidden Pool Toy

New system:
- movable pool noodle bridge
- anchor/push roles
- optional pool robot patrol

## Phase 3: House Chaos Pack

### 6. Cleaning Day Invasion

New system:
- human/cleaner route
- vacuum safe zones
- toy rescue before put-away
- mud/pawprint risk

### 7. Operation Pee Break

New system:
- human attention meter
- leash/shoe/charger interactions
- bladder emergency meter

### 8. Kitchen Falling Food Frenzy

New system:
- falling object spawner
- good/bad food tags
- warning circles
- catch combo

### 9. Dinner Table Begging Boss Fight

New system:
- line of sight
- soft/strict humans
- begging pose meter

### 10. Dog Couch War

New system:
- territory hold zones
- moving sunbeam
- toy gathering
- human interruption

### 11. Great Toy Search

New system:
- scent trails
- decoy toys
- scent confusion zones

## Phase 4: Specialty Pack

### 12. Vet Appointment

New system:
- multi-stage mission chain
- panic/calm meter
- held dog limited controls

### 13. Nail Clip Nightmare

New system:
- boss phases
- rhythm distraction
- held paw timing

### 14. Car Ride Chaos

New system:
- side balance
- turn sliding
- smell data meter

### 15. Walkies

New system:
- leash tether
- wrap/unwrap
- tension annoyance
- synchronized trot bonus

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
- bark/interact cannot be solved by spam;
- clear and fail states are funny and legible;
- replay resets the mission fully;
- score events explain what happened;
- keyboard and controller paths work;
- PlayMode tests cover clear, fail, replay, and session state;
- placeholder visuals have replacement slots.
