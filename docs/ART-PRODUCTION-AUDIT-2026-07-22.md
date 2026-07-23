# Production Art Audit — 2026-07-22

## Scope and decision

This pass audited the active Unity project only: `unity/CheddarAndCocoa/`. Frozen TypeScript and
prototype folders were not changed. The review covered the live `ArenaFinal` resource tree,
`FinalGameplayArt`, mission-controller visual attachments, character-motion resources, mission
tiles, world/HUD skins, PlayMode art checks, and the explicit placeholder limits in
`ARENA-PLAYABLE.md` and `VISUAL-READABILITY-CONTRACT.md`.

The highest-impact uncovered gap was the newly registered **Tick Invasion** mission. It had a
mission-select tile, but the playable interaction reused `dog_bowl` as its pool, displayed no tick
silhouette on either dog, and communicated groom/rinse success through labels and generic juice.
That violated the silhouette-first and state-change parts of the visual readability contract.

## Ranked remaining production needs

1. **Character style convergence (P0):** Cheddar/Cocoa and several threat cutouts still lean toward
   rendered/realistic fur while the mission props use flat cel-shaded cartoons. Re-author the two
   dog master turnarounds first, then regenerate directional motion from one approved silhouette.
   Four-view V02 model sheets now establish that authority and run/bark/groom use it; remaining
   directional clips and outcome poses still need convergence.
2. **Authored character animation (P0):** current run/bark/dig/sniff and threat strips are readable,
   but many frames are transform-derived. Replace the most repeated verbs—run, bark, groom, tug,
   stunned/rescue—with hand-authored key poses and consistent timing. Groom and the roster-wide
   bark are now resolved in the flat-storybook benchmark; run is resolved for the primary side
   silhouette with legacy directional fallback; tug, stunned, and rescue remain.
3. **Mission-state pack review (P1):** the 25-mission roster has broad generated state coverage, but
   each mission still needs a couch-distance review for scale, silhouette, palette, and before/after
   state contrast. Prioritize any prop that still needs a world label to identify it.
4. **Environment cohesion (P1):** the backyard mixes painterly/photo-inspired plates, flat prop
   overlays, and generated geometry. Produce a single approved ground/landmark paintover and tune
   overlay saturation/outline weight against it before adding more decoration.
5. **HUD and transition motion (P1):** mission select and end cards have a generated skin but remain
   runtime IMGUI/TMP surfaces. Add authored focus, confirm, lock, clear, fail, and next-mission
   transitions while preserving controller navigation and accessibility toggles.
6. **Interaction VFX and physical response (P2):** bark, pickup, rescue, and several mission beats
   have generic bursts. Continue adding verb-specific effects only where they communicate a rule:
   hold, transfer, catch, rinse, comfort, distract, or defend.
7. **Final mix and capture review (P2):** presentation still needs a two-player couch mix and a
   fresh `--arena-art-review` capture audit at representative gameplay zooms.

## Implemented in this pass: Tick Invasion production pack

Five transparent, no-text sprites now live under `ArenaFinal`:

- `Props/TickInvasion/tick_invasion_pool`
- `Props/TickInvasion/tick_invasion_swarm`
- `Props/TickInvasion/tick_invasion_super_tick`
- `VFX/TickInvasion/tick_invasion_groom_burst`
- `VFX/TickInvasion/tick_invasion_rinse_burst`

`TickInvasionMissionController` now owns their runtime use. The pool replaces the dog-bowl stand-in
and pulses as a proximity affordance. Each dog gets a mission-local infestation overlay that grows,
brightens, and pulses with pressure; the Super Tick swaps to a distinct urgent silhouette; wet dogs
receive a cool tint. Successful grooming and pool dives spawn different short-lived art bursts.
Gameplay state, collision, score, timing, and mission outcome logic are unchanged.

## Generation record

The assets were created with the built-in image-generation path, then converted from a flat
`#00ff00` chroma background with the imagegen skill's `remove_chroma_key.py` helper. All outputs were
validated as RGBA PNGs with transparent corners and visually inspected after key removal.

Shared prompt contract: polished flat cel-shaded storybook Unity sprite; warm saturated palette;
thick dark-navy outline; clean couch-distance silhouette; no baked text, logo, watermark, cast
shadow, background texture, floor plane, or photorealism. Asset-specific subjects were: top-down
kiddie pool with orange bone and teal paw accents; compact seven-tick swarm; oversized magenta Super
Tick; brush/paws/ticks grooming burst; and open-center turquoise rinse splash throwing ticks away.

## Acceptance evidence

- Every path is centralized in `FinalGameplayArt.TickInvasionArtPack`.
- PlayMode coverage checks that all five resources load, the pool uses its authored sprite, clean
  dogs hide the overlay, pressure reveals the swarm, Super Tick swaps the sprite, and groom/rinse
  actions spawn distinct VFX objects.
