#!/usr/bin/env python3
"""Generate the greybox mission-select picture tile for Tick Invasion.

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

OUT = Path("unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/UI/MissionTiles/tickinvasion.png")
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


def tick_dot(d: ImageDraw.ImageDraw, x: float, y: float, r: float) -> None:
    """Tiny rounded tick body with stub legs, deliberately readable-hazard shaped (a small dark bug)."""
    d.ellipse([x - r, y - r * 1.15, x + r, y + r * 1.15], fill=(60, 24, 22))
    d.ellipse([x - r * 0.55, y - r * 1.5, x + r * 0.55, y - r * 0.55], fill=(80, 32, 28))
    for dx, dy in ((-1, -1), (1, -1), (-1.3, 0), (1.3, 0), (-1, 1), (1, 1)):
        d.line([(x, y), (x + dx * r * 1.3, y + dy * r * 0.9)], fill=(40, 16, 14), width=max(2, int(r * 0.22)))


def main() -> None:
    img = Image.new("RGB", (SIZE, SIZE), (40, 58, 34))
    d = ImageDraw.Draw(img)

    # Overgrown backyard sky + lawn.
    for y in range(int(SIZE * 0.55)):
        t = y / (SIZE * 0.55)
        d.line([(0, y), (SIZE, y)], fill=(int(120 + 40 * t), int(168 + 30 * t), int(90 + 40 * t)))
    d.rectangle([0, int(SIZE * 0.55), SIZE, SIZE], fill=(58, 96, 42))
    d.rectangle([0, int(SIZE * 0.55), SIZE, int(SIZE * 0.58)], fill=(44, 78, 34))

    # Overgrown grass tufts across the lower band - the infestation habitat.
    for i in range(24):
        gx = (i * 53) % SIZE
        gy = SIZE * 0.6 + (i * 37) % (SIZE * 0.35)
        gh = 34 + (i * 17) % 40
        d.line([(gx, gy), (gx - 10, gy - gh)], fill=(70, 118, 48), width=6)
        d.line([(gx, gy), (gx + 10, gy - gh * 0.8)], fill=(80, 128, 54), width=6)

    # Pool, upper-right: the risk/reward escape hatch.
    px, py = SIZE * 0.78, SIZE * 0.22
    pw, ph = SIZE * 0.16, SIZE * 0.1
    d.ellipse([px - pw, py - ph, px + pw, py + ph], fill=(60, 150, 210))
    d.ellipse([px - pw * 0.7, py - ph * 0.6, px + pw * 0.7, py + ph * 0.6], fill=(90, 180, 230))

    # Cheddar (gold) and Cocoa (chocolate) grooming each other, close together center-lawn.
    long_dog(d, SIZE * 0.36, SIZE * 0.82, SIZE * 0.078, (222, 160, 64), mouth_up=True)
    long_dog(d, SIZE * 0.60, SIZE * 0.82, SIZE * 0.072, (108, 70, 46), mouth_up=False)

    # Ticks swarming both dogs and the grass between them - the readable hazard.
    tick_positions = [
        (SIZE * 0.30, SIZE * 0.70, 12), (SIZE * 0.40, SIZE * 0.66, 10), (SIZE * 0.34, SIZE * 0.78, 9),
        (SIZE * 0.56, SIZE * 0.70, 11), (SIZE * 0.64, SIZE * 0.75, 10), (SIZE * 0.62, SIZE * 0.64, 9),
        (SIZE * 0.47, SIZE * 0.60, 13), (SIZE * 0.50, SIZE * 0.72, 10), (SIZE * 0.44, SIZE * 0.85, 9),
        (SIZE * 0.70, SIZE * 0.86, 9), (SIZE * 0.22, SIZE * 0.60, 10),
    ]
    for tx, ty, tr in tick_positions:
        tick_dot(d, tx, ty, tr)

    # Warning burst over the groom moment - readable-chaos telegraph.
    cx, cy = SIZE * 0.48, SIZE * 0.55
    for k in range(3):
        r = SIZE * (0.05 + 0.03 * k)
        a0 = 40 + k * 8
        d.arc([cx - r, cy - r, cx + r, cy + r], start=a0, end=a0 + 100, fill=(255, 210, 90), width=10)

    OUT.parent.mkdir(parents=True, exist_ok=True)
    img.save(OUT)
    print(f"wrote {OUT}")


if __name__ == "__main__":
    main()
