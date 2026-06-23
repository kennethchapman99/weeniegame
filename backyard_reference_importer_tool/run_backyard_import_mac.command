#!/bin/bash
# Runs the backyard map-based greybox planner importer from the repo root.
set -e
cd "$(dirname "$0")/.."   # repo root (this tool lives in backyard_reference_importer_tool/)

SOURCE="${1:-MAPS - template/01_exports_flat}"
AERIAL="${2:-MAPS - template/Backyard - Aerial.png}"
ANNOTATED="${3:-MAPS - template/01_exports_flat/export_map.HEIC}"
UNITY="${4:-unity/CheddarAndCocoa}"

echo "Backyard Map-Based Greybox Planner — importer"
echo "Source:    $SOURCE"
echo "Aerial:    $AERIAL"
echo "Annotated: $ANNOTATED"
echo "Unity:     $UNITY"

if ! command -v python3 >/dev/null 2>&1; then
  echo "ERROR: python3 not found."; read -p "Press Enter to exit..."; exit 1
fi
if [ ! -d "$SOURCE" ]; then
  echo "ERROR: Source folder not found: $SOURCE"; read -p "Press Enter to exit..."; exit 1
fi
if [ ! -d "$UNITY" ]; then
  echo "ERROR: Unity project not found: $UNITY"; read -p "Press Enter to exit..."; exit 1
fi

python3 backyard_reference_importer_tool/tools/backyard_importer/import_backyard_reference.py \
  --source "$SOURCE" --aerial "$AERIAL" --annotated "$ANNOTATED" --unity "$UNITY"

echo ""
echo "Done. Next, in Unity: Cheddar & Cocoa > Backyard > Build Map-Based Greybox Planner"
read -p "Press Enter to close..."
