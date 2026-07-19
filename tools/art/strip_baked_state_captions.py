#!/usr/bin/env python3
"""Strip baked-in text captions from the Home Trip / Coop Tricks state-pack sprites.

The one-shot generator that produced these files is gone from tools/art/ (see
docs/ASSET-PRODUCTION-CATALOG.md's V3.1 audit), so the fix operates on the shipped PNGs
directly. Every affected mission controller already surfaces the same (or better-worded)
state text through the shared SetCue/SpawnWorldPop/MissionActorFeedback path (verified per
controller before writing this script), so the baked caption is redundant and can simply be
erased rather than replaced.

Two techniques are needed:

1. Component strip (most files): each caption is a cluster of small connected components
   confined to the top of the 512x512 canvas, separated by a clear vertical gap from the
   real icon art below it. Erase any component whose bounding box never dips below
   CAPTION_CUTOFF_Y.

2. Top-arc patch (the human-bust family only): on 5 files the second caption line sits
   close enough to the head circle that individual letters touch its outline and get
   flood-filled into the same component as the head, so technique 1 leaves letter
   fragments behind. Those files share an identical head silhouette and fill color with a
   sibling state in the same mission that has only a single-line caption (clean after
   technique 1) - patch just the top of the canvas (well above where eyes/mouth/question
   marks are drawn, so each state's own expression survives) from that clean sibling.
"""

from __future__ import annotations

from pathlib import Path

from PIL import Image

ROOT = Path("unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props")

# Below this row every file in the component-strip list has its real icon art; above it is
# always either caption text or a small decorative accent glyph that belongs to the caption
# (e.g. the Switcheroo decoy's feint arrow). Verified per-file against connected-component
# bounding boxes before picking this value - see the V3.2 task notes.
CAPTION_CUTOFF_Y = 150

# The human-bust head circle's own apex sits at y=86 in every file of this sub-family (measured
# from the clean single-line siblings), and no state's facial feature (eyes, mouth, question
# mark) is drawn above y=140. 110 sits safely between "captions always end by here" and
# "expressions never start before here".
TOP_ARC_PATCH_Y = 110

COMPONENT_STRIP_FILES = [
    "GateCrash/gate_crash_gate_closed.png",
    "GateCrash/gate_crash_gate_held.png",
    "GateCrash/gate_crash_gate_snap.png",
    "GateCrash/gate_crash_toy_waiting.png",
    "GateCrash/gate_crash_toy_claimed.png",
    "TableStealth/table_stealth_human_watching.png",
    "TableStealth/table_stealth_human_spotted.png",
    "TableStealth/table_stealth_human_caught.png",
    "TableStealth/table_stealth_human_distracted.png",
    "TableStealth/table_stealth_steak_available.png",
    "TableStealth/table_stealth_steak_sneak_progress.png",
    "TableStealth/table_stealth_steak_gone.png",
    "SquirrelSwitcheroo/switcheroo_decoy_guarded.png",
    "SquirrelSwitcheroo/switcheroo_decoy_chased.png",
    "SquirrelSwitcheroo/switcheroo_decoy_backfire.png",
    "SquirrelSwitcheroo/switcheroo_stash_guarded.png",
    "SquirrelSwitcheroo/switcheroo_stash_open.png",
    "SquirrelSwitcheroo/switcheroo_stash_raided.png",
    "WalkCampaign/walk_campaign_human_confused.png",
    "WalkCampaign/walk_campaign_human_getting_it.png",
    "WalkCampaign/walk_campaign_human_misread.png",
    "WalkCampaign/walk_campaign_human_walkies.png",
    "WalkCampaign/walk_campaign_human_gave_up.png",
    "WalkCampaign/walk_campaign_leash_waiting.png",
    "WalkCampaign/walk_campaign_leash_presented.png",
    "WalkCampaign/walk_campaign_leash_grabbed.png",
    "BoneRelay/bone_relay_scent_post_idle.png",
    "BoneRelay/bone_relay_scent_post_called.png",
    "BoneRelay/bone_relay_mound_unknown.png",
    "BoneRelay/bone_relay_mound_called.png",
    "BoneRelay/bone_relay_mound_wrong.png",
    "BoneRelay/bone_relay_mound_found.png",
]

