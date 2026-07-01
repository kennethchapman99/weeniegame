#!/usr/bin/env python3
"""Generate transparent state sprites for Great Escape, Blanket Catch, and Kitchen Frenzy.

These are deterministic couch-test sprites that replace visually ambiguous shape blocks while
preserving the mission-controller-owned gameplay objects, labels, colliders, and timing.
"""

from __future__ import annotations

import math
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path("unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props")
REF_ROOT = Path("unity/CheddarAndCocoa/Assets/Art/ReferenceOnly/GeneratedEscapeCatchKitchenP0")
SIZE = 512


def rgba(hex_value: str, alpha: int = 255) -> tuple[int, int, int, int]:
    hex_value = hex_value.lstrip("#")
    return tuple(int(hex_value[i : i + 2], 16) for i in (0, 2, 4)) + (alpha,)


def canvas() -> tuple[Image.Image, ImageDraw.ImageDraw]:
    image = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    return image, ImageDraw.Draw(image)


def shadow(draw: ImageDraw.ImageDraw, box=(68, 388, 444, 446), alpha=34):
    draw.ellipse(box, fill=(0, 0, 0, alpha))


def paw(draw: ImageDraw.ImageDraw, cx: int, cy: int, scale: float, fill, outline="#3d2514"):
    r = int(21 * scale)
    draw.ellipse((cx - r, cy - r, cx + r, cy + r), fill=fill, outline=rgba(outline), width=max(3, int(5 * scale)))
    for ox, oy, tr in [(-24, -25, 8), (-8, -34, 7), (9, -34, 7), (25, -25, 8)]:
        rr = int(tr * scale)
        x = int(cx + ox * scale)
        y = int(cy + oy * scale)
        draw.ellipse((x - rr, y - rr, x + rr, y + rr), fill=fill, outline=rgba(outline), width=max(2, int(3 * scale)))


def gear(draw: ImageDraw.ImageDraw, cx: int, cy: int, r: int, fill="#f6c247"):
    points = []
    for i in range(28):
        angle = -math.pi / 2 + i * math.pi * 2 / 28
        radius = r if i % 2 == 0 else int(r * 0.74)
        points.append((cx + math.cos(angle) * radius, cy + math.sin(angle) * radius))
    draw.polygon(points, fill=rgba(fill), outline=rgba("#4f3218"))
    draw.ellipse((cx - r * 0.48, cy - r * 0.48, cx + r * 0.48, cy + r * 0.48),
                 fill=rgba("#fff2a8"), outline=rgba("#4f3218"), width=5)
    draw.ellipse((cx - r * 0.16, cy - r * 0.16, cx + r * 0.16, cy + r * 0.16),
                 fill=rgba("#4f3218"))


def escape_station(draw: ImageDraw.ImageDraw, accent: str, active: bool = False, done: bool = False,
                   fumble: bool = False, settle: bool = False, cocoa: bool = False):
    shadow(draw)
    draw.rounded_rectangle((122, 134, 390, 346), radius=28, fill=rgba("#7b5432"), outline=rgba("#3d2514"), width=9)
    for x in [158, 218, 278, 338]:
        draw.line((x, 142, x - 18, 338), fill=rgba("#d9a45e", 100), width=7)
    draw.rounded_rectangle((166, 184, 346, 294), radius=18, fill=rgba("#f1d7a6"), outline=rgba("#3d2514"), width=7)
    draw.arc((194, 172, 318, 300), 190, 350, fill=rgba("#3d2514"), width=9)
    gear(draw, 150, 330, 34, accent)
    gear(draw, 362, 320, 30, "#8dd7ff" if cocoa else "#ffbf5a")
    if active:
        draw.polygon([(256, 72), (296, 138), (216, 138)], fill=rgba(accent), outline=rgba("#3d2514"))
        paw(draw, 256, 116, 0.78, rgba("#8dd7ff" if cocoa else "#ffbf5a", 230))
    if done:
        draw.line((176, 260, 232, 316, 342, 198), fill=rgba("#58d26f"), width=22)
    if fumble:
        draw.line((188, 160, 330, 314), fill=rgba("#f05d4f"), width=22)
        draw.line((330, 160, 188, 314), fill=rgba("#f05d4f"), width=22)
    if settle:
        draw.arc((174, 98, 338, 262), 30, 320, fill=rgba("#63c8ff"), width=13)
        draw.polygon([(178, 180), (148, 188), (168, 214)], fill=rgba("#63c8ff"))


