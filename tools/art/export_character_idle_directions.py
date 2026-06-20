#!/usr/bin/env python3
"""Extract diagonal and straight idle boards into normalized alpha frames."""

from PIL import Image
from export_character_motion_tier_a import BASELINE_Y, CANVAS, OUTPUT_ROOT, PADDING_X, SOURCE_ROOT, connected_background_mask

GROUPS = (("diagonals", ("se", "ne")), ("straights", ("s", "n")))


def extract(sheet_path):
    with Image.open(sheet_path) as opened:
        sheet = opened.convert("RGBA")
    cells = []
    for row in range(2):
        for column in range(4):
            cell = sheet.crop((round(column * sheet.width / 4) + 5, round(row * sheet.height / 2) + 8,
                               round((column + 1) * sheet.width / 4) - 5, round((row + 1) * sheet.height / 2) - 18))
            background = connected_background_mask(cell)
            alpha = Image.eval(background, lambda value: 255 - value)
            cell.putalpha(alpha)
            bbox = alpha.point(lambda value: 255 if value > 12 else 0).getbbox()
            if bbox is None:
                raise RuntimeError(f"No idle sprite in {sheet_path.name} row {row} column {column}")
            cells.append(cell.crop(bbox))
    return cells


def main():
    exported = 0
    for group, directions in GROUPS:
        for dog in ("cheddar", "cocoa"):
            cells = extract(SOURCE_ROOT / f"{dog}_idle_{group}_v01.png")
            scale = min((CANVAS[0] - PADDING_X * 2) / max(c.width for c in cells),
                        (BASELINE_Y - 12) / max(c.height for c in cells), 1.0)
            for index, cell in enumerate(cells):
                if scale < 1.0:
                    cell = cell.resize((round(cell.width * scale), round(cell.height * scale)), Image.Resampling.LANCZOS)
                canvas = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
                canvas.alpha_composite(cell, ((CANVAS[0] - cell.width) // 2, BASELINE_Y - cell.height))
                output = OUTPUT_ROOT / dog.capitalize() / "Motion" / f"{dog}_idle_{directions[index // 4]}_{index % 4:02d}.png"
                output.parent.mkdir(parents=True, exist_ok=True)
                canvas.save(output, optimize=True)
                exported += 1
    print(f"Exported {exported} directional idle frames.")


if __name__ == "__main__":
    main()
