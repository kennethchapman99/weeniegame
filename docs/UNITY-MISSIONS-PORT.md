# Unity Missions — Port Status & Wiring

> **Status: HISTORICAL PORT NOTES.** The described missions and direct wiring recipes are not an
> active queue. Do not create new `GameManager` variants or resume this backlog. Follow
> `NEXT-PRODUCTION-SLICE.md` and the controller boundary in `ARCHITECTURE.md`.

This is the working doc for porting co-op missions into the Unity project (`unity/CheddarAndCocoa/`).
The TypeScript/Canvas build is **frozen reference only** — read it to learn how a mechanic behaves,
then build it here. See the freeze banners in `README.md`, `CLAUDE.md`, `AGENTS.md`, `BUILD-PLAN.md`.

> ⚠️ **Verification owed:** the C# below was authored without a Unity compile/PlayMode pass (this
> environment can't run the licensed editor — batch-mode hits the license gate, see
> `docs/UNITY-FIRST-PLAYABLE.md`). The files are **additive** (no edits to `GameManager`/
> `ArenaBootstrap`), so the verified first-playable slice and its PlayMode tests cannot regress, and
> any fix stays local to these files. **Open the project once to confirm it compiles (no Safe Mode)
> before trusting them.**

## Reusable primitive added: shared panic / co-regulation

`Assets/Scripts/Game/PanicMeter.cs` — per-pup panic (0..1). Cuddling within `cuddleR` drains BOTH
pups; being apart raises each; `AddSpike` pushes panic up; `Maxed` is the fail trigger. This is the
building block for **Thunderstorm, Vet, Nail-Grinder, Big-Dog** scenes. Pure-ish (`Step` depends only
on its inputs) so it's easy to cover with an EditMode test.

## Mission A — "The Thunderstorm" (SURVIVE / comfort)

New files:
- `Assets/Scripts/Game/PanicMeter.cs`
- `Assets/Scripts/Hazards/ThunderstormHazard.cs` (`: Hazard`) — telegraph flash → boom → recover;
  the clap spikes each pup's panic scaled by distance to the strike, blunted while sheltered.

Historical wiring sketch (preserved for behavior context; do not use it to add a `GameManager`
mission branch):
1. Spawn the two dogs + shared camera (reuse `ArenaBootstrap` helpers).
2. Add a `PanicMeter`; call `ResetMeter()` on entry; each `FixedUpdate`/`Update` call
   `panic.Step(cheddar.position, cocoa.position, dt)`.
3. Add a `ThunderstormHazard`; `Configure(panic, cheddarTf, cocoaTf)`; `AddShelter(tf, radius)` for
   each shelter (table / deck chair / blanket nest).
4. Add a `Survive` `LevelObjective` (e.g. 45s). The mission manager ticks the survive timer →
   `Complete()`; if `panic.Maxed != null` → `Fail()`.
5. HUD: two panic bars (read `panic.CheddarPanic`/`CocoaPanic`), a screen flash from
   `hazard.Flash`, and a bolt at `hazard.StrikeX`.

PlayMode test to add (`Assets/Tests/PlayMode/ThunderstormPlayModeTests.cs`):
- cuddling drains panic, apart raises it (drive `PanicMeter.Step`);
- a maxed meter fails the objective;
- a sheltered pup eats a smaller spike than an exposed one at the same x;
- surviving the full timer succeeds.

## Mission B — "The Cleaning Ladies Are Here" (distract + carry / ESCORT)

New files:
- `Assets/Scripts/Hazards/VacuumHazard.cs` (`: Hazard`) — patrols; fixates/creeps toward a non-carrier
  pup within `distractR`; catches an un-distracted carrier within `catchR` → drops the toy + stuns.
- `Assets/Scripts/Interactions/CarriedItem.cs` — a free pup within `grabR` picks up the toy; it rides
  the carrier; reaching the safe zone within `deliverR` completes the Escort objective; `PutAway`
  knocks it loose.

Historical wiring sketch (preserved for behavior context; deferred):
1. Spawn dogs + camera. Place a safe-zone transform (the dog couch) and the toy (`CarriedItem`).
2. `carriedItem.Configure(dogs, safeZoneTf, escortObjective)` where `dogs` is the two `DogIdentity`.
3. `vacuum.Configure(dogs, carriedItem)`.
4. Add an `Escort` `LevelObjective`; the mission fails on timeout.
5. HUD: objective "Carry the toy to the dog couch"; vacuum shows an alarmed state when
   `vacuum.Distracted`.

PlayMode test to add (`Assets/Tests/PlayMode/CleaningPlayModeTests.cs`):
- a free pup on the toy picks it up;
- carrying past an un-distracted vacuum drops it + stuns the carrier (`Carrier` clears, mode Stunned);
- with a teammate luring the vacuum (`Distracted == true`) the carrier delivers and the Escort
  objective completes.

## Tuning provenance

The serialized defaults in these components are ported from the (frozen) TS mission designs, converted
from px to world units (~0.0156 u/px on the 20×12u arena / 1280px field). Re-tune against feel in a
two-player playtest; keep numbers in the components (or promote to a ScriptableObject) — do **not**
re-add them to the frozen `src/config/balance.ts`.

## Backlog (same pattern)

`HawkHazard` ("Stay Together"), `CoyoteHazard`, `EagleHazard`, `ChairSwat` (Hazard subclasses);
`PressurePadGate` / `BoostJumpGate` / `DistractGrabGate` (CoopInteraction subclasses); the Vet and
Nail-Grinder scenes (reuse `PanicMeter`). The frozen TS `src/scenes/missions/*` + `systems/*` are the
behavior reference for each.
