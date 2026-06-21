# Design Review — Soul & Whimsy Audit (2026-06)

> Reviewer brief: a senior co-op designer/engineer reviewed the repo for whether it can become a
> production-level couch co-op experience with the **soul and whimsy** of *It Takes Two* /
> *Split Fiction* — explicitly **not** chasing their 3D production scale. This doc is the honest
> verdict and the pivot. It supersedes the broad-mission direction implied by the current
> `GameManager` and several planning docs.

## The one-paragraph verdict

The engineering hygiene is genuinely good (87 passing PlayMode tests, clean reusable co-op puzzle
primitives that are actually wired to live dog positions). The co-op *design thinking* — forced
interdependence, role-lock, asymmetry, funny recoverable failure — is the hard part, and it's
already here. **The problem is not the medium and not 3D. The problem is breadth.** Many
60–90-second score-attack rounds crammed into one `GameManager.cs` that is nearly 8,000 lines as of
2026-06-20 is the single biggest
enemy of soul this project has. Whimsy does not live in a mission *count*; it lives in **depth,
specificity, surprise, personal jokes, animation/sound charm, and failure that's funny instead of
punishing.** The pivot is: stop adding missions, pick ONE, and make it deep and alive.

## Where soul actually comes from (and where this project leaks it)

| Soul ingredient | What it looks like in *It Takes Two* | Where we leak it today |
|---|---|---|
| **Specificity** | Every set-piece is *this* exact thing (the toolbox, the vacuum) | Missions are abstracted into generic `MissionDefinition` data fields — the joke gets flattened into `SquirrelStealingCue` strings |
| **Depth/escalation** | A mechanic is taught, explored, twisted, then retired ~10 min later | Missions are ~90 s and end on a *timer*, before any twist can land |
| **Funny failure** | You fail, you laugh, you retry instantly | Failure is mostly score subtraction + a fail-reason string |
| **Reactive world** | The world animates *at* you; characters emote | Generated sprites + synth tones; emotes are placeholder pops |
| **Personal voice** | Hazelight's weird humor is everywhere | Your *inside jokes* (the teenager-on-the-phone bible entry!) are documented but barely built |

Read that last row twice. `GAME-DESIGN-BIBLE.md` §6 literally names the teenager-phone pee-break as
the thesis for "personal jokes become mechanics." **That is the soul of this game and it's sitting
unbuilt while the repo accumulated 20 thin rounds.** That's the pivot in one sentence.

## What to STOP

1. **Stop adding missions.** Twenty-one mission variants is breadth theater. It feels like progress
   (commits! tests!) but it dilutes the game. Freeze the mission roster.
2. **Stop measuring against Hazelight's *production*.** You correctly don't care about 3D. Drop the
   comparison to their scale and keep only the comparison to their *craft of a single beat*.
3. **Stop expanding `GameManager.cs`.** As of 2026-06-20 it is 7,954 lines and holds state and
   branches for 21 mission variants.
   Every new mission there raises the "brace/scope drift" risk CLAUDE.md already warns about.

## What to DO — go deep on ONE slice

Build **one** mission into a 5–8 minute, 4-beat, role-reversing, fully-juiced experience that you'd
be proud to put in front of Sue cold. Recommendation: **Operation Pee Break** (full spec in
`docs/DEEP-SLICE-OPERATION-PEE-BREAK.md`). Rationale:

- It is the **highest-soul, most-personal** idea in the whole bible (the teenager inside joke).
- It reuses the **co-op primitive you already built and tested** (`CoopSocialManipulationPuzzle`,
  shipped as the WalkCampaign mission) — so it's depth, not new plumbing.
- "Humans are puzzle systems" is whimsy-dense and *cannot* exist in a generic platformer — it's
  uniquely yours.

Kitchen Falling Food Frenzy is already implemented. It now serves as the first behavior-preserving
controller extraction before Pee Break, not as an alternate new-slice choice.

### The depth bar (acceptance for "deep enough")

A slice is deep enough when it has, in order:

1. **Teach** — one clear new verb/mechanic, learned without text in ~30 s.
2. **Explore** — a beat that makes you *use* it under light pressure.
3. **Twist** — the mechanic changes or the roles flip (Cheddar's job becomes Cocoa's).
4. **Climax** — a united-bark / both-dogs set-piece payoff with real audio-visual juice.
5. **Funny failure everywhere** — every fail state is a gag + instant retry from a checkpoint, never
   a silent score hit.

If a slice can't fill all five, it's a round, not a chapter.

## The architecture fix that protects the soul

Soul dies in a god class because nobody dares add the weird, specific, hand-tuned beat to a file
that was nearly 8,000 lines on 2026-06-20. Decomposition isn't bureaucracy here — it's what makes
whimsy *cheap to add*.

### `GameManager.cs` decomposition plan (incremental, test-green at every step)

The half-built data-driven framework already points the way. Target shape:

```
GameManager                         orchestration, mission select, session flow, shared-service wiring
  └─ IMissionController (interface) setup, state, Tick(dt), input, cleanup, Outcome, snapshots
       ├─ BackyardRescueMission
       ├─ KitchenFrenzyMission
       ├─ PeeBreakMission           ← the deep slice lives in its OWN file, ~300-600 lines
       └─ … (one file per mission)
  └─ shared services (already mostly extracted):
       ├─ Co-op puzzle primitives (CoopHoldRelease/BaitSwitch/ScentRelay/… — keep as-is, they're good)
       ├─ DogController / DogReadabilityFeedback (keep)
       ├─ ArenaHud / juice / camera (keep)
       └─ MissionDefinition data (outside GameManager; per-mission code owns the *behavior*)
  └─ controller registry (outside GameManager)
```

**Migration recipe (one mission at a time, never a big-bang rewrite):**

1. After the baseline couch playtest and its critical fixes, define `IMissionController` and a
   narrow `MissionContext`. Include only the shared services and test hooks a controller needs.
2. Extract the **existing Kitchen mission first**: its setup, state, ticking, input handling,
   cleanup, outcome, and snapshot. Put its definition and registration outside `GameManager`.
3. Run the full PlayMode suite. Do not continue until it is green.
4. Build Pee Break entirely through that proven controller structure.
5. Continue extracting existing missions one at a time only when the active sequence calls for it,
   and keep tests green after every extraction.
6. Stop when ownership is correct: `GameManager` orchestrates and wires shared services; controllers
   own mission behavior. No arbitrary line count defines completion.

## The thing you keep deferring (do it this week)

Every gate in `PRODUCTION-READINESS.md` is blocked on the same unrun test: **two humans, two
controllers, 20 minutes, on the couch.** You cannot tune soul from a headless test suite. Whimsy is
a felt quantity. Play it with Sue, watch where she laughs and where she's confused, and let *that*
re-rank everything below.

## Doc hygiene (small, but it's lying to you)

- `COOP-VISION.md` still describes a dead **TypeScript/Tauri/Capacitor** stack the project left for
  Unity. Update or archive it.
- `PRODUCTION-READINESS.md` calls a never-playtested build a "release candidate." It's a *technical*
  RC, not a *fun* RC. Rename the bar so it stops implying the game is proven.
- ~50 design docs is itself a soul-leak: it hides that there is no single deep, proven, fun slice.
  After the Pee Break slice ships, prune.

## Bottom line

You don't need 3D and you don't need Hazelight's budget to get their *soul*. You need **one deep,
specific, funny, role-flipping, fully-juiced mission** instead of many thin ones — and a code
structure where adding the next weird joke costs an afternoon, not a god-class surgery. The
foundation is real. Point it at depth.
