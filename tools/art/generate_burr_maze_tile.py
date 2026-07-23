#!/usr/bin/env python3
"""Generate the greybox mission-select picture tile for Burr Maze.

Per docs/GAMEPLAY-FIRST-GREYBOX-PIVOT.md, realistic/painterly art is deferred; this writes a
flat, deterministic placeholder directly to the production mission-tile path so the mission is
selectable and passes the MissionSelect_HasPictureTileAndInstructionsForEveryMission contract
test (every GameManager.MissionVariant needs a non-white-square tile at
ArenaFinal/UI/MissionTiles/<variant>.png). Swap this file for authored art later without any
code changes - FinalGameplayArt.MissionTilePath keys purely off the lowercased enum name.
"""

from __future__ import annotations

import math
from pathlib import Path

from PIL import Image, ImageDraw

OUT = Path("unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/UI/MissionTiles/burrmaze.png")
SIZE = 1254


def long_dog(d: ImageDraw.ImageDraw, x: float, y: float, s: float, body_rgb: tuple[int, int, int], crouch: bool) -> None:
    """Exaggerated dachshund silhouette, matching the style used by other generated tiles."""
    body_y = y + (s * 0.15 if crouch else 0.0)
    d.rounded_rectangle([x - s * 1.7, body_y - s * 0.5, x + s * 1.5, body_y + s * 0.4], radius=int(s * 0.4), fill=body_rgb)
    for lx in (x - s * 1.35, x - s * 0.55, x + s * 0.45, x + s * 1.15):
        leg_h = s * 0.4 if crouch else s * 0.55
        d.rectangle([lx, body_y + s * 0.35, lx + s * 0.26, body_y + s * 0.35 + leg_h], fill=body_rgb)
    hx, hy = x + s * 1.75, body_y - s * (0.55 if crouch else 0.7)
    d.polygon([(x + s * 1.1, body_y - s * 0.45), (hx, hy), (x + s * 1.5, body_y + s * 0.25)], fill=body_rgb)
    d.ellipse([hx - s * 0.6, hy - s * 0.55, hx + s * 0.6, hy + s * 0.55], fill=body_rgb)
    d.rounded_rectangle([hx + s * 0.18, hy - s * 0.2, hx + s * 1.0, hy + s * 0.2], radius=int(s * 0.18), fill=body_rgb)
    d.ellipse([hx - s * 0.7, hy - s * 0.32, hx - s * 0.18, hy + s * 0.5],
              fill=tuple(max(0, c - 36) for c in body_rgb))
    d.ellipse([hx + s * 0.06, hy - s * 0.28, hx + s * 0.24, hy - s * 0.1], fill=(30, 24, 20))
    d.polygon([(x - s * 1.7, body_y - s * 0.35), (x - s * 2.2, body_y - s * 0.95), (x - s * 1.45, body_y - s * 0.1)], fill=body_rgb)


