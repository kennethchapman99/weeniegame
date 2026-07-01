#!/usr/bin/env python3
"""Generate transparent cartoon environment sprites for ArenaFinal.

These sprites replace the first-read of broad backyard district rectangles while
leaving the Unity-generated objects, colliders, and gameplay markers intact.
"""

from __future__ import annotations

import math
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path("unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props/Environment")
REF_ROOT = Path("unity/CheddarAndCocoa/Assets/Art/ReferenceOnly/GeneratedEnvironmentProps")
SIZE = 512


def rgba(hex_value: str, alpha: int = 255) -> tuple[int, int, int, int]:
    hex_value = hex_value.lstrip("#")
    return tuple(int(hex_value[i : i + 2], 16) for i in (0, 2, 4)) + (alpha,)


def canvas() -> tuple[Image.Image, ImageDraw.ImageDraw]:
    image = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    return image, ImageDraw.Draw(image)


def shadow(draw: ImageDraw.ImageDraw, box=(62, 394, 450, 454), alpha=34):
    draw.ellipse(box, fill=(0, 0, 0, alpha))


def house_patio(draw: ImageDraw.ImageDraw):
    shadow(draw)
    draw.rounded_rectangle((74, 118, 438, 376), radius=34, fill=rgba("#b99f7b", 228), outline=rgba("#4f3a25"), width=12)
    for x in [122, 210, 298, 386]:
        draw.line((x, 132, x - 42, 366), fill=rgba("#8c745b", 165), width=8)
    for y in [180, 250, 320]:
        draw.line((88, y, 424, y + 16), fill=rgba("#8c745b", 165), width=8)
    draw.rounded_rectangle((294, 58, 414, 246), radius=20, fill=rgba("#744729"), outline=rgba("#3c2415"), width=10)
    draw.rounded_rectangle((322, 84, 388, 156), radius=10, fill=rgba("#ffe7a4", 210), outline=rgba("#6f4d20"), width=7)
    draw.ellipse((382, 156, 406, 180), fill=rgba("#f6d86d"), outline=rgba("#5c4216"), width=4)
    draw.rounded_rectangle((286, 248, 432, 288), radius=12, fill=rgba("#d9c8a8"), outline=rgba("#5c4a37"), width=7)
    draw.rounded_rectangle((114, 278, 254, 334), radius=16, fill=rgba("#e86545"), outline=rgba("#5c2b1e"), width=7)
    draw.text((132, 296), "GO", fill=rgba("#fff2ca"))


def back_door(draw: ImageDraw.ImageDraw):
    shadow(draw, (132, 392, 380, 440), 30)
    draw.rounded_rectangle((150, 58, 362, 408), radius=28, fill=rgba("#72472c"), outline=rgba("#362013"), width=14)
    draw.rounded_rectangle((190, 92, 322, 202), radius=16, fill=rgba("#ffefad", 220), outline=rgba("#5d3e1c"), width=9)
    draw.line((256, 96, 256, 198), fill=rgba("#5d3e1c"), width=6)
    draw.line((194, 146, 318, 146), fill=rgba("#5d3e1c"), width=6)
    draw.ellipse((306, 238, 340, 272), fill=rgba("#f5d96a"), outline=rgba("#5a4317"), width=5)
    draw.arc((110, 284, 402, 494), 204, 336, fill=rgba("#ffd45a", 185), width=18)


def fence_run(draw: ImageDraw.ImageDraw):
    shadow(draw, (48, 392, 464, 450), 26)
    for x in [70, 150, 230, 310, 390]:
        draw.rounded_rectangle((x, 86, x + 44, 408), radius=14, fill=rgba("#bb8a50"), outline=rgba("#513018"), width=8)
        draw.polygon([(x, 86), (x + 22, 42), (x + 44, 86)], fill=rgba("#d2a46a"), outline=rgba("#513018"))
    for y in [154, 254, 338]:
        draw.rounded_rectangle((36, y, 476, y + 34), radius=12, fill=rgba("#d39a59"), outline=rgba("#513018"), width=7)
    draw.line((58, 142, 456, 372), fill=rgba("#7a4d2a", 120), width=7)


