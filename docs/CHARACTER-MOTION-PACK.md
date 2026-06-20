# Cheddar & Cocoa Character Motion Pack

This is the production contract for replacing single-pose dog sprites with direction-aware motion while preserving Cheddar/Cocoa identity and deterministic Unity fallbacks.

## Staged workflow

1. Generate four reference-only boards: an eight-direction turnaround and an action-key-pose board for each dog.
2. Review silhouette, markings, collar color, camera angle, scale, and dog identity. Reference boards never load at runtime.
3. Approve one visual model per dog, then generate animation frames clip-by-clip from that approved model.
4. Export individual transparent PNGs using the runtime naming contract below.
5. Validate the pack, import through Unity, add animation selection, and retain the current single-pose fallback for every missing frame.

Generated exploration lives under `Assets/Art/ReferenceOnly/GeneratedCharacterMotion/`. Approved runtime frames belong under `Assets/Art/Resources/ArenaFinal/Characters/Dogs/<Dog>/Motion/`. Never overwrite the existing eight-pose ArenaFinal set while a motion pack is under review.

The initial four V01 boards are generated and intentionally remain reference-only. They establish a strong Cheddar/Cocoa identity and useful action silhouettes, but validation correctly rejects them as runtime sources because the generator baked a checkerboard into RGB output instead of producing true alpha. Direction ordering and Cocoa's comfort/hide props also need human review before any slicing.

Tier A idle/run/bark boards are generated separately against flat near-white backgrounds. The extraction pipeline exports normalized 512x384 true-alpha frames with a shared paw-baseline pivot. Live gameplay now has east idle/bark loops plus east, southeast, and northeast run loops; west-side travel mirrors them. Pure north/south chooses the closest diagonal, while missing non-run direction art and every Tier B clip retain the established single-pose fallback. Current completion is **40/336 frames**.

## Camera and direction contract

Directions are world-relative: `e`, `se`, `s`, `sw`, `w`, `nw`, `n`, `ne`. Every angle uses the same elevated three-quarter 2.5D gameplay camera, consistent body length, paw baseline, soft down-right grounding shadow, and centered body pivot. East/west may be mirrored only after asymmetric markings and collar reads have been checked.

At couch distance the order of importance is: long-low dachshund silhouette, facing/snouth direction, dog identity color, collar accent, current verb, then expression detail.

## Clip matrix

The machine-readable source is `tools/art/character_motion_manifest.json`; it currently plans 336 individual frames across both dogs.

| Tier | Clip | Directions | Frames/direction | Gameplay read |
| --- | --- | ---: | ---: | --- |
| A | idle | 8 | 4 | breathing/tail personality |
| A | run | 8 | 4 | traversal and facing |
| A | bark | 8 | 4 | anticipation, bark pop, settle |
| A | tug | E/W | 3 | brace, pull, recover |
| B | dig | E/S/W | 4 | sniff, paws, dirt kick, recover |
| B | carry | 8 | 2 | readable held-object silhouette |
| B | herd | E/SE/SW/W | 3 | low chase and cutoff pressure |
| B | hide | S/N | 2 | crouch and peek |
| B | comfort | E/W | 3 | approach, lean, settle |
| B | stunned/rescued/proud/sad | E/W | 2 each | outcome/emotion transitions |

Cheddar motion should overshoot, lead with the head, and use loose ears/tail. Cocoa motion should plant more firmly, carry cleaner arcs, and read as deliberate control. Their timing may differ; their collision footprint must not.

## Runtime naming

`<dog>_<clip>_<direction>_<frame>.png`, lowercase snake case with zero-based two-digit frames. Examples:

- `cheddar_run_se_02.png`
- `cocoa_bark_n_01.png`
- `cheddar_tug_w_00.png`
- `cocoa_comfort_e_02.png`

`CharacterMotionArt` constructs these paths and falls back to the current `FinalDogPoseArt` sprite when a frame is absent. Gameplay must never depend on a generated frame's pixel bounds.

## Review gates

- Transparent background with no sheet labels, dividers, neighboring poses, white halo, or clipped ears/tail.
- Same apparent body volume and paw baseline across angles.
- Cheddar remains golden with red/orange collar and eager forward energy.
- Cocoa remains dark chocolate with teal collar, cream/spot identity, and grounded posture.
- Snout direction is unambiguous in all eight angles.
- A 1920x1080 local-camera and full-yard capture confirms each motion remains readable.
- Human approval is required before a generated board is sliced or promoted into ArenaFinal runtime frames.

Run `python3 tools/art/validate_character_motion_pack.py --require-boards` after generating the source boards. Use `--require-runtime` only when an entire 336-frame pack is expected.
