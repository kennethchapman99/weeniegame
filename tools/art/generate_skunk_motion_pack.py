#!/usr/bin/env python3
"""Generate a placeholder animated skunk motion pack for Skunk Blast Mayhem.

Couch test 2026-07-24: the skunk mission's shared PredatorObject had no skunk art or animation
of its own, so ThreatReadabilityAnimator's hardcoded Eagle default rendered a banking eagle in
its place ("we need an animated skunk not an eagle"). This script draws a flat, transparent,
couch-test-quality skunk silhouette - matching the existing flat-icon style used for mission
props (tools/art/generate_mission_prop_pack.py) - across the three clips
ThreatMotionArt.TryInfer maps SKUNK-labeled actor states onto: Patrol (calm guard), Threaten
(tail-lift danger telegraph), and Retreat (gives up).

This is intentionally a placeholder tier, not a painted character-tier asset: the Eagle/Coyote/
Squirrel motion art was produced through the project's separate AI image-generation pipeline
(see docs/CHARACTER-MOTION-GENERATION.md), which this deterministic script does not attempt to
replicate. Promoting the skunk to that same painted fidelity is a follow-up, not this pass.
"""

from __future__ import annotations

import math
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path("unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Characters/Skunk/Motion")
# Matches the existing Eagle/Coyote/Squirrel Motion contract: 512x384 with a near-bottom paw
# baseline (see ArenaFinalArtImporter's custom pivot for anything under a Motion/ folder), not a
# square 512x512 canvas.
WIDTH, HEIGHT = 512, 384

BLACK = (28, 24, 22, 255)
BODY = (36, 32, 30, 255)
STRIPE = (250, 248, 240, 255)
NOSE = (232, 200, 205, 255)
OUTLINE = (18, 15, 14, 255)
SHADOW = (0, 0, 0, 42)

CLIPS = ("patrol", "threaten", "retreat")
FRAMES = 4


def canvas() -> tuple[Image.Image, ImageDraw.ImageDraw]:
    image = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 0))
    return image, ImageDraw.Draw(image)


