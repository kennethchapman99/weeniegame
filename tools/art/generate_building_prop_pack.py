#!/usr/bin/env python3
"""Generate transparent building-exterior sprites for ArenaFinal.

These sprites give the backyard house cluster a real illustrated first read
without changing any Unity gameplay objects, mission markers, or colliders.
"""

from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path("unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props/Buildings")
REF_ROOT = Path("unity/CheddarAndCocoa/Assets/Art/ReferenceOnly/GeneratedBuildingProps")
SIZE = 512


def rgba(hex_value: str, alpha: int = 255) -> tuple[int, int, int, int]:
    hex_value = hex_value.lstrip("#")
    return tuple(int(hex_value[i : i + 2], 16) for i in (0, 2, 4)) + (alpha,)


def canvas() -> tuple[Image.Image, ImageDraw.ImageDraw]:
    image = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    return image, ImageDraw.Draw(image)


def shadow(draw: ImageDraw.ImageDraw, box=(46, 392, 466, 456), alpha=34):
    draw.ellipse(box, fill=(0, 0, 0, alpha))


def siding(draw: ImageDraw.ImageDraw, x0: int, y0: int, x1: int, y1: int):
    for y in range(y0 + 28, y1 - 8, 34):
        draw.line((x0 + 10, y, x1 - 10, y + 5), fill=rgba("#7e9aa5", 95), width=5)


def window(draw: ImageDraw.ImageDraw, xy, glow="#ffe6a0"):
    draw.rounded_rectangle(xy, radius=14, fill=rgba(glow, 230), outline=rgba("#39515e"), width=7)
    x0, y0, x1, y1 = xy
    cx = (x0 + x1) / 2
    cy = (y0 + y1) / 2
    draw.line((cx, y0 + 8, cx, y1 - 8), fill=rgba("#39515e"), width=5)
    draw.line((x0 + 8, cy, x1 - 8, cy), fill=rgba("#39515e"), width=5)
    draw.arc((x0 - 18, y0 - 16, x1 + 18, y1 + 22), 200, 340, fill=rgba("#fff6c8", 130), width=5)


def roof(draw: ImageDraw.ImageDraw, points, fill="#74452c"):
    draw.polygon(points, fill=rgba(fill), outline=rgba("#321c13"))
    draw.line((points[0][0] + 18, points[0][1] + 10, points[-1][0] - 18, points[-1][1] + 12),
              fill=rgba("#2e1a12"), width=8)


def home_exterior_facade(draw: ImageDraw.ImageDraw):
    shadow(draw)
    draw.rounded_rectangle((74, 146, 438, 404), radius=26, fill=rgba("#b9d7dc", 238),
                           outline=rgba("#35505a"), width=10)
    siding(draw, 74, 146, 438, 404)
    roof(draw, [(52, 158), (256, 50), (462, 158), (430, 194), (256, 100), (82, 194)])
    draw.rounded_rectangle((106, 122, 164, 168), radius=8, fill=rgba("#65402a"),
                           outline=rgba("#321c13"), width=6)
    draw.rounded_rectangle((320, 112, 380, 162), radius=8, fill=rgba("#65402a"),
                           outline=rgba("#321c13"), width=6)
    window(draw, (112, 210, 204, 296))
    window(draw, (304, 204, 394, 292))
    draw.rounded_rectangle((218, 208, 292, 404), radius=18, fill=rgba("#7b4d31"),
                           outline=rgba("#311d12"), width=8)
    draw.rounded_rectangle((232, 232, 278, 292), radius=10, fill=rgba("#ffefb5", 210),
                           outline=rgba("#4a2d1d"), width=5)
    draw.ellipse((270, 308, 292, 330), fill=rgba("#f3d45f"), outline=rgba("#5a4216"), width=3)
    draw.rounded_rectangle((198, 404, 314, 438), radius=12, fill=rgba("#d9c5a1"),
                           outline=rgba("#5a4630"), width=6)
    draw.line((74, 408, 438, 408), fill=rgba("#5a4630"), width=8)
    draw.polygon([(132, 372), (186, 358), (206, 406), (144, 420)], fill=rgba("#e66a48"),
                 outline=rgba("#5a2c20"))
    draw.ellipse((352, 350, 414, 402), fill=rgba("#3f8e46"), outline=rgba("#1f4d25"), width=5)


