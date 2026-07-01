#!/usr/bin/env python3
"""Generate transparent cartoon mission prop sprites for ArenaFinal.

The output is intentionally deterministic: these are couch-test gameplay icons,
not final authored art. Runtime code treats them as overlays on existing
controller-owned markers so gameplay geometry remains unchanged.
"""

from __future__ import annotations

import math
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path("unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props/Missions")
REF_ROOT = Path("unity/CheddarAndCocoa/Assets/Art/ReferenceOnly/GeneratedMissionProps")
SIZE = 512


def rgba(hex_value: str, alpha: int = 255) -> tuple[int, int, int, int]:
    hex_value = hex_value.lstrip("#")
    return tuple(int(hex_value[i : i + 2], 16) for i in (0, 2, 4)) + (alpha,)


def canvas() -> tuple[Image.Image, ImageDraw.ImageDraw]:
    image = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    return image, ImageDraw.Draw(image)


def outline(draw: ImageDraw.ImageDraw, shape: str, box, fill, width=12):
    if shape == "ellipse":
        draw.ellipse(box, fill=fill, outline=rgba("#33251c"), width=width)
    elif shape == "round":
        draw.rounded_rectangle(box, radius=28, fill=fill, outline=rgba("#33251c"), width=width)
    else:
        draw.rectangle(box, fill=fill, outline=rgba("#33251c"), width=width)


def shadow(draw: ImageDraw.ImageDraw):
    draw.ellipse((82, 388, 430, 444), fill=(0, 0, 0, 42))


def bone(draw: ImageDraw.ImageDraw, color=rgba("#fff1cf")):
    draw.rounded_rectangle((152, 236, 360, 292), radius=26, fill=color, outline=rgba("#5a422b"), width=10)
    for cx, cy in [(139, 222), (137, 306), (373, 222), (375, 306)]:
        draw.ellipse((cx - 46, cy - 46, cx + 46, cy + 46), fill=color, outline=rgba("#5a422b"), width=10)


def snack_plate(draw):
    shadow(draw)
    outline(draw, "ellipse", (92, 274, 420, 390), rgba("#f6f0dd"))
    draw.ellipse((132, 292, 380, 370), fill=rgba("#d7f1ff"), outline=rgba("#5e8aa6"), width=8)
    draw.polygon([(214, 192), (318, 220), (262, 312)], fill=rgba("#ffd94f"), outline=rgba("#9a6d00"))
    draw.line([(230, 230), (282, 244), (256, 286)], fill=rgba("#fff28a"), width=8)
    for x, y in [(158, 244), (350, 272), (308, 178)]:
        draw.ellipse((x - 16, y - 14, x + 16, y + 14), fill=rgba("#e68645"), outline=rgba("#6d3c1b"), width=5)


def sock_bundle(draw):
    shadow(draw)
    outline(draw, "round", (158, 98, 286, 392), rgba("#dff4ff"))
    draw.rounded_rectangle((164, 102, 280, 172), radius=22, fill=rgba("#69b6f5"), outline=rgba("#2b5477"), width=8)
    draw.rounded_rectangle((214, 280, 380, 396), radius=34, fill=rgba("#dff4ff"), outline=rgba("#2b5477"), width=10)
    draw.rectangle((168, 210, 282, 234), fill=rgba("#f05b7a"))
    draw.rectangle((168, 252, 282, 274), fill=rgba("#ffd34f"))
    draw.arc((230, 272, 386, 404), 185, 355, fill=rgba("#f05b7a"), width=18)


def laundry_basket(draw, open_=False):
    shadow(draw)
    outline(draw, "round", (112, 166, 400, 390), rgba("#d9a969"))
    draw.rounded_rectangle((90, 128, 422, 190), radius=24, fill=rgba("#f0c989"), outline=rgba("#5b3b20"), width=10)
    for x in [152, 208, 264, 320, 376]:
        draw.line((x, 196, x - 18, 374), fill=rgba("#80512b"), width=12)
    if open_:
        draw.arc((110, 70, 402, 220), 190, 350, fill=rgba("#5b3b20"), width=18)
        draw.ellipse((258, 86, 346, 174), fill=rgba("#89c7ff"), outline=rgba("#2b5477"), width=8)


