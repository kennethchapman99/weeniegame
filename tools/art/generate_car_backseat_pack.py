#!/usr/bin/env python3
"""Generate the Car Ride backseat interior pack for ArenaFinal.

Replaces the retired car-silhouette balance sprites (car_ride_level/lurch/spill
and the LevelAreas car interior plates) with a proper backseat stage set:

- backseat_cabin_shell      big framing plate: doors, windshield band, rear shelf
- backseat_bench            the leather bench lane the dogs ride on
- backseat_windshield_scenery  scrolling road/houses strip shown through the windshield
- car_dashboard_driver      driver + wheel + mirror actor art (telegraphs road events)
- seat_cooler               sliding obstacle: picnic cooler
- seat_toy_bin              sliding obstacle: toy bin

Deterministic placeholder art in the same flat-cartoon language as
generate_mission_prop_pack.py: solid fills, #33251c outlines, soft shadows.
"""

from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path("unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props/CarRide")
REF_ROOT = Path("unity/CheddarAndCocoa/Assets/Art/ReferenceOnly/GeneratedCarBackseat")

INK = "#33251c"


def rgba(hex_value: str, alpha: int = 255) -> tuple[int, int, int, int]:
    hex_value = hex_value.lstrip("#")
    return tuple(int(hex_value[i : i + 2], 16) for i in (0, 2, 4)) + (alpha,)


def canvas(width: int, height: int) -> tuple[Image.Image, ImageDraw.ImageDraw]:
    image = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    return image, ImageDraw.Draw(image)


def cabin_shell() -> Image.Image:
    w, h = 1280, 768
    image, d = canvas(w, h)
    # Cabin floor fills everything inside the body shell.
    d.rounded_rectangle((8, 8, w - 8, h - 8), radius=64, fill=rgba("#565d66"),
                        outline=rgba(INK), width=14)
    # Windshield band across the top (the scenery strip renders over this opening).
    d.rounded_rectangle((120, 34, w - 120, 178), radius=36, fill=rgba("#9fd4e6"),
                        outline=rgba(INK), width=12)
    # A-pillars framing the windshield.
    d.polygon((120, 34, 60, 178, 132, 178), fill=rgba("#3d434b"))
    d.polygon((w - 120, 34, w - 60, 178, w - 132, 178), fill=rgba("#3d434b"))
    # Front-seat backs with headrests: the row the dogs ride behind.
    for cx in (352, 928):
        d.rounded_rectangle((cx - 190, 196, cx + 190, 268), radius=30,
                            fill=rgba("#7a5b48"), outline=rgba(INK), width=10)
        d.rounded_rectangle((cx - 70, 150, cx + 70, 208), radius=24,
                            fill=rgba("#8a684f"), outline=rgba(INK), width=10)
    # Center console gap between the front seats.
    d.rounded_rectangle((600, 206, 680, 262), radius=16, fill=rgba("#41474f"),
                        outline=rgba(INK), width=8)
    # Door panels left/right with armrests and silver handles.
    for x0, x1 in ((22, 118), (w - 118, w - 22)):
        d.rounded_rectangle((x0, 210, x1, 640), radius=28, fill=rgba("#8f6c50"),
                            outline=rgba(INK), width=10)
        d.rounded_rectangle((x0 + 18, 360, x1 - 18, 412), radius=18, fill=rgba("#7a5b48"),
                            outline=rgba(INK), width=8)
        d.rounded_rectangle((x0 + 24, 300, x1 - 24, 332), radius=12, fill=rgba("#cfd4d9"),
                            outline=rgba(INK), width=6)
    # Rear parcel shelf band along the bottom.
    d.rounded_rectangle((60, 648, w - 60, 744), radius=30, fill=rgba("#4a4038"),
                        outline=rgba(INK), width=10)
    # Rear window sliver on the shelf.
    d.rounded_rectangle((330, 668, w - 330, 704), radius=14, fill=rgba("#7fb6c9"),
                        outline=rgba(INK), width=6)
    return image


