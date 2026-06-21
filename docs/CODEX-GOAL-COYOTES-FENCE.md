# Codex Goal: Coyotes at the Fence

> **Status: COMPLETED / HISTORICAL.** The mission is implemented. Do not execute these instructions
> or add their branches to `GameManager`; see `NEXT-PRODUCTION-SLICE.md` for active work.

Implement after `SquirrelConspiracy` and `EagleShadowPanic` are stable.

Repo guardrails:
- Unity only: work under `unity/CheddarAndCocoa/` and `docs/`.
- Do not edit frozen TS/Canvas code.
- Keep first pass inside `ArenaScene` and existing mission flow.
- Keep PlayMode tests deterministic.

Goal:
Add a selectable mission variant named `CoyotesFence` / `Coyotes at the Fence`.

Gameplay:
- A coyote tests weak spots along the yard fence.
- Dogs must patrol left/right fence gaps.
- One dog barks at the coyote while the other repairs/fills a weak spot.
- If dogs are too far apart, the coyote targets the isolated dog.
- Boss gag: fake snack lure that Cheddar is extra vulnerable to.

Implementation target:
- Extend `GameManager.MissionVariant` and `MissionOrder`.
- Add `BuildMissionDefinition` branch with unique objective copy, score labels, clear/fail reasons.
- Add generated fence gap actors with labels such as `WEAK SPOT` and `FILL DIRT`.
- Add coyote pressure state that chooses a gap deterministically.
- Add bark window to hold off coyote.
- Add repair/fill interaction that progresses only when the partner is holding pressure.
- Add isolated-dog danger if dog distance exceeds threshold during active pressure.
- Add fake snack lure as a late-phase event, with Cheddar-specific funny feedback if he is closer.
- Reuse score pops, audio/rumble cues, objective arrows, event log, replay flow.

Suggested score labels:
- `FENCE HELD`
- `DIRT FILLED`
- `COYOTE BLOCKED`
- `FAKE SNACK BAIT`
- `PARTNER SAVED`
- `YARD DEFENDED`

Tests:
- mission appears in select;
- start sets expected name/objective;
- bark pressure changes coyote state;
- repair interaction progresses only with partner pressure;
- isolated dog path creates fail pressure or rescue need;
- fake snack lure event fires deterministically;
- clear and fail paths work;
- replay resets gaps, coyote, lure, score, and outcome.

Validation:
Run `./unity/run-playmode-tests.sh`.
