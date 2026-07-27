#!/usr/bin/env python3
"""Normalize the approved V03 jump/wrestle/interact boards into runtime frames."""

from pathlib import Path

from collections import deque

from PIL import Image, ImageChops, ImageFilter


ROOT = Path(__file__).resolve().parents[2]
SOURCE_ROOT = ROOT / "unity/CheddarAndCocoa/Assets/Art/ReferenceOnly/GeneratedCharacterMotion"
OUTPUT_ROOT = ROOT / "unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Characters/Dogs"
CLIPS = ("jump", "wrestle", "interact")
CANVAS = (512, 384)
BASELINE_Y = 360
PADDING_X = 16
ALPHA_THRESHOLD = 12


def remove_small_alpha_islands(cell: Image.Image) -> Image.Image:
    """Drop detached generation specks while preserving every substantial silhouette component."""
    alpha = cell.getchannel("A")
    width, height = alpha.size
    pixels = alpha.load()
    seen = bytearray(width * height)
    components: list[list[tuple[int, int]]] = []

    for start_y in range(height):
        for start_x in range(width):
            index = start_y * width + start_x
            if seen[index] or pixels[start_x, start_y] <= ALPHA_THRESHOLD:
                continue
            seen[index] = 1
            queue = deque([(start_x, start_y)])
            component: list[tuple[int, int]] = []
            while queue:
                x, y = queue.popleft()
                component.append((x, y))
                for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
                    if nx < 0 or nx >= width or ny < 0 or ny >= height:
                        continue
                    neighbor = ny * width + nx
                    if seen[neighbor] or pixels[nx, ny] <= ALPHA_THRESHOLD:
                        continue
                    seen[neighbor] = 1
                    queue.append((nx, ny))
            components.append(component)

    if not components:
        return cell
    minimum_area = max(64, round(max(len(component) for component in components) * 0.02))
    keep = Image.new("L", alpha.size, 0)
    keep_pixels = keep.load()
    for component in components:
        if len(component) < minimum_area:
            continue
        for x, y in component:
            keep_pixels[x, y] = 255
    keep = keep.filter(ImageFilter.MaxFilter(5))
    cleaned = cell.copy()
    cleaned.putalpha(ImageChops.multiply(alpha, keep))
    return cleaned


def extract_cells(sheet_path: Path) -> list[Image.Image]:
    with Image.open(sheet_path) as opened:
        sheet = opened.convert("RGBA")

    cells: list[Image.Image] = []
    for row in range(3):
        for column in range(4):
            left = round(column * sheet.width / 4) + 4
            top = round(row * sheet.height / 3) + 4
            right = round((column + 1) * sheet.width / 4) - 4
            bottom = round((row + 1) * sheet.height / 3) - 4
            cell = remove_small_alpha_islands(sheet.crop((left, top, right, bottom)))
            alpha = cell.getchannel("A")
            bbox = alpha.point(
                lambda value: 255 if value > ALPHA_THRESHOLD else 0
            ).getbbox()
            if bbox is None:
                raise RuntimeError(
                    f"No action sprite in {sheet_path.name} row {row} column {column}"
                )
            cells.append(cell.crop(bbox))
    return cells


def export_dog(dog: str) -> int:
    source = SOURCE_ROOT / f"{dog}_action_jump_wrestle_interact_v03.png"
    cells = extract_cells(source)
    scale = min(
        (CANVAS[0] - PADDING_X * 2) / max(cell.width for cell in cells),
        (BASELINE_Y - 12) / max(cell.height for cell in cells),
        1.0,
    )
    folder = OUTPUT_ROOT / dog.capitalize() / "Motion"
    folder.mkdir(parents=True, exist_ok=True)

    for index, cell in enumerate(cells):
        if scale < 1.0:
            cell = cell.resize(
                (round(cell.width * scale), round(cell.height * scale)),
                Image.Resampling.LANCZOS,
            )
        canvas = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
        canvas.alpha_composite(
            cell,
            ((CANVAS[0] - cell.width) // 2, BASELINE_Y - cell.height),
        )
        clip = CLIPS[index // 4]
        frame = index % 4
        canvas.save(folder / f"{dog}_{clip}_e_{frame:02d}.png", optimize=True)
    return len(cells)


def write_contact_sheet(path: Path) -> None:
    cell_width, cell_height = 260, 200
    contact = Image.new("RGB", (cell_width * 4, cell_height * 6), (44, 71, 38))
    for dog_index, dog in enumerate(("cheddar", "cocoa")):
        for clip_index, clip in enumerate(CLIPS):
            for frame in range(4):
                source = (
                    OUTPUT_ROOT
                    / dog.capitalize()
                    / "Motion"
                    / f"{dog}_{clip}_e_{frame:02d}.png"
                )
                with Image.open(source) as opened:
                    sprite = opened.convert("RGBA")
                sprite.thumbnail(
                    (cell_width - 18, cell_height - 18), Image.Resampling.LANCZOS
                )
                x = frame * cell_width + (cell_width - sprite.width) // 2
                row = dog_index * len(CLIPS) + clip_index
                y = row * cell_height + (cell_height - sprite.height) // 2
                contact.paste(sprite, (x, y), sprite)
    path.parent.mkdir(parents=True, exist_ok=True)
    contact.save(path)


def main() -> None:
    exported = sum(export_dog(dog) for dog in ("cheddar", "cocoa"))
    contact = ROOT / "captures/character-motion-jump-wrestle-interact-v03.png"
    write_contact_sheet(contact)
    print(f"Exported {exported} normalized action frames.")
    print(f"Contact sheet: {contact}")


if __name__ == "__main__":
    main()
