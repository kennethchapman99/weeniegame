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
| CF1.4 | Teenager reflects beat progression | — | DONE (2026-07-20, 670 green (668→670, 2 new tests); new `TeenagerBeatLook` enum drives a per-beat baseline (phone height/foot-wiggle/hoodie stretch/head tension-glance) layered under the existing `TeenState` deltas in the per-part render block, plus a one-shot `"NEXT!"` world pop + timed Oh-bubble flash on `AdvanceBeat()`, skipped on the final door-open transition) |
| CF1.5 | Beat-3 role flip readable on-screen | — | DONE (2026-07-20, 671 green (670→671, 1 new test); banner `SetJuice` + per-dog "NEW JOB" world pops + a symmetric per-dog pulse pair (not a directional swoosh — see task rationale) + both `HandoffChimeCheddar`/`HandoffChimeCocoa` fire exactly once entering Beat 3, gated on `_beatIndex == 2` in `AdvanceBeat()`; `SignalRoleHandoff`/`HandoffSignalCount` proven untouched) |
| CF1.6 | Door stare anchors to the floor | — | DONE (2026-07-20, 673 green (671→673, 2 new tests); `DoorStareAnchor` (the existing doormat's floor position) replaces `_doorPosition` (door art's tall-wall center) for the stare stimulus, united-bark proximity, entry staging, objective/breadcrumb target, and the `cocoaAtDoor` read driving labels/markers — measured ~2.48-unit vertical gap, over `StationRange` 2.25; `HandleBark`'s bark-timing pad widened +1f→+1.3f so Cheddar's real leash-position bark still registers "near the door" against the relocated anchor; Cocoa gets a grounded facing-the-door idle pose via the existing `ShowGuidanceNudge`; door ART itself unchanged) |
| CF1.7 | Leash carried in mouth | — | DONE (2026-07-20, 674 green (673→674, 1 new test); `_leashArt` now follows Cheddar's muzzle (`_context.Dogs[cheddarIndex].transform.position + facing * 0.45f + (0,-0.2f)`, small sin-based dangle sway) while `cheddarAtLeash` is true, and rests on `_leashHook` otherwise, instead of sitting fixed at `_leash`'s station regardless of Cheddar; facing signal is `DogReadabilityFeedback.FacingDirection`, a new read-only exposure of the existing persistent `_lastIntentDir` field (survives standing still, unlike `DogController.CurrentVelocity`); `_leash`/`_leashPosition`/the `PresentLeash` stimulus check/objective target are unchanged; test proven to fail against pre-fix code (1.16-unit gap, matching the hook-vs-station offset) before the fix restored it green) |
| CF1.8 | Toys discoverable (cosmetic only) | — | DONE (2026-07-20, 676 green (674→676, 2 new tests); mission-wide one-shot `"TOYS! (just for fun)"` world pop + `_context.Pulse` fires the first time either dog comes within `ToyInteractRange + 1` of either toy and never again that mission (scoped mission-wide, not per-toy - the finding's "fires once... never fires again" phrasing reads as one reassuring aside, not a per-prop nag); an untouched toy also gets a deltaTime-accumulated ~10s idle scale/tint wobble (`AdvanceToyDiscovery`, `ToyWobbleFactor`) gated off whenever that toy's kick velocity is non-trivial so it never fights `AdvanceToy`'s physics; both cues proven to never alter toy position/velocity) |
| CF1.9 | Title-card crop + team-plan visual chips | — | DONE (2026-07-20, 679 green (676→679, 3 new tests); PIL offline simulation across 6 missions measured the old 300px/1.36x `FitDetailCover` constants showed only ~18% of the square cover art's area, new 340px/1.05x constants show ~34% (+90% relative, nearly double) with `DetailCoverFillsWidth`/`DetailCoverUsesTitleFreeCrop` both keeping large margin; data-driven `TeamPlanChipsFor(variant)` (parallel array to `PreviewStepsFor`, null = no chip data = renders exactly as before) adds 4 icon chips to Operation Pee Break's team plan (door/leash/charger/`BarkBurst` - the real shared bark VFX sprite, not a placeholder) via a fixed 4-row chip pool that swaps in for the single text block only when chip data exists; every other mission verified still text-only) |
| CF2.1 | Roster audit: auto-dismissing info UI | CF1.1 | DONE (2026-07-21, 685 green (679→685, 6 new tests); audited every timed/state-based instructional surface roster-wide - ActionTutorial and the CF1.1 card already pass, MissionBanner is unrendered (N/A), Pee Break's opening video isn't timer-gated - and fixed the one real gap: the F4.1 control strip had no way back once spent, now re-summonable from pause via `FirstMissionControlStripAvailable`/`ReplayFirstMissionControlStrip`, proven through real pause-menu input dispatch) |
| CF2.2 | Roster audit: HUD/meter occlusion | CF1.2 | DONE (2026-07-21, 686 green (685->686, 1 new roster-wide test); audited all 23 controllers - every other mission already mirrors its mid-play progress number into the screen-space `ObjectiveLabel` top bar, so none has Pee Break's pre-CF1.2 shape (a continuously-updating value that exists ONLY on a fixed world object); no new HUD interface implementations needed. Follow-up per coordinator request: `ObjectiveLabelProgressEchoPlayModeTests.cs` encodes the invariant itself - loops all 23 missions, pokes one small deterministic Force* hook per mission, asserts `ObjectiveLabel` changes - so a future mission that adds a load-bearing counter without echoing it into ObjectiveLabel fails this test instead of waiting for the next couch test) |
| CF2.3 | Roster audit: per-beat world-state progression | CF1.4 | DONE (2026-07-21, 689 green (686→689, 3 new tests); audited all 23 controllers for sequential-phase/beat structure - most multi-phase and repeated-cycle missions already carry the beat forward into persistent per-station/per-object world art (Great Escape, Chaos Machine, Leash Walk, Coyotes Fence, Eagle Shadow Panic, Squirrel Conspiracy, Scent Search, Weenie Roundup); fixed the worst 3 genuine gaps where the ONLY place overall mission-level progress was visible was HUD text - Baby Bird Bedlam's nest, Car Ride's dashboard, and Bone Relay's scent post now each carry a persistent progress baseline using only existing sprites/tints) |
| CF2.4 | Roster audit: funny-failure promises vs actual gags | CF1.3 | DONE (2026-07-21, 691 green (689→691, 2 new tests); swept `MissionInstructionCatalog.cs`/mission `IntroPrompt`s/`docs/GAME-DESIGN-BIBLE.md` for comedy claims - only ONE genuine silent gap found (WalkCampaign's misread, which reused the roster-wide generic `ThreatWarning` cue and a static sprite/label swap, no distinct cue or dog reaction); fixed it with `TriggerMisreadGag` (human leans toward the dogs holding the wrong item via `_misreadGagT`, a distinct `SquirrelStunned` cue kept ahead of the existing `ThreatWarning` call, both dogs `ShowGuidanceNudge`, third/mission-ending misread snaps to the full offer as a freeze-frame); honestly reported fewer than 3 fixes since the broader sweep found no other in-scope gaps - see findings table below) |
| CF2.5 | Roster audit: carried-object visuals | CF1.7 | DONE (2026-07-21, 692 green (691→692, 1 new test); audited all 23 controllers for dog-carries-an-object mechanics — Weenie Roundup, Baby Bird Bedlam, and Blanket Catch already track the carrying dog(s) correctly (two arrived at the reference idiom independently of this audit); fixed the one genuine gap, Walk Campaign's leash, which sat fixed at its station regardless of `_leashPresented` — now follows Cheddar's position via `ResolveLeashPosition()`, the same standalone per-tick-follow idiom as CF1.7/`_carriedMarkers`, never `SetParent` (trap #5)) |
| CF2.6 | Roster audit: wall-station perspective | CF1.6 | DONE (2026-07-21, 693 green (692→693, 1 new test); audited all 23 controllers' `NewMarker`/`NewScenery` scale calls for tall (Y>>X) wall-drawn art - only Gate Crash's gate and Coyotes Fence's fence gaps qualify as architecture, plus 3 tall-but-non-architectural markers (a squirrel decoy, two human characters) correctly excluded; fixed Gate Crash's gate (a HOLD-type station, the mission's namesake, flagged high-risk) with a `GateFloorAnchor` child transform (same -0.62 ratio as Pee Break's doormat) now driving the hold check, engage check, and Cocoa's guidance arrow, plus a grounded pose via the existing `ShowGuidanceNudge`; tabled Coyotes Fence's tighter-margin fence-gap repair check because it's brief-touch, not sustained, per the task's own prioritization) |
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
**Theme:** timed instructional UI fails slow readers - the owner's exact complaint about Pee
Break's briefing card (fixed roster-wide by CF1.1). This audit sweeps every OTHER mission-facing
surface that could gate comprehension on a timer.
**Rule applied:** a *blocking* instruction (players genuinely cannot proceed / are meant to read it
before acting) must be player-paced (waits for input) or re-summonable. An *ambient* reminder (one
that doesn't gate actual comprehension) may stay timed, but must still be re-summonable (e.g. from
pause) even if it can auto-dismiss on its own.

**Findings table** (surveyed via `GameManager.cs`/`ArenaHud.cs` full-method reads per trap #4, plus
a codebase-wide grep for `Time.time <`-gated `*Visible` properties and `...Seconds` constants tied
to instructional/explainer text):

| Surface | Classification | Current dismiss behavior | Pass/Fail | Notes |
|---|---|---|---|---|
| Mission briefing/controls card (`MissionBriefingVisible`) | Blocking | Player-paced (`_briefingAwaitingAccept`, waits for bark/interact/grab) | **Pass** | CF1.1's shipped fix; out of this task's scope, unchanged. |
| Backyard Rescue `ActionTutorial` (`ShowActionTutorial`/`CurrentTutorialAction`) | Blocking | Completion-gated only - each dog must actually perform Bark→Interact→Jump→Wrestle in order; **no timer anywhere in the enclosing method** (`TutorialActionDoneForAll`/`TryRecordTutorialAction`, `GameManager.cs:990-3018`, read in full). Pause exposes both **Skip Tutorial** and **Replay Tutorial** (`SkipActionTutorial`/`ReplayActionTutorial`, wired in `ArenaHud.ActivatePauseOption`). | **Pass** | Confirms the task doc's suspicion: this is stronger than the rule requires (it demands action, not just a read), plus it already has a working pause re-summon. No change needed. |
| F4.1 first-mission control strip (`FirstMissionControlStripVisible`) | Ambient (explicitly documented as a reminder, not a required read - the ActionTutorial covers the same ground in more depth whenever it's mission one) | Timed: 20s + 2s fade, or all 4 verbs used once, or explicit pause **Skip**. Once gone (timeout, skip, or all-verbs-used) there was **no way back** for the rest of the session - not even a fresh mission re-arms it (by design, it's a once-per-session reminder). | **FAIL → FIXED** | Genuine gap: ambient reminders must stay re-summonable and this one was a permanent dead end once spent. See fix below. |
| Mission-start banner (`MissionBanner`, driven by `_introPromptUntil`) | N/A - not a UI surface at all | Set to timed text in `GameManager.cs`, but **grepped roster-wide and confirmed unrendered**: no `ArenaHud`/UGUI code reads `MissionBanner` for on-screen display (`ArenaHud.cs` never references it). It exists purely as test-observable state (`ArenaGameLoopPlayModeTests`/`KitchenFoodFrenzyPlayModeTests` assert its clear/fail text) - a leftover from before CF1.1's card took over the player-facing role. The code comment at `GameManager.cs:1623` already calls it "(currently unrendered)". | **N/A** | Nothing to fix - there is no player-visible timer to gate comprehension on, since nothing reaches the screen. Flagging here so a future reader doesn't mistake the `_introPromptUntil` gate on this string for a live UI bug. |
| Operation Pee Break opening video explainer (`IMissionOpeningPresentationController`/`IsPresentingOpening`) | Ambient/skippable, single-mission today | Plays to its natural video-completion event (`OnOpeningExplainerFinished`), an explicit per-player skip (`SkipMissionOpeningPresentation`), or an 18s safety-timeout fail-safe (`OpeningExplainerSafetyTimeoutSeconds`) miles past the video's own ~10s runtime, guarding only against a stuck/broken video player. | **Pass** | Already passed the couch test itself (checklist #2/#3 both Pass); it isn't the comprehension gate (the player-paced briefing card that follows is), and it's the only mission with this interface today, so there's no roster-wide instance to fix. |
| One-shot world-pop juice (misread gag, beat-transition "NEXT!"/Oh-bubble, beat-3 role-flip banner, toy-discovery pop, handoff chip flash, score pop) | Ambient/decorative reinforcement | Each fires once and fades on its own short timer; none is any mission's sole way to learn a required mechanic - all are supplementary flourish layered on already-visible HUD/world state. | **Pass** | Not "instructional UI" in the sense this theme targets (nothing is being read here that isn't otherwise available); no re-summon expectation applies to a flourish reacting to something that already happened. |
| Mission-select "YOUR TEAM PLAN" panel (`DetailHowToVisible`) | N/A | State-based (visible whenever the mission-select detail panel is open), no timer at all. | **Pass** | Not in scope for this theme; CF2.7/CF2.8 territory. |

**Fix — F4.1 control strip is now re-summonable from pause.** Added
`GameManager.FirstMissionControlStripAvailable` (`GameManager.cs:434`, mirrors
`ActionTutorialAvailable`'s role: true for any active mission other than Backyard Rescue,
deliberately not limited to literally the session's first mission, so a player who missed the one
automatic showing is never stuck) and `GameManager.ReplayFirstMissionControlStrip()`
(`GameManager.cs:478`, mirrors `ReplayActionTutorial` - re-arms a fresh 20s window and clears prior
verb-used marks). `ArenaHud`'s pause menu (`PauseHasActionRow`, `ActivatePauseOption`,
`DrawPauseMenu`) now gates the control-strip row on `FirstMissionControlStripAvailable` instead of
`FirstMissionControlStripVisible`, so the row stays present even after the strip is gone; the same
row toggles its action and label between **Skip Control Reminder** (while showing) and **Show
Control Reminder** (once gone) exactly like the Tutorial row already toggles Skip/Replay. The two
rows remain mutually exclusive, unchanged.

Covered by five new tests in `FirstMissionControlStripPlayModeTests.cs` (availability gating vs.
Backyard Rescue's own tutorial slot; re-summon after skip, after natural timeout, and after all
verbs used, each restoring full-strength alpha and clearing prior verb marks; a no-op guard during
Backyard Rescue) plus one new file, `FirstMissionControlStripPauseMenuPlayModeTests.cs`, driving the
fix through **real pause-menu input** (`InputTestFixture` keyboard device, actual
`ArenaHud.Update()` D-pad/keyboard navigation and `ActivatePauseOption` dispatch, not a direct
`GameManager` method call) per trap #2. Verified the new tests fail to compile against the
pre-fix source (temporarily reverted `GameManager.cs`/`ArenaHud.cs` only, kept the new tests) before
restoring the fix. Full suite green at `685/685` (679→685, 6 new tests), reproduced clean on three
consecutive runs.

Manual acceptance check: start any non-Backyard-Rescue mission, let the control-strip reminder
fade out (or press Skip from pause), then open pause again - a **Show Control Reminder** button
should now appear where **Skip Control Reminder** used to be; selecting it brings the strip back at
full strength.

### CF2.2 — HUD/meter occlusion
**Theme:** world-anchored meters/labels that camera-follow can push off-screen.
**Do:** for every mission, list each meter/label players need mid-play, its anchor object, and
whether the camera (which follows the dogs, clamped to `ArenaBounds` by
`SharedCameraController`) can leave it off-screen during normal play. Fix load-bearing ones by
implementing CF1.2's `IMissionBeatProgressHud` (or `IMissionPressureHud` if it's pressure-shaped);
decorative ones just get a table row. Check `WorldLabelVisibility` proximity gating too — a label
that only appears near the object AND off-screen is doubly invisible.

**Camera mechanics confirmed (re-verified against current code, not assumed from the task title):**
`SharedCameraController` (`Assets/Scripts/Camera/SharedCameraController.cs`) follows the midpoint of
the two dogs and zooms to frame both plus a fixed margin (`horizontalMargin`/`verticalMargin`),
clamped between `minOrthoSize` (7.5) and an effective max derived from `levelBounds`
(`IsClampedToBounds`/`LevelBounds`, ~34-38 depending on aspect). `ArenaBootstrap.Start()` calls
`Configure(..., clamp: true, bounds)` with the SAME `Rect` `GameManager` exposes as `ArenaBounds`
(120x68 world units, `ArenaMissionTuning.BackyardWidth/Height`) — so the task doc's "clamped to
ArenaBounds" description is accurate; the camera's position is a function of where the two DOGS are,
never of any third fixed object. At the tightest zoom (dogs close together, `minOrthoSize` 7.5) the
visible half-extents are only ~7.5 units vertically / ~13.3 horizontally (16:9) around the dogs'
midpoint — comfortably smaller than several missions' station spread (e.g. LeashWalk's checkpoints
sit at the four corners of the full 120x68 arena). A world-anchored object that ISN'T one of the two
dogs, and isn't guaranteed to be near one of them, can absolutely fall outside that frame. That is
exactly Pee Break's pre-CF1.2 bug: the Teenager (a fixed third object) carried the ONLY copy of a
continuously-updating comprehension value, off in his own part of the room while the actual beat
work (door/leash/phone) happened elsewhere.

**Findings table** (all 23 controllers re-verified directly — file/line reads, not the pre-survey
assumption; `ObjectiveLabel` column confirms whether the same info the world-anchored cue carries is
ALSO in the always-on screen-space top bar, since `GameManager.BuildObjectiveLabel()` returns
`_activeMissionController.ObjectiveLabel` verbatim and `ArenaHud.DrawProductionGameplayHud` renders
it in the fixed 102px top bar regardless of camera position):

| Mission | Meter/label(s) players need mid-play | Anchor | Also in screen-space `ObjectiveLabel`? | Camera occlusion risk | Verdict |
|---|---|---|---|---|---|
| Backyard Rescue | TEAM TUG progress | `IMissionPressureHud` | N/A - already HUD | none (pre-existing) | **Pass** (already screen-space) |
| Baby Bird Bedlam | Chick "SHAKE n/N"/"GRAB IT", parent-bird dive state | Chick actor (dog must be holding/grabbing it) + parent bird near the one fixed nest | Yes (`chicks n/N, pecks n/N`) | Low - single-nest mission, chick label only matters while a dog is directly interacting with the chick | **Pass** - co-located + duplicated |
| Blanket Catch | Blanket taut/rip state, falling-item state | Blanket midpoint + falling item, both inherently between the two catching dogs | Yes (`caught n/N, rips n/N`) | Low - the catch mechanic itself requires the two dogs to be close together under the drop point | **Pass** |
| Bone Relay | Scent-post/mound "called" state | Relay mounds (Cocoa's heat-search targets) | Yes (`bones n/N, wasted n/N`) | Low - arrow/breadcrumb already routes Cheddar to the called mound; heat feedback reads at the dog, not a fixed object | **Pass** |
| Car Ride | SLIDE FORCE tilt | `IMissionPressureHud` | N/A - already HUD | none (pre-existing) | **Pass** |
| Chaos Machine | Lever + 3 junction labels ("i. ACTION"/"FIRED") | Junctions spread ~28 world units apart (`(-14,-7)`→`(14,7)`) | Yes (`junctions n/N, misfires n/N`); `IMissionRoleOwner` also drives the screen-independent `RoleTurnBeacon` at the live junction | Low - only the CURRENT junction matters, and the mechanic requires a dog to pre-position there before the cascade arrives | **Pass** |
| Coyotes Fence | Predator "COYOTE BREACH n/N", weak-spot "WEAK SPOT FILLED n/N" | Predator + squirrel(weak-spot) actors, both driven to the SAME active fence gap each round | Yes (`repairs n/N, breaches n/N`) | Low - Cocoa (bark-pin) and Cheddar (fill dirt) must both be at that one gap for their actions to register | **Pass** |
| Eagle Shadow Panic | "HIDES n/N"/"EXPOSURE n/N", rescue-window "GRIP CRACKED"/"TALON GRIP" | Predator (sweep) + squirrel-actor (talon rescue, fixed `_snatchPosition` near arena center) | Hides/exposures/pulls yes; the open/closed window flip is NOT echoed in `ObjectiveLabel` text | Structurally none - during the rescue beat the held dog is teleport-pinned to `_snatchPosition` and the free dog must be within 3.5u to act, so both dogs (and the badge) are forced together regardless of camera zoom | **Pass** - one un-echoed value, but geometrically impossible to occlude |
| Gate Crash | SQUEEZE THROUGH progress | `IMissionPressureHud` | N/A - already HUD | none (pre-existing) | **Pass** |
| Great Escape | 4 station labels ("i. Action"/"DONE") | Sequential stations, alternating Cocoa/Cheddar/Cocoa/Cheddar | Yes (`step n/N, botched n/N`); `IMissionRoleOwner` beacon | Low - same "only the current station matters, and a dog must already be there to act" pattern as Chaos Machine | **Pass** |
| Kitchen Food Frenzy | Counter ready/barked state, drop telegraph | Counter + safe-zone, ~13 units apart (single kitchen-scale room) | Yes (`caught n/N, combo`, finale count) | Low - well within even the tightest camera frame | **Pass** |
| Leash Walk | 4 corner "CHECKPOINT" markers + distance-signal badge | Checkpoints at the four corners of the full 120x68 arena | Yes (`reach checkpoint n/N`, `snaps n/N`) | The markers themselves are WAYPOINTS, not a progress meter - the shared arrow/breadcrumb system (already screen-independent-safe by design) is how players find them, and the count they need is in the HUD | **Pass** - wayfinding, not a meter; duplicated anyway |
| Mark The Yard | 5 zone "CLAIM" markers, squirrel re-mark state | 5 zones incl. all 4 yard corners | Yes (`n/N marked`, `steals n`) | Same wayfinding argument as Leash Walk | **Pass** |
| Operation Pee Break | Beat/comprehension progress, bladder pressure | `IMissionBeatProgressHud` + `IMissionPressureHud` (CF1.2, this task's reference fix) | N/A - already HUD | none (fixed) | **Pass** (reference implementation) |
| Scent Search | 6 dig-spot markers, HOT/WARM/COLD calls | Dig spots spread across the yard; heat text is a `SpawnWorldPop` **at the acting dog's own position**, not a fixed object | Yes (`bones n/N, cold digs n/N`) | Low - the transient heat pop is guaranteed on-camera because it spawns where a dog already is; the called-spot marker is the arrow's target | **Pass** |
| Snack Heist | Squirrel-thief state ("SNACK HEIST - BARK!") | The mobile squirrel actor dogs are actively chasing/guarding | Yes (`Stash snacks n/N`) | Low - it's the thing being chased, so it's wherever the chase currently is | **Pass** |
| Sock Panic | Basket hold/dive state | One laundry basket (single station) | Yes (`n/N returned`, `fumbles n`) | None - single-station mission | **Pass** |
| Squirrel Conspiracy | Squirrel route/taunt state, cutoff-zone label | Mobile squirrel actor + cutoff zones along its route | Yes (`Route n/N, controls n/N, taunts n/N`) | Low - same "chased actor" argument as Snack Heist | **Pass** |
| Squirrel Switcheroo | Decoy/stash zone labels | Decoy and stash ~20 units apart | Yes (`raids n/N, backfires n/N`) | Low - each zone only matters while the dog responsible for that sub-step is standing there | **Pass** |
| Table Stealth | STEAK SNEAK progress | `IMissionPressureHud` | N/A - already HUD | none (pre-existing) | **Pass** |
| Thunderstorm Comfort | PANIC meter | `IMissionPressureHud` | N/A - already HUD | none (pre-existing) | **Pass** |
| Walk Campaign | HUMAN GETS IT comprehension | `IMissionPressureHud` | N/A - already HUD | none (pre-existing) | **Pass** |
| Weenie Roundup | HOME BOWL "n/N" delivery counter, loose-marker labels | Fixed bowl (one corner) + 5 loose spots scattered across the yard | Yes (`ObjectiveLabel` carries `{Delivered}/{RequiredDeliveries}` in every branch) | The bowl's world label CAN scroll off-screen while a dog is out collecting a far loose weenie - but the exact same count is already in the top bar every frame | **Pass** - redundant, not load-bearing |

**Conclusion: no roster-wide fix required beyond CF1.2 (already shipped).** The audit's honest
result is a clean sweep, for a structural reason worth recording rather than a coincidence: every
other controller already funnels its numeric mid-play progress into `ObjectiveLabel`
(`GameManager.BuildObjectiveLabel()` → `_activeMissionController.ObjectiveLabel`), which
`ArenaHud.DrawProductionGameplayHud` renders in the fixed screen-space top bar every frame
regardless of camera position. World-anchored `SetActorState`/`AddWorldLabel` text on props and
actors is consistently a DIEGETIC ECHO of that same number, not its only copy - and reactive
one-shot feedback (`SpawnWorldPop`) fires at the acting dog's own position, which is on-camera by
construction since the camera follows the dogs. Pee Break's pre-fix bug was the one place a
continuously-updating value (comprehension) existed ONLY on a fixed third object (the Teenager) with
zero HUD echo, while the actual beat work happened at spatially separate stations (door/leash/phone).
No other controller has that shape: the 7 pre-existing `IMissionPressureHud` missions (Backyard
Rescue, Car Ride, Gate Crash, Table Stealth, Thunderstorm Comfort, Walk Campaign, plus Pee Break's
own pressure meter) already mirror their one continuous value to the HUD, and every other controller
re-verified above (16 controllers, full `ObjectiveLabel` property + station-geometry read, not a
grep-only pass) either has no continuous fill at all (only discrete counts, already duplicated) or
has action geometry that forces the dogs to stand where the cue lives. The one un-echoed boolean
found (Eagle Shadow Panic's rescue-window flip) is safe for a different, verified reason: the
mechanic itself teleport-pins both dogs to the same point while it's active, so camera occlusion is
geometrically impossible, not just unlikely.

Per the task's own explicit guidance ("DECORATIVE meters/labels... just get a table row, no fix
required" and "Don't force every mission to have a screen-space meter... Use judgment"), this audit
therefore ships as a doc-only commit: no new `IMissionBeatProgressHud`/`IMissionPressureHud`
implementations, because none of the 16 audited controllers has a genuine gap to close. Implementing
either interface on a controller that doesn't need it would itself violate the "optional interface,
don't add unnecessary surface area" rule stated in this same task's instructions. Full suite
reconfirmed green at 685/685 (unchanged - no behavior changed, so no new tests were warranted this
run; see the report for why this deviates from the "total > 685" default expectation).

**Follow-up (2026-07-21) - regression-guard test.** The audit's conclusion rests on a real invariant
(every mission's load-bearing progress counter is echoed into the screen-space `ObjectiveLabel`, so
camera-follow can never fully hide it) that nothing was previously asserting in code - a future
mission could silently regress it. `Assets/Tests/PlayMode/ObjectiveLabelProgressEchoPlayModeTests.cs`
encodes it directly: one `[UnityTest]` loops all 23 `GameManager.MissionVariant` values, starts each
mission, pokes one small deterministic step of progress through that controller's own existing
`Force*`/compat hook (reusing `GameManager.ControllerHooks.cs`'s forwarding methods - no new
plumbing), and asserts `ObjectiveLabel` is non-empty both before and after and that it visibly
changed. Getting this green surfaced two real hook-selection bugs worth recording as a caution for
future test authors on this codebase: (1) some controllers' `Tick()` recomputes hold/anchor state
from the (unrelated, still out-of-range) real dog positions every frame and will silently clobber a
forced value on the next frame - the fix was reading `ObjectiveLabel` synchronously right after the
poke with no intervening `yield return null`, not adding dog positioning; (2) a couple of Force hooks
resolve a puzzle step and immediately reset it within the same call (`SnackHeistMissionController.
ForceSteal()`; `SquirrelSwitcheroo`'s bait puzzle overbait-backfires if driven for a full second) -
picking the hook/parameter that leaves state changed, not just touched, mattered. 686 green
(685->686; one new test method covering all 23 missions in a single roster-wide loop), reproduced
clean on two consecutive runs.

### CF2.3 — Per-beat world-state progression
**Theme:** multi-phase missions should change something visible in the WORLD per phase — HUD
text alone didn't land.
**Do:** for each mission with sequential phases/beats (Pee Break, Great Escape, Chaos Machine,
Bone Relay, Table Stealth, Walk Campaign, Car Ride, Baby Bird Bedlam at minimum — derive the real
list from controllers), record what visibly changes in the world when a phase completes. CF1.4
(Teenager deltas) is the reference. Fix the worst 3-5 using existing art/props only — a prop
state change, an NPC posture shift, a lighting/sunbeam change. No new art in this task; table the
rest as future art asks.

**Findings table** (all 23 controllers read in full per trap #4; "sequential/multi-phase?" separates
missions with Pee-Break-shaped distinct beats or a repeated find/deliver/catch cycle from missions
that are one continuous puzzle with no phase boundaries at all):

| Mission | Sequential/multi-phase? | What already changes in the WORLD per phase | Verdict |
|---|---|---|---|
| Operation Pee Break | Yes — 4 distinct beats | CF1.4 (done): per-beat Teenager posture baseline + one-shot "NEXT!" pop | **Pass** (reference) |
| Great Escape | Yes — 4 distinct station actions, alternating owner | Each station's sprite/tint persists waiting→active→done (or fumble/settle flash), label flips to "DONE" | **Pass** |
| Chaos Machine | Yes — 3 distinct junction actions | Each junction's sprite/tint persists fired/stalled/active, lever sprite flips ready↔running | **Pass** |
| Leash Walk | Yes — 4 sequential checkpoints, alternating scout | Reached checkpoints get a permanent "Reached" sprite then deactivate; remaining checkpoints visibly shrink in count | **Pass** |
| Eagle Shadow Panic | Yes — hide phase → rescue phase → united-front phase | Predator's label/tint/position changes at every phase boundary (sweep→spotted→snatch-teleport→retreat); geometrically forces both dogs together during rescue | **Pass** |
| Squirrel Conspiracy | Yes — herd/control phase → stash phase | Squirrel prop's sprite permanently flips idle→stash-revealed→stash-cracked | **Pass** |
| Coyotes Fence | Yes — repeated gap-repair cycle across 5 gaps | Each gap's repaired/breached sprite is set once and PERSISTS (doesn't revert) — the fence itself shows the whole campaign's history | **Pass**, arguably the roster's best example |
| Scent Search | Yes — repeated dig cycle across 6 spots | Correctly-dug spots deactivate (a permanent "hole filled"); wrongly-dug spots keep a persistent "cold" sprite even after a new spot is chosen | **Pass** |
| Weenie Roundup | Yes — repeated pickup/deliver cycle + a final "jumbo" beat | Home bowl sprite persists empty→progress→full across the whole delivery count; final jumbo marker gets distinct art/label | **Pass** |
| Snack Heist | Repeated steal-guard cycle | Each plate's targeted/stashed/stolen sprite persists; the final snack gets an explicit "watched" gate distinct from the rest | **Pass** |
| Mark The Yard | Order-independent (not sequential, but multi-object) | Each of the 5 zones individually and permanently shows claimed/unclaimed/reclaimed | **Pass** |
| Squirrel Switcheroo | Repeated bait/raid cycle (single decoy+stash pair) | Decoy/stash sprites react per hit/whiff/backfire, but the reaction is TIMED (~0.55s) and reverts — no persistent trace of overall Hits/HitsNeeded in the world | Minor gap, tabled (see below) |
| Sock Panic | Repeated tip/dive cycle (single basket) | Basket open/closed state reacts per dive; no accumulation display (small ObjectiveGoal, low severity) | Minor gap, tabled |
| Blanket Catch | Repeated catch cycle (single blanket + falling item) | Blanket taut/slack/rip state and the falling item's caught/splat reaction are both TIMED per catch; no persistent "N caught" trace beyond HUD | Minor gap, tabled |
| **Bone Relay** | Repeated call/dig cycle across 4 mounds, 3 finds | Mound found/wrong sprite is DELIBERATELY timed (1s) and reverts, because the same mound can be re-called later (`CoopScentRelayPuzzle`'s random sequence allows repeats) — so nothing in the world shows overall Finds/FindsNeeded once the flash fades | **FIXED** (see below) |
| **Baby Bird Bedlam** | Repeated grab/shake/gulp cycle across 4 chicks | The nest and parent bird react moment-to-moment (dive/warning/attacking/rescue) but the nest itself looks identical at chick 1 and chick 4 — no world trace of the mission's overall ChicksEaten/ChicksNeeded arc | **FIXED** (see below) |
| **Car Ride** | Yes — 7-event scripted road sequence (turn/turn/brake…) | The dashboard reacts moment-to-moment per event (cruising/telegraph/turning/brake-settle) but never reflects how many of the 7 events are behind the ride — "getting closer to home" only ever existed as HUD text | **FIXED** (see below) |
| Table Stealth | No — one continuous distraction/sneak puzzle, not discrete beats | N/A (out of theme scope; already the CF2.2-reference `IMissionPressureHud` shape) | **N/A**, single-phase |
| Walk Campaign | No — one continuous comprehension puzzle | N/A | **N/A**, single-phase |
| Backyard Rescue, Gate Crash, Thunderstorm Comfort | No — one continuous pressure meter each | N/A | **N/A**, single-phase |
| Kitchen Food Frenzy | No — one continuous catch loop, no phase boundaries | N/A | **N/A**, single-phase |

**Why only 3 fixed, not 5:** per the task's own "don't over-invest equally" guidance, the honest
result is that most of the roster already does this well — 10 of the 23 controllers already carry
a PERSISTENT per-object/per-station world change through their whole sequence (that's the load-
bearing difference from Pee Break's original bug: a *persistent* trace, not just a momentary
reaction). Three repeated-single-prop missions (Squirrel Switcheroo, Sock Panic, Blanket Catch)
have the same "timed reaction, no persistent trace" shape as Bone Relay, but each centers on a
single shared prop rather than multiple discrete objects, so a good fix would need either new art
(a small accumulating tally prop) or restructuring the existing single-object reaction timing in a
way that risks fighting the puzzle's own reuse logic — tabled as **future art asks**, not fixed
badly. The three shipped fixes were chosen because they are the closest structural match to Pee
Break's actual bug (a value that matters for "how close to done" the mission is, that existed
NOWHERE in the world, only in HUD text) and each had an obvious, safe, existing-art-only fix using
one already-present, always-visible fixed prop (nest / dashboard / scent post).

**Fixes shipped (all presentation-only; puzzle mechanics, scoring, and reactive per-event feedback
are bit-for-bit unchanged):**

1. **Baby Bird Bedlam** (`BabyBirdBedlamMissionController.cs`) — the nest is the one fixed prop for
   the mission's whole ChicksEaten/ChicksNeeded arc (chicks, dogs, and the parent bird all move;
   the nest doesn't). New `NestDepletion` read-only property (0→1) drives `UpdateNestPresentation()`:
   the nest's existing shade-tree art overlay tints from white toward a washed-out "emptied" color
   as chicks are gulped, and its existing world label persistently reads
   `"THE NEST (n/4)"` instead of the static `"THE NEST"`. Called once from `StartMission()` (reset)
   and once from `HandleShake()` right after a chick is credited. Layered under, not replacing, the
   parent bird's existing per-dive reactive state machine.
2. **Car Ride** (`CarRideMissionController.cs`) — the dashboard is the one fixed prop for the whole
   7-event road script. New `RideProgress` read-only property (`EventsResolved`/`RequiredEvents`)
   feeds `SetDriverCalm()` (previously a hardcoded string+color every cruise phase): the calm-phase
   copy now reads `"DRIVER: cruising - N stops from home"` (or "almost home" on the last stretch),
   and its tint warms from the existing `DriverTint` toward a new `DriverAlmostHomeTint` — both via
   the same `_context.SetActorState` call already wired to the dashboard's `MissionActorFeedback`.
   Only the calm/cruise-phase presentation changed; the per-event telegraph/turn/brake-settle
   copy and tint are untouched.
3. **Bone Relay** (`BoneRelayMissionController.cs`) — mounds keep their existing 1-second timed
   found/wrong override (deliberately, since `CoopScentRelayPuzzle`'s random sequence can re-call
   the same mound), so nothing about that mechanic changed. Instead the scent post — the one fixed
   prop Cocoa always returns to — now carries a persistent `RelayProgress`-driven tint (its existing
   art overlay warms toward gold as `Finds`/`FindsNeeded` climbs) and its existing world label now
   persistently reads `"SCENT POST (n/3 FOUND)"` instead of the static `"SCENT POST"`, surviving
   past the per-mound override's revert.

**Tabled as future art asks (timed single-prop reaction, no persistent world trace — would need new
art or a puzzle-timing-sensitive restructure to fix well, not attempted here):**
- Squirrel Switcheroo — stash zone's raided flash fades; nothing shows overall Hits/HitsNeeded.
- Sock Panic — basket's open/closed flash doesn't accumulate (low severity, small goal count).
- Blanket Catch — blanket/falling-item reaction is per-catch only, no running "N caught" world trace.

**Tests:** three new deterministic PlayMode tests, one per fix, each stepping the mission through
its existing `Force*` hooks and asserting the new read-only presentation property plus the actual
rendered world state (label text and, for Baby Bird Bedlam, the authored art overlay's tint via
`ArtSpriteOverlay.CurrentTint`) moves — and, for Bone Relay, that the persistent scent-post tally
survives past the timed per-mound override's 1-second revert window (the exact trap this fix
closes). See `CoopBabyBirdBedlamPlayModeTests.cs`, `CarRidePlayModeTests.cs`,
`CoopBoneRelayPlayModeTests.cs`.

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

**Findings table** (grepped `MissionInstructionCatalog.cs` for `funny|hilarious|comedic|comedy|gag`,
cross-checked every `IntroPrompt` in `MissionCatalog.cs` the same way — zero hits there — and every
`### N.` level-idea-bank entry in `docs/GAME-DESIGN-BIBLE.md`, then filtered to claims that (a) are
about a FAILURE, not a success payoff, and (b) are tied to one of the 23 shipped
`*MissionController.cs` files, not an unbuilt idea-bank entry):

| Claim found | Location | Failure or success? | Shipped mission? | Verdict |
|---|---|---|---|---|
| "One signal held alone eventually makes the human fetch a funny wrong item." | `MissionInstructionCatalog.HowToPlayStepsFor`, WalkCampaign step | Failure (misread) | Yes — `WalkCampaignMissionController.cs` | **FAIL → FIXED.** Traced the misread branch (`HandleProgress()`, then ~lines 280-312): pre-fix it only did `AddScore` (penalty), `SetCue`/`SetJuice`/`SpawnWorldPop` (text), a `SetHumanState` sprite/label swap, and `RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning)` — the SAME generic cue nearly every other mission's warning-miss/breach/exposure moment already uses (confirmed via `grep -rn "ArenaFeedbackCatalog.ThreatWarning"` — 15+ other call sites across Coyotes Fence, Eagle Shadow Panic, Car Ride, Chaos Machine, etc., all serious "uh oh" moments, none comedic). A label/sprite swap plus the roster's stock danger cue does not read as "funny" to a couch-test player any more than Pee Break's pre-CF1.3 misread did. Fixed below. |
| "Barking too early just gets misread with a funny gag — it doesn't fail the run." | `MissionInstructionCatalog.HowToPlayStepsFor`, OperationPeeBreak step | Failure (misread) | Yes — `PeeBreakMissionController.cs` | **Pass (already shipped).** This is exactly the claim CF1.3 fixed (`TriggerMisreadGag`, shipped commit history above) — confirmed the shipped code still does the offer-tween/distinct-cue/dog-nudge/escalation described in the CF1.3 status-board row. Nothing to re-fix; explicitly out of this task's scope per the queue's own framing. |
| "Secure the steak before four exposures; the stolen-steak gag holds in the live world before the result card." | `MissionInstructionCatalog.HowToPlayStepsFor`, TableStealth step | **Success payoff**, not failure | Yes — `TableStealthMissionController.cs` | **Out of scope.** This describes the completed-objective payoff ("the stolen-steak gag holds... before the result card"), not a promise that a MISTAKE is funny — the theme and CF1.3's reference are specifically about the failure/misread case. Not audited further per the task's own explicit carve-out for this exact line. |
| "Boss gag: fake snack lure that Cheddar absolutely believes." | `docs/GAME-DESIGN-BIBLE.md` level-idea-bank entry "3. Coyotes at the Fence" | Ambiguous — reads like flavor, not a promised failure reaction | Yes, the mission is shipped (`CoyotesFenceMissionController.cs`) and the lure IS implemented (`_state.FakeSnackActive`, `TriggerFakeSnack()`) | **Out of scope — no failure branch exists to gag on.** Traced `PatrolDefenseMissionState.cs` and the controller in full: the fake-snack beat only ever RESOLVES via the same bark-pin action that handles the coyote (`TryPin()` → `_state.ResolveFakeSnack()`); there is no timeout, no "Cheddar actually eats it" branch, and no separate failure state tied to it at all (a dead `PlayerStatsAccumulator.RecordFakeSnackEaten()`/`FakeSnacksEaten` stat exists but is never called anywhere — confirmed via `grep -rn "RecordFakeSnackEaten"`, zero call sites). The bible line is flavor text about the temptation in the original idea-bank entry, not a shipped "if you fail here, it's funny" promise with a silent reaction to fix. |
| "Multi-stage comedic horror" (Vet Appointment: The Betrayal, idea #12); "Comedy movement level" (Halloween Costume Escape, idea #19) | `docs/GAME-DESIGN-BIBLE.md` | N/A | **No** — neither has a shipped controller (`ls *MissionController.cs` confirms no VetAppointment/HalloweenCostume file) | **Out of scope, per `docs/README.md`'s DEFERRED section and the task's own instruction** — these are unbuilt idea-bank entries, not live mission behavior to audit. |
| General design-principle mentions of "funny failure"/"comedy" (CLAUDE.md-style build philosophy, `docs/GAME-DESIGN-BIBLE.md` lines ~25, 37, 44-45, 58, 70, 84, 109, 154, 159, 177, 602, 615) | `docs/GAME-DESIGN-BIBLE.md` | N/A | N/A | **Out of scope** — roster-wide design doctrine, not a claim about one mission's specific failure branch. |

**Conclusion: exactly ONE genuine silent-comedy gap found, not three.** Per the task's own explicit
guidance ("if fewer than three spots genuinely need fixing, say so explicitly rather than forcing
three"), the honest result of this sweep is one fix, not three. Every other comedy claim tied to a
shipped mission's failure branch was either already fixed (Pee Break, by CF1.3), describes a success
payoff rather than a failure (Table Stealth), or has no actual failure branch to gag on in the first
place (Coyotes Fence's fake-snack flavor text) or no shipped mission at all (Vet Appointment,
Halloween Costume Escape — both still in the idea bank).

**Fix shipped — WalkCampaign's misread now has a real gag
(`WalkCampaignMissionController.cs`).** Presentation-only, reusing the CF1.3 gag-template shape:
- **State + timer:** `_misreadGagT` (0→1 offer progress) and `_misreadEscalated` (bool), both reset
  in `StartMission()` (trap #7).
- **Trigger method:** `TriggerMisreadGag(bool escalated)` — called from the misread branch in
  `HandleProgress()` BEFORE the existing `RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning)` call,
  so `ThreatWarning` stays the LAST cue requested and the pre-existing pinned assertion
  (`Walk_SingleMisread_CoachesRecoverably_AndTheCorrectComboStillWorks`'s
  `Assert.AreEqual(ArenaFeedbackCatalog.ThreatWarning, _game.LastAudioCueRequested, ...)`) is
  unchanged. Fires a distinct `ArenaFeedbackCatalog.SquirrelStunned` cue (the same one CF1.3 picked
  for Pee Break's misread — an existing, authored, non-invented cue per trap #9) and nudges both
  dogs via the existing `DogReadabilityFeedback.ShowGuidanceNudge` (no new art).
- **Tick/ease branch:** for a recoverable (non-final) misread, `Tick()` eases `_misreadGagT` from 0
  toward 1 over `MisreadGagSeconds` (0.4f) while the existing post-misread reaction window
  (`_humanReactionUntil`) is live; `UpdateLabels()` composes the resulting `MisreadGagOffset()` onto
  the human's position so it visibly leans toward wherever the dogs currently are, holding out the
  wrong item, then snaps back to rest when the window closes and the sprite reverts to
  CONFUSED/GETTING IT.
- **Escalation:** the third, mission-ending misread snaps `_misreadGagT` straight to 1 (full offer)
  inside `TriggerMisreadGag` itself, rather than easing — `Tick()` stops calling `UpdateLabels()` the
  instant `_failed` is set that same frame, so there are no further frames to ease through; this
  reads as a held freeze-frame at the game-over beat instead of a cut-off tween.
- **Why `SquirrelStunned` and not a new cue:** it's the exact cue CF1.3 already established roster-wide
  as "the funny stunned reaction," distinct from every mission's generic `ThreatWarning`/`ScorePenalty`
  danger cues (confirmed both resolve to real clips in `AuthoredAudioCatalog`, trap #9) — reusing it
  keeps one consistent "that was a gag, not a real failure" audio signature instead of inventing a
  second one for a single mission.

**Tests:** two new deterministic PlayMode tests in `CoopWalkCampaignPlayModeTests.cs`, mirroring
CF1.3's two-assertion shape:
1. `Walk_SingleMisread_TriggersComedicGagButStaysQuietBeforehand` — negative case (holding the
   correct combo, no misread yet) proves the gag stays quiet (`MisreadGagProgress == 0`, no
   `SquirrelStunned` cue requested); positive case (one misread) proves `SquirrelStunned` fired,
   `ThreatWarning` is still the last cue requested (mechanics/audio-ordering unchanged), both dogs'
   `FacingIntentLabel` turned toward the human from opposite sides, and (after one real frame) the
   offer progress is strictly between 0 and 1 — a lerp, not a teleport — without that settling frame
   causing a second misread.
2. `Walk_ThirdMisread_EscalatesGagWithoutChangingFailMechanics` — drives all three misreads, asserts
   the first two do NOT escalate, the third does (`MisreadEscalated == true`,
   `MisreadGagProgress == 1`), and that `Outcome`/`Phase`/`EndSummaryLabel` (the fail mechanics the
   pre-existing `Walk_FailPath_TooManyMixedSignals` test already pins) are bit-for-bit unchanged.

Full suite green at 691/691 (689→691, 2 new tests), no flaky-test reruns needed this pass.

### CF2.5 — Carried-object visuals
**Theme:** logically-held objects rendered as static/floating break the fantasy.
**Do:** per mission, list every object a dog logically holds/drags mid-mechanic (leash, weenies,
bones, socks, blanket corners, rope, chicks…) and how it renders during the carry. Weenie
Roundup's `_carriedMarkers` and CF1.7's leash are the reference idiom (per-frame follow, never
SetParent — trap #5). Fix gaps where an object visibly teleports or floats unheld; sway/dangle
polish is a bonus, not required.

**Findings table** (all 23 controllers read in full per trap #4; grepped every `*MissionController.cs`
for pickup/carry/drag/deliver/hold verbs, then hand-traced each hit to see whether it's an actual
"dog carries a prop through space" mechanic or a same-spot interact/dig/collect that only looks like
one from the label text):

| Mission | Carried object? | Current render during the carry | Verdict |
|---|---|---|---|
| Operation Pee Break | Leash, in Cheddar's mouth while presenting | `_leashArt` follows Cheddar's muzzle via `ResolveLeashArtPosition` (CF1.7, shipped reference) | **Pass** (reference) |
| Weenie Roundup | Loose weenies, carried to the home bowl | `_carriedMarkers[i].transform.position = dog.position + Vector3.up*0.6f` every tick while `_dogCarrying[i]` (`Tick()` line ~124); standalone objects, never parented (reference idiom) | **Pass** (reference) |
| Baby Bird Bedlam | A grabbed chick, held in Cheddar's mouth while he shakes it down | `TickHeldChick()` sets `_chickX/_chickY` to Cheddar's position + a snout offset every tick while `ChickState.Held`; `UpdateChickVisuals()` applies that to `_chickObj.transform.position` — already the correct per-tick-follow idiom, independently arrived at before this audit | **Pass** (already correct) |
| Blanket Catch | The blanket, held taut between both dogs' hands | `_blanketObj.transform.position` = the live midpoint of both dogs' X positions every tick (`_puzzle.MidpointX`), width = their live separation — a two-dog dynamic stretch, not a single-dog carry, but the same "tracks the actual holder(s), never a fixed spot" principle | **Pass** |
| Walk Campaign | Leash, presented toward the human while Cheddar stands at the leash station | **Pre-fix:** `_leash.transform.position = _leashZone` unconditionally, every frame, regardless of whether `_leashPresented` was true — the leash visual (and its label) sat dead at the fixed station the whole mission, the same static-prop gap CF1.7 fixed for Pee Break | **FAIL → FIXED** (see below) |
| Bone Relay | Bones, found in the ground at fixed mounds | Bones are found-in-place (dig, credit, done) — never picked up and walked anywhere; no carry phase exists to render | **N/A** — not a carry mechanic |
| Sock Panic, Snack Heist, Kitchen Food Frenzy | Socks / snacks / plates, collected by a dog | All route through the shared `Treat`/`HandleTreatCollected` flow: touch → instantly scored → `RecoverCollectible` (hidden/consumed same frame) — there is no in-between "carrying it somewhere" state to render | **N/A** — instant collect, not carry |
| Great Escape, Chaos Machine | Station/lever/junction actions | Fixed-position sequential interactions; nothing is picked up or moved by a dog | **N/A** |
| Squirrel Switcheroo, Squirrel Conspiracy | Decoy/stash/route props | Fixed zones the dogs act on in place (bark-bait, interact-raid); no object ever leaves its zone | **N/A** |
| Eagle Shadow Panic | Cheddar himself, snatched by the eagle | `TickRescue()` pins Cheddar's own transform to the fixed `_snatchPosition` every tick while held (not a prop a dog carries — the DOG is what's held, by the eagle, in the air above one spot). Deliberately static-in-place per the mechanic (both dogs must converge there); not the "object floats away from where it's held" bug this theme targets | **N/A** — different mechanic shape, not a dog-carries-object case |
| Leash Walk | (no physical leash prop) | The "leash" here is an abstract max-distance constraint between the two dogs (`MaxLeash`), not a rendered object | **N/A** |
| Bone Relay's scent post, Mark The Yard's zones, Scent Search's dig spots, Coyotes Fence's gaps, Table Stealth, Thunderstorm Comfort, Car Ride, Gate Crash, Backyard Rescue | No dog-carried prop | Reviewed for completeness; all are fixed-station interactions, pressure meters, or (Backyard Rescue) a squirrel — not a dog — moving a `Treat` toward itself | **N/A** |

**Conclusion: one genuine gap found, not a roster-wide pattern.** Per the task's own "don't force
fixes where none are needed" guidance: Weenie Roundup, Baby Bird Bedlam, Blanket Catch, and Pee Break
already implement the reference idiom correctly (two of them — Weenie Roundup and Baby Bird Bedlam —
arrived at it independently of this audit, confirming it's the codebase's natural default once a
mission actually has a carry mechanic). The other 18 controllers have no dog-carries-an-object beat
at all — their objects are either fixed-station interactions, instant-collect treats, or (Eagle
Shadow Panic) a dog being held rather than holding something — so there is nothing to fix there.

**Fix shipped — Walk Campaign's leash now follows Cheddar while presenting
(`WalkCampaignMissionController.cs`).** Presentation-only; the `_leashPresented`/`_leashZone`
mechanics, `StationRange` gating, and puzzle state are bit-for-bit unchanged. Added
`ResolveLeashPosition()`: while `_leashPresented` is true, returns Cheddar's live position plus a
snout offset along his facing direction (`CheddarFacingDirection`, mirroring CF1.7's
`DogReadabilityFeedback.FacingDirection` read); otherwise returns the fixed `_leashZone` rest
position. `UpdateLabels()`'s one line (`_leash.transform.position = _leashZone;`) now calls this
instead — a standalone position assignment recomputed every call, never a `SetParent` (trap #5),
identical in shape to `WeenieRoundupMissionController._carriedMarkers` and Pee Break's
`ResolveLeashArtPosition`. `_leash.transform` also carries the "LEASH" label and interaction badge,
so both now visually travel with the presented leash too — arguably an improvement over Pee Break's
own split (where the label stays at the station while only the separate generated-art sprite moves).
No sway/dangle added (explicitly a bonus per the task, not required; tabled as future polish).

**Test:** `Walk_LeashVisualFollowsCheddarWhilePresenting_AndRestsAtStationOtherwise` in
`CoopWalkCampaignPlayModeTests.cs`, driven through the real position + `Interact()` dispatch path
(not `ForceWalkCampaign`, which is a puzzle-level-only shortcut that never sets `_leashPresented` —
proven necessary by checking `ForceWalkCampaign`'s body, which never touches that field). Asserts:
(1) before Cheddar engages, the leash sits exactly at `WalkLeashZone`; (2) once Cheddar interacts
from an off-center spot within `StationRange`, the leash visibly moves to within ~1 unit of his
position and away from the old fixed spot; (3) once he walks the leash far out of range, `Tick()`'s
existing `_leashPresented && !cheddarAtLeash` reset drops the flag and the leash snaps back to rest
exactly at `WalkLeashZone`.

### CF2.6 — Wall-station perspective
**Theme:** interaction anchors centered on tall wall-drawn art make dogs look like they're
climbing.
**Do:** per mission, list every interaction point whose art is drawn vertically (doors, gates,
fences, counters, tables, windows) and check whether the dog's required standing position
overlaps the art's upper body. CF1.6's floor-anchor pattern is the fix. Prioritize any station a
dog must HOLD (sustained overlap reads worst); table brief-touch stations.

**Survey method (matches the task's own instruction):** grepped every `*MissionController.cs` for
`NewMarker`/`NewScenery`/`localScale =` calls whose `Vector3` scale has Y meaningfully larger than
X (the signature of a marker drawn tall against a back wall, per CF1.6's door - scale (2.4, 4, 1)).
Confirmed via `MissionPropArtAttachment`/`ArtSpriteOverlay.Init` (`ActualArtOverlay` is a CHILD of
the marker and inherits its non-uniform `lossyScale`) that this scale genuinely dictates on-screen
tallness regardless of the source PNG's own aspect ratio - every `FinalGameplayArt` PNG actually
shipped is roughly square-to-landscape (checked all of them, tallest is 1.23:1), so a marker's own
scale is the ONLY source of "drawn tall" in this codebase. Also confirmed the four shared actors
(`ArenaArtCatalog.ActorKind.Squirrel/Predator/Rope/LaundryBasket`, used by Sock Panic, Snack Heist,
Coyotes Fence's predator, Eagle Shadow Panic's predator, etc.) all use `ActorVisualSlot`'s single
uniform `rootScale` float, not an asymmetric `Vector3` - so no shared actor can produce this bug
class by construction, and Sock Panic's laundry basket (a named candidate in this task) is
confirmed NOT tall (uniform scale 1.1).

**Findings table** (every `NewMarker`/`NewScenery`/`localScale` call across all 23 controllers with
Y >= 1.8 and Y >= 1.3x X; Pee Break's own door/window/charger-cord markers are the already-fixed
CF1.6 reference and excluded below):

| Mission / marker | Scale (X, Y) | Is it architecture (door/gate/fence/counter/table/window)? | Station-check math | HOLD or brief-touch? | Verdict |
|---|---|---|---|---|---|
| Gate Crash — `GateCrashGate` | (1.4, 4) | **Yes** - the mission's namesake gate | Anchor-engage (`HandleInteract`) and the sustained hold (`Tick`'s `anchorInRange`) both measured distance to `_holdZone`, the gate marker's own tall-center position; `TryGetObjectiveTarget` pointed Cocoa's arrow at `_gate.transform` (same point) | **HOLD** - Cocoa must stay anchored the entire squeeze, the worst-case "sustained overlap" shape this task calls out | **FAIL → FIXED** (see below) |
| Coyotes Fence — `FenceGap_i` | (1.2, 2.4) | **Yes** - fence weak-spot markers | `TryRepair`'s distance check measures to `_activeGapPosition`, the SAME point used for the tall gap marker's position; margin is tighter than Gate Crash's (repair range 2f vs a ~1.49-unit floor offset at the same -0.62 ratio CF1.6/this task use = only ~0.51 units of slack, versus Gate Crash's ~1.52) | **Brief-touch** - `TryRepair` is a single instantaneous `Interact()` credit, not a sustained hold (the mission's actual HOLD mechanic, bark-pinning the coyote, is anchored to the mobile predator actor's own position, not the fence) | **Tabled** - genuinely tighter margin than Gate Crash, worth a future look, but per the task's own explicit prioritization ("table brief-touch stations") this is lower priority than a HOLD station and was not fixed this pass |
| Squirrel Switcheroo — `SwitcherooDecoy` | (1.6, 3) | **No** - a squirrel decoy/lure prop, not wall-drawn set-dressing | `BaitRange` = 4f vs a ~1.86-unit floor offset - generous slack (~2.14 units), and it is a free-standing yard prop rather than something drawn flush against a wall | HOLD (baiter must sustain proximity), but excluded on the architecture criterion, not the hold/brief-touch one | **N/A** - out of this theme's scope (matches the "doors, gates, fences, counters, tables, windows" list; not a wall element) |
| Table Stealth — `TableStealthHuman` | (1.6, 4) | **No** - a standing human character actor (the mission's "HUMAN"), not architecture | `DistractRange` = 4f vs a ~2.48-unit floor offset - generous slack (~1.52 units) | HOLD (Cocoa must sustain the distraction), but excluded on the architecture criterion | **N/A** - a bipedal character sprite is expected to be tall (body proportions), the same reason Pee Break's own Teenager character was correctly left out of CF1.6's fix; approaching a character at roughly chest height reads as "standing near a person," not "climbing a wall" |
| Walk Campaign — `WalkCampaignHuman` | (1.8, 3.4) | **No** - same as Table Stealth's human | N/A - not a required standing/interaction anchor at all (the human is the puzzle's signal-reading target, not a station dogs must reach) | N/A | **N/A** - same character-actor exclusion as Table Stealth |
| All other 21 controllers | — | — | Read every `NewMarker`/`NewScenery`/`localScale` call in each file (trap #4); Great Escape, Chaos Machine, Kitchen Food Frenzy, Backyard Rescue, Eagle Shadow Panic use either `FinalGameplayArt`-only stations with near-square placeholder scale (1.7×1.3 or narrower), a wide flat range marker (Kitchen's counter, 2.4x radius × 1.1), or uniform-scale radius markers (Backyard Rescue's trap gap, Eagle Shadow Panic's cover circles - both `Vector3.one * radius`) | N/A | N/A | **Pass** - no tall (Y >> X) marker exists in these controllers at all, so there is no "climbing" geometry to check |

**Conclusion: one genuine HOLD-type gap found and fixed, one tighter-margin brief-touch gap
tabled, and three tall-but-non-architectural markers correctly excluded on the theme's own
criterion.** Per the task's explicit prioritization ("prioritize stations a dog must HOLD... table
brief-touch stations"), Gate Crash's gate - the mission literally named after a gate, flagged
"high risk" by this task's own candidate list - was the one worth fixing this pass. Unlike Pee
Break's door (where `StationRange` 2.25 was smaller than the ~2.48-unit gap, making the true floor
spot mechanically *unreachable*), Gate Crash's `HoldRange` (4f) was always generous enough that a
dog standing at the true floor spot would already satisfy the old center-anchored check - so this
was not a "mechanically impossible" bug like Pee Break's, but a **guidance/consistency** one: the
arrow/breadcrumb (`TryGetObjectiveTarget`) pointed straight at the gate's own tall-center
transform, pulling Cocoa toward the halfway-up-the-gate spot that produces the couch-test-style
"climbing" read, even though the hold check itself would have tolerated her standing at the base.
That distinction is recorded honestly in the fix and its test below, not glossed over.

**Fix shipped — Gate Crash's anchor-engage/hold now grounds to the gate's floor
(`GateCrashMissionController.cs`).** Presentation/guidance-only; `HoldRange`, `CrossRange`, the
puzzle's snap/cross mechanics, and the gate ART's own position (`_gate`, `_holdZone`) are
bit-for-bit unchanged. Added `_gateFloorAnchor`: a bare child `GameObject` (no `SpriteRenderer` -
no new art) parented to `_gate` at local Y -0.62, the identical ratio Pee Break's doormat uses
(CF1.6), so its world position sits ~2.48 units below the gate's own tall center at the gate's
visual base. `GateFloorAnchor` (a new read-only `Vector2` property) is now the anchor for: (1) the
sustained hold check in `Tick()` (`anchorInRange`), (2) the initial engage-distance check in
`HandleInteract()`, and (3) Cocoa's `TryGetObjectiveTarget` arrow/breadcrumb (now points at
`_gateFloorAnchor.transform` instead of `_gate.transform`). A new `UpdateGateAnchorPose()` (called
from `Tick()` whenever the anchor is actually held) gives Cocoa a grounded "holding the gate" read
via the existing `DogReadabilityFeedback.ShowGuidanceNudge` - the same idiom CF1.6 used for the
door stare, refreshed every tick so its 0.7s forced-pose window never lapses mid-hold, no new art.
The cosmetic "door outrage" gag (`TrySpawnDoorOutrage`, Cheddar's running "every closed door is a
personal attack" joke) deliberately keeps checking `_holdZone` unchanged - it is flavor, not a
required station, and its existing shipped test places Cheddar exactly at `_holdZone` with a
tighter 2.2-unit radius that retargeting would have broken for no required-behavior gain.

**Test:** `GateCrash_AnchorGuidanceAndHoldAnchorToTheFloorNotTheTallGateCenter` in
`CoopGateCrashPlayModeTests.cs`. Asserts (1) the floor anchor sits >2 units below the gate's own
center (the CF1.6-style "meaningful gap" proof); (2) Cocoa's objective-target position now equals
`GateFloorAnchor` and is no longer equal to `GateHoldZone` - the two assertions that actually fail
against the pre-fix code, since the arrow previously pointed exactly at `GateHoldZone`; (3)
engaging and sustaining the hold from the floor anchor itself (through real `Interact()` dispatch,
trap #2) succeeds. Full suite green at 693/693 (692→693, 1 new test).

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
