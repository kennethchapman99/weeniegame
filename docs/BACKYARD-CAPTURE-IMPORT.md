# Backyard Capture Import — Map-Based Greybox Planner

This pipeline turns Ken's backyard capture into an **editable level-planning scene** in Unity.
It is **not** a photo wall. The aerial photo becomes the map; the numbered photos stay as
external references; Unity gets an editable greybox scaffold you place by hand.

> Supersedes the old "photo contact-board" scene. Reference photos are **no longer** copied into
> the Unity project or pasted into the scene as planes.

---

## The four inputs and what each is for

| Input | Path | Role |
|-------|------|------|
| **Aerial map** | `MAPS - template/Backyard - Aerial.png` | Top-down **layout underlay** in Unity |
| **Annotated map** | `MAPS - template/01_exports_flat/export_map.HEIC` | Shows **which numbered photo groups map to which backyard area** (capture-point annotation source) |
| **Numbered photos** | `MAPS - template/01_exports_flat/*.jpg` `*.png` | Per-point **reference photos** (dog-view + overview, by direction) |
| Unity project | `unity/CheddarAndCocoa` | Destination for the planner scene + map assets |

Photos are grouped by their **leading filename number** into capture points:

```
1_high.jpg, 1_dogview.jpg               -> point_001
4_dogview_left.png, 4_dogview_right.jpg -> point_004
```

Files without a leading number (e.g. `IMG_20260619_...jpg`) are indexed but left **ungrouped**.

---

## Run it (two steps)

### Step 1 — importer (copies maps, converts HEIC, indexes photos)

```bash
./rerun_backyard_greybox_planner_mac.command
```

Or directly:

```bash
python3 backyard_reference_importer_tool/tools/backyard_importer/import_backyard_reference.py \
  --source    "/Users/kchapman/Weeniegame/MAPS - template/01_exports_flat" \
  --aerial    "/Users/kchapman/Weeniegame/MAPS - template/Backyard - Aerial.png" \
  --annotated "/Users/kchapman/Weeniegame/MAPS - template/01_exports_flat/export_map.HEIC" \
  --unity     "/Users/kchapman/Weeniegame/unity/CheddarAndCocoa"
```

This:

- copies `Backyard - Aerial.png` → `Assets/Reference/BackyardCapture/Map/Backyard_Aerial.png`
- converts `export_map.HEIC` → `Assets/Reference/BackyardCapture/Map/export_map.png` (via `sips`)
- writes `backyard_manifest.json` / `.md`, `backyard_capture_points.json`
- writes external contact sheets to `Assets/Reference/BackyardCapture/contact_sheets/*.html`
  (these link to the **original** photos in `MAPS - template`, nothing is copied)

If HEIC conversion fails (non-macOS, or `sips` missing) the importer prints a manual step:
open `export_map.HEIC` in **Preview → File → Export → PNG** and save it to the `Map/` folder above.

### Step 2 — build the planner scene in Unity

```
Cheddar & Cocoa > Backyard > Build Map-Based Greybox Planner
```

Creates / overwrites `Assets/Scenes/BackyardGreyboxPlannerScene.unity`.

---

## The planner scene

```
BackyardGreyboxRoot
├── AerialMapUnderlay        flat top-down plane using Backyard_Aerial.png
├── AnnotatedMapReference    export_map.png plane (inactive — tick active to overlay)
├── CapturePoints            one draggable point_xxx marker per group
├── GreyboxZones             patio / deck / garden / tree / shrub / fence / open_yard blocks
├── RouteCandidates          dogview route, tunnel/secret, hide, predator-escape, squirrel-route
├── InteractiveCandidates    cover, fence_gap, dig_spot, bark_trigger, leash_checkpoint
├── ReferenceNotes           text labels (filenames + tags only — no photos)
├── PlannerCamera            orthographic, framed on the map
└── PlannerLight
```

Unplaced capture points start parked in a **staging grid west of the map**. Zone / route /
interactive starters start parked in **strips north of the map** — drag whichever you need onto
the aerial.

---

## Placing capture points (and keeping them)

Positions live in `backyard_capture_points.json` as normalized `map_position` coordinates
(`x`,`y` in 0..1, origin bottom-left) with a `placed` flag. There are two ways to set them:

1. **In Unity (recommended).** Drag `point_xxx` markers onto the aerial, then run
   `Cheddar & Cocoa > Backyard > Save Capture Point Positions`. This writes the dragged
   positions back to the JSON with `placed: true`.
2. **By hand.** Edit `map_position` in `backyard_capture_points.json` and set `placed: true`.

Re-running the importer **merges** — it preserves any `map_position` you already saved and only
adds new points. `Cheddar & Cocoa > Backyard > Create-Refresh Capture Point Markers` adds markers
for new points **without** moving markers you already placed.

> Note: the editor menu is `Create-Refresh …` (hyphen). A literal `/` in a Unity menu name makes a
> submenu, so the requested "Create/Refresh" is spelled with a hyphen.

---

## What gets written / what is safe to rebuild

| Path | Regenerated each run? |
|------|----------------------|
| `Assets/Reference/BackyardCapture/Map/Backyard_Aerial.png` | yes (copied) |
| `Assets/Reference/BackyardCapture/Map/export_map.png` | yes (converted) |
| `Assets/Reference/BackyardCapture/backyard_manifest.json` / `.md` | yes |
| `Assets/Reference/BackyardCapture/backyard_capture_points.json` | **merged** (positions preserved) |
| `Assets/Reference/BackyardCapture/contact_sheets/*.html` | yes |
| `Assets/Scenes/BackyardGreyboxPlannerScene.unity` | only when you run the menu |

> Do **not** delete the `Assets/Reference/BackyardCapture/` folder itself (Unity tracks its `.meta`
> files). Delete only the contents above if you need a clean rebuild.

This pipeline never touches `ArenaScene`, the playable missions, or any PlayMode tests.

---

## Known limitations

- **Capture-point positions are not auto-extracted** from `export_map.HEIC`. The annotated map is a
  human reference; markers import into the staging grid and you place them (then Save). Automatic
  position extraction (reading the printed numbers off the annotated map) is a possible later step.
- **Photo filenames carry view + direction only** (`N_dogview_left`), not feature keywords, so the
  importer's gameplay tags are currently just `dogview` / `overview` / `layout`. Rename photos with
  words like `fence`, `deck`, `tunnel`, `dig`, etc. to auto-tag gameplay candidates, or just place
  the starter zone/route/interactive blocks by hand.
- **Aerial underlay orientation** may read mirrored vs. standing in the yard. It is cosmetic — the
  marker save/load round-trip is internally consistent — but if it bothers you, set the
  `AerialMapUnderlay` transform `Scale X` negative.
- `export_map.png` is full-resolution (~20 MB). Unity downsamples it on import for display; it is
  editor-reference only and never ships in a build.

---

## Next step toward a playable backyard

Once capture points are placed and zones/routes are roughed in on the aerial:

1. Replace the flat greybox blocks with real 2.5D geometry (extrude zones to height).
2. Author the first co-op beat (e.g. one dog opens a fence gap / distracts, the other slips a
   route) using the placed route + interactive candidates.
3. Wire it through an `IMissionController` per `docs/ARCHITECTURE.md` — keep it a small, tested,
   shippable slice, not broad architecture.
