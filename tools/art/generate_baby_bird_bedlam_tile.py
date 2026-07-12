#!/usr/bin/env python3
"""Generate the placeholder mission-select tile for Baby Bird Bedlam.

The other MissionTiles portraits are authored art; this one is a deterministic
generated placeholder (sky, oak nest, tumbling chicks, diving parent, and the
two long-dog silhouettes) so the new mission's tile slot never renders blank.
Replace with authored art when the mission earns its portrait pass.
"""

from __future__ import annotations

import math
from pathlib import Path

from PIL import Image, ImageDraw

OUT = Path("unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/UI/MissionTiles/babybirdbedlam.png")
SIZE = 1254


def main() -> None:
    img = Image.new("RGB", (SIZE, SIZE), (110, 170, 220))
    d = ImageDraw.Draw(img)

    # Sky gradient.
    for y in range(SIZE):
        t = y / SIZE
        d.line([(0, y), (SIZE, y)], fill=(int(105 + 60 * t), int(165 + 40 * t), int(220 - 30 * t)))

    # Lawn.
    d.rectangle([0, int(SIZE * 0.78), SIZE, SIZE], fill=(96, 158, 84))
    d.rectangle([0, int(SIZE * 0.78), SIZE, int(SIZE * 0.80)], fill=(78, 132, 68))

    # Oak canopy + trunk in the top-left, holding the nest.
    d.rectangle([int(SIZE * 0.10), int(SIZE * 0.30), int(SIZE * 0.17), int(SIZE * 0.80)], fill=(94, 66, 40))
    for cx, cy, r in ((0.14, 0.24, 0.16), (0.03, 0.30, 0.13), (0.26, 0.29, 0.13), (0.14, 0.36, 0.13)):
        x, y, rr = SIZE * cx, SIZE * cy, SIZE * r
        d.ellipse([x - rr, y - rr, x + rr, y + rr], fill=(60, 118, 58))
    # Nest: a brown bowl with straw ticks.
    nx, ny, nw, nh = SIZE * 0.24, SIZE * 0.34, SIZE * 0.11, SIZE * 0.05
    d.ellipse([nx - nw, ny - nh, nx + nw, ny + nh], fill=(122, 86, 44))
    d.ellipse([nx - nw * 0.72, ny - nh * 0.9, nx + nw * 0.72, ny + nh * 0.2], fill=(86, 58, 30))
    for i in range(9):
        a = math.pi * (0.15 + 0.7 * i / 8)
        d.line([(nx + math.cos(a) * nw, ny - math.sin(a) * nh),
                (nx + math.cos(a) * nw * 1.18, ny - math.sin(a) * nh * 1.6)],
               fill=(150, 110, 56), width=6)

    def chick(x: float, y: float, s: float, tumble: float) -> None:
        body = s
        d.ellipse([x - body, y - body * 0.8, x + body, y + body * 0.8], fill=(255, 224, 92))
        hx, hy = x + body * 0.7 * math.cos(tumble), y - body * 0.9
        d.ellipse([hx - body * 0.5, hy - body * 0.5, hx + body * 0.5, hy + body * 0.5], fill=(255, 232, 110))
        d.polygon([(hx + body * 0.4, hy), (hx + body * 0.85, hy + body * 0.12), (hx + body * 0.4, hy + body * 0.24)],
                  fill=(240, 150, 50))
        d.ellipse([hx + body * 0.05, hy - body * 0.18, hx + body * 0.23, hy], fill=(40, 30, 24))
        # Stubby flailing wings.
        d.ellipse([x - body * 1.3, y - body * 0.5, x - body * 0.5, y + body * 0.1], fill=(250, 205, 70))
        d.ellipse([x + body * 0.5, y - body * 0.6, x + body * 1.3, y - body * 0.05], fill=(250, 205, 70))

    # Tumbling chicks between nest and lawn.
    chick(SIZE * 0.36, SIZE * 0.48, SIZE * 0.045, 0.4)
    chick(SIZE * 0.30, SIZE * 0.64, SIZE * 0.038, 2.2)
    # Motion ticks behind the falling chicks.
    for cx, cy in ((0.36, 0.41), (0.30, 0.57)):
        for k in range(3):
            y0 = SIZE * cy - k * 26
            d.line([(SIZE * cx - 10, y0), (SIZE * cx + 10, y0 - 12)], fill=(255, 255, 255), width=8)

    def parent_bird(x: float, y: float, s: float, flip: bool) -> None:
        f = -1 if flip else 1
        d.polygon([(x, y), (x - s * 1.6 * f, y - s * 0.9), (x - s * 0.7 * f, y + s * 0.05)], fill=(70, 78, 102))
        d.polygon([(x, y), (x - s * 1.3 * f, y + s * 0.9), (x - s * 0.5 * f, y + s * 0.25)], fill=(58, 64, 88))
        d.ellipse([x - s * 0.55, y - s * 0.35, x + s * 0.75, y + s * 0.45], fill=(80, 90, 116))
        d.polygon([(x + s * 0.7 * f, y - s * 0.05), (x + s * 1.15 * f, y + s * 0.12), (x + s * 0.66 * f, y + s * 0.26)],
                  fill=(238, 176, 60))
        d.ellipse([x + s * 0.3 * f - 9, y - s * 0.16 - 9, x + s * 0.3 * f + 9, y - s * 0.16 + 9], fill=(255, 70, 50))

    # One parent circling high, one mid-dive at the eater.
    parent_bird(SIZE * 0.72, SIZE * 0.16, SIZE * 0.075, flip=False)
    parent_bird(SIZE * 0.62, SIZE * 0.44, SIZE * 0.095, flip=True)
    # Dive streaks.
    for k in range(3):
        d.line([(SIZE * (0.70 + 0.02 * k), SIZE * 0.30), (SIZE * (0.64 + 0.02 * k), SIZE * 0.40)],
               fill=(255, 255, 255), width=10)

    def long_dog(x: float, y: float, s: float, body_rgb: tuple[int, int, int], mouth_up: bool) -> None:
        # Exaggerated dachshund silhouette: long low body, short legs, big snout.
        d.rounded_rectangle([x - s * 1.7, y - s * 0.55, x + s * 1.5, y + s * 0.45], radius=int(s * 0.45), fill=body_rgb)
        for lx in (x - s * 1.35, x - s * 0.55, x + s * 0.45, x + s * 1.15):
            d.rectangle([lx, y + s * 0.4, lx + s * 0.28, y + s * 0.95], fill=body_rgb)
        hx, hy = x + s * 1.75, y - s * (0.85 if mouth_up else 0.6)
        # Neck wedge so the raised head stays attached to the long body.
        d.polygon([(x + s * 1.1, y - s * 0.5), (hx, hy), (x + s * 1.5, y + s * 0.3)], fill=body_rgb)
        d.ellipse([hx - s * 0.62, hy - s * 0.6, hx + s * 0.62, hy + s * 0.6], fill=body_rgb)
        d.rounded_rectangle([hx + s * 0.2, hy - s * 0.24, hx + s * 1.1, hy + s * 0.22], radius=int(s * 0.2), fill=body_rgb)
        # Floppy ear + eye.
        d.ellipse([hx - s * 0.72, hy - s * 0.35, hx - s * 0.18, hy + s * 0.55],
                  fill=tuple(max(0, c - 36) for c in body_rgb))
        d.ellipse([hx + s * 0.08, hy - s * 0.3, hx + s * 0.26, hy - s * 0.12], fill=(30, 24, 20))
        # Tail flag.
        d.polygon([(x - s * 1.7, y - s * 0.4), (x - s * 2.25, y - s * 1.0), (x - s * 1.45, y - s * 0.15)], fill=body_rgb)

    # Cheddar (gold, snout up mid-gulp under the chicks); Cocoa (chocolate, barking at the diver).
    long_dog(SIZE * 0.27, SIZE * 0.86, SIZE * 0.085, (222, 160, 64), mouth_up=True)
    long_dog(SIZE * 0.66, SIZE * 0.86, SIZE * 0.09, (108, 70, 46), mouth_up=True)
    # Cocoa's bark burst aimed at the diving parent.
    bx, by = SIZE * 0.735, SIZE * 0.62
    for k in range(3):
        r = SIZE * (0.035 + 0.028 * k)
        d.arc([bx - r, by - r, bx + r, by + r], start=-70, end=25, fill=(255, 244, 180), width=14)

    OUT.parent.mkdir(parents=True, exist_ok=True)
    img.save(OUT)
    print(f"wrote {OUT}")


if __name__ == "__main__":
    main()
