#!/usr/bin/env python3
"""Inventory draft art and export non-destructive transparent ArenaFinal sprites."""

from __future__ import annotations

import argparse
from collections import deque
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

from PIL import Image, ImageFilter


ROOT = Path(__file__).resolve().parents[2]
SOURCE_ROOTS = (
    ROOT / "DRAFT assets",
    ROOT / "unity/CheddarAndCocoa/Assets/Art/Resources/ArenaDraft",
    ROOT / "unity/CheddarAndCocoa/Assets/Art/ReferenceOnly/ArenaDraft",
)
OUTPUT_ROOT = ROOT / "unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal"
INVENTORY_PATH = ROOT / "docs/ART-SOURCE-INVENTORY.md"


@dataclass(frozen=True)
class Export:
    source: str
    output: str
    box: tuple[int, int, int, int]


# Boxes use Pillow's top-left coordinate convention and deliberately exclude sheet captions.
EXPORTS = (
    # Cheddar pose sheet: 3 + 3 + 2 layout.
    Export("cheddar - poses.png", "Characters/Dogs/Cheddar/cheddar_idle.png", (20, 0, 480, 345)),
    Export("cheddar - poses.png", "Characters/Dogs/Cheddar/cheddar_run.png", (510, 0, 950, 340)),
    Export("cheddar - poses.png", "Characters/Dogs/Cheddar/cheddar_bark.png", (990, 0, 1491, 350)),
    Export("cheddar - poses.png", "Characters/Dogs/Cheddar/cheddar_tug.png", (0, 370, 510, 690)),
    Export("cheddar - poses.png", "Characters/Dogs/Cheddar/cheddar_stunned.png", (780, 750, 1320, 1055)),
    Export("cheddar - poses.png", "Characters/Dogs/Cheddar/cheddar_rescued.png", (985, 370, 1491, 690)),
    Export("cheddar - poses.png", "Characters/Dogs/Cheddar/cheddar_proud.png", (985, 370, 1491, 690)),
    Export("cheddar - poses.png", "Characters/Dogs/Cheddar/cheddar_sad.png", (160, 680, 790, 1055)),
    # Cocoa pose sheet: labeled 4 x 2 layout.
    Export("cocoa - poses .png", "Characters/Dogs/Cocoa/cocoa_idle.png", (15, 80, 375, 445)),
    Export("cocoa - poses .png", "Characters/Dogs/Cocoa/cocoa_run.png", (410, 80, 725, 430)),
    Export("cocoa - poses .png", "Characters/Dogs/Cocoa/cocoa_bark.png", (750, 75, 1065, 430)),
    Export("cocoa - poses .png", "Characters/Dogs/Cocoa/cocoa_tug.png", (1080, 75, 1448, 430)),
    Export("cocoa - poses .png", "Characters/Dogs/Cocoa/cocoa_stunned.png", (1090, 500, 1448, 890)),
    Export("cocoa - poses .png", "Characters/Dogs/Cocoa/cocoa_rescued.png", (385, 500, 720, 890)),
    Export("cocoa - poses .png", "Characters/Dogs/Cocoa/cocoa_proud.png", (385, 500, 720, 890)),
    Export("cocoa - poses .png", "Characters/Dogs/Cocoa/cocoa_sad.png", (745, 500, 1080, 890)),
    # Mission characters.
    Export("squirrel poses.png", "Characters/Squirrel/squirrel_idle.png", (15, 70, 380, 500)),
    Export("squirrel poses.png", "Characters/Squirrel/squirrel_steal.png", (730, 70, 1040, 500)),
    Export("squirrel poses.png", "Characters/Squirrel/squirrel_scared.png", (730, 500, 1035, 940)),
    Export("eagle - poses.png", "Characters/Eagle/eagle_threat.png", (610, 80, 1122, 490)),
    Export("eagle - poses.png", "Characters/Eagle/eagle_action.png", (410, 470, 800, 930)),
    Export("coyote - poses.png", "Characters/Coyote/coyote_threat.png", (10, 735, 600, 1085)),
    Export("bunny - poses.png", "Characters/Bunny/bunny_idle.png", (120, 25, 520, 370)),
    Export("bunny - poses.png", "Characters/Bunny/bunny_scared.png", (540, 690, 1000, 1040)),
    # Mission props.
    Export("props.png", "Props/Mission/weenie_collectible.png", (20, 65, 550, 500)),
    Export("props.png", "Props/Mission/rope_tug.png", (0, 520, 440, 1045)),
    Export("props.png", "Props/Mission/rope_complete.png", (0, 520, 440, 1045)),
    Export("props.png", "Props/Mission/dog_bowl.png", (720, 540, 1120, 1020)),
    # Backyard props: 4 x 3 sheet.
    Export("backyard props.png", "Props/Backyard/grass_patch.png", (0, 20, 470, 320)),
    Export("backyard props.png", "Props/Backyard/bush.png", (475, 20, 805, 325)),
    Export("backyard props.png", "Props/Backyard/rock.png", (820, 50, 1060, 325)),
    Export("backyard props.png", "Props/Backyard/fence_section.png", (1070, 20, 1536, 345)),
    Export("backyard props.png", "Props/Backyard/dig_spot.png", (0, 370, 430, 670)),
    # VFX sheet: five top cells and six lower cells.
    Export("VFX assets.png", "VFX/bark_burst.png", (0, 70, 285, 415)),
    Export("VFX assets.png", "VFX/bark_ring.png", (300, 65, 610, 415)),
    Export("VFX assets.png", "VFX/success_pop.png", (620, 55, 915, 415)),
    Export("VFX assets.png", "VFX/warning_alert.png", (930, 55, 1210, 415)),
    Export("VFX assets.png", "VFX/fail_puff.png", (1220, 55, 1536, 415)),
    Export("VFX assets.png", "VFX/pickup_sparkle.png", (0, 560, 250, 820)),
    Export("VFX assets.png", "VFX/rescue_burst.png", (1270, 560, 1536, 820)),
)


