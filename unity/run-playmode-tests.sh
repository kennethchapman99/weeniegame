#!/usr/bin/env bash
# Headless runtime proof for the Unity first playable: runs the PlayMode test that injects two
# virtual gamepads and asserts the two dogs move independently and bark fires. No physical
# controllers and no manual "press Play" needed.
#
# REQUIRES a licensed editor. If Unity exits with a license error, open Unity Hub and sign in once
# to refresh the local editor license, then re-run this script.
set -euo pipefail

PROJ="$(cd "$(dirname "$0")/CheddarAndCocoa" && pwd)"

# Pick the newest installed 6000.0.x editor (project pins 6000.0.32f1; any 6000.0.x is compatible).
HUB_EDITORS="/Applications/Unity/Hub/Editor"
UNITY=""
if [ -d "$HUB_EDITORS" ]; then
  while IFS= read -r ver; do
    cand="$HUB_EDITORS/$ver/Unity.app/Contents/MacOS/Unity"
    [ -x "$cand" ] && { UNITY="$cand"; break; }
  done < <(find "$HUB_EDITORS" -maxdepth 1 -mindepth 1 -type d -name '6000.0.*' -exec basename {} \; | sort -t. -k1,1nr -k2,2nr -k3,3nr)
fi
[ -n "$UNITY" ] || { echo "No 6000.0.x Unity editor found under $HUB_EDITORS" >&2; exit 1; }

RESULTS="$PROJ/../playmode-results.xml"
echo "Editor:  $UNITY"
echo "Project: $PROJ"
echo "Running PlayMode tests..."

"$UNITY" -runTests -batchmode -projectPath "$PROJ" \
  -testPlatform PlayMode \
  -testResults "$RESULTS" \
  -logFile - \
  -nographics

code=$?
echo "Exit: $code   Results XML: $RESULTS"
exit $code
