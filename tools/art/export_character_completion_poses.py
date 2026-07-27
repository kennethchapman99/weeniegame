#!/usr/bin/env python3
"""Promote the approved Cheddar/Cocoa completion atlases to Unity motion sprites.

The source boards are 4x3 chroma-keyed image-generation outputs. The imagegen
chroma helper produces the tracked ``*_alpha.png`` inputs; this script only
splits, trims, and normalizes those approved subjects onto the established
512x384 dog-motion canvas.
"""

from __future__ import annotations

from pathlib import Path
from collections import deque

from PIL import Image


REFERENCE_ROOT = Path(
    "unity/CheddarAndCocoa/Assets/Art/ReferenceOnly/GeneratedCharacterCompletion"
)
RUNTIME_ROOT = Path(
    "unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Characters/Dogs"
)
CANVAS = (512, 384)
BASELINE_Y = 360
MAX_SUBJECT = (470, 340)

# Left-to-right, top-to-bottom in the approved 4x3 board.
POSES = (
    "jump_complete",
    "swim",
    "comfort",
    "flop",
    "beg",
    "headtilt",
    "pawtap",
    "pushpull",
    "hide",
    "wetshake",
    "trapped",
    "sleepy",
)


def cell_bounds(width: int, height: int, index: int) -> tuple[int, int, int, int]:
    column = index % 4
    row = index // 4
    x0 = round(column * width / 4)
    x1 = round((column + 1) * width / 4)
    y0 = round(row * height / 3)
    y1 = round((row + 1) * height / 3)
    return x0, y0, x1, y1


def remove_neighbor_fragments(cell: Image.Image, keep_accents: bool) -> Image.Image:
    """Keep the main pose plus nearby acting accents, discarding adjacent-cell bleed."""
    alpha = cell.getchannel("A")
    width, height = cell.size
    pixels = alpha.load()
    visited: set[tuple[int, int]] = set()
    components: list[tuple[list[tuple[int, int]], tuple[int, int, int, int]]] = []

    for y in range(height):
        for x in range(width):
            if pixels[x, y] <= 24 or (x, y) in visited:
                continue
            queue = deque([(x, y)])
            visited.add((x, y))
            points: list[tuple[int, int]] = []
            min_x = max_x = x
            min_y = max_y = y
            while queue:
                px, py = queue.popleft()
                points.append((px, py))
                min_x, max_x = min(min_x, px), max(max_x, px)
                min_y, max_y = min(min_y, py), max(max_y, py)
                for nx, ny in ((px - 1, py), (px + 1, py), (px, py - 1), (px, py + 1)):
                    if 0 <= nx < width and 0 <= ny < height and pixels[nx, ny] > 24 and (nx, ny) not in visited:
                        visited.add((nx, ny))
                        queue.append((nx, ny))
            components.append((points, (min_x, min_y, max_x, max_y)))

    if not components:
        return cell
    main_points, main_box = max(components, key=lambda item: len(item[0]))

    def box_gap(box: tuple[int, int, int, int]) -> int:
        x_gap = max(main_box[0] - box[2], box[0] - main_box[2], 0)
        y_gap = max(main_box[1] - box[3], box[1] - main_box[3], 0)
        return max(x_gap, y_gap)

    keep_boxes = [main_box]
    for points, box in components:
        if points is main_points:
            continue
        if keep_accents and len(points) >= 8 and box_gap(box) <= 32:
            keep_boxes.append(box)

    output = Image.new("RGBA", cell.size, (0, 0, 0, 0))
    source_pixels = cell.load()
    output_pixels = output.load()
    for box in keep_boxes:
        min_x = max(0, box[0] - 2)
        min_y = max(0, box[1] - 2)
        max_x = min(width - 1, box[2] + 2)
        max_y = min(height - 1, box[3] + 2)
        for y in range(min_y, max_y + 1):
            for x in range(min_x, max_x + 1):
                output_pixels[x, y] = source_pixels[x, y]
    return output


def normalize(cell: Image.Image, keep_accents: bool) -> Image.Image:
    cell = remove_neighbor_fragments(cell, keep_accents)
    alpha = cell.getchannel("A")
    bounds = alpha.getbbox()
    if bounds is None:
        raise RuntimeError("approved completion-atlas cell is empty")

    subject = cell.crop(bounds)
    scale = min(MAX_SUBJECT[0] / subject.width, MAX_SUBJECT[1] / subject.height, 1.0)
    if scale < 1.0:
        subject = subject.resize(
            (round(subject.width * scale), round(subject.height * scale)),
            Image.Resampling.LANCZOS,
        )

    canvas = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    x = (CANVAS[0] - subject.width) // 2
    y = BASELINE_Y - subject.height
    canvas.alpha_composite(subject, (x, y))
    return canvas


def export_dog(dog: str) -> int:
    source = REFERENCE_ROOT / f"{dog}_completion_atlas_alpha.png"
    atlas = Image.open(source).convert("RGBA")
    folder = "Cheddar" if dog == "cheddar" else "Cocoa"
    output_root = RUNTIME_ROOT / folder / "Motion"
    output_root.mkdir(parents=True, exist_ok=True)

    for index, pose in enumerate(POSES):
        cell = atlas.crop(cell_bounds(atlas.width, atlas.height, index))
        output = output_root / f"{dog}_{pose}_e_00.png"
        normalize(cell, pose in {"swim", "pawtap", "wetshake"}).save(output)
    return len(POSES)


def main() -> None:
    count = export_dog("cheddar") + export_dog("cocoa")
    print(f"Exported {count} completion-pose sprites")


if __name__ == "__main__":
    main()
