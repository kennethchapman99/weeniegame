#!/usr/bin/env python3
"""Generate transparent Kitchen falling-food telegraph sprites for ArenaFinal.

These replace generic pulsing warning geometry with readable good/bad food callouts
for the Kitchen Falling Food Frenzy mission while preserving controller-owned
markers, timing, and collision behavior.
"""

from __future__ import annotations

import math
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path("unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/UI/KitchenCues")
REF_ROOT = Path("unity/CheddarAndCocoa/Assets/Art/ReferenceOnly/GeneratedKitchenCues")
SIZE = 512


def rgba(hex_value: str, alpha: int = 255) -> tuple[int, int, int, int]:
    hex_value = hex_value.lstrip("#")
    return tuple(int(hex_value[i : i + 2], 16) for i in (0, 2, 4)) + (alpha,)


def canvas() -> tuple[Image.Image, ImageDraw.ImageDraw]:
    image = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    return image, ImageDraw.Draw(image)


def burst(draw: ImageDraw.ImageDraw, cx: int, cy: int, outer: int, inner: int, fill, outline):
    points = []
    for i in range(24):
        angle = -math.pi / 2 + i * math.pi * 2 / 24
        radius = outer if i % 2 == 0 else inner
        points.append((cx + math.cos(angle) * radius, cy + math.sin(angle) * radius))
    draw.polygon(points, fill=fill, outline=outline)


def steam(draw: ImageDraw.ImageDraw, color):
    for x, phase in [(188, 0), (256, 34), (324, 68)]:
        points = []
        for t in range(22):
            y = 210 - t * 7
            wave = math.sin((t + phase) * 0.42) * 18
            points.append((x + wave, y))
        draw.line(points, fill=color, width=10, joint="curve")


def toast(draw: ImageDraw.ImageDraw, cx: int, cy: int, scale: float):
    w = int(132 * scale)
    h = int(112 * scale)
    left = cx - w // 2
    top = cy - h // 2
    draw.rounded_rectangle((left, top + 18, left + w, top + h), radius=int(24 * scale),
                           fill=rgba("#f6b45d"), outline=rgba("#5f3516"), width=max(4, int(7 * scale)))
    draw.pieslice((left, top - 8, left + w, top + 78), 180, 360,
                  fill=rgba("#ffd37d"), outline=rgba("#5f3516"), width=max(4, int(7 * scale)))
    draw.ellipse((cx - int(30 * scale), cy - int(6 * scale), cx + int(28 * scale), cy + int(30 * scale)),
                 fill=rgba("#ffef94", 210), outline=rgba("#9a5a1f"), width=max(2, int(4 * scale)))
    draw.arc((left + 20, top + 20, left + w - 20, top + h - 12), 202, 334,
             fill=rgba("#fff6bb", 170), width=max(4, int(8 * scale)))


def onion(draw: ImageDraw.ImageDraw, cx: int, cy: int, scale: float):
    r = int(58 * scale)
    draw.ellipse((cx - r, cy - r, cx + r, cy + r), fill=rgba("#b878d6"), outline=rgba("#4b245d"),
                 width=max(4, int(7 * scale)))
    draw.arc((cx - r + 18, cy - r + 16, cx + r - 16, cy + r - 12), 190, 340,
             fill=rgba("#f0cbff", 190), width=max(4, int(7 * scale)))
    draw.line((cx, cy - r, cx + int(34 * scale), cy - int(104 * scale)),
              fill=rgba("#5d8a39"), width=max(5, int(8 * scale)))
    draw.line((cx + int(10 * scale), cy - r + 4, cx - int(28 * scale), cy - int(96 * scale)),
              fill=rgba("#7abf4b"), width=max(4, int(7 * scale)))


def arrow_down(draw: ImageDraw.ImageDraw, color, outline):
    draw.polygon([(256, 356), (168, 252), (220, 252), (220, 82), (292, 82), (292, 252), (344, 252)],
                 fill=color, outline=outline)
    draw.line((256, 334, 194, 262, 234, 262, 234, 98, 278, 98, 278, 262, 318, 262, 256, 334),
              fill=rgba("#ffffff", 155), width=9)


def lane_marks(draw: ImageDraw.ImageDraw, color):
    for y in [94, 150, 206, 262, 318, 374]:
        draw.rounded_rectangle((232, y, 280, y + 26), radius=13, fill=color)


def gold_telegraph(draw: ImageDraw.ImageDraw):
    draw.ellipse((72, 378, 440, 438), fill=(0, 0, 0, 32))
    burst(draw, 256, 254, 184, 144, rgba("#ffd84d", 210), rgba("#6a3a12"))
    arrow_down(draw, rgba("#ffbd3f", 230), rgba("#5e3210"))
    steam(draw, rgba("#ffffff", 120))
    toast(draw, 256, 306, 1.0)
    draw.arc((82, 82, 430, 430), 208, 332, fill=rgba("#fff3a1", 170), width=15)


