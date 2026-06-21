# Co-op Puzzle Primitives — Implementation Reference

> **Status: OPERATIONAL REFERENCE.** The primitives remain reusable. The old integration pattern of
> storing primitive fields and mission branches in `GameManager` is historical; all new use belongs
> inside an `IMissionController` through a narrow `MissionContext`.

Companion to `COOP-PUZZLE-DESIGN.md` (the doctrine). That doc says *what* a co-op beat must feel like; this one is the *reusable, tested code* to build them, and how to drop each into a mission.

All primitives are **pure logic** (no `MonoBehaviour`), in `CheddarAndCocoa.Game`, so a mission drives them from real dog positions/inputs while PlayMode tests drive them deterministically. Each has scene-free unit tests under `Assets/Tests/PlayMode/`. Roles are soft — either dog can take either side; defaults below are for comedy/clarity, not hard locks.

> Historical landing status: all primitives and a position/input driver each landed and were wired
> into real missions. They originally proved an integration pattern inside `GameManager`; do not
> repeat that ownership. Preserve the behavior while moving mission use behind controllers.
> - **Gate Crash** (Hold-and-Release): Cocoa anchors the gate, Cheddar squeezes through; let go mid-squeeze and it snaps.
> - **Table Stealth** (Human-Distraction): Cocoa flops belly-up to hold the human's gaze while Cheddar sneaks the dropped steak; sneak while the human is watching and you get spotted.
> - **The Ol' Switcheroo** (Bait-and-Switch): Cheddar feints at a decoy nut pile to commit the squirrel off its buried stash; Cocoa raids the stash only while the squirrel is committed. Hold the feint too long and the squirrel wises up (backfire).
> - **The Walk Campaign** (Social-Manipulation): Cocoa fixes the human with a dignified door-stare while Cheddar presents the leash — neither stimulus reads alone, so both dogs must hold their stations at once until the human gets it. Cover only one (or wander off) and the human gets confused and brings the wrong thing (misread); too many misreads and the walk is off.
> - **The Bone Detail** (Scent-Relay split-information): four look-alike dirt mounds, only one hiding the real bone. Cocoa noses the scent post to call which mound is real; Cheddar is the only one who can dig but can't tell them apart, so he must wait for her call. Digging blind or digging a decoy wastes a dig; waste too many and the team gives up. Each find re-buries the next bone elsewhere.
> - **The Great Escape** (Sequence-Chain): an ordered contraption chain with alternating owners — Cocoa paws the latch, Cheddar shoulders the gate, Cocoa drags the cooler, Cheddar squeezes through. The active station glows for its owner; wrong dog / wrong order is a harmless fumble, and dawdling eases the chain back a step. Botch it too many times and the breakout fails.
> - **The Rube Goldberg** (Chaos-Machine): pre-position at junctions, pull the lever, and the cascade runs ITSELF (towel drop → basket tip → toy launch) — but each junction has a brief window where its owner dog must be in place or the machine misfires and visibly jams there. A re-pull resumes from the jam; too many misfires fail.
> - **The Blanket Catch** (Stretch-Span): both dogs grip a blanket stretched between them to catch food falling from the counter — too close and it sags (slack), too far and it RIPS, only the taut separation band works, and the midpoint must be under the snack. Both dogs coordinate spacing AND position; over-stretch too often and the blanket tears apart.
> - **Eagle Shadow Panic** (Rescue-Timing, *upgrade of an existing mission*): after the hides, the eagle SNATCHES Cheddar into its talons. Cheddar wiggles (Tug/Rescue) to crack the grip open for a brief window; Cocoa pulls in that window to yank him free — pulling with no window open is a mistimed miss. Enough well-timed pulls free him, then the united-front bark. Replaces the old proximity-only toy rescue.

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
| `CoopBaitSwitchPuzzle` | #4 bait-and-switch + #7 readable deception | **Commitment + overbait hold**: the baiter feints an enemy onto a decoy; the striker's snatch lands anywhere in the **Committed** band (threshold→full), so the baiter commits then feathers rather than pinning, and the striker reads the window | Under-bait = **Whiff** (enemy still guarding); *holding* the pin at full past the overbait tolerance → enemy wises up / Cheddar takes his own bait = **Backfire** (window snaps shut) |

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
- `CoopBaitSwitchBeat` — proximity feint (baiter inside `BaitRange` of the decoy spot raises the enemy's commitment; back off and it decays); mission calls `StrikeTarget` when the striker reaches the real prize.

This now covers the doctrine's full puzzle-family taxonomy (#1–#10), including the previously-missing #4 bait-and-switch / readable-deception beat.

These give missions a template for every interaction style (continuous, discrete-interact, input-event).

Artwork the beats need (poses, VFX, human states, markers) is catalogued in `COOP-PUZZLE-ART-NEEDS.md` so a later art pass can fill them against the already-exposed primitive state.

## API at a glance

- **HoldRelease**: `Configure(crossNeeded, holdWindow)`; `SetHeld(bool)`; `Advance(dt)`; read `Solved/Snaps/CrossRatio/HoldRatio`. Keep `holdWindow >= crossNeeded` for a solvable beat (a snap resets crossing).
- **DistractSneak**: `Configure(segments, segmentTime)`; `Advance(dt, distracting, sneaking)`; read `Segment/Solved/Spotted/Watchfulness/Annoyance`. Checkpoints are safe; only an **exposed** sneaker is spotted.
- **SequenceChain**: `Configure(ChainActor[] owners, settleTime)`; `TryStep(ChainActor)`; `Advance(dt)`; read `Step/Solved/Fumbles/Settles/NextOwner`. `settleTime <= 0` disables regression.
- **RescueTiming**: `Configure(pullsNeeded, windowDuration)`; `Wiggle()` (held dog); `Pull()` (free dog); `Advance(dt)`; read `Freed/Pulls/MissedPulls/WindowOpen`.
- **BaitSwitch**: `Configure(commitThreshold, commitRate, decayRate, overbaitTolerance, hitsNeeded, maxBackfires)`; `Advance(dt, baiting)` (baiter feints); `Strike()` (striker snatches); read `Committed/Overbaited/Hits/Whiffs/Backfires/Solved/TooManyBackfires`. The strike window is the whole `Committed` band; only *holding* commitment pinned at full past `overbaitTolerance` backfires, so the baiter commits then eases off.

## Historical/example controller mappings

These mappings document prior or possible uses; they are not an active mission queue. When an
authorized controller uses one, keep the primitive and its state inside that controller.

- **Backyard Rescue → squirrel trap** (`CoopHoldReleasePuzzle`): Cocoa holds the garden gate/cover gap (anchor) while Cheddar squeezes through to steal back the dropped weenie (crosser). Snap = gate flaps shut, weenie skitters away.
- **Snack Heist → table stealth** (`CoopDistractSneakPuzzle`): Cheddar distracts the table-watcher; Cocoa sneaks the snack along a safe lane in segments. Over-bark and the human turns; go quiet and they look back.
- **Coyotes at the Fence → repair contraption** (`CoopSequenceChainPuzzle`): owners `[Cocoa bark-pin, Cheddar dig/fill, Cocoa tug board across]`; dawdle and the dirt settles back.
- **Eagle Shadow Panic → real rescue** (`CoopRescueTimingPuzzle`): the snatched dog wiggles to crack the talon grip; the free dog pulls in the window. Replaces proximity-only bark rescue.
- **Squirrel Conspiracy → the ol' switcheroo** (`CoopBaitSwitchPuzzle`): Cheddar feints a chase to lure the squirrel onto a decoy nut pile while Cocoa raids the real buried stash — but only while the squirrel is committed. Over-feint and the squirrel bolts (or Cheddar chases his own decoy) and the window snaps shut.

## Integration checklist (per controller)

1. Hold the primitive instance inside the mission's `IMissionController`.
2. During controller setup, call `Configure(...)`, place readable actors through `MissionContext`,
   and reset all owned state.
3. During controller tick/input handling, feed real dog positions and inputs to `Advance/Try*`.
4. Expose controller-level deterministic hooks mirroring the primitive's verbs for clear, failure,
   and replay-reset tests.
5. Emit score events and readable feedback through narrow context services; surface funny failure
   as visible recovery, not silent subtraction.
6. Produce primitive-derived state in the controller snapshot and remove owned actors on cleanup.

Do not add a primitive field, setup block, `Tick*` branch, test hook, or definition branch directly
to `GameManager`.
