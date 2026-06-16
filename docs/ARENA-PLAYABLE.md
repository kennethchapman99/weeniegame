# Arena First Playable — 60-second treat-grab co-op

The first **actually playable loop** for Cheddar & Cocoa: two players, one shared score, a clock,
treats to grab, and a restart. It builds on the verified controller baseline
(`docs/UNITY-FIRST-PLAYABLE.md`) without changing it — that scene/test still proves two controllers
move two dogs. This one adds the *game*.

## What it is

A small walled arena with two dogs — **Cheddar** (golden, left) and **Cocoa** (brown, right) —
and **5 "weenie" treats** scattered around. Touch a treat to collect it; the **shared score** goes
up and a fresh treat spawns. A **60-second timer** counts down; at zero the round ends, shows the
**final score**, and waits for a restart. Barking makes a visible **expanding ring** (plus the
sprite pop and a "WOOF!" flash) — not just a console log.

Everything is generated from code by `ArenaBootstrap` (no art/prefab dependencies), so the scene
file is a single GameObject.

## How to run it

1. Open the project in **Unity 6 LTS (`6000.0.x`)** (see `docs/UNITY-FIRST-PLAYABLE.md` for setup).
2. Open **`Assets/Scenes/ArenaScene.unity`**.
3. Press **Play**.

## Controls

You can play with **two controllers, two keyboard layouts, or a mix** — controller and keyboard are
read together per player.

| Player | Dog | Controller | Keyboard | Bark |
|---|---|---|---|---|
| **P1** | Cheddar (golden) | pad 0, left stick | **W A S D** | controller **X** / **Space** |
| **P2** | Cocoa (brown) | pad 1, left stick | **Arrow keys** | controller **X** / **Enter** or **Right-Shift** |

No controllers needed — the keyboard fallback alone is enough for two people on one keyboard.

## How to play

- **Move** onto treats to collect them. Each collect = **+1 shared score** and a new treat appears.
- **Bark** for feedback/juice (a ring pulse). It doesn't score yet — it's the core verb being kept
  alive for the united-front mechanics to come.
- When the **timer hits 0**, the round ends and the **final score** card appears.
- **Restart** with **R**, **Enter**, or a controller **Start**/**A** button (or click the on-screen
  **Restart** button). Score and timer reset; dogs return to their start spots.

## What to check by hand (manual acceptance)

| Check | Expected |
|---|---|
| Scene loads | Green arena, two dogs, top-center `SCORE 0` and a countdown |
| Both players move independently | P1 keys/pad move Cheddar; P2 keys/pad move Cocoa — no cross-talk |
| Collect a treat | Score increments by 1; a replacement treat appears |
| Bark | Visible ring pulse + sprite pop + "WOOF!" over the dog |
| Timer expires | "TIME!" card with the final score; dogs stop accepting input |
| Restart | Score back to 0, timer refilled, dogs re-homed |

## Automated proof

The headless PlayMode test `Assets/Tests/PlayMode/ArenaGameLoopPlayModeTests.cs` loads ArenaScene
and asserts: both dogs exist, score starts at 0, collecting a treat increments the score (and
respawns it), the countdown reaches game-over, and restart resets score + timer. It runs alongside
the original controller-movement test:

```sh
./unity/run-playmode-tests.sh      # runs ALL PlayMode tests → unity/playmode-results.xml
```

> Both scenes are registered in `ProjectSettings/EditorBuildSettings.asset` (ArenaScene first) so
> the test can load ArenaScene by name and so a build opens straight into the playable loop.
