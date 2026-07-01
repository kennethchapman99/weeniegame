#!/usr/bin/env python3
"""Generate transparent Chaos Machine junction props for ArenaFinal.

These replace samey generic junction art with action-specific Rube Goldberg
stations while preserving the controller-owned markers, labels, and timing.
"""

from __future__ import annotations

import math
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path("unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props/ChaosMachine")
REF_ROOT = Path("unity/CheddarAndCocoa/Assets/Art/ReferenceOnly/GeneratedChaosMachineProps")
SIZE = 512


def rgba(hex_value: str, alpha: int = 255) -> tuple[int, int, int, int]:
    hex_value = hex_value.lstrip("#")
    return tuple(int(hex_value[i : i + 2], 16) for i in (0, 2, 4)) + (alpha,)


def canvas() -> tuple[Image.Image, ImageDraw.ImageDraw]:
    image = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    return image, ImageDraw.Draw(image)


def shadow(draw: ImageDraw.ImageDraw, box=(78, 386, 434, 438), alpha=36):
    draw.ellipse(box, fill=(0, 0, 0, alpha))


def plank(draw: ImageDraw.ImageDraw, xy, fill="#8b5a2b"):
    draw.rounded_rectangle(xy, radius=16, fill=rgba(fill), outline=rgba("#3f2411"), width=7)
    x0, y0, x1, y1 = xy
    for x in range(int(x0) + 34, int(x1) - 20, 58):
        draw.line((x, y0 + 10, x + 20, y1 - 10), fill=rgba("#f0b36a", 90), width=5)


def gear(draw: ImageDraw.ImageDraw, cx: int, cy: int, r: int, fill="#d8b24c"):
    points = []
    for i in range(24):
        angle = -math.pi / 2 + i * math.pi * 2 / 24
        radius = r if i % 2 == 0 else int(r * 0.76)
        points.append((cx + math.cos(angle) * radius, cy + math.sin(angle) * radius))
    draw.polygon(points, fill=rgba(fill), outline=rgba("#4c3512"))
    draw.ellipse((cx - r * 0.48, cy - r * 0.48, cx + r * 0.48, cy + r * 0.48),
                 fill=rgba("#fff1a8"), outline=rgba("#4c3512"), width=5)
    draw.ellipse((cx - r * 0.18, cy - r * 0.18, cx + r * 0.18, cy + r * 0.18),
                 fill=rgba("#4c3512"))


def dog_paw(draw: ImageDraw.ImageDraw, cx: int, cy: int, scale: float, fill):
    r = int(22 * scale)
    draw.ellipse((cx - r, cy - r, cx + r, cy + r), fill=fill, outline=rgba("#3f2411"), width=4)
    for ox, oy, tr in [(-25, -26, 9), (-8, -35, 8), (10, -35, 8), (28, -26, 9)]:
        rr = int(tr * scale)
        x = int(cx + ox * scale)
        y = int(cy + oy * scale)
        draw.ellipse((x - rr, y - rr, x + rr, y + rr), fill=fill, outline=rgba("#3f2411"), width=3)


def rails(draw: ImageDraw.ImageDraw):
    plank(draw, (86, 356, 426, 392), "#77451f")
    draw.line((104, 346, 408, 346), fill=rgba("#3f2411"), width=10)
    for x in [130, 190, 250, 310, 370]:
        draw.line((x, 326, x + 26, 386), fill=rgba("#4c2a14"), width=8)


def towel_drop(draw: ImageDraw.ImageDraw):
    shadow(draw)
    rails(draw)
    draw.line((164, 106, 164, 362), fill=rgba("#4c2a14"), width=14)
    draw.line((348, 106, 348, 362), fill=rgba("#4c2a14"), width=14)
    plank(draw, (128, 112, 384, 146), "#8b5a2b")
    draw.rounded_rectangle((182, 132, 330, 306), radius=26, fill=rgba("#f4f0df"),
                           outline=rgba("#3b5960"), width=8)
    draw.arc((190, 150, 322, 292), 190, 340, fill=rgba("#79d4e6", 150), width=10)
    draw.line((208, 182, 304, 182), fill=rgba("#79d4e6", 170), width=9)
    draw.line((216, 230, 296, 230), fill=rgba("#ffcf4f", 160), width=8)
    draw.polygon([(256, 318), (224, 264), (288, 264)], fill=rgba("#ffcf4f", 220),
                 outline=rgba("#4c3512"))
    gear(draw, 124, 274, 42, "#ffcf4f")
    dog_paw(draw, 388, 294, 0.78, rgba("#63f1e6", 210))


