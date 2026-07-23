#!/usr/bin/env python3
"""Promote the reviewed flat-cel backyard plate while preserving its runtime contract."""

from pathlib import Path
from shutil import copyfile

from PIL import Image


SOURCE = Path(
    "unity/CheddarAndCocoa/Assets/Art/ReferenceOnly/GeneratedBackyardMegapass/"
    "yard_backyard_plate_storybook_v03.png"
)
RUNTIME = Path(
    "unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props/Environment/"
    "yard_backyard_plate_v02.png"
)
EXPECTED_SIZE = (1672, 941)


def main():
    if not SOURCE.exists():
        raise FileNotFoundError(f"Missing approved source: {SOURCE}")
    with Image.open(SOURCE) as image:
        if image.size != EXPECTED_SIZE:
            raise ValueError(
                f"Approved backyard must remain {EXPECTED_SIZE}, got {image.size}"
            )
        if image.mode not in ("RGB", "RGBA"):
            raise ValueError(f"Expected RGB/RGBA backyard source, got {image.mode}")

    RUNTIME.parent.mkdir(parents=True, exist_ok=True)
    copyfile(SOURCE, RUNTIME)
    print(f"wrote {RUNTIME}")


if __name__ == "__main__":
    main()