def snack_table(draw: ImageDraw.ImageDraw):
    shadow(draw)
    draw.rounded_rectangle((88, 238, 424, 306), radius=22, fill=rgba("#8b5a35"), outline=rgba("#3f2514"), width=10)
    draw.line((142, 304, 92, 420), fill=rgba("#5a351f"), width=22)
    draw.line((370, 304, 420, 420), fill=rgba("#5a351f"), width=22)
    draw.ellipse((170, 148, 342, 228), fill=rgba("#f2ead8"), outline=rgba("#5e4b37"), width=8)
    draw.polygon([(226, 116), (316, 140), (266, 218)], fill=rgba("#ffd84d"), outline=rgba("#8a6200"))
    for x, y in [(148, 180), (354, 196), (306, 116)]:
        draw.ellipse((x - 15, y - 12, x + 15, y + 12), fill=rgba("#d9793d"), outline=rgba("#5a2e16"), width=4)
    draw.arc((108, 94, 404, 286), 190, 350, fill=rgba("#ffe86a", 165), width=10)


def laundry_corner(draw: ImageDraw.ImageDraw):
    shadow(draw)
    draw.line((84, 118, 428, 118), fill=rgba("#f6f2d8"), width=12)
    for x in [96, 416]:
        draw.line((x, 116, x, 398), fill=rgba("#6b4b2d"), width=14)
    clothes = [("#6fa8ff", 124), ("#ffe05c", 190), ("#ff7e92", 256), ("#dff4ff", 322)]
    for color, x in clothes:
        draw.rounded_rectangle((x, 130, x + 54, 246), radius=14, fill=rgba(color), outline=rgba("#4a3a2d"), width=6)
        draw.rectangle((x + 18, 118, x + 36, 136), fill=rgba("#4a3a2d"))
    draw.rounded_rectangle((156, 286, 358, 420), radius=30, fill=rgba("#d9a969"), outline=rgba("#5b3b20"), width=10)
    for x in [192, 236, 280, 324]:
        draw.line((x, 300, x - 10, 404), fill=rgba("#80512b"), width=9)
    draw.ellipse((262, 248, 348, 328), fill=rgba("#89c7ff"), outline=rgba("#2b5477"), width=7)


def scent_trail(draw: ImageDraw.ImageDraw):
    for i, (x, y, r) in enumerate([(116, 324, 42), (184, 268, 34), (250, 226, 44), (326, 174, 32), (394, 128, 38)]):
        draw.ellipse((x - r, y - r * 0.55, x + r, y + r * 0.55), fill=rgba("#6cd69a", 118), outline=rgba("#2f8d55", 200), width=8)
        draw.arc((x - r - 12, y - r - 12, x + r + 12, y + r + 12), 210, 330, fill=rgba("#eaffc8", 210), width=8)
    draw.line((92, 350, 420, 106), fill=rgba("#eaffc8", 105), width=9)


def leash_route(draw: ImageDraw.ImageDraw):
    for i, (x, y) in enumerate([(98, 346), (164, 302), (232, 256), (304, 212), (380, 166)]):
        draw.ellipse((x - 42, y - 24, x + 42, y + 24), fill=rgba("#d1c08a", 220), outline=rgba("#6c6140"), width=7)
        draw.text((x - 8, y - 13), str(i + 1), fill=rgba("#4c4128"))
    draw.arc((76, 120, 438, 412), 210, 340, fill=rgba("#38bdd2", 220), width=18)
    draw.ellipse((326, 116, 408, 198), outline=rgba("#177383"), width=12)


