# Cheddar & Cocoa — Unity project

This is the **Unity rebuild** of Cheddar & Cocoa, the local couch co-op dachshund game. It lives
beside the original web/Canvas build (repo root: `src/`, `prototype/`), which remains the
**design / balance / behavior spec**. See `../../docs/UNITY-PIVOT-PLAN.md` for the full plan.

> ⚠️ This folder is a **hand-authored scaffold**, not yet a Unity-generated project. It has the
> folder layout, `Packages/manifest.json`, a pinned `ProjectSettings/ProjectVersion.txt`, an
> assembly definition, and compiling C# stubs. The first time you open it, Unity will generate
> `Library/`, `.meta` files, default `ProjectSettings/*`, and resolve packages. Nothing here
> assumes Unity is installed — these are plain files you open later.

## Opening it (first time)

1. Install **Unity Hub** and **Unity 6 LTS** (`6000.0.x` — the pin in `ProjectSettings/ProjectVersion.txt`).
   Include the **Mac** and (optionally) **Windows** build support modules. If your installed
   patch differs, Unity Hub will offer to open with the closest version — that's fine.
2. In Unity Hub: **Add → Add project from disk →** select `unity/CheddarAndCocoa`.
3. Open it. Unity resolves the packages in `Packages/manifest.json` (Input System, URP,
   Cinemachine, 2D, Test Framework). If a pinned package version doesn't resolve, open
   **Window → Package Manager** and let it pick the version compatible with your editor.
4. When prompted to enable the **new Input System** backend, choose **Yes** (restarts the editor).
5. Set up URP if not auto-configured: **Assets → Create → Rendering → URP Asset (with 2D Renderer)**,
   then assign it in **Project Settings → Graphics** and **Quality**.

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
