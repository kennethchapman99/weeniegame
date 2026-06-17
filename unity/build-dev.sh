#!/usr/bin/env bash
# Creates a local macOS development build of the ArenaScene playable.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJ="$SCRIPT_DIR/CheddarAndCocoa"
HUB_EDITORS="/Applications/Unity/Hub/Editor"
OUTPUT="$SCRIPT_DIR/builds/dev/CheddarAndCocoa-Arena.app"

UNITY=""
if [ -d "$HUB_EDITORS" ]; then
  while IFS= read -r ver; do
    cand="$HUB_EDITORS/$ver/Unity.app/Contents/MacOS/Unity"
    [ -x "$cand" ] && { UNITY="$cand"; break; }
  done < <(find "$HUB_EDITORS" -maxdepth 1 -mindepth 1 -type d -name '6000.0.*' -exec basename {} \; | sort -t. -k1,1nr -k2,2nr -k3,3nr)
fi
[ -n "$UNITY" ] || { echo "No 6000.0.x Unity editor found under $HUB_EDITORS" >&2; exit 1; }

echo "Editor:  $UNITY"
echo "Project: $PROJ"
echo "Scene:   Assets/Scenes/ArenaScene.unity"
echo "Output:  $OUTPUT"

rm -rf "$OUTPUT"

"$UNITY" -quit -batchmode -nographics -projectPath "$PROJ" \
  -executeMethod CheddarAndCocoa.EditorTools.ArenaDevBuild.BuildDevMac \
  -logFile -

[ -d "$OUTPUT" ] || { echo "Build completed but output app was not found: $OUTPUT" >&2; exit 1; }
echo "Development build ready: $OUTPUT"
