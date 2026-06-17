# Cheddar & Cocoa Asset Catalog

This is the intake contract for future visual and audio assets made outside Unity. Do not import
final assets into `unity/CheddarAndCocoa` until a deliberate import pass is planned. Keep collected
or authored assets in the external drop folder first:

```text
/Users/kchapman/WeeniegameAssetDrop/
```

The current arena uses generated placeholders and named replacement slots, plus a first imported
DRAFT pass under `unity/CheddarAndCocoa/Assets/Art/Resources/ArenaDraft/`. This catalog describes
what to make, how to name it, and what "ready to import" means.

## Current Imported DRAFT Pass

The tracked `DRAFT assets/` files have been copied into the Unity project with normalized names and
Unity `.meta` files:

```text
unity/CheddarAndCocoa/Assets/Art/Resources/ArenaDraft/
  Characters/
    Dogs/
    Squirrel/
    Eagle/
    Coyote/
    Bunny/
  Props/Backyard/
  UI/
  VFX/
```

Unity imports these files as single sprites via `Assets/Scripts/Editor/ArenaDraftArtImporter.cs`.
Runtime access goes through `Assets/Scripts/Game/ArenaDraftArt.cs`.

Current status:

- Imported and visible in-game as draft badges/reference accents: Cheddar, Cocoa, squirrel, eagle,
  coyote, bunny, backyard props, UI kit, and VFX sheet.
- Still not production-ready: most files are full pose sheets, portraits, or reference-card images
  with backgrounds rather than individual transparent gameplay sprites.
- Keep the generated `ArenaArtCatalog` silhouettes as the primary gameplay read until the draft art
  is sliced/exported into final transparent assets.

Next art task:

- Export individual transparent sprites for each needed state (`cheddar_idle_01.png`,
  `cocoa_bark_01.png`, `squirrel_steal_01.png`, `mission_rope_tug_01.png`,
  `vfx_bark_burst_01.png`, etc.).
- Preserve the current folder/category organization and update this catalog when a draft badge is
  replaced by a final gameplay sprite.

## External Folder Structure

Recommended local structure:

```text
/Users/kchapman/WeeniegameAssetDrop/
  README.md
  tracking/
    asset-tracking.csv
  source/
    cheddar/
    cocoa/
    squirrel/
    backyard_props/
    mission_objects/
    feedback_vfx/
    ui/
    audio/
  export/
    sprites/
      cheddar/
      cocoa/
      squirrel/
      backyard_props/
      mission_objects/
      feedback_vfx/
      ui/
    audio/
      gameplay/
      ui/
  rejected_or_reference_only/
```

Use `source/` for layered PSD, Procreate, Figma, Blender, Logic, GarageBand, Audacity, or other
editable source files. Use `export/` for Unity-ready PNG/WAV files. Keep reference-only or rejected
material separate so it does not accidentally enter an import batch.

## Tracking Template

Use [asset-tracking-template.csv](asset-tracking-template.csv) as the starting tracker. Copy it to:

```text
/Users/kchapman/WeeniegameAssetDrop/tracking/asset-tracking.csv
```

Statuses should stay simple:

- `needed`
- `in_progress`
- `ready_for_review`
- `approved_for_import`
- `imported`
- `rejected`

## Naming Conventions

Use lowercase snake_case. Include the category/purpose, subject, state or variant, and two-digit
version number. Keep source files and exported files aligned by base name.

Visual examples:

- `cheddar_idle_01.png`
- `cheddar_bark_01.png`
- `cocoa_run_01.png`
- `squirrel_steal_01.png`
- `prop_sock_red_01.png`
- `mission_rope_tug_01.png`
- `vfx_bark_burst_01.png`
- `ui_button_replay_01.png`

Audio examples:

- `sfx_bark_cheddar_01.wav`
- `sfx_bark_cocoa_01.wav`
- `sfx_collect_snack_01.wav`
- `sfx_collect_sock_01.wav`
- `sfx_squirrel_steal_01.wav`
- `sfx_mission_win_01.wav`
- `ui_select_01.wav`

If an asset is a revision, increment the suffix: `_02`, `_03`, etc. Do not overwrite approved
exports unless the tracker notes why.

## Technical Specs

Visual assets:

- Export game-ready sprites as transparent PNG.
- Store editable source files separately under `source/`.
- Use a consistent 2.5D/top-down-three-quarter camera angle that matches `ArenaScene`.
- Keep shadow direction consistent, preferably soft down/right grounding shadows.
- Keep outline thickness consistent across a related set.
- Make shapes readable at current gameplay zoom, not only in close-up.
- Keep transparent bounds tight but leave enough padding for ears, tails, rope ends, bark bursts,
  and small animation motion.
- Avoid tiny details that disappear at small size.
- Prefer clear silhouette and color separation over realistic texture.

Audio assets:

