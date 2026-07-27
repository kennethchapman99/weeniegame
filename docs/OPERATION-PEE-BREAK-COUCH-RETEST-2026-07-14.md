# Operation Pee Break Couch Retest — 2026-07-14

> **Status: NOT RUN — NO VERDICT.** This sheet is for the required two-human, two-controller cold
> retest of the response build. Automated checks and a prepared form are not acceptance evidence.

## Locked preflight evidence

- Player: `unity/builds/dev/CheddarAndCocoa-Arena.app`
- Player executable built: 2026-07-27 with Unity 6000.4.2f1 from source commit
  `a9cbd39c1fbb062c12bbb8ca017da0578e2f6ce3`
- Player executable SHA-256: `299573912f545c6459b0dd78dd0d072d2193d72ed91cf771ea1cb35ccad50a98`
- Packaged explainer SHA-256: `280de2f463f2f64d337937a16673c549a970e99a6d1c2bb3774a05b34d002e63` (unchanged
  since 2026-07-16 — the video asset itself was not touched by the couch-fix queue either;
  re-verified by rehashing `Assets/StreamingAssets/OperationPeeBreak/operation_pee_break_intro.mp4`
  directly during this refresh)
- Full PlayMode result: 822/822 passed, 0 failed, 0 skipped, 2026-07-27. The current run completed
  cleanly after moving aside a stale generated Bee cache map that blocked Unity before compilation;
  no source or test change was required. The earlier 698-test couch-fix baseline was also reproduced
  cleanly on **three consecutive runs** because it verified the flaky-test fix documented below.
- PlayMode result: `unity/playmode-results.xml`
- PlayMode result SHA-256: `32f3c06f6f58218b2ba08ef064e54feb66350a62e5f55af1f957291c3c7044a1`
- Packaged-player startup smoke: passed against this rebuild
- **This build now also includes the full 2026-07-20..07-21 couch-fix queue**
  (`docs/AGENT-WORK-QUEUE-COUCHFIX.md`, all rows DONE, status header COMPLETE), which answers the
  2026-07-20 couch playtest (`docs/COUCH-PLAYTEST-2026-07-20-PEE-BREAK.md`, gate PARTIAL) on top of
  the 2026-07-19 pre-launch-queue build this sheet previously referenced:
  - **Phase CF1 — nine Operation Pee Break fixes**, one per playtest finding: the control/keyboard
    card no longer auto-dismisses on a timer, it waits for either player's bark/interact and the
    mission stays frozen the entire time (CF1.1); the beat-progress ("TEENAGER GETS IT") meter is
    now also mirrored into the always-on screen-space HUD so it can't scroll off with the camera
    (CF1.2); an early/misread signal now gets a real physical-comedy beat — the Teenager half-rises
    and offers the wrong item toward the dogs with a distinct cue and both dogs reacting, escalating
    after 3+ misreads in a beat (CF1.3); the Teenager's posture/props now visibly shift per beat plus
    a one-shot "NEXT!" transition pop, so advancing reads as a moment, not just a new steady state
    (CF1.4); Beat 3's simultaneous role flip now fires an on-screen banner, a named "NEW JOB" pop at
    each dog, and both dogs' own handoff chimes (CF1.5); the door-stare station now anchors to the
    doormat's floor position instead of the door art's tall wall-center, so the correct stare spot no
    longer reads as "climbing the door" (CF1.6); the leash now visibly follows Cheddar's muzzle while
    he's presenting it instead of sitting static at its station (CF1.7); both optional toys now get a
    one-shot discovery pop plus a periodic idle wobble so players actually notice them, with zero
    change to their "never affects mission state" design intent (CF1.8); and the mission-select
    detail cover crops less of the tile art, plus the "YOUR TEAM PLAN" panel now shows small icon
    chips (door/leash/charger/bark-burst) next to its text bullets (CF1.9).
  - **Phase CF2 — eight roster-wide theme audits**, each checking whether the same failure class
    Pee Break had exists anywhere else in the other 22 missions, fixing genuine gaps only: the F4.1
    first-mission control-strip reminder is now re-summonable from the pause menu even after it's
    been skipped/timed out/fully used, closing the one real "no way back" gap found roster-wide
    (CF2.1); a roster-wide audit of HUD/meter camera occlusion found no other mission has Pee
    Break's pre-fix shape (every other controller already echoes its progress into the always-on
    top bar), so this shipped as a clean-sweep doc-only pass plus one new regression-guard test that
    loops all 23 missions (CF2.2); three missions whose only progress signal lived in HUD text —
    Baby Bird Bedlam's nest, Car Ride's dashboard, Bone Relay's scent post — now also carry a
    persistent per-mission-progress world-prop tint/label (CF2.3); Walk Campaign's misread (the one
    other mission whose briefing promised a "funny" wrong-guess failure with none shipped) now gets
    the same comedic gag treatment as CF1.3 (CF2.4); Walk Campaign's leash (the one other
    dog-carries-an-object gap found) now follows Cheddar the same way Pee Break's does (CF2.5);
    Gate Crash's gate — the one other HOLD-type wall station with the same "climbing" geometry —
    now anchors its hold-check and Cocoa's guidance arrow to the gate's floor base (CF2.6);
    Backyard Rescue's pool splash/shake moments (the one other briefing beat with zero on-screen
    echo) now pop "SPLASH! SWIMMING!"/"SHAKE OFF!" text matching the briefing's own wording (CF2.7);
    and all 23 missions' title cards were re-verified under the CF1.9 crop math (uniform ~34%
    visible band roster-wide, no per-mission bias needed) with team-plan icon chips authored for
    21 of the other 22 missions — Backyard Rescue stays the one text-only holdout because its
    bullets don't have enough distinct matching sprites (CF2.8).
  - **CF3.1 (this evidence refresh)** additionally fixed a known low-frequency flaky test —
    `PeeBreakPlayModeTests.BeatTransitionFiresOneShotWorldPopAndOhBubbleDistinctFromDoorOpenClimax`
    was waiting only 1.3 real seconds for a 1.05-second-lifetime world pop to expire (~0.25s/24%
    buffer); the wait was widened to 2.5 real seconds (~1.45s/138% buffer). Only the real-time
    margin changed — the assertions, the mechanic under test, and what it proves are untouched. The
    fix was verified with three consecutive full-suite runs, all 698/698 green.
  - None of this changed Pee Break's own beats, tuning, scoring, or fail conditions — every change
    above is presentation/guidance-only. The checklist below has been rewritten specifically to
    watch for whether these fixes read correctly to a live human pair, not just whether the old
    complaints are gone.