def infer_subject(path: Path) -> str:
    name = path.name.lower()
    parts = {part.lower() for part in path.parts}
    for token, subject in (
        ("cheddar", "Cheddar"), ("cocoa", "Cocoa"), ("squirrel", "Squirrel"),
        ("eagle", "Eagle"), ("coyote", "Coyote"), ("bunny", "Bunny"),
        ("vfx", "VFX"), ("prop", "Props"),
    ):
        if token in name:
            return subject
    if "ui" in parts or name.startswith("ui_") or name.startswith("ui "):
        return "UI"
    return "Unknown"


def infer_type(path: Path) -> str:
    name = path.name.lower()
    if "pose" in name:
        return "pose"
    if "expression" in name:
        return "expression"
    if "vfx" in name:
        return "VFX sheet"
    if name.startswith("ui_") or name.startswith("ui "):
        return "UI sheet"
    if "prop" in name:
        return "prop sheet"
    if "portrait" in name or "reference" in name or path.suffix.lower() in {".jpg", ".jpeg"}:
        return "identity"
    if "character" in name or path.stem.lower() in {"squirrel", "eagle", "coyote", "bunny"}:
        return "identity"
    return "unknown"


def likely_outputs(subject: str, sheet_type: str) -> str:
    if subject in {"Cheddar", "Cocoa"} and sheet_type == "pose":
        return "idle, run, bark, tug, stunned/alert, rescued/proud, sad"
    if subject in {"Cheddar", "Cocoa"} and sheet_type == "expression":
        return "portrait reactions; optional state overlays"
    if subject == "Squirrel" and sheet_type in {"pose", "expression"}:
        return "idle, steal, scared"
    if subject == "Eagle" and sheet_type == "pose":
        return "threat, action"
    if subject == "Coyote" and sheet_type == "pose":
        return "threat"
    if subject == "Bunny" and sheet_type == "pose":
        return "idle, scared"
    if subject == "Props":
        return "mission collectibles or backyard landmarks"
    if subject == "VFX":
        return "bark, pickup, success, warning, rescue, fail effects"
    if subject == "UI":
        return "menu/HUD pieces (deferred)"
    return "reference only or manual review"


def suitable(subject: str, sheet_type: str) -> str:
    if sheet_type in {"pose", "expression", "prop sheet", "VFX sheet", "UI sheet"}:
        return "yes"
    if subject == "Squirrel" and sheet_type == "identity":
        return "limited"
    return "reference-only"


def image_files() -> Iterable[Path]:
    for root in SOURCE_ROOTS:
        if not root.exists():
            continue
        for path in sorted(root.rglob("*")):
            if path.suffix.lower() in {".png", ".jpg", ".jpeg"}:
                yield path


