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

Every controller-owned objective target also drives a short three-paw scent breadcrumb near the
relevant dog. The trail is a heading hint, not a GPS route: it reveals only the first few metres,
uses that dog's identity color at low opacity, animates softly in sequence, and clears inside the
interaction zone. It must never extend all the way to a hidden objective or replace authored
landmarks, prop silhouettes, state animation, and partner communication. Because all 23 registered
missions use the shared `TryGetObjectiveTarget` contract, this rule applies roster-wide without
moving mission state into `GameManager`.

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

## Shared Player-Facing HUD Rule

Normal couch play must preserve the center of the play field for dogs, objectives, and hazards. The
current arena contract is one compact 102-pixel top bar for mission, objective/progress, timer, and
score, plus two bottom-corner identity chips for **P1 CHEDDAR** and **P2 COCOA**. Each chip may report
`PAD READY`, `PAD LOST`, `KEYS`, or `CONNECT PAD`; it must not become a full control legend. The
`PAD LOST` state stays short even though that dog's keyboard fallback remains available.

The Backyard Rescue action tutorial is a temporary teaching layer, not a second persistent HUD. It
reveals Bark, Interact, Jump, and Wrestle one at a time; Cheddar and Cocoa have independent cells, and
the next verb cannot appear until both players complete the current one. Pause must expose
skip/replay plus Audio, Rumble, and Camera Shake controls using controller navigation.

Diagnostic state, readiness gates, event history, full world-label maps, and control walls are not
part of the production first read. They remain behind the explicit F1/backquote observer overlay.
The bottom-left diagnostics toggle may appear only after that overlay has already been requested.

## Guidance Ladder

A shared, roster-wide stall-aware escalation ladder (`MissionGuidanceEscalation`, owned by
`GameManager`) answers "the team hasn't made progress in a while - help without breaking flow."
Any progress signal (a score event, an objective-copy change) resets it to Tier 0. It freezes
(does not accumulate) during the briefing/sniff-around lead-in, the opening explainer, pause, and a
held success payoff, and resets on every mission start/replay. **It never auto-completes a step for
the player** - every tier is a stronger nudge toward doing it themselves, never a substitute.

- **Tier 0 - Discovery (always on, default: 0-12s stalled):** the baseline from the rules above -
  proximity-gated labels, the quiet arrow icon, breadcrumbs, prop silhouettes. No ladder-specific
  rendering.
- **Tier 1 - Nudge (default: 12-25s stalled):** the current objective's `ObjectiveArrowFeedback`
  cue and breadcrumbs brighten (a warm tint pulse), the corresponding `MissionPropArtAttachment` on
  the objective prop pulses periodically regardless of dog proximity, and the acting dog(s) turn to
  face the objective using the existing idle-facing read (no new art).
- **Tier 2 - Coach (default: 25-45s stalled):** the active objective's contextual world label
  (the shared `AddWorldLabel`/`WorldLabelVisibility` path) has its proximity gate widened so it's
  readable well before the dog is close enough to normally trigger it. When exactly one dog owns the
  step (see the caveat below), the other dog's HUD identity chip pulses toward their own accent
  color.
- **Tier 3 - Rescue (default: 45s+ stalled, the ceiling):** the compact HUD objective line flashes
  (amber pulse) and, when the owning dog is known, is prefixed with their name. One placeholder audio
  cue (`ArenaFeedbackCatalog.Bark`; a dedicated "coach woof" cue is S5.1) fires once on the
  tier-up edge, not every frame.

**Owning-dog caveat:** `GameManager.GuidanceOwningDogIndex` is only set when exactly one dog has an
objective target this frame (`TryGetObjectiveTarget` returns true for one dog and false for the
other). Most missions hand *both* dogs a target at once, with different copy telling the non-acting
dog to stand down - by design this is not disambiguated by parsing that copy text, so on those
missions Tier 2's chip pulse and Tier 3's dog-naming simply stay off while the label-widening and
HUD-flash effects still apply. A future task (G1.3, the role-turn beacon) is expected to need a real
per-controller "who currently owns this step" signal; this ladder does not add one preemptively.

