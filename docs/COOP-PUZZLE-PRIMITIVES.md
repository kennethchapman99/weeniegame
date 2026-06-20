# Co-op Puzzle Primitives — Implementation Reference

Companion to `COOP-PUZZLE-DESIGN.md` (the doctrine). That doc says *what* a co-op beat must feel like; this one is the *reusable, tested code* to build them, and how to drop each into a mission.

All primitives are **pure logic** (no `MonoBehaviour`), in `CheddarAndCocoa.Game`, so a mission drives them from real dog positions/inputs while PlayMode tests drive them deterministically. Each has scene-free unit tests under `Assets/Tests/PlayMode/`. Roles are soft — either dog can take either side; defaults below are for comedy/clarity, not hard locks.

> Status: all primitives **and** a position/input driver each landed on branch `claude/post-art-followups`, full PlayMode suite green. The Hold-and-Release beat is now also **wired into a real mission — Gate Crash** (Cocoa anchors the gate, Cheddar squeezes through; let go mid-squeeze and it snaps), proving the end-to-end integration pattern: a primitive field on `GameManager`, a `BeginRound` setup block, a `Tick*` driving it from dog positions, `Force*` test hooks, per-dog guidance, and a `MissionDefinition`. Wiring the rest is the same shape.

## The toolkit

| Primitive | Doctrine family | Co-op lock (the "you must" tension) | Funny/recoverable fail |
|---|---|---|---|
| `CoopHoldReleasePuzzle` | #1 Hold-and-release + #7 timed | Crosser only advances **while** the anchor holds; the hold has a draining patience window so the crosser must finish in time | Anchor lets go mid-cross → **snap** back to start |
| `CoopDistractSneakPuzzle` | #2 Distract-and-sneak | Squeeze: under-distract → enemy **Watchfulness** returns; over-distract → **Annoyance** turns the enemy anyway | **Spotted** → back only to the last banked **checkpoint** |
| `CoopSequenceChainPuzzle` | #4 cause/effect + #5 role reversal | Each step is **role-gated and ordered**; alternating owners force the dogs to take turns | Wrong dog/order = harmless **Fumble**; dawdling = chain **Settles** back a step |
| `CoopRescueTimingPuzzle` | #8 rescue + #7 dual-timing | Held dog alone opens a **weakness window** (wiggle); free dog alone can **pull**, only inside it | Mistimed pull = **MissedPull** (no permanent punish) |
| `CoopHumanDistractionPuzzle` | #2 + soft asymmetry | Same human distraction, two signature moves: Cheddar's **burp** (burst + cooldown → timed sneak windows) or Cocoa's **belly-rub flop** (sustain, but committed + stamina-limited); partner sneaks only while `HumanDistracted` | Eager re-burp = **WastedBurp**; flop runs out → Cocoa gets up; exposed sneak = **Exposure** (stall, no punish) |
| `CoopScentRelayPuzzle` | #3 smell-and-act + #6 split info | **Information** asymmetry: only the reader can **Reveal** which look-alike target is real; only the digger can **ActOn** one. Digger can't act blind, so it must wait for and dig the reader's call | Acting before a reveal = **BlindAct**; wrong dig = harmless **decoy** (`WrongDigs`), reader still knows |
| `CoopStretchSpanPuzzle` | #5 long-dog geometry / bridge | **Two-body spacing band**: both dogs stretch a span (blanket / long-dog bridge); usable only when their **separation** is in band (too close `Slack`, too far `Overstretched`) and the **midpoint** is under the target | Over-stretching **Rips** (once per event); slack/off-centre/ripped = **Missed** catch |
| `CoopChaosMachinePuzzle` | #10 cooperative chaos machine | Pre-position, pull the lever, and the **cascade runs itself** through junctions; it only rolls on while each junction's helper dog is in its **assist window** | A missing helper **misfires** and the machine **stalls visibly** at that stage (`StalledStage`); re-trigger resumes from there |
| `CoopSocialManipulationPuzzle` | #9 social manipulation | A human reads an **exact combination** of stimuli (door-stare, present-leash, …) drawn from **both** dogs — neither can send the message alone | Wrong/incomplete combo builds `Confusion` (faster with an off-message stimulus); maxed → the human **Misreads** (brings the wrong thing) and resets |

Each primitive also ships a `MonoBehaviour` **driver** that turns it into an in-scene beat:

- `CoopHoldReleaseBeat` — continuous proximity (anchor in hold zone, crosser in cross corridor).
- `CoopDistractSneakBeat` — continuous proximity (distractor in enemy zone, sneaker in lane).
- `CoopSequenceChainBeat` — discrete interaction (a dog interacts at the next step's station).
- `CoopRescueTimingBeat` — input events + proximity gate (held dog wiggles; free dog pulls only when adjacent).
- `CoopHumanDistractionBeat` — input + proximity (Cheddar burps / Cocoa flops; whichever dog isn't distracting sneaks the lane).
- `CoopScentRelayBeat` — proximity (reader reveals at the scent source; digger digs the station it stands on).
- `CoopStretchSpanBeat` — two-transform geometry (separation + midpoint from both dogs; mission calls `CatchItem` at catch height).
- `CoopChaosMachineBeat` — junction positions + per-stage owner; `Trigger` pulls the lever, the cascade rolls while each junction's helper is in range.
- `CoopSocialManipulationBeat` — stimulus stations + owner; a stimulus is active while its owner dog stands at its station, so the required combo needs both dogs in place.

This now covers the doctrine's full puzzle-family taxonomy (#1–#10).

These give missions a template for every interaction style (continuous, discrete-interact, input-event).

Artwork the beats need (poses, VFX, human states, markers) is catalogued in `COOP-PUZZLE-ART-NEEDS.md` so a later art pass can fill them against the already-exposed primitive state.

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