def write_inventory() -> None:
    rows = []
    for path in image_files():
        subject = infer_subject(path)
        sheet_type = infer_type(path)
        rows.append((path.relative_to(ROOT).as_posix(), subject, sheet_type,
                     suitable(subject, sheet_type), likely_outputs(subject, sheet_type)))

    lines = [
        "# Art Source Inventory", "",
        "Generated by `tools/art/export_arena_final.py --inventory-only`. Source sheets are never modified.", "",
        "| Source path | Subject | Sheet type | Runtime extraction | Likely outputs |",
        "| --- | --- | --- | --- | --- |",
    ]
    lines.extend(f"| `{path}` | {subject} | {kind} | {ok} | {outputs} |"
                 for path, subject, kind, ok, outputs in rows)
    lines.extend(["", f"Total source images: **{len(rows)}**.", ""])
    INVENTORY_PATH.write_text("\n".join(lines), encoding="utf-8")


def connected_background_mask(image: Image.Image) -> Image.Image:
    rgb = image.convert("RGB")
    width, height = rgb.size
    pixels = rgb.load()
    background = bytearray(width * height)
    queue: deque[tuple[int, int]] = deque()

    def candidate(x: int, y: int) -> bool:
        r, g, b = pixels[x, y]
        spread = max(r, g, b) - min(r, g, b)
        return min(r, g, b) >= 225 and spread <= 28

    def add(x: int, y: int) -> None:
        index = y * width + x
        if background[index] or not candidate(x, y):
            return
        background[index] = 255
        queue.append((x, y))

    for x in range(width):
        add(x, 0)
        add(x, height - 1)
    for y in range(height):
        add(0, y)
        add(width - 1, y)

    while queue:
        x, y = queue.popleft()
        if x > 0:
            add(x - 1, y)
        if x + 1 < width:
            add(x + 1, y)
        if y > 0:
            add(x, y - 1)
        if y + 1 < height:
            add(x, y + 1)

    return Image.frombytes("L", (width, height), bytes(background)).filter(ImageFilter.GaussianBlur(1.0))


def extract(export: Export) -> None:
    source = ROOT / "DRAFT assets" / export.source
    with Image.open(source) as sheet:
        crop = sheet.convert("RGBA").crop(export.box)
    background = connected_background_mask(crop)
    alpha = Image.eval(background, lambda value: 255 - value)
    crop.putalpha(alpha)
    bbox = alpha.point(lambda value: 255 if value > 12 else 0).getbbox()
    if bbox is None:
        raise RuntimeError(f"Extraction produced no visible pixels: {export.output}")
    crop = crop.crop(bbox)
    padded = Image.new("RGBA", (crop.width + 24, crop.height + 24), (0, 0, 0, 0))
    padded.alpha_composite(crop, (12, 12))
    output = OUTPUT_ROOT / export.output
    output.parent.mkdir(parents=True, exist_ok=True)
    padded.save(output, optimize=True)


def export_all() -> None:
    for export in EXPORTS:
        extract(export)


def write_contact_sheet(path: Path) -> None:
    sprites = sorted(OUTPUT_ROOT.rglob("*.png"))
    cell_width, cell_height = 220, 190
    columns = 5
    rows = (len(sprites) + columns - 1) // columns
    sheet = Image.new("RGB", (columns * cell_width, rows * cell_height), (42, 46, 54))
    for index, sprite_path in enumerate(sprites):
        with Image.open(sprite_path) as opened:
            sprite = opened.convert("RGBA")
        sprite.thumbnail((cell_width - 20, cell_height - 35), Image.Resampling.LANCZOS)
        tile = Image.new("RGBA", (cell_width, cell_height), (0, 0, 0, 0))
        tile.alpha_composite(sprite, ((cell_width - sprite.width) // 2, 6))
        sheet.paste(tile.convert("RGB"), ((index % columns) * cell_width, (index // columns) * cell_height))
    path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(path)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--inventory-only", action="store_true")
    parser.add_argument("--contact-sheet", type=Path)
    args = parser.parse_args()
    write_inventory()
    if not args.inventory_only:
        export_all()
        print(f"Exported {len(EXPORTS)} sprites to {OUTPUT_ROOT.relative_to(ROOT)}")
        if args.contact_sheet:
            write_contact_sheet(args.contact_sheet)
            print(f"Wrote contact sheet to {args.contact_sheet}")
    print(f"Inventoried sources in {INVENTORY_PATH.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
