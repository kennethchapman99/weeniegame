# Couch Test Plan — Operation Pee Break (host runbook)

> **What this is.** The step-by-step script Ken + Sue follow *at the couch* to run the Operation Pee
> Break acceptance retest. This is the **runbook**; the **recording form** is
> [`OPERATION-PEE-BREAK-COUCH-RETEST-2026-07-14.md`](OPERATION-PEE-BREAK-COUCH-RETEST-2026-07-14.md)
> (the 16-item table, laugh log, and verdict live there). Keep that sheet open next to you and fill
> it in as you go — this plan tells you *how to run the session and what each fix should look like*.
>
> **The gate this feeds:** the second two-player couch playtest that keeps the 23-mission roster
> frozen until it passes. See [`README.md`](README.md) ACTIVE and
> [`NEXT-PRODUCTION-SLICE.md`](NEXT-PRODUCTION-SLICE.md) step 6.

---

## 0. The one rule that makes this test valid

**No coaching.** The whole point is finding out whether two humans understand the dog-life joke and
their co-op jobs *without help*. So:

- The host gives **no** unsolicited mechanic tips. Don't say "stare at the door," don't point.
- Allow yourselves **one** hint per beat, and only after a genuine "wait, what do I do?" — then use
  **F4** (on-screen guidance) rather than talking, and write down the exact moment and words.
- If you catch yourself explaining, stop — that moment is a finding, not a rescue. Note where you
  felt the urge; that's where the game isn't communicating yet.

If you break this rule, the run still has value as a fun playtest, but mark the sheet's verdict as
**not a clean cold read** so we don't over-trust it.

---

## 1. Before you sit down (preflight, ~5 min)

Do this once, before the players are watching.

- [ ] **Launch the packaged player, not the Unity Editor.** Open
  `unity/builds/dev/CheddarAndCocoa-Arena.app`. (Editor Play Mode is not acceptance evidence.)
- [ ] **Confirm the build is the tested one.** The retest sheet locks a player SHA-256 and a
  698/698 PlayMode result. If you rebuilt since, re-hash and re-run the suite first (the sheet's
  "Locked preflight evidence" section says exactly what to replace).
