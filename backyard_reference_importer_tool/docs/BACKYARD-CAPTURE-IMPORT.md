# Backyard Capture Import

This tool now builds a **map-based greybox planner**, not a photo wall.

The aerial photo becomes the Unity map underlay, the annotated `export_map` shows which numbered
photo groups belong to which backyard area, and the numbered photos stay as external references.

Authoritative, up-to-date pipeline doc lives in the repo:

```text
docs/BACKYARD-CAPTURE-IMPORT.md
```

Quick run (from repo root):

```sh
./rerun_backyard_greybox_planner_mac.command
```

Then in Unity: `Cheddar & Cocoa > Backyard > Build Map-Based Greybox Planner`.