def bench() -> Image.Image:
    w, h = 1280, 384
    image, d = canvas(w, h)
    d.ellipse((40, h - 96, w - 40, h - 24), fill=(0, 0, 0, 42))
    # Bench base and seat back lip.
    d.rounded_rectangle((16, 60, w - 16, h - 48), radius=54, fill=rgba("#b3805a"),
                        outline=rgba(INK), width=14)
    d.rounded_rectangle((16, 24, w - 16, 96), radius=36, fill=rgba("#9a6c4b"),
                        outline=rgba(INK), width=12)
    # Three cushion segments with stitched seams.
    for seam_x in (436, 852):
        d.line((seam_x, 96, seam_x, h - 60), fill=rgba(INK), width=10)
        for y in range(112, h - 68, 34):
            d.line((seam_x - 12, y, seam_x + 12, y), fill=rgba("#7d5236"), width=6)
    # Cushion sheen.
    for cx in (226, 644, 1062):
        d.ellipse((cx - 130, 128, cx + 130, 196), fill=rgba("#c99268", 130))
    # Two seatbelt buckles poking out of the seams.
    for bx in (436, 852):
        d.rounded_rectangle((bx - 30, 74, bx + 30, 122), radius=10, fill=rgba("#8c9199"),
                            outline=rgba(INK), width=8)
        d.rectangle((bx - 8, 88, bx + 8, 108), fill=rgba("#33251c"))
    return image


def windshield_scenery() -> Image.Image:
    # Two identical halves so the strip can wrap at half-width while scrolling.
    w, h = 1280, 256
    image, d = canvas(w, h)
    half = w // 2
    for offset in (0, half):
        d.rectangle((offset, 0, offset + half, 150), fill=rgba("#a5d8ea"))
        d.rectangle((offset, 150, offset + half, h), fill=rgba("#6f7d72"))
        # Road with dashes.
        d.rectangle((offset, 170, offset + half, 236), fill=rgba("#5a6068"))
        for x in range(offset + 20, offset + half, 120):
            d.rounded_rectangle((x, 196, x + 56, 210), radius=7, fill=rgba("#f4e9c9"))
        # Sun on the first tile position of each half.
        d.ellipse((offset + 60, 22, offset + 124, 86), fill=rgba("#ffd76a"),
                  outline=rgba(INK), width=6)
        # Passing houses and trees.
        for i, hx in enumerate(range(offset + 170, offset + half - 60, 150)):
            if i % 2 == 0:
                d.rectangle((hx, 84, hx + 84, 150), fill=rgba("#d9a066"),
                            outline=rgba(INK), width=6)
                d.polygon((hx - 10, 84, hx + 42, 44, hx + 94, 84), fill=rgba("#a04b3c"),
                          outline=rgba(INK))
            else:
                d.rectangle((hx + 34, 104, hx + 50, 150), fill=rgba("#7a5b48"))
                d.ellipse((hx, 40, hx + 84, 116), fill=rgba("#5f9e57"),
                          outline=rgba(INK), width=6)
    return image


def dashboard_driver() -> Image.Image:
    w, h = 512, 384
    image, d = canvas(w, h)
    # Dashboard arc.
    d.rounded_rectangle((16, 250, w - 16, 356), radius=32, fill=rgba("#41474f"),
                        outline=rgba(INK), width=10)
    # Steering wheel with hands.
    d.arc((120, 170, 392, 420), start=180, end=360, fill=rgba("#2c3138"), width=26)
    for hx in (132, 340):
        d.ellipse((hx, 224, hx + 44, 268), fill=rgba("#e8b08a"), outline=rgba(INK), width=8)
    # Driver: headrest, shoulders, back of head.
    d.rounded_rectangle((176, 196, 336, 268), radius=22, fill=rgba("#7a5b48"),
                        outline=rgba(INK), width=8)
    d.ellipse((196, 60, 316, 190), fill=rgba("#6b4a33"), outline=rgba(INK), width=10)
    d.ellipse((222, 66, 296, 118), fill=rgba("#7d5a40"))
    # Rear-view mirror with watching eyes.
    d.rounded_rectangle((160, 8, 352, 60), radius=16, fill=rgba("#cfd4d9"),
                        outline=rgba(INK), width=8)
    for ex in (216, 272):
        d.ellipse((ex, 20, ex + 28, 48), fill=rgba("#ffffff"), outline=rgba(INK), width=5)
        d.ellipse((ex + 9, 28, ex + 21, 42), fill=rgba(INK))
    return image