- Manual couch check: confirm the pool footprint matches its interaction radius, infestation never
  obscures dog identity, the Super Tick reads as comic rather than frightening, and both bursts are
  legible without reading the HUD.

The macOS dev build succeeded and its updated `--arena-art-review` harness produced all 75 expected
frames (25 missions × start/main/payoff). The three Tick Invasion frames were inspected at the
player's 1920×1080 target: clean dogs remain unobscured, the rising swarms read at body scale, the
Super Tick is unmistakable, the pool fits both dogs, and draw order keeps dogs inside the water while
the urgent tick remains above character art. Focused Tick Invasion PlayMode evidence: **19/19**
passing on 2026-07-22.

## Follow-on character benchmark: authored mutual grooming

Tick Invasion now includes two transparent E-facing grooming keyframes for each dog:

- `Characters/Dogs/Cheddar/Motion/cheddar_groom_e_00` and `_01`
- `Characters/Dogs/Cocoa/Motion/cocoa_groom_e_00` and `_01`

These are the first gameplay character frames deliberately converged toward the production pack's
flat storybook language: compact color shapes, thick navy contour, warm highlights, and no rendered
fur texture. Cheddar's crouch is quicker and more forward; Cocoa's is steadier and more deliberate,
preserving their animation asymmetry. West-facing use mirrors the approved E silhouette.

`DogGroomAnimation` loads the pair through `CharacterMotionArt.Clip.Groom` and temporarily overrides
the existing single dog-art renderer for 0.68 seconds. The successful interaction remains owned by
`TickInvasionMissionController`; no grooming input, state, ticking, or outcome branch was added to
`GameManager`. Reusing the shared renderer also prevents the old body from showing through the new
silhouette. Mission cleanup restores the normal pose immediately.

Generation used the built-in image-generation path in four individual edit passes, with the existing
Cheddar/Cocoa idle art as identity reference and the approved Tick Invasion VFX as style reference.
Each prompt requested a single full-body side-view dachshund grooming pose on a solid `#00ff00`
background, no text, shadow, scenery, extra animals, photorealistic fur, or cropped anatomy. Frame 1
also referenced frame 0 and requested a clear head/paw/tail change while preserving model and canvas
placement. The imagegen skill's chroma-key helper produced the transparent RGBA exports; the runtime
copies are now normalized to the shared 512×384 canvas at baseline Y=360 and the project's 256
pixels-per-unit import scale.

Focused Tick Invasion PlayMode evidence after integration: **21/21** passing on 2026-07-22. Coverage
now verifies all four resources, two distinct keyframes per dog, successful-groom playback on the
correct groomer, and cleanup restoration of the shared pose renderer.

The fresh macOS dev build succeeded. Its graphics-enabled standalone review captured **78/78**
frames for the current 26-mission roster. The Tick Invasion main-interaction frame was staged
through a real successful Cocoa groom (using her wider gameplay reach): the E pose mirrored toward
Cheddar, stayed grounded at the established dog scale, retained a clean silhouette under the
groom/score effects, and did not expose Cocoa's previous renderer underneath. Full PlayMode
regression evidence after the shared-renderer change: **811/811** passing.

## Follow-on character benchmark: roster-wide storybook bark

The second character-convergence slice adds two E-facing bark poses for each dog without deleting
the previous five-direction set:

- `Characters/Dogs/Cheddar/Motion/cheddar_bark_storybook_e_00` and `_01`
- `Characters/Dogs/Cocoa/Motion/cocoa_bark_storybook_e_00` and `_01`

`CharacterMotionArt.Clip.BarkStorybook` is now the live `Pose.Bark` mapping. It alternates the
anticipation and peak-WOOF silhouettes for the existing 0.35-second bark window, mirroring for west.
Cheddar runs at 10 fps with an airborne-ear, high-tail peak; Cocoa runs at 7 fps with a planted,
authoritative peak. If either storybook resource is unavailable, `DogReadabilityFeedback` retries
the previous directional `Clip.Bark` frame before accepting the static pose fallback. The bark
event, gameplay radius, mission handling, feedback VFX, audio, input, and timing are unchanged.

The four assets used the built-in image-generation path. The final Cocoa correction uses her V02
model sheet as the identity authority and the earlier action only as a pose reference. Frame-0
prompts requested full-body side-view bark
anticipation on uniform green chroma, preserving collar, markings, baseline, and proportions.
Frame-1 prompts referenced their matching frame 0 and changed only mouth, muzzle, ear, tail, and
brace acting for a clear peak bark. Chroma removal used the imagegen skill helper with soft matte
and despill. `tools/art/normalize_storybook_bark.py` trims the alpha subject and places both frames
on the shared 512×384 dog-motion canvas at baseline Y=360; Unity imports them at 256 pixels per unit.

