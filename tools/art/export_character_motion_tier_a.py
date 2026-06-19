#!/usr/bin/env python3
"""Extract the approved east-facing idle/run/bark boards into normalized true-alpha frames."""

from __future__ import annotations

import argparse
from collections import deque
from pathlib import Path

from PIL import Image, ImageFilter


ROOT = Path(__file__).resolve().parents[2]
SOURCE_ROOT = ROOT / "unity/CheddarAndCocoa/Assets/Art/ReferenceOnly/GeneratedCharacterMotion"
OUTPUT_ROOT = ROOT / "unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Characters/Dogs"
SHEETS = {
    "cheddar": SOURCE_ROOT / "cheddar_tier_a_east_v01.png",
    "cocoa": SOURCE_ROOT / "cocoa_tier_a_east_v01.png",
}
CLIPS = ("idle", "run", "bark")
CANVAS = (512, 384)
PADDING_X = 16
BASELINE_Y = 360


def connected_background_mask(image: Image.Image) -> Image.Image:
    rgb = image.convert("RGB")
    width, height = rgb.size
    pixels = rgb.load()
    background = bytearray(width * height)
    queue: deque[tuple[int, int]] = deque()

    def candidate(x: int, y: int) -> bool:
        red, green, blue = pixels[x, y]
        return min(red, green, blue) >= 224 and max(red, green, blue) - min(red, green, blue) <= 18

    def add(x: int, y: int) -> None:
        index = y * width + x
        if background[index] or not candidate(x, y):
            return
        background[index] = 255
        queue.append((x, y))

    for x in range(width):
        add(x, 0)
        add(x, height - 1)
    for y in range(height):
        add(0, y)
        add(width - 1, y)

    while queue:
        x, y = queue.popleft()
        if x > 0:
            add(x - 1, y)
        if x + 1 < width:
            add(x + 1, y)
        if y > 0:
            add(x, y - 1)
        if y + 1 < height:
            add(x, y + 1)

    return Image.frombytes("L", (width, height), bytes(background)).filter(ImageFilter.GaussianBlur(0.8))


def extract_cells(sheet_path: Path) -> list[Image.Image]:
    with Image.open(sheet_path) as opened:
        sheet = opened.convert("RGBA")
    cells: list[Image.Image] = []
    for row in range(3):
        top = round(row * sheet.height / 3) + 8
        bottom = round((row + 1) * sheet.height / 3) - 22
        for column in range(4):
            left = round(column * sheet.width / 4) + 5
            right = round((column + 1) * sheet.width / 4) - 5
            cell = sheet.crop((left, top, right, bottom))
            background = connected_background_mask(cell)
            alpha = Image.eval(background, lambda value: 255 - value)
            cell.putalpha(alpha)
            bbox = alpha.point(lambda value: 255 if value > 12 else 0).getbbox()
            if bbox is None:
                raise RuntimeError(f"No visible sprite in {sheet_path.name} row {row} column {column}")
            cells.append(cell.crop(bbox))
    return cells


def export_dog(dog: str, sheet_path: Path) -> None:
    cells = extract_cells(sheet_path)
    max_width = max(cell.width for cell in cells)
    max_height = max(cell.height for cell in cells)
    scale = min((CANVAS[0] - PADDING_X * 2) / max_width, (BASELINE_Y - 12) / max_height, 1.0)
    folder = dog.capitalize()
    for index, cell in enumerate(cells):
        if scale < 1.0:
            cell = cell.resize((round(cell.width * scale), round(cell.height * scale)), Image.Resampling.LANCZOS)
        canvas = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
        x = (CANVAS[0] - cell.width) // 2
        y = BASELINE_Y - cell.height
        canvas.alpha_composite(cell, (x, y))
        clip = CLIPS[index // 4]
        frame = index % 4
        output = OUTPUT_ROOT / folder / "Motion" / f"{dog}_{clip}_e_{frame:02d}.png"
        output.parent.mkdir(parents=True, exist_ok=True)
        canvas.save(output, optimize=True)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--contact-sheet", type=Path)
    args = parser.parse_args()
    for dog, sheet in SHEETS.items():
        if not sheet.exists():
            raise FileNotFoundError(sheet)
        export_dog(dog, sheet)
    print("Exported 24 Tier-A character motion frames.")
    if args.contact_sheet:
        write_contact_sheet(args.contact_sheet)


def write_contact_sheet(path: Path) -> None:
    cell_width, cell_height = 260, 200
    sheet = Image.new("RGB", (cell_width * 4, cell_height * 6), (44, 71, 38))
    for dog_index, dog in enumerate(SHEETS):
        folder = dog.capitalize()
        for clip_index, clip in enumerate(CLIPS):
            for frame in range(4):
                source = OUTPUT_ROOT / folder / "Motion" / f"{dog}_{clip}_e_{frame:02d}.png"
                with Image.open(source) as opened:
                    sprite = opened.convert("RGBA")
                sprite.thumbnail((cell_width - 18, cell_height - 18), Image.Resampling.LANCZOS)
                x = frame * cell_width + (cell_width - sprite.width) // 2
                row = dog_index * 3 + clip_index
                y = row * cell_height + (cell_height - sprite.height) // 2
                sheet.paste(sprite, (x, y), sprite)
    path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(path)


if __name__ == "__main__":
    main()
