#!/usr/bin/env python3
"""Export the authored Car Ride backseat art pack.

Tracked source masters live in ``Assets/Art/ReferenceOnly/GeneratedCarBackseat/Sources``.
This deterministic derivation step removes chroma keys, aligns the windshield opening,
builds the contract-sensitive seamless scenery, writes the seven runtime PNGs, and
produces after plus before/after review sheets.

The scenery is intentionally code-authored: its first and last columns are identical,
and every pixel is opaque, because Unity uses the sprite alpha as its windshield mask.
"""

from __future__ import annotations

import math
from pathlib import Path

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter


PROJECT = Path(__file__).resolve().parents[2]
ROOT = PROJECT / "unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props/CarRide"
TILE_ROOT = PROJECT / "unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/UI/MissionTiles"
REF_ROOT = PROJECT / "unity/CheddarAndCocoa/Assets/Art/ReferenceOnly/GeneratedCarBackseat"
SOURCE_ROOT = REF_ROOT / "Sources"

INK = (51, 37, 28, 255)


def cover_crop(image: Image.Image, size: tuple[int, int]) -> Image.Image:
    """Resize without distortion and crop evenly to the requested canvas."""
    image = image.convert("RGBA")
    target_w, target_h = size
    scale = max(target_w / image.width, target_h / image.height)
    resized = image.resize((round(image.width * scale), round(image.height * scale)), Image.Resampling.LANCZOS)
    left = (resized.width - target_w) // 2
    top = (resized.height - target_h) // 2
    return resized.crop((left, top, left + target_w, top + target_h))


def chroma_alpha(image: Image.Image) -> Image.Image:
    """Remove the generated green screen with a soft matte and conservative despill."""
    source = image.convert("RGBA")
    output: list[tuple[int, int, int, int]] = []
    for r, g, b, _ in source.getdata():
        dominance = g - max(r, b)
        if g > 145 and dominance > 35:
            # The generated background is highly saturated. A 35..105 ramp keeps
            # antialiased #33251c outlines while eliminating green edge halos.
            alpha = round(255 * max(0.0, min(1.0, (105 - dominance) / 70)))
            g = min(g, max(r, b) + 14)
        else:
            alpha = 255
        output.append((r, g, b, alpha))
    source.putdata(output)
    return source


