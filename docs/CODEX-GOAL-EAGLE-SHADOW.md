# Codex Goal: Eagle Shadow Panic

Implement after `SquirrelConspiracy` lands.

Repo guardrails:
- Unity only: work under `unity/CheddarAndCocoa/` and `docs/`.
- Do not edit frozen TS/Canvas code.
- Keep first pass inside `ArenaScene` and existing mission flow.
- Keep PlayMode tests deterministic.

Goal:
Add a selectable mission variant named `EagleShadowPanic` / `Eagle Shadow Panic`.

Gameplay:
- A large shadow sweeps across the yard.
- Dogs must hide in safe cover zones.
- One dog can bark to distract while the other rescues a toy/treat.
- Final phase requires both dogs close together for a united-front bark circle.

Implementation target:
- Extend `GameManager.MissionVariant` and `MissionOrder`.
- Add `BuildMissionDefinition` branch with unique objective copy, score labels, clear/fail reasons.
- Add a deterministic sweeping hazard state using generated placeholder shadow actor.
- Add safe cover zones with labels such as `HIDE HERE`.
- Add rescue objective after first safe hide succeeds.
- Add final united bark circle using existing united bark timing/proximity.
- Reuse score pops, audio/rumble cues, objective arrows, event log, replay flow.

Suggested score labels:
- `SAFE HIDE`
- `SHADOW DISTRACTED`
- `TOY RESCUED`
- `UNITED FRONT`
- `SHADOW PANIC CLEAR`
- `EAGLE SPOOK`

Tests:
- mission appears in select;
- start sets expected name/objective;
- safe cover prevents penalty;
- missed cover creates fail pressure or score penalty;
- rescue objective can complete;
- united-front bark clears final phase;
- replay resets shadow, rescue, score, and outcome.

Validation:
Run `./unity/run-playmode-tests.sh`.
