#!/usr/bin/env python3
"""Extract approved south/north carry pairs into normalized alpha sprites."""

from PIL import Image

from export_character_motion_tier_a import (
    BASELINE_Y,
    CANVAS,
    OUTPUT_ROOT,
    PADDING_X,
    SOURCE_ROOT,
    connected_background_mask,
)


DIRECTIONS = ("s", "n")


def extract_straights(sheet_path):
    with Image.open(sheet_path) as opened:
        sheet = opened.convert("RGBA")

    cells = []
    row = 1
    for column in range(4):
        cell = sheet.crop((
            round(column * sheet.width / 4) + 8,
            round(row * sheet.height / 2) + 8,
            round((column + 1) * sheet.width / 4) - 8,
            sheet.height - 18,
        ))
        background = connected_background_mask(cell)
        alpha = Image.eval(background, lambda value: 255 - value)
        cell.putalpha(alpha)
        bbox = alpha.point(lambda value: 255 if value > 12 else 0).getbbox()
        if bbox is None:
            raise RuntimeError(f"No carry sprite in {sheet_path.name} column {column}")
        cells.append(cell.crop(bbox))
    return cells


def main():
    exported = 0
    for dog in ("cheddar", "cocoa"):
        cells = extract_straights(SOURCE_ROOT / f"{dog}_carry_directions_v02.png")
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
            direction = DIRECTIONS[index // 2]
            frame = index % 2
            output = (
                OUTPUT_ROOT
                / dog.capitalize()
                / "Motion"
                / f"{dog}_carry_{direction}_{frame:02d}.png"
            )
            output.parent.mkdir(parents=True, exist_ok=True)
            canvas.save(output, optimize=True)
            exported += 1
    print(f"Exported {exported} straight carry frames.")


if __name__ == "__main__":
    main()