def back_porch_entry(draw: ImageDraw.ImageDraw):
    shadow(draw, (84, 390, 428, 450), 36)
    draw.rounded_rectangle((138, 154, 374, 402), radius=24, fill=rgba("#c4d9d9", 235),
                           outline=rgba("#3c5960"), width=9)
    siding(draw, 138, 154, 374, 402)
    roof(draw, [(98, 166), (256, 74), (414, 166), (390, 202), (256, 120), (122, 202)],
         "#805136")
    for x in [150, 356]:
        draw.rounded_rectangle((x, 202, x + 26, 406), radius=10, fill=rgba("#8a5b36"),
                               outline=rgba("#3f2413"), width=5)
    draw.rounded_rectangle((190, 184, 322, 394), radius=20, fill=rgba("#72482e"),
                           outline=rgba("#321d12"), width=9)
    window(draw, (216, 214, 296, 292), "#ffe7a7")
    draw.ellipse((300, 302, 324, 326), fill=rgba("#f5d765"), outline=rgba("#5a4216"), width=4)
    draw.rounded_rectangle((156, 394, 356, 430), radius=13, fill=rgba("#d5c29b"),
                           outline=rgba("#5a4630"), width=6)
    draw.rounded_rectangle((132, 430, 380, 462), radius=12, fill=rgba("#b79f79"),
                           outline=rgba("#5a4630"), width=6)
    draw.arc((130, 310, 382, 492), 202, 338, fill=rgba("#ffd25d", 155), width=18)
    for x, y in [(118, 370), (394, 374)]:
        draw.ellipse((x - 28, y - 24, x + 28, y + 24), fill=rgba("#4a9a47"),
                     outline=rgba("#255326"), width=5)


def yard_shed_storage(draw: ImageDraw.ImageDraw):
    shadow(draw, (72, 394, 440, 456), 34)
    draw.rounded_rectangle((106, 174, 406, 410), radius=22, fill=rgba("#b97155", 238),
                           outline=rgba("#4d261b"), width=10)
    for x in range(126, 388, 42):
        draw.line((x, 188, x + 6, 402), fill=rgba("#79402d", 130), width=6)
    roof(draw, [(80, 176), (256, 82), (432, 176), (408, 210), (256, 132), (104, 210)],
         "#51465d")
    draw.rounded_rectangle((202, 238, 310, 410), radius=14, fill=rgba("#8a4c37"),
                           outline=rgba("#321b14"), width=8)
    draw.line((256, 242, 256, 408), fill=rgba("#321b14"), width=6)
    draw.ellipse((238, 320, 254, 336), fill=rgba("#f0cf57"), outline=rgba("#5a4216"), width=3)
    draw.ellipse((258, 320, 274, 336), fill=rgba("#f0cf57"), outline=rgba("#5a4216"), width=3)
    draw.rounded_rectangle((128, 232, 184, 292), radius=10, fill=rgba("#ffe9a8", 220),
                           outline=rgba("#4d261b"), width=6)
    draw.line((156, 236, 156, 288), fill=rgba("#4d261b"), width=4)
    draw.line((132, 262, 180, 262), fill=rgba("#4d261b"), width=4)
    draw.rounded_rectangle((326, 322, 392, 386), radius=16, fill=rgba("#d3ad63"),
                           outline=rgba("#5a391e"), width=6)
    draw.arc((330, 278, 390, 350), 196, 348, fill=rgba("#5a391e"), width=7)
    draw.polygon([(124, 372), (172, 356), (202, 410), (144, 422)], fill=rgba("#63b8d0", 230),
                 outline=rgba("#234f5c"))
    draw.ellipse((362, 156, 416, 206), fill=rgba("#3d8d42"), outline=rgba("#1f4d25"), width=5)


DRAWERS = {
    "back_porch_entry": back_porch_entry,
    "home_exterior_facade": home_exterior_facade,
    "yard_shed_storage": yard_shed_storage,
}


def write_readme():
    REF_ROOT.mkdir(parents=True, exist_ok=True)
    names = "\n".join(f"- `{name}.png`" for name in sorted(DRAWERS))
    (REF_ROOT / "README.md").write_text(
        "# Generated Building Prop Pack\n\n"
        "Deterministic transparent cartoon house and yard-building sprites generated by "
        "`tools/art/generate_building_prop_pack.py`. Runtime code layers these over the "
        "existing house/patio and back-door Unity objects so the playable yard reads as a "
        "home exterior instead of stacked geometric district markers. These are generated "
        "couch-test assets, not a final hand-authored building art pipeline.\n\n"
        f"{names}\n",
        encoding="utf-8",
    )


def write_contact_sheet(images: list[tuple[str, Image.Image]]):
    REF_ROOT.mkdir(parents=True, exist_ok=True)
    cell = 256
    sheet = Image.new("RGBA", (cell * 3, cell), (246, 240, 228, 255))
    for i, (_name, image) in enumerate(images):
        thumb = image.resize((cell, cell), Image.Resampling.LANCZOS)
        sheet.alpha_composite(thumb, (i * cell, 0))
    sheet.convert("RGB").save(REF_ROOT / "building_prop_pack_contact_sheet.png", quality=95)


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
    print(f"Generated {len(generated)} building sprites in {ROOT}")


if __name__ == "__main__":
    main()