Focused final-art evidence: **13/13** passing. The coverage verifies both storybook frames per dog,
the stable `bark_storybook` resource contract, personality-specific frame timing, live preference
for the new clip, and continued loading of every legacy directional bark frame. Full PlayMode
regression after live promotion: **811/811** passing. The fresh macOS dev build succeeded and the
26-mission review harness wrote **78/78** frames. Backyard Rescue's main frame now triggers a real
Cheddar bark and uses a temporary 4.5-unit review zoom: the final capture matches the established
idle ground/shadow offset, keeps a clean chroma edge, and remains readable under bark sparks,
paw-ring feedback, direction guidance, and the nearby Cocoa silhouette.

## V02 identity sheets and storybook run

The current production identity authority is now explicit:

- `cheddar_storybook_model_sheet_v02.png`
- `cocoa_storybook_model_sheet_v02.png`

Each sheet provides neutral side, three-quarter, front, and rear views. Cheddar remains
golden-orange with pale cream chest/toe tips and a red-orange collar. Cocoa remains uniform deep
chocolate with subtle warm-brown muzzle/eyebrow/lower-paw points and a teal collar; cream or white
body markings are forbidden. `CHARACTER-ART-MODEL-SHEETS.md` records the authority hierarchy and
promotion checklist.

The matching reference run boards contain four clean right-facing poses per dog: contact, extension,
landing, and push-off. `tools/art/export_storybook_run.py` extracts them to
`<dog>_run_storybook_e_00..03` on the shared 512×384/Y=360 canvas. Cheddar's 10 fps cycle has more
airtime and head-led overshoot; Cocoa's 8 fps cycle stays lower and more controlled.
`CharacterMotionArt.Clip.RunStorybook` is the live `Pose.Run` mapping. Missing V02 resources retry
the old complete directional `Clip.Run` set before the static pose fallback.

The same sheet review exposed cream drift in Cocoa's initial storybook bark and grooming frames.
Those four assets were regenerated with her V02 sheet as identity authority and the old assets as
pose references only. The corrected strip is uniform chocolate with stable warm points and teal
collar. `normalize_storybook_bark.py` and `normalize_storybook_groom.py` keep both dogs on the same
runtime canvas and baseline.

Verification after promotion: the focused final-art suite passes **13/13**, the full PlayMode suite
passes **811/811**, and a fresh macOS development build succeeds. The graphics-enabled review
harness writes **78/78** frames with no exceptions. Its Snack Heist main frame uses a temporary
4.5-unit camera and held Cocoa run velocity; the resulting frame shows a clean, unobstructed V02
silhouette, stable teal collar, uniform chocolate coat, and readable lower-bounce personality.

## 2026-07-23 environment, indicator, and small-prop pass

The three indoor blockout cards are now reviewed storybook plates rather than flat geometry:

- the living-room den uses honey wood, a quiet teal rug, edge furniture, and a dog-toy basket;
- the dining room uses warm oak, a woven burgundy rug, compact chairs/sideboard, water bowl, and
  mouth-scale rope toy;
- the kitchen uses cream-and-sage tile, a perimeter cabinet edge, dog-bowl mat, and tennis ball.

All keep their central co-op lanes quiet, share a rounded 4:3 footprint and upper-left lighting,
and render at a common 48×36-world-unit room scale. Approved V02 source copies live in
`ReferenceOnly/GeneratedLevelAreas`; `generate_indoor_level_area_packs.py` promotes those sources so
regeneration cannot silently restore the blockouts.

Indoor staging now temporarily suppresses the shared backyard decorative root and the pool's
visual-only child. This fixes the pool, patio, stepping stones, and route stripes compositing over
room art while keeping the pool component, colliders, camera, and gameplay systems alive. The
suppression is reference-counted so an indoor-to-indoor mission switch cannot flash the yard for one
frame.

Thunderstorm Comfort's four cue states were replaced with one coherent cloud/blanket family:
waiting paws, lightning/sparks, paired huddle/heart, and sunlit clear. The cue uses an explicit
3.6-world-unit width instead of inheriting source texture resolution; PlayMode coverage constrains
it to 3.2–4.2 units beside the roughly two-unit dogs. Operation Pee Break's optional squeaky toy now
uses authored art at mouth scale and disables its primitive fallback while loaded. The shared toy
sprite is no longer a smiley-blob blockout: it is a coral rubber dachshund with gold ears/squeaker
and a teal collar, retained as `GeneratedMissionProps/squeaky_toy_storybook_v02.png` and promoted by
the deterministic mission-prop generator.