Per-mission tier-cap and timing overrides live on `GameManager.MissionDefinition`
(`GuidanceTierCap`, `GuidanceTier1/2/3Seconds`) as data, not code branches. All 23 missions currently
default to the full ladder at 12s/25s/45s.

## Pee Break Audit

Current staging issues addressed in this pass:

- The Teenager read too small/static compared with Cheddar and Cocoa, weakening the human-scale
  world fantasy.
- Door, leash, hallway, charger, phone, and bladder stations were carried by always-on colored pads
  and labels.
- The phone was mechanically important, but the first read could still be "cyan marker" rather than
  "attention blocker."
- The leash/door area needed a recognizable walk-object cluster instead of a colored station pad.
- Dog urgency was mostly meter/HUD-driven instead of visible on the dogs.
- Teenager cause/effect needed clearer presentation states: phone idle, annoyed glance, distracted
  again, and stand-up success.

The 2026-07-13 follow-up uses `pee_break_living_room_plate.png` as the dominant full-frame interior
read. The plate includes the closed front door and hides the procedural wall, wood-floor, baseboard,
standalone window, side-table, door frame, and door slab while preserving the full-arena foundation
underneath for extreme shared-camera framing. There is no active separate closed-door sprite. When
the team succeeds, `pee_break_living_room_success_plate.png` replaces the base plate and embeds both
the open architectural doorway and standing Teenager as one coherent payoff state.

Before success, the distracted seated Teenager and leash remain controller-owned overlays. The
standalone couch stays fallback-only because the Teenager art includes its seat, the standalone
phone/charger appears only when Beat 3 makes it actionable, and the isolated open-door sprite is a
fallback if the success plate cannot load. Their block-built silhouettes are suppressed whenever
replacement art loads. The current beat alone gets the strongest affordance: the phone enlarges
during the charger gambit, the leash brightens/pulses only when relevant, the hallway rug stays
hidden until Beat 3, and the comprehension/confusion fills plus four beat pips are thick enough to
read from the sofa.

Remaining placeholder limits:

- Pee Break now has coherent painterly base/success room plates and generated cartoon state props,
  but these are still raster production candidates with lightweight transform/tint motion rather
  than final authored character and prop animation.
- Text prompts still exist as close-range/debug scaffolding for couch testing.
- Audio still uses authored cue banks with generated fallback and needs a physical two-player mix
  pass.
- The shared dog-mounted objective arrows remain part of the current couch-test UI.

## Pee Break Review Shots

Use the intended couch-coop camera, not Scene View, for review. Recommended screenshots:

- Start: the base room plate fills the couch camera with no backyard leak or procedural
  wall/wood-floor/window rectangles showing through. Its baked closed door, both dogs, the seated
  phone-absorbed Teenager/beanbag, and hanging leash read cleanly without duplicate couch, phone, or
  door sprites.
- Beat 1: Cocoa near the door; only a close-range door prompt appears.
- Beat 2 partial: Cocoa at door without Cheddar on leash; Teenager reacts and leash remains visually
  obvious.
- Beat 3: enlarged generated phone/charger art, plug-state cue, phone drain, Cheddar hallway block,
  and Cocoa charger action visible; the procedural cord itself must not show through.
- Misread: tennis ball gag visible without needing the label.
- Beat 4: leash plus door setup and united-bark payoff framed together.
- Success: the authored success plate fully replaces the base room, embedding the standing Teenager
  and open doorway while the phone distraction disappears and the grass/hydrant payoff holds for the
  full 1.15-second live-world beat before the end card. The hold must remain a clear, readable reward
  rather than looking like a frozen or late UI transition.

Automated capture support already exists in the standalone player:

```sh
unity/CheddarAndCocoa/<built-player> --arena-art-review=/absolute/output/path
```

The current review harness writes start/main/payoff frames plus
`arena-art-review-manifest.md`. For Pee Break, inspect the Operation Pee Break frames first and
confirm the production view works with the playtest overlay off, then repeat with the overlay on to
verify debug labels/rings are available for developer review.
