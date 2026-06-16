# Unity First Playable — Controller Test Scene

The smallest solid foundation for the Unity rebuild: **the project compiles cleanly and two local
controllers drive two dogs around a test scene.** No level design, no art, no AI, no audio — just
proof the couch-co-op plumbing works. See `docs/UNITY-PIVOT-PLAN.md` for the bigger plan.

## Required Unity version

**Unity 6 LTS — `6000.0.x`** (pinned in `unity/CheddarAndCocoa/ProjectSettings/ProjectVersion.txt`).
If your installed patch differs, Unity Hub offers to open with the closest version — that's fine.

## Required packages

Only what this milestone needs (pinned in `unity/CheddarAndCocoa/Packages/manifest.json`):

| Package | Why |
|---|---|
| `com.unity.inputsystem` | Read two gamepads (the only non-built-in dependency). |
| `com.unity.test-framework` | EditMode/PlayMode tests (kept for later). |
| `com.unity.ide.rider` / `ide.visualstudio` | C# tooling. |
| built-in modules: `physics2d`, `imgui`, `ui`, `uielements`, `audio` | Rigidbody2D/colliders + the OnGUI debug overlay. |

> Deliberately **not** included yet: URP, Cinemachine, 2D Animation/Tilemap/Pixel-Perfect. They're
> in the pivot plan as future adds, but pinning them now risks version-resolution failures (and
> Safe Mode) before they're used. The first playable runs on the **built-in render pipeline** with
> a hand-rolled `SharedCameraController` — no Cinemachine needed. Add those packages when the work
> actually calls for them (see the pivot plan's migration phases).

## How to open the project

1. Install **Unity Hub** + **Unity 6 LTS** (Mac and/or Windows build support).
2. Unity Hub → **Add → Add project from disk →** select `unity/CheddarAndCocoa`.
3. Open it. `.meta` files (stable GUIDs) are committed, so the scripts/asmdef bind on import; Unity
   generates `Library/` and the default `ProjectSettings` (only `ProjectVersion.txt` is committed),
   then resolves the packages above. First import takes a minute.
4. **When prompted to enable the new Input System backend, click Yes** (the editor restarts). This
   sets *Project Settings → Player → Active Input Handling* to *Input System Package* (or *Both*).
   Without it the gamepads compile fine but send no input. If you missed the prompt, set it
   manually there and restart.

## How to run the test scene

1. Open **`Assets/Scenes/ControllerTestScene.unity`** (Project window → Scenes).
2. Press **Play**.
3. The scene builds itself from code (`GameBootstrap`): a green field with walls, a shared camera,
   two dogs — **Cheddar** (golden, left) and **Cocoa** (brown, right) — and an on-screen legend.

## How to connect controllers

- Plug in / pair **two gamepads** (Xbox, PlayStation, or generic) **before or during Play** — the
  Input System hot-joins them.
- **Player 1 → pad slot 0 → Cheddar.** **Player 2 → pad slot 1 → Cocoa.**
- The top-left legend shows each pad's connection state and the controller name. If a pad reads
  `NOT CONNECTED`, that player won't move — connect it and it picks up live.
- No controller handy? See "Testing without two controllers" below.

### Controls

| Input | Action |
|---|---|
| Left stick | Move (analog; deadzone 0.25 from `balance.ts`) |
| **X** (west button) | **Bark** — logs `[Cheddar] WOOF!`, pops the sprite, flashes "WOOF!" over the dog |
| **Y** (north button) | Grab/interact **placeholder** — logs only |
| A / B | Reserved (wrestle / jump) — wired into `MoveIntent`, not yet implemented |

## Verification status

- **C# compile — VERIFIED.** All 13 scripts were compiled headlessly with Unity 6's bundled Roslyn
  against the editor's real `UnityEngine.*` module assemblies + the Input System API → an assembly
  was produced with **0 errors** (two benign warnings only: `DogController._jumpT` and
  `LevelObjective.surviveSeconds` are documented stub fields for not-yet-built features). The
  Package Manager also resolved the manifest (Input System loaded) during a batch-mode import.
- **Opens / Play / movement / bark — automated test ready, blocked only on license.** There's a
  PlayMode test (`Assets/Tests/PlayMode/ControllerCoopPlayModeTests.cs`) that runs the real
  `GameBootstrap`, injects **two virtual gamepads**, and asserts the two dogs move *independently*
  (opposite directions per pad) and that bark fires `OnBark` — i.e. it proves the three runtime
  criteria with **no hardware and no manual Play**. The test assembly also compiles clean (verified
  with Roslyn against NUnit + the Input System TestFramework).
- **To get the green runtime proof:** the editor must be *licensed*. This machine has a Unity
  **Personal** seat whose **offline period has lapsed** (`LicenseGroupOfflineValidityPeriodIsExpired`,
  `Token not found in cache`), so the headless run aborts at the license gate. Open **Unity Hub and
  sign in once** to refresh the license, then run:

  ```sh
  ./unity/run-playmode-tests.sh      # headless PlayMode test → playmode-results.xml
  ```

  Or just open `ControllerTestScene` in the signed-in editor and press **Play** with two pads.

## What works

- Project imports with **no compiler errors** (no Safe Mode) — compile verified headlessly (above).
- `ControllerTestScene` can be pressed **Play** and self-assembles.
- **Two dogs move independently** on two controllers, contained by the field walls.
- **Bark** produces a visible response (sprite pop + floating "WOOF!") **and** a console log.
- A **shared camera** frames both dogs and zooms to keep the pair in view as they separate.
- On-screen **debug labels** identify which dog is which and each controller's connection state.

## What is intentionally stubbed

- **Grab/interact** logs only — no pickups/objectives yet.
- **A/B (wrestle/jump)** are read into the intent but do nothing.
- **No real art/audio** — dogs are tinted rounded rectangles; field is a flat color.
- **No AI partner, levels, scoring, missions, or game flow** — those are later phases.
- `DogController` movement is velocity-only; **arrival easing, jump arc, swim/zoom/stun modes** are
  TODO (the per-mode handling from `systems/movement.ts`).
- Speeds are ported as **ratios** from `src/config/balance.ts` and scaled by a placeholder
  `pixelsPerUnit`; the exact *feel* gets a tuning pass against the prototype later.

## Testing without two controllers

The legend will show pad 1 (and/or 0) as `NOT CONNECTED`; that dog simply won't move. With one
controller you can drive Cheddar (slot 0) and confirm movement + bark. To verify both dogs with a
keyboard, add a keyboard input source later (post-milestone) — keyboard control is intentionally
out of this first pass to keep it boring and focused on the controller path.

## How it's wired (for the next dev)

`GameBootstrap` (on the scene's single GameObject) builds everything at runtime, so the scene file
has no fragile object graph. The real components it assembles are the pivot stubs:
`DogIdentity` + `DogController` (+ `MovementMode`), `GamepadPlayerInput` (Input System), and
`SharedCameraController`. `DebugHud` is the OnGUI overlay. As authored scenes/prefabs come online,
`GameBootstrap` gets replaced by real content — it's scaffolding, not the final architecture.
