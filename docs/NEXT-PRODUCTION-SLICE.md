# Next Production Slice

## Current status

**Kitchen Falling Food Frenzy is implemented and playtest-ready.** It is selectable in the Unity
ArenaScene and includes Cheddar's explicit counter bark, readable pre-drop and landing telegraphs,
Cocoa catch/dodge feedback, compact shared-camera staging, recoverable failures, and the coordinated
GOOD → BAD → GOOD dinner-rush finale.

The production catalog exposes it as `kitchen_food_frenzy` through
`ProductionMissionCatalog.KitchenFoodFrenzy`. Deterministic state and PlayMode coverage live in
`KitchenFoodFrenzyMissionStateTests` and `KitchenFoodFrenzyPlayModeTests`; manual acceptance is in
`docs/ARENA-PLAYABLE.md`.

## Immediate next gate

Run the deferred two-human couch playtest before adding another mission: two controllers, Ken and
Sue, and at least 20 minutes across Backyard Rescue, Blanket Catch, and Kitchen Falling Food Frenzy.
Record where role ownership, telegraphs, camera framing, and recovery are unclear or stop being fun.
Use those observations to tune the existing slices.

## Next authored deep slice

After the couch-playtest fixes, build **Operation Pee Break: The Teenager Phone Rescue** from
`docs/DEEP-SLICE-OPERATION-PEE-BREAK.md`. Follow `docs/DESIGN-REVIEW-2026-06.md`: introduce the small
mission-controller boundary with this slice instead of adding another large behavior branch to
`GameManager`.

## Guardrails

- The Unity project remains the only active codebase.
- Keep placeholder art until the playtest proves where deliberate art work matters.
- Preserve deterministic mission coverage and recoverable failure states.
- Do not begin broad mission expansion before the couch-playtest findings are addressed.
