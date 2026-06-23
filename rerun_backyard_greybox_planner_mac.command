#!/bin/bash
# rerun_backyard_greybox_planner_mac.command
#
# Double-click (or run) to rebuild the backyard MAP-BASED GREYBOX PLANNER data:
#   - copies the aerial map into Unity
#   - converts the annotated export_map.HEIC to PNG (via macOS `sips`)
#   - indexes the numbered reference photos into capture points + tags
#   - writes manifest.json / manifest.md / backyard_capture_points.json
#   - writes external HTML contact sheets (photos are NOT pasted into Unity)
#
# After this finishes, open Unity and run:
#   Cheddar & Cocoa > Backyard > Build Map-Based Greybox Planner

set -euo pipefail

REPO="$(cd "$(dirname "$0")" && pwd)"
IMPORTER="$REPO/backyard_reference_importer_tool/tools/backyard_importer/import_backyard_reference.py"
SOURCE="$REPO/MAPS - template/01_exports_flat"
AERIAL="$REPO/MAPS - template/Backyard - Aerial.png"
ANNOTATED="$REPO/MAPS - template/01_exports_flat/export_map.HEIC"
UNITY="$REPO/unity/CheddarAndCocoa"

echo ""
echo "=================================================="
echo "  Backyard Map-Based Greybox Planner — importer"
echo "=================================================="
echo "Photos:     $SOURCE"
echo "Aerial:     $AERIAL"
echo "Annotated:  $ANNOTATED"
echo "Unity:      $UNITY"
echo ""

python3 "$IMPORTER" \
  --source    "$SOURCE" \
  --aerial    "$AERIAL" \
  --annotated "$ANNOTATED" \
  --unity     "$UNITY"

echo ""
echo "=================================================="
echo "  Import complete."
echo ""
echo "  Next, in Unity:"
echo "    Cheddar & Cocoa > Backyard > Build Map-Based Greybox Planner"
echo ""
echo "  Then drag the point_xxx markers onto the aerial map and run:"
echo "    Cheddar & Cocoa > Backyard > Save Capture Point Positions"
echo "=================================================="
echo ""

# Keep the terminal window open when double-clicked from Finder.
if [ -t 0 ]; then
  read -n 1 -s -r -p "Press any key to close..."
  echo ""
fi
