# Baseline Couch Playtest — 2026-06-20

> **Gate status: FIX VERIFICATION DEFERRED BY OWNER.** Initial human playtest feedback was received
> on 2026-06-20. PR #15 and the subsequent first-minute objective pass addressed the recorded
> failures. On 2026-06-21 the owner explicitly authorized the documented controller migration to
> proceed while the two-player verification session remains unavailable. This is not a claim that
> the human readability gate passed.

## Scope

- Two humans, two physical controllers, one shared couch-distance display.
- Cold 20-minute session: Backyard Rescue, Blanket Catch, and Kitchen Falling Food Frenzy.
- No coaching beyond launching the build and resolving controller setup.
- Mission roster remains frozen. Architecture work has not started.

## Technical validation

Validated locally on 2026-06-20 EDT before this report was added, then revalidated on 2026-06-21
after merging PR #15.

| Check | Result | Evidence |
| --- | --- | --- |
| Unity project/scene/bootstrap wiring | Pass | `./unity/validate-demo.sh` completed successfully. |
| Unity editor | Pass | Project-pinned Unity `6000.0.65f1` on macOS arm64. |
| Script compilation | Pass | Unity batch import reported `Tundra build success`; no compile failure. |
| Full PlayMode suite | Pass | 333 total, 333 passed, 0 failed, 0 skipped after the Kitchen controller extraction; 18.405 s. Results: `unity/playmode-results.xml`. |
| Development build | Pass | `unity/builds/dev/CheddarAndCocoa-Arena.app`; Unity reported 341,691,494 bytes. |
| Packaged-player startup | Pass | `./unity/smoke-player.sh unity/builds/dev/CheddarAndCocoa-Arena.app`. |

Build executable SHA-256:
`af14aa7404c5e1527d9286925d5b10574b4ba60f87db83ee9815d537dc2b0710`.

Automated validation proves build health, deterministic mission coverage, and startup only. It does
not satisfy the human fun/readability gate.

## Cold-session protocol

### Setup — not part of the 20 minutes

1. Connect both controllers and put the game on the intended shared display at normal couch distance.
2. Launch `unity/builds/dev/CheddarAndCocoa-Arena.app`.
3. Verify only that controller 1 moves Cheddar and controller 2 moves Cocoa. State only the basic
   controls if needed: left stick moves, South/X barks, North/Y interacts, Start pauses/accepts.
4. Return to the cold-start mission picker. Keep the F1 playtest overlay off while players form their
   first impressions.

Do not explain objectives, roles, telegraphs, failure rules, scoring, mission mechanics, or end-flow
buttons. If input does not work, fix hardware/controller assignment and restart the cold session.

### Timed run — 20 minutes

| Time | Activity | Observer behavior |
| --- | --- | --- |
| 0:00–7:00 | Backyard Rescue | Say only: “Start Backyard Rescue.” Observe silently through an outcome or the time limit. |
| 7:00–12:30 | Blanket Catch | Ask the players to return to mission select and start Blanket Catch. Do not explain the blanket. |
| 12:30–18:00 | Kitchen Falling Food Frenzy | Ask them to return to mission select and start Kitchen Falling Food Frenzy. Do not assign roles. |
| 18:00–20:00 | End-flow and debrief | Ask them to use the controller to reach mission select, then ask the six neutral prompts below. |

If a mission ends early, let players choose Replay, Next, or Mission Select without help. Redirect to
the next required title only when needed to cover all three within 20 minutes. A fail, timeout, or
incomplete mission is valid evidence; do not force a clear.

Neutral debrief prompts:

1. What did you think each mission wanted you to do?
2. What did each dog own, and what did you say to each other?
3. What was hard to see or notice from the couch?
4. Where did you wait, repeat work, or not know how to recover?
5. What, if anything, was confusing about the controllers or ending a mission?
6. What made you laugh or surprised you? What was frustrating?

## Observation record

This record contains the player's reported interpretation. Observer timestamps and behavioral notes
were not captured, so they are explicitly left unknown rather than reconstructed.

Session metadata:

- Actual session date/time: **2026-06-20; exact time not recorded**
- Players: **One reporting player confirmed; whether a second player participated was not recorded**
- Controller models/connection: **Not yet recorded**
- Display and approximate couch distance: **Not yet recorded**
- Observer: **Not yet recorded**

| Mission/time | Observed behavior or short player quote | Category | Impact |
| --- | --- | --- | --- |
| Multiple levels | "It is not at all clear what to do." | Objective and telegraph readability | Critical: the player could not form a useful goal or connect visible mechanics to progress. |
| Multiple levels | Referenced shadows and squirrels were not visible on screen. | Camera and couch-distance visibility | Critical: threat/objective language referred to actors the player could not see. Fixed by PR #15. |
| General play | Words followed the character, were hard to read, and obscured the dog. | Camera and couch-distance visibility | Critical: identity/status labels competed with the playable character. Fixed by PR #15. |
| General play | Rotating labeled objects were skewed enough to be unidentifiable. | Objective and telegraph readability | Critical: continuous prop rotation destroyed silhouettes and text readability. Fixed by PR #15. |
| General play | Dog running animation and camera separation behavior worked well enough for now. | Camera and couch-distance visibility | Acceptable for this gate; retain current behavior. |
| Multiple levels | Mechanics were partly visible, but the low-fidelity presentation made their intended use hard to assess. | Objective and telegraph readability | Important: placeholder presentation is limiting mechanic comprehension. |

Required category coverage:

- Role ownership and communication
- Objective and telegraph readability
- Camera and couch-distance visibility
- Recovery friction and dead time
- Controller and end-flow confusion
- Laughter, surprise, and frustration

## Prioritized findings

### Critical — must fix before `IMissionController`

1. Threats and objective actors were off-screen. **Addressed by merged PR #15.**
2. Dog-following labels obscured the character. **Addressed by merged PR #15.**
3. Continuously rotating labeled props were unreadable. **Addressed by merged PR #15.**
4. The player could not tell what to do or how visible mechanics produced progress. **Implemented,
   awaiting player verification:** the mission picker now exposes a readable wrapped goal and the
   opening seconds show the premise, first objective, and controller verbs in a dedicated card.

### Important — after the controller boundary unless promoted by the session

- Replace the most comprehension-limiting placeholder art as each deep slice is authored; do not
  broaden this into an arena-wide art rewrite before the architecture gate.

### Later polish

No findings yet.

## Recommendation and gate decision

PR #15 correctly fixes the concrete visibility defects without changing mission rules. The narrow
onboarding/readability pass for the remaining "not clear what to do" finding is implemented and
available in the rebuilt development player. The next action is a human verification of the revised
first-minute experience.

**Baseline gate: FIX VERIFICATION DEFERRED BY OWNER.** The owner explicitly authorized architecture
work to proceed on 2026-06-21 because a couch session was not currently possible. The formal
two-human/two-controller coverage remains unconfirmed; capture it explicitly when verification can
resume. Do not reinterpret the authorization as human acceptance evidence.
