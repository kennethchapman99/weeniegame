# Backyard Rescue Art Integration Slice

## Live runtime art

`ArenaFinal` now contains 40 transparent, individually trimmed PNG sprites extracted from the existing draft sheets. The Backyard Rescue runtime prefers these assets while retaining every generated silhouette, collider, and safe fallback.

- Cheddar and Cocoa: idle, run, bark, tug, stunned, rescued, proud, and sad.
- Mission characters: squirrel idle/steal/scared, eagle threat/action, and coyote threat.
- Optional cameo: bunny idle/scared.
- Mission props: weenie collectible, tug rope, completed rope, and dog bowl.
- Backyard props: bush, fence section, rock, grass patch, and dig spot.
- VFX: bark burst/ring, pickup sparkle, success pop, warning alert, rescue burst, and fail puff.

The current live pass wires dog poses, squirrel state, eagle/coyote threat state, rope state, dynamic Backyard Rescue treats, selected backyard landmarks, and bark VFX. Other exported props and VFX have stable `FinalGameplayArt` paths ready for later feedback hooks.

## Sources and export

All exports come from the corresponding pose, prop, backyard prop, or VFX sheets in `DRAFT assets/`. The extraction tool at `tools/art/export_arena_final.py` removes edge-connected pale backgrounds, trims alpha bounds, adds 12 pixels of transparent padding, and never modifies source sheets. Run it from the repository root with the bundled/runtime Python or any Python installation with Pillow:

```sh
python3 tools/art/export_arena_final.py
```

Use `--inventory-only` to refresh only the source inventory, or `--contact-sheet <path>` to produce a review sheet outside the Unity asset tree.

## Deliberate skips and source limits

- UI sheets were inventoried but not exported; current priority is in-game Backyard Rescue readability.
- Expression sheets remain reference-only because full-body pose sheets produce more readable gameplay silhouettes.
- `proud` and `rescued` currently use the same best available win pose for each dog.
- `rope_tug` and `rope_complete` currently use the same source drawing; the separate runtime paths preserve a clean future replacement point.
- Bunny sprites were easy to isolate and are included, but only the idle cameo is currently live.
- Generated gameplay objects remain authoritative for transforms and collision. Sprite bounds do not affect gameplay.

## Human visual review still required

Review scale and pivot feel with two players on a television, especially dog foot contact, full-yard zoom readability, and overlays against bright yard areas. Check the soft pale edge left by background removal, sorting at fence/cover intersections, and the eagle/coyote scale during active threats. Capture local-camera, full-yard, and bark/rescue/warning frames after those tuning decisions.

The deterministic PlayMode coverage verifies resource loading, pose mapping, live overlays, retained colliders, dynamic treat art, and safe missing-resource fallback; it cannot judge composition or animation quality.
