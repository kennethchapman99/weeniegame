#!/usr/bin/env python3
"""Generate transparent indoor floor plates for V3.4's staging audit.

Table Stealth (dining room), Chaos Machine and Thunderstorm Comfort (living room,
shared pack since both missions' briefing art depicts the same cozy den) currently
have no MissionLevelAreaArt plate, so their gameplay plays out on the bare backyard
lawn despite an indoor fantasy. This mirrors kitchen_floor_area.png's exact grammar
(1024x768 rounded-rect floor, thick outline, plank/tile lines, small silhouette
accents) rather than inventing a new visual language - see
Assets/Art/ReferenceOnly/GeneratedLevelAreas/README.md for the established pack.
"""

from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path("unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props/LevelAreas")
REF_ROOT = Path("unity/CheddarAndCocoa/Assets/Art/ReferenceOnly/GeneratedLevelAreas")
SIZE = (1024, 768)
OUTLINE = 14
POLISHED_LIVING_ROOM_SOURCE = REF_ROOT / "livingroom_floor_area_storybook_v02.png"
POLISHED_DINING_ROOM_SOURCE = REF_ROOT / "diningroom_floor_area_storybook_v02.png"
POLISHED_KITCHEN_SOURCE = REF_ROOT / "kitchen_floor_area_storybook_v02.png"
POLISHED_KITCHEN_COUNTER_SOURCE = REF_ROOT / "kitchen_counter_wall_storybook_v02.png"


def rgba(hex_value: str, alpha: int = 255) -> tuple[int, int, int, int]:
    hex_value = hex_value.lstrip("#")
    return tuple(int(hex_value[i : i + 2], 16) for i in (0, 2, 4)) + (alpha,)


def canvas() -> tuple[Image.Image, ImageDraw.ImageDraw]:
    image = Image.new("RGBA", SIZE, (0, 0, 0, 0))
    return image, ImageDraw.Draw(image)


def floor_base(draw: ImageDraw.ImageDraw, fill: str, outline: str):
    draw.rounded_rectangle(
        (10, 10, SIZE[0] - 10, SIZE[1] - 10), radius=60,
        fill=rgba(fill), outline=rgba(outline), width=OUTLINE,
    )


def wood_planks(draw: ImageDraw.ImageDraw, seam: str, count: int = 7):
    # Vertical plank seams read as "wood floor" at a glance, distinct from Kitchen's
    # square tile grid - the silhouette-first cue that separates rooms without text.
    step = (SIZE[0] - 20) / count
    for i in range(1, count):
        x = 10 + step * i
        draw.line((x, 30, x, SIZE[1] - 30), fill=rgba(seam, 110), width=5)


def rug(draw: ImageDraw.ImageDraw, fill: str, outline: str, box):
    draw.rounded_rectangle(box, radius=48, fill=rgba(fill), outline=rgba(outline), width=8)


def diningroom_floor() -> Image.Image:
    image, draw = canvas()
    floor_base(draw, "#c98a4a", "#5c3418")
    wood_planks(draw, "#8a5a2c")
    # Dining rug: warm burgundy, centered where a table would sit.
    rug(draw, "#8b3a3a", "#4a1e1e", (232, 218, 792, 550))
    for x, y in ((150, 150), (874, 150), (150, 618), (874, 618)):
        draw.ellipse((x - 22, y - 22, x + 22, y + 22), fill=rgba("#5c3418", 70))
    return image


def livingroom_floor() -> Image.Image:
    image, draw = canvas()
    floor_base(draw, "#a97c50", "#4f341c")
    wood_planks(draw, "#7c5530")
    # Living-room rug: cool teal (Cocoa's cool accent family per ART-DIRECTION.md),
    # distinct from the dining room's warm rug so the two rooms silhouette apart.
    rug(draw, "#3f8a9c", "#1f4a54", (212, 200, 812, 568))
    # A low bookshelf/couch-base silhouette along one long edge for room context.
    draw.rounded_rectangle((60, 40, 400, 112), radius=20, fill=rgba("#5c3a20"), outline=rgba("#2c1a0c"), width=8)
    for x in range(96, 380, 62):
        draw.line((x, 50, x, 102), fill=rgba("#2c1a0c", 140), width=4)
    return image


def save(image: Image.Image, name: str):
    ROOT.mkdir(parents=True, exist_ok=True)
    REF_ROOT.mkdir(parents=True, exist_ok=True)
    out = ROOT / f"{name}.png"
    image.save(out)
    image.save(REF_ROOT / f"{name}.png")
    print(f"wrote {out}")

def polished_or_fallback(source: Path, fallback: Image.Image) -> Image.Image:
    if not source.exists():
        return fallback
    polished = Image.open(source).convert("RGBA")
    return polished.resize(SIZE, Image.Resampling.LANCZOS) if polished.size != SIZE else polished


def main():
    save(polished_or_fallback(POLISHED_DINING_ROOM_SOURCE, diningroom_floor()),
         "diningroom_floor_area")
    # The deterministic geometry remains a safe fallback, but the runtime den now promotes the
    # reviewed storybook paintover when it is present. Keeping the approved source beside the
    # generated pack makes regeneration repeatable instead of silently reverting to draft blocks.
    save(polished_or_fallback(POLISHED_LIVING_ROOM_SOURCE, livingroom_floor()),
         "livingroom_floor_area")
    if POLISHED_KITCHEN_SOURCE.exists():
        save(polished_or_fallback(POLISHED_KITCHEN_SOURCE, Image.new("RGBA", SIZE)),
             "kitchen_floor_area")
    if POLISHED_KITCHEN_COUNTER_SOURCE.exists():
        save(polished_or_fallback(POLISHED_KITCHEN_COUNTER_SOURCE, Image.new("RGBA", SIZE)),
             "kitchen_counter_wall")


if __name__ == "__main__":
    main()