def pee_break_path(draw: ImageDraw.ImageDraw):
    draw.polygon([(62, 326), (450, 214), (430, 298), (78, 398)], fill=rgba("#ffd45a", 130), outline=rgba("#e3a825", 170))
    draw.rounded_rectangle((328, 118, 384, 280), radius=20, fill=rgba("#d9533f"), outline=rgba("#5c231c"), width=9)
    draw.ellipse((300, 74, 412, 150), fill=rgba("#e86a52"), outline=rgba("#5c231c"), width=9)
    draw.rectangle((348, 276, 364, 378), fill=rgba("#5c231c"))
    for x, y in [(148, 304), (202, 286), (250, 270)]:
        draw.ellipse((x - 16, y - 10, x + 16, y + 10), fill=rgba("#fff4a6", 220), outline=rgba("#c49a25"), width=4)


def threat_lane(draw: ImageDraw.ImageDraw):
    draw.rounded_rectangle((56, 226, 456, 302), radius=32, fill=rgba("#141820", 120), outline=rgba("#343b45", 180), width=9)
    draw.polygon([(106, 184), (220, 226), (102, 266), (154, 226)], fill=rgba("#202733", 155), outline=rgba("#080c12"))
    draw.polygon([(406, 184), (292, 226), (410, 266), (358, 226)], fill=rgba("#202733", 155), outline=rgba("#080c12"))
    draw.rectangle((64, 330, 448, 364), fill=rgba("#8b5a2d", 185), outline=rgba("#4c2b13"))
    for x in [116, 196, 276, 356]:
        draw.polygon([(x, 326), (x + 42, 326), (x + 34, 392), (x + 8, 392)], fill=rgba("#b47a42"), outline=rgba("#4c2b13"))
    draw.ellipse((236, 204, 276, 244), fill=rgba("#ffe15c", 220), outline=rgba("#5d4214"), width=5)


def lawn_landmarks(draw: ImageDraw.ImageDraw):
    draw.ellipse((74, 92, 438, 414), fill=rgba("#65ad4d", 92), outline=rgba("#2f7d35", 170), width=12)
    draw.polygon([(116, 270), (244, 230), (278, 334), (138, 374)], fill=rgba("#d96b55", 230), outline=rgba("#612d22"))
    draw.line((136, 268, 260, 334), fill=rgba("#fff0b2", 170), width=8)
    draw.line((202, 242, 164, 368), fill=rgba("#fff0b2", 170), width=8)
    draw.rounded_rectangle((286, 178, 414, 286), radius=22, fill=rgba("#d3b06d", 220), outline=rgba("#72562d"), width=8)
    draw.ellipse((328, 96, 446, 190), fill=rgba("#3f8a38", 210), outline=rgba("#1e4d1f"), width=8)
    draw.rectangle((374, 180, 398, 304), fill=rgba("#70451f"))


def pond(draw: ImageDraw.ImageDraw):
    shadow(draw, (70, 374, 442, 438), 26)
    draw.ellipse((66, 104, 446, 390), fill=rgba("#2f78a8", 220), outline=rgba("#14405f"), width=12)
    draw.ellipse((126, 150, 386, 340), fill=rgba("#63bce0", 170), outline=rgba("#d7f7ff", 130), width=8)
    for box in [(130, 184, 224, 230), (280, 232, 382, 282), (206, 292, 298, 330)]:
        draw.ellipse(box, fill=rgba("#90d8f0", 120), outline=rgba("#e5fcff", 165), width=5)
    draw.ellipse((120, 248, 186, 292), fill=rgba("#5fb65a"), outline=rgba("#256c31"), width=6)
    draw.ellipse((328, 160, 390, 202), fill=rgba("#5fb65a"), outline=rgba("#256c31"), width=6)
    draw.arc((170, 178, 342, 304), 205, 330, fill=rgba("#fff4a8", 180), width=9)


