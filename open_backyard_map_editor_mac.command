#!/bin/bash
# Double-click to open the backyard map editor in your browser.
# Drag the numbered circles to the right spots, then click Save.
# No Unity required for this step.
REPO="$(cd "$(dirname "$0")" && pwd)"

# Kill any old instance on port 5500 before starting a fresh one.
OLD=$(lsof -ti:5500 2>/dev/null)
if [ -n "$OLD" ]; then
  echo "Stopping previous server (pid $OLD)..."
  kill "$OLD" 2>/dev/null || true
  sleep 0.5
fi

python3 "$REPO/backyard_reference_importer_tool/tools/backyard_importer/map_editor_server.py"
