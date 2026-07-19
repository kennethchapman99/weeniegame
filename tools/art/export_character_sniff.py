#!/usr/bin/env python3
"""Extract east/south sniff boards into normalized true-alpha runtime frames."""

from collections import deque

from PIL import Image

from export_character_motion_tier_a import (
    BASELINE_Y,
    CANVAS,
    OUTPUT_ROOT,
    PADDING_X,
    SOURCE_ROOT,
    connected_background_mask,
)


DIRECTIONS = ("e", "s")


def largest_foreground_island(alpha):
    """Zero out every foreground blob except the largest connected one.

    Tight per-cell crops on this board occasionally isolate a sliver of a
    neighboring cell's ear/tail as its own disconnected island (a bleed
    artifact, not part of this frame's dog) - keep only the dominant blob.
    """
    width, height = alpha.size
    pixels = alpha.load()
    visited = bytearray(width * height)
    best_component = None
    best_size = 0

    for start_y in range(height):
        for start_x in range(width):
            index = start_y * width + start_x
            if visited[index] or pixels[start_x, start_y] <= 12:
                continue
            component = []
            queue = deque([(start_x, start_y)])
            visited[index] = 1
            while queue:
                x, y = queue.popleft()
                component.append((x, y))
                for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
                    if 0 <= nx < width and 0 <= ny < height:
                        nindex = ny * width + nx
                        if not visited[nindex] and pixels[nx, ny] > 12:
                            visited[nindex] = 1
                            queue.append((nx, ny))
            if len(component) > best_size:
                best_size = len(component)
                best_component = component

    cleaned = Image.new("L", alpha.size, 0)
    cleaned_pixels = cleaned.load()
    for x, y in best_component or []:
        cleaned_pixels[x, y] = pixels[x, y]
    return cleaned


def extract(sheet_path):
    with Image.open(sheet_path) as opened:
        sheet = opened.convert("RGBA")

    cells = []
    for row in range(2):
        for column in range(4):
            cell = sheet.crop((
                round(column * sheet.width / 4) + 8,
                round(row * sheet.height / 2) + 8,
                round((column + 1) * sheet.width / 4) - 8,
                round((row + 1) * sheet.height / 2) - 18,
            ))
            background = connected_background_mask(cell)
            alpha = Image.eval(background, lambda value: 255 - value)
            alpha = largest_foreground_island(alpha)
            cell.putalpha(alpha)
            bbox = alpha.point(lambda value: 255 if value > 12 else 0).getbbox()
            if bbox is None:
                raise RuntimeError(f"No sniff sprite in {sheet_path.name} row {row} column {column}")
            cells.append(cell.crop(bbox))
    return cells


def main():
    exported = 0
    for dog in ("cheddar", "cocoa"):
        cells = extract(SOURCE_ROOT / f"{dog}_sniff_east_south_v01.png")
        scale = min(
            (CANVAS[0] - PADDING_X * 2) / max(cell.width for cell in cells),
            (BASELINE_Y - 12) / max(cell.height for cell in cells),
            1.0,
        )
        for index, cell in enumerate(cells):
            if scale < 1.0:
                cell = cell.resize(
                    (round(cell.width * scale), round(cell.height * scale)),
                    Image.Resampling.LANCZOS,
                )
            canvas = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
            canvas.alpha_composite(cell, ((CANVAS[0] - cell.width) // 2, BASELINE_Y - cell.height))
            direction = DIRECTIONS[index // 4]
            frame = index % 4
            output = (
                OUTPUT_ROOT
                / dog.capitalize()
                / "Motion"
                / f"{dog}_sniff_{direction}_{frame:02d}.png"
            )
            output.parent.mkdir(parents=True, exist_ok=True)
            canvas.save(output, optimize=True)
            exported += 1
    print(f"Exported {exported} sniff frames.")


if __name__ == "__main__":
    main()
