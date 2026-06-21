# Backyard Rescue Art Integration Slice

> **Status: OPERATIONAL REFERENCE.** This records the current asset integration. Further art work
> is selected from critical couch-playtest findings; it does not supersede the active sequence in
> `NEXT-PRODUCTION-SLICE.md`.

## Live runtime art

`ArenaFinal` now contains 40 transparent, individually trimmed PNG sprites extracted from the existing draft sheets. The Backyard Rescue runtime prefers these assets while retaining every generated silhouette, collider, and safe fallback.

- Cheddar and Cocoa: idle, run, bark, tug, stunned, rescued, proud, and sad.
- Mission characters: squirrel idle/steal/scared, eagle threat/action, and coyote threat.
- Optional cameo: bunny idle/scared.
- Mission props: weenie collectible, tug rope, completed rope, and dog bowl.
- Backyard props: bush, fence section, rock, grass patch, and dig spot.
- VFX: bark burst/ring, pickup sparkle, success pop, warning alert, rescue burst, and fail puff.

The current live pass wires dog poses, squirrel state, eagle/coyote threat state, rope state, dynamic Backyard Rescue treats, backyard bushes/rocks, and bark VFX. `FinalJuiceEffect` now consumes the sequenced gameplay-feedback stream and displays pickup sparkle, success pop, warning alert, rescue burst, or fail puff without changing gameplay state. A newer event replaces the previous transient effect so rapid couch-play beats remain readable instead of stacking.

## Sources and export

All exports come from the corresponding pose, prop, backyard prop, or VFX sheets in `DRAFT assets/`. The extraction tool at `tools/art/export_arena_final.py` removes edge-connected pale backgrounds, trims alpha bounds, adds 12 pixels of transparent padding, and never modifies source sheets. Run it from the repository root with the bundled/runtime Python or any Python installation with Pillow:

```sh
python3 tools/art/export_arena_final.py
```

Use `--inventory-only` to refresh only the source inventory, or `--contact-sheet <path>` to produce a review sheet outside the Unity asset tree.

## Deliberate skips and source limits

- UI sheets were inventoried but not exported; the integration pass prioritized Backyard Rescue
  readability. Further priorities come from the baseline couch playtest.
- Expression sheets remain reference-only because full-body pose sheets produce more readable gameplay silhouettes.
- `proud` and `rescued` currently use the same best available win pose for each dog.
- `rope_tug` and `rope_complete` currently use the same source drawing; the separate runtime paths preserve a clean future replacement point.
- Bunny sprites were easy to isolate and are included, but only the idle cameo is currently live.
- Generated gameplay objects remain authoritative for transforms and collision. When a final bush or rock loads, only the rectangular fallback renderer is hidden; the fallback object remains available and is shown normally if the final sprite is missing. Sprite bounds do not affect gameplay.

## Rendered visual review

The macOS release player was reviewed at a 1920x1080 camera target in local gameplay, bark, warning, rescue, and full-yard states. That pass removed visible rectangular bush/rock fallbacks, tightened the bush extraction to remove adjacent-sheet fragments, reduced oversized bark/world-pop text, replaced all nine stepping-stone rectangles with varied final rocks, and verified distinct warning and rescue art.

The opt-in standalone review harness avoids macOS Screen Recording permissions. Pass `--arena-art-review=/absolute/output/path` to the built player; it writes five PPM frames and exits. The harness is dormant during normal play and its argument parsing is covered by PlayMode tests.

A real two-player television session is still required to judge attention, dog foot contact during movement, and whether the warning/rescue scale feels appropriately loud at couch distance. The current rendered frames establish that assets are transparent, positioned, non-boxed, and readable at local and full-yard zoom.

The deterministic PlayMode coverage verifies resource loading, pose mapping, live overlays, retained fallback objects/colliders, hidden loaded fallback renderers, dynamic treat art, VFX mapping/lifetime, capture argument safety, and safe missing-resource fallback.
