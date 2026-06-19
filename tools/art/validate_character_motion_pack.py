#!/usr/bin/env python3
"""Report completeness and technical validity of generated character motion assets."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
MANIFEST = ROOT / "tools/art/character_motion_manifest.json"
BOARD_ROOT = ROOT / "unity/CheddarAndCocoa/Assets/Art/ReferenceOnly/GeneratedCharacterMotion"
RUNTIME_ROOT = ROOT / "unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Characters/Dogs"


def directions(value: object, all_directions: list[str]) -> list[str]:
    return all_directions if value == "all" else list(value)


def inspect_png(path: Path, require_alpha: bool) -> tuple[bool, str]:
    try:
        with Image.open(path) as image:
            if image.format != "PNG":
                return False, f"not PNG ({image.format})"
            if image.width < 256 or image.height < 256:
                return False, f"too small ({image.width}x{image.height})"
            if require_alpha and image.mode not in {"RGBA", "LA"}:
                return False, f"no alpha channel ({image.mode})"
            if require_alpha:
                alpha = image.getchannel("A")
                extrema = alpha.getextrema()
                if extrema == (255, 255):
                    return False, "alpha channel is fully opaque"
            return True, f"{image.width}x{image.height} {image.mode}"
    except Exception as error:  # pragma: no cover - command-line diagnostics
        return False, str(error)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--require-boards", action="store_true")
    parser.add_argument("--require-runtime", action="store_true")
    args = parser.parse_args()

    data = json.loads(MANIFEST.read_text(encoding="utf-8"))
    missing_boards: list[str] = []
    invalid_boards: list[str] = []
    board_names = data["review_boards"] + data.get("tier_a_boards", [])
    for name in board_names:
        path = BOARD_ROOT / name
        if not path.exists():
            missing_boards.append(name)
            continue
        valid, detail = inspect_png(path, require_alpha=False)
        print(f"board {'OK' if valid else 'INVALID'} {name}: {detail}")
        if not valid:
            invalid_boards.append(name)

    expected_runtime: list[Path] = []
    for dog in data["dogs"]:
        for clip in data["clips"]:
            for direction in directions(clip["directions"], data["directions"]):
                for frame in range(clip["frames"]):
                    expected_runtime.append(
                        RUNTIME_ROOT / dog.capitalize() / "Motion" /
                        f"{dog}_{clip['name']}_{direction}_{frame:02d}.png"
                    )
    missing_runtime = [path for path in expected_runtime if not path.exists()]
    invalid_runtime: list[Path] = []
    for path in expected_runtime:
        if not path.exists():
            continue
        valid, detail = inspect_png(path, require_alpha=True)
        if not valid:
            print(f"runtime INVALID {path.relative_to(ROOT)}: {detail}")
            invalid_runtime.append(path)

    print(f"source boards: {len(board_names) - len(missing_boards)}/{len(board_names)}")
    print(f"runtime frames: {len(expected_runtime) - len(missing_runtime)}/{len(expected_runtime)}")
    print(f"planned runtime frames: {len(expected_runtime)}")
    if missing_boards:
        print("missing boards: " + ", ".join(missing_boards))

    failed = bool(invalid_boards or invalid_runtime)
    failed |= args.require_boards and bool(missing_boards)
    failed |= args.require_runtime and bool(missing_runtime)
    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
