#!/usr/bin/env bash
# Headless runtime proof for the Unity first playable: runs the PlayMode test that injects two
# virtual gamepads and asserts the two dogs move independently and bark fires. No physical
# controllers and no manual "press Play" needed.
#
# REQUIRES a licensed editor. This repo's machine has a Unity *Personal* seat whose offline period
# has lapsed (see docs/UNITY-FIRST-PLAYABLE.md "Verification status"); open Unity Hub and sign in
# once to refresh the license, then re-run this script.
set -euo pipefail

PROJ="$(cd "$(dirname "$0")/CheddarAndCocoa" && pwd)"

# Pick the newest installed 6000.0.x editor (project pins 6000.0.32f1; any 6000.0.x is compatible).
HUB_EDITORS="/Applications/Unity/Hub/Editor"
UNITY=""
for ver in $(ls "$HUB_EDITORS" 2>/dev/null | grep '^6000\.0\.' | sort -V -r); do
  cand="$HUB_EDITORS/$ver/Unity.app/Contents/MacOS/Unity"
  [ -x "$cand" ] && { UNITY="$cand"; break; }
done
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
