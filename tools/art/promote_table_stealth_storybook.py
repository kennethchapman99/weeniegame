#!/usr/bin/env python3
"""Promote the reviewed Table Stealth storybook state family into Resources."""

from pathlib import Path

from PIL import Image


REFERENCE = Path(
    "unity/CheddarAndCocoa/Assets/Art/ReferenceOnly/GeneratedTableStealthStorybook"
)
RUNTIME = Path(
    "unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props/TableStealth"
)
ASSETS = (
    "table_stealth_human_watching",
    "table_stealth_human_distracted",
    "table_stealth_human_spotted",
    "table_stealth_human_caught",
    "table_stealth_steak_available",
    "table_stealth_steak_sneak_progress",
    "table_stealth_steak_gone",
)


def main():
    RUNTIME.mkdir(parents=True, exist_ok=True)
    for name in ASSETS:
        source = REFERENCE / f"{name}.png"
        if not source.exists():
            raise FileNotFoundError(f"Missing approved source: {source}")
        image = Image.open(source).convert("RGBA")
        expected = (512, 512)
        if image.size != expected:
            image = image.resize(expected, Image.Resampling.LANCZOS)
        destination = RUNTIME / f"{name}.png"
        image.save(destination)
        print(f"wrote {destination}")


if __name__ == "__main__":
    main()