def shade_tree(draw: ImageDraw.ImageDraw):
    shadow(draw, (82, 386, 430, 450), 30)
    draw.rounded_rectangle((226, 178, 286, 420), radius=20, fill=rgba("#7b4b22"), outline=rgba("#3d2411"), width=9)
    draw.line((252, 232, 178, 152), fill=rgba("#7b4b22"), width=22)
    draw.line((262, 224, 354, 142), fill=rgba("#7b4b22"), width=20)
    for cx, cy, rx, ry, color in [
        (174, 154, 86, 70, "#2f7d35"),
        (252, 112, 108, 88, "#3d963e"),
        (338, 158, 88, 74, "#2f8138"),
        (244, 204, 132, 88, "#4fa947"),
    ]:
        draw.ellipse((cx - rx, cy - ry, cx + rx, cy + ry), fill=rgba(color, 225), outline=rgba("#1e4d1f"), width=8)
    for x, y in [(168, 160), (228, 118), (314, 188), (260, 220)]:
        draw.ellipse((x - 10, y - 8, x + 10, y + 8), fill=rgba("#d9f286", 180))


def garden_bed(draw: ImageDraw.ImageDraw):
    shadow(draw, (76, 394, 436, 448), 26)
    draw.rounded_rectangle((126, 92, 386, 414), radius=34, fill=rgba("#6b4526", 235), outline=rgba("#321f11"), width=10)
    for y in [144, 210, 276, 342]:
        draw.arc((102, y - 26, 410, y + 34), 5, 175, fill=rgba("#a56f38", 150), width=9)
    for x, y, color in [
        (178, 140, "#d8557f"), (266, 182, "#e8c24a"), (336, 238, "#8cd65d"),
        (202, 292, "#6fa8ff"), (300, 342, "#ff8a60"),
    ]:
        draw.line((x, y + 42, x, y + 10), fill=rgba("#2f7d35"), width=8)
        draw.ellipse((x - 22, y - 20, x + 22, y + 20), fill=rgba(color), outline=rgba("#4b2a20"), width=5)


def flower_patch(draw: ImageDraw.ImageDraw):
    draw.line((256, 294, 256, 402), fill=rgba("#2f7d35"), width=16)
    for angle in range(0, 360, 60):
        rad = math.radians(angle)
        cx = 256 + math.cos(rad) * 54
        cy = 246 + math.sin(rad) * 42
        draw.ellipse((cx - 30, cy - 22, cx + 30, cy + 22), fill=rgba("#f06a93"), outline=rgba("#6b2338"), width=5)
    draw.ellipse((222, 212, 290, 280), fill=rgba("#ffd94f"), outline=rgba("#7a5614"), width=6)
    draw.ellipse((184, 348, 258, 386), fill=rgba("#4ca94a", 180), outline=rgba("#256c31"), width=5)
    draw.ellipse((252, 346, 332, 388), fill=rgba("#4ca94a", 180), outline=rgba("#256c31"), width=5)


def picnic_blanket(draw: ImageDraw.ImageDraw):
    shadow(draw, (82, 388, 430, 450), 28)
    draw.polygon([(92, 170), (398, 112), (430, 342), (118, 408)], fill=rgba("#d96b55"), outline=rgba("#612d22"))
    for t in [0.25, 0.5, 0.75]:
        x1 = 92 + (398 - 92) * t
        x2 = 118 + (430 - 118) * t
        draw.line((x1, 170 - 58 * t, x2, 408 - 66 * t), fill=rgba("#fff0b2", 180), width=10)
    for t in [0.32, 0.62]:
        y1 = 170 + (408 - 170) * t
        y2 = 112 + (342 - 112) * t
        draw.line((104, y1, 418, y2), fill=rgba("#fff0b2", 180), width=10)
    draw.ellipse((278, 160, 372, 210), fill=rgba("#f3ead8"), outline=rgba("#5e4b37"), width=6)
    draw.polygon([(298, 124), (354, 140), (326, 190)], fill=rgba("#ffd84d"), outline=rgba("#8a6200"))