- [ ] **Two physical controllers**, one per human. Confirm each is independently bound — Cheddar on
  one, Cocoa on the other — and stays bound after a restart. (Sheet item #1.)
- [ ] **Shared display at real couch distance.** Sit where you'd actually sit. Readability failures
  only show up at couch distance, not leaning into a monitor.
- [ ] **F1 diagnostics OFF.** The big station/debug circles must be hidden for a cold read. You'll
  only touch F1 if you're specifically checking that they stay hidden (sheet item #7).
- [ ] **Pick roles for the session:** both of you play; one of you is also the **recorder** who keeps
  the laugh log and confusion log live *during* play (don't reconstruct it after — you'll forget the
  exact quotes, which are the most valuable output).
- [ ] Start from **cold mission select** and use **Play Recommended** to enter Pee Break.

**Two things to eyeball in mission select before you launch** (roster-wide fixes that a normal
session passes through anyway):

- The Pee Break **detail cover** should show noticeably more artwork than before (less top/bottom
  crop), and the **"YOUR TEAM PLAN"** panel should show small **icon chips** — door / leash /
  charger / bark-burst — next to the text bullets, not text alone. (CF1.9 / CF2.8, sheet observation
  block after the 16 items.)

---

## 2. The run — play it straight through, watch for these

Play the mission for real. The list below walks the actual flow of a session; at each point there's
**what should happen** (the shipped fix working) and the **🚩 red flag** that means it failed. The
number in parentheses is the row to mark on the recording sheet.

### A. Opening explainer + control card

- **Explainer plays first.** A ~10-second opening explainer should play *before* the control card.
  Either player can skip it, and the skip should be obvious enough that you understand you skipped
  it. (#2, #3)
- **The control card waits for you.** 🚩 *This was the #1 complaint last time.* The control card must
  **not** vanish on its own after a few seconds — it should sit there until one of you makes a
  deliberate **bark/interact** press. Test it on purpose: **read it slowly, don't touch anything for
  ~10 seconds.** It should still be there. (#4)
- **Everything is frozen while it's up.** The bladder meter and both dogs must be completely frozen
  the entire time the card is up — no timer ticking, no drift — until that press. (#5)

### B. Beat 1 — Door stare (`DoorStare`)

- **The door-stare spot reads as the floor.** 🚩 Last time the dogs looked like they were *climbing
  the door*. The correct stare spot should now read as sitting/standing at the door's **floor
  level** (the doormat), not partway up the door art. (#6)
- **The bladder meter is number-free.** "BLADDER EMERGENCY" should be a visible **meter**, not a
  number. (#9)
- **Toys are discoverable but harmless.** The optional toys should give a one-shot **pop** the first
  time a dog gets near, plus a periodic idle **wobble** so you actually notice them — and picking at
  them should still never change mission progress. (#10)

### C. Beat 2 — Leash message (`LeashMessage`)

- **The leash is carried, not static.** When Cheddar presents the leash, it should visibly **move
  with / be held at his muzzle**, not sit parked at one spot on the floor. (#13)

### D. Progress + misreads (throughout beats 1–3)

- **The progress meter never scrolls off.** 🚩 Last time the "TEENAGER GETS IT" meter disappeared
  when the dogs walked to the bottom of the play area. Deliberately **walk both dogs to the bottom
  edge** and confirm the beat-progress meter is still visible (it's mirrored into the always-on HUD
  now). (#8, first half)
- **The Teenager visibly changes.** By Beat 3–4 the Teenager should **look/sit differently** than in
  Beat 1 — posture/props shift, plus a one-shot **"NEXT!"** pop on each beat change so advancing
  feels like a moment. (#8, second half; #4-of-debrief)
- **A misread is funny, not just a buzzer.** 🚩 Last time misreads (they hit 35!) produced "nothing
  really funny." Now a wrong/early signal should trigger a real **physical-comedy beat** — the
  Teenager half-rises and offers the *wrong item* toward the dogs, both dogs react, and it escalates
  after 3+ misreads in a beat. **You should smile or laugh at least once at a misread.** If you
  don't, that's the finding. (#11)

### E. Beat 3 — The job swap (`ChargerGambit`)

- **The role flip is announced the instant it happens.** 🚩 Last time nobody could tell their jobs
  had swapped. Now the swap should fire an **on-screen banner**, a named **"NEW JOB"** pop at *each*
  dog, and both dogs' handoff chimes. **You should be able to point at the exact moment it happened**
  — not realize it after the fact. (#12) — this is also debrief question 4.

### F. Payoff + end

- **The payoff is visible before the end card.** The open-door / hydrant payoff should be clearly on
  screen *before* the end card, and the earned success must **not time out** during that hold — don't
  let the win get yanked away mid-celebration. (#14, #15)

### G. Replay (do at least one)

- **Everything resets.** Start a replay and confirm a clean reset of: beat, bladder, phone/battery,
  misread count, toys, and door state. Nothing should carry over from the first run. (#16)

---

## 3. While you play — capture the gold

The recorder keeps two logs **live** on the retest sheet:

- **Confusion / coaching / failure log:** every "what do I do?", every hint you gave (exact words or
  "none"), and how you recovered.
- **Laugh & surprise log:** time/beat, what happened, who reacted, the exact quote, and whether it
  repeated. Quotes are the single most useful thing this session produces — write them verbatim.

---

## 4. After the last run — debrief (5 min, use their own words)

Ask, and write answers close to verbatim:

1. Where did you know *exactly* what your dog should do?
2. Where did you feel lost?
3. What made you laugh?
4. Did the charger job-swap make sense — and could you tell the moment it happened?
5. What would you want to replay?
6. What should be fixed before showing this to someone else?

---

## 5. Calling the verdict

Fill the sheet's **Evidence-backed verdict** section, not this one. The gate **passes** only with
direct evidence that:

- both humans understood the dog-life joke and their co-op jobs,
- mistakes were recoverable,
- the host used **no more than one hint per beat**, and
- at least one player wanted a replay or another mission.

Any critical **readability, control, progression, or reset** failure → record **REJECTED**, list the
smallest blocking findings on the sheet, and **keep the 23-mission roster frozen.** A pass unlocks
the roster (Skunk Blast Mayhem and the rest of the idea bank become fair game).

---

## Quick reference card (print this bit)

| When | Do | Fails if 🚩 |
| --- | --- | --- |
| Control card up | Wait ~10s, don't press | It disappears on its own (#4) / anything moves (#5) |
| Beat 1 | Look at the door spot | Dogs look like they climb the door (#6) |
| Any beat | Walk both dogs to bottom edge | Progress meter scrolls off screen (#8) |
| On a wrong signal | Just watch | Misread isn't funny — no laugh (#11) |
| Beat 3 | Watch for the swap | You can't tell the moment jobs flipped (#12) |
| End | Let the win play out | Success times out mid-payoff (#15) |
| Replay | Restart once | Anything carries over from last run (#16) |

**Golden rule again: no coaching. Silence is data.**
