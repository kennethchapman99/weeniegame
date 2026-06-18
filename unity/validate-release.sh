#!/usr/bin/env bash
# Full local release-candidate gate: tests, release build, metadata, and packaged startup.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
APP="$SCRIPT_DIR/builds/release/CheddarAndCocoa-Demo.app"
PLIST="$APP/Contents/Info.plist"

"$SCRIPT_DIR/run-playmode-tests.sh"
"$SCRIPT_DIR/build-release.sh"

[ -f "$PLIST" ] || { echo "Release Info.plist not found: $PLIST" >&2; exit 1; }
[ "$(/usr/libexec/PlistBuddy -c 'Print :CFBundleName' "$PLIST")" = "Cheddar and Cocoa" ] || {
  echo "Unexpected release product name." >&2
  exit 1
}
[ "$(/usr/libexec/PlistBuddy -c 'Print :CFBundleIdentifier' "$PLIST")" = "com.kennethchapman.cheddarandcocoa" ] || {
  echo "Unexpected release bundle identifier." >&2
  exit 1
}
[ "$(/usr/libexec/PlistBuddy -c 'Print :CFBundleShortVersionString' "$PLIST")" = "0.1.0" ] || {
  echo "Unexpected release version." >&2
  exit 1
}

"$SCRIPT_DIR/smoke-player.sh" "$APP"
echo "Release validation complete."