def hedge(d: ImageDraw.ImageDraw, x: float, y: float, w: float, h: float) -> None:
    d.rounded_rectangle([x, y, x + w, y + h], radius=int(h * 0.3), fill=(34, 70, 30))
    for i in range(int(w // 40)):
        bx = x + 20 + i * 40
        d.ellipse([bx - 18, y - 10, bx + 18, y + 18], fill=(42, 84, 36))


def main() -> None:
    img = Image.new("RGB", (SIZE, SIZE), (26, 42, 24))
    d = ImageDraw.Draw(img)

    # Twilight-green maze sky/lawn split.
    for y in range(int(SIZE * 0.6)):
        t = y / (SIZE * 0.6)
        d.line([(0, y), (SIZE, y)], fill=(int(30 + 18 * t), int(46 + 26 * t), int(30 + 20 * t)))
    d.rectangle([0, int(SIZE * 0.6), SIZE, SIZE], fill=(58, 96, 48))

    # Hedge maze walls framing a corridor running toward the viewer.
    hedge(d, 0, SIZE * 0.55, SIZE * 0.32, SIZE * 0.45)
    hedge(d, SIZE * 0.68, SIZE * 0.55, SIZE * 0.32, SIZE * 0.45)
    hedge(d, SIZE * 0.10, SIZE * 0.30, SIZE * 0.22, SIZE * 0.22)
    hedge(d, SIZE * 0.68, SIZE * 0.30, SIZE * 0.22, SIZE * 0.22)

    # Burr patches: small spiky brown clusters along the corridor floor.
    for bx, by in ((SIZE * 0.38, SIZE * 0.78), (SIZE * 0.62, SIZE * 0.70)):
        for k in range(6):
            a = k * 60
            px = bx + math.cos(math.radians(a)) * 26
            py = by + math.sin(math.radians(a)) * 18
            d.ellipse([px - 10, py - 10, px + 10, py + 10], fill=(96, 70, 34))
        d.ellipse([bx - 22, by - 16, bx + 22, by + 16], fill=(122, 92, 46))

    # The cat, mid-corridor, facing across the frame with a visible vision-cone wedge.
    cx, cy = SIZE * 0.52, SIZE * 0.46
    s = SIZE * 0.075
    cone_dir = -35
    cone_half = 40
    p0 = (cx, cy)
    p1 = (cx + math.cos(math.radians(cone_dir - cone_half)) * s * 6.5, cy + math.sin(math.radians(cone_dir - cone_half)) * s * 6.5)
    p2 = (cx + math.cos(math.radians(cone_dir + cone_half)) * s * 6.5, cy + math.sin(math.radians(cone_dir + cone_half)) * s * 6.5)
    d.polygon([p0, p1, p2], fill=(255, 232, 140, 255) if img.mode == "RGBA" else (210, 196, 120))

    # Cat silhouette: round head, triangle ears, long tail, sitting pose.
    d.ellipse([cx - s * 0.95, cy - s * 0.5, cx + s * 0.55, cy + s * 0.85], fill=(28, 28, 30))
    d.ellipse([cx - s * 0.35, cy - s * 1.05, cx + s * 0.35, cy - s * 0.35], fill=(28, 28, 30))
    d.polygon([(cx - s * 0.3, cy - s * 0.9), (cx - s * 0.5, cy - s * 1.3), (cx - s * 0.05, cy - s * 0.95)], fill=(28, 28, 30))
    d.polygon([(cx + s * 0.05, cy - s * 0.95), (cx + s * 0.45, cy - s * 1.3), (cx + s * 0.25, cy - s * 0.9)], fill=(28, 28, 30))
    d.ellipse([cx - s * 0.12, cy - s * 0.75, cx + s * 0.04, cy - s * 0.6], fill=(255, 220, 60))
    d.polygon([(cx - s * 0.9, cy + s * 0.7), (cx - s * 1.7, cy + s * 0.2), (cx - s * 1.55, cy + s * 0.55), (cx - s * 0.85, cy + s * 0.95)], fill=(28, 28, 30))

    # A bush (hiding spot) lower-left of the corridor.
    bx2, by2 = SIZE * 0.14, SIZE * 0.86
    for dx, dy, r in ((-30, 0, 46), (0, -24, 52), (30, 0, 46), (0, 14, 40)):
        d.ellipse([bx2 + dx - r, by2 + dy - r, bx2 + dx + r, by2 + dy + r], fill=(30, 66, 28))

    # Cheddar (gold) barreling through the bramble at left; Cocoa (chocolate) crouched, reading the
    # patrol, at right in the shadow of the far hedge.
    long_dog(d, SIZE * 0.30, SIZE * 0.90, SIZE * 0.075, (222, 160, 64), crouch=False)
    long_dog(d, SIZE * 0.86, SIZE * 0.88, SIZE * 0.07, (108, 70, 46), crouch=True)

    # A few burrs stuck to Cheddar to sell the "picking up burrs fast" read.
    for dx, dy in ((-40, -10), (10, -25), (35, 5)):
        px, py = SIZE * 0.30 + dx, SIZE * 0.90 + dy
        d.ellipse([px - 8, py - 8, px + 8, py + 8], fill=(122, 92, 46))

    OUT.parent.mkdir(parents=True, exist_ok=True)
    img.save(OUT)
    print(f"wrote {OUT}")


if __name__ == "__main__":
    main()
