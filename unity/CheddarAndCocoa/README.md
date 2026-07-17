# Cheddar & Cocoa — Unity project

This is the **Unity rebuild** of Cheddar & Cocoa, the local couch co-op dachshund game. It lives
beside the original web/Canvas build (repo root: `src/`, `prototype/`), which remains the
**design / balance / behavior spec**. See `../../docs/UNITY-PIVOT-PLAN.md` for the full plan.

This is the active Unity 6 project. `ArenaScene` contains the mission-select, two-player arena,
mission-controller roster, shared HUD/input/session flow, and the current family-showcase content.
The web/Canvas folders outside this project are frozen reference material.

> ▶️ **First playable:** open `Assets/Scenes/ControllerTestScene.unity` and press Play to drive two
> dogs with two controllers. Full instructions: **`../../docs/UNITY-FIRST-PLAYABLE.md`**.

> 🎮 **Playable loop:** open `Assets/Scenes/ArenaScene.unity` for the couch-co-op mission roster
> (mission select, briefing, gameplay, result, replay, and return flow). How to play:
> **`../../docs/ARENA-PLAYABLE.md`**.

The game intentionally remains on the built-in render pipeline and uses the existing Input System,
2D physics, UGUI/TextMeshPro mission select, and controller-owned mission presentation systems.

## Couch-test candidate verification — 2026-07-16

Candidate player:
`/Users/kchapman/Weeniegame/unity/builds/dev/CheddarAndCocoa-Arena.app`

- Full PlayMode suite: **551/551 passed**, 0 skipped, 57.9986 seconds
  (`unity/playmode-results.xml`, 2026-07-16 14:37:42 EDT).
- Development-player executable SHA-256:
  `b0d0c321e44a3100810aedb0cb15566bde39c081616ebd60d1f783253ecc1e67`.
- Packaged Operation Pee Break explainer SHA-256:
  `280de2f463f2f64d337937a16673c549a970e99a6d1c2bb3774a05b34d002e63`.
- Packaged-player startup smoke: passed after the fresh build.
- 16:9 player inspection: mission select and Operation Pee Break opening render correctly; Car Ride
  renders the authored cabin, both dogs, loose junk, driver objective, pressure bar, and P1/P2 chips
  inside the letterboxed game area.

Showcase-five manual acceptance checks for this candidate:

- Operation Pee Break: start from **Play Recommended**; verify explainer/controls freeze, contextual
  stations, number-free bladder bar, recoverable misread, united bark, live hydrant payoff, replay,
  failure, and return-to-select.
- Kitchen Falling Food Frenzy: verify Cheddar's counter bark, Cocoa's bowl catch/dodge role, readable
  gold/purple cause and effect, recoverable splats, Dinner Rush, and the live **DINNER SAVED** payoff
  before the result card.
- Car Ride Chaos: verify brace, jump, asymmetric slide, united-bark driver response, recoverable
  tumbles, fail/replay reset, and the live **WE'RE HOME** payoff before the result card.
- Baby Bird Bedlam: verify Cheddar grab/shake, Cocoa bark-defense, airlift/peck recovery, fail/replay
  reset, and the full-bellies/parent-give-up payoff before the result card.
- Gate Crash: verify Cocoa holds, Cheddar squeezes, gate snaps are understandable/recoverable, the
  claimed toy remains visible before the result card, and replay resets the gate/toy/snaps.
- Complete flow: bind two separate controllers, disconnect/reconnect each one in turn, then verify
  pause, replay, mission-select return, and independent P1 Cheddar/P2 Cocoa ownership.

No automated or packaged-startup critical blocker is known for this candidate. The checklist above
still requires the scheduled two-human, two-controller couch run; this record does **not** claim that
the human couch-test gate passed.

## Opening it (first time)

1. Install **Unity Hub** and **Unity 6 LTS** (`6000.0.x` — the pin in `ProjectSettings/ProjectVersion.txt`).
   Include the **Mac** and (optionally) **Windows** build support modules. If your installed
   patch differs, Unity Hub will offer to open with the closest version — that's fine.
2. In Unity Hub: **Add → Add project from disk →** select `unity/CheddarAndCocoa`.
3. Open it. Unity resolves the packages in `Packages/manifest.json` (Input System, Test Framework,
   IDE + built-in 2D physics/IMGUI). If a pinned version doesn't resolve, open **Window → Package
   Manager** and let it pick the version compatible with your editor.
4. When prompted to enable the **new Input System** backend, choose **Yes** (restarts the editor).
5. Open `Assets/Scenes/ControllerTestScene.unity` and press **Play** (see
   `../../docs/UNITY-FIRST-PLAYABLE.md`). URP/Cinemachine setup is **not** needed yet — the test
   scene uses the built-in pipeline.

## Layout

```
Assets/
  Scripts/            # C# (assembly: CheddarAndCocoa.asmdef)
    Dogs/             # DogIdentity, DogController (+ MovementMode)
    Input/            # GamepadPlayerInput (Unity Input System)
    Camera/           # SharedCameraController (one TV camera frames both dogs)
    Interactions/     # CoopInteraction (needs-both-dogs base), ToyInteractable, ScentTrail
    Objectives/       # LevelObjective (single mutation point)
    Minigames/        # TugOfWarMinigame
    Hazards/          # Hazard (predator/swat/water base)
    Data/             # DogTuning ScriptableObject (ports balance.ts numbers)
  Scenes/             # .unity scenes (Backyard vertical slice goes here)
  Prefabs/            # Dog, Toy, Gate, Hazard prefabs
  Art/                # sprites, sheets
  Animation/          # clips, controllers
  Audio/              # clips / mixer
  Data/               # ScriptableObject *assets* (DogTuning_Cheddar, _Cocoa, level data, …)
Packages/manifest.json
ProjectSettings/ProjectVersion.txt
```

## The stubs

Every script in `Assets/Scripts/` is a **compiling stub** with `PROTOTYPE MAP:` comments pointing
at the exact TypeScript module + `balance.ts` constants it must reproduce. They are deliberately
thin — wire them up during the vertical slice (see the pivot plan). Don't re-derive tuning
numbers; port them from `src/config/balance.ts` (catalogued in `../../docs/MECHANICS.md`).