- Store WAV source/export files, trimmed with clean starts and endings.
- Keep gameplay sounds short and game-ready; most cues should be under one second.
- Use cute, comic, toy-like, readable sounds.
- Do not use realistic aggressive dog sounds, harsh snarls, scary predator audio, or distress
  sounds.
- Barks should be funny and expressive, not annoying when repeated.
- Squirrel cues should feel mischievous.
- Win/fail cues should be playful; fail should be recoverable, not harsh.

Licensing:

- Only use assets Ken owns, made personally, commissioned with clear rights, or sourced under a
  license that allows use in this personal game.
- Track license/ownership in the CSV before marking an asset `approved_for_import`.
- Do not add paid pack assets, AI outputs, or web downloads without provenance notes.

## Needed Assets

### Cheddar

Visual:

| id | suggested filename | notes | import priority |
| --- | --- | --- | --- |
| cheddar_idle | `cheddar_idle_01.png` | Golden chaos puppy, long-low dachshund, red/orange collar. | high |
| cheddar_run | `cheddar_run_01.png` | Forward-leaning chaos zoom pose. | high |
| cheddar_bark | `cheddar_bark_01.png` | Explosive funny bark pose. | high |
| cheddar_tug | `cheddar_tug_01.png` | Overcommitted rope pull. | medium |
| cheddar_stunned | `cheddar_stunned_01.png` | Funny recoverable "oops" state. | medium |
| cheddar_rescued | `cheddar_rescued_01.png` | Warm recovery pop. | medium |
| cheddar_proud | `cheddar_proud_01.png` | Bouncy victory. | medium |
| cheddar_sad | `cheddar_sad_01.png` | Sad flop, not grim. | medium |

Audio:

| id | suggested filename | notes | import priority |
| --- | --- | --- | --- |
| sfx_bark_cheddar | `sfx_bark_cheddar_01.wav` | Bright, eager, comic bark. | high |

### Cocoa

Visual:

| id | suggested filename | notes | import priority |
| --- | --- | --- | --- |
| cocoa_idle | `cocoa_idle_01.png` | Chocolate spot queen, teal collar, steady posture. | high |
| cocoa_run | `cocoa_run_01.png` | Purposeful spot patrol. | high |
| cocoa_bark | `cocoa_bark_01.png` | Authoritative but warm bark pose. | high |
| cocoa_tug | `cocoa_tug_01.png` | Anchor-strength rope pull. | medium |
| cocoa_stunned | `cocoa_stunned_01.png` | "I warned you" danger state. | medium |
| cocoa_rescued | `cocoa_rescued_01.png` | Warm recovery pop. | medium |
| cocoa_proud | `cocoa_proud_01.png` | Royal satisfaction. | medium |
| cocoa_sad | `cocoa_sad_01.png` | Recoverable sad flop. | medium |

Audio:

| id | suggested filename | notes | import priority |
| --- | --- | --- | --- |
| sfx_bark_cocoa | `sfx_bark_cocoa_01.wav` | Lower, confident, queenly comic bark. | high |

### Squirrel

Visual:

| id | suggested filename | notes | import priority |
| --- | --- | --- | --- |
| squirrel_idle | `squirrel_idle_01.png` | Small readable thief silhouette. | high |
| squirrel_steal | `squirrel_steal_01.png` | Urgent stealing state. | high |
| squirrel_scared | `squirrel_scared_01.png` | Dropped-it reaction after bark. | medium |
| squirrel_escape | `squirrel_escape_01.png` | Smug escape/miss state. | medium |

Audio:

| id | suggested filename | notes | import priority |
| --- | --- | --- | --- |
| sfx_squirrel_warning | `sfx_squirrel_warning_01.wav` | Mischievous warning chirp/tap. | high |
| sfx_squirrel_steal | `sfx_squirrel_steal_01.wav` | Comic theft/miss cue. | high |

### Backyard Props

Visual:

| id | suggested filename | notes | import priority |
| --- | --- | --- | --- |
| prop_grass_tile | `prop_grass_tile_01.png` | Soft backyard ground read. | low |
| prop_fence | `prop_fence_01.png` | Human-sized backyard boundary. | low |
| prop_bush | `prop_bush_01.png` | Non-blocking background prop. | low |
| prop_shadow_blob | `prop_shadow_blob_01.png` | Grounding shadow style reference. | medium |

### Mission Objects

Visual:

| id | suggested filename | notes | import priority |
| --- | --- | --- | --- |
| mission_weenie | `mission_weenie_01.png` | Red/yellow readable food. | high |
| mission_snack | `mission_snack_01.png` | Forbidden snack, visually distinct from weenie. | high |
| prop_sock_red | `prop_sock_red_01.png` | Sock collectible, readable at small size. | high |
| prop_sock_blue | `prop_sock_blue_01.png` | Optional sock variant. | medium |
| mission_rope_tug | `mission_rope_tug_01.png` | Horizontal shared tug object with readable ends. | high |
| mission_predator_shadow | `mission_predator_shadow_01.png` | Warning shadow, threat pressure but not horror. | medium |