def blanket(draw: ImageDraw.ImageDraw, mode: str):
    shadow(draw, (52, 348, 460, 430), 30)
    color = {"slack": "#f0d95c", "taut": "#58c871", "ripping": "#f26b51", "caught": "#65d77e"}[mode]
    if mode == "slack":
        points = [(76, 212), (176, 302), (256, 332), (338, 302), (436, 212), (418, 276), (256, 386), (96, 276)]
    elif mode == "ripping":
        points = [(54, 238), (188, 218), (236, 292), (276, 220), (458, 238), (420, 308), (284, 310), (240, 376), (190, 306), (90, 306)]
    else:
        points = [(58, 238), (454, 238), (424, 326), (88, 326)]
    draw.polygon(points, fill=rgba(color, 235), outline=rgba("#3d2514"))
    for x in range(94, 424, 54):
        draw.line((x, 240, x + 24, 326), fill=rgba("#fff5ba", 100), width=6)
    draw.line((64, 238, 450, 238), fill=rgba("#3d2514"), width=8)
    paw(draw, 78, 234, 0.62, rgba("#ffbf5a", 220))
    paw(draw, 434, 234, 0.62, rgba("#8dd7ff", 220))
    if mode == "ripping":
        draw.line((232, 224, 252, 270, 234, 314, 258, 358), fill=rgba("#3d2514"), width=8)
    if mode == "caught":
        snack(draw, "caught", small=True)


def snack(draw: ImageDraw.ImageDraw, mode: str, small: bool = False):
    if not small:
        shadow(draw, (144, 372, 370, 424), 28)
    cx, cy, s = (256, 210, 0.75) if small else (256, 236, 1.0)
    if mode == "splat":
        draw.ellipse((cx - 106, cy + 64, cx + 104, cy + 126), fill=rgba("#d87a45", 190), outline=rgba("#6b341b"), width=6)
        for ox, oy in [(-96, 46), (-46, 112), (54, 98), (106, 48)]:
            draw.ellipse((cx + ox - 18, cy + oy - 12, cx + ox + 18, cy + oy + 12), fill=rgba("#d87a45", 190))
        return
    bun = "#f0b866" if mode != "bad" else "#a779c6"
    draw.rounded_rectangle((cx - int(104 * s), cy - int(42 * s), cx + int(104 * s), cy + int(42 * s)),
                           radius=int(36 * s), fill=rgba(bun), outline=rgba("#5a3318"), width=max(4, int(7 * s)))
    draw.rounded_rectangle((cx - int(86 * s), cy - int(14 * s), cx + int(86 * s), cy + int(16 * s)),
                           radius=int(15 * s), fill=rgba("#b64b2c" if mode != "bad" else "#6e4b8f"),
                           outline=rgba("#5a3318"), width=max(3, int(5 * s)))
    if mode == "falling":
        for y in [88, 122, 156]:
            draw.line((cx - 116, y, cx - 82, y + 34), fill=rgba("#fff1a8", 180), width=7)
            draw.line((cx + 106, y + 10, cx + 72, y + 44), fill=rgba("#fff1a8", 160), width=6)
    if mode == "caught":
        draw.line((cx - 126, cy + 68, cx + 126, cy + 68), fill=rgba("#58c871"), width=12)


def kitchen_counter(draw: ImageDraw.ImageDraw, barked: bool = False):
    shadow(draw)
    draw.rounded_rectangle((80, 178, 432, 338), radius=24, fill=rgba("#996036"), outline=rgba("#3d2514"), width=9)
    draw.rounded_rectangle((70, 144, 442, 206), radius=26, fill=rgba("#d29b62"), outline=rgba("#3d2514"), width=9)
    for x in [132, 210, 288, 366]:
        draw.line((x, 206, x, 336), fill=rgba("#553016"), width=7)
    draw.rounded_rectangle((188, 82, 324, 142), radius=18, fill=rgba("#f6cf6e"), outline=rgba("#3d2514"), width=7)
    paw(draw, 142, 128, 0.7, rgba("#ffbf5a", 220))
    if barked:
        for angle in [-40, -15, 15, 40]:
            rad = math.radians(angle)
            x1 = 256 + math.cos(rad) * 150
            y1 = 94 + math.sin(rad) * 92
            draw.line((256, 112, x1, y1), fill=rgba("#fff1a8", 210), width=9)
        snack(draw, "falling", small=True)


def safe_bowl(draw: ImageDraw.ImageDraw, caught: bool = False):
    shadow(draw, (104, 362, 408, 430), 34)
    draw.ellipse((126, 192, 386, 374), fill=rgba("#4cc36f"), outline=rgba("#244b2c"), width=9)
    draw.ellipse((160, 200, 352, 294), fill=rgba("#d8ffe0"), outline=rgba("#244b2c"), width=6)
    draw.arc((160, 206, 352, 342), 15, 165, fill=rgba("#244b2c"), width=8)
    paw(draw, 256, 330, 0.68, rgba("#8dd7ff", 220), "#244b2c")
    if caught:
        snack(draw, "caught", small=True)
        draw.line((168, 178, 226, 236, 348, 126), fill=rgba("#58d26f"), width=18)


