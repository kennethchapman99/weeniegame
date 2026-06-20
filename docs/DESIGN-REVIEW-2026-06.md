# Design Review — Soul & Whimsy Audit (2026-06)

> Reviewer brief: a senior co-op designer/engineer reviewed the repo for whether it can become a
> production-level couch co-op experience with the **soul and whimsy** of *It Takes Two* /
> *Split Fiction* — explicitly **not** chasing their 3D production scale. This doc is the honest
> verdict and the pivot. It supersedes the "20-mission breadth" direction implied by the current
> `GameManager` and several planning docs.

## The one-paragraph verdict

The engineering hygiene is genuinely good (87 passing PlayMode tests, clean reusable co-op puzzle
primitives that are actually wired to live dog positions). The co-op *design thinking* — forced
interdependence, role-lock, asymmetry, funny recoverable failure — is the hard part, and it's
already here. **The problem is not the medium and not 3D. The problem is breadth.** Twenty
60–90-second score-attack rounds crammed into one 7,238-line `GameManager.cs` is the single biggest
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

1. **Stop adding missions.** Twenty shallow missions is breadth theater. It feels like progress
   (commits! tests!) but it dilutes the game. Freeze the mission roster.
2. **Stop measuring against Hazelight's *production*.** You correctly don't care about 3D. Drop the
   comparison to their scale and keep only the comparison to their *craft of a single beat*.
3. **Stop expanding `GameManager.cs`.** It is a 7,238-line god class holding 20 missions of state.
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

(If you'd rather lead with food chaos, **Kitchen Falling Food Frenzy** is the strong alternate; it
leans on Cheddar/Cocoa asymmetry — barf vs. steady — instead of social manipulation. Pick one. Not
both. Not yet.)

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

Soul dies in a god class because nobody dares add the weird, specific, hand-tuned beat to a
7,000-line file. Decomposition isn't bureaucracy here — it's what makes whimsy *cheap to add*.

### `GameManager.cs` decomposition plan (incremental, test-green at every step)

The half-built data-driven framework already points the way. Target shape:

```
GameManager (thin)                  ~400 lines — flow state, mission select, shared score/HUD wiring
  └─ IMissionController (interface) Start(), Tick(dt), ForceAdvance(seconds) [test hook], Outcome
       ├─ BackyardRescueMission
       ├─ KitchenFrenzyMission
       ├─ PeeBreakMission           ← the deep slice lives in its OWN file, ~300-600 lines
       └─ … (one file per mission)
  └─ shared services (already mostly extracted):
       ├─ Co-op puzzle primitives (CoopHoldRelease/BaitSwitch/ScentRelay/… — keep as-is, they're good)
       ├─ DogController / DogReadabilityFeedback (keep)
       ├─ ArenaHud / juice / camera (keep)
       └─ MissionDefinition data (keep, but per-mission code owns the *behavior*)
```

**Migration recipe (one mission at a time, never a big-bang rewrite):**

1. Define `IMissionController` with the test hooks the PlayMode suite already calls
   (`ForceAdvance(seconds)`, exposed puzzle state, `Outcome`).
2. Move **one** mission's state + `Update` branch + helpers out of `GameManager` into its own
   `*Mission.cs`. Point `GameManager` at it via the interface.
3. Run the PlayMode suite. It must stay 87/87 green — the tests already pin this behavior, so they
   are your safety net for the extraction.
4. Repeat per mission. `GameManager` shrinks ~300–600 lines each pass.
5. Stop when `GameManager` is just flow + shared wiring. Now adding a weird new beat touches ~one
   small file, and the whimsy gets cheap again.

Do step 1–3 for the **Pee Break** slice first, so the deep slice is *born* in the clean structure.

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
specific, funny, role-flipping, fully-juiced mission** instead of twenty thin ones — and a code
structure where adding the next weird joke costs an afternoon, not a god-class surgery. The
foundation is real. Point it at depth.