On-screen world prompts now cancel non-uniform marker-root scale. This removes room-wide stretched
labels on broad interaction zones (most visibly Kitchen's counter) while preserving contextual
near-range visibility, icon-only distance signals, and the F1 diagnostic overlay.

Generation used the built-in image-generation path. Room prompts preserved each blockout's exact
footprint and requested flat-cel storybook rendering, quiet play lanes, human-scale perimeter
details, transparent outer corners, and no dogs, people, text, letters, UI, or objective markers.
Thunderstorm prompts requested a compact charcoal/lavender cloud and teal blanket state family on
green chroma; the imagegen helper removed the key and despilled the final transparent sprites.

The final-art suite passes **13/13** in muted/headless mode; the shared guidance suite passes
**7/7**; the focused Pee Break toy interaction and final indoor-switch cleanup checks each pass
**1/1**. Coverage includes resource loading, indoor ownership, same-frame mission switches, stable
world-label scale, Thunderstorm's rendered bounds, and the toy's gameplay-neutral behavior. The
macOS development build succeeds and its bundle passes deep strict code-signature verification.

The partial standalone review captured 63 frames before audible player review was stopped at the
owner's request; those frames were the evidence that exposed the outdoor-over-indoor compositing
defect. No further audible player capture was launched.

### Foreground furniture and Table Stealth continuation

Kitchen's broad counter marker now carries a reviewed storybook furniture pass: honey-oak cabinet
panels, brass knobs, a pale blue-gray worktop, and compact ingredient boards frame the action
without occupying its center landing lane. The approved source is
`GeneratedLevelAreas/kitchen_counter_wall_storybook_v02.png`, and the indoor-level generator
promotes it alongside the three room plates.

Table Stealth's circle-and-rectangle blockouts were replaced with a coherent seven-state family.
One friendly blue-shirted adult and honey-oak/burgundy table persist through watching, distracted,
spotted, and playful caught poses. One cream plate with a burgundy rim persists through steak
available, sliding sneak-progress, and empty success reads. The states deliberately avoid embedded
text, punishment, menace, and realistic meat detail; posture, eyeline, motion ticks, aroma, crumbs,
and the controller-owned HUD communicate the changes.

The human/table vignette has a stable 4.4-world-unit width and the steak plate a 1.8-unit width.
`MissionPropArt.AttachObjectAtWorldWidth` cancels both axes of non-uniform marker scale, preserving
the square source proportions while leaving marker collision and mission state authoritative.
Normal play hides those primitive fallback shapes; F1 still reveals them for diagnostic review.
Reviewed sources live in `ReferenceOnly/GeneratedTableStealthStorybook`, and
`promote_table_stealth_storybook.py` makes the runtime promotion repeatable.

These eight foreground assets used the built-in image-generation path on green chroma followed by
the imagegen skill's soft matte and despill helper. The kitchen prompt preserved the existing wide
counter footprint and requested warm flat-cel furniture, a quiet center lane, restrained
upper-left highlights, and no people, dogs, text, UI, or objective markers. The Table Stealth
prompts required state-to-state identity continuity, readable comedy at gameplay scale, transparent
backgrounds, and no embedded words or gore.

Muted/headless verification passes: **13/13** final-art integration checks and **9/9** Table Stealth
mission checks. The added coverage constrains both authored world widths, preserves source aspect
ratios under the human marker's non-uniform scale, and confirms the normal-play fallback is hidden.
No standalone player or audible review was launched.

## 2026-07-23 shared backyard style-convergence pass

The shared backyard plate now matches the flat-cel storybook benchmark established by the V02 dog
model sheets, indoor rooms, and mission-state props. The paintover preserves the exact 1672×941
footprint and existing landmark map—house stoop, perimeter fence, flower beds, tree and swing,
stepping-stone path, sandbox, picnic blanket, stone patio, hedges, and the broad central lawn—so no
mission staging, camera framing, or gameplay ownership changed. It simplifies realistic foliage
microdetail into outlined two-tone shapes, concentrates saturation around the perimeter, and keeps
the central and lower-center play lanes quiet beneath bright runtime characters and cues.

The reviewed source is
`ReferenceOnly/GeneratedBackyardMegapass/yard_backyard_plate_storybook_v03.png`.
`promote_backyard_storybook.py` validates its exact dimensions and copies it into the stable
`yard_backyard_plate_v02` runtime resource path. The built-in image-generation edit used the prior
yard as strict composition authority and the living-room V02 plate only as a style reference. The
prompt explicitly prohibited dogs, people, threats, mission props, frozen actors, labels, text, UI,
logos, and new structures.

Muted/headless acceptance passes: **9/9** backyard resource/contract checks, **13/13** live backyard
enhancer scene checks, and **13/13** final-art integration checks. Coverage confirms the exact source
footprint, full-yard world bounds, full opacity, background sorting, scenery-only resource
ownership, and continued retirement of the old photo-crop collage. The overlay audit kept
mission-scoped trails, threat lanes, hide bushes, and district props because they communicate live
rules; it did not remove them solely for visual minimalism. No standalone player or audible review
was launched.