def sandbox(draw: ImageDraw.ImageDraw):
    shadow(draw)
    draw.rounded_rectangle((102, 168, 410, 364), radius=28, fill=rgba("#c18b47"), outline=rgba("#61421f"), width=12)
    draw.rounded_rectangle((132, 194, 380, 332), radius=22, fill=rgba("#e1bd74"), outline=rgba("#8a6530"), width=8)
    draw.rectangle((112, 150, 400, 198), fill=rgba("#9a6330"), outline=rgba("#4a2e17"), width=8)
    draw.ellipse((200, 220, 272, 278), fill=rgba("#f4d28a"), outline=rgba("#8a6530"), width=5)
    draw.line((304, 210, 358, 286), fill=rgba("#dc5d4a"), width=18)
    draw.polygon([(344, 278), (388, 298), (344, 322)], fill=rgba("#5aa4e8"), outline=rgba("#24435e"))


def stepping_stone(draw: ImageDraw.ImageDraw):
    shadow(draw, (104, 358, 408, 420), 24)
    draw.ellipse((108, 156, 404, 346), fill=rgba("#8c887e"), outline=rgba("#46433d"), width=10)
    draw.ellipse((154, 188, 250, 236), fill=rgba("#a6a198", 160))
    draw.ellipse((282, 242, 354, 282), fill=rgba("#747067", 130))
    draw.arc((128, 140, 394, 354), 205, 330, fill=rgba("#d5d0c4", 160), width=8)


DRAWERS = {
    "yard_back_door": back_door,
    "yard_fence_run": fence_run,
    "yard_flower_patch": flower_patch,
    "yard_garden_bed": garden_bed,
    "yard_house_patio": house_patio,
    "yard_laundry_corner": laundry_corner,
    "yard_lawn_landmarks": lawn_landmarks,
    "yard_leash_route": leash_route,
    "yard_picnic_blanket": picnic_blanket,
    "yard_pond": pond,
    "yard_pee_break_path": pee_break_path,
    "yard_sandbox": sandbox,
    "yard_scent_trail": scent_trail,
    "yard_shade_tree": shade_tree,
    "yard_snack_table": snack_table,
    "yard_stepping_stone": stepping_stone,
    "yard_threat_lane": threat_lane,
}


def write_readme():
    REF_ROOT.mkdir(parents=True, exist_ok=True)
    names = "\n".join(f"- `{name}.png`" for name in sorted(DRAWERS))
    (REF_ROOT / "README.md").write_text(
        "# Generated Environment Prop Pack\n\n"
        "Deterministic transparent cartoon backyard environment sprites generated by "
        "`tools/art/generate_environment_prop_pack.py`. Runtime code layers these over "
        "the generated Unity district props so the couch-test arena no longer reads as "
        "large geometric placeholders. These remain generated couch-test art, not a "
        "final hand-authored art pipeline.\n\n"
        f"{names}\n",
        encoding="utf-8",
    )


def contact_sheet(images: list[tuple[str, Image.Image]]):
    cell = 180
    cols = 5
    rows = math.ceil(len(images) / cols)
    sheet = Image.new("RGBA", (cols * cell, rows * cell), (246, 249, 242, 255))
    draw = ImageDraw.Draw(sheet)
    for index, (name, image) in enumerate(images):
        x = (index % cols) * cell
        y = (index // cols) * cell
        thumb = image.copy()
        thumb.thumbnail((150, 128), Image.Resampling.LANCZOS)
        sheet.alpha_composite(thumb, (x + (cell - thumb.width) // 2, y + 8))
        draw.text((x + 8, y + 142), name[:24], fill=(38, 38, 38, 255))
    sheet.save(REF_ROOT / "environment_prop_pack_contact_sheet.png")


def main():
    ROOT.mkdir(parents=True, exist_ok=True)
    REF_ROOT.mkdir(parents=True, exist_ok=True)
    rendered = []
    for name, drawer in sorted(DRAWERS.items()):
        image, draw = canvas()
        drawer(draw)
        image.save(ROOT / f"{name}.png")
        rendered.append((name, image))
    contact_sheet(rendered)
    write_readme()
    print(f"Generated {len(rendered)} environment prop sprites in {ROOT}")


if __name__ == "__main__":
    main()
