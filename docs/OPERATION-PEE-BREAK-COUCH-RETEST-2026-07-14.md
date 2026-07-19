# Operation Pee Break Couch Retest — 2026-07-14

> **Status: NOT RUN — NO VERDICT.** This sheet is for the required two-human, two-controller cold
> retest of the response build. Automated checks and a prepared form are not acceptance evidence.

## Locked preflight evidence

- Player: `unity/builds/dev/CheddarAndCocoa-Arena.app`
- Player executable built: 2026-07-19 with Unity 6000.4.2f1
- Player executable SHA-256: `ba57952c534ce679497c676fd0efe84c006721fbea63a4190932708837ea6a9e`
- Packaged explainer SHA-256: `280de2f463f2f64d337937a16673c549a970e99a6d1c2bb3774a05b34d002e63` (unchanged
  since 2026-07-16 - the video asset itself was not touched by the pre-launch queue)
- Full PlayMode result: 662/662 passed, 0 failed, 0 skipped, 2026-07-19
- PlayMode result: `unity/playmode-results.xml`
- PlayMode result SHA-256: `95b2ac18d567225294b4b1adad40bd7d536cc29f6e3473c839dfc4d66c7dc17c`
- Packaged-player startup smoke: passed after this rebuild
- **This build now also includes the full 2026-07-17..07-19 pre-launch queue** (see
  `docs/AGENT-WORK-QUEUE-PRELAUNCH.md`, all rows DONE): the stall-aware guidance escalation ladder
  (Tier 0-3), role-turn beacons, handoff flip flourish, a roster-wide wrong-role coaching audit,
  animation coverage fixes (interact micro-animation, sniff pose, threat/NPC acting, held-payoff
  poses), an art-consistency sweep (baked-text removal, mission-tile detail-panel fix, indoor staging
  for 3 missions, HUD/end-card copy pass), a universal first-mission control-reminder strip and
  post-clear routing audit, a briefing-accuracy audit (23/23 checked), and named audio cues for the
  new signals plus a feedback-slot coverage sweep (9 previously-silent moments fixed). None of this
  changed Pee Break's own beats, tuning, or fail conditions - the checklist below is unaffected by any
  of it except that the new signals (ladder tiers, beacons, handoff chimes, control strip) are now
  live during this same session and worth watching for, not just the original response-pass items.
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

| Requirement | Result | Human evidence / exact observation |
| --- | --- | --- |
| Two physical controllers remain independently bound to Cheddar and Cocoa |  |  |
| Ten-second opening explainer plays before control card |  |  |
| Either player can skip the explainer; skip is understood |  |  |
| Visual keyboard/controller card is readable without verbal control coaching |  |  |
| Mission time and dog movement remain frozen through explainer/card |  |  |
| Players discover the station sequence in the intended order |  |  |
| Large station/debug circles remain hidden with F1 off |  |  |
| Dog arrows, command signals, labels, props, and Teenager reactions communicate progress |  |  |
| BLADDER EMERGENCY reads as a number-free visible meter |  |  |
| Both optional toys can be batted and do not advance mission state |  |  |
| Early/incomplete signals produce a funny, understandable, recoverable mistake |  |  |
| Beat 3 role flip creates understandable two-player coordination |  |  |
| Door/leash setup plus both barks produces the united-bark climax |  |  |
| Open-door/hydrant payoff remains visible before the end card |  |  |
| Earned success does not time out during the payoff hold |  |  |
| Replay resets beat, bladder, phone/battery, misreads, toys, and door state |  |  |

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
