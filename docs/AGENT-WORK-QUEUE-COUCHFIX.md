# Couch-Fix Agent Work Queue — 2026-07-20 findings

> **Status: OPEN.** Written 2026-07-20 in response to the Operation Pee Break couch retest
> (`docs/COUCH-PLAYTEST-2026-07-20-PEE-BREAK.md`, gate verdict PARTIAL). Phase CF1 fixes the nine
> Pee Break findings; Phase CF2 audits the eight roster-wide themes the owner flagged; CF3.1 closes
> with an evidence refresh and retest handoff. Successor to the completed
> `docs/AGENT-WORK-QUEUE-PRELAUNCH.md` — same working rules, same evidence bar.

## Authorization scope

The pre-launch queue's "never change mission rules" guardrail is **narrowly relaxed** here: each
task below names the exact behavior change the owner's playtest feedback authorizes (e.g. the
control card no longer auto-dismisses; the door-stare check anchors to the floor). Nothing beyond
what a task names is authorized. Still absolutely off-limits: new missions, roster changes,
difficulty/scoring retuning, mission-specific branches in `GameManager`, anything in
`src/`/`tests/`/`prototype/`, weakening an existing test to get green.

## How to work this queue

- **One task per run.** Finish it test-green, record evidence, update this file's status column,
  commit with the task ID in the message. Do not batch tasks or start a task whose listed
  dependency is not DONE.
- **Preflight every run:** `git status` clean. If another agent's work is uncommitted, stop and
  report — do not build on or revert it.
- **Every task ends with:** `./unity/run-playmode-tests.sh` fully green (baseline **662** as of
  `7e80b52`); a new deterministic test that fails without your change (where the task changes
  behavior); the doc updates named in the task; a commit naming the task ID.
- **Visual tasks additionally end with:** `./unity/build-dev.sh`, `./unity/smoke-player.sh`, and an
  art-review capture (`<built-player> --arena-art-review=<abs-path>`) — **but read the sandbox
  caveat below before trusting capture frames.**
- Status values: `OPEN`, `IN PROGRESS`, `DONE (date, evidence)`, `BLOCKED (reason)`.

## Context pack — read before your first task

