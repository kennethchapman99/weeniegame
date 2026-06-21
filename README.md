# Cheddar & Cocoa 🐶🐶

A personal 2D couch co-op game starring two long-haired mini dachshunds: Cheddar, the golden chaos
puppy, and Cocoa, the chocolate reigning spot queen. Ken and Sue play exaggerated dog-life
adventures built around communication, asymmetric roles, readable chaos, and funny recovery.

> **Active codebase:** Unity only (`unity/CheddarAndCocoa/`). The TypeScript/Canvas build (`src/`,
> `tests/`) and `prototype/` are frozen, read-only behavior and balance references. Do not add or
> edit gameplay there. See `docs/README.md` for the document status index.

## Current depth-first pivot

The existing Unity build has broad mechanic coverage but has not passed its human fun gate. As of
2026-06-20, `GameManager.cs` is nearly 8,000 lines and declares 21 mission variants. The roster is
frozen while the project proves one deep, personal slice and introduces an incremental controller
boundary for mission behavior.

Canonical work sequence:

1. Run a baseline two-player couch playtest of the existing slices.
2. Address critical playtest findings.
3. Define `IMissionController` and a narrow `MissionContext`.
4. Extract the existing Kitchen mission first, keeping all PlayMode tests green.
5. Build Operation Pee Break entirely through the new controller structure.
6. Run a second couch playtest as the deep-slice acceptance gate.
7. Keep the mission roster frozen until that gate passes.

Do not substitute roadmap work, more mission variants, or direct `GameManager` branches for this
sequence. The migration succeeds when ownership is clear and behavior stays green after every
extraction; no arbitrary line count defines completion.

## Start here

1. Read `AGENTS.md` and `CLAUDE.md` for the operating contract.
2. Read `docs/NEXT-PRODUCTION-SLICE.md` for the active gate and sequence.
3. Read `docs/GAME-DESIGN-BIBLE.md` for the creative north star.
4. Read `docs/DEEP-SLICE-OPERATION-PEE-BREAK.md` for the deep-slice design.
5. Read `docs/ARCHITECTURE.md` and `docs/MISSION-SYSTEM.md` before touching mission code.
6. Use `docs/ARENA-PLAYABLE.md` for current controls and manual playtest acceptance.
7. Open `unity/CheddarAndCocoa/README.md` for Unity setup.

## Product facts

- Unity 6 LTS, C#, URP 2D, Unity Input System, two local controllers.
- Desktop-to-TV on macOS/Windows is first; other platforms are later decisions.
- Two-human couch co-op is the headline. Automated tests prove technical behavior, not fun.
- Bark must remain gameplay-relevant, and substantial missions require authored co-op role locks.