def squirrel_stash(draw):
    shadow(draw)
    outline(draw, "round", (126, 202, 386, 388), rgba("#8a5226"))
    draw.arc((132, 86, 380, 272), 180, 360, fill=rgba("#4a2c16"), width=18)
    for cx, cy in [(190, 220), (250, 184), (314, 230), (226, 286), (302, 300)]:
        draw.ellipse((cx - 34, cy - 28, cx + 34, cy + 28), fill=rgba("#b6772f"), outline=rgba("#43240f"), width=8)
        draw.rectangle((cx - 22, cy - 30, cx + 22, cy - 10), fill=rgba("#5d3518"))


def escape_gap(draw):
    shadow(draw)
    draw.rectangle((116, 118, 178, 390), fill=rgba("#9b6a34"), outline=rgba("#4c2b13"), width=8)
    draw.rectangle((334, 118, 396, 390), fill=rgba("#9b6a34"), outline=rgba("#4c2b13"), width=8)
    for y in [156, 236, 316]:
        draw.rectangle((70, y, 442, y + 28), fill=rgba("#c88b44"), outline=rgba("#4c2b13"), width=6)
    draw.polygon([(236, 150), (276, 150), (276, 376), (236, 376)], fill=(0, 0, 0, 0), outline=rgba("#ffdf5a"), width=18)


def gate(draw):
    shadow(draw)
    draw.rectangle((102, 132, 154, 396), fill=rgba("#935a30"), outline=rgba("#3d2212"), width=8)
    draw.rectangle((358, 132, 410, 396), fill=rgba("#935a30"), outline=rgba("#3d2212"), width=8)
    for x in [176, 232, 288]:
        draw.rounded_rectangle((x, 118, x + 36, 402), radius=16, fill=rgba("#d18b4c"), outline=rgba("#3d2212"), width=7)
    draw.arc((148, 90, 364, 230), 180, 360, fill=rgba("#3d2212"), width=12)


def squeaky_toy(draw):
    shadow(draw)
    outline(draw, "ellipse", (118, 178, 394, 372), rgba("#ff6b75"))
    draw.ellipse((212, 112, 300, 204), fill=rgba("#ffdb55"), outline=rgba("#4b2b14"), width=8)
    draw.ellipse((182, 236, 218, 272), fill=rgba("#1f2026"))
    draw.ellipse((294, 236, 330, 272), fill=rgba("#1f2026"))
    draw.arc((204, 260, 308, 328), 15, 165, fill=rgba("#1f2026"), width=10)


def steak_plate(draw):
    shadow(draw)
    outline(draw, "ellipse", (100, 282, 412, 394), rgba("#edf0f5"))
    outline(draw, "ellipse", (152, 148, 374, 318), rgba("#b84d3a"))
    draw.ellipse((218, 194, 292, 258), fill=rgba("#ffd7b0"), outline=rgba("#6f2b1e"), width=8)
    draw.line((152, 252, 374, 210), fill=rgba("#802b21"), width=10)


def table_human(draw):
    shadow(draw)
    draw.rounded_rectangle((118, 286, 394, 350), radius=24, fill=rgba("#9d6842"), outline=rgba("#463018"), width=10)
    for x in [152, 344]:
        draw.rectangle((x, 342, x + 34, 420), fill=rgba("#6d452c"))
    draw.ellipse((202, 86, 310, 194), fill=rgba("#f1b083"), outline=rgba("#4b2719"), width=8)
    draw.arc((174, 76, 338, 210), 190, 350, fill=rgba("#3d261c"), width=26)
    draw.rounded_rectangle((176, 194, 336, 300), radius=32, fill=rgba("#5d9ad9"), outline=rgba("#24435e"), width=8)
    draw.rectangle((206, 244, 306, 286), fill=rgba("#333b44"))


