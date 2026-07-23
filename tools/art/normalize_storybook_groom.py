#!/usr/bin/env python3
"""Normalize generated storybook grooming cutouts to the shared dog-motion canvas."""

from pathlib import Path

from PIL import Image


ROOT = (
    Path(__file__).resolve().parents[2]
    / "unity"
    / "CheddarAndCocoa"
    / "Assets"
    / "Art"
    / "Resources"
    / "ArenaFinal"
    / "Characters"
    / "Dogs"
)
CANVAS = (512, 384)
BASELINE_Y = 360
PADDING_X = 16


def subject(path: Path) -> Image.Image:
    with Image.open(path) as opened:
        image = opened.convert("RGBA")
    bbox = image.getchannel("A").point(lambda value: 255 if value > 12 else 0).getbbox()
    if bbox is None:
        raise RuntimeError(f"No opaque grooming subject in {path}")
    return image.crop(bbox)


def main() -> None:
    exported = 0
    for dog in ("cheddar", "cocoa"):
        folder = ROOT / dog.capitalize() / "Motion"
        paths = [folder / f"{dog}_groom_e_{frame:02d}.png" for frame in range(2)]
        subjects = [subject(path) for path in paths]
        scale = min(
            (CANVAS[0] - PADDING_X * 2) / max(image.width for image in subjects),
            (BASELINE_Y - 12) / max(image.height for image in subjects),
            1.0,
        )
        for path, image in zip(paths, subjects):
            if scale < 1.0:
                image = image.resize(
                    (round(image.width * scale), round(image.height * scale)),
                    Image.Resampling.LANCZOS,
                )
            canvas = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
            canvas.alpha_composite(image, ((CANVAS[0] - image.width) // 2, BASELINE_Y - image.height))
            canvas.save(path, optimize=True)
            exported += 1
    print(f"Normalized {exported} storybook grooming frames to {CANVAS[0]}x{CANVAS[1]}.")


if __name__ == "__main__":
    main()
