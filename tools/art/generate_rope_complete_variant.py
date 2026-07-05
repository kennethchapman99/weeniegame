#!/usr/bin/env python3
"""Derive a visually distinct "rope complete" sprite from the exported rope_tug sprite.

`export_arena_final.py`'s EXPORTS table crops rope_tug.png and rope_complete.png from the
identical box on the source sheet (there is only one rope illustration on props.png), so the
two files were byte-identical: BackyardRescueArtEnhancer swaps the rope overlay to
RopeComplete on tug clear, but the prop looked exactly the same before and after the payoff
(docs/ARENA-PLAYABLE.md "Final polish still needed: rope tug/complete source poses").

Run this after export_arena_final.py (it reads that script's rope_tug.png output). It does not
touch the source sheet or the crop box; it only re-derives the complete variant with a golden
success tint and a couple of sparkle accents, matching the existing success/gold VFX language
(e.g. BackyardRescueArtEnhancer's TugTogether pop color).
"""

from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageEnhance

ROOT = Path(__file__).resolve().parents[2]
PROP_DIR = ROOT / "unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props/Mission"
SOURCE = PROP_DIR / "rope_tug.png"
OUTPUT = PROP_DIR / "rope_complete.png"

GOLD_TINT = (255, 214, 89)


def sparkle(draw: ImageDraw.ImageDraw, cx: int, cy: int, size: int) -> None:
    """A small 4-point sparkle, matching the success/celebration accents used elsewhere."""
    draw.polygon(
        [(cx, cy - size), (cx + size // 3, cy - size // 3), (cx + size, cy),
         (cx + size // 3, cy + size // 3), (cx, cy + size), (cx - size // 3, cy + size // 3),
         (cx - size, cy), (cx - size // 3, cy - size // 3)],
        fill=(255, 246, 214, 235),
    )


def main() -> None:
    with Image.open(SOURCE) as opened:
        rope = opened.convert("RGBA")

    alpha = rope.getchannel("A")
    tinted = ImageEnhance.Color(rope.convert("RGB")).enhance(1.35)
    tinted = ImageEnhance.Brightness(tinted).enhance(1.12)
    gold = Image.new("RGB", rope.size, GOLD_TINT)
    warmed = Image.blend(tinted, gold, 0.22)
    warmed = warmed.convert("RGBA")
    warmed.putalpha(alpha)

    draw = ImageDraw.Draw(warmed)
    width, height = warmed.size
    sparkle(draw, round(width * 0.14), round(height * 0.16), 20)
    sparkle(draw, round(width * 0.86), round(height * 0.22), 15)
    sparkle(draw, round(width * 0.78), round(height * 0.82), 17)

    warmed.save(OUTPUT, optimize=True)
    print(f"Wrote {OUTPUT.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