# (file needing a patch, clean sibling to patch from) - the sibling must be processed by the
# component strip above (in COMPONENT_STRIP_FILES) before its pixels are copied here.
TOP_ARC_PATCH_FILES = [
    ("TableStealth/table_stealth_human_distracted.png", "TableStealth/table_stealth_human_caught.png"),
    ("TableStealth/table_stealth_human_watching.png", "TableStealth/table_stealth_human_caught.png"),
    ("WalkCampaign/walk_campaign_human_confused.png", "WalkCampaign/walk_campaign_human_gave_up.png"),
    ("WalkCampaign/walk_campaign_human_getting_it.png", "WalkCampaign/walk_campaign_human_gave_up.png"),
    ("WalkCampaign/walk_campaign_human_misread.png", "WalkCampaign/walk_campaign_human_gave_up.png"),
]


def _connected_components(mask: list[list[bool]], w: int, h: int) -> list[tuple[int, int, list[tuple[int, int]]]]:
    """Returns (ymin, ymax, pixel coordinates) for each 4-connected opaque blob."""
    seen = [[False] * w for _ in range(h)]
    comps = []
    for sy in range(h):
        for sx in range(w):
            if seen[sy][sx] or not mask[sy][sx]:
                continue
            stack = [(sx, sy)]
            seen[sy][sx] = True
            pixels = []
            ymin = ymax = sy
            while stack:
                x, y = stack.pop()
                pixels.append((x, y))
                ymin, ymax = min(ymin, y), max(ymax, y)
                for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                    if 0 <= nx < w and 0 <= ny < h and not seen[ny][nx] and mask[ny][nx]:
                        seen[ny][nx] = True
                        stack.append((nx, ny))
            comps.append((ymin, ymax, pixels))
    return comps


def strip_caption(path: Path) -> tuple[int, int]:
    with Image.open(path) as img:
        img = img.convert("RGBA")
        w, h = img.size
        px = img.load()
        mask = [[px[x, y][3] > 10 for x in range(w)] for y in range(h)]
        comps = _connected_components(mask, w, h)

        icon_area_after = sum(len(pixels) for _, ymax, pixels in comps if ymax >= CAPTION_CUTOFF_Y)
        erased_area = 0
        for _ymin, ymax, pixels in comps:
            if ymax < CAPTION_CUTOFF_Y:
                erased_area += len(pixels)
                for x, y in pixels:
                    px[x, y] = (0, 0, 0, 0)

        assert icon_area_after > 0, f"{path}: nothing survived below the caption cutoff"
        img.save(path)
    return erased_area, icon_area_after


def patch_top_arc(path: Path, reference: Path) -> None:
    with Image.open(reference) as ref:
        ref = ref.convert("RGBA")
        ref_px = ref.load()
        w, _ = ref.size
    with Image.open(path) as img:
        img = img.convert("RGBA")
        px = img.load()
        for y in range(TOP_ARC_PATCH_Y):
            for x in range(w):
                px[x, y] = ref_px[x, y]
        img.save(path)


if __name__ == "__main__":
    total_erased = 0
    for rel in COMPONENT_STRIP_FILES:
        erased, kept = strip_caption(ROOT / rel)
        total_erased += erased
        print(f"strip {rel}: erased {erased}px caption, kept {kept}px icon")

    for rel, ref_rel in TOP_ARC_PATCH_FILES:
        patch_top_arc(ROOT / rel, ROOT / ref_rel)
        print(f"patch {rel}: top {TOP_ARC_PATCH_Y}px rows copied from {ref_rel}")

    print(f"\n{len(COMPONENT_STRIP_FILES)} files component-stripped, "
          f"{len(TOP_ARC_PATCH_FILES)} files top-arc patched, "
          f"{total_erased} caption pixels erased total")
