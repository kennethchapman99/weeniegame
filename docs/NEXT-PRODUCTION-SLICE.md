# Next Production Slice

Build next: **Kitchen Falling Food Frenzy** (Game Design Bible #8).

Purpose: turn the documented arcade-collection-with-danger-filtering level into a real, playable
HouseChaos mission. It introduces a new dog-authentic co-op split — Cheddar scouts the counter and
tips food down; Cocoa sweeps the floor catching the good and dodging the gross/spicy/hot — without
needing a bespoke scene.

## Landed so far

- `ProductionMechanicModule.FallingFood` and the `kitchen_food_frenzy` catalog spec
  (`ProductionMissionCatalog.KitchenFoodFrenzy`), so the mission is addressable by id through
  `ProductionMissionFactory` and covered by the catalog-consistency guards.
- `KitchenFoodFrenzyMissionState` — the deterministic, UnityEngine-free core loop:
  - Scout `TriggerDrop(Food)` tips one item into flight at a time (forces the call-out hand-off).
  - Sweeper `Catch()` / `LetFall()` resolves it; scout `Nudge()` redirects danger into the safe
    landing zone for a teamwork save.
  - Good catches build a combo multiplier; bad/hot catches are strikes; dropped good food and
    nudged-away good food are combo-breaking misses; three strikes fail the round.
- `KitchenFoodFrenzyMissionStateTests` — deterministic coverage for the hand-off lock, combo
  scaling to clear, danger filtering, teamwork nudges, miss handling, fail, post-round no-ops, and
  config clamping/reset.

## Remaining to make it playable

- Add a `KitchenFoodFrenzy` value to `GameManager.MissionVariant` and a `MissionDefinition` that
  drives `KitchenFoodFrenzyMissionState` from real falling-item timing (warning circles for hot
  drops), wiring scout/sweeper roles to Cheddar/Cocoa.
- Route catch/dodge/nudge results onto the existing score path with readable labels, e.g.
  `+YUM`, `+COMBO`, `TEAMWORK SAVE`, `BAD BITE`, `BURN`, `MISSED TREAT`.
- Surface it in the mission picker/HUD and `MissionOutcomeSummaryBuilder`.
- Add a `MissionRuntimeSnapshot` id matching `kitchen_food_frenzy`.
- Author the explicit co-op puzzle beat per `docs/COOP-PUZZLE-DESIGN.md`: some items must be nudged
  into the safe zone by the scout before the sweeper can act, so neither dog can clear it alone.

## Guardrails

- Keep it inside ArenaScene; no new campaign persistence required for the first pass.
- Do not require final art — placeholder good/bad/hot readability is fine.
- Keep score mutations on the existing score path.
- Keep the round short, readable, and replayable; reset all frenzy state on replay/scene entry.