def basket_tip(draw: ImageDraw.ImageDraw):
    shadow(draw)
    rails(draw)
    draw.line((150, 316, 366, 196), fill=rgba("#5d3419"), width=16)
    draw.rounded_rectangle((190, 176, 350, 294), radius=24, fill=rgba("#c78b4b"),
                           outline=rgba("#3f2411"), width=8)
    for x in [214, 250, 286, 322]:
        draw.line((x, 184, x - 28, 286), fill=rgba("#fff0bc", 115), width=6)
    for y in [210, 244, 278]:
        draw.line((196, y, 344, y - 36), fill=rgba("#fff0bc", 110), width=6)
    draw.arc((210, 138, 334, 230), 190, 350, fill=rgba("#3f2411"), width=9)
    for x, y, color in [(136, 220, "#79d4e6"), (166, 250, "#f4f0df"), (360, 310, "#ffcf4f")]:
        draw.rounded_rectangle((x - 24, y - 16, x + 28, y + 18), radius=10,
                               fill=rgba(color, 230), outline=rgba("#3f2411"), width=5)
    gear(draw, 128, 306, 40, "#79d4e6")
    dog_paw(draw, 394, 210, 0.78, rgba("#ffbf5a", 215))


def toy_launch(draw: ImageDraw.ImageDraw):
    shadow(draw)
    rails(draw)
    plank(draw, (142, 286, 360, 324), "#8b5a2b")
    draw.line((194, 286, 248, 178), fill=rgba("#4c2a14"), width=14)
    draw.line((316, 286, 248, 178), fill=rgba("#4c2a14"), width=14)
    draw.ellipse((216, 142, 280, 206), fill=rgba("#ffcf4f"), outline=rgba("#4c3512"), width=7)
    draw.line((248, 206, 248, 286), fill=rgba("#4c2a14"), width=12)
    draw.rounded_rectangle((146, 108, 260, 148), radius=20, fill=rgba("#ff7a51"),
                           outline=rgba("#4c2414"), width=7)
    draw.ellipse((126, 96, 176, 158), fill=rgba("#fff1a8"), outline=rgba("#4c2414"), width=6)
    draw.ellipse((232, 98, 286, 158), fill=rgba("#fff1a8"), outline=rgba("#4c2414"), width=6)
    for angle in [-35, -12, 18]:
        rad = math.radians(angle)
        x0, y0 = 316, 154
        x1 = x0 + math.cos(rad) * 92
        y1 = y0 + math.sin(rad) * 92
        draw.line((x0, y0, x1, y1), fill=rgba("#fff1a8", 180), width=9)
    gear(draw, 370, 296, 42, "#ff7a51")
    dog_paw(draw, 118, 282, 0.78, rgba("#63f1e6", 210))


DRAWERS = {
    "chaos_junction_basket_tip": basket_tip,
    "chaos_junction_towel_drop": towel_drop,
    "chaos_junction_toy_launch": toy_launch,
}


def write_readme():
    REF_ROOT.mkdir(parents=True, exist_ok=True)
    names = "\n".join(f"- `{name}.png`" for name in sorted(DRAWERS))
    (REF_ROOT / "README.md").write_text(
        "# Generated Chaos Machine Prop Pack\n\n"
        "Deterministic transparent cartoon Rube Goldberg junction props generated by "
        "`tools/art/generate_chaos_machine_prop_pack.py`. Runtime code uses these for the "
        "towel-drop, basket-tip, and toy-launch junctions while retaining controller-owned labels, "
        "range markers, and cascade timing. These are couch-test props, not a final hand-authored "
        "prop/animation pipeline.\n\n"
        f"{names}\n",
        encoding="utf-8",
    )


def write_contact_sheet(images: list[tuple[str, Image.Image]]):
    REF_ROOT.mkdir(parents=True, exist_ok=True)
    cell = 256
    sheet = Image.new("RGBA", (cell * 3, cell), (245, 240, 226, 255))
    for i, (name, image) in enumerate(images):
        thumb = image.resize((cell, cell), Image.Resampling.LANCZOS)
        sheet.alpha_composite(thumb, (i * cell, 0))
    sheet.convert("RGB").save(REF_ROOT / "chaos_machine_prop_pack_contact_sheet.png", quality=95)


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
    print(f"Generated {len(generated)} Chaos Machine prop sprites in {ROOT}")


if __name__ == "__main__":
    main()