def fit_subject(image: Image.Image, size: tuple[int, int], padding: int) -> Image.Image:
    """Trim a keyed subject and fit it into a stable transparent runtime canvas."""
    bbox = image.getchannel("A").getbbox()
    if bbox is None:
        raise ValueError("Chroma removal produced an empty subject")
    subject = image.crop(bbox)
    available = (size[0] - padding * 2, size[1] - padding * 2)
    subject.thumbnail(available, Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", size, (0, 0, 0, 0))
    canvas.alpha_composite(subject, ((size[0] - subject.width) // 2, (size[1] - subject.height) // 2))
    return canvas


def cabin_shell() -> Image.Image:
    image = cover_crop(Image.open(SOURCE_ROOT / "cabin_shell_master.png"), (1280, 768))
    # A deliberately simple opening matches the fixed 44 x 5.8 world-unit mask.
    # The dark lip makes the new opening feel authored into the source cabin rather
    # than looking like a rectangle punched out after the fact.
    frame = Image.new("RGBA", image.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(frame)
    d.rounded_rectangle((104, 24, 1176, 194), radius=42, fill=(35, 30, 27, 255), outline=INK, width=12)
    d.rounded_rectangle((122, 40, 1158, 178), radius=30, fill=(103, 143, 153, 255), outline=(93, 66, 46, 255), width=7)
    image.alpha_composite(frame)

    alpha = image.getchannel("A")
    mask = Image.new("L", image.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle((128, 45, 1152, 173), radius=26, fill=255)
    # Feather only the antialiased edge; the broad opening stays truly transparent.
    mask = mask.filter(ImageFilter.GaussianBlur(0.65))
    alpha_pixels = [max(0, a - m) for a, m in zip(alpha.getdata(), mask.getdata())]
    alpha.putdata(alpha_pixels)
    image.putalpha(alpha)

    # Camera-coverage contract: every outside edge is fully opaque.
    pixels = image.load()
    for x in range(image.width):
        pixels[x, 0] = (*pixels[x, 0][:3], 255)
        pixels[x, image.height - 1] = (*pixels[x, image.height - 1][:3], 255)
    for y in range(image.height):
        pixels[0, y] = (*pixels[0, y][:3], 255)
        pixels[image.width - 1, y] = (*pixels[image.width - 1, y][:3], 255)
    return image


def authored_subject(source_name: str, size: tuple[int, int], padding: int) -> Image.Image:
    return fit_subject(chroma_alpha(Image.open(SOURCE_ROOT / source_name)), size, padding)


def bench_sprite() -> Image.Image:
    """Calm the leather into a midtone lane so both gold and chocolate dogs read."""
    bench = authored_subject("bench_chroma_master.png", (1280, 384), 10)
    alpha = bench.getchannel("A")
    bench = ImageEnhance.Color(bench).enhance(0.72)
    bench = ImageEnhance.Brightness(bench).enhance(0.84)
    bench = ImageEnhance.Contrast(bench).enhance(0.88)
    bench.putalpha(alpha)
    return bench


def windshield_scenery() -> Image.Image:
    """Create a fully opaque, horizontally seamless parallax neighborhood strip."""
    scale = 2
    w, h = 1280 * scale, 256 * scale
    image = Image.new("RGB", (w, h), (156, 210, 224))
    d = ImageDraw.Draw(image)

    # Vertical-only gradients tile perfectly horizontally.
    for y in range(h):
        if y < 292:
            t = y / 292
            color = (round(133 + 52 * t), round(198 + 34 * t), round(226 + 12 * t))
        elif y < 350:
            t = (y - 292) / 58
            color = (round(108 - 15 * t), round(151 - 18 * t), round(95 - 20 * t))
        else:
            t = (y - 350) / (h - 350)
            color = (round(91 - 19 * t), round(98 - 20 * t), round(101 - 22 * t))
        d.line((0, y, w, y), fill=color)

    # Soft periodic cloud streaks and distant tree canopy add depth without breaking seams.
    cloud = Image.new("RGBA", image.size, (0, 0, 0, 0))
    cd = ImageDraw.Draw(cloud)
    for x, y, rx, ry in ((230, 72, 210, 34), (920, 108, 260, 42), (1740, 66, 230, 36)):
        cd.ellipse((x - rx, y - ry, x + rx, y + ry), fill=(244, 244, 220, 65))
    cloud = cloud.filter(ImageFilter.GaussianBlur(18))
    image = Image.alpha_composite(image.convert("RGBA"), cloud)
    d = ImageDraw.Draw(image)

    # Back row: smaller desaturated homes.
    for x, body, roof in ((90, "#c58c69", "#7b4b43"), (590, "#d8b07c", "#586f72"),
                          (1120, "#b98273", "#6a4b59"), (1680, "#d2a46d", "#70544a"),
                          (2180, "#c88f72", "#526c70")):
        x *= scale
        base_y = 310
        d.rectangle((x, base_y - 92, x + 178, base_y), fill=body, outline=INK[:3], width=7)
        d.polygon((x - 18, base_y - 92, x + 89, base_y - 164, x + 196, base_y - 92),
                  fill=roof, outline=INK[:3])
        for wx in (x + 30, x + 116):
            d.rounded_rectangle((wx, base_y - 70, wx + 34, base_y - 28), radius=4,
                                fill="#bde1e4", outline=INK[:3], width=4)
        d.rectangle((x + 74, base_y - 62, x + 108, base_y), fill="#704b38", outline=INK[:3], width=4)

    # Trees sit in front of houses; none touches an edge, so the boundary remains pure background.
    for x, radius, green in ((410, 58, "#4f8b55"), (980, 72, "#5d9654"),
                             (1500, 62, "#3f7d4c"), (2050, 76, "#568d49"), (2420, 54, "#477f50")):
        d.rounded_rectangle((x - 11, 238, x + 11, 330), radius=6, fill="#76513c")
        d.ellipse((x - radius, 154, x + radius, 154 + radius * 2), fill=green, outline=INK[:3], width=6)
        d.ellipse((x - radius // 2, 132, x + radius // 2, 230), fill="#77ab5b")

    # Near verge, curb, and road have enough texture to imply speed while remaining quiet.
    d.rectangle((0, 310, w, 352), fill="#779452")
    d.rectangle((0, 346, w, 366), fill="#d8c9a5")
    d.rectangle((0, 366, w, h), fill="#535b63")
    d.line((0, 374, w, 374), fill="#33251c", width=8)
    for x in range(96, w, 320):
        d.rounded_rectangle((x, 432, x + 138, 451), radius=9, fill="#f3e4ba")
    for x in range(44, w, 180):
        shade = 87 + round(8 * math.sin(2 * math.pi * x / w))
        d.line((x, 390, x + 90, 506), fill=(shade, shade + 5, shade + 8, 75), width=3)

    image = image.convert("RGB").resize((1280, 256), Image.Resampling.LANCZOS).convert("RGBA")
    # Hard guarantees consumed by BuildWindshieldScroller/SpriteMask.
    pixels = image.load()
    for y in range(image.height):
        pixels[image.width - 1, y] = pixels[0, y]
    image.putalpha(Image.new("L", image.size, 255))
    return ImageEnhance.Color(image).enhance(1.04)


def thumbnail(image: Image.Image, size: tuple[int, int]) -> Image.Image:
    thumb = image.copy().convert("RGBA")
    checker = Image.new("RGBA", thumb.size, (239, 235, 225, 255))
    cd = ImageDraw.Draw(checker)
    step = 32
    for y in range(0, checker.height, step):
        for x in range(0, checker.width, step):
            if (x // step + y // step) % 2:
                cd.rectangle((x, y, x + step, y + step), fill=(219, 225, 220, 255))
    checker.alpha_composite(thumb)
    checker.thumbnail(size, Image.Resampling.LANCZOS)
    return checker


def contact_sheet(images: list[tuple[str, Image.Image]]) -> Image.Image:
    cell_w, cell_h, cols = 430, 290, 3
    rows = math.ceil(len(images) / cols)
    sheet = Image.new("RGBA", (cell_w * cols, cell_h * rows), (246, 243, 235, 255))
    d = ImageDraw.Draw(sheet)
    for index, (name, image) in enumerate(images):
        x = (index % cols) * cell_w
        y = (index // cols) * cell_h
        thumb = thumbnail(image, (cell_w - 26, cell_h - 54))
        sheet.alpha_composite(thumb, (x + (cell_w - thumb.width) // 2, y + 10))
        d.rounded_rectangle((x + 9, y + cell_h - 39, x + cell_w - 9, y + cell_h - 9),
                            radius=10, fill=(51, 37, 28, 235))
        d.text((x + 18, y + cell_h - 32), name, fill=(255, 247, 224, 255))
    return sheet


def before_after(after: Image.Image) -> Image.Image:
    before = Image.open(REF_ROOT / "car_backseat_pack_before.png").convert("RGBA")
    old_tile = Image.open(REF_ROOT / "carride_tile_before.png").convert("RGBA")
    old_tile.thumbnail((340, 340), Image.Resampling.LANCZOS)
    before_panel = Image.new("RGBA", after.size, (246, 243, 235, 255))
    before.thumbnail((after.width, after.height - 100), Image.Resampling.LANCZOS)
    before_panel.alpha_composite(before, ((after.width - before.width) // 2, 64))
    before_panel.alpha_composite(old_tile, (after.width - old_tile.width - 22, after.height - old_tile.height - 12))

    gutter = 26
    combined = Image.new("RGBA", (after.width * 2 + gutter, after.height + 54), (40, 34, 29, 255))
    combined.alpha_composite(before_panel, (0, 54))
    combined.alpha_composite(after, (after.width + gutter, 54))
    d = ImageDraw.Draw(combined)
    d.text((20, 18), "BEFORE - placeholder shape primitives / old balance tile", fill=(255, 231, 178, 255))
    d.text((after.width + gutter + 20, 18), "AFTER - authored backseat chaos pack", fill=(174, 239, 207, 255))
    return combined


def validate(outputs: dict[str, Image.Image]) -> None:
    expected = {
        "backseat_cabin_shell": (1280, 768), "backseat_bench": (1280, 384),
        "backseat_windshield_scenery": (1280, 256), "car_dashboard_driver": (512, 384),
        "seat_cooler": (512, 512), "seat_toy_bin": (512, 512), "carride": (1254, 1254),
    }
    for name, image in outputs.items():
        if image.size != expected[name]:
            raise ValueError(f"{name}: expected {expected[name]}, got {image.size}")
    scenery = outputs["backseat_windshield_scenery"].convert("RGBA")
    if scenery.getchannel("A").getextrema() != (255, 255):
        raise ValueError("Windshield scenery must be fully opaque")
    if list(scenery.crop((0, 0, 1, scenery.height)).getdata()) != list(
            scenery.crop((scenery.width - 1, 0, scenery.width, scenery.height)).getdata()):
        raise ValueError("Windshield scenery first/last columns must match exactly")
    cabin = outputs["backseat_cabin_shell"].convert("RGBA")
    edge_alpha = list(cabin.crop((0, 0, cabin.width, 1)).getchannel("A").getdata())
    edge_alpha += list(cabin.crop((0, cabin.height - 1, cabin.width, cabin.height)).getchannel("A").getdata())
    edge_alpha += list(cabin.crop((0, 0, 1, cabin.height)).getchannel("A").getdata())
    edge_alpha += list(cabin.crop((cabin.width - 1, 0, cabin.width, cabin.height)).getchannel("A").getdata())
    if min(edge_alpha) != 255:
        raise ValueError("Cabin outer edge must be opaque to prevent backyard bleed")


def main() -> None:
    ROOT.mkdir(parents=True, exist_ok=True)
    REF_ROOT.mkdir(parents=True, exist_ok=True)
    outputs = {
        "backseat_cabin_shell": cabin_shell(),
        "backseat_bench": bench_sprite(),
        "backseat_windshield_scenery": windshield_scenery(),
        "car_dashboard_driver": authored_subject("driver_chroma_master.png", (512, 384), 8),
        "seat_cooler": authored_subject("cooler_chroma_master.png", (512, 512), 16),
        "seat_toy_bin": authored_subject("toy_bin_chroma_master.png", (512, 512), 16),
        "carride": cover_crop(Image.open(SOURCE_ROOT / "carride_tile_master.png"), (1254, 1254)).convert("RGB"),
    }
    validate(outputs)

    for name, image in outputs.items():
        path = TILE_ROOT / "carride.png" if name == "carride" else ROOT / f"{name}.png"
        image.save(path, optimize=True)

    sheet_items = [(name, outputs[name]) for name in (
        "backseat_cabin_shell", "backseat_bench", "backseat_windshield_scenery",
        "car_dashboard_driver", "seat_cooler", "seat_toy_bin", "carride")]
    after = contact_sheet(sheet_items)
    after.save(REF_ROOT / "car_backseat_pack_contact_sheet.png", optimize=True)
    before_after(after).save(REF_ROOT / "car_backseat_before_after_contact_sheet.png", optimize=True)
    print("Exported 7 Car Ride assets; opacity, edge coverage, dimensions, and seamless wrap validated.")


if __name__ == "__main__":
    main()
