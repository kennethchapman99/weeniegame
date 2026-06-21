# Baseline Couch Playtest — 2026-06-20

> **Gate status: BLOCKED — awaiting the actual two-human couch session.** Technical validation and
> the local build are complete. No human observations or findings are claimed below.

## Scope

- Two humans, two physical controllers, one shared couch-distance display.
- Cold 20-minute session: Backyard Rescue, Blanket Catch, and Kitchen Falling Food Frenzy.
- No coaching beyond launching the build and resolving controller setup.
- Mission roster remains frozen. Architecture work has not started.

## Technical validation

Validated locally on 2026-06-20 EDT from a clean worktree before this report was added.

| Check | Result | Evidence |
| --- | --- | --- |
| Unity project/scene/bootstrap wiring | Pass | `./unity/validate-demo.sh` completed successfully. |
| Unity editor | Pass | Project-pinned Unity `6000.0.65f1` on macOS arm64. |
| Script compilation | Pass | Unity batch import reported `Tundra build success`; no compile failure. |
| Full PlayMode suite | Pass | 330 total, 330 passed, 0 failed, 0 skipped; 16.835 s. Results: `unity/playmode-results.xml`. |
| Development build | Pass | `unity/builds/dev/CheddarAndCocoa-Arena.app`; Unity reported 341,678,998 bytes. |
| Packaged-player startup | Pass | `./unity/smoke-player.sh unity/builds/dev/CheddarAndCocoa-Arena.app`. |

Build executable SHA-256:
`adef9446a086cfb9a3fc3a0f3bbe6e8671017f75ca21d9da7d11efbe64af418d`.

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

Complete this from behavior and short verbatim player comments. Distinguish observed behavior from
player interpretation. Use timestamps when possible.

Session metadata:

- Actual session date/time: **Not yet run**
- Players: **Not yet recorded**
- Controller models/connection: **Not yet recorded**
- Display and approximate couch distance: **Not yet recorded**
- Observer: **Not yet recorded**

| Mission/time | Observed behavior or short player quote | Category | Impact |
| --- | --- | --- | --- |
| — | **Awaiting actual session observations.** | — | — |

Required category coverage:

- Role ownership and communication
- Objective and telegraph readability
- Camera and couch-distance visibility
- Recovery friction and dead time
- Controller and end-flow confusion
- Laughter, surprise, and frustration

## Prioritized findings

### Critical — must fix before `IMissionController`

No findings yet. Do not interpret this as “none found”; the human session has not occurred.

### Important — after the controller boundary unless promoted by the session

No findings yet.

### Later polish

No findings yet.

## Recommendation and gate decision

No fix recommendation is valid until actual observations are captured and prioritized. Do not define
`IMissionController` or begin architecture work.

**Baseline gate: BLOCKED.** Unblock only after the actual two-human/two-controller session is recorded
above and any critical pre-architecture fixes are clearly identified (or the evidence supports that
there are none).
