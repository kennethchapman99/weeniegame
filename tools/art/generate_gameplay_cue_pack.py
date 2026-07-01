#!/usr/bin/env python3
"""Generate transparent cartoon gameplay cue sprites for ArenaFinal.

These replace text/generic geometry as the first visual read for objective arrows
and actionable bark/tug/rescue ranges while retaining existing text copy as
accessibility support.
"""

from __future__ import annotations

import math
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path("unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/UI/Cues")
REF_ROOT = Path("unity/CheddarAndCocoa/Assets/Art/ReferenceOnly/GeneratedGameplayCues")
SIZE = 512


def rgba(hex_value: str, alpha: int = 255) -> tuple[int, int, int, int]:
    hex_value = hex_value.lstrip("#")
    return tuple(int(hex_value[i : i + 2], 16) for i in (0, 2, 4)) + (alpha,)


def canvas() -> tuple[Image.Image, ImageDraw.ImageDraw]:
    image = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    return image, ImageDraw.Draw(image)


def shadow(draw: ImageDraw.ImageDraw, box=(112, 372, 400, 430), alpha=40):
    draw.ellipse(box, fill=(0, 0, 0, alpha))


def paw(draw: ImageDraw.ImageDraw, cx: int, cy: int, scale: float, fill, outline):
    r = int(34 * scale)
    draw.ellipse((cx - r, cy - r, cx + r, cy + r), fill=fill, outline=outline, width=max(3, int(7 * scale)))
    for ox, oy, tr in [(-38, -42, 16), (-12, -58, 15), (16, -58, 15), (42, -42, 16)]:
        rr = int(tr * scale)
        x = int(cx + ox * scale)
        y = int(cy + oy * scale)
        draw.ellipse((x - rr, y - rr, x + rr, y + rr), fill=fill, outline=outline, width=max(2, int(5 * scale)))


def objective_arrow(draw: ImageDraw.ImageDraw):
    shadow(draw, (132, 390, 380, 438), 34)
    draw.polygon(
        [(254, 62), (408, 244), (318, 244), (318, 372), (190, 372), (190, 244), (100, 244)],
        fill=rgba("#ffd84d"),
        outline=rgba("#5b3a10"),
    )
    draw.line((254, 86, 384, 238, 306, 238, 306, 356, 202, 356, 202, 238, 124, 238, 254, 86),
              fill=rgba("#fff4a8", 190), width=14)
    paw(draw, 254, 214, 0.82, rgba("#8b4f2a"), rgba("#3b2112"))
    draw.arc((124, 104, 388, 368), 210, 330, fill=rgba("#ffffff", 170), width=12)


def target_paw(draw: ImageDraw.ImageDraw):
    shadow(draw, (112, 378, 400, 432), 34)
    for r, color, alpha, width in [
        (178, "#46c6d8", 70, 18),
        (132, "#ffe05d", 100, 16),
        (88, "#ffffff", 125, 12),
    ]:
        draw.ellipse((256 - r, 256 - r, 256 + r, 256 + r), outline=rgba(color, alpha), width=width)
    paw(draw, 256, 284, 1.18, rgba("#f7d2a1"), rgba("#5b3a22"))
    draw.arc((132, 128, 380, 376), 200, 335, fill=rgba("#fff4a8", 170), width=13)


def ring_base(draw: ImageDraw.ImageDraw, ring_color: str, accent: str):
    for i, radius in enumerate([210, 174, 138]):
        alpha = [96, 118, 88][i]
        draw.ellipse((256 - radius, 256 - radius, 256 + radius, 256 + radius),
                     outline=rgba(ring_color, alpha), width=18)
    for angle in range(0, 360, 45):
        rad = math.radians(angle)
        x = 256 + math.cos(rad) * 204
        y = 256 + math.sin(rad) * 204
        draw.ellipse((x - 12, y - 12, x + 12, y + 12), fill=rgba(accent, 185), outline=rgba("#3b2a16"), width=3)


def bark_range(draw: ImageDraw.ImageDraw):
    ring_base(draw, "#ffe05d", "#ff8f4f")
    paw(draw, 202, 286, 0.72, rgba("#f1b36d"), rgba("#4a2b16"))
    paw(draw, 310, 286, 0.72, rgba("#c6844b"), rgba("#4a2b16"))
    for box in [(126, 126, 238, 218), (274, 126, 386, 218)]:
        draw.arc(box, 200, 340, fill=rgba("#fff4a8", 220), width=13)
        draw.arc((box[0] - 22, box[1] - 22, box[2] + 22, box[3] + 22), 205, 335, fill=rgba("#fff4a8", 150), width=9)
    draw.rounded_rectangle((184, 220, 328, 258), radius=18, fill=rgba("#ffcf4f", 210), outline=rgba("#5b3a10"), width=6)


def tug_range(draw: ImageDraw.ImageDraw):
    ring_base(draw, "#ffca3a", "#64d3df")
    draw.line((138, 278, 374, 226), fill=rgba("#4b3020"), width=30)
    for x, y, color in [(128, 282, "#f1b36d"), (384, 224, "#c6844b")]:
        paw(draw, x, y, 0.72, rgba(color), rgba("#4a2b16"))
    draw.arc((156, 164, 356, 344), 20, 160, fill=rgba("#fff4a8", 190), width=14)
    draw.arc((156, 164, 356, 344), 200, 340, fill=rgba("#fff4a8", 190), width=14)


def rescue_range(draw: ImageDraw.ImageDraw):
    ring_base(draw, "#79f2a8", "#ffe05d")
    paw(draw, 256, 292, 0.98, rgba("#f2c28a"), rgba("#4a2b16"))
    draw.polygon([(256, 108), (318, 208), (274, 208), (274, 254), (238, 254), (238, 208), (194, 208)],
                 fill=rgba("#79f2a8", 225), outline=rgba("#245334"))
    draw.arc((128, 118, 384, 374), 210, 330, fill=rgba("#ffffff", 160), width=12)


DRAWERS = {
    "cue_bark_range": bark_range,
    "cue_objective_arrow": objective_arrow,
    "cue_rescue_range": rescue_range,
    "cue_target_paw": target_paw,
    "cue_tug_range": tug_range,
}


def write_readme():
    REF_ROOT.mkdir(parents=True, exist_ok=True)
    names = "\n".join(f"- `{name}.png`" for name in sorted(DRAWERS))
    (REF_ROOT / "README.md").write_text(
        "# Generated Gameplay Cue Pack\n\n"
        "Deterministic transparent cartoon UI/gameplay cue sprites generated by "
        "`tools/art/generate_gameplay_cue_pack.py`. Runtime code uses these for objective "
        "arrows and actionable bark/tug/rescue range indicators while retaining text labels as "
        "support copy. These are couch-test cue assets, not a final hand-authored UI pipeline.\n\n"
        f"{names}\n",
        encoding="utf-8",
    )


def write_contact_sheet(images: list[tuple[str, Image.Image]]):
    REF_ROOT.mkdir(parents=True, exist_ok=True)
    cell = 256
    sheet = Image.new("RGBA", (cell * len(images), cell), (245, 240, 226, 255))
    for i, (name, image) in enumerate(images):
        thumb = image.resize((cell, cell), Image.Resampling.LANCZOS)
        sheet.alpha_composite(thumb, (i * cell, 0))
    sheet.convert("RGB").save(REF_ROOT / "gameplay_cue_pack_contact_sheet.png", quality=95)


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
    print(f"Generated {len(generated)} gameplay cue sprites in {ROOT}")


if __name__ == "__main__":
    main()