**Required reading, in order:** `CLAUDE.md`, `docs/README.md`,
`docs/COUCH-PLAYTEST-2026-07-20-PEE-BREAK.md` (the findings this queue exists to fix — includes
the owner's exact quotes), `docs/ARCHITECTURE.md`, `docs/DEEP-SLICE-OPERATION-PEE-BREAK.md`,
`docs/VISUAL-READABILITY-CONTRACT.md`.

**Primary evidence artifacts:** the owner's annotated screenshots in `captures/2026-07-20/` —
`pee-break-door-stare-wall-climb.png` (CF1.6), `pee-break-misreads-ticker-teenager.png`
(CF1.2/CF1.3/CF1.4), `pee-break-title-card.png` (CF1.9). Look at them before starting the matching
task; they show exactly what the owner saw.

**Architecture map (all paths under `unity/CheddarAndCocoa/Assets/Scripts/Game/` unless noted):**

- `GameManager.cs` (~3,900 lines) orchestrates sessions/missions; mission logic lives in
  per-mission `IMissionController` implementations. Controllers reach shared services only through
  `MissionContext` (defined in `IMissionController.cs`). Shared→controller queries go through
  narrow optional interfaces in `IMissionController.cs` (`IMissionPressureHud`,
  `IMissionOpeningPresentationController`, `IMissionRoleOwner`, …) — follow that pattern for any
  new shared surface.
- `PeeBreakMissionController.cs` (~1,400 lines) owns the deep slice: four beats
  (`Beat.DoorStare → LeashMessage → ChargerGambit → UnitedBark`), a
  `CoopSocialManipulationPuzzle` `_puzzle` tracking comprehension/confusion/misreads, and a
  `TeenPresentationState` enum driving the Teenager's presentation.
- `ArenaHud.cs` draws all screen-space HUD via `OnGUI` (briefing card, top bar, pressure meter).
- Tests live in `unity/CheddarAndCocoa/Assets/Tests/PlayMode/`; Pee Break's are
  `PeeBreakPlayModeTests.cs`. Tests drive missions via deterministic `Force*` hooks on
  `GameManager`.

**Known traps — real bugs previous agents hit in this codebase. Read twice:**

1. **Never `AddComponent<T>()` onto GameManager's own GameObject when `T` hides/moves itself.**
   A component calling `this.gameObject.SetActive(false)` there disables GameManager itself
   (broke 324 tests once). Give it a dedicated child GameObject and toggle only the child
   (see `RoleTurnBeacon.cs` / `ActorSignalBadge.cs` for the correct pattern).
2. **Force-hook tests bypass `GameManager.OnDogBarked`/`OnDogInteracted` dispatch.** If your
   change interacts with real input dispatch (bark fallbacks, interact routing), add at least one
   test through the real dispatch path, not just the controller method.
3. **The full existing suite is the spec.** A plausible improvement that passes the tests you
   wrote for it can still contradict intentional behavior pinned by older tests. Run the FULL
   suite before concluding anything; if an old test disagrees with your reading of the task, the
   old test usually wins — re-scope, don't weaken it.
4. **Read the whole enclosing method before declaring something missing.** Narrow grep windows
   produced false positives twice (a shared block further down already handled the case).
5. **Dog transform writes:** `DogReadabilityFeedback` writes `_authoredPose.transform.localScale`
   from three methods; only `ApplyPersonalityMotion` (called last each frame) sticks. Never
   parent visuals to a dog transform expecting your scale/position to survive — position
   companion objects per-frame instead (see `WeenieRoundupMissionController._carriedMarkers`,
   updated at its line ~124).
6. **`MissionOrder` is append-only.** Inserting anywhere but the end reshuffles every mission's
   deterministic seed. (No task here should touch it at all.)
7. **Periodic ambient-timer fields (`_nextXAt`) default to 0** — initialize them in the same
   method that enables the feature or they fire instantly on the first frame.
8. **Sandbox has no GPU.** The art-review capture completes "clean" but produces flat placeholder
   frames. Before trusting any capture, sample a few frames for unique-color count / pixel
   variance. For UI-composite verification, reproduce the exact layout math in a throwaway
   offline PIL script and render that instead (proven technique; it found a real shipped bug).
   Nobody in the sandbox can see the running game — your tests and offline renders are the proof.
9. **Audio cues must map to real imported clips.** `AuthoredAudioCatalog` has a test requiring
   every cue to resolve to an authored clip — reuse existing cues; do not invent placeholder
   synth slots.

## Status board

| ID | Task | Depends on | Status |
|---|---|---|---|
| CF1.1 | Control card waits for explicit accept | — | DONE (2026-07-20, 664 green (662→664, 2 new tests); `_briefingAwaitingAccept` gates `MissionBriefingVisible` and the lead-in freeze, accept hands off into unchanged `LeadInSniffSeconds` beat) |
| CF1.2 | Comprehension meter mirrored in screen-space HUD | — | DONE (2026-07-20, 666 green (664→666, 2 new tests); `IMissionBeatProgressHud` mirrors `_puzzle.Comprehension` into the top bar via a shared `ProgressNormalized` property also consumed by the world-anchored track; stacked compact meters (27px, unchanged 102px top bar) when paired with the always-on bladder pressure meter) |
| CF1.3 | Misread comedy payoff | — | DONE (2026-07-20, 668 green (666→668, 2 new tests); `TriggerMisreadGag` moves `_misreadProp` from the Teenager toward the dogs via a `_misreadGagT` tween (not a teleport), pulses the question bubble, plays a distinct `SquirrelStunned` cue (kept ahead of the existing `ScorePenalty` call so `LastAudioCueRequested` is unchanged), and nudges both dogs with `ShowGuidanceNudge`; 3+ misreads in one beat (existing per-beat `_beatMisreadsSeen`) escalate the flourish only; misread count/Comprehension/Confusion/beat/outcome proven bit-for-bit unchanged) |
| CF1.4 | Teenager reflects beat progression | — | OPEN |
| CF1.5 | Beat-3 role flip readable on-screen | — | OPEN |
| CF1.6 | Door stare anchors to the floor | — | OPEN |
| CF1.7 | Leash carried in mouth | — | OPEN |
| CF1.8 | Toys discoverable (cosmetic only) | — | OPEN |
| CF1.9 | Title-card crop + team-plan visual chips | — | OPEN |
| CF2.1 | Roster audit: auto-dismissing info UI | CF1.1 | OPEN |
| CF2.2 | Roster audit: HUD/meter occlusion | CF1.2 | OPEN |
| CF2.3 | Roster audit: per-beat world-state progression | CF1.4 | OPEN |
| CF2.4 | Roster audit: funny-failure promises vs actual gags | CF1.3 | OPEN |
| CF2.5 | Roster audit: carried-object visuals | CF1.7 | OPEN |
| CF2.6 | Roster audit: wall-station perspective | CF1.6 | OPEN |
| CF2.7 | Roster audit: briefing beats self-verifying in-game | CF1.5 | OPEN |
| CF2.8 | Roster pass: title cards + team-plan chips | CF1.9 | OPEN |
| CF3.1 | Evidence refresh + retest handoff | all above | OPEN |

---

## Phase CF1 — Operation Pee Break fixes

### CF1.1 — Control card waits for explicit accept
**Finding (#4/#5, PARTIAL):** "The control card dismisses itself after a period of time - I need
time to look at it and accept it."
**Current behavior:** after the opening explainer finishes, `GameManager` shows the briefing/
controls card for a fixed `ArenaMissionTuning.IntroPromptSeconds` (5f, `ArenaMissionTuning.cs:22`)
via `_introPromptUntil` (`GameManager.cs:602`, set at `GameManager.cs:1413` and again at
`GameManager.cs:1495` when the explainer completes; visibility gate `MissionBriefingVisible`,
`GameManager.cs:281`). The card is part of the lead-in freeze (`_leadInRemaining`,
`GameManager.cs:1414` — briefing + sniff seconds; any bark/interact skips straight to GO).
**Authorized change:** the card no longer expires on a timer. It stays up — game frozen — until
either player presses bark or interact ("accept"). The `LeadInSniffSeconds` discovery beat still
follows after accept, unchanged.
**Do:**
1. Make card visibility state-based, not time-based (e.g. a `_briefingAwaitingAccept` flag
   consumed by the same input path that currently ends the lead-in early — trace
   `EndLeadIn`/bark/interact handling around `GameManager.cs:751`).
2. While the card is up, the mission clock and dogs stay frozen exactly as the current lead-in
   freeze already does — reuse that mechanism, don't invent a second freeze.
3. Update the card's footer copy in `ArenaHud.cs` to say what accept is (follow the existing
   glyph helpers `DrawPadButton`/`DrawKey`; see the skip hint idiom at `ArenaHud.cs:415`).
4. Check the early-skip interaction at `GameManager.cs:1581-1582` (skip while card is up drops
   card and banner together) still makes sense.
**Watch out:** many tests use the `LeadInSecondsOverride` seam (`GameManager.cs:457-458`) to skip
lead-ins — keep that seam working (override present ⇒ old timed behavior or auto-accept, your
choice, but document it in the seam's XML comment). The F4.1 first-mission control strip and
Backyard Rescue tutorial share a mutually-exclusive `else if` slot with the pause-menu — don't
disturb it.
**Done when:** new tests prove (a) card still visible well past the old 5s with no input,
(b) `ForceBark`-through-real-dispatch dismisses it, (c) mission clock did not advance while it was
up. Full suite green. Update `docs/ARENA-PLAYABLE.md` manual acceptance notes and
`docs/DEEP-SLICE-OPERATION-PEE-BREAK.md`'s first-session flow section.

### CF1.2 — Comprehension meter mirrored in screen-space HUD
**Finding (#8, FAIL):** "The progress meter disappears when the dogs move to bottom part of
screen." See `captures/2026-07-20/pee-break-misreads-ticker-teenager.png`.
**Root cause (verified):** the beat-progress ("TEENAGER GETS IT") meter is world-anchored — a
child of the Teenager at +3.1 local Y (`PeeBreakMissionController.cs:741-743`, updated at
`:1292-1303`). The camera follows the dogs; when they move to the room's lower half the Teenager
and his meter scroll off-screen, exactly when players most need to see whether their signal is
landing.
**Authorized change:** mirror beat progress into the screen-space HUD. Keep the world-anchored
track too (it's good diegetic feedback when visible).
**Do:**
1. Add a narrow optional interface to `IMissionController.cs` — suggested name
   `IMissionBeatProgressHud` — modeled exactly on `IMissionPressureHud`
   (`IMissionController.cs:88-95`): `ProgressVisible`, `ProgressLabel`, `ProgressNormalized`,
   `ProgressColor`.
2. Render it in `ArenaHud.cs` directly below the pressure meter — reuse
   `BuildPressureMeterRect` (`ArenaHud.cs:138`) / `DrawPressureMeter` (`ArenaHud.cs:668`) layout
   idioms; the render call site to mirror is `ArenaHud.cs:435-439`.
3. Implement on `PeeBreakMissionController`: label like `"TEENAGER GETS IT (BEAT n/4)"`,
   normalized = the same clamped comprehension computed at `:1292-1294`, hidden once `DoorOpen`.
**Watch out:** `ArenaHud` is `OnGUI` immediate-mode — no GameObjects involved, so trap #1 doesn't
apply here, but keep the meter inside the top-bar layout so it can't collide with the F4.1
control strip or the guidance-ladder amber flash.
**Done when:** tests assert HUD values track `_puzzle.Comprehension` across a beat and hide on
`DoorOpen`; full suite green. Note the new interface in `docs/VISUAL-READABILITY-CONTRACT.md`
(new "screen-space progress meter" bullet) and `docs/ARENA-PLAYABLE.md`.

### CF1.3 — Misread comedy payoff
**Finding (#11, FAIL):** "Nothing really funny happens." The session ticker hit **MISREADS 35** —
thirty-five silent failures in the mission whose design promise is "early barks cause a funny
misread, not a failure."
**Current behavior:** misreads are detected at `PeeBreakMissionController.cs:459-470`: a juice
warning, a cycling wrong-guess label ("TENNIS BALL?" / "BLANKET?" / "DINNER?"), and
`ShowMisreadProp` (prop objects at `:620-621`, tennis-ball art at `:810`). Functional, not funny.
**Authorized change:** presentation-only comedy on misread. Misread *mechanics* (attempt reset,
scoring, rank thresholds) must not change.
**Do:** make the Teenager act out the wrong guess. On misread: he half-rises and offers the
wrong item toward the dogs (move/animate the existing misread prop from him toward the dogs),
question bubble (`_teenagerQuestionBubble` exists, `:104`), a distinct existing audio cue, and
both dogs visibly react (e.g. `ShowGuidanceNudge`/head-shake — reuse existing dog feedback calls,
no new art). Vary staging by the same `Misreads % 3` cycle already used for the label. Add one
escalation beat: after 3+ misreads in one beat, a bigger flourish (e.g. he brings ALL three wrong
items out, bubble goes "???").
**Reuse — the gag template (proven five times in `BackyardRescueArtEnhancer.cs`):** state field +
cooldown constant + timer initialized in the same method that enables the feature (trap #7) +
`Update()`/tick branch + a public `TrySpawnX()` seam + a two-assertion test (fires when the
condition holds, stays quiet when it doesn't).
**Watch out:** trap #9 (audio cues must resolve to real clips — pick from existing
`ArenaFeedbackCatalog` cues). `FinalJuiceEffect` once destroyed same-frame controller pops —
verify your pop survives a same-frame `Tick()`+misread by asserting it in a test.
**Done when:** the two-assertion gag test passes; a test proves misread mechanics unchanged
(same misread count/attempt state before vs after for the same inputs); full suite green. Update
`docs/DEEP-SLICE-OPERATION-PEE-BREAK.md`'s misread section.

### CF1.4 — Teenager reflects beat progression
**Finding (#8b):** "Ideally after each phase, the teenager looks a little different, or shifts
positions to indicate we're on the 'next subproblem'."
**Current behavior:** `TeenPresentationState` (`PeeBreakMissionController.cs:24-31`, driven at
`:1325-1346`, rendered at `:1050-1129`) reacts to *moment-to-moment* dog signals but looks the
same in beat 1 as in beat 3. The Teenager has ready-made child parts to work with: `_teenagerHead`,
`_teenagerHoodie`, `_teenagerThumbs`, `_teenagerFootWiggle`, `_teenagerPhoneBeam`,
`_teenagerQuestionBubble`, `_teenagerOhBubble` (`:98-105`), phone battery props (`:106-108`).
**Authorized change:** presentation-only per-beat deltas layered on top of the existing state
machine (do not replace it).
**Do:** drive a visible posture/prop delta from `_beatIndex`, e.g.: beat 1 — as today; beat 2 —
phone held lower, foot wiggle stops; beat 3 — sits upright, phone-battery panic props visible
(they already exist for the charger beat — make the posture change too); beat 4 — half-standing,
glancing at the door between phone checks. Add a one-shot transition flourish when a beat
completes (world pop + the "Oh" bubble) so the *moment* of advancing reads, not just the new
steady state. Keep `StandingSuccess` exactly as-is.
**Done when:** a test steps through beats via the existing beat-completion path and asserts a
distinct presentation marker per beat (expose a small read-only presentation summary if needed
for determinism); full suite green. Update the mission's manual acceptance notes in
`docs/ARENA-PLAYABLE.md`.

### CF1.5 — Beat-3 role flip readable on-screen
**Finding (#12):** "Not sure what this means - but i finished the level this time" — the
checklist's "beat 3 role flip" was invisible to the players.
**Current behavior:** entering `Beat.ChargerGambit` silently swaps both dogs to new jobs
(`RequiredByBeat`, `PeeBreakMissionController.cs:48`: Cocoa unplugs charger, Cheddar blocks
hallway). G1.4 **deliberately did not** wire `MissionContext.SignalRoleHandoff`
(`IMissionController.cs:177`) here because this is a simultaneous both-dogs re-assignment, not a
fromDog→toDog handoff — forcing that shape would misread. That decision stands; build the right
shape instead.
**Authorized change:** presentation-only role-flip moment on entering beat 3.
**Do:** when beat 3 begins: freeze-adjacent banner beat (a short `SetJuice` + two per-dog world
pops at each dog: "CHEDDAR: NEW JOB - BLOCK HALLWAY!" / "COCOA: NEW JOB - UNPLUG CHARGER!"),
swap-arrow visual between the two dogs (`DogHandoffSwoosh.cs` is the existing flourish component
— spawn it standalone, NOT on GameManager's GameObject, trap #1; if a single swoosh between dogs
misreads as one-way, spawn the simpler pulse pair instead), one audio cue per dog (the S5.1
handoff chimes `HandoffChimeCheddar`/`HandoffChimeCocoa` are each dog's own bark take — firing
both is honest here). Also check the beat-3 objective copy (`:218` area) names BOTH new jobs in
one line.
**Done when:** a test drives beats 1→3 and asserts the flip presentation fired exactly once, with
both dogs' pops; full suite green. Update `docs/DEEP-SLICE-OPERATION-PEE-BREAK.md` beat 3
description.

### CF1.6 — Door stare anchors to the floor
**Finding (#6):** "It's weird that the door stare has to happen by climbing up the wall?" See
`captures/2026-07-20/pee-break-door-stare-wall-climb.png` — both dogs render lying ON the door.
**Root cause (verified):** `_doorPosition` (`PeeBreakMissionController.cs:237`) is the door
object's center, and the door art is drawn tall up the room's back wall (`:609`, scale 2.4×4).
The stare check requires standing within `StationRange` (2.25, `:33`) of that center (`:442`), so
the "correct" spot is visually halfway up the door.
**Authorized change:** the stare interaction anchors to a floor point at the door's base; door
art unchanged; range values unchanged.
**Do:**
1. Add `_doorStareAnchorPosition` — the doormat's world position (doormat child at `:762`, local
   offset (-0.02, -0.62)) or an explicit floor point just below the door slab.
2. Retarget every dog-position check and placement from `_doorPosition` to the anchor:
   the stimulus check `:442`, united-bark proximity `:328`, forced placement `:353`,
   success placement `:536-537`, `EntryTarget`/`DoorPosition` consumers (`:196-200`), and the
   `TryGetObjectiveTarget` door target (`:367-375` — point it at the anchor or a marker there so
   arrows/beacons guide dogs to the floor spot, not up the wall).
3. Give the staring dog a grounded pose while the stimulus is active — use an existing pose only
   (per the A2.1 audit, Beg/Sit strips don't exist; a flop or idle-facing-door with the existing
   pleading presentation is fine; do NOT add new art in this task).
**Watch out:** `PeeBreakPlayModeTests.cs` drives dogs to `_doorPosition`-relative spots — those
tests will need retargeting to the anchor; that's expected fallout, not test-weakening, but touch
only position constants in them, never assertions about outcomes.
**Done when:** a test proves the stare stimulus fires at the floor anchor and does NOT fire at
the old door-center height; full suite green. Update `docs/DEEP-SLICE-OPERATION-PEE-BREAK.md`
station map + `docs/ARENA-PLAYABLE.md` acceptance notes.

### CF1.7 — Leash carried in mouth
**Finding (#13):** "Pretty good - the leash needs to be animated though, maybe carried around in
mouth?"
**Current behavior:** the leash (`PeeBreakMissionController.cs:58`, placed at `:238`, presented
trail `:118`) sits static at its station while `PresentLeash` is active.
**Authorized change:** presentation-only: while Cheddar holds `PresentLeash`, the leash visual
follows his muzzle; when not presenting, it rests back at the hook.
**Do:** copy the carried-marker idiom from `WeenieRoundupMissionController.cs` — a standalone
marker object repositioned every tick to `dog.position + offset`
(`WeenieRoundupMissionController.cs:124`, art override at `:246-250`). Do NOT `SetParent` to the
dog (trap #5 — the dog's pose system owns its transform; parented visuals inherit scale flips).
Offset toward the muzzle on the dog's facing side; reuse the existing leash sprite/art. Add a
small sway (sin-based, like existing personality motion) so it reads as dangling.
**Done when:** a test asserts the leash visual is at the dog-adjacent position while
`PresentLeash` is active and back at the hook after; full suite green.

### CF1.8 — Toys discoverable (cosmetic only)
**Finding (#10, UNVERIFIED):** "Didn't see this working or not…" — the batting toys
(`_squeakyToy`/`_playBall`, kick logic `PeeBreakMissionController.cs:302-319`) exist but nobody
found them.
**Authorized change:** cosmetic discoverability only. The design intent — toys never affect the
mission — is load-bearing; do not add scoring, objectives, or guidance arrows to them.
**Do:** (1) first time either dog comes within `ToyInteractRange + 1` of a toy, a one-shot world
pop: "TOYS! (just for fun)" + a wiggle pulse on the toy; (2) an ambient idle wobble/glint on
untouched toys every ~10s so they read as interactive (trap #7: initialize the `_nextXAt` timer
where toys are set up, `:262` area). Follow the gag template shape from CF1.3.
**Done when:** two-assertion test (discovery pop fires once on approach; never fires again;
ambient wobble doesn't alter toy position/velocity); full suite green.

### CF1.9 — Title-card crop + team-plan visual chips
**Finding (freeform):** title-card art "seems to crop down most of the image… detail is lost";
"the Team plan should ideally show little visual clips of what things will actually look like
on-screen if possible, just the important ones." See `captures/2026-07-20/pee-break-title-card.png`.
**Current behavior:** `MissionSelectScreen.cs` — detail cover is a 300px-tall window
(`DetailCoverHeight`, `:37`; built `:323-337`) filled with cover-fit (`FitDetailCover`, `:530`,
`:577-592` — deliberately `Mathf.Max` cover-fit since V3.3, which fills width by cropping
top/bottom of the square tile art; that crop is the detail loss the owner saw). The team plan is
text-only (`BuildHowToPlayText`, `:480`, rendered `:540`; header "YOUR TEAM PLAN" `:366`).
**Authorized change:** (a) reduce crop loss in the detail cover; (b) add small visual chips to
the team plan showing the actual on-screen signal objects — "just the important ones."
**Do:**
1. **Crop:** options in order of preference — taller `DetailCoverHeight` (steal height from the
   text pane only if the plan text still fits), and/or a per-mission crop-bias so the tile's
   subject stays in frame. **Verify offline first** (trap #8): rebuild the throwaway PIL
   simulation of `FitTileCover`/`FitDetailCover` math (the V3.3 one was scratchpad-only and is
   gone — its two past bugs to avoid: apply the pixel multiplier to cover scale, and flip the
   Y-axis sign when converting Unity's Y-up `anchoredPosition` to PIL's Y-down paste). Render
   before/after for at least 6 missions including Pee Break before touching C#.
2. **Chips:** next to each of Pee Break's plan bullets, a small icon of the real on-screen
   object (door, leash, charger, bark burst) loaded from the same `FinalGameplayArt` resources
   gameplay uses — so the picker literally previews what players will look for. Build it
   data-driven (per-mission list of `(bullet, sprite)` in `MissionInstructionCatalog.cs` or
   alongside `BuildHowToPlayText`) because CF2.8 rolls it out roster-wide. Missions without chip
   data render text-only, exactly as today.
**Watch out:** `BuildHowToPlayText` has a hand-written 4-line Pee-Break-only override and a
generic first-4-steps cap — read it fully before wiring chips so counts line up.
`MissionSelectScreenPlayModeTests.cs` pins cover-fit behavior (`DetailCoverFillsWidth`, `:145`) —
keep width-filling true.
**Done when:** PIL before/after renders show more art surviving (attach to commit or describe
measured crop %); tests cover chip lookup (Pee Break has chips; a chip-less mission renders
text-only); full suite green. Update `docs/ARENA-PLAYABLE.md` mission-select notes.

---

## Phase CF2 — Roster theme audits

Each CF2 task is an audit-then-fix: produce a per-mission findings table **in this file under
your task's section**, then fix what the task scopes. The matching CF1 task is your reference
implementation — read its shipped code first. Audit method requirements: read the WHOLE enclosing
method before declaring a gap (trap #4), and check the shared dispatch layers
(`GameManager.OnDogBarked` fires a generic bark cue; `MarkFailedInteraction` fires
UI-disabled audio) before calling a moment "silent."

### CF2.1 — Auto-dismissing info UI
**Theme:** timed instructional UI fails slow readers.
**Note:** CF1.1's fix is in shared `GameManager`/`ArenaHud` code, so the briefing card is fixed
roster-wide already. This audit sweeps for OTHER timed instructional surfaces: the Backyard
Rescue per-dog tutorial steps (`GameManager.cs:334` area), the F4.1 first-mission control strip
(20s fade), mission-start banners, and anything else gating comprehension on a timer.
**Rule to apply:** *blocking* instruction (players can't reasonably proceed without reading it)
must be player-paced or re-summonable; *ambient* reminders (control strip, banners) may stay
timed but must be re-summonable (e.g. from pause). Fix what fails the rule; table everything.

### CF2.2 — HUD/meter occlusion
**Theme:** world-anchored meters/labels that camera-follow can push off-screen.
**Do:** for every mission, list each meter/label players need mid-play, its anchor object, and
whether the camera (which follows the dogs, clamped to `ArenaBounds` by
`SharedCameraController`) can leave it off-screen during normal play. Fix load-bearing ones by
implementing CF1.2's `IMissionBeatProgressHud` (or `IMissionPressureHud` if it's pressure-shaped);
decorative ones just get a table row. Check `WorldLabelVisibility` proximity gating too — a label
that only appears near the object AND off-screen is doubly invisible.

### CF2.3 — Per-beat world-state progression
**Theme:** multi-phase missions should change something visible in the WORLD per phase — HUD
text alone didn't land.
**Do:** for each mission with sequential phases/beats (Pee Break, Great Escape, Chaos Machine,
Bone Relay, Table Stealth, Walk Campaign, Car Ride, Baby Bird Bedlam at minimum — derive the real
list from controllers), record what visibly changes in the world when a phase completes. CF1.4
(Teenager deltas) is the reference. Fix the worst 3-5 using existing art/props only — a prop
state change, an NPC posture shift, a lighting/sunbeam change. No new art in this task; table the
rest as future art asks.

### CF2.4 — Funny-failure promises vs actual gags
**Theme:** 35 silent misreads. If a briefing, HowToPlay step, or design doc promises a failure is
funny, there must be an actual on-screen gag.
**Do:** sweep `MissionInstructionCatalog.cs`, mission `IntroPrompt`s, and
`docs/GAME-DESIGN-BIBLE.md` mission entries for comedy claims ("funny", "hilarious", gag
descriptions); for each, trace the failure branch in the controller and verify a visible+audible
reaction actually fires (S5.2 already verified *audio* coverage — this audit is about whether the
result is a GAG, not a beep). CF1.3 is the reference implementation and
`BackyardRescueArtEnhancer.cs` holds the five-gag template. Fix the three worst silent-comedy
spots; table the rest.

### CF2.5 — Carried-object visuals
**Theme:** logically-held objects rendered as static/floating break the fantasy.
**Do:** per mission, list every object a dog logically holds/drags mid-mechanic (leash, weenies,
bones, socks, blanket corners, rope, chicks…) and how it renders during the carry. Weenie
Roundup's `_carriedMarkers` and CF1.7's leash are the reference idiom (per-frame follow, never
SetParent — trap #5). Fix gaps where an object visibly teleports or floats unheld; sway/dangle
polish is a bonus, not required.

### CF2.6 — Wall-station perspective
**Theme:** interaction anchors centered on tall wall-drawn art make dogs look like they're
climbing.
**Do:** per mission, list every interaction point whose art is drawn vertically (doors, gates,
fences, counters, tables, windows) and check whether the dog's required standing position
overlaps the art's upper body. CF1.6's floor-anchor pattern is the fix. Prioritize any station a
dog must HOLD (sustained overlap reads worst); table brief-touch stations.

### CF2.7 — Briefing beats self-verifying in-game
**Theme:** a player read "beat 3 role flip" in the plan and still didn't recognize it happening.
F4.3 verified briefings are *accurate*; this audit verifies they're *recognizable*.
**Do:** for each mission, walk each team-plan bullet (`BuildHowToPlayText` output — remember the
4-step cap and Pee Break override) and ask: at the moment this bullet happens in-game, does any
on-screen text/pop/banner use the same words? Where the moment passes unnamed, add a world pop or
juice line echoing the briefing's phrasing at the transition (CF1.5 is the reference). Keep copy
in the mission's existing voice; verify gold-caps quoting mechanically (grep the literal string)
per the F4.3 method.

### CF2.8 — Title cards + team-plan chips, roster pass
**Do:** apply CF1.9's crop settings and chip system to all 23 missions: author the per-mission
chip lists (2-4 chips each, only the load-bearing signals), and verify every mission's detail
cover under the new crop via the PIL simulation (render all 23, eyeball each — subjects centered,
no important detail cropped). Fix per-mission crop bias where needed. This is the roster
application of CF1.9, not a redesign.

---

## CF3.1 — Evidence refresh + retest handoff
**Do (after all CF1/CF2 tasks are DONE or explicitly BLOCKED):** refresh the couch-retest
preflight doc (`docs/OPERATION-PEE-BREAK-COUCH-RETEST-2026-07-14.md`): new suite count, build
hash, date, and a summary of everything this queue changed. Regenerate the 16-item watch-for
checklist: keep items that passed (verify none regressed), rewrite items 4/5/6/8/10/11/12/13
to check the specific fixes (e.g. "card waits for your button press and the game is frozen until
then", "misread makes you laugh at least once", "you can tell the moment beat 3 flips your
jobs"), and add new watch-fors from CF2 findings. Update
`docs/COUCH-PLAYTEST-2026-07-20-PEE-BREAK.md`'s gate banner to note the fix queue completed and
the gate awaits the next human session. Update this file's status header to COMPLETE. Run full
suite + dev build + smoke and record the evidence here.