def seat_cooler() -> Image.Image:
    w, h = 512, 512
    image, d = canvas(w, h)
    d.ellipse((70, 400, 442, 460), fill=(0, 0, 0, 42))
    d.rounded_rectangle((84, 170, 428, 424), radius=30, fill=rgba("#2f6fb2"),
                        outline=rgba(INK), width=14)
    d.rounded_rectangle((66, 108, 446, 196), radius=26, fill=rgba("#e9eef2"),
                        outline=rgba(INK), width=14)
    d.rounded_rectangle((196, 76, 316, 122), radius=18, fill=rgba("#e9eef2"),
                        outline=rgba(INK), width=12)
    # Latch and side sheen.
    d.rounded_rectangle((236, 168, 276, 224), radius=10, fill=rgba("#cfd4d9"),
                        outline=rgba(INK), width=8)
    d.ellipse((120, 230, 220, 380), fill=rgba("#4b8ccb", 120))
    return image


def seat_toy_bin() -> Image.Image:
    w, h = 512, 512
    image, d = canvas(w, h)
    d.ellipse((70, 406, 442, 464), fill=(0, 0, 0, 42))
    # Duck and bone poking out before the bin body overlaps them.
    d.ellipse((116, 96, 232, 200), fill=rgba("#ffd447"), outline=rgba(INK), width=10)
    d.ellipse((196, 76, 252, 128), fill=rgba("#ffd447"), outline=rgba(INK), width=8)
    d.polygon((248, 94, 292, 104, 248, 118), fill=rgba("#ef8b3a"))
    for bx in (300, 396):
        d.ellipse((bx - 22, 108, bx + 22, 152), fill=rgba("#fff1cf"), outline=rgba(INK), width=8)
    d.rounded_rectangle((306, 118, 392, 142), radius=10, fill=rgba("#fff1cf"),
                        outline=rgba(INK), width=8)
    # Bin body.
    d.polygon((76, 168, 436, 168, 400, 428, 112, 428), fill=rgba("#c9473f"))
    d.polygon((76, 168, 436, 168, 400, 428, 112, 428), outline=rgba(INK), width=14)
    d.rounded_rectangle((60, 140, 452, 196), radius=22, fill=rgba("#a83a33"),
                        outline=rgba(INK), width=12)
    # Pawprint decal.
    d.ellipse((216, 260, 296, 332), fill=rgba("#f4e9c9"))
    for px, py in ((206, 236), (250, 222), (294, 236)):
        d.ellipse((px, py, px + 34, py + 34), fill=rgba("#f4e9c9"))
    return image


SPRITES = {
    "backseat_cabin_shell": cabin_shell,
    "backseat_bench": bench,
    "backseat_windshield_scenery": windshield_scenery,
    "car_dashboard_driver": dashboard_driver,
    "seat_cooler": seat_cooler,
    "seat_toy_bin": seat_toy_bin,
}


def contact_sheet(images: list[tuple[str, Image.Image]]) -> None:
    cell_w, cell_h = 340, 220
    cols = 3
    rows = (len(images) + cols - 1) // cols
    sheet = Image.new("RGBA", (cols * cell_w, rows * cell_h), (245, 248, 244, 255))
    d = ImageDraw.Draw(sheet)
    for index, (name, image) in enumerate(images):
        x = (index % cols) * cell_w
        y = (index // cols) * cell_h
        thumb = image.copy()
        thumb.thumbnail((cell_w - 24, cell_h - 48), Image.Resampling.LANCZOS)
        sheet.alpha_composite(thumb, (x + (cell_w - thumb.width) // 2, y + 10))
        d.text((x + 10, y + cell_h - 28), name, fill=(38, 38, 38, 255))
    sheet.save(REF_ROOT / "car_backseat_pack_contact_sheet.png")


def main() -> None:
    ROOT.mkdir(parents=True, exist_ok=True)
    REF_ROOT.mkdir(parents=True, exist_ok=True)
    rendered = []
    for name, drawer in SPRITES.items():
        image = drawer()
        image.save(ROOT / f"{name}.png")
        rendered.append((name, image))
    contact_sheet(rendered)
    print(f"Generated {len(rendered)} backseat sprites in {ROOT}")


if __name__ == "__main__":
    main()
