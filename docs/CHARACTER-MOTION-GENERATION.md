# Character Motion Generation Record

Generated on 2026-06-19 with the built-in image-generation tool. These outputs are AI-assisted reference exploration, owned by this project, and are not approved runtime sprites.

## Inputs

- Cheddar: current `cheddar_idle.png`, draft Cheddar identity portrait, and draft pose sheet.
- Cocoa: current `cocoa_idle.png`, draft Cocoa identity portrait, and draft pose sheet.
- Each action board also used its generated turnaround as the authoritative identity reference.

## Final prompt specs

### Cheddar turnaround

Use case `stylized-concept`; reference-only 2.5D game character turnaround. Generate one consistent golden, long-low, long-haired miniature dachshund with cream chest and red-orange collar in eight isolated directions, strict 4x2 order `E, SE, S, SW, W, NW, N, NE`; identical volume, scale, paw baseline, camera elevation, soft daylight, and small down-right shadow. Match the current runtime illustration. No text, labels, arrows, dividers, props, extra dogs, clipped features, or watermark; request true transparency.

### Cocoa turnaround

Same turnaround contract, using one consistent dark-chocolate, long-low, long-haired miniature dachshund with subtle cream/spot identity and clearly visible teal-blue collar. Cocoa must read grounded, capable, and controlled. Avoid black featureless fur, golden drift, missing collar, generic anatomy, and inconsistent angles.

### Cheddar action key poses

Use case `stylized-concept`; strict 4x3 action board preserving the approved turnaround identity. Reading order: chaos run, explosive bark, overcommitted tug, enthusiastic dig, sock carry, low herd chase, crouched hide/peek, warm comfort lean, funny stunned compression, hopeful rescued hop, bouncing proud victory, recoverable sad flop. Keep scale/camera consistent; use only a short rope or one sock where required; request true transparency and no labels/dividers.

### Cocoa action key poses

Same 4x3 action order, but motion language is purposeful run, authoritative bark, planted tug, controlled dig, deliberate carry/herd, composed hide/comfort, annoyed stunned, relieved rescue, royal proud, dignified sad flop. Preserve dark-chocolate fur and teal collar with grounded, queenly timing.

## Validation result

- Four requested boards generated at 1536x1024 or 1774x887.
- Character identity, collar differentiation, and most action silhouettes are strong enough for review.
- All four files are RGB with a baked checkerboard; `validate_character_motion_pack.py --require-boards` rejects them because they lack true alpha.
- Turnaround direction order is suggestive rather than production-certified. Do not auto-slice.
- Cheddar's action board is the stronger motion-language reference. Cocoa's hide includes a bush and comfort reads as a composed sit; those cells need targeted regeneration if selected for production.

Next generation should be clip-by-clip after human board approval, one direction and animation strip at a time. This reduces identity drift and makes transparent extraction, frame spacing, and paw-baseline validation tractable.

## Tier-A locomotion generation

After the reference boards were approved, the built-in tool generated one 4x3 east-facing sheet per dog against a flat near-white background. Each sheet uses four idle frames, four run frames, and four bark frames. The Cheddar prompt emphasized settle/head-lift/tail-wag, airborne chaos-run phases, and explosive bark recovery. The Cocoa prompt used the same timing contract with planted landings, controlled tail motion, and an authoritative bark.

`tools/art/export_character_motion_tier_a.py` removes only edge-connected near-white background, excludes neighboring-cell bleed, applies one scale per dog sheet, places every frame on a 512x384 canvas with a common 24-pixel paw baseline, and exports 24 runtime PNGs. `--contact-sheet` produces a temporary visual QA board. The promoted files pass true-alpha validation; the RGB source sheets remain reference-only.

The directional run boards use strict 4x2 layouts: SE above NE, then S above N. `export_character_run_diagonals.py` promotes those into 32 additional frames. SW and NW are mirrored at runtime, completing eight-way traversal coverage without duplicating symmetric west-side art.

The east-facing tug strips contain brace, pull, and recovery frames with no baked rope prop. `export_character_tug.py` promotes six frames; gameplay mirrors them west and keeps the live rope actor visually authoritative. Cheddar uses a faster, airborne overcommit while Cocoa stays planted and controlled.

Directional bark uses matching SE/NE and S/N 4x2 boards. `export_character_bark_directions.py` promotes 32 frames; west-side angles mirror at runtime. Each direction preserves anticipation, open-mouth burst, recoil, and settle, while the separate bark ring remains the authoritative gameplay-radius read.

Directional idle uses the same SE/NE and S/N board contract. `export_character_idle_directions.py` promotes 32 subtle breathe/look/tail/settle frames. Cheddar's loop is eager and loose; Cocoa's is measured and composed. Combined with mirroring, idle, run, bark, and tug now have their complete Tier-A gameplay-facing coverage.