def decoy_toy(draw):
    shadow(draw)
    outline(draw, "ellipse", (132, 178, 380, 380), rgba("#69d889"))
    draw.rectangle((224, 106, 288, 190), fill=rgba("#ffd95a"), outline=rgba("#5c4010"), width=8)
    draw.ellipse((188, 228, 224, 264), fill=rgba("#1e2620"))
    draw.ellipse((288, 228, 324, 264), fill=rgba("#1e2620"))
    draw.arc((208, 274, 304, 338), 20, 160, fill=rgba("#1e2620"), width=10)


def walk_human(draw):
    shadow(draw)
    draw.ellipse((198, 70, 314, 186), fill=rgba("#f2b78d"), outline=rgba("#4b2719"), width=8)
    draw.rounded_rectangle((176, 182, 336, 326), radius=30, fill=rgba("#7057c8"), outline=rgba("#2c2550"), width=10)
    draw.line((204, 322, 174, 422), fill=rgba("#2c2550"), width=28)
    draw.line((304, 322, 344, 422), fill=rgba("#2c2550"), width=28)
    draw.arc((130, 196, 382, 358), 190, 345, fill=rgba("#36b7c8"), width=14)


def walk_leash(draw):
    shadow(draw)
    draw.arc((128, 118, 384, 374), 30, 330, fill=rgba("#2fc2d4"), width=22)
    draw.ellipse((194, 168, 318, 292), outline=rgba("#165c66"), width=18)
    draw.rectangle((330, 276, 382, 330), fill=rgba("#ffcf4f"), outline=rgba("#5b4211"), width=8)
    draw.line((128, 364, 210, 296), fill=rgba("#2fc2d4"), width=18)


def car_balance(draw):
    shadow(draw)
    outline(draw, "round", (82, 202, 430, 326), rgba("#67a9e8"))
    draw.polygon([(152, 202), (214, 132), (308, 132), (364, 202)], fill=rgba("#95d8ff"), outline=rgba("#24435e"))
    for cx in [166, 348]:
        draw.ellipse((cx - 38, 306, cx + 38, 382), fill=rgba("#27323b"), outline=rgba("#0c1115"), width=8)
        draw.ellipse((cx - 15, 329, cx + 15, 359), fill=rgba("#dfe7ea"))
    draw.line((260, 112, 260, 402), fill=rgba("#ffde59"), width=10)


def dig_mound(draw):
    shadow(draw)
    draw.ellipse((112, 246, 400, 390), fill=rgba("#8f5a2e"), outline=rgba("#3f2412"), width=10)
    draw.ellipse((164, 198, 280, 292), fill=rgba("#a96b35"), outline=rgba("#3f2412"), width=8)
    draw.line((142, 318, 376, 280), fill=rgba("#c88444"), width=12)
    draw.line((178, 350, 328, 326), fill=rgba("#643a1c"), width=8)


def scent_post(draw):
    shadow(draw)
    draw.rounded_rectangle((218, 118, 294, 396), radius=24, fill=rgba("#c48a4d"), outline=rgba("#4b2c17"), width=10)
    for y in [166, 232, 298]:
        draw.rectangle((182, y, 330, y + 28), fill=rgba("#ebc070"), outline=rgba("#4b2c17"), width=6)
    for r in [60, 96, 132]:
        draw.arc((256 - r, 212 - r, 256 + r, 212 + r), 205, 325, fill=rgba("#63d7ff", 185), width=10)


def territory_zone(draw):
    draw.ellipse((80, 92, 432, 420), fill=rgba("#98e067", 70), outline=rgba("#338a37", 220), width=18)
    draw.ellipse((176, 170, 336, 330), fill=rgba("#eaf6b3", 110), outline=rgba("#338a37", 180), width=10)
    draw.line((220, 260, 256, 306, 318, 202), fill=rgba("#2f7d34"), width=18)


