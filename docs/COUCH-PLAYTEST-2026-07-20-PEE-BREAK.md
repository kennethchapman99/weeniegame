# Couch Playtest — 2026-07-20 — Operation Pee Break

> **Gate status: PARTIAL.** The deep-slice acceptance couch test ran on 2026-07-20 and the
> mission was completed ("i finished the level this time"). 9 of 16 watch-for items passed
> cleanly, but 1 failed, 1 was partial, and 5 were unverified or unclear to the players. The
> gate is not cleanly passed until the fix list below is addressed and re-verified.
>
> **Execution queue:** every finding below is now an implementation-ready task in
> `docs/AGENT-WORK-QUEUE-COUCHFIX.md` (CF1.x = Pee Break fixes, CF2.x = roster theme audits).
> Work from that queue, not from this report.

Source of truth: `captures/2026-07-20/CouchTest 7.20.docx` (owner's annotated checklist with
embedded screenshots, extracted alongside it as PNGs). This file is the faithful transcription.

## Checklist results

| # | Watch for | Result | Notes / quote |
| --- | --- | --- | --- |
| 1 | Both controllers stay bound: P1=Cheddar, P2=Cocoa | Pass | |
| 2 | 10-sec opening explainer plays before the control card | Pass | |
| 3 | Either player can skip it, and skip made sense | Pass | |
| 4 | Control card readable without you explaining controls | **Partial** | "The control card dismisses itself after a period of time - I need time to look at it and accept it" |
| 5 | Game stays frozen during explainer/card | (See #4) | Tied to the auto-dismiss issue above |
| 6 | Players figure out the station order on their own | **Unclear** | "It's weird that the door stare has to happen by climbing up the wall?" — see `pee-break-door-stare-wall-climb.png`: both dogs render lying flat against the door/wall at the stare station |
| 7 | No big debug circles visible (F1 off) | Pass | "They go away when the F1 is off" |
| 8 | Arrows, signals, labels, Teenager reactions show progress | **Fail** | "The progress meter disappears when the dogs move to bottom part of screen" — see `pee-break-misreads-ticker-teenager.png`. Also: "Ideally after each phase, the teenager looks a little different, or shifts positions to indicate we're on the 'next subproblem'" |
| 9 | BLADDER EMERGENCY reads as a meter (no numbers) | Pass | |
| 10 | Toys can be batted around, don't affect the mission | **Unverified** | "Didn't see this working or not…" |
| 11 | An early/wrong signal is funny + recoverable | **Fail** | "Nothing really funny happens" — the ticker in the screenshot shows **MISREADS 35**, so misreads happened constantly with no comedic payoff |
| 12 | Beat 3 role flip makes sense as a two-player moment | **Unclear** | "Not sure what this means - but i finished the level this time" |
| 13 | Door + leash + both barks = united-bark climax | Partial | "Pretty good - the leash needs to be animated though, maybe carried around in mouth?" |
| 14 | Open-door/hydrant payoff visible before end card | Pass | |
| 15 | Success doesn't time out during the payoff hold | Pass | |
| 16 | Replay resets everything (bladder, phone, toys, door) | Pass | |

## Freeform feedback — mission title card

See `pee-break-title-card.png`:

- The title card art "seems to crop down most of the image, not a big deal, but detail is lost."
- "The Team plan should ideally show little visual clips of what things will actually look like
  on-screen if possible, just the important ones" — text-only beat descriptions didn't connect to
  what players later saw (which also explains the #12 confusion about "beat 3 role flip").

## Pee Break fix list (before gate re-verification)

1. **Control card requires explicit accept** — no auto-dismiss timer; each player (or either
   player) confirms when ready. (#4/#5)
2. **Door-stare station readability** — dogs visually "climb the wall" at the door; fix the pose,
   station placement, or perspective so the stare reads as sitting at the door. (#6)
3. **Progress meter anchoring** — the meter vanishes when dogs move to the bottom of the screen;
   pin mission progress UI to screen space, or keep it clear of the play area. (#8)
4. **Teenager reflects beat progression** — pose/position/prop change per beat so players can see
   they are on the "next subproblem." (#8)
5. **Misread comedy payoff** — 35 misreads produced zero laughs; an early/wrong signal needs a
   visible funny reaction (gag template from the 2026-07-05 pass is the tool for this). (#11)
6. **Label the role flip in-game** — beat 3's swap needs an on-screen moment (role beacons /
   handoff copy) so it reads as an intentional two-player flip. (#12)
7. **Leash is carried, not static** — animate the leash, ideally in-mouth carry. (#13)
8. **Toys need visible presence** — players never noticed them; either make them discoverable or
   cut them from the briefing. (#10)
9. **Title card art crop + team-plan visuals** — reduce crop loss; add small visual clips of the
   actual on-screen signals to the team plan. (Freeform)

## Themes to audit across the rest of the roster

The owner flagged this document as containing themes to check in other levels. Each theme below
generalizes past Pee Break:

1. **Timed auto-dismissing info UI.** Any mission card, explainer, or tutorial overlay that
   dismisses on a timer instead of player confirmation fails slow readers. Audit every mission's
   pre-mission flow.
2. **HUD occlusion by player position.** Any world-anchored meter/label that players can cover or
   push off-screen by moving. Audit every mission's meters against the full movement range.
3. **World reflects phase progress.** Missions with multi-beat structure should change something
   visible in the world (NPC pose, prop state, lighting) per beat — HUD text alone didn't land.
4. **"Funny failure" promises must be audited against actual gags.** If a briefing or design doc
   claims a failure is funny, there must be a concrete on-screen gag. 35 silent misreads is the
   failure mode to hunt for.
5. **Carried/used objects need carry animations.** Static props that are logically "held" (leash,
   food, chicks, rope) break the fantasy; check what each mission's key object looks like mid-use.
6. **Wall/vertical station perspective.** Stations mounted on walls/doors in the top-down-ish view
   make dogs look like they're climbing. Check every wall-adjacent interaction point.
7. **Briefing beats must be self-verifying in-game.** If a player can read a beat description and
   still not recognize the moment when it happens ("not sure what this means"), the beat needs an
   in-game label. Cross-check against the F4.3 briefing-accuracy audit.
8. **Title cards: crop loss + text-only team plans.** Applies to all 23 mission cards.

## Verification notes

- Session date: 2026-07-20. Players: Ken and Sue, two controllers, couch distance.
- Automated baseline at time of test: 662 PlayMode tests green (see
  `docs/AGENT-WORK-QUEUE-PRELAUNCH.md` completion state).
- This document records human feedback only; no code changes were made as part of the
  transcription.
