#!/usr/bin/env python3
"""Normalize Operation Pee Break generated prop sprites to the ArenaFinal 512px contract.

The original generated Pee Break atlas exports were square but smaller than the rest of the
mission-state packs. This script pads them onto transparent 512x512 canvases without scaling the
art, preserving the authored silhouettes while making the resource dimensions consistent.
"""

from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path("unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props/PeeBreak")
REF_ROOT = Path("unity/CheddarAndCocoa/Assets/Art/ReferenceOnly/GeneratedPeeBreakProps")
SIZE = 512
NAMES = [
    "pee_break_bladder_meter",
    "pee_break_couch",
    "pee_break_hydrant_relief",
    "pee_break_leash",
    "pee_break_misread_tennis_ball",
    "pee_break_open_door",
    "pee_break_phone_charger",
    "pee_break_teenager",
]


def normalize(path: Path) -> Image.Image:
    image = Image.open(path).convert("RGBA")
    if image.size == (SIZE, SIZE):
        return image
    canvas = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    x = (SIZE - image.width) // 2
    y = (SIZE - image.height) // 2
    canvas.alpha_composite(image, (x, y))
    canvas.save(path)
    return canvas


def write_contact_sheet(images: list[tuple[str, Image.Image]]) -> None:
    REF_ROOT.mkdir(parents=True, exist_ok=True)
    cell = 256
    cols = 4
    rows = 2
    sheet = Image.new("RGBA", (cols * cell, rows * cell), (245, 240, 226, 255))
    draw = ImageDraw.Draw(sheet)
    for i, (name, image) in enumerate(images):
        x = (i % cols) * cell
        y = (i // cols) * cell
        thumb = image.resize((cell, cell), Image.Resampling.LANCZOS)
        sheet.alpha_composite(thumb, (x, y))
        draw.text((x + 8, y + cell - 20), name, fill=(45, 35, 25, 255))
    sheet.convert("RGB").save(REF_ROOT / "pee_break_prop_pack_contact_sheet.png", quality=95)


def write_readme() -> None:
    REF_ROOT.mkdir(parents=True, exist_ok=True)
    names = "\n".join(f"- `{name}.png` (`512x512`)" for name in NAMES)
    (REF_ROOT / "README.md").write_text(
        "# Generated Pee Break Prop Pack\n\n"
        "Operation Pee Break generated couch-test props normalized by "
        "`tools/art/normalize_pee_break_prop_pack.py` to the same transparent `512x512` "
        "resource contract used by the other mission-state packs. The source atlas remains "
        "`pee_break_props_atlas_source.png` for reference.\n\n"
        f"{names}\n",
        encoding="utf-8",
    )


def main() -> None:
    rendered: list[tuple[str, Image.Image]] = []
    for name in NAMES:
        path = ROOT / f"{name}.png"
        if not path.exists():
            raise FileNotFoundError(path)
        rendered.append((name, normalize(path)))
    write_contact_sheet(rendered)
    write_readme()
    print(f"Normalized {len(rendered)} Pee Break props to {SIZE}x{SIZE} in {ROOT}")


if __name__ == "__main__":
    main()
