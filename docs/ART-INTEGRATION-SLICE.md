# Backyard Rescue Art Integration Slice

> **Status: OPERATIONAL REFERENCE.** This records the current asset integration. Further art work
> is selected from critical couch-playtest findings; it does not supersede the active sequence in
> `NEXT-PRODUCTION-SLICE.md`.

## Live runtime art

`ArenaFinal` now contains the original transparent, individually trimmed gameplay sprites plus
promoted lightweight motion frames for the dogs and threat characters. The Backyard Rescue runtime
prefers these assets while retaining every generated silhouette, collider, and safe fallback.

- Cheddar and Cocoa: idle, run, bark, tug, stunned, rescued, proud, and sad.
- Mission characters: squirrel idle/steal/scared, eagle threat/action, and coyote threat, plus
  runtime motion strips for squirrel idle/run/steal/scared, eagle sweep/attack, and coyote patrol/
  threaten/retreat.
- Optional cameo: bunny idle/scared.
- Mission props: weenie collectible, tug rope, completed rope, and dog bowl.
- Backyard props: bush, fence section, rock, grass patch, and dig spot.
- Environment prop overlays: patio/back door, fence run, snack table, laundry corner, lawn landmark,
  Pee Break outdoor path, scent trail, leash route, pond, shade tree, garden bed, flower patch,
  picnic blanket, sandbox, stepping-stone path, and eagle/coyote threat lane.
- Building prop overlays: house facade, back-porch entry, and yard shed.
- VFX: bark burst/ring, pickup sparkle, success pop, warning alert, rescue burst, and fail puff.
- Gameplay cues: objective arrow, target paw, bark range, rescue range, and tug range.
- Dog-local VFX: Cheddar/Cocoa paw trails, action spark/glint particles, collar glint, and ground
  glow.
- Kitchen cues: gold-food and purple-onion counter telegraphs plus matching floor landing warnings.
- Chaos Machine props: towel-drop, basket-tip, and toy-launch Rube Goldberg junction stations.
- HUD skin: panel frame, mission tile, selected mission tile, badge frame, primary button, and
  overlay panel.
- World-label skin: mission label bubble, command ribbon, warning triangle, and score-pop burst.
- Generated arena SFX: dog bark, team success, crunch collect, squirrel alarm, score sparkle,
  penalty thunk, victory fanfare, failure sigh, UI blip, and threat rattle.

The current live pass wires dog poses and motion, squirrel state and motion, eagle/coyote threat
state and motion, rope state, dynamic Backyard Rescue treats, backyard bushes/rocks, and bark VFX.
`FinalJuiceEffect` now consumes the sequenced gameplay-feedback stream and displays pickup sparkle,
success pop, warning alert, rescue burst, or fail puff without changing gameplay state. A newer event
replaces the previous transient effect so rapid couch-play beats remain readable instead of stacking.
The generated environment prop pass layers transparent cartoon sprites over broad yard district
objects through `BackyardRescueArtEnhancer`, capping the underlying square renderer alpha while
preserving every generated object as the nonblocking layout/readability fallback.
The generated gameplay cue pass gives `ObjectiveArrowFeedback` and `InteractionRangeIndicator`
transparent cartoon sprites for objective direction and actionable bark/tug/rescue ranges while
retaining text copy as support.
The generated dog-FX pass gives `DogActionFeedback` and `DogShowcasePolish` transparent dog-local
sprites for paw trails, action particles, collar glints, and ground glows while preserving the
existing movement/action timing and collider roots.
The generated Kitchen cue pass gives `KitchenFoodFrenzyMissionController` transparent gold-food and
purple-onion counter/floor warning sprites while preserving the existing telegraph timing,
controller-owned warning markers, and support labels.
The generated Chaos Machine prop pass gives `ChaosMachineMissionController` distinct transparent
towel-drop, basket-tip, and toy-launch station sprites while preserving the controller-owned markers,
owner labels, objective targets, cascade timing, and stall/re-pull behavior.
The dynamic treat-art stability pass keeps mission-specific collectible overlays active after the
older `DynamicTreatArtEnhancer` scan, so generated Snack Heist snack plates, Sock Panic socks, and
Blanket Catch falling snacks do not revert to Backyard weenie art.
The generated Building prop pass gives `BackyardRescueArtEnhancer` transparent home-exterior,
back-porch, and yard shed sprites while preserving the existing generated house/patio and back-door
layout objects as nonblocking anchors.
The generated HUD skin pass gives `ArenaHud` transparent couch-test panel, tile, badge, button, and
overlay sprites for mission select, briefing, pause, end-card, session summary, selected showcase,
playtest overlay, and debug toggle surfaces while preserving IMGUI text and hitboxes as the current
input layer.
The generated world-label skin pass gives `WorldLabelSkin` transparent bubble, command ribbon,
warning triangle, and score-pop burst sprites through the shared mission-label and score-pop paths
while preserving the existing `TextMesh` strings, positions, and controller-owned state.
The generated arena-SFX pass gives `ArenaFeedbackCatalog` named procedural dog-life profiles for the
major event cues while preserving replaceable cue slots, the F2 audio toggle, event-driven cue
requests, and the separate rumble request path.

## Sources and export

All exports come from the corresponding pose, prop, backyard prop, or VFX sheets in `DRAFT assets/`. The extraction tool at `tools/art/export_arena_final.py` removes edge-connected pale backgrounds, trims alpha bounds, adds 12 pixels of transparent padding, and never modifies source sheets. Run it from the repository root with the bundled/runtime Python or any Python installation with Pillow:

```sh
python3 tools/art/export_arena_final.py
```