- Human players: pending
- Physical controllers: pending
- Shared display / couch distance: pending
- Observer: pending
- Session start/end: pending

If the executable or project is rebuilt before the session, replace the hashes and rerun the full
PlayMode suite before treating this sheet as evidence for that build.

## Cold-run rules

- Launch the packaged development player, not Play Mode in the Unity Editor.
- Start from cold mission select and use **Play Recommended**.
- Keep F1 diagnostics off during the cold read. Use F4 on the first spontaneous “what do I do?”
  question, then record the exact moment and quote below.
- Give no unsolicited mechanic coaching. Record every hint, including wording and beat.
- Keep the laugh/quote log live; do not reconstruct it after the session.
- Complete one replay far enough to verify reset behavior.

## Required observations

Use `PASS`, `FAIL`, or `NOT SEEN`. A pass requires a direct human/runtime observation.

| # | Requirement | Result | Human evidence / exact observation |
| --- | --- | --- | --- |
| 1 | Two physical controllers remain independently bound to Cheddar and Cocoa |  |  |
| 2 | Ten-second opening explainer plays before control card |  |  |
| 3 | Either player can skip the explainer; skip is understood |  |  |
| 4 | The control card waits for a deliberate bark/interact press — it does **not** disappear on its own after a few seconds |  |  |
| 5 | Mission time and dog movement stay completely frozen the entire time the control card is up, until that press |  |  |
| 6 | The door-stare station reads as sitting/standing at the door's floor level — dogs do not appear to climb or lie on the door art |  |  |
| 7 | Large station/debug circles remain hidden with F1 off |  |  |
| 8 | The beat-progress meter stays visible on screen even when the dogs move to the bottom of the play area, **and** the Teenager visibly looks/sits differently by Beat 3-4 than Beat 1 |  |  |
| 9 | BLADDER EMERGENCY reads as a number-free visible meter |  |  |
| 10 | The toys are noticeably discoverable (a pop the first time a dog gets close, a periodic idle wobble) and still never affect mission progress |  |  |
| 11 | A misread produces a visible/audible comedic beat, not just a warning sound — you should laugh or smile at least once at a misread |  |  |
| 12 | The exact moment jobs swap at Beat 3 is called out on screen (both dogs' new jobs named) — you should be able to point to the moment it happened, not just infer it after finishing |  |  |
| 13 | The leash visibly moves with / is held by Cheddar while he's presenting it, rather than sitting static at one spot |  |  |
| 14 | Open-door/hydrant payoff remains visible before the end card |  |  |
| 15 | Earned success does not time out during the payoff hold |  |  |
| 16 | Replay resets beat, bladder, phone/battery, misreads, toys, and door state |  |  |

### Additional watch-fors from the CF2 roster audits

Worth a human eyeball this same session even though the underlying fixes are roster-wide, not
Pee-Break-only — both are things a normal single-mission session will naturally pass through
(mission select, and pause):

- **Mission-select detail cover + team plan (CF1.9/CF2.8).** Before launching, glance at Operation
  Pee Break's tile in mission select: the detail cover should show noticeably more of the artwork
  than before (less top/bottom crop), and the "YOUR TEAM PLAN" panel should show small icon chips
  (door / leash / charger / bark-burst) next to its text bullets, not text alone.
- **Control-strip reminder is re-summonable from pause (CF2.1).** If the first-mission control-strip
  reminder fades out (or either player skips it) before it's fully read, open pause — a
  **Show Control Reminder** option should now be available to bring it back, instead of it being
  gone for the rest of the session.

## Live confusion, coaching, failures, and reactions

| Time / beat | Trigger | Player reaction or failure | Coaching given (exact words, or none) | Recovery | Quote |
| --- | --- | --- | --- | --- | --- |
|  |  |  |  |  |  |

## Laugh and surprise log

| Time / beat | What happened | Who reacted | Exact quote / reaction | Repeated? |
| --- | --- | --- | --- | --- |
|  |  |  |  |  |

## Post-run debrief

Record answers as closely as possible to the players' own words.

1. Where did you know exactly what your dog should do?
2. Where did you feel lost?
3. What made you laugh?
4. Did the charger role flip make sense?
5. What would you want to replay?
6. What should be fixed before showing this to someone else?

## Evidence-backed verdict

**Verdict: NOT CALLED.**

Acceptance requires the completed observations above plus direct evidence that both humans understood
the dog-life joke and co-op jobs, mistakes were recoverable, the host used no more than one hint per
beat, and at least one player wanted a replay or another mission. If any critical readability,
control, progression, or reset failure occurs, record **REJECTED**, list the smallest blocking
findings below, and keep the 23-mission roster frozen.

### Blocking findings / smallest follow-up

- Pending live session.
