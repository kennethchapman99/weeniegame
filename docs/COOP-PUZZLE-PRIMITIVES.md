# Co-op Puzzle Primitives — Implementation Reference

Companion to `COOP-PUZZLE-DESIGN.md` (the doctrine). That doc says *what* a co-op beat must feel like; this one is the *reusable, tested code* to build them, and how to drop each into a mission.

All primitives are **pure logic** (no `MonoBehaviour`), in `CheddarAndCocoa.Game`, so a mission drives them from real dog positions/inputs while PlayMode tests drive them deterministically. Each has scene-free unit tests under `Assets/Tests/PlayMode/`. Roles are soft — either dog can take either side; defaults below are for comedy/clarity, not hard locks.

> Status: primitives + one position driver landed on branch `claude/post-art-followups`, full PlayMode suite green. Wiring into specific missions is a small follow-up (each is a few lines in the mission's `Begin/Tick/Force*` and a `MissionDefinition`).

## The toolkit

| Primitive | Doctrine family | Co-op lock (the "you must" tension) | Funny/recoverable fail |
|---|---|---|---|
| `CoopHoldReleasePuzzle` | #1 Hold-and-release + #7 timed | Crosser only advances **while** the anchor holds; the hold has a draining patience window so the crosser must finish in time | Anchor lets go mid-cross → **snap** back to start |
| `CoopDistractSneakPuzzle` | #2 Distract-and-sneak | Squeeze: under-distract → enemy **Watchfulness** returns; over-distract → **Annoyance** turns the enemy anyway | **Spotted** → back only to the last banked **checkpoint** |
| `CoopSequenceChainPuzzle` | #4 cause/effect + #5 role reversal | Each step is **role-gated and ordered**; alternating owners force the dogs to take turns | Wrong dog/order = harmless **Fumble**; dawdling = chain **Settles** back a step |
| `CoopRescueTimingPuzzle` | #8 rescue + #7 dual-timing | Held dog alone opens a **weakness window** (wiggle); free dog alone can **pull**, only inside it | Mistimed pull = **MissedPull** (no permanent punish) |

Plus `CoopHoldReleaseBeat` (a `MonoBehaviour`) that drives `CoopHoldReleasePuzzle` from two dog transforms and a hold zone / cross corridor — the pattern for turning any primitive into an in-scene beat.

## API at a glance

- **HoldRelease**: `Configure(crossNeeded, holdWindow)`; `SetHeld(bool)`; `Advance(dt)`; read `Solved/Snaps/CrossRatio/HoldRatio`. Keep `holdWindow >= crossNeeded` for a solvable beat (a snap resets crossing).
- **DistractSneak**: `Configure(segments, segmentTime)`; `Advance(dt, distracting, sneaking)`; read `Segment/Solved/Spotted/Watchfulness/Annoyance`. Checkpoints are safe; only an **exposed** sneaker is spotted.
- **SequenceChain**: `Configure(ChainActor[] owners, settleTime)`; `TryStep(ChainActor)`; `Advance(dt)`; read `Step/Solved/Fumbles/Settles/NextOwner`. `settleTime <= 0` disables regression.
- **RescueTiming**: `Configure(pullsNeeded, windowDuration)`; `Wiggle()` (held dog); `Pull()` (free dog); `Advance(dt)`; read `Freed/Pulls/MissedPulls/WindowOpen`.

## Recommended mission wiring (from the doctrine's example upgrades)

- **Backyard Rescue → squirrel trap** (`CoopHoldReleasePuzzle`): Cocoa holds the garden gate/cover gap (anchor) while Cheddar squeezes through to steal back the dropped weenie (crosser). Snap = gate flaps shut, weenie skitters away.
- **Snack Heist → table stealth** (`CoopDistractSneakPuzzle`): Cheddar distracts the table-watcher; Cocoa sneaks the snack along a safe lane in segments. Over-bark and the human turns; go quiet and they look back.
- **Coyotes at the Fence → repair contraption** (`CoopSequenceChainPuzzle`): owners `[Cocoa bark-pin, Cheddar dig/fill, Cocoa tug board across]`; dawdle and the dirt settles back.
- **Eagle Shadow Panic → real rescue** (`CoopRescueTimingPuzzle`): the snatched dog wiggles to crack the talon grip; the free dog pulls in the window. Replaces proximity-only bark rescue.

## Integration checklist (per mission)

1. Hold a primitive instance on `GameManager` (like the existing mission states).
2. In `BeginRound`'s mission block: `Configure(...)`, place the readable zone/marker actors, reset.
3. In `Update`'s mission tick: feed it from dog positions/distances (see `CoopHoldReleaseBeat` for the pattern) and call the relevant `Advance/Try*`.
4. Add `Force*` hooks mirroring the primitive's verbs for deterministic PlayMode tests (clear / fail / replay-reset).
5. Map the verbs to score events + readable objective copy; surface the funny fail as a world pop, not a silent subtraction.
