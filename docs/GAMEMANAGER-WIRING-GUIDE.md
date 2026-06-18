# GameManager Wiring Guide

This guide tells local agents exactly how to wire the production helpers into gameplay.

## 1. Rank Calculation

Current target:
- Find duplicated rank/star logic in `GameManager`.
- Replace it with `MissionRankCalculator.Calculate(...)`.

Acceptance:
- Existing rank labels remain unchanged.
- Existing star behavior remains unchanged.
- Arena tests still pass.

## 2. Score Labels

Current target:
- Keep existing Backyard Rescue, Snack Heist, Sock Panic labels stable.
- For new missions, use `ScoreEventCatalog` rather than raw strings.

Example:
- `ScoreEventCatalog.GoodHerd.Label`
- `ScoreEventCatalog.Cutoff.Label`
- `ScoreEventCatalog.FakeOut.Label`

Acceptance:
- Score pop text remains readable.
- Event log records the same label.

## 3. Runtime Snapshot

Current target:
- Add a method such as `BuildRuntimeSnapshot()` on `GameManager`.
- Return `MissionRuntimeSnapshot` using current mission id, score, timer, objective progress, goal, mistake count, clear/fail flags.

Acceptance:
- Tests can query mission state without scraping UI.
- Challenge objectives can evaluate from snapshot.

## 4. Challenge Objectives

Current target:
- Add per-mission challenge objective specs.
- Evaluate at end of round using `ChallengeObjectiveEvaluator`.
- Add end-summary text for completed challenges later.

First pass:
- Squirrel Conspiracy: score 1500 and no fake-outs.
- Eagle: no dog grabbed.
- Coyote: no breaches.

## 5. Mission Seeds

Current target:
- Use `MissionSeedGenerator.StableSeed(...)` when selecting layout/comedy variants.
- Seed should include mission id, session mission count, and selected variant index.

Acceptance:
- PlayMode tests can force deterministic variants.
- Replay with same seed resets the same layout.

## 6. Production Tests

Add or update tests whenever wiring changes.

Required command:

```sh
./unity/run-playmode-tests.sh
```

Do not merge if existing missions regress.