Use `--inventory-only` to refresh only the source inventory, or `--contact-sheet <path>` to produce a review sheet outside the Unity asset tree.

Generated couch-test prop packs are deterministic and can be regenerated from the repository root:

```sh
python3 tools/art/generate_mission_prop_pack.py
python3 tools/art/generate_environment_prop_pack.py
python3 tools/art/generate_gameplay_cue_pack.py
python3 tools/art/generate_dog_fx_pack.py
python3 tools/art/generate_kitchen_cue_pack.py
python3 tools/art/generate_chaos_machine_prop_pack.py
python3 tools/art/generate_building_prop_pack.py
python3 tools/art/generate_hud_skin_pack.py
python3 tools/art/generate_world_label_skin_pack.py
```

## Deliberate skips and source limits

- UI sheets were inventoried but not exported; the integration pass prioritized Backyard Rescue
  readability. Further priorities come from the baseline couch playtest.
- Expression sheets remain reference-only because full-body pose sheets produce more readable gameplay silhouettes.
- `proud` and `rescued` currently use the same best available win pose for each dog.
- `rope_tug` and `rope_complete` currently use the same source drawing; the separate runtime paths preserve a clean future replacement point.
- Threat motion frames are derived from the approved transparent state sprites with small squash,
  offset, and tilt changes. They are intentionally first-test readable motion, not final hand-authored
  animation.
- Bunny sprites were easy to isolate and are included, but only the idle cameo is currently live.
- Generated gameplay objects remain authoritative for transforms and collision. When a final bush,
  rock, mission prop, or environment district overlay loads, the rectangular fallback renderer is
  hidden or dimmed; the fallback object remains available and is shown normally if the final sprite
  is missing. Sprite bounds do not affect gameplay.
- The mission, environment, gameplay cue, dog-FX, Kitchen cue, Chaos Machine prop, building prop, HUD
  skin, and world-label skin packs are generated cartoon PNGs for couch-test readability; they are
  not a final hand-authored art/UI/VFX pipeline.
- Generated arena SFX are procedural cue profiles, not authored recordings, final mix, or
  platform-tuned haptics.

## Rendered visual review

The macOS release player was reviewed at a 1920x1080 camera target in local gameplay, bark, warning, rescue, and full-yard states. That pass removed visible rectangular bush/rock fallbacks, tightened the bush extraction to remove adjacent-sheet fragments, reduced oversized bark/world-pop text, replaced all nine stepping-stone rectangles with varied final rocks, and verified distinct warning and rescue art.

The opt-in standalone review harness avoids macOS Screen Recording permissions. Pass
`--arena-art-review=/absolute/output/path` to the built player; it writes start/main/payoff PPM
frames for each roster mission and exits. The 2026-06-28 roster pass writes 66 frames for the 22
current missions. The harness is dormant during normal play and its argument parsing is covered by
PlayMode tests.

The latest generated review output is
`unity/builds/art-review-current/arena-art-review-contact-sheet.jpg`. It was captured from the macOS
dev player in graphics-enabled batchmode after the Mission Prop Pack Pass 2 cleanup. That pass
confirmed nonblank captures, smaller gameplay juice overlays, quieter colored fallback pads behind
loaded mission props, and improved harness staging for Weenie Roundup, Walkies on the Leash, and the
Rube Goldberg/Chaos Machine junction beat.

A real two-player television session is still required to judge attention, dog foot contact during movement, and whether the warning/rescue scale feels appropriately loud at couch distance. The current rendered frames establish that assets are transparent, positioned, non-boxed, and readable at local and full-yard zoom.

The deterministic PlayMode coverage verifies resource loading, dog and threat pose/motion mapping,
live overlays, retained fallback objects/colliders, hidden loaded fallback renderers, marker fallback
states, dynamic treat art, VFX mapping/lifetime, capture argument safety, and safe missing-resource
fallback. Targeted evidence after the generated environment prop overlay pass-2 expansion:
`2/2` filtered PlayMode tests passing on 2026-06-29. Targeted evidence after the generated gameplay
cue pass: `2/2` filtered PlayMode tests passing on 2026-06-29. Targeted evidence after the generated
dog-FX pass: `6/6` filtered PlayMode tests passing on 2026-06-29. Targeted evidence after the
generated Kitchen cue pass: `5/5` Kitchen PlayMode tests, `1/1` final-art resource check, and `1/1`
mission-prop art contract test passing on 2026-06-29. Targeted evidence after the generated Chaos
Machine prop pass: `9/9` Chaos Machine PlayMode tests and `8/8` final-art integration tests passing
on 2026-06-29. Targeted evidence after the dynamic treat-art stability pass: `1/1` final-art
integration PlayMode test passing on 2026-06-29. Targeted evidence after the generated Building prop pass: `11/11` environment
PlayMode tests and `1/1` final-art resource check passing on 2026-06-29. Targeted evidence after
the generated HUD skin pass: `12/12` environment/HUD PlayMode tests and `1/1` final-art resource
check passing on 2026-06-29. Targeted evidence after the generated world-label skin pass: `13/13`
environment/world-label PlayMode tests and `1/1` final-art resource check passing on 2026-06-29.
Targeted evidence after the generated arena-SFX pass: `1/1` catalog profile PlayMode test and `1/1`
event-driven audio/rumble PlayMode test passing on 2026-06-29.
Targeted evidence after the generated P0 mission-state art pass: `8/8` final-art integration tests
passing on 2026-07-01, including resource loading and visible mission focus prop state coverage.
Full-suite evidence after the generated P0 mission-state art pass: `400/400` PlayMode tests passing
on 2026-07-01.
