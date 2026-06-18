# Next Production Slice

Build next: Great Backyard Squirrel Conspiracy.

Purpose: expand the current ArenaScene from three prototype missions into a real chase/herding mission while reusing existing squirrel, bark, score, replay, objective, and PlayMode test systems.

First pass:

- Add `SquirrelConspiracy` as a selectable mission variant.
- Add deterministic squirrel route nodes.
- Add cutoff zones for the second dog.
- Add bark timing so early bark causes a fake-out and correct bark applies pressure.
- Reveal a hidden stash after enough successful herds.
- End the mission on stash found, timer expiry, or repeated squirrel taunts.

Score events:

- `+75 GOOD HERD`
- `+125 CUTOFF`
- `+150 DOUBLE BARK BLOCK`
- `-75 FAKE OUT`
- `+300 STASH FOUND`
- `+500 CONSPIRACY CRACKED`

Tests required:

- Mission select includes the new mission.
- Starting the mission sets the right objective copy.
- Cutoff success awards score.
- Early bark applies fake-out penalty.
- Stash reveal updates objective copy.
- Replay resets score, route state, stash state, and outcome.

Guardrails:

- Keep this inside ArenaScene.
- Do not add campaign persistence yet.
- Do not require final art.
- Keep score mutations on the existing score path.
- Keep the round short, readable, and replayable.
