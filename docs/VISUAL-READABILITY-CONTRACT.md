# Visual Readability Contract

> Status: ACTIVE. Use this for Unity scene staging and couch-coop readability reviews. The frozen
> TypeScript/Canvas and prototype folders are reference only.

## Contract Rules

- Humans must be visibly larger than the dogs in every gameplay camera composition.
- Cheddar and Cocoa should feel like miniature dachshunds in a human-scale world: furniture, doors,
  counters, shoes, beds, chairs, and humans should tower over them.
- Major objectives must be understandable from the environment before players read instructions.
- Production gameplay should not rely on giant objective circles or explanatory world text.
- Debug labels, debug rings, and oversized instructional pads must be dev-only or playtest-overlay
  only.
- Interactables need visual affordances: silhouette, contrast, glow, motion, state change, proximity
  highlight, or a small contextual prompt.
- Important NPCs need at least basic idle, react, success, and fail/recovery states.
- Each level needs clear camera composition and a scale hierarchy: playable dogs, objective props,
  blockers, NPCs, and exit/payoff must read in that order.
- Every objective needs readable before/after state changes, not only a score or HUD update.

## Shared Label/Prompt Rule

All mission world labels created through the shared Unity `AddWorldLabel` path are contextual
prompts, not production staging. In normal play, their text and generated label skin render only
when Cheddar or Cocoa is near enough to plausibly interact with the object. Pressing the F1
playtest/debug overlay restores the full label map for developer review. Score pops remain visible
because they are short-lived cause/effect feedback.

Dog-mounted objective arrows may remain as couch-coop wayfinding, but their explanatory text is
debug/playtest copy. In normal play the generated arrow icon can point at the next useful target;
the text label appears only with the F1 overlay. If a level cannot be understood with the arrow text
hidden, the level needs stronger environment staging, object pose, or state-change cues.

Actionable bark, tug, and rescue range rings may remain visible while the action is available, but
their support text is debug/playtest copy. In normal play these rings must read through icon shape,
placement, scale, color, and object/character context, not through labels like `BARK RANGE`, `BOTH
DOGS`, or `RESCUE BARK`.

Mission actor state labels such as squirrel, predator, basket, bowl, or rope state copy are support
text, not the production first read. Their state strings may remain available to tests, HUD/debug
review, and close-range prompts, but the actor itself must communicate the primary state through
silhouette, sprite state, color, pulse, pose, motion, or before/after placement.

Generated mission prop overlays attached through `MissionPropArt` must behave like interactables in
the room, not permanent UI. When no dog is nearby they should sit as ordinary staged objects. When a
dog enters staging range they may pulse/tint subtly to say "this object matters now." The cue must
clear when the dogs leave range, so the whole level does not glow at once.

Roster-wide audit note: many current missions still have objective logic represented by labels,
arrows, range rings, and generated silhouettes. That is acceptable for the current couch-test slice
only when the environment/object silhouette gives the first read and the text is nearby/debug
scaffolding. Future mission-specific staging passes should replace label-first reads with scale,
pose, object clusters, and before/after prop states.

## Pee Break Audit

Current staging issues addressed in this pass:

- The Teenager read too small/static compared with Cheddar and Cocoa, weakening the human-scale
  world fantasy.
- Door, leash, hallway, charger, phone, and bladder stations were carried by always-on colored pads
  and labels.
- The phone was mechanically important, but the first read could still be "cyan marker" rather than
  "attention blocker."
- The leash/door area needed more walk-object context such as shoes and a mat.
- Dog urgency was mostly meter/HUD-driven instead of visible on the dogs.
- Teenager cause/effect needed clearer presentation states: phone idle, annoyed glance, distracted
  again, and stand-up success.

Remaining placeholder limits:

- Pee Break still uses generated cartoon PNGs and Unity primitive silhouettes, not final authored
  animation.
- Text prompts still exist as close-range/debug scaffolding for couch testing.
- Audio cues are still procedural/slot placeholders.
- The shared dog-mounted objective arrows remain part of the current couch-test UI.

## Pee Break Review Shots

Use the intended couch-coop camera, not Scene View, for review. Recommended screenshots:

- Start: both dogs, couch, Teenager, glowing phone, door, leash hook, shoes, and door mat visible.
- Beat 1: Cocoa near the door; only a close-range door prompt appears.
- Beat 2 partial: Cocoa at door without Cheddar on leash; Teenager reacts and leash remains visually
  obvious.
- Beat 3: charger cord, outlet, phone drain, Cheddar hallway block, and Cocoa charger action visible.
- Misread: tennis ball gag visible without needing the label.
- Beat 4: leash plus door setup and united-bark payoff framed together.
- Success: Teenager stands up, phone is no longer dominant, door opens to grass/hydrant payoff.

Automated capture support already exists in the standalone player:

```sh
unity/CheddarAndCocoa/<built-player> --arena-art-review=/absolute/output/path
```

The current review harness writes start/main/payoff frames plus
`arena-art-review-manifest.md`. For Pee Break, inspect the Operation Pee Break frames first and
confirm the production view works with the playtest overlay off, then repeat with the overlay on to
verify debug labels/rings are available for developer review.