def purple_telegraph(draw: ImageDraw.ImageDraw):
    draw.ellipse((72, 378, 440, 438), fill=(0, 0, 0, 30))
    burst(draw, 256, 254, 184, 140, rgba("#b678df", 215), rgba("#4b245d"))
    arrow_down(draw, rgba("#9e5ec4", 232), rgba("#3d1c52"))
    lane_marks(draw, rgba("#fff2a1", 155))
    onion(draw, 256, 310, 1.12)
    for box in [(108, 120, 214, 226), (298, 120, 404, 226)]:
        draw.line((box[0] + 20, box[1] + 20, box[2] - 20, box[3] - 20),
                  fill=rgba("#fff1a0", 220), width=14)
        draw.line((box[2] - 20, box[1] + 20, box[0] + 20, box[3] - 20),
                  fill=rgba("#fff1a0", 220), width=14)


def landing_base(draw: ImageDraw.ImageDraw, ring: str, accent: str):
    for radius, alpha, width in [(206, 92, 18), (158, 122, 16), (108, 118, 12)]:
        draw.ellipse((256 - radius, 256 - radius, 256 + radius, 256 + radius),
                     outline=rgba(ring, alpha), width=width)
    for angle in range(0, 360, 30):
        rad = math.radians(angle)
        x = 256 + math.cos(rad) * 198
        y = 256 + math.sin(rad) * 198
        draw.ellipse((x - 10, y - 10, x + 10, y + 10),
                     fill=rgba(accent, 185), outline=rgba("#4a2b16"), width=3)
    draw.arc((104, 104, 408, 408), 210, 330, fill=rgba("#ffffff", 115), width=12)


def gold_landing(draw: ImageDraw.ImageDraw):
    landing_base(draw, "#ffda55", "#ff9f38")
    toast(draw, 256, 278, 0.86)
    draw.rounded_rectangle((184, 350, 328, 392), radius=21, fill=rgba("#79f2a8", 210),
                           outline=rgba("#245334"), width=6)
    draw.arc((184, 326, 328, 392), 185, 355, fill=rgba("#ffffff", 130), width=8)


def purple_landing(draw: ImageDraw.ImageDraw):
    landing_base(draw, "#b678df", "#fff06c")
    onion(draw, 256, 276, 0.92)
    draw.line((154, 152, 358, 360), fill=rgba("#fff1a0", 225), width=20)
    draw.line((358, 152, 154, 360), fill=rgba("#fff1a0", 225), width=20)
    draw.ellipse((176, 176, 336, 336), outline=rgba("#4b245d", 230), width=12)


DRAWERS = {
    "kitchen_landing_gold": gold_landing,
    "kitchen_landing_purple": purple_landing,
    "kitchen_telegraph_gold": gold_telegraph,
    "kitchen_telegraph_purple": purple_telegraph,
}


def write_readme():
    REF_ROOT.mkdir(parents=True, exist_ok=True)
    names = "\n".join(f"- `{name}.png`" for name in sorted(DRAWERS))
    (REF_ROOT / "README.md").write_text(
        "# Generated Kitchen Cue Pack\n\n"
        "Deterministic transparent cartoon Kitchen falling-food cue sprites generated by "
        "`tools/art/generate_kitchen_cue_pack.py`. Runtime code uses these for the counter "
        "pre-drop telegraph and floor landing warning so good food and onion dodges read before "
        "the item releases. Existing world labels remain as support copy.\n\n"
        f"{names}\n",
        encoding="utf-8",
    )


def write_contact_sheet(images: list[tuple[str, Image.Image]]):
    REF_ROOT.mkdir(parents=True, exist_ok=True)
    cell = 256
    sheet = Image.new("RGBA", (cell * 4, cell), (245, 240, 226, 255))
    for i, (name, image) in enumerate(images):
        thumb = image.resize((cell, cell), Image.Resampling.LANCZOS)
        sheet.alpha_composite(thumb, (i * cell, 0))
    sheet.convert("RGB").save(REF_ROOT / "kitchen_cue_pack_contact_sheet.png", quality=95)


def main():
    ROOT.mkdir(parents=True, exist_ok=True)
    generated: list[tuple[str, Image.Image]] = []
    for name, draw_fn in DRAWERS.items():
        image, draw = canvas()
        draw_fn(draw)
        image.save(ROOT / f"{name}.png")
        generated.append((name, image))
    write_readme()
    write_contact_sheet(generated)
    print(f"Generated {len(generated)} Kitchen cue sprites in {ROOT}")


if __name__ == "__main__":
    main()
