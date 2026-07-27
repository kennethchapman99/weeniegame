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

Same turnaround contract, using one consistent dark-chocolate, long-low, long-haired miniature
dachshund with subtle warm-brown muzzle/eyebrow/lower-paw points and a clearly visible teal-blue
collar. Cocoa must read grounded, capable, and controlled. Avoid cream or white body markings,
black featureless fur, golden drift, missing collar, generic anatomy, and inconsistent angles.

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

The outcome boards use a 4x2 contract: stunned pair, rescued pair, proud pair, sad pair. `export_character_outcomes.py` promotes 16 east-facing frames for west mirroring. Cheddar's outcomes bounce and overreact; Cocoa's remain controlled and queenly. Detached stars/motion marks were explicitly removed before extraction so gameplay VFX stay authoritative.

The carry strips are prop-free 2x1 east-facing loops. `export_character_carry.py` promotes four frames while the existing dog-mounted weenie marker remains authoritative. Carry is persistent gameplay state: pickup begins it after the brief celebration, delivery/drop ends it, and mission restart clears it deterministically.

The V02 carry-direction boards extend that persistent state toward the camera and away from it. The
built-in image-generation workflow produced one 4x2 board per dog using the approved east carry,
diagonal idle, and straight idle boards as identity/camera references. Visual review accepted the
south and north two-frame pairs and rejected the diagonal row because its angles remained too close
to profile. `export_character_carry_straights.py` therefore promotes only eight trustworthy 512x384
alpha frames. This is deliberate partial promotion: vertical travel now keeps the carried pose,
while diagonal travel retains the established east-facing fallback instead of shipping ambiguous
direction art.

The dig boards use a 4x2 east/south contract: nose-low anticipation, alternating forepaw rakes,
then a readable recovery. Cheddar's loop is gleeful and overcommitted; Cocoa's is planted and
methodical. Both boards are deliberately prop-free—Scent Search's live mound, bone, and dirt feedback
remain authoritative. `export_character_dig.py` promotes 16 frames, runtime mirrors east for west,
and `DogReadabilityFeedback.ShowDig` now makes successful and cold digs display the authored loop
before returning to normal locomotion.

## V02 sheet-locked storybook generation

On 2026-07-23 the built-in image-generation workflow produced neutral four-view model sheets for
both dogs. These V02 sheets supersede runtime frames and V01 boards as the identity authority while
retaining the owner portraits and pose sheets as the original source. The invariant matrix and
review order live in `CHARACTER-ART-MODEL-SHEETS.md`.

The first sheet-locked action is a four-pose east-facing run board for each dog. Prompts supplied the
V02 sheet as the hard identity reference and the original pose sheet as movement reference only.
Cheddar uses a contact, airborne stretch, compact landing, and push-off with loose head-led energy.
Cocoa uses a lower, controlled version of the same phases with no cream/white markings.
`export_storybook_run.py` produces eight 512×384 true-alpha runtime frames at baseline Y=360.

Cocoa's two bark and two grooming frames were also regenerated in this workflow after contact-sheet
review found noncanonical cream drift. Her V02 sheet was authoritative; the previous frames supplied
pose only. Chroma-key removal used the imagegen helper with a soft matte and despill before the bark
and grooming normalizers aligned the corrected cutouts.

## V03 jump, wrestle, and accepted-interact generation

On 2026-07-26 the built-in image-generation workflow produced one strict 4x3 action board per dog,
using each dog's V02 model sheet as the hard identity reference. Rows are jump, wrestle/play, and
generic accepted Interact; each row contains anticipation, action, impact, and recovery. Cheddar's
board uses loose head-led overcommit, while Cocoa's keeps planted, controlled arcs and preserves her
uniform chocolate coat and teal collar without cream/white drift.

The source boards use a flat magenta key. The shared imagegen chroma-removal helper produced reviewed
true-alpha boards. The first contact sheet caught two detached Cheddar generation specks; the
exporter now removes only disconnected components smaller than two percent of the primary
silhouette, then normalizes all 24 frames to 512x384 with baseline Y=360. Runtime east frames live
under each dog's `Motion/` folder and mirror west. `tools/art/export_character_jump_wrestle_interact.py`
rebuilds the frames and `captures/character-motion-jump-wrestle-interact-v03.png` is the promotion
contact sheet.

Runtime synchronization is action-specific: jump chooses one of four frames from
`DogController.JumpProgress01`; wrestle and Interact start their one-shot clocks when the pose is
forced and hold the recovery frame instead of looping. Generic accepted Interact never replaces
specific dig, sniff, or tug acting.