def kitchen_food(draw: ImageDraw.ImageDraw, mode: str):
    if mode == "good":
        snack(draw, "falling")
    elif mode == "bad":
        snack(draw, "bad")
        for angle in [-32, -8, 18]:
            rad = math.radians(angle)
            draw.line((256, 124, 256 + math.cos(rad) * 98, 124 + math.sin(rad) * 74),
                      fill=rgba("#c99be7", 190), width=8)
    else:
        snack(draw, "splat")


def save_asset(folder: str, name: str, draw_fn):
    out = ROOT / folder
    out.mkdir(parents=True, exist_ok=True)
    image, draw = canvas()
    draw_fn(draw)
    image.save(out / f"{name}.png")
    return folder, name, image


def write_readme(items: list[tuple[str, str, Image.Image]]):
    REF_ROOT.mkdir(parents=True, exist_ok=True)
    names = "\n".join(f"- `Props/{folder}/{name}.png` (`512x512`)" for folder, name, _ in items)
    (REF_ROOT / "README.md").write_text(
        "# Generated Escape / Catch / Kitchen P0 Pack\n\n"
        "Deterministic transparent couch-test sprites generated by "
        "`tools/art/generate_escape_catch_kitchen_p0_pack.py`. Runtime mission controllers use "
        "these as state overlays for Great Escape, Blanket Catch, and Kitchen Falling Food Frenzy "
        "so active gameplay props read as authored objects instead of inert color blocks.\n\n"
        f"{names}\n",
        encoding="utf-8",
    )


def write_contact_sheet(items: list[tuple[str, str, Image.Image]]):
    REF_ROOT.mkdir(parents=True, exist_ok=True)
    cell = 192
    cols = 6
    rows = math.ceil(len(items) / cols)
    sheet = Image.new("RGBA", (cols * cell, rows * cell), (245, 240, 226, 255))
    draw = ImageDraw.Draw(sheet)
    for i, (folder, name, image) in enumerate(items):
        x = (i % cols) * cell
        y = (i // cols) * cell
        thumb = image.resize((cell, cell), Image.Resampling.LANCZOS)
        sheet.alpha_composite(thumb, (x, y))
        draw.text((x + 8, y + cell - 20), name[:28], fill=(45, 35, 25, 255))
    sheet.convert("RGB").save(REF_ROOT / "escape_catch_kitchen_p0_contact_sheet.png", quality=95)


def main():
    items = [
        save_asset("GreatEscape", "great_escape_station_waiting", lambda d: escape_station(d, "#94989f")),
        save_asset("GreatEscape", "great_escape_station_cheddar_active", lambda d: escape_station(d, "#ffbf5a", active=True)),
        save_asset("GreatEscape", "great_escape_station_cocoa_active", lambda d: escape_station(d, "#8dd7ff", active=True, cocoa=True)),
        save_asset("GreatEscape", "great_escape_station_completed", lambda d: escape_station(d, "#58d26f", done=True)),
        save_asset("GreatEscape", "great_escape_station_fumble", lambda d: escape_station(d, "#f05d4f", fumble=True)),
        save_asset("GreatEscape", "great_escape_station_settle", lambda d: escape_station(d, "#63c8ff", settle=True)),
        save_asset("BlanketCatch", "blanket_catch_slack", lambda d: blanket(d, "slack")),
        save_asset("BlanketCatch", "blanket_catch_taut", lambda d: blanket(d, "taut")),
        save_asset("BlanketCatch", "blanket_catch_ripping", lambda d: blanket(d, "ripping")),
        save_asset("BlanketCatch", "blanket_catch_caught", lambda d: blanket(d, "caught")),
        save_asset("BlanketCatch", "blanket_snack_falling", lambda d: snack(d, "falling")),
        save_asset("BlanketCatch", "blanket_snack_caught", lambda d: snack(d, "caught")),
        save_asset("BlanketCatch", "blanket_snack_splat", lambda d: snack(d, "splat")),
        save_asset("KitchenFrenzy", "kitchen_counter_ready", lambda d: kitchen_counter(d, False)),
        save_asset("KitchenFrenzy", "kitchen_counter_barked", lambda d: kitchen_counter(d, True)),
        save_asset("KitchenFrenzy", "kitchen_safe_bowl_empty", lambda d: safe_bowl(d, False)),
        save_asset("KitchenFrenzy", "kitchen_safe_bowl_catch", lambda d: safe_bowl(d, True)),
        save_asset("KitchenFrenzy", "kitchen_food_good_falling", lambda d: kitchen_food(d, "good")),
        save_asset("KitchenFrenzy", "kitchen_food_bad_falling", lambda d: kitchen_food(d, "bad")),
        save_asset("KitchenFrenzy", "kitchen_food_splat", lambda d: kitchen_food(d, "splat")),
    ]
    write_readme(items)
    write_contact_sheet(items)
    print(f"Generated {len(items)} Escape/Catch/Kitchen P0 sprites in {ROOT}")


if __name__ == "__main__":
    main()
