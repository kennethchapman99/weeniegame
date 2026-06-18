#!/usr/bin/env bash
# Starts a packaged macOS player long enough to prove runtime initialization, then stops it.
set -euo pipefail

APP="${1:-}"
[ -n "$APP" ] || { echo "Usage: $0 /path/to/Game.app" >&2; exit 1; }
[ -d "$APP" ] || { echo "App not found: $APP" >&2; exit 1; }

EXECUTABLE_NAME="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleExecutable' "$APP/Contents/Info.plist")"
EXECUTABLE="$APP/Contents/MacOS/$EXECUTABLE_NAME"
[ -x "$EXECUTABLE" ] || { echo "Player executable not found: $EXECUTABLE" >&2; exit 1; }

LOG="$(mktemp -t cheddar-cocoa-player).log"
cleanup() {
  rm -f "$LOG"
}
trap cleanup EXIT

"$EXECUTABLE" -batchmode -nographics -logFile "$LOG" &
PID=$!

for _ in 1 2 3 4 5; do
  sleep 1
  kill -0 "$PID" 2>/dev/null || {
    wait "$PID" || true
    echo "Packaged player exited during startup." >&2
    tail -80 "$LOG" >&2
    exit 1
  }
done

kill "$PID" 2>/dev/null || true
wait "$PID" 2>/dev/null || true

if grep -Eqi 'NullReferenceException|MissingReferenceException|Crash!!!|Aborting batchmode due to failure' "$LOG"; then
  echo "Packaged player logged a startup error." >&2
  tail -80 "$LOG" >&2
  exit 1
fi

grep -q "Initialize engine version" "$LOG" || {
  echo "Packaged player never initialized the Unity engine." >&2
  tail -80 "$LOG" >&2
  exit 1
}

echo "Packaged player startup smoke passed: $APP"
