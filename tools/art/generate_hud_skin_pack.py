#!/usr/bin/env python3
"""Generate transparent HUD skin sprites for ArenaFinal.

These sprites replace flat IMGUI box/white-rectangle presentation for the
mission picker, briefing, end cards, and playtest overlay while keeping the
existing GameManager-driven text and input hitboxes authoritative.
"""

from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path("unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/UI/Hud")
REF_ROOT = Path("unity/CheddarAndCocoa/Assets/Art/ReferenceOnly/GeneratedHudSkin")
SIZE = 512


def rgba(hex_value: str, alpha: int = 255) -> tuple[int, int, int, int]:
    hex_value = hex_value.lstrip("#")
    return tuple(int(hex_value[i : i + 2], 16) for i in (0, 2, 4)) + (alpha,)


def canvas() -> tuple[Image.Image, ImageDraw.ImageDraw]:
    image = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    return image, ImageDraw.Draw(image)


def shadow(draw: ImageDraw.ImageDraw, xy=(42, 64, 470, 448), alpha=78):
    draw.rounded_rectangle(xy, radius=44, fill=(0, 0, 0, alpha))


def paw(draw: ImageDraw.ImageDraw, cx: int, cy: int, s: float, fill):
    r = int(13 * s)
    draw.ellipse((cx - r, cy - r, cx + r, cy + r), fill=fill)
    for ox, oy, tr in [(-16, -17, 6), (-5, -24, 5), (7, -24, 5), (18, -17, 6)]:
        rr = int(tr * s)
        x = int(cx + ox * s)
        y = int(cy + oy * s)
        draw.ellipse((x - rr, y - rr, x + rr, y + rr), fill=fill)


def panel_frame(draw: ImageDraw.ImageDraw):
    shadow(draw)
    draw.rounded_rectangle((54, 44, 458, 424), radius=38, fill=rgba("#18333a", 232),
                           outline=rgba("#f6d86d"), width=12)
    draw.rounded_rectangle((78, 74, 434, 394), radius=26, outline=rgba("#78d8df", 170), width=6)
    draw.rectangle((78, 102, 434, 164), fill=rgba("#24515b", 170))
    draw.arc((72, 58, 438, 402), 204, 334, fill=rgba("#ffffff", 42), width=10)
    for x, y, c in [(104, 98, "#ffbf54"), (384, 104, "#7de2e0"), (404, 360, "#ff6f58")]:
        paw(draw, x, y, 0.85, rgba(c, 185))


def mission_tile(draw: ImageDraw.ImageDraw):
    shadow(draw, (44, 156, 468, 362), 48)
    draw.rounded_rectangle((56, 142, 456, 346), radius=30, fill=rgba("#18333a", 224),
                           outline=rgba("#5fa9b0", 210), width=9)
    draw.rounded_rectangle((78, 164, 188, 322), radius=24, fill=rgba("#f6d86d", 120),
                           outline=rgba("#f6d86d", 210), width=6)
    draw.line((214, 192, 420, 192), fill=rgba("#f5f1dc", 130), width=14)
    draw.line((214, 236, 392, 236), fill=rgba("#f5f1dc", 90), width=10)
    draw.line((214, 282, 414, 282), fill=rgba("#78d8df", 105), width=10)
    paw(draw, 132, 250, 1.2, rgba("#f5f1dc", 175))


def mission_tile_selected(draw: ImageDraw.ImageDraw):
    shadow(draw, (36, 146, 476, 372), 62)
    draw.rounded_rectangle((48, 132, 464, 356), radius=34, fill=rgba("#213d42", 236),
                           outline=rgba("#ffd45e"), width=13)
    draw.rounded_rectangle((76, 162, 192, 324), radius=24, fill=rgba("#ffd45e", 165),
                           outline=rgba("#fff2a8", 235), width=7)
    draw.line((218, 188, 424, 188), fill=rgba("#fff2a8", 170), width=16)
    draw.line((218, 236, 398, 236), fill=rgba("#f5f1dc", 120), width=11)
    draw.line((218, 286, 426, 286), fill=rgba("#78d8df", 140), width=11)
    for x in [68, 444]:
        draw.polygon([(x, 244), (x + (28 if x < 256 else -28), 220), (x + (28 if x < 256 else -28), 268)],
                     fill=rgba("#ff6f58", 220), outline=rgba("#5b241c"))
    paw(draw, 134, 250, 1.25, rgba("#18333a", 210))


