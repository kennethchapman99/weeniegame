# Deep Slice — Operation Pee Break: The Teenager Phone Rescue

> The first **deep** vertical slice: one 5–8 minute, 4-beat, role-flipping, fully-juiced mission,
> built to the depth bar in `docs/DESIGN-REVIEW-2026-06.md`. This replaces "add another 90-second
> round" as the active priority. It deliberately reuses the already-shipped, already-tested
> `CoopSocialManipulationPuzzle` primitive — this is **depth, not new plumbing.**
>
> Why this one: it's the highest-soul idea in `GAME-DESIGN-BIBLE.md` (§6 names it as *the* thesis
> for "personal jokes become mechanics"), and "a human is a puzzle system" is whimsy that can only
> exist in *your* game.

## The fantasy

Cheddar and Cocoa **need to pee.** The only human home is the Teenager, fused to the couch and the
phone, headphones in, thumbs flying. The dogs cannot open the door. Their entire toolkit is *being
dogs at a human until the human gives up and takes them out.* The Bladder Emergency meter is rising.
This is a comedy of escalating, increasingly desperate, increasingly clever dog manipulation.

The joke that carries the whole slice: **the Teenager is not an obstacle, the Teenager is the
puzzle.** Every "enemy attack" is actually the Teenager *almost* getting it and then getting
distracted again.

## Tone & soul targets (the non-negotiables)

