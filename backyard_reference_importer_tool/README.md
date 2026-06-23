# Backyard Map-Based Greybox Planner — Importer Tool

Unzip this into `/Users/kchapman/Weeniegame`, then double-click:

```text
run_backyard_import_mac.command
```

(or, from the repo root, `./rerun_backyard_greybox_planner_mac.command`)

Default inputs:

```text
MAPS - template/01_exports_flat                   numbered reference photos
MAPS - template/Backyard - Aerial.png             top-down aerial (layout underlay)
MAPS - template/01_exports_flat/export_map.HEIC   annotated capture-point map
```

The importer writes (it does **not** copy the photos into Unity):

```text
unity/CheddarAndCocoa/Assets/Reference/BackyardCapture/
  Map/Backyard_Aerial.png          (copied)
  Map/export_map.png               (converted from HEIC via sips)
  backyard_manifest.json / .md
  backyard_capture_points.json     (placement data; positions preserved on rerun)
  contact_sheets/*.html            (external, link to the original photos)
```

Then open Unity and run:

```text
Cheddar & Cocoa > Backyard > Build Map-Based Greybox Planner
```

…to build `Assets/Scenes/BackyardGreyboxPlannerScene.unity` (aerial underlay + editable greybox
markers, zones, and route/interaction candidates). Drag the `point_xxx` markers onto the map, then
run `Cheddar & Cocoa > Backyard > Save Capture Point Positions` to persist them.

Full pipeline doc: `docs/BACKYARD-CAPTURE-IMPORT.md`. No installs required beyond Python 3 and
macOS `sips` (for HEIC conversion).