def leash_checkpoint(draw):
    draw.ellipse((92, 92, 420, 420), fill=rgba("#e7fbff", 80), outline=rgba("#33bcd0"), width=20)
    draw.ellipse((176, 176, 336, 336), fill=rgba("#fff4a8", 120), outline=rgba("#ffca36"), width=14)
    draw.arc((184, 164, 328, 348), 70, 430, fill=rgba("#33bcd0"), width=16)


def chaos_lever(draw):
    shadow(draw)
    outline(draw, "round", (168, 296, 344, 388), rgba("#9aa1aa"))
    draw.line((256, 306, 182, 128), fill=rgba("#6b4f3a"), width=34)
    draw.ellipse((136, 76, 222, 162), fill=rgba("#ff615c"), outline=rgba("#5c1e1b"), width=9)


def chaos_junction(draw):
    shadow(draw)
    for box in [(146, 146, 254, 254), (258, 146, 366, 254), (202, 258, 310, 366)]:
        outline(draw, "ellipse", box, rgba("#f0c25c"))
    draw.line((238, 238, 274, 274), fill=rgba("#5f4620"), width=18)
    draw.line((274, 238, 238, 274), fill=rgba("#5f4620"), width=18)
    draw.ellipse((226, 226, 286, 286), fill=rgba("#ffe58a"), outline=rgba("#5f4620"), width=8)


def escape_station(draw):
    shadow(draw)
    outline(draw, "round", (154, 132, 358, 382), rgba("#b9c2ce"))
    draw.ellipse((192, 168, 320, 296), fill=rgba("#f2dd6b"), outline=rgba("#51461c"), width=10)
    draw.line((214, 232, 298, 232), fill=rgba("#51461c"), width=14)
    draw.line((256, 190, 256, 274), fill=rgba("#51461c"), width=14)


def catch_blanket(draw):
    shadow(draw)
    draw.polygon([(92, 250), (420, 178), (404, 338), (108, 390)], fill=rgba("#f4cf5b"), outline=rgba("#5b4520"))
    for i in range(4):
        x = 130 + i * 68
        draw.line((x, 236 - i * 14, x + 40, 366 - i * 10), fill=rgba("#f06e58"), width=12)
    draw.line((96, 252, 420, 178), fill=rgba("#5b4520"), width=10)
    draw.line((108, 390, 404, 338), fill=rgba("#5b4520"), width=10)


def kitchen_counter(draw):
    shadow(draw)
    draw.rectangle((76, 242, 436, 328), fill=rgba("#c58b53"), outline=rgba("#4a2c19"), width=10)
    draw.rectangle((104, 326, 408, 414), fill=rgba("#8c5a32"), outline=rgba("#4a2c19"), width=10)
    for x in [168, 256, 344]:
        draw.ellipse((x - 18, 356, x + 18, 392), fill=rgba("#ffd26b"), outline=rgba("#4a2c19"), width=6)
    draw.rectangle((154, 178, 358, 238), fill=rgba("#fff3d0"), outline=rgba("#4a2c19"), width=8)


def kitchen_bowl(draw):
    shadow(draw)
    draw.pieslice((108, 160, 404, 438), 0, 180, fill=rgba("#7fd0ff"), outline=rgba("#24435e"))
    draw.ellipse((108, 138, 404, 266), fill=rgba("#d9f3ff"), outline=rgba("#24435e"), width=12)
    draw.ellipse((174, 170, 338, 236), fill=rgba("#ffe07b"), outline=rgba("#8d5b13"), width=8)