- Every failure is a **gag with an instant retry**, never a silent score hit. (Teenager says "not
  now," puts AirPod back in, meter ticks — you laugh, you re-plan.)
- The Teenager **emotes constantly**: confused "?", a hopeful "👀", an annoyed sigh, a triumphant
  "oh you need to GO go." Readable chaos via emote, not text.
- Cheddar and Cocoa are **mechanically different** the whole way through (see roles per beat).
- The climax is a **united-bark payoff** with full juice — camera push, rumble, the door opens, sun
  floods in. The reward is *catharsis*, not points.

## Mechanic spine (reused primitive)

Drive `CoopSocialManipulationPuzzle` (`Assets/Scripts/Game/CoopSocialManipulationPuzzle.cs`) from
dog positions/inputs. Stimuli already modeled in the `SocialStimulus` flags enum:

`DoorStare · PresentLeash · BarkRhythm · NudgeShoe · BlockHallway · UnplugCharger`

The lock is already exactly right for soul: the human only "gets it" on the **exact** required combo
(`ExactMatch`), an off-message stimulus builds `Confusion` 1.5× faster, and maxing confusion makes
the human **misread** (brings the wrong thing — leash → tennis ball → blanket) instead of punishing.
We escalate by **changing the required combo per beat** and **splitting which dog can apply which
stimulus.**

## The four beats (Teach → Explore → Twist → Climax)

### Beat 1 — TEACH: "Make eye contact" (single stimulus, ~30s, no fail)
- Bladder meter starts low. Required combo: `DoorStare` only.
- Cocoa (the veteran, the *stare* specialist — see bible: "royal glare") walks to the door spot and
  holds the stare; an arrow + the Teenager's "?" emote teaches the loop with zero text.
- Comprehension fills, Teenager glances up — and an **AirPod notification** yanks attention back.
  Beat clears on first comprehension tick. *Player has learned: position = stimulus, fill the bar.*

### Beat 2 — EXPLORE: "Two-signal combo" (split roles, light pressure)
- Required: `DoorStare + PresentLeash`. Cocoa holds the stare; **Cheddar** must drag the leash off
  its hook (a `CarriedItem`) to the door spot. Neither dog can do both — the lock is split.
- Bladder meter now rises on a timer; `Confusion` ticks if only one signal is up.
- **Funny failure:** if Cheddar drops the leash en route (Cheddar drops things — chaos puppy),
  Confusion climbs; at max the Teenager "misreads" and lobs a **tennis ball** ("you wanna play?").
  The dogs must reset the combo. Gag, not death.

### Beat 3 — TWIST / ROLE-FLIP: "The charger gambit"
- The Teenager's phone is the boss. New required combo: `UnplugCharger + BlockHallway`.
- **Role reversal is the whole point:** now **Cheddar** is the patient one — he must *hold*
  `BlockHallway` (stand in the one spot, like a Hold-and-Release anchor) while **Cocoa** does the
  precise, nervy `UnplugCharger` nudge under a low-battery timer. The veteran does the delicate job;
  the puppy does the dumb-but-vital blocking job. Their bible identities **invert** for comedy.
- Pressure: phone battery drains on a visible bar. If it dies the Teenager gets *up to find a
  charger* — which is progress-ish chaos (they wander, dogs must re-herd). Off-message stimulus here
  (e.g. an excited `BarkRhythm` too early) spikes Confusion 1.5×, risking a misread (blanket: "are
  you cold?").

### Beat 4 — CLIMAX: "United bark, the door" (set-piece payoff)
- Bladder meter is near max — visible, urgent, funny (crossed-legs dog dance animation).
- Required: the **full** desperate combo — `DoorStare + PresentLeash + BarkRhythm`, with the
  `BarkRhythm` requiring **both dogs barking in the timing window near the door** (united bark).
- When `ExactMatch` holds through comprehension: **the Teenager finally GETS IT** — big "OH." emote,
  phone drops, stands up, the door opens, sun floods the frame. Camera push-in, rumble, music sting.
  Cheddar and Cocoa bolt out. Relief. Roll the star rating on *how few misreads / how fast*, but the
  real reward is the catharsis beat.

## Co-op Puzzle Beat (authoring-standard block)

- **Name:** The Charger Gambit (Beat 3, the signature beat).
- **Roles:** Anchor = Cheddar (`BlockHallway` hold). Actor = Cocoa (`UnplugCharger` precision).
- **Lock/key:** Comprehension only accrues on `ExactMatch`; the charger can only be reached while the
  hallway is blocked (human's pathing is pinned), so Cocoa's action *requires* Cheddar's hold.
- **Hint:** hallway spot glows for the holder; charger cord highlights for the actor; Teenager emote
  telegraphs imminent misread.
- **Funny failure:** premature bark or a dropped block → Confusion max → Teenager misreads (tennis
  ball / blanket / "you hungry?" → walks to kitchen). Recoverable; resets the combo, not the mission.
- **World-state change:** unplugging the charger visibly kills the phone glow; the Teenager's face
  lifts from the screen — the world reacts.
- **Role flip:** Beats 2→3 swap who's patient vs. who's precise, so it never settles into routine.

## Sequence gate

Do not start this implementation until the baseline two-player couch playtest, its critical fixes,
the `IMissionController`/`MissionContext` boundary, and the behavior-preserving **Kitchen-first
extraction** are complete with the full PlayMode suite green. Pee Break is the first new deep slice
through the proven structure, not the first extraction.

## Implementation plan (born in the proven controller structure)

1. Register `PeeBreakMissionController` and its mission definition outside `GameManager`; do not add
   a `MissionVariant` behavior branch or mission-specific fields to `GameManager`.
2. The controller owns setup, input handling, cleanup, outcome, snapshots, bladder meter, and the
   four beat configs (each a
   `CoopSocialManipulationPuzzle.Configure(required, comprehendNeeded, confusionMax)` + role/stimulus
   wiring), Teenager emote state machine, checkpoint-per-beat reset.
3. Drive stimuli from real dog positions/inputs each `Tick` via `SetActiveSet(...)` (mirror the
   existing WalkCampaign position-driver beat — `CoopSocialManipulationBeat.cs`).
4. Juice: Teenager emote sprites, phone/battery/bladder HUD bars, door-open set-piece, united-bark
   camera+rumble. Placeholder-but-cohesive is fine; readability over realism.
5. Keep the full PlayMode suite green at each migration step, then run the second human couch
   playtest as the deep-slice acceptance gate. Keep the mission roster frozen until it passes.

## Test hooks (matches `MISSION-AUTHORING-FRAMEWORK.md` + existing suite style)

Add `PeeBreakPlayModeTests.cs` covering:

- **Start state:** Beat 1, bladder low, required == `DoorStare`, outcome InProgress.
- **Beat clear path:** `ForceAdvance` with the exact combo held → `Solved`, advances to next beat,
  required combo changes.
- **Split-role lock:** holding only one of a two-stimulus combo never solves; `Confusion` rises.
- **Funny-failure event:** off-message stimulus drives `Confusion` to max → `Misreads` increments,
  comprehension resets, mission does **not** fail.
- **Role-flip integrity (Beat 3):** charger combo requires both `UnplugCharger` and `BlockHallway`;
  dropping the block mid-advance halts comprehension.
- **Climax:** full combo incl. united-bark window → mission `Clear`; door-open world flag set.
- **Replay reset & session summary:** restart returns to Beat 1, meter/misreads/comprehension cleared.

## Done = the real bar

This slice is done when **two humans play it on the couch and laugh at the misreads, feel the Beat-3
tension, and cheer when the door opens** — not when the tests pass. Tests prove it works; Sue proves
it has soul. Run that playtest before declaring it shippable.
