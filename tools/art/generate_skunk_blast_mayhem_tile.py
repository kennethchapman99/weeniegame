#!/usr/bin/env python3
"""Generate the greybox mission-select picture tile for Skunk Blast Mayhem.

Per docs/GAMEPLAY-FIRST-GREYBOX-PIVOT.md, realistic/painterly art is deferred; this writes a
flat, deterministic placeholder directly to the production mission-tile path so the mission is
selectable and passes the MissionSelect_HasPictureTileAndInstructionsForEveryMission contract
test (every GameManager.MissionVariant needs a non-white-square tile at
ArenaFinal/UI/MissionTiles/<variant>.png). Swap this file for authored art later without any
code changes - FinalGameplayArt.MissionTilePath keys purely off the lowercased enum name.
"""

from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw

OUT = Path("unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/UI/MissionTiles/skunkblastmayhem.png")
SIZE = 1254


def long_dog(d: ImageDraw.ImageDraw, x: float, y: float, s: float, body_rgb: tuple[int, int, int], mouth_up: bool) -> None:
    """Exaggerated dachshund silhouette, matching the style used by other generated tiles."""
    d.rounded_rectangle([x - s * 1.7, y - s * 0.55, x + s * 1.5, y + s * 0.45], radius=int(s * 0.45), fill=body_rgb)
    for lx in (x - s * 1.35, x - s * 0.55, x + s * 0.45, x + s * 1.15):
        d.rectangle([lx, y + s * 0.4, lx + s * 0.28, y + s * 0.95], fill=body_rgb)
    hx, hy = x + s * 1.75, y - s * (0.85 if mouth_up else 0.6)
    d.polygon([(x + s * 1.1, y - s * 0.5), (hx, hy), (x + s * 1.5, y + s * 0.3)], fill=body_rgb)
    d.ellipse([hx - s * 0.62, hy - s * 0.6, hx + s * 0.62, hy + s * 0.6], fill=body_rgb)
    d.rounded_rectangle([hx + s * 0.2, hy - s * 0.24, hx + s * 1.1, hy + s * 0.22], radius=int(s * 0.2), fill=body_rgb)
    d.ellipse([hx - s * 0.72, hy - s * 0.35, hx - s * 0.18, hy + s * 0.55],
              fill=tuple(max(0, c - 36) for c in body_rgb))
    d.ellipse([hx + s * 0.08, hy - s * 0.3, hx + s * 0.26, hy - s * 0.12], fill=(30, 24, 20))
    d.polygon([(x - s * 1.7, y - s * 0.4), (x - s * 2.25, y - s * 1.0), (x - s * 1.45, y - s * 0.15)], fill=body_rgb)


def main() -> None:
    img = Image.new("RGB", (SIZE, SIZE), (40, 28, 48))
    d = ImageDraw.Draw(img)

    # Dusk sky gradient (skunks come out at twilight).
    for y in range(int(SIZE * 0.78)):
        t = y / (SIZE * 0.78)
        d.line([(0, y), (SIZE, y)], fill=(int(40 + 30 * t), int(28 + 20 * t), int(58 + 40 * t)))

    # Lawn.
    d.rectangle([0, int(SIZE * 0.78), SIZE, SIZE], fill=(48, 84, 46))
    d.rectangle([0, int(SIZE * 0.78), SIZE, int(SIZE * 0.80)], fill=(38, 70, 38))

    # Skunk, front and center: black body, bold white stripe, tail up mid-telegraph.
    sx, sy = SIZE * 0.60, SIZE * 0.70
    s = SIZE * 0.10
    d.ellipse([sx - s * 1.5, sy - s * 0.9, sx + s * 1.1, sy + s * 0.7], fill=(18, 16, 18))
    d.polygon([(sx - s * 0.3, sy - s * 0.85), (sx + s * 0.5, sy - s * 0.9), (sx + s * 0.1, sy + s * 0.55)],
              fill=(240, 238, 230))
    d.ellipse([sx - s * 1.85, sy - s * 0.45, sx - s * 1.15, sy + s * 0.25], fill=(20, 18, 20))
    d.ellipse([sx - s * 1.7, sy - s * 0.3, sx - s * 1.45, sy - s * 0.1], fill=(30, 26, 26))
    # Tail lifted straight up - the telegraph.
    d.polygon([(sx + s * 1.0, sy - s * 0.2), (sx + s * 1.55, sy - s * 2.1), (sx + s * 1.9, sy - s * 2.15),
               (sx + s * 1.35, sy - s * 0.05)], fill=(24, 20, 22))
    d.polygon([(sx + s * 1.15, sy - s * 1.0), (sx + s * 1.75, sy - s * 2.05), (sx + s * 1.55, sy - s * 0.85)],
               fill=(235, 232, 224))
    # Warning burst ticks around the tail tip.
    for k in range(3):
        a = 60 + k * 25
        import math
        rx = sx + s * 1.7 + math.cos(math.radians(a)) * s * 0.6
        ry = sy - s * 2.1 + math.sin(math.radians(a)) * s * 0.6
        d.ellipse([rx - 8, ry - 8, rx + 8, ry + 8], fill=(255, 226, 90))

    # The prized dead bird, guarded just behind the skunk.
    bx, by = SIZE * 0.66, SIZE * 0.755
    d.ellipse([bx - s * 0.5, by - s * 0.28, bx + s * 0.5, by + s * 0.22], fill=(70, 58, 46))
    d.polygon([(bx + s * 0.42, by - s * 0.02), (bx + s * 0.75, by + s * 0.05), (bx + s * 0.4, by + s * 0.16)],
              fill=(200, 170, 60))

    # Laundry basket, lower-left: the de-skunk supply line.
    lx, ly = SIZE * 0.16, SIZE * 0.86
    lw, lh = s * 1.1, s * 0.62
    d.rectangle([lx - lw, ly - lh, lx + lw, ly + lh], fill=(196, 150, 92))
    for i in range(4):
        xx = lx - lw + (i + 0.5) * (2 * lw / 4)
        d.line([(xx, ly - lh), (xx, ly + lh)], fill=(120, 84, 44), width=10)
    d.ellipse([lx - lw * 0.7, ly - lh * 1.3, lx + lw * 0.7, ly - lh * 0.6], fill=(230, 230, 240))

    # Cheddar (gold) barking loud to hold the skunk's attention; Cocoa (chocolate) sneaking low.
    long_dog(d, SIZE * 0.34, SIZE * 0.90, SIZE * 0.075, (222, 160, 64), mouth_up=True)
    long_dog(d, SIZE * 0.80, SIZE * 0.90, SIZE * 0.07, (108, 70, 46), mouth_up=False)

    # Cheddar's bark burst aimed at the skunk.
    cx, cy = SIZE * 0.45, SIZE * 0.80
    for k in range(3):
        r = SIZE * (0.03 + 0.022 * k)
        d.arc([cx - r, cy - r, cx + r, cy + r], start=200, end=320, fill=(255, 244, 180), width=12)

    OUT.parent.mkdir(parents=True, exist_ok=True)
    img.save(OUT)
    print(f"wrote {OUT}")


if __name__ == "__main__":
    main()
