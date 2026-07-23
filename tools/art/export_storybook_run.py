#!/usr/bin/env python3
"""Slice approved V02 storybook run boards into normalized east-facing runtime frames."""

from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
SOURCE_ROOT = ROOT / "unity/CheddarAndCocoa/Assets/Art/ReferenceOnly/GeneratedCharacterMotion"
OUTPUT_ROOT = ROOT / "unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Characters/Dogs"
CANVAS = (512, 384)
BASELINE_Y = 360
PADDING_X = 16


def occupied_runs(alpha: Image.Image) -> list[tuple[int, int]]:
    occupied = [
        any(alpha.getpixel((x, y)) > 12 for y in range(alpha.height))
        for x in range(alpha.width)
    ]
    runs: list[tuple[int, int]] = []
    start = None
    for x, present in enumerate(occupied + [False]):
        if present and start is None:
            start = x
        elif not present and start is not None:
            if x - start > 40:
                runs.append((start, x))
            start = None
    if len(runs) != 4:
        raise RuntimeError(f"Expected four separated run poses, found {len(runs)}: {runs}")
    return runs


def extract_frames(path: Path) -> list[Image.Image]:
    with Image.open(path) as opened:
        sheet = opened.convert("RGBA")
    alpha = sheet.getchannel("A")
    frames: list[Image.Image] = []
    for left, right in occupied_runs(alpha):
        column_alpha = alpha.crop((left, 0, right, alpha.height))
        bbox = column_alpha.point(lambda value: 255 if value > 12 else 0).getbbox()
        if bbox is None:
            raise RuntimeError(f"No visible subject in {path.name} at x={left}:{right}")
        frames.append(sheet.crop((left + bbox[0], bbox[1], left + bbox[2], bbox[3])))
    return frames


def export_dog(dog: str) -> None:
    source = SOURCE_ROOT / f"{dog}_storybook_run_east_v02.png"
    frames = extract_frames(source)
    scale = min(
        (CANVAS[0] - PADDING_X * 2) / max(frame.width for frame in frames),
        (BASELINE_Y - 12) / max(frame.height for frame in frames),
        1.0,
    )
    folder = OUTPUT_ROOT / dog.capitalize() / "Motion"
    folder.mkdir(parents=True, exist_ok=True)
    for index, frame in enumerate(frames):
        if scale < 1.0:
            frame = frame.resize(
                (round(frame.width * scale), round(frame.height * scale)),
                Image.Resampling.LANCZOS,
            )
        canvas = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
        canvas.alpha_composite(
            frame,
            ((CANVAS[0] - frame.width) // 2, BASELINE_Y - frame.height),
        )
        canvas.save(folder / f"{dog}_run_storybook_e_{index:02d}.png", optimize=True)


def main() -> None:
    for dog in ("cheddar", "cocoa"):
        export_dog(dog)
    print("Exported 8 normalized storybook run frames.")


if __name__ == "__main__":
    main()
