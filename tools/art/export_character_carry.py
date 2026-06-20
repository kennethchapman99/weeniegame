#!/usr/bin/env python3
"""Extract prop-free two-frame carry strips into normalized alpha sprites."""

from PIL import Image
from export_character_motion_tier_a import BASELINE_Y, CANVAS, OUTPUT_ROOT, PADDING_X, SOURCE_ROOT, connected_background_mask


def main():
    for dog in ("cheddar", "cocoa"):
        with Image.open(SOURCE_ROOT / f"{dog}_carry_east_v01.png") as opened:
            sheet = opened.convert("RGBA")
        cells = []
        for column in range(2):
            cell = sheet.crop((round(column * sheet.width / 2) + 8, 8,
                               round((column + 1) * sheet.width / 2) - 8, sheet.height - 18))
            background = connected_background_mask(cell)
            alpha = Image.eval(background, lambda value: 255 - value)
            cell.putalpha(alpha)
            bbox = alpha.point(lambda value: 255 if value > 12 else 0).getbbox()
            if bbox is None:
                raise RuntimeError(f"No carry sprite in {dog} column {column}")
            cells.append(cell.crop(bbox))
        scale = min((CANVAS[0] - PADDING_X * 2) / max(c.width for c in cells),
                    (BASELINE_Y - 12) / max(c.height for c in cells), 1.0)
        for frame, cell in enumerate(cells):
            if scale < 1.0:
                cell = cell.resize((round(cell.width * scale), round(cell.height * scale)), Image.Resampling.LANCZOS)
            canvas = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
            canvas.alpha_composite(cell, ((CANVAS[0] - cell.width) // 2, BASELINE_Y - cell.height))
            output = OUTPUT_ROOT / dog.capitalize() / "Motion" / f"{dog}_carry_e_{frame:02d}.png"
            output.parent.mkdir(parents=True, exist_ok=True)
            canvas.save(output, optimize=True)
    print("Exported 4 carry frames.")


if __name__ == "__main__":
    main()
