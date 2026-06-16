# Unity Pivot Plan — Cheddar & Cocoa

> Status: **proposed direction** (owner decision pending sign-off, 2026-06-15). This is the
> bridge from the validated TypeScript/Canvas build to a serious Unity couch co-op game. It does
> **not** delete or stop the web build — that becomes the spec. Read `CLAUDE.md` (operating
> contract), `docs/COOP-VISION.md` (what we're building toward), and `docs/MECHANICS.md`
> (the balance numbers) alongside this.

---

## 1. Why Unity is now the target

The web/Canvas build (M0–M14) did exactly what it was for: it **proved the game is fun** and
**locked the balance** through a deterministic, sim-tested core. It is an excellent *spec*. It is
the wrong *long-term product shell* for where the game wants to go:

| Driver | Canvas/TS reality | What Unity buys |
|---|---|---|
| **Couch co-op on a TV, "It Takes Two" feel** | Hand-rolled camera, particles, and juice in 2D Canvas; every polish pass is bespoke code | First-class shared/Cinemachine cameras, particle systems, animation, post-processing, lighting — polish is tooling, not hand-code |
| **Two controllers, reliable** | `navigator.getGamepads()` polling + Tauri/webview quirks; couch input is fragile | Unity Input System: device pairing, hot-join, rumble, deadzones, rebinding — built for local multiplayer |
| **Content velocity (more missions)** | Each level is a TS painter + systems; no scene editor, no visual authoring | Scenes, Prefabs, ScriptableObjects, Tilemaps, an Inspector — designers/owner can author levels without touching engine code |
| **Animation & art depth** | Procedural dachshund drawn per-frame in code | Sprite/skeletal animation (2D Animation), blend trees, timeline — real character acting |
| **Future native platforms (tvOS/Apple TV, maybe consoles)** | Webview wrappers cap out at desktop + iOS; Apple TV via webview is shaky; consoles impossible | Unity ships native tvOS today and is a *real* console path later (separate dev programs) |
| **Audio** | Web Audio synth, no files | Audio mixer, spatial audio, real clips + the synth feel if wanted |

The expensive, risky work — *is it fun? are the numbers right?* — is **already done and won't be
thrown away**. The pivot is about giving that proven design a production-grade body.

**This is not an admission the TS build was wrong.** It was the correct, cheap way to de-risk the
design. We're cashing in that de-risking.

---

## 2. What is preserved from the web prototype

Everything. Nothing in `src/`, `prototype/`, or `docs/` is deleted by this pivot. Specifically:

| Asset | Role after pivot |
|---|---|
| `prototype/cheddar-and-cocoa.prototype.html` | **Behavior spec.** The living "this is how it should feel" reference. Keep runnable. |
| `src/` (the staged TS port, M0–M14) | **Design + balance + behavior reference**, and a **runnable oracle**: its headless sims encode the correct outcomes. Cross-check Unity systems against it. |
| `src/config/balance.ts`, `src/config/dogs.ts` | **Authoritative tuning.** Ported verbatim into Unity `DogTuning` + per-system fields. Do not re-derive. |
| `docs/MECHANICS.md` | **The balance bible** — every constant + rationale. Single source for the numbers. |
| `docs/COOP-VISION.md` | **The design pillars** (two dogs/one goal, interdependence, asymmetry, comedy). Still governs *what*. |
| `docs/ARCHITECTURE.md` | TS architecture; the `MovementMode` model + logic/render-separation lessons carry into C#. |
| `docs/BUILD-PLAN.md` | History of how the design was built + validated, milestone by milestone. |
| `docs/LEVEL-IDEAS.md`, `docs/ROOM-SCHEMA.md` | Unbuilt level concepts + the data-driven map seam — direct input to Unity level authoring. |
| `tests/sim/*` | The **acceptance oracle.** When a Unity system feels off, the TS sim says what the right answer is. |
| `src-tauri/` | The desktop/iOS webview wrapper for the TS build. Kept until the Unity desktop build supersedes it; not used by Unity. |

**Preserved-files contract:** no file under `prototype/`, `src/`, `docs/`, or `tests/` is removed
or rewritten by the Unity work. The Unity project is purely additive under `unity/`.

---

## 3. Target platforms

| Platform | When | How |
|---|---|---|
| **macOS desktop** (→ TV via HDMI) | **Vertical slice target** | Unity Standalone (Apple Silicon + Intel). Primary couch experience: Mac → big TV, two controllers. |
| **Windows desktop** (→ TV) | Same slice, cheap add | Unity Standalone Windows. Same input/code; just another build target. |
| **tvOS / Apple TV** | **After** the desktop slice is fun | Unity supports tvOS natively; controllers pair over Bluetooth. Re-verify input + perf on-device. |
| **iOS / iPadOS** | Optional later | Unity iOS build; touch + MFi controllers. Lower priority than TV. |
| **Consoles (PS5/Switch)** | Only if ever wanted | Separate track: platform dev programs + dev kits. Unity makes it *possible*; out of scope now. |

**Non-assumption:** no public distribution / store presence is assumed. Builds are for the owner's
own machines and TV. No App Store / Steam plumbing in scope.

---

## 4. Recommended Unity packages

Pinned in `unity/CheddarAndCocoa/Packages/manifest.json`. Editor: **Unity 6 LTS (6000.0.x)**.

| Package | Why |
|---|---|
| `com.unity.inputsystem` | Local multiplayer input: device pairing, hot-join, deadzones, rumble. Non-negotiable for couch co-op. |
| `com.unity.render-pipelines.universal` (URP, 2D Renderer) | 2D lights, post-processing (bloom/vignette — we already added a vignette in the TS build), performant on Mac/TV/tvOS. |
| `com.unity.cinemachine` | Shared "frame both dogs" camera (Target Group) without hand-rolling follow/zoom — though `SharedCameraController` is the dependency-light fallback. |
| `com.unity.2d.animation` + `2d.sprite` | Skeletal/sprite animation for the dachshunds (replaces the procedural renderer). |
| `com.unity.2d.pixel-perfect` | Crisp 2D scaling across TV resolutions (the prototype's DPR/letterbox concern, solved). |
| `com.unity.2d.tilemap` | Author level geometry/zones as data (the data-driven map seam from ROOM-SCHEMA.md). |
| `com.unity.test-framework` | EditMode/PlayMode tests — port the headless sim discipline. **Keep determinism testable.** |
| `com.unity.timeline` | Scripted mission moments (squirrel event, predator rescue, finale). |
| `com.unity.ugui` (incl. TextMeshPro) | HUD: objective checklist, combined score, lobby/join screen, result screen. |
| `com.unity.ide.rider` / `ide.visualstudio` | C# tooling. |

If a pinned version doesn't resolve in your editor patch, let Package Manager pick the compatible
one — versions are a starting point, not a contract.

---

## 5. High-level folder structure

```
weeniegame/
  prototype/                 # (UNCHANGED) the spec
  src/  tests/  docs/         # (UNCHANGED) TS reference + balance oracle + design docs
  src-tauri/                 # (UNCHANGED) webview wrapper for the TS build
  unity/
    CheddarAndCocoa/         # the Unity project (open THIS in Unity Hub)
      Assets/
        Scripts/             # C# (asmdef: CheddarAndCocoa)
          Dogs/ Input/ Camera/ Interactions/ Objectives/ Minigames/ Hazards/ Data/
        Scenes/ Prefabs/ Art/ Animation/ Audio/ Data/
      Packages/manifest.json
      ProjectSettings/ProjectVersion.txt
      README.md              # how to open + layout
```

Rationale: the Unity project is **fully self-contained** under `unity/CheddarAndCocoa/` so it can
be opened directly in Unity Hub, while the repo root keeps shipping the TS build until the slice
lands. Scripts are split by system (matching the TS `systems/` seams) and gated behind one
assembly definition so they stay modular and testable.

---

## 6. Gameplay systems (TS → Unity mapping)

Every system below already exists, tuned and tested, in `src/`. The Unity column is the rebuild
home; the stub files are committed and annotated with `PROTOTYPE MAP:` comments.

| System | TS source | Unity home (stub) | Notes |
|---|---|---|---|
| Dog movement + states | `systems/movement.ts`, `state/dog.ts` | `Dogs/DogController.cs` (`MovementMode`) | Use Rigidbody2D + units/sec; copy speed **ratios**, not the `*60` frame hack. |
| Dog identity / asymmetry | `config/dogs.ts`, per-system branches | `Dogs/DogIdentity.cs` + `Data/DogTuning.cs` | Cheddar/Cocoa differences collapse into a ScriptableObject. |
| Input (controllers) | `core/gamepad.ts`, `core/input.ts` | `Input/GamepadPlayerInput.cs` | Unity Input System; deadzone 0.25 from `balance.ts`. |
| Camera | `core/camera.ts` | `Camera/SharedCameraController.cs` | Dynamic "frame both dogs"; Cinemachine TargetGroup recommended. |
| Toys / pickup / rope | `systems/toys.ts` | `Interactions/ToyInteractable.cs` | Rope toys (~30%) hand off to tug. |
| "Needs both dogs" gates | `systems/gates.ts` | `Interactions/CoopInteraction.cs` | Pressure pads / boost-jump / distract+grab / both-on-spots as subclasses. |
| Objectives / missions | `systems/mission.ts` | `Objectives/LevelObjective.cs` | Single mutation point; reach/collect/survive/escort; stars + retry. |
| Tug-of-war | `systems/tug.ts` | `Minigames/TugOfWarMinigame.cs` | winAt 0.98, stalemate 14s, mash rates from `balance.ts`. |
| Predators / hazards | `systems/predators.ts` | `Hazards/Hazard.cs` | Coyote/eagle/hawk + chair-swat as subclasses; united-front defense + rescue. |
| Scent / sniff (new) | — (implied) | `Interactions/ScentTrail.cs` | Net-new co-op verb; design + sim before shipping. |
| Wrestle | `systems/wrestle.ts` | *(not yet stubbed)* | Reversal odds Cocoa .78 / Cheddar .70; add `WrestleSystem.cs` in slice. |
| Zoomies / jump | `systems/movement.ts`, `systems/jump.ts` | `DogController` overlays | Zoomies = 3 scores / 8s → ×1.85; jump arc 0.5s, dodge at height>0.3. |
| Sunbeam / couch / squish | `systems/sunbeam.ts`, `couch.ts`, `house.ts` | *(post-slice)* | "Hold a timer" spots — reuse one HoldSpot component. |
| Pool wet/swim/shake | `scenes/pool.ts`, `systems/movement.ts` | *(post-slice)* | MovementMode Swimming/Shaking + wet timer overlay. |
| Ambient events | `systems/events.ts` | *(slice: squirrel)* | Squirrel +3, treat +2, belly-rub immunity 3s. |
| Audio | `core/audio.ts` | Unity AudioMixer | Keep the synth character; real clips optional. |
| Particles / juice | `systems/particles.ts`, screen shake | URP + ParticleSystem | Vignette + shake already exist in TS — reproduce. |

---

## 7. Vertical slice scope (the first thing to build)

**One level — the Backyard — that proves the whole co-op loop on a TV with two controllers.**
Pick the smallest scope that is *actually fun* and exercises every core pillar.

In scope:
- **Backyard scene** (lawn, fence, patio, the magnolia squirrel hideout — model on `scenes/yard.ts`).
- **Both dogs playable** with two controllers (Cheddar = P1, Cocoa = P2), via `GamepadPlayerInput`.
  Solo fallback (AI partner) is **out** of the slice — couch co-op first.
- **Shared camera** framing both dogs (`SharedCameraController` or Cinemachine TargetGroup).
- **Core verbs:** move, **bark**, **grab** (toy/treat), **tug** (one rope minigame).
- **Squirrel chase event** — telegraphed sprint to the fence, +3 (from `systems/events.ts`).
- **Predator moment** — one eagle OR coyote: telegraph → strike, defended by the **united-front
  huddle** (both dogs close + bark) or a **rescue** (sibling frees the grabbed dog). This is the
  co-op centerpiece and the comedy beat.
- **One competitive tug minigame** (`TugOfWarMinigame`) as the versus flavor inside the co-op level.
- **Score / result screen** — combined score + a 1–3★ rating + "play again."

Out of scope for the slice: pool, house/rooms, the full 5-mission campaign, all asymmetric kitchen
abilities, tvOS/iOS builds, AI partner, settings/persistence. Those come after the slice is fun.

**Slice done-when:** two people play the Backyard start-to-finish on a TV with two controllers; it
needs genuine teamwork (the predator beat); it reads as charming/funny; it ends on a result screen;
and the movement/tug/predator numbers match the TS oracle.

---

## 8. Migration phases

| Phase | Goal | Done-when |
|---|---|---|
| **P0 — Scaffold** *(this change)* | Unity project shell, package manifest, C# stubs, docs. No engine assumed. | `unity/CheddarAndCocoa/` opens in Unity 6; stubs compile; pivot plan + roadmap committed. |
| **P1 — Walking dog** | One dog moves with a controller in an empty Backyard; URP + camera up. | Drag-free analog movement at the right *feel* vs prototype; shared camera frames the dog. |
| **P2 — Two dogs, two pads** | Both dogs, two controllers, shared camera, bark + grab. | Two humans move both dogs on a TV; toys/treats score; camera frames the pair. |
| **P3 — Tug + squirrel** | The rope tug minigame + the squirrel chase event. | Tug resolves to win/stalemate matching `balance.ts`; squirrel sprint scores +3. |
| **P4 — Predator + co-op defense** | One predator with telegraph, united-front huddle, rescue. | Lone dog is threatened; huddling/rescue defends; comedy reads; penalty on carry-off. |
| **P5 — Slice polish + result** | HUD (objectives/combined score), result screen + stars, juice (shake/vignette/audio). | The full **vertical slice done-when** (§7) is met. **Playtest sign-off.** |
| **P6 — Content + parity** | Port remaining missions/levels (pool, house, the 5-mission campaign, asymmetric kitchen) one at a time, each cross-checked against the TS oracle. | Each level plays start→finish, needs teamwork, matches the oracle. |
| **P7 — tvOS** | Apple TV build + on-device input/perf pass. | Slice runs on Apple TV with paired controllers at target frame rate. |

Each phase is independently demoable. Don't start a phase with the previous phase's "feel" unsigned.

---

## 9. Risks & tradeoffs (no sugar-coating)

| Risk | Severity | Mitigation |
|---|---|---|
| **Two builds to keep mentally in sync.** The TS build is the oracle; if it drifts from Unity, "which is right?" gets murky. | High | Freeze the TS build as a **read-only spec** at pivot time (stop adding features there). The numbers live in `MECHANICS.md`; that's the contract. |
| **"Feel" doesn't port 1:1.** Canvas `pos += v*dt*60` vs Rigidbody2D units/sec; arrival easing; analog ramps. The game can feel subtly wrong even with the right constants. | High | Port **ratios**, then tune against the prototype side-by-side. Budget explicit feel passes (P1, P5). Keep the prototype runnable for A/B. |
| **Determinism is harder in Unity.** Physics + frame-variant update threaten the sim-test discipline that caught balance bugs. | Medium | Keep game logic in FixedUpdate; prefer kinematic/manual integration over full physics for dogs; port key sims as PlayMode tests. |
| **Scope creep — "rebuild everything."** Unity invites polishing forever before anything ships. | High | The **vertical slice** is the gate. One level, fun, on a TV — *then* breadth. |
| **Unity version / package churn.** Pinned versions may not resolve; URP/Input System setup has sharp edges. | Medium | Pin Unity 6 LTS; let Package Manager reconcile; README has the setup steps. |
| **Asymmetric abilities re-balance.** Cheddar chair-leap/barf etc. shift balance; the TS build only partly modeled them. | Medium | Treat each asymmetric ability as a tuning task with a test, same as wrestle odds were. |
| **Art pipeline is now real work.** Procedural dachshunds were free; sprites/animation are not. | Medium | Slice can use simple placeholder sprites; the *systems* are the proof, art follows. |
| **No engine experience compounding.** More moving parts than vanilla TS. | Low–Med | The stubs + this plan front-load the structure; lean on the asmdef + tests. |

---

## 10. Non-goals (explicitly out)

- **No deleting or freezing-by-removal of the web build.** It stays; it's the spec.
- **No public store / distribution.** Owner machines + TV only.
- **No console targets now.** PS5/Switch are a separate, later, opt-in track.
- **No networked / online multiplayer.** Local couch co-op only (same screen, shared camera).
- **No splitscreen** in the slice — the shared "frame both dogs" camera is the design.
- **No AI partner in the slice.** Two humans first; AI fallback is post-slice.
- **No premature 3D.** 2D / 2.5D orthographic; the charm is 2D.
- **No re-deriving balance numbers.** They're ported from `balance.ts` / `MECHANICS.md`, full stop.

---

## 11. Next commands / how to continue in Unity

1. **Install** Unity Hub + Unity 6 LTS (`6000.0.x`) with Mac (and optionally Windows) build support.
2. **Open** the project: Unity Hub → *Add project from disk* → `unity/CheddarAndCocoa`.
   Let it generate `Library/`, `.meta`, default `ProjectSettings`, and resolve packages.
   Enable the new **Input System** backend when prompted.
3. **Configure URP** (2D Renderer): create a URP Asset, assign in *Project Settings → Graphics/Quality*.
4. **First scene (P1):** create `Assets/Scenes/Backyard.unity`. Add a `DogController` + `DogIdentity`
   + `GamepadPlayerInput` on a sprite; create `DogTuning_Cheddar` / `DogTuning_Cocoa` assets in
   `Assets/Data/` and fill from `src/config/balance.ts`. Add the `SharedCameraController` to the
   Main Camera. Verify a controller moves the dog and the camera follows.
5. **Cross-check** every tuned value against `docs/MECHANICS.md` / `src/config/balance.ts` — never
   re-invent a number.
6. Follow the **phases in §8**; gate breadth behind the **vertical slice (§7)**.

> Keep `npm run verify` green in the TS build as long as it's the oracle — a red oracle can't
> arbitrate Unity behavior.
