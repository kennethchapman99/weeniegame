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

## Station map

Physical anchors the dogs stand at/near for each beat's stimuli
(`PeeBreakMissionController.BuildScene()`/`StartMission()`):

- **Door (Cocoa's `DoorStare`, Beats 1/2/4).** CF1.6 (2026-07-20, finding #6 — "It's weird that the
  door stare has to happen by climbing up the wall?",
  `captures/2026-07-20/pee-break-door-stare-wall-climb.png`): the stimulus, the united-bark
  "at the door" check, Cocoa's entry placement, her objective-arrow/breadcrumb target, and the
  door label's proximity gate all anchor to `DoorStareAnchor` — the existing doormat prop's
  floor-level world position (a child of the door art, offset toward its visual base) — instead
  of `_doorPosition`, the door ART's own center drawn tall up the back wall (scale `(x, 4, 1)`).
  Standing within `StationRange` of that center visually read as climbing the door; the doormat
  sits roughly 2.5 units below it, comfortably more than `StationRange` (2.25), so this is a real
  relocation, not a cosmetic tweak. The door ART itself — position, scale, open/closed swap — is
  unchanged. Cocoa also gets a grounded, facing-the-door idle pose (the existing guidance-nudge
  read, no new art) while she holds the stare.
- **Leash (Cheddar's `PresentLeash`, Beats 2/4).** Unchanged — hangs on its hook left of the door.
- **Hallway (Cheddar's `BlockHallway`, Beat 3 only).** Unchanged — mid-room choke point.
- **Charger (Cocoa's `UnplugCharger`, Beat 3 only).** Unchanged — couch-side outlet.

## First-session flow (opening explainer → control card → sniff beat → Beat 1)

Pee Break is the first mission with a controller-owned opening explainer (the 10-second MP4), so its
cold-start sequence has more steps than the rest of the roster:

1. **Opening explainer** (skippable, either player's Bark or Interact) — the mission clock and dogs
   stay locked the whole time (`IMissionOpeningPresentationController`).
2. **Control card** (CF1.1, 2026-07-20) — once the explainer ends or is skipped, the shared
   briefing/control card appears and the game is fully frozen underneath it. The card no longer
   expires on a timer; it waits for either player to press Bark or Interact, or grab the first
   collectible ("accept"). This is shared `GameManager`/`ArenaHud` behavior, not Pee-Break-specific,
   but Pee Break is the mission most affected since it is the only one with a preceding video the
   players have already sat through — the couch-test finding ("the control card dismisses itself
   after a period of time - I need time to look at it and accept it") happened on this mission.
3. **Sniff-around beat** — accepting the card drops it and starts the existing `LeadInSniffSeconds`
   (2.5s) open-yard freeze; the yard is visible and dogs can roam, but the bladder meter, Teenager
   state machine, and phone battery all stay frozen. A further Bark/Interact, or scooping the first
   collectible, ends this early exactly as before CF1.1.
4. **GO → Beat 1 (TEACH)** — the round clock and Beat 1's `DoorStare` requirement go live exactly as
   described below.

Manual check: start Operation Pee Break, let the explainer play out, and confirm the control card
then stays up with zero input for well past the old 5-second window — the bladder meter, Teenager,
and phone battery must not move. Bark or Interact — the card drops into the sniff countdown, still
frozen. Bark/Interact again — `GO!` fires and Beat 1 begins.

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
- **Funny failure (CF1.3, 2026-07-20):** if Cheddar drops the leash en route (Cheddar drops
  things — chaos puppy), Confusion climbs; at max the Teenager "misreads" and lobs a **tennis
  ball** ("you wanna play?"). The dogs must reset the combo. Gag, not death. The 2026-07-20
  couch-test finding (#11, FAIL: "Nothing really funny happens" at a session-ending **MISREADS
  35**) was that this stayed mechanically correct but silent — a generic penalty beep and a label,
  no reaction. The payoff is now: the Teenager half-rises and *offers* the wrong item toward the
  dogs (a brief tween off his rest spot, not a teleport — `PeeBreakMissionController.MisreadOfferPosition`),
  his question bubble gets a fresh emphatic scale-pulse on top of its existing visibility rule,
  both dogs turn to react (`DogReadabilityFeedback.ShowGuidanceNudge`), and a distinct
  `ArenaFeedbackCatalog.SquirrelStunned` "dazed/wrong guess" cue plays alongside the `ScorePenalty`
  warning beep that already fired for the same event (kept last so it remains the audible
  penalty cue). Three-plus misreads in the *same* beat (a beat's own `_beatMisreadsSeen` count, not
  the session total) add one bigger flourish — the accent forces on, the label reads
  "+ EVERYTHING?!", and the question bubble goes "???" — while Misreads, Comprehension, Confusion,
  and rank thresholds stay exactly as before; this is presentation only.

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
- **CF1.5 (2026-07-20) role-flip presentation moment:** the swap used to be entirely silent — the
  couch-test finding (#12) was "Not sure what this means - but i finished the level this time."
  Entering this beat now fires a one-shot banner, layered on top of CF1.4's generic beat-transition
  flourish: a `SetJuice` ping ("NEW JOBS - SWAP!"), a world-pop callout at each dog naming their
  actual new job ("CHEDDAR: NEW JOB - BLOCK HALLWAY!" over Cheddar, "COCOA: NEW JOB - UNPLUG
  CHARGER!" over Cocoa), a symmetric same-shaped pulse above each dog, and both dogs' own S5.1
  handoff chimes (`HandoffChimeCheddar`/`HandoffChimeCocoa`). It deliberately does **not** use a
  directional swoosh between the two dogs (`DogHandoffSwoosh`) or `MissionContext.SignalRoleHandoff`
  — both would misread as "one dog's job moving to the other," which is wrong here: this is a
  simultaneous double-reassignment to two brand-new jobs, not a fromDog→toDog handoff of the same
  job. Fires exactly once per beat-3 entry (`PeeBreakMissionController.RoleFlipSignalCount`); the
  existing beat-3 objective copy already names both new jobs in one line and did not need to change.

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