def draw_skunk(draw: ImageDraw.ImageDraw, tail_lift: float, body_bob: float, hunch: float) -> None:
    """tail_lift 0..1 (0=low/relaxed, 1=fully raised), body_bob -1..1 (breathing/gait),
    hunch 0..1 (0=upright guard stance, 1=hunkered-down retreat crouch)."""

    # cx sits right of center: the tail sweeps left of the body and the fully-relaxed pose
    # needs room for it without clipping the 512-wide canvas' left edge.
    cx, cy = 296, 200 + int(body_bob * 5) + int(hunch * 18)

    # Ground shadow.
    draw.ellipse((cx - 150, cy + 100, cx + 150, cy + 130), fill=SHADOW)

    # Tail: a broad plume that sweeps from low-and-back (relaxed) to high-and-curled
    # (tail-up danger telegraph). Drawn first so the body silhouette overlaps its base.
    tail_base = (cx - 92, cy + 40)
    # 165 deg = trailing left, drooping slightly (relaxed); 265 deg = nearly straight up,
    # leaning back a touch (fully raised alarm tail). Screen space is y-down, so "up" needs a
    # negative sine, i.e. an angle past 180 deg - not less than 200 deg as an earlier version
    # of this had it, which raised the tail into a down-and-right droop instead of lifting it.
    lift_angle = math.radians(165 + 100 * tail_lift)
    tail_len = 170 - 30 * hunch
    tip_x = tail_base[0] + math.cos(lift_angle) * tail_len
    tip_y = tail_base[1] + math.sin(lift_angle) * tail_len
    mid_x = tail_base[0] + math.cos(lift_angle) * tail_len * 0.55 - 30
    mid_y = tail_base[1] + math.sin(lift_angle) * tail_len * 0.55 - 20 * tail_lift
    plume_w = 46 + 18 * tail_lift
    draw.line([tail_base, (mid_x, mid_y), (tip_x, tip_y)], fill=BODY, width=int(plume_w), joint="curve")
    draw.ellipse((tip_x - plume_w * 0.6, tip_y - plume_w * 0.6, tip_x + plume_w * 0.6, tip_y + plume_w * 0.6),
                 fill=BODY, outline=OUTLINE, width=8)
    draw.line([tail_base, (mid_x, mid_y), (tip_x, tip_y)], fill=STRIPE, width=int(plume_w * 0.4), joint="curve")

    # Body: a low, long oval crouched lower during the retreat hunch.
    body_h = 128 - 22 * hunch
    body_box = (cx - 118, cy - body_h * 0.5, cx + 60, cy + body_h * 0.5)
    draw.ellipse(body_box, fill=BODY, outline=OUTLINE, width=10)

    # White dorsal stripe running from the head down the spine.
    draw.polygon(
        [(cx - 30, cy - body_h * 0.46), (cx + 6, cy - body_h * 0.46),
         (cx + 30, cy + body_h * 0.34), (cx - 6, cy + body_h * 0.34)],
        fill=STRIPE,
    )

    # Head, tucked low during the retreat hunch, held high and alert otherwise.
    head_cx, head_cy = cx + 78, cy - 20 - 24 * (1 - hunch)
    draw.ellipse((head_cx - 46, head_cy - 40, head_cx + 46, head_cy + 40), fill=BODY, outline=OUTLINE, width=9)
    draw.polygon([(head_cx - 14, head_cy - 34), (head_cx + 8, head_cy - 34), (head_cx - 6, head_cy - 6)],
                 fill=STRIPE)
    # Nose/muzzle.
    draw.ellipse((head_cx + 30, head_cy - 8, head_cx + 60, head_cy + 16), fill=NOSE, outline=OUTLINE, width=6)
    # Eye.
    draw.ellipse((head_cx + 6, head_cy - 10, head_cx + 20, head_cy + 4), fill=BLACK)
    # Ear.
    draw.ellipse((head_cx - 20, head_cy - 42, head_cx + 4, head_cy - 18), fill=BODY, outline=OUTLINE, width=6)

    # Legs: short black stubs, staggered slightly by bob for a subtle walk-in-place read.
    for lx in (-70, -20, 20, 62):
        ly = cy + body_h * 0.42 + (6 if (lx // 40) % 2 == 0 else -4) * (0.5 + 0.5 * body_bob)
        draw.rounded_rectangle((cx + lx - 12, cy + body_h * 0.3, cx + lx + 12, ly + 22), radius=8,
                                fill=BLACK, outline=OUTLINE, width=5)


def frame_params(clip: str, frame: int) -> tuple[float, float, float]:
    phase = frame / FRAMES
    if clip == "patrol":
        # Calm guard stance: tail low, gentle breathing sway, no hunch.
        tail_lift = 0.12 + 0.06 * math.sin(phase * 2 * math.pi)
        body_bob = math.sin(phase * 2 * math.pi)
        hunch = 0.0
    elif clip == "threaten":
        # Tail-lift danger telegraph: the tail rises across the strip toward fully raised.
        tail_lift = 0.35 + 0.65 * (frame / (FRAMES - 1))
        body_bob = math.sin(phase * 2 * math.pi * 2) * 0.6
        hunch = 0.05
    else:  # retreat
        # Gives up and waddles off: tail drops back down, body hunkers and scurries.
        tail_lift = 0.5 - 0.4 * (frame / (FRAMES - 1))
        body_bob = math.sin(phase * 2 * math.pi * 3)
        hunch = 0.55
    return tail_lift, body_bob, hunch


def main() -> None:
    ROOT.mkdir(parents=True, exist_ok=True)
    count = 0
    for clip in CLIPS:
        for frame in range(FRAMES):
            image, draw = canvas()
            tail_lift, body_bob, hunch = frame_params(clip, frame)
            draw_skunk(draw, tail_lift, body_bob, hunch)
            path = ROOT / f"skunk_{clip}_e_{frame:02d}.png"
            image.save(path)
            count += 1
    print(f"Generated {count} placeholder skunk motion frames in {ROOT}")


if __name__ == "__main__":
    main()