Audio:

| id | suggested filename | notes | import priority |
| --- | --- | --- | --- |
| sfx_collect_weenie | `sfx_collect_weenie_01.wav` | Small score/collect pop. | high |
| sfx_collect_snack | `sfx_collect_snack_01.wav` | Snack collect pop. | high |
| sfx_collect_sock | `sfx_collect_sock_01.wav` | Soft sock rescue pop. | high |
| sfx_tug_success | `sfx_tug_success_01.wav` | Satisfying teamwork pop. | medium |
| sfx_rescue_success | `sfx_rescue_success_01.wav` | Warm rescue cue. | medium |

### Feedback/VFX

Visual:

| id | suggested filename | notes | import priority |
| --- | --- | --- | --- |
| vfx_bark_ring | `vfx_bark_ring_01.png` | Expanding bark readability ring. | high |
| vfx_bark_burst | `vfx_bark_burst_01.png` | Comic bark burst. | high |
| vfx_score_pop_gain | `vfx_score_pop_gain_01.png` | Optional score gain accent. | low |
| vfx_score_pop_penalty | `vfx_score_pop_penalty_01.png` | Optional score penalty accent. | low |
| vfx_rescue_pop | `vfx_rescue_pop_01.png` | Warm recovery burst. | medium |
| vfx_tug_pop | `vfx_tug_pop_01.png` | Rope completion burst. | medium |
| vfx_warning_pulse | `vfx_warning_pulse_01.png` | Squirrel/predator warning readability. | medium |

Audio:

| id | suggested filename | notes | import priority |
| --- | --- | --- | --- |
| sfx_score_gain | `sfx_score_gain_01.wav` | Tiny positive score blip. | high |
| sfx_score_penalty | `sfx_score_penalty_01.wav` | Tiny penalty blip, not harsh. | high |
| sfx_mission_win | `sfx_mission_win_01.wav` | Playful win sting. | high |
| sfx_mission_fail | `sfx_mission_fail_01.wav` | Recoverable fail sting. | high |

### UI

Visual:

| id | suggested filename | notes | import priority |
| --- | --- | --- | --- |
| ui_button_replay | `ui_button_replay_01.png` | Optional future button/icon. | low |
| ui_button_next | `ui_button_next_01.png` | Optional future button/icon. | low |
| ui_button_mission_select | `ui_button_mission_select_01.png` | Optional future button/icon. | low |
| ui_star_paw | `ui_star_paw_01.png` | Optional paw/star rating marker. | medium |
| ui_mission_card_backyard | `ui_mission_card_backyard_01.png` | Optional mission picker art. | low |
| ui_mission_card_snack | `ui_mission_card_snack_01.png` | Optional mission picker art. | low |
| ui_mission_card_sock | `ui_mission_card_sock_01.png` | Optional mission picker art. | low |

### Gameplay Audio

These map to current placeholder cue slots in `ArenaFeedbackCatalog`.

| cue slot | suggested filename | notes | import priority |
| --- | --- | --- | --- |
| `bark` | `sfx_bark_cheddar_01.wav`, `sfx_bark_cocoa_01.wav` | Dog-specific variants can replace one generic cue later. | high |
| `tug_rescue_success` | `sfx_rescue_success_01.wav`, `sfx_tug_success_01.wav` | Current slot can split later if needed. | high |
| `snack_sock_collect` | `sfx_collect_snack_01.wav`, `sfx_collect_sock_01.wav` | Current slot can split by item later. | high |
| `squirrel_steal_miss` | `sfx_squirrel_steal_01.wav` | Mischievous theft/miss. | high |
| `score_gain` | `sfx_score_gain_01.wav` | Short, stackable. | high |
| `score_penalty` | `sfx_score_penalty_01.wav` | Short, readable, not mean. | high |
| `mission_win` | `sfx_mission_win_01.wav` | Playful success. | high |
| `mission_fail` | `sfx_mission_fail_01.wav` | Playful fail. | high |
| `threat_warning` | `sfx_warning_threat_01.wav` | Predator warning placeholder slot. | medium |

### UI Audio

| cue slot | suggested filename | notes | import priority |
| --- | --- | --- | --- |
| `ui_replay_next_select` | `ui_select_01.wav` | Small UI blip for mission select/replay/next. | high |
| future hover | `ui_hover_01.wav` | Optional if authored UI adds hover/focus. | low |
| future confirm | `ui_confirm_01.wav` | Optional if select/confirm split becomes useful. | low |

## Import Readiness Rules

Before any import pass, each asset should have:

- a tracker row with status `approved_for_import`;
- source/tool noted;
- license/ownership confirmed;
- exported PNG/WAV in the correct `export/` folder;
- source file stored in the matching `source/` folder;
- filename following this catalog;
- notes for intended slot or cue name;
- import priority set.

If any of those are missing, leave the asset outside Unity.