def badge_frame(draw: ImageDraw.ImageDraw):
    draw.ellipse((88, 68, 424, 404), fill=rgba("#f6d86d", 224), outline=rgba("#58391f"), width=14)
    draw.ellipse((130, 110, 382, 362), fill=rgba("#18333a", 236), outline=rgba("#78d8df"), width=9)
    for angle, x, y in [(0, 256, 90), (1, 394, 244), (2, 256, 382), (3, 118, 244)]:
        paw(draw, x, y, 0.8, rgba("#ff6f58" if angle % 2 else "#78d8df", 190))


def button_primary(draw: ImageDraw.ImageDraw):
    shadow(draw, (50, 178, 462, 344), 54)
    draw.rounded_rectangle((62, 162, 450, 326), radius=36, fill=rgba("#ffbf54", 235),
                           outline=rgba("#5d3619"), width=10)
    draw.rounded_rectangle((88, 188, 424, 300), radius=26, fill=rgba("#fff3b4", 112),
                           outline=rgba("#ffffff", 70), width=5)
    draw.polygon([(104, 244), (150, 214), (150, 274)], fill=rgba("#18333a", 210))
    draw.polygon([(408, 244), (362, 214), (362, 274)], fill=rgba("#18333a", 210))
    draw.line((178, 244, 334, 244), fill=rgba("#18333a", 115), width=14)


def overlay_panel(draw: ImageDraw.ImageDraw):
    shadow(draw, (52, 58, 460, 456), 68)
    draw.rounded_rectangle((66, 42, 446, 438), radius=30, fill=rgba("#111c23", 220),
                           outline=rgba("#78d8df", 190), width=8)
    for y in range(100, 382, 48):
        draw.line((92, y, 420, y), fill=rgba("#78d8df", 58), width=5)
    draw.rounded_rectangle((88, 70, 424, 108), radius=16, fill=rgba("#213d42", 210),
                           outline=rgba("#f6d86d", 140), width=4)
    paw(draw, 396, 88, 0.6, rgba("#f6d86d", 160))


DRAWERS = {
    "hud_badge_frame": badge_frame,
    "hud_button_primary": button_primary,
    "hud_mission_tile": mission_tile,
    "hud_mission_tile_selected": mission_tile_selected,
    "hud_overlay_panel": overlay_panel,
    "hud_panel_frame": panel_frame,
}


def write_readme():
    REF_ROOT.mkdir(parents=True, exist_ok=True)
    names = "\n".join(f"- `{name}.png`" for name in sorted(DRAWERS))
    (REF_ROOT / "README.md").write_text(
        "# Generated HUD Skin Pack\n\n"
        "Deterministic transparent cartoon HUD skin sprites generated by "
        "`tools/art/generate_hud_skin_pack.py`. Runtime IMGUI text and button hitboxes remain "
        "authoritative, but major mission-select, briefing, end-card, session-summary, and playtest "
        "overlay surfaces use these sprites instead of flat white-rectangle boxes. These are "
        "couch-test UI assets, not a final hand-authored menu system.\n\n"
        f"{names}\n",
        encoding="utf-8",
    )


def write_contact_sheet(images: list[tuple[str, Image.Image]]):
    REF_ROOT.mkdir(parents=True, exist_ok=True)
    cell = 192
    cols = 3
    rows = 2
    sheet = Image.new("RGBA", (cell * cols, cell * rows), (246, 240, 228, 255))
    for i, (_name, image) in enumerate(images):
        thumb = image.resize((cell, cell), Image.Resampling.LANCZOS)
        sheet.alpha_composite(thumb, ((i % cols) * cell, (i // cols) * cell))
    sheet.convert("RGB").save(REF_ROOT / "hud_skin_pack_contact_sheet.png", quality=95)


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
    print(f"Generated {len(generated)} HUD skin sprites in {ROOT}")


if __name__ == "__main__":
    main()