def kitchen_food(draw, bad=False):
    shadow(draw)
    if bad:
        draw.ellipse((138, 132, 374, 368), fill=rgba("#b171cf"), outline=rgba("#4e275d"), width=12)
        draw.line((180, 190, 332, 320), fill=rgba("#fff0ff"), width=14)
        draw.line((332, 190, 180, 320), fill=rgba("#fff0ff"), width=14)
    else:
        draw.ellipse((142, 158, 370, 346), fill=rgba("#f59d48"), outline=rgba("#5b3417"), width=12)
        draw.line((164, 238, 350, 238), fill=rgba("#ffe86b"), width=12)
        draw.line((184, 196, 326, 288), fill=rgba("#ffe86b"), width=10)


def kitchen_warning(draw):
    draw.polygon([(256, 86), (438, 398), (74, 398)], fill=rgba("#ffe05c", 215), outline=rgba("#4e3512"))
    draw.line((256, 174, 256, 286), fill=rgba("#4e3512"), width=26)
    draw.ellipse((238, 324, 274, 360), fill=rgba("#4e3512"))


def falling_snack(draw):
    kitchen_food(draw, bad=False)


DRAWERS = {
    "snack_plate": snack_plate,
    "sock_bundle": sock_bundle,
    "laundry_basket": lambda d: laundry_basket(d, False),
    "laundry_basket_open": lambda d: laundry_basket(d, True),
    "squirrel_stash": squirrel_stash,
    "escape_gap": escape_gap,
    "gate": gate,
    "squeaky_toy": squeaky_toy,
    "steak_plate": steak_plate,
    "table_human": table_human,
    "decoy_toy": decoy_toy,
    "walk_human": walk_human,
    "walk_leash": walk_leash,
    "car_balance": car_balance,
    "dig_mound": dig_mound,
    "buried_bone": lambda d: (shadow(d), bone(d)),
    "scent_post": scent_post,
    "territory_zone": territory_zone,
    "leash_checkpoint": leash_checkpoint,
    "bone_mound": dig_mound,
    "chaos_lever": chaos_lever,
    "chaos_junction": chaos_junction,
    "escape_station": escape_station,
    "catch_blanket": catch_blanket,
    "falling_snack": falling_snack,
    "kitchen_counter": kitchen_counter,
    "kitchen_safe_bowl": kitchen_bowl,
    "kitchen_good_food": lambda d: kitchen_food(d, False),
    "kitchen_bad_food": lambda d: kitchen_food(d, True),
    "kitchen_warning": kitchen_warning,
}


def write_readme():
    REF_ROOT.mkdir(parents=True, exist_ok=True)
    names = "\n".join(f"- `{name}.png`" for name in sorted(DRAWERS))
    (REF_ROOT / "README.md").write_text(
        "# Generated Mission Prop Pack Pass 2\n\n"
        "Deterministic transparent cartoon gameplay sprites generated by "
        "`tools/art/generate_mission_prop_pack.py` on demand. These are couch-test "
        "runtime overlays for controller-owned Unity markers; they are not final "
        "hand-authored art.\n\n"
        f"{names}\n",
        encoding="utf-8",
    )


def contact_sheet(images: list[tuple[str, Image.Image]]):
    cell = 160
    cols = 6
    rows = math.ceil(len(images) / cols)
    sheet = Image.new("RGBA", (cols * cell, rows * cell), (245, 248, 244, 255))
    draw = ImageDraw.Draw(sheet)
    for index, (name, image) in enumerate(images):
        x = (index % cols) * cell
        y = (index // cols) * cell
        thumb = image.copy()
        thumb.thumbnail((132, 112), Image.Resampling.LANCZOS)
        sheet.alpha_composite(thumb, (x + (cell - thumb.width) // 2, y + 8))
        draw.text((x + 8, y + 126), name[:22], fill=(38, 38, 38, 255))
    sheet.save(REF_ROOT / "mission_prop_pack_pass_2_contact_sheet.png")


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
    print(f"Generated {len(rendered)} mission prop sprites in {ROOT}")


if __name__ == "__main__":
    main()
