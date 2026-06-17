#!/usr/bin/env bash
# Deterministic local validation for the ArenaScene demo slice.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJ="$SCRIPT_DIR/CheddarAndCocoa"
SCENE="$PROJ/Assets/Scenes/ArenaScene.unity"
BOOTSTRAP="$PROJ/Assets/Scripts/Bootstrap/ArenaBootstrap.cs"
BOOTSTRAP_META="$BOOTSTRAP.meta"
BUILD_SETTINGS="$PROJ/ProjectSettings/EditorBuildSettings.asset"
RUN_BUILD=1

if [ "${1:-}" = "--skip-build" ]; then
  RUN_BUILD=0
fi

require_file() {
  [ -f "$1" ] || { echo "Missing: $1" >&2; exit 1; }
}

echo "Validating Unity project path..."
[ -d "$PROJ" ] || { echo "Missing Unity project: $PROJ" >&2; exit 1; }

echo "Validating ArenaScene..."
require_file "$SCENE"
require_file "$BUILD_SETTINGS"
grep -q 'Assets/Scenes/ArenaScene.unity' "$BUILD_SETTINGS" || {
  echo "ArenaScene is not listed in EditorBuildSettings.asset" >&2
  exit 1
}

echo "Validating ArenaBootstrap..."
require_file "$BOOTSTRAP"
require_file "$BOOTSTRAP_META"
grep -q 'public sealed class ArenaBootstrap' "$BOOTSTRAP" || {
  echo "ArenaBootstrap class declaration not found" >&2
  exit 1
}
BOOTSTRAP_GUID="$(awk '/^guid:/ { print $2; exit }' "$BOOTSTRAP_META")"
[ -n "$BOOTSTRAP_GUID" ] || { echo "ArenaBootstrap meta file has no guid" >&2; exit 1; }
grep -q "m_Name: ArenaBootstrap" "$SCENE" || {
  echo "ArenaBootstrap GameObject not found in ArenaScene" >&2
  exit 1
}
grep -q "guid: $BOOTSTRAP_GUID" "$SCENE" || {
  echo "ArenaScene does not reference ArenaBootstrap script guid $BOOTSTRAP_GUID" >&2
  exit 1
}

echo "Running PlayMode tests..."
"$SCRIPT_DIR/run-playmode-tests.sh"

if [ "$RUN_BUILD" -eq 1 ]; then
  echo "Creating local development build..."
  "$SCRIPT_DIR/build-dev.sh"
else
  echo "Skipping development build by request. To build: ./unity/build-dev.sh"
fi

echo "Demo validation complete."
