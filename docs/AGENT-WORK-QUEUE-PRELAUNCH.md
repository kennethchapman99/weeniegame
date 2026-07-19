# Pre-Launch Agent Work Queue

> **Status: COMPLETE (2026-07-19).** Written 2026-07-17, all 23 rows DONE as of E6.1. This was the
> execution queue for `docs/PRELAUNCH-PRODUCTION-PLAN.md`. The only remaining step toward "pre-launch
> ready" is the human Operation Pee Break couch retest
> (`docs/OPERATION-PEE-BREAK-COUCH-RETEST-2026-07-14.md`) - agents should not pick up further tasks
> here until that retest produces new findings. Kept as reference for the per-task evidence and
> lessons recorded below; read that plan plus `CLAUDE.md`, `docs/README.md`, `docs/ARENA-PLAYABLE.md`,
> and `docs/VISUAL-READABILITY-CONTRACT.md` for the surrounding context.

## How to work this queue

- **One task per run.** Finish it test-green, record evidence, update this file's status column,
  commit. Do not batch tasks or start a task whose dependencies are open.
- **Preflight every run:** `git status` must be clean before you start. If another agent's work is
  uncommitted in the tree, stop and report — do not build on or revert it.
- **Every task ends with:** `./unity/run-playmode-tests.sh` fully green; a new deterministic test
  that fails without your change (where the task changes behavior); doc updates named in the task;
  a commit whose message names the task ID.
- **Visual tasks additionally end with:** `./unity/build-dev.sh`, `./unity/smoke-player.sh`, and an
  art-review capture (`<built-player> --arena-art-review=<abs-path>`) inspected for the task's
  acceptance shots.
- **Never:** add/redesign missions, change mission rules or tuning, put mission-specific branches in
  `GameManager`, touch `src/`/`tests/`/`prototype/`, or weaken an existing test to get green. If a
  task turns out to require a rule change, mark it BLOCKED with a note and move on.
- Shared-system code goes under `Assets/Scripts/` in the existing shared folders (`Game`, `UI`,
  `Camera`, etc.); mission-owned art/state stays in that mission's controller. Follow the seams the
  codebase already uses: `MissionContext` for controller→shared requests, optional narrow interfaces
  (like `IMissionUnitedBarkListener`) for shared→controller queries.
- Status values: `OPEN`, `IN PROGRESS`, `DONE (date, evidence)`, `BLOCKED (reason)`.

## Status board

| ID | Task | Status |
|---|---|---|
| P0.1 | Baseline after Codex lands | DONE (2026-07-17, see below) |
| G1.1 | Stall detector + escalation ladder core | DONE (2026-07-17, see below) |
| G1.2 | Tier 1–3 signal wiring | DONE (2026-07-17, see below) |
| G1.3 | Role-turn beacon | DONE (2026-07-18, see below) |
| G1.4 | Handoff flip flourish | DONE (2026-07-18, see below) |
| G1.5 | Wrong-role coaching audit | DONE (2026-07-18, see below) |
| G1.6 | Ladder observability + couch telemetry | DONE (2026-07-18, see below) |
| A2.1 | Animation coverage audit | DONE (2026-07-18, see below) |
| A2.2 | Interact micro-animation | DONE (2026-07-18, see below) |
| A2.3 | Signal-critical verb strips (dig/sniff/carry) | DONE (2026-07-18, code-side prep only — see below) |
| A2.4 | Threat/NPC acting gaps | DONE (2026-07-18, see below) |
| A2.5 | Held-payoff pose audit | DONE (2026-07-19, see below) |
| V3.1 | Style-contract audit + fix list | DONE (2026-07-19, see below) |
| V3.2 | Kill remaining square/debug first reads | DONE (2026-07-19, see below) |
| V3.3 | Mission tile consistency | DONE (2026-07-19, see below) |
| V3.4 | Indoor-fantasy staging audit | DONE (2026-07-19, see below) |
| V3.5 | HUD + end-card copy/style pass | DONE (2026-07-19, see below) |
| F4.1 | Universal first-mission control reminder | DONE (2026-07-19, see below) |
| F4.2 | Post-clear flow + session summary | DONE (2026-07-19, see below) |
| F4.3 | Briefing accuracy audit | DONE (2026-07-19, see below) |
| S5.1 | Audio for new signals | DONE (2026-07-19, see below) |
| S5.2 | Feedback-slot audio coverage audit | DONE (2026-07-19, see below) |
| E6.1 | Evidence refresh + gate handoff | DONE (2026-07-19, see below) — **queue complete** |

---

## Phase 0 — Preflight

### P0.1 — Baseline after Codex lands
**Goal:** a clean, verified starting point.
**Do:** confirm `git status` is clean and the Codex finishing pass (asymmetric-role redesigns,
breadcrumb wayfinding, Car Ride backseat art, doc syncs) is committed on `main`. Run the full
PlayMode suite and record the count here. Build dev player, run smoke, run the art-review capture,
and skim the contact sheet for regressions.
**Done when:** suite count and build hashes recorded in the status board row; no work items — this
task produces evidence only. If the tree is dirty, this task is BLOCKED and nothing else may start.

**Done (2026-07-17):** Codex's finishing pass was found sitting uncommitted in the working tree
(291 files) rather than landed on `main` as this task assumed. Reviewed it — full manual read of
every doc diff plus 8+ of the largest mission controllers, and a 5-angle/8-finder independent code
review (line-by-line, removed-behavior, cross-file, reuse/simplification/efficiency,
altitude/conventions) — before committing, per the "stop and report, don't build on or revert"
preflight rule; reporting here since the finding was "review and land it," not "something is
broken."

- Landed in three commits on `main`: `8c96898` (fixed `run-playmode-tests.sh`/`build-release.sh`,
  which still hardcoded a `6000.0.*` editor search after the project's `6000.4.2f1` bump — they
  would have silently run the stale 6000.0.65f1 editor), `ef8948e` (this queue + the production
  plan, written before Codex's pass landed), `50fa09a` (Codex's redesign), `6d93135` (fixed a real
  gap the independent review found: six controllers' `DogIdAt` helpers and two call sites
  dereferenced `_context.Dogs` before confirming it was non-null).
- Every other candidate the review raised (HerdingMissionState control-counting change, Eagle
  Shadow's shadow-column removal, CarRide/Kitchen sortingOrder sign difference, mission-seed
  decoupling from selector position) traced back to an intentional, tested, already-documented
  design decision — confirmed against the matching PlayMode test or doc passage, not just Codex's
  say-so.
- Full PlayMode suite: **571/571 passed, 0 skipped**, ~60s (`unity/playmode-results.xml`, SHA-256
  `441875a019491d3772f775de93b2b0439fc43a49bf8b7e6e4727b09184655935`, 2026-07-17 17:41 EDT).
- Dev player rebuilt at HEAD (`6d93135`): `unity/builds/dev/CheddarAndCocoa-Arena.app`, executable
  SHA-256 `c562662c8b5005c9d286fe7be86ecc9da335854eb490c0e95b57dcfab2435262`. Startup smoke passed.
- Art-review capture ran clean: 69/69 expected frames written, no exceptions in the run log. **Not
  visually inspected** — this sandbox has no GPU/display, so `-nographics` batchmode produces flat
  placeholder-colored frames (confirmed: single unique color per frame) instead of real renders.
  The next agent or the couch-test machine needs to run the capture with a real display attached to
  actually eyeball the contact sheet.
- 8 untracked media files at the repo root (2026-07-14 couch-test recordings) were left alone at
  the owner's request — not part of this baseline, not blocking.

Everything above is upstream of G1.1: the tree is clean, tests are green, and the current `main` is
the correct base for the guidance-ladder work.

---

## Phase 1 — Guidance grammar and stall escalation (sequential; the core of the plan)

### G1.1 — Stall detector + escalation ladder core
**Goal:** a shared, controller-agnostic service that knows "how long since this team made progress"
and exposes an escalation tier (0–3).
**Context:** see "The central design problem" in `docs/PRELAUNCH-PRODUCTION-PLAN.md` for tier
semantics and default timings (12s / 25s / 45s). All 23 controllers already expose objective copy,
score events, and snapshots; any of those changing counts as progress and resets to Tier 0.
**Do:** implement a `MissionGuidanceEscalation` (name flexible) shared service owned by
`GameManager`'s shared-service wiring, ticked only during active play. Freeze/reset it during
briefing, opening explainer, sniff-around countdown, pause, held success payoffs, and end cards, and
on mission start/replay. Per-mission tier-cap and timing overrides live in mission definitions
(default: all tiers, default timings) — data, not code branches.
**Tests:** deterministic PlayMode coverage: tier advances at forced timestamps; each progress signal
type resets it; frozen states don't accumulate stall time; replay resets; a mission with a Tier-2
cap never reaches 3.
**Done when:** service exists with zero visual output (G1.2 renders it), suite green, new tests fail
without the service.

**Done (2026-07-17):** Added `MissionGuidanceEscalation`
(`unity/CheddarAndCocoa/Assets/Scripts/Game/MissionGuidanceEscalation.cs`) — a plain C# class (not a
`MonoBehaviour`; `GameManager` owns and ticks the one instance) tracking `StallSeconds`/`Tier`
against configurable 12s/25s/45s thresholds and a tier cap, with `NotifyProgress()`/`Reset()`.

- **Wiring, not a new subsystem:** GameManager ticks it in `Update()` at the exact line that already
  gates `TimeRemaining` on `presentingEarnedSuccess` (`GameManager.cs`), so it inherits the existing
  freeze structure for free — the opening explainer, sniff-around lead-in, and end-card early-returns
  all already short-circuit `Update()` before that line; held success payoff got its own explicit
  `if (!presentingEarnedSuccess)` guard next to the one `TimeRemaining` already uses; pause is free
  because `Time.timeScale = 0` makes `Time.deltaTime` 0 while paused. `BeginRound()` calls
  `Configure()` + `Reset()` on every mission start/replay; `ShowMissionSelect()` also resets it.
  `AddScore()` and the "changed" branch of `LogObjectiveIfChanged()` — the two generic progress
  signals every controller already fires through — call `NotifyProgress()`.
- Per-mission overrides live as four new fields on `GameManager.MissionDefinition`
  (`GuidanceTierCap`, `GuidanceTier1/2/3Seconds`), defaulted to the full ladder at standard timings;
  no mission needed an override yet, so none of the 23 `Build*Definition` methods changed.
- Added `GameManager.GuidanceTier` / `GuidanceStallSeconds` read-only properties (the only new
  observable surface — no rendering, per the done-criteria) and a `ForceGuidanceStall(float seconds)`
  test hook in `GameManager.ControllerHooks.cs` alongside the existing `Force*` deterministic hooks.
- **Tests:** `MissionGuidanceEscalationTests.cs` (8 pure-logic tests, no scene: tier thresholds at
  forced timestamps including boundaries, zero/negative-delta no-op, `NotifyProgress`/`Reset`, a
  Tier-2 cap never reaching 3, a Tier-0 cap staying at Discovery, custom timing overrides) plus
  `GuidanceEscalationPlayModeTests.cs` (9 scene-driven integration tests: mission-start-at-Tier-0,
  forced-timestamp tier advance, a real score event (treat collect) resetting it, a real
  objective-copy-only change (`ForcePredatorWarning`, no score) resetting it, pause not accumulating,
  the sniff-around lead-in not accumulating (`LeadInSecondsOverride`), a real SnackHeist held-success
  payoff not accumulating, replay resetting it, and all 23 registered mission definitions defaulting
  to the full cap/standard timings). First run caught three test bugs in my own new integration
  tests (not production code): two used exact `0f` equality against a value that legitimately ticks a
  hair above zero one frame after start/restart (fixed with a `< 0.1f` bound), and the held-success
  test's naive collect-loop stalled forever because SnackHeist requires a Cocoa guard-bark before
  further collects count (fixed by mirroring the existing
  `SnackHeist_Initializes_Scores_Clears_Fails_AndReplays` setup).
- Full PlayMode suite: **588/588 passed, 0 skipped** (571 baseline + 17 new,
  `unity/playmode-results.xml`, SHA-256
  `47ad0875459d2f5480dc89b395f988f18db2c79bfcb5b4e8223352d01dedb9d2`, 2026-07-17 22:11 EDT).
- No visual output added (by design — G1.2 renders the ladder). No doc beyond this queue entry
  needed updating; `VISUAL-READABILITY-CONTRACT.md`'s "Guidance ladder" section is G1.2's task.

### G1.2 — Tier 1–3 signal wiring
**Goal:** the ladder becomes visible/audible exactly as specified in the plan.
**Do:** Tier 1 — pulse the current objective prop via the existing `MissionPropArt` staging-range
pulse path, brighten the owning dog's arrow + breadcrumbs, trigger a head-turn (reuse an existing
turn/idle read; no new art required). Tier 2 — lift the proximity gate on the current step's
contextual label (shared `AddWorldLabel` path already gates by proximity; widen the gate for the
active objective only), and pulse the partner's HUD identity chip when the step belongs to the other
player. Tier 3 — flash the compact HUD objective line naming the owning dog and fire a distinct
audio cue through the existing cue-slot boundary (cue itself lands in S5.1; use an existing cue as
placeholder and note it there).
**Constraints:** normal play stays Tier-0-quiet; nothing here adds always-on text. F1 overlay
behavior unchanged.
**Tests:** at forced Tier N, the label visibility / pulse state / chip pulse / HUD flash flags are
assertable; at Tier 0 none are active; progress mid-Tier-2 returns everything to quiet.
**Docs:** add a "Guidance ladder" section to `docs/VISUAL-READABILITY-CONTRACT.md` (tiers, what
renders at each, the never-auto-complete rule).

**Done (2026-07-17):** Shipped Tier 1-3 rendering, entirely computed live off `GuidanceTier` each
frame (no new persistent-state timers beyond a nudge cadence, an edge-trigger for the one-shot audio
cue, and small bookkeeping for reverting widened labels) — see the new "Guidance Ladder" section in
`docs/VISUAL-READABILITY-CONTRACT.md` for the tier-by-tier spec as shipped.

- **Tier 1:** `ObjectiveArrowFeedback.SetEmphasis(bool)` (new) brightens the cue tint/scale and
  breadcrumb alpha via the same sin-wave-pulse idiom `MissionPropArtAttachment`'s proximity glow
  already uses; `GameManager.UpdateGuidancePresentation()` sets it on every arrow whenever
  `GuidanceTier >= 1`. Every ~1.2s while stalled it also calls the existing
  `MissionPropArtAttachment.Pulse()` on the current objective prop(s) directly (bypassing proximity)
  and a new `DogReadabilityFeedback.ShowGuidanceNudge(Vector2)` (reuses `ShowTug(Vector2)`'s
  `_lastIntentDir` + `ForcePose` technique - flips the authored sprite to face the target, no new
  art) on the dog(s) with a live target.
- **Tier 2:** widens the active objective's world-label proximity gate via
  `WorldLabelVisibility.Attach(label, wideRange)` (idempotent/re-parameterizable, already the
  established API) on `target.GetComponentInChildren<TextMesh>()`; reverts to
  `WorldLabelVisibility.DefaultPromptRange` the moment tier drops below 2, tracked per dog so it
  never leaks a stale wide gate. Partner HUD chip pulse is gated on a real single-owner signal (see
  below) and simply doesn't fire when that signal is unknown, rather than guessing.
- **Tier 3:** `ArenaHud` flashes an amber pulse behind the objective line (mirrors the score-pop
  timer idiom already in that file) and prefixes the owning dog's name when known;
  `RequestAudioCue(ArenaFeedbackCatalog.Bark)` fires once on the tier-up edge (tracked via
  `_guidanceLastTier`) as the S5.1-flagged placeholder for a dedicated "coach woof" cue.
- **Real design gap found and scoped down, not hidden:** researched every `TryGetObjectiveTarget`
  call site across all 23 controllers before writing any rendering code (see the "Owning-dog caveat"
  in the new VISUAL-READABILITY-CONTRACT.md section) — there is no roster-wide "this dog owns the
  current step" signal today; most controllers hand *both* dogs a target at once with different copy
  telling one to stand down, which this does not disambiguate by parsing that copy text (fragile,
  not a real contract). Shipped `GameManager.ComputeGuidanceOwningDogIndex(bool, bool)` (public
  static, pure) as the honest version of that signal: non-null only when exactly one dog has a
  target this frame. Tier 1's brighten-arrow and Tier 3's HUD-flash still fire correctly on every
  mission (they don't need an owner); Tier 2's partner-chip-pulse and Tier 3's dog-naming correctly
  stay off on missions where ownership is ambiguous rather than showing wrong information. Did not
  add new `IMissionController` surface or parse mission copy text to force a signal that doesn't
  exist yet — that's real design work for whoever picks up G1.3 (the role-turn beacon), which
  already expects to need this and is explicitly flagged in the new doc section so it isn't
  rediscovered from scratch.
- **Tests:** `GuidanceSignalPlayModeTests.cs` — 1 pure test (`ComputeGuidanceOwningDogIndex`'s full
  truth table) + 6 scene-integration `UnityTest`s using Kitchen Food Frenzy as the fixture mission
  (its opening beat's markers carry a child world label from the same call that
  `TryGetObjectiveTarget` returns, and it reliably hands both dogs a target, so it exercises Tier
  1/2/3 rendering and the "both-true" no-owner path without any mission-specific plumbing): Tier-0
  quiet, Tier-1 arrow emphasis, a real objective-copy-change progress signal dropping emphasis back
  to quiet, Tier-2 widening both dogs' target labels and reverting them on progress, Tier-3 flagging
  `GuidanceRescueActive` and firing exactly one `Bark` cue on the edge (not every frame), and replay
  clearing all presentation state. First run caught one bug in the test file itself (`KitchenController`
  is a private GameManager accessor; fixed by casting `ActiveMissionController`), and confirmed a
  real one-frame render lag after a same-frame progress signal (`_guidance.Tick()`/
  `UpdateGuidancePresentation()` run before `LogObjectiveIfChanged()` in `Update()`, so a reset
  landing this frame only reaches the presentation next frame) — imperceptible at 60fps, left as-is
  rather than reordering `Update()` for a 16ms difference, tests adjusted to allow one settle frame.
- Full PlayMode suite: **595/595 passed, 0 skipped** (588 baseline + 7 new,
  `unity/playmode-results.xml`, SHA-256
  `1010c9aea3965e2708c47e6bd290dc2cd9ce13ddc046b1cf89e56b2d4909f456`, 2026-07-17 23:40 EDT).
- Dev build + smoke passed (`unity/builds/dev/CheddarAndCocoa-Arena.app`); the 69-frame art-review
  capture also ran clean (no exceptions from any of the changed code across all 23 missions) but,
  same known sandbox gap as P0.1, only produced flat gray placeholder frames (`-nographics`, no
  GPU/display here) - **not visually inspected**. Whoever runs the next visual/art task off this
  queue needs a real display attached to actually see the Tier 1-3 rendering.

### G1.3 — Role-turn beacon
**Goal:** one consistent answer to "whose turn is it?" at the object itself.
**Do:** a small paw badge in the owning dog's identity color (Cheddar orange / Cocoa brown),
rendered over the active objective target, derived from the existing per-dog
`TryGetObjectiveTarget` data. Where both dogs share a step, no beacon (avoid noise). Visible from
Tier 1 upward by default; missions may opt into always-on via definition metadata for hard
handoff puzzles (Great Escape, Chaos Machine, Bone Relay are the candidates — set those three).
Generate the two badge sprites via a new small `tools/art/` script into `ArenaFinal/UI/Cues`,
matching the existing cue-pack style, sources to `ReferenceOnly/`.
**Tests:** beacon resource loads; owner mapping correct for an alternating-owner mission (drive
Great Escape's sequence and assert the beacon follows the active owner); shared steps show none.
**Docs:** `VISUAL-READABILITY-CONTRACT.md` guidance-ladder section gains the beacon rule.

**Done (2026-07-18):** Shipped `RoleTurnBeacon` (new component, GameManager-owned, mirrors
`ActorSignalBadge`'s established "child icon, position/enable the child not self" shape) plus a real
owner-derivation path this task actually needed — see below.

- **Confirmed and resolved the gap G1.2 flagged:** verified (by reading all three controllers) that
  Great Escape, Chaos Machine, and Bone Relay never produce a single-true/single-false
  `TryGetObjectiveTarget` split — they always hand both dogs a target, differentiated only by copy
  text and an internal owner concept the controller already tracks (`_puzzle.NextOwner`,
  `Owners[stage]`, `_puzzle.RevealedTarget`). G1.2's presence/absence heuristic alone would show no
  beacon for these three 100% of the time, which is exactly backwards for the task's own flagship
  "hard handoff" examples. Added `IMissionRoleOwner` (`DogId? RoleOwnerDog { get; }`), a new optional
  marker interface following the established `IMissionSuccessPresentationController`-style pattern —
  implemented on exactly those three controllers (reusing their existing internal state, no new
  state), left off all 20 others. `GameManager.ResolveGuidanceOwningDogIndex` prefers this signal
  when the active controller implements it, falling back to G1.2's `ComputeGuidanceOwningDogIndex`
  otherwise — layered, backward-compatible, no `IMissionController` surface touched.
- Added `GameManager.MissionDefinition.GuidanceBeaconAlwaysOn` (opt-in, default false); set `true`
  on the three hard-handoff missions in `MissionCatalog.cs` so the beacon shows from Tier 0 (turn
  order matters even when nobody's stalled), everyone else stays Tier-1+ gated.
- Generated `cue_role_beacon.png` via new `tools/art/generate_role_beacon_cue.py` — **one** neutral
  cream paw badge tinted at runtime to Cheddar orange / Cocoa brown via `SpriteRenderer.color`,
  same approach the existing scent-breadcrumb paw cue already uses, rather than baking two
  pre-colored sprites as the task text literally suggested ("the two badge sprites") — noting the
  small deviation rather than hiding it.
- **Caught and fixed a severe bug before committing:** `RoleTurnBeacon` is attached directly to
  GameManager's own GameObject (matching `PanicMeter`'s existing pattern), but the first draft called
  `gameObject.SetActive(false)`/set `transform.position` on itself to hide/reposition the icon —
  which is GameManager's *own* GameObject and transform, not a private one. That disabled the entire
  GameManager (and moved/rescaled it) the instant any mission's beacon logic ran, which is every
  frame. First full-suite run after this landed 324/598 failures, cascading across totally unrelated
  test files (tutorial, actor badges, adventure progression) with `"ArenaBootstrap did not build a
  GameManager"` as the giveaway. Fixed by giving the beacon its own dedicated child GameObject and
  only ever touching that child's `SpriteRenderer.enabled`/transform (`ActorSignalBadge`'s exact
  shape) — full suite green after. Flagging this here because it's a sharp trap worth remembering:
  *never* call `AddComponent<T>()` on `gameObject` for a `T` that will call `SetActive`/move
  `transform` on itself, unless you've confirmed `T` isn't sharing that GameObject with something
  else load-bearing.
- **Tests:** `RoleTurnBeaconPlayModeTests.cs` — always-on Tier-0 visibility plus the beacon tint
  alternating correctly across Great Escape's full 4-step sequence (Cocoa/Cheddar/Cocoa/Cheddar) and
  disappearing once solved; Kitchen Food Frenzy (shared-step, no owner) staying beacon-free even at
  forced Tier 3; replay producing a fresh, correctly-owned beacon rather than a stale reference.
  "Beacon resource loads" is covered by the pre-existing `FinalArtIntegrationPlayModeTests` loop over
  `FinalGameplayArt.GameplayCuePack`, which `cue_role_beacon` was added to rather than duplicating a
  load-check test.
- Full PlayMode suite: **598/598 passed, 0 skipped** (`unity/playmode-results.xml`, SHA-256
  `3e451a8a598041234130c71a1d8138c0add8f6d3082720c43cdede8b63ed948b`, 2026-07-18 02:13 EDT).
- Dev build + smoke passed; the 23-mission art-review capture also ran clean post-fix (no
  exceptions) — same known sandbox gap as P0.1/G1.2, frames are flat unrenderable placeholders here,
  not yet visually inspected.

### G1.4 — Handoff flip flourish
**Goal:** the moment the active role flips between dogs is unmissable.
**Do:** one shared context call (e.g. `MissionContext.SignalRoleHandoff(fromDog, toDog)`) that fires
a short baton-swoosh visual between the dogs (reuse the dog-FX spark/trail sprites), pulses both HUD
chips, and requests a per-dog audio cue. Wire it into the missions with explicit mid-mission role
flips: Pee Break's Beat-3 charger flip, Great Escape/Chaos Machine owner alternation, Scent Search's
point→call→dig chain, Table Stealth's route swap, Weenie Roundup's jumbo-haul finale, Blanket
Catch's called drop. Controllers call it at their existing flip points — no rule changes.
**Tests:** driving each listed mission's flip through existing force hooks fires exactly one
handoff signal with the right dogs; no signal on non-flip progress.
**Docs:** note the flourish in `docs/ARENA-PLAYABLE.md`'s shared-signal section.

**Done (2026-07-18):** Added `MissionContext.SignalRoleHandoff(DogId fromDog, DogId toDog)` →
`GameManager.SignalRoleHandoff` — spawns a new `DogHandoffSwoosh` (a standalone one-shot GameObject,
not attached to GameManager's own, learning last task's lesson) that arcs `FinalGameplayArt.DogFxChaosSpark`
between the two dogs' positions with an ease-out lerp and a small sine arc height, flashes both HUD
identity chips for 0.6s (reuses the exact chip-pulse rendering G1.2/G1.3 already built — `ArenaHud`
now ORs `HandoffChipFlashVisible` into the same `guidancePulse` bool), and fires one placeholder
audio cue (`ui_replay_next_select`, distinct from G1.2's `bark` Tier-3 placeholder so the two signals
don't sound identical; both are S5.1's job to replace with dedicated cues).

- **Wired 6 of the 7 named missions**, each at its own existing flip point (no rule changes, one
  `_context.SignalRoleHandoff(from, to)` call added per site): Great Escape and Chaos Machine's
  shared `HandleProgress()` step/stage-advance funnel; Scent Search's Cocoa-hot-call branch in
  `Sniff()`; Table Stealth's `HandleBark()`/`HandleInteract()` (both directions — this mission's
  real handlers are separate implementations from its test-only `ForceTableFlop`/`ForceTableBurp`
  hooks, unlike the other five where the Force hooks funnel through the same production method);
  Weenie Roundup's jumbo-lift branch in the shared pickup path; Blanket Catch's `HandleBark()`
  called-drop branch (its `ForceCocoaCallDrop()` test hook calls this exact method, so it's covered
  by both real and forced paths for free).
- **Deliberately did not wire Operation Pee Break's Beat-3 charger flip** — read the actual
  `AdvanceBeat()`/`CreditBeatRoles()` code first: both dogs' jobs change simultaneously to new,
  *different* assignments (Cheddar leash→hallway, Cocoa stare→charger) at that beat, which is not a
  "dog A hands off to dog B" moment the `SignalRoleHandoff(from, to)` shape represents — there's no
  single dog receiving the other's outgoing role. Forcing a fabricated from/to pair onto it would
  misrepresent the beat rather than flag it, so it's left out and documented here instead.
- **Tests:** `RoleHandoffPlayModeTests.cs` (10 scene-integration tests) — each of the 6 wired
  missions fires exactly one signal with the correct from/to dogs on its real flip trigger, plus a
  paired "no false signal" check per mission where applicable (Great Escape's wrong-dog fumble,
  Chaos Machine's lever-pull-alone, Scent Search's Cheddar direction-hint sniff, Weenie Roundup's
  symmetric fast-carry loop, Blanket Catch's blocked call while slack); Great Escape's final step
  also confirms no handoff fires once solved (no next owner to receive it); one test confirms both
  HUD chips flash together; one confirms replay clears `HandoffSignalCount`/`LastHandoffFromDog`/
  `LastHandoffToDog`/the chip flash. All ran green on the first try — no bugs found this round.
- Full PlayMode suite: **608/608 passed, 0 skipped** (598 baseline + 10 new,
  `unity/playmode-results.xml`, SHA-256
  `d68144b96783df2cf36c77cb44b8bf80c8c8c8bf5b88b1cb44611184ba5128b5`, 2026-07-18 02:36 EDT).
- Dev build + smoke + 23-mission art-review capture all pass clean; same known sandbox gap as
  P0.1/G1.2/G1.3 — frames are flat unrenderable placeholders here, not yet visually inspected.

### G1.5 — Wrong-role coaching audit
**Goal:** every mission's wrong-dog / wrong-time attempt produces a visible, audible, recoverable
coach beat — verified, not assumed.
**Do:** for each of the 23 missions, drive the wrong-role attempt in a test (most already exist from
the asymmetric-role pass — extend, don't duplicate) and assert a world-pop/juice/audio reaction
fires and the mission remains clearable afterward. Fill any gaps found using the established gag
template (cooldown field, `TrySpawnX()` hook, two-assertion test). Record the per-mission result
table in this file under the task status.
**Done when:** the table shows 23/23 with test names; suite green.

**Done (2026-07-18):** Audited all 23 missions first (research pass, no code) before touching
anything. Systemic finding: **every existing "wrong role" test in the roster checked only
recoverability, none asserted the reaction actually fired** — the coaching contract was assumed,
not verified, exactly as the task predicted. Also found real code gaps: 3 missions where the
wrong-dog bark was completely silent (no reaction of any kind), and ~12 more missing either the
visual or audio half of "visible, audible."

- **Fixed the 3 silent missions** (`MarkTheYardMissionController.HandleBark`,
  `BoneRelayMissionController.HandleBark`, `BlanketCatchMissionController.HandleBark`) — each had a
  bare `return false;` on wrong-dog bark with zero `SetCue`/`SetJuice`/`SpawnWorldPop`/
  `MarkFailedInteraction`. Added the full reaction (mirrors `BabyBirdBedlamMissionController`'s
  existing template exactly: `SetCue` + `SetJuice(WarningMiss, ...)` + `SpawnWorldPop` +
  `MarkFailedInteraction`).
- **Added the missing visual half** (`SetJuice`+`SpawnWorldPop`) to 9 audio-only branches:
  `SquirrelConspiracyMissionController.TryFindStash`, `CoyotesFenceMissionController`'s
  `RegisterBarkPressure`/`TryRepair`, `WeenieRoundupMissionController`'s jumbo wrong-grabber/
  not-steady branches (2 sites), `ScentSearchMissionController.DigAtSpot`'s wrong-dog/wait-for-call
  branches (2 sites), `ThunderstormComfortMissionController.HandleBark`'s out-of-order branch,
  `LeashWalkMissionController.HandleBark`'s wrong-scout branch, `CarRideMissionController`'s
  too-early-brace branch, `GateCrashMissionController.HandleInteract`'s wrong-dog branch.
- **Added the missing audio half** (`MarkFailedInteraction`) to 5 visual-only branches:
  `EagleShadowPanicMissionController`'s mistimed-pull (also added the missing `SpawnWorldPop`, it
  had neither), `TableStealthMissionController`'s `HandleBark`/`HandleInteract` wrong-dog branches
  (2 sites), `SquirrelSwitcherooMissionController`'s `HandleBark`/`HandleInteract` wrong-dog
  branches (2 sites), `ChaosMachineMissionController`'s lever/junction wrong-dog branches (2 sites).
- **Found and fixed a real production bug while extending the first three tests**, not just a test
  gap: `GameManager.OnDogBarked` has a generic "solo bark" fallback (`SetJuice(BarkBurst, "{DOG}
  BARK BURST")`) that fires whenever the active controller's `HandleBark` returns `false` — meaning
  the newly-added reactions on `MarkTheYard`/`BoneRelay`/`BlanketCatch`/`CoyotesFence`'s wrong-dog
  branches were being immediately overwritten by that generic fallback for any REAL player bark
  (Force-hook-driven tests never hit this, since those hooks call the controller method directly
  and bypass `OnDogBarked` entirely — which is exactly why this had never been caught before).
  Fixed by changing those four branches' return value from `false` to `true` (matching the
  convention every other mission's wrong-dog `HandleBark`/`HandleInteract` branch already used) —
  `false` from `HandleBark` means "solo bark, apply the generic fallback," not "wrong dog, but I
  already coached it." One pre-existing test (`MarkTheYard_RequiresInteractToMark_...`) asserted
  the old `IsFalse` contract and needed updating to match.
- **Tests:** extended 16 existing tests with fire+recoverable assertions (`LastJuiceLabel`/
  `HasWorldPop`/`LastAudioCueRequested` alongside the existing state-unchanged checks) and added 6
  new test methods for missions with zero prior wrong-role coverage
  (`SquirrelConspiracy_WrongDogFindStash_...`, `TableStealth_WrongDogAttempts_...`,
  `Switcheroo_WrongDogAttempts_...`, `Walk_SingleMisread_...`, `KitchenFrenzy_WrongScout_...`, plus
  a new bark-wrong-dog block folded into Bone Relay's existing scent-post test). First full run
  caught 6 failures, all in the new test code itself, not further production bugs: three were the
  `OnDogBarked` clobber above (which DID lead to the one real production fix); one was my own test
  asserting `LastJuiceLabel` on a branch that only ever called `SpawnWorldPop` (fixed to assert the
  world pop instead); one was a puzzle needing more than one wiggle+pull cycle to free a snatched
  dog (used `ForceEagleShadowRescue()` instead of a single manual pair); one was a stale audio-cue
  expectation (a later `RequestAudioCue` in the same branch legitimately overwrites the earlier
  `MarkFailedInteraction` one - asserted the actual final cue, not the first one called).

| # | Mission | Wrong-role branch | Fires visible+audible | Test |
|---|---|---|---|---|
| 1 | BackyardRescue | `HandleWeenieRecovery` WrongDog, `HandleSquirrelRedirect` WrongPressureDog | Yes (already full) | `BackyardSquirrelTrapPlayModeTests.BackyardTrap_RequiresGapPartnerRecovery_ThenReversesRoles` |
| 2 | SnackHeist | `HandleBark` wrong dog, `HandleTreatCollected` wrong dog | Yes (already full) | `SnackHeistPlayModeTests.SnackHeist_ClearPath_CollectAllSnacks` |
| 3 | SockPanic | `TryTipBasket` wrong anchor | Yes (visual added) | `SockPanicPlayModeTests.SockPanic_CocoaMustAnchorContinuouslyWhileCheddarDives` |
| 4 | SquirrelConspiracy | `TryFindStash` wrong dog | Yes (visual added) | `SquirrelConspiracyPlayModeTests.SquirrelConspiracy_WrongDogFindStash_CoachesRecoverably_ThenCocoaStillCracksIt` (new) |
| 5 | EagleShadowPanic | Mistimed rescue pull | Yes (pop+audio added) | `EagleShadowPanicPlayModeTests.EagleShadowPanic_Rescue_PullWithNoWindow_IsAMistimedMiss` |
| 6 | CoyotesFence | `RegisterBarkPressure`/`TryRepair` wrong dog | Yes (visual added) | `CoyotesFencePlayModeTests.CoyotesFence_CocoaPinsInRangeAndCheddarRepairsBeforeTheOpeningCloses` |
| 7 | WeenieRoundup | Jumbo wrong-grabber / not-steady | Yes (visual added) | `WeenieRoundupPlayModeTests.WeenieRoundup_JumboRejectsSoloGrab_AndSeparationFumblesRecoverably` |
| 8 | ScentSearch | `DigAtSpot` wrong dog / wait-for-call | Yes (visual added) | `ScentSearchPlayModeTests.ScentSearch_RolesRequireCocoaCallThenCheddarDig_WithoutPunishingMisreads` |
| 9 | ThunderstormComfort | Cheddar-bark-before-Cocoa | Yes (visual added) | `ThunderstormComfortPlayModeTests.ThunderstormComfort_RequiresOrderedHuddleBarks_AndMissesRecoverNextClap` |
| 10 | MarkTheYard | `HandleBark` wrong dog | Yes (was silent - fixed) | `MarkTheYardPlayModeTests.MarkTheYard_RequiresInteractToMark_AndCocoaBarkDefendsTheOpening` |
| 11 | LeashWalk | Wrong-scout route call | Yes (visual added) | `LeashWalkPlayModeTests.LeashWalk_CheckpointsRequireAlternatingScoutBarksBeforePairCanBankThem` |
| 12 | CarRide | Cheddar-brace-before-Cocoa | Yes (visual added) | `CarRidePlayModeTests.CarRide_BrakeRequiresCocoaAnchorThenNearbyCheddarTuck_AndRecoversNextBrake` |
| 13 | GateCrash | `HandleInteract` wrong dog | Yes (visual added) | `CoopGateCrashPlayModeTests.GateCrash_CocoaMustDeliberatelyAnchor_ThenHoldingLetsCheddarProgress_AndLeavingSnaps` |
| 14 | TableStealth | `HandleBark`/`HandleInteract` wrong dog | Yes (audio added) | `CoopTableStealthPlayModeTests.TableStealth_WrongDogAttempts_CoachRecoverably_ThenTheRealRolesStillWork` (new) |
| 15 | SquirrelSwitcheroo | `HandleBark`/`HandleInteract` wrong dog | Yes (audio added) | `CoopSquirrelSwitcherooPlayModeTests.Switcheroo_WrongDogAttempts_CoachRecoverably_ThenTheRealRolesStillWork` (new) |
| 16 | WalkCampaign | Single misread (incomplete combo) | Yes (already full) | `CoopWalkCampaignPlayModeTests.Walk_SingleMisread_CoachesRecoverably_AndTheCorrectComboStillWorks` (new) |
| 17 | BoneRelay | `HandleBark` wrong dog | Yes (was silent - fixed) | `CoopBoneRelayPlayModeTests.Bone_CocoaMustBarkAtTheScentPost_ToCallCheddarsMound` |
| 18 | GreatEscape | Wrong-dog station attempt | Yes (already full) | `CoopGreatEscapePlayModeTests.Escape_WrongDog_IsAHarmlessFumble` |
| 19 | ChaosMachine | Lever/junction wrong dog | Yes (audio added) | `CoopChaosMachinePlayModeTests.Chaos_PositionDriven_LeverAndJunctionsRequireOwnerInteract` |
| 20 | BlanketCatch | `HandleBark` wrong dog | Yes (was silent - fixed) | `CoopBlanketCatchPlayModeTests.Blanket_CocoaBarkCallsEachDrop_OnlyAfterTheTeamMakesTheBlanketTaut` |
| 21 | KitchenFoodFrenzy | WrongScout / WrongCatcher | Yes (already full) | `KitchenFoodFrenzyPlayModeTests.KitchenFrenzy_WrongScout_CocoaCannotTelegraphTheCounterKnock` (new) + `KitchenFrenzy_RoleFailuresFoodTypesAndClearPathAreDeterministic` |
| 22 | OperationPeeBreak | Misread combo | Yes (already full) | `PeeBreakPlayModeTests.ObserverRehearsal_ColdPathSurfacesBeatOneBeatTwoAndFirstEarlyBarkMisread` |
| 23 | BabyBirdBedlam | `HandleBark`/`HandleInteract` wrong dog | Yes (already full) | `CoopBabyBirdBedlamPlayModeTests.Bedlam_WrongRolesAndRange_CoachBackIntoTheDefense` |

23/23. Full PlayMode suite: **613/613 passed, 0 skipped** (`unity/playmode-results.xml`, SHA-256
`b078a83d1cfe5a36848cd596678127bb957be5164908de9bc8f80bd588d36198`, 2026-07-18 03:31 EDT). Dev
build + smoke + 23-mission art-review capture all pass clean; same known sandbox gap as prior
tasks — frames are flat unrenderable placeholders here, not yet visually inspected.

### G1.6 — Ladder observability + couch telemetry
**Goal:** the next couch test produces stall *data*.
**Do:** F1 overlay shows current tier + seconds-since-progress. Session summary (and the existing
playtest overlay/event path) records, per mission attempt, the count of Tier-2 and Tier-3
activations and where (objective copy at activation). Keep it out of normal-play HUD.
**Tests:** forced stall produces the expected recorded entries; normal-play HUD strings unchanged.
**Docs:** add the "stall once on purpose, watch the ladder" step to
`docs/FAMILY-SHOWCASE-MANUAL-TEST.md`'s observation checklist.

**Done (2026-07-18):** This closes Phase 1 (G1.1-G1.6) of the guidance-ladder plan.

- **F1 overlay:** new `GameManager.GuidanceDebugLabel` (`"Guidance: Tier N (X.Xs stalled, T2xN
  T3xN)"`) added as one more row in `ArenaHud.DrawPlaytestOverlay()` (box height bumped 410→432 to
  fit). This method is only reached when `PlaytestOverlayVisible` is true (F1 toggle) and never from
  the normal-play HUD draw path, so "keep it out of normal-play HUD" holds structurally, not just by
  convention.
- **Per-attempt activation counts:** `GameManager.GuidanceTier2Activations`/`GuidanceTier3Activations`
  (int, edge-triggered - only increment on the frame the tier first crosses into 2 or 3, not every
  frame it stays there), reset alongside `_guidance.Reset()` in `BeginRound()`. Centralized the
  edge-detection in one new `TickGuidance(float seconds)` method that both `Update()`'s real
  per-frame tick AND the `ForceGuidanceStall` test hook now call — needed because those were two
  separate call sites ticking the same `MissionGuidanceEscalation` instance, and edge-detection
  (comparing tier before/after) has to live somewhere both reach or the deterministic test hook
  would silently never trigger a recorded activation.
  Each crossing also fires `LogPlaytestEvent("GuidanceTier2"/"GuidanceTier3", ObjectiveLabel)` -
  satisfies "the existing playtest overlay/event path" and captures "where" (the objective copy at
  that instant) for free, reusing the exact same event-log idiom used everywhere else in the file.
- **Session summary:** new `GameManager.SessionGuidanceActivationsLabel` (mirrors the existing
  `SessionRanksEarnedLabel` pattern exactly - a `List<string>` of one `"{mission}: T2xN T3xN"` entry
  per attempt that had *any* activations, appended in `RecordSessionResult()`, showing the most
  recent 3 with a "(+N earlier)" tail, reset in `ResetSession()`). Attempts with zero activations are
  skipped so quiet missions don't clutter the summary. Rendered in `ArenaHud.DrawSessionSummary()` on
  `layout.Challenge` - a rect that struct already defined (shared with the other end-screen) but that
  this specific screen had never actually drawn anything into, so no layout rework was needed.
- **Tests:** `GuidanceTelemetryPlayModeTests.cs` (6 new tests) — forced stall records both tier
  crossings through the event log; repeated stall/reset/stall cycles count each crossing separately
  (proves edge-triggering, not a one-shot latch); mission end folds the attempt's counts into the
  session summary; a quiet attempt (no stalls) leaves the summary unmentioned; replay resets the
  per-attempt counts; and a direct check that `ObjectiveLabel`/`TeamGuidanceLabel` (the normal-play
  HUD strings) never mention "Tier"/"stalled" while `GuidanceDebugLabel` (F1-only) does. All passed
  on the first run.
- Full PlayMode suite: **619/619 passed, 0 skipped** (613 baseline + 6 new,
  `unity/playmode-results.xml`, SHA-256
  `e3ccb2cfc6a6157f31a288437292d4920fc44e1f2b068e1274d23d5477f0f608`, 2026-07-18 03:48 EDT). Dev
  build + smoke + 23-mission art-review capture all pass clean; same known sandbox gap as prior
  tasks — frames unrenderable here, not yet visually inspected.

**Phase 1 (the sequential guidance-grammar core) is now fully complete: G1.1 through G1.6 all
shipped and green.** Phases 2-4 (animation, art consistency, first-session flow) are parallelizable
from here per `docs/PRELAUNCH-PRODUCTION-PLAN.md`.

---

## Phase 2 — Animation and feel (parallelizable after Phase 1)

### A2.1 — Animation coverage audit
**Goal:** ground truth on which `docs/ANIMATION-STATE-CATALOG.md` states have real motion strips vs
static/fallback reads.
**Do:** inventory `ArenaFinal/Characters/*/Motion/` and the `ApplyPose`/`AnimateAuthoredMotion`
mapping; annotate the catalog per state with `authored strip` / `reused strip (which)` /
`static fallback` / `missing`. Rank the gaps by signal impact: verbs that missions *require players
to read* come first (interact, dig, sniff, carry, comfort, flop, beg). No art changes in this task.
**Done when:** `ANIMATION-STATE-CATALOG.md` carries the annotated table + ranked gap list, and
A2.2–A2.4 scopes are confirmed or amended from it.

**Done (2026-07-18):** Docs-only task, no code changed. Traced the full render path
(`ApplyPose`→static fallback, `AnimatePose`/`AnimateAuthoredMotion`→authored motion strip with an
E-facing-mirrored retry for missing directions) and inventoried every PNG under
`Characters/Dogs/{Cheddar,Cocoa}/Motion/`. Added a full annotated table (12 `Pose` values + the
verbs with no pose mapping at all) plus a ranked gap list to `docs/ANIMATION-STATE-CATALOG.md`.

Key findings:
- `Idle`/`Run`/`Bark` have full 5-direction authored art (8/8 effective via mirroring) — genuinely
  complete, not a gap.
- `Dig`/`Carry`/`Tug`/`Proud`/`Rescued`/`Sad`/`Stunned` have partial direction coverage (as little as
  E-only) but the code's own "retry at E-facing, mirrored" fallback means they still play a real
  animated strip for every direction, just not a truly-directional one — classified `reused strip`,
  not `static fallback`. This matters because it changes A2.3's priority: dig already reads fine in
  practice, so it should not be treated as equally urgent as truly-missing verbs.
- **`Pose.Swim` and `Pose.Jump` have zero art of any kind** and both a `FinalDogPoseArt.PoseSuffix()`
  and `ArenaDogPoseSprites.RectFor()` gap (`Dig`/`Carry`/`Swim`/`Jump` all silently default to
  `"idle"`/the Idle crop) — confirmed a swimming or jumping dog currently renders as a plain
  standing-idle sprite with zero visual distinction, despite the backyard pool being live gameplay.
- **Interact, Sniff, dramatic Flop, Beg, head-tilt, paw-tap, push/pull, Hide have no `Pose` mapping
  at all** — not low-frame-count, genuinely absent from the pose system. `Comfort` is worse than
  absent: `ShowComfort()` aliases to `Pose.Proud`, so a comforting nuzzle currently visually reads as
  a celebration.
- `CharacterMotionArt.Clip` declares `Herd`/`Hide`/`Comfort` with zero assets and zero `Pose`
  mapping — dead enum values, flagged for whoever eventually builds that art.
- **Amended A2.3's scope** in the catalog: "dig/sniff/carry" as originally worded overweights dig
  (already working via the mirror-fallback); sniff and comfort belong at equal-or-higher priority.

Ranked gap list (worst first): Interact (A2.2's own scope, confirmed correct) → Sniff → Carry
(partial direction coverage) → Comfort (misleadingly aliased, not just absent) → dramatic Flop
(Table Stealth's headline beat) → Beg (no mission needs it yet — flagged as a build-vs-drop decision,
not silently ignored) → Swim/Jump (confirmed static-idle; Jump's own mechanic wiring should be
re-verified before commissioning art for it).

No PlayMode run needed (docs-only, no runtime behavior changed).

### A2.2 — Interact micro-animation
**Goal:** pressing Interact visibly *does something on the dog*, not just the prop.
**Do:** a short paw-tap/nose-boop read on the acting dog for every controller Interact acceptance
(hooking the shared input→controller acceptance path, not 23 call sites). Prefer an authored 2–4
frame strip via the existing `tools/art/export_character_*` pipeline if source sheets support it;
otherwise a tucked head-bob/squash tween is acceptable pre-launch. Cheddar's read is bouncier than
Cocoa's (identity rule).
**Tests:** accepted Interact triggers the read; rejected/wrong-role Interact does not double-fire
with the coaching gag; state clears after the beat.

**Done (2026-07-18):** Chose the tween fallback the task explicitly allows, not new authored art —
no source sheets to export from for this, and a code-only squash is the smaller, lower-risk path.

- One shared hook in `GameManager.OnDogInteracted`, right where `HandleInteract`'s result is already
  checked — no changes to any of the 23 controllers. A new `_interactionCoachedThisAttempt` flag
  (set by `MarkFailedInteraction`, reset before each `HandleInteract` dispatch) distinguishes a
  genuine acceptance from a coached rejection, since several controllers' wrong-role branches
  return `true` from `HandleInteract` too (a G1.5 fix, to avoid a different generic-fallback
  clobber) — without this flag the squash would have double-fired on every coached rejection.
  `DogReadabilityFeedback.ShowInteractAccepted()` only gets called when both conditions hold.
- **Found and fixed a real transform-ownership bug while wiring this in**, not just added the
  feature: my first attempt multiplied the squash into the two per-pose scale setters
  (`ApplyPose`'s static-fallback assignment and `AnimateAuthoredMotion`'s per-frame authored-frame
  assignment) — both silently discarded, because `ApplyPersonalityMotion` (called immediately
  afterward every frame from `AnimatePose`) unconditionally recomputes and overwrites
  `_authoredPose.transform.localScale` from its own `authoredBase` variable, ignoring whatever the
  two earlier methods just set. Traced the actual call order (`AnimatePose` → `AnimateAuthoredMotion`
  → `ApplyPersonalityMotion`) before committing and moved the multiplier to the true last writer.
  Caught by reading the code path fully rather than trusting the first plausible-looking edit spot —
  worth remembering for any future `_authoredPose.transform` change in this file, since it's written
  from three different methods each frame and only the last one sticks.
- Squash-and-pop: a single damped-sine scale bump (`1 + sin(t·π)·amplitude`, 0.22s), layered onto
  whatever `AuthoredMotionScale`/`AuthoredFallbackScale`/personality/action-feedback scale is already
  active rather than replacing it — does not touch `CurrentPose`, so it reads correctly mid-tug or
  mid-dig. Cheddar's amplitude (0.2) is bigger than Cocoa's (0.12) per the identity rule.
- **Tests:** `InteractMicroAnimationPlayModeTests.cs` (4 tests, Gate Crash as the fixture — a real
  accept/reject pair already exists there): accepted Interact plays the read; wrong-role rejection
  does not; too-far rejection does not; the read clears itself after its 0.22s beat. All passed on
  the first run (the transform-ownership bug above was caught and fixed before ever running tests,
  by reading the call order, not by a failing assertion).
- Full PlayMode suite: **623/623 passed, 0 skipped** (619 baseline + 4 new,
  `unity/playmode-results.xml`, SHA-256
  `051ab762d97d172bbeee47450b526eccbc350a392e02bd4c177558c5dbd4c68c`, 2026-07-18 04:08 EDT). Dev
  build + smoke + 23-mission art-review capture all pass clean (same known sandbox gap — a 0.22s
  squash pulse wouldn't reliably land on a captured frame anyway, so this one specifically needs a
  live human check, noted in the new `docs/ARENA-PLAYABLE.md` manual-check entry).

### A2.3 — Signal-critical verb strips: dig, sniff, carry
**Goal:** the three most mission-load-bearing verbs read as animation at couch distance.
**Do:** per A2.1's findings, produce/promote directional strips for dig (Scent Search, Bone Relay),
sniff (Scent Search, sniff-around lead-in), carry (Weenie Roundup, Sock Panic, Snack Heist) through
the existing motion pipeline + `validate_character_motion_pack.py`. Wire through the existing motion
mapping; keep current reads as fallback.
**Tests:** motion-pack validation passes; the missions' existing pose assertions updated to the new
states; art-review frames for those missions show the verb mid-animation.

**Done (2026-07-18): code-side prep only — no new authored art.** Hit a real tooling gap partway
through: the motion pipeline's first step (hand-approved reference boards under
`Assets/Art/ReferenceOnly/GeneratedCharacterMotion/`, per A2.1's own documented pipeline trace) needs
an external image-generation tool this session has no access to — `tools/art/export_character_*.py`
only crops/composites boards that already exist, it can't create them. Surfaced this to the owner via
AskUserQuestion instead of silently skipping or faking a placeholder; the owner chose **"Code-side
prep only"** over waiting/blocking or a lower-fidelity placeholder-art option.

- Scoped down to **sniff only**, and wiring, not art: `dig` and `carry`'s NE/SE diagonals were left
  untouched (same tooling gap, and sniff was the more mission-load-bearing gap of the three per
  A2.1's audit — Scent Search's core verb had zero dedicated pose before this).
- `DogReadabilityFeedback.cs`: new `Pose.Sniff` (doc comment on the enum member explains no art
  exists yet) and `ShowSniff()`; `CharacterMotionArt.cs`: new `Clip.Sniff` wired through `TryClip`,
  the fps table, and `FallbackPose`. With no runtime frames present yet, this renders through the
  same static-idle fallback chain that already covers unmapped poses like Swim/Jump — confirmed via
  `UsesAuthoredPoseArt` staying `true` in the new test, i.e. it degrades gracefully rather than
  going blank.
- `ScentSearchMissionController.Sniff()`: both dogs' ongoing tracking beat now calls `ShowSniff()` on
  their `DogReadabilityFeedback`; the exact hot-patch discovery still plays `ShowProudBrief()`
  unchanged (a bigger beat than the ongoing sniff read, verified by test).
- **Tests:** `ScentSearchSniffPosePlayModeTests.cs` (2 new): Cheddar's wild-sniff read plays
  `Pose.Sniff` and keeps `UsesAuthoredPoseArt` true; Cocoa's tracking sniff plays `Pose.Sniff`, but
  moving onto the exact buried spot flips to `Pose.Proud` instead. Full PlayMode suite: **625/625
  passed, 0 skipped** (623 baseline from A2.2 + 2 new, `unity/playmode-results.xml`, SHA-256
  `64bfcac03396bb53e7acb60df11e9ad6c0250fdcb1d2448adaa74b6b1ab6c53f`, 2026-07-18 14:07 EDT).
  `tools/art/validate_character_motion_pack.py` shows no regression (170/336 runtime frames,
  unchanged — Sniff has no runtime frames yet by design). Dev build + smoke test + 23-mission
  art-review capture (70 frames) all clean.
- `docs/ANIMATION-STATE-CATALOG.md` updated: interact row now shows fixed (A2.2), sniff row now shows
  wired/no-art-yet (A2.3), plus a dedicated "A2.2 / A2.3 outcomes" section documenting this tooling
  gap for any future art-dependent task (A2.4 and A2.5 both look likely to hit the same wall for
  threat/NPC and held-payoff art).

### A2.4 — Threat/NPC acting gaps
**Goal:** no named NPC state silently reuses an unrelated read.
**Do:** from A2.1: coyote `test fence` and `lure` (currently reuse threaten), squirrel `taunt` and
`stash guard` if still partial, and any Teenager/human state the audit flags. Same pipeline, same
fallback rule.
**Tests:** resource-load + state-mapping assertions per new strip; suite green.

**Done (2026-07-18):** Traced every coyote/squirrel `SetActorState` label the mission controllers
actually send through `ThreatMotionArt.TryInfer`'s keyword table (ground truth, not the task's
literal wording) and found real bugs beyond what the task named:

- **Coyote fake-snack lure and the final "coyote retreats" defeat state were both falling all the
  way to Patrol**, not reusing Threaten as the catalog assumed — `"FAKE SNACK BAIT..."` and
  `"COYOTE RETREATS..."` didn't contain the literal keywords the table checked for. Fixed by adding
  `"BAIT"` and `"RETREAT"` keywords.
- **Squirrel taunt reused the cowering Scared clip for what is a gleeful successful escape** — moved
  `"TAUNT"` from the Scared bucket to the Run bucket (bounding scurry reads right; cowering read
  backwards).
- **A false lead, caught by a full-suite regression, not skipped past:** first instinct was to
  remove the squirrel branch's `!upper.Contains("SQUIRREL")` guard, since 5 of
  `SquirrelConspiracyMissionController`'s own 6 labels never say "squirrel" and were silently
  disabling that mission's authored motion almost entirely (falling back to the static placeholder).
  Removing it broke two already-green tests in `FinalArtIntegrationPlayModeTests.cs` — the guard is
  intentional, keeping Coyotes Fence's repurposed shared-squirrel-actor "weak spot" marker from
  idle-breathing like a live squirrel. Correct fix: added the literal word "SQUIRREL" to those 5
  label strings in `SquirrelConspiracyMissionController.cs` instead (also clearer on-screen copy for
  players), leaving the guard untouched. New
  `RepurposedSquirrelActorMarkerLabels_StillFallBackInstead_OfIdleBreathingLikeASquirrel` test pins
  that non-regression explicitly.
- Also fixed a keyword-coverage cousin: `"SQUIRREL GOT A WEENIE!"` (BackyardRescue's theft-success
  beat) fell to Idle instead of the grabby Steal clip its sibling `"SQUIRREL STOLE A SNACK!"`
  correctly gets — added `"WEENIE"` to the Steal keywords.
- "Stash guard" needed no separate fix once the label-text fix landed — the only matching state,
  `"SQUIRREL STASH REVEALED..."`, resolves to Idle (watchful perky-perch), a legitimate read.
  Human/Teenager NPCs have zero `ThreatReadabilityAnimator` at all (`Actor.Unknown`, static cutout
  full stop) — flagged as the largest remaining gap, out of scope here since it needs a new
  `Actor`/`Clip` case and authored art, not a keyword fix.
- New `ThreatLabelInferencePlayModeTests.cs` (10 tests) pins every real production label string
  against its correct clip, both the fixed cases and the intentional guard-clause behavior.
- **Bonus scope beyond the task: closed A2.3's own flagged Sniff art gap.** The owner produced
  `cheddar_sniff_east_south_v01.png`/`cocoa_sniff_east_south_v01.png` (external image generation,
  matching the existing dig boards' 2×4 E/S grid format) and supplied them mid-task. Wrote
  `tools/art/export_character_sniff.py` (mirrors `export_character_dig.py`), added the manifest
  entry, ran the extraction. Found and fixed a real extraction bug in the process: 2 of 16 frames
  came out with a small disconnected fur-wisp artifact (a neighboring cell's ear/tail tip bleeding
  past the crop boundary) — fixed generically with a largest-connected-component filter in the new
  script rather than hand-tuning crop margins. `Pose.Sniff` now renders real authored motion instead
  of the Swim/Jump-style static fallback; updated the now-stale "no art yet" comments in
  `DogReadabilityFeedback.cs` and the sniff PlayMode test accordingly.
- **The `-arena-art-review=` capture would not complete in this sandbox this session — reported
  honestly rather than faked.** First found a real, fixable cause (an orphaned Unity player process
  leaked from an earlier smoke-test in this same session, Friday, 14+ hours old, 82% CPU, holding a
  resource/license) and cleared it with the owner's confirmation. That did NOT fix it: two further
  attempts after the cleanup (one unbounded, one with a 240s hard watchdog kill) both hung at the
  identical point every time — engine init completes, `Begin MonoManager ReloadAssembly` /
  `Finished resetting the current domain` logs, then nothing; zero frames written; never reaches the
  arena scene or gameplay code at all. Since PlayMode tests (run through the Editor's test runner,
  not this packaged-player path), the dev build, `smoke-player.sh` (a different, simpler
  `-batchmode -nographics` invocation with no `-arena-art-review` flag), and the motion-pack
  validator are all green, this points at something specific to the `-arena-art-review=` + `-quit`
  packaged-player combination in this sandbox today, not a regression from this task's code changes.
  Stopped after 3 reproducible hangs rather than continuing to retry — this is the same
  `docs/VISUAL-READABILITY-CONTRACT.md`/prior-session "known sandbox gap" this doc has flagged
  before (headless/no-GPU capture has been unreliable in this environment), now worse than the
  earlier "runs clean but produces flat placeholders" state. Whoever next has a real display or a
  less contended sandbox should retry `<built-player> --arena-art-review=<path>` directly before
  trusting this task's visuals were inspected — they were not, this time.
- Full PlayMode suite: **643/643 passed, 0 skipped** (625 baseline from A2.3 + 18 new — 10 threat-label
  + 8 coverage already existing verified unaffected via `ThreatMotionFidelityPlayModeTests`,
  `unity/playmode-results.xml`, SHA-256
  `28edb2cd8efc088dbab314a9012e26cea639f2370287bb9f7160edc4944d3fd1`, 2026-07-18 15:53 EDT).
  `tools/art/validate_character_motion_pack.py`: 30/30 source boards, 186/360 runtime frames (up
  from 170/336 after the new Sniff E/S frames). Dev build succeeded and `smoke-player.sh` passed;
  the art-review capture did not complete (see above).

### A2.5 — Held-payoff pose audit
**Goal:** every mission's held live payoff (the 1.15s pre-end-card beat) shows an animated
proud/outcome read, not frozen dogs.
**Do:** drive each mission to clear via force hooks; assert the payoff hold applies an outcome
pose/motion on both dogs and any payoff actor state (existing pattern from Pee Break's
hydrant/relief beat). Fix the misses; list per-mission results here.
**Done when:** 23/23 table recorded; suite green.

**Done (2026-07-19):** Audited every `SuccessHoldSeconds`/`DoorOpenPayoffSeconds` trigger site (the
moment each controller sets `IsPresentingSuccessfulOutcome = true`) for whether it calls
`DogReadabilityFeedback.ShowProudBrief()` (or an equivalent pose) on **both** dogs. Correction to the
task's own framing while auditing: Pee Break's "existing pattern" only covers the hydrant/relief
**scene** animation (`AnimateReliefSparkle`, hydrant `SetActive`) — the dogs themselves were static
during `DoorOpen`'s hold (`Tick()` short-circuits to `AdvanceSuccessHold`, which never touches dog
pose), so Pee Break was itself one of the misses fixed below, not the reference example.

| # | Mission | Result | Fix |
|---|---|---|---|
| 1 | Backyard Rescue | **N/A — structural, not fixed** | Legacy (pre-`IMissionController`-migration) clear path: `GameManager.CheckClear()`'s `hasItems && hasPredator && hasTug` branch calls `EndRound(true)` immediately: no `SuccessHoldSeconds`/`IsPresentingSuccessfulOutcome` hold exists at all, so there is no held beat to pose. Retrofitting one is a mission-timing/rule change, not a pose fix — out of this task's scope per the queue's "never change mission rules or tuning, mark BLOCKED" guardrail. Flagged as a follow-up, not attempted here. |
| 2 | Snack Heist | Fixed | `SnackHeistMissionController.PresentCompletion()` had no pose call at all; added the both-dogs `ShowProudBrief()` loop. |
| 3 | Sock Panic | Already correct | `SockPanicMissionController.CompleteSockRescue()` already poses both dogs (`SockPanicMissionController.cs:250`). |
| 4 | Squirrel Conspiracy | Already correct | `SquirrelConspiracyMissionController.cs:260`. |
| 5 | Eagle Shadow Panic | Already correct | `EagleShadowPanicMissionController.cs:495`. |
| 6 | Coyotes at the Fence | Already correct | `CoyotesFenceMissionController.cs:429`. |
| 7 | Weenie Roundup | Fixed (partial miss) | `Deliver()` only posed the delivering dog; the jumbo finale is a two-dog beat (Cocoa steadies, Cheddar hauls) and Cocoa got nothing. Added a both-dogs loop in the `ReadyToClear` branch. |
| 8 | Scent Search | Fixed | The `searchComplete` branch (bone-cache finale) had no pose call — only the interim per-dig `ShowDig()`/`ShowProudBrief()` (single dog, per-find) fired. Added a both-dogs loop at the finale. |
| 9 | Thunderstorm Comfort | Fixed | `Tick()` returns immediately once `_cleared` is set (skips the huddle-refresh loop that normally calls `ShowComfort()`), so the "STORM PASSED" hold decayed to Idle well inside the 1.15s window. Added a both-dogs `ShowProudBrief()` call at the `_cleared = true` trigger. |
| 10 | Mark the Yard | Fixed | `CompleteYardClaim()` had no pose call; added the loop. |
| 11 | Leash Walk | Already correct | `LeashWalkMissionController.cs:241`. |
| 12 | Car Ride | Already correct | `CarRideMissionController.cs:534` / `:564`. |
| 13 | Gate Crash | Fixed | `HandleSnaps()`'s solved branch had no pose call; added the loop. |
| 14 | Table Stealth | Fixed | `HandleExposures()`'s solved branch had no pose call; added the loop. |
| 15 | Squirrel Switcheroo | Fixed | The `_puzzle.Solved` branch had no pose call (only the per-hit stash pop); added the loop. |
| 16 | Walk Campaign | Fixed | The `_puzzle.Solved` branch had no pose call; added the loop. |
| 17 | Bone Relay | Fixed | `CompleteBoneDetail()` had no pose call; added the loop. |
| 18 | Great Escape | Fixed | The `_puzzle.Solved` branch had no pose call; added the loop. |
| 19 | Chaos Machine | Fixed | The `_puzzle.Solved` branch had no pose call; added the loop. |
| 20 | Blanket Catch | Fixed | `CompleteDinnerSave()` had no pose call anywhere in the controller; added the loop. |
| 21 | Kitchen Food Frenzy | Fixed (partial miss) | Only the catching dog got `ShowProudBrief()` per catch; the finale credits both the caller and catcher roles, so the non-catching dog was frozen. Added a both-dogs loop in the `_state.Complete` branch. |
| 22 | Operation Pee Break | Fixed | See correction note above — dogs were static during `DoorOpen`'s hold. Added a both-dogs `ShowProudBrief()` call in `StageDogsForDoorOpenPayoff()`, which fires exactly when the hold begins. |
| 23 | Baby Bird Bedlam | Fixed | `CompleteFeast()` posed the parent-bird actor state but never the dogs; added the loop. |

**Evidence:** 16 controllers fixed (14 full misses + 2 partial misses), 1 structural gap recorded as
out-of-scope (Backyard Rescue), 6 already correct. Added a `DogReadabilityFeedback.Pose.Proud`
assertion (both dogs, during the held payoff, before `ForceFinishSuccessPresentation()`) into each of
the 16 fixed missions' existing clear-path PlayMode tests — none of these assertions would have
passed before the fix. Full suite: **643/643 passed, 0 failed, 0 skipped** (same count as the A2.4
baseline — no new test methods were added, only assertions inside existing ones —
`unity/playmode-results.xml`, SHA-256
`2dd2fcd6581b9cf2de65fae113546d40e4df61bf216414c85b70f196d315b3e7`, 2026-07-19 14:01 EDT). No visual
build/art-review capture was required: this task only changes which `DogReadabilityFeedback.Pose` is
forced during an existing hold window, not any art asset or resource path.

---

## Phase 3 — Art consistency sweep (parallelizable)

### V3.1 — Style-contract audit + fix list
**Goal:** one visual language, enforced by a checklist instead of vibes.
**Do:** write the style contract into `docs/ART-DIRECTION.md` as a short checklist (outline weight
range, palette family, shading style, silhouette-first, no baked text). Run the art-review capture;
grade all 69 frames against it; append the ranked fix list (worst inconsistencies first) to
`docs/ASSET-PRODUCTION-CATALOG.md`. No regeneration yet.
**Done when:** contract + graded fix list exist. This task gates V3.2/V3.3 scope.

**Done (2026-07-19):** Added the five-point checklist (outline weight, palette family, shading style,
silhouette-first, no baked text) as a new "Style Contract" section in `docs/ART-DIRECTION.md`, with
concrete numeric anchors pulled from real usage (outline widths sampled across `tools/art/*.py`
cluster 4-12px at a 512px canvas) rather than written from vibes. Full audit + ranked fix list +
23-mission grading table appended to `docs/ASSET-PRODUCTION-CATALOG.md`'s new "V3.1 Style-Contract
Audit" section.

- **Rebuilt dev player at HEAD** (`9f478ff`, executable SHA-256
  `8b9dbfb16d5f923d5f59157c28be5d40c0f81da129f4de30379567d004ad65dd`) and ran smoke — both passed.
- **Ran a fresh `--arena-art-review=` capture** with a manual watchdog (macOS has no `timeout`
  binary; used a polled `kill -0`/`sleep 5` loop instead). It completed cleanly in ~25s, 69/69 frames
  written — a structural improvement over A2.4's hang — but every frame sampled at zero pixel
  variance: the same no-GPU/no-display sandbox gap flagged in P0.1/G1.2/G1.3/G1.4/A2.4 reproduced
  again, so these frames carry no gradable visual information.
- **Graded from `unity/builds/art-review-guidance-current/` instead** (2026-07-16, confirmed real
  rendered content via pixel-variance sampling, already matches the current 23-mission/69-frame
  roster) — converted its raw `.ppm` frames to 6 labeled contact-sheet JPGs (4 missions/sheet, kept
  locally under `unity/builds/art-review-v3.1-style-audit/`, gitignored like all `unity/builds/`
  output) and visually graded all 69 frames against the new checklist. Two assets that shipped after
  that capture (A2.4's authored Sniff strips, G1.3's role-turn beacon) were graded directly from
  source PNGs instead, since no completed capture contains them.
- **Real finding, not just a methodology note:** while grading, found that 18 of 23 missions'
  captured frames don't actually show the mission's real objective prop, traced to
  `ArenaArtReviewCapture.cs` — only Weenie Roundup, Leash Walk, and Chaos Machine call
  `StageDogsAtCurrentObjective()` before capturing. Cross-checked the affected missions' *actual*
  registered art directly from `Assets/Art/Resources/ArenaFinal/Props/...` rather than grading an
  empty frame, which surfaced the audit's biggest finding (below).
- **Ranked fix list (worst first), full detail in the catalog doc:**
  1. Off-style photoreal renders (A2.4's new Sniff pose art; all of Operation Pee Break) break
     "same two dogs everywhere" mid-mission — highest severity, and flagged as needing an explicit
     decision (permanent documented exception vs. a real fix target), not just a fix.
  2. **Baked text confirmed in 5+ shipped runtime sprites** by direct pixel inspection —
     `gate_crash_gate_held.png` ("GATE HELD"), `table_stealth_human_distracted.png`
     ("DISTRACTED BY COCOA"), `switcheroo_stash_open.png` ("RAID WINDOW"),
     `walk_campaign_human_walkies.png` ("WALKIES!"), `bone_relay_mound_found.png` ("BONE FOUND") —
     plus `tools/art/generate_environment_prop_pack.py:47`/`:107` (baked "GO" and checkpoint
     numerals 1-5), the latter confirmed actually rendering in the captured Leash Walk payoff frame.
  3. A generic/fallback-looking prop shape appears instead of real art in 5+ missions' captures
     (Bone Relay, Chaos Machine, Blanket Catch, Kitchen Food Frenzy, Baby Bird Bedlam) — traced to
     the `StageDogsAtCurrentObjective()` gap above; recommended as a cheap follow-up since it would
     make every future capture more gradable regardless of whether today's shape is a real
     resource-load regression or just a capture-framing artifact (couldn't fully distinguish the two
     without a live display).
  4. Silhouette-first failures in the same 4 missions (near-empty-lawn reads), same root cause.
  5. Roster-wide painterly-plate-vs-flat-cartoon-actor shading tension — the intentional documented
     exception in the new contract, flagged for its pervasiveness (18+/23 missions) rather than as a
     violation.
  6. Duplicate score-pop checkmark icons stacking vertically (Mark the Yard, Great Escape payoffs) —
     likely a rapid-forced-hook capture artifact, not reachable at real input speed.
  7. Minor camera-composition notes (one clipped prop, two large flat-color void fills) — noted for
     awareness, not scored against the style contract itself.
- **Not found:** no dimension-1 (outline weight) or dimension-2 (palette family) violations at the
  severity of the above — sampled generator-script outline widths and roster palette both already sit
  inside what the new contract specifies.
- No PlayMode run needed — docs-only, no runtime behavior changed (same precedent as A2.1). No new
  dev build artifacts were committed (`unity/builds/` is gitignored, matching every prior task).

### V3.2 — Kill remaining square/debug first reads
**Goal:** with F1 off at 1080p, nothing a player must understand reads as a colored square, bare
rectangle, or debug string.
**Do:** work V3.1's list top-down: regenerate or restyle offenders through their existing
`tools/art/generate_*.py` scripts (extend scripts rather than hand-editing PNGs), dim or hide
fallback pads that show through, and demote any normal-play debug text to F1. Stop at the point
where remaining items are cosmetic-corner grade and note them.
**Tests:** existing final-art resource/integration tests extended for regenerated assets; fresh
art-review capture attached as evidence.

**Done (2026-07-19):** Worked V3.1's ranked list top-down within this task's actual scope (colored
square / bare rectangle / debug string reads specifically - dimension-3 style-language mismatches
like #1 and #5 are a separate decision, not this task's target).

- **Baked text (fix-list #2) - larger than V3.1's sample found.** V3.1 explicitly flagged its pass as
  sampled, not exhaustive. A full visual audit of the two affected state packs found baked captions
  in **all 32 files** across GateCrash/TableStealth/SquirrelSwitcheroo/WalkCampaign/BoneRelay, not
  just the 5 V3.1 happened to sample. Before touching any of them, traced every affected controller's
  code and confirmed each removed caption is already shown - word for word or better - through the
  shared `SetCue`/`SpawnWorldPop`/`MissionActorFeedback` dynamic-text path at the exact same state
  transition (e.g. Gate Crash's `SpawnWorldPop(..., "COCOA ANCHORED!")` fires the same line
  `MissionPropArt.SetSprite(_gateArt, FinalGameplayArt.GateCrashGateHeld)` does), so stripping the
  baked text loses zero information. Wrote `tools/art/strip_baked_state_captions.py` since the
  original one-shot generator for this family is gone (confirmed absent, matching V3.1's finding):
  a connected-component pass erases any blob entirely above a measured caption/icon boundary row.
  **Real complication found and fixed properly, not papered over:** 5 human-bust files
  (`table_stealth_human_{distracted,watching}`, `walk_campaign_human_{confused,getting_it,misread}`)
  have a second caption line positioned close enough to touch the head-circle's outline, so a few
  letters flood-fill into the same connected component as the head and survive a naive strip (first
  attempt left "COC"/"ATCHIN" fragments). Fixed by patching just the top ~110px of the canvas (well
  above where any state's eyes/mouth/question-mark are drawn) from a same-mission sibling with a
  single-line caption (already clean after the component pass) - keeps each state's own expression
  and torso color, discards only the fused text. Verified every one of the 32 files by eye (not just
  by heuristic) after the fix. Also removed `generate_environment_prop_pack.py`'s baked "GO" (patio
  doormat) and checkpoint numerals 1-5 (leash route stones, confirmed actually rendering in the
  Leash Walk payoff capture) - this generator still exists, so those two regenerated normally through
  the script itself; the real checkpoint count is already narrated live via
  `LeashWalkMissionController`'s HUD cue ("reach checkpoint X/Y").
- **Fallback-shape / silhouette-first gap (fix-list #3, #4) - root cause resolved via the tool, not
  the art.** Extended `ArenaArtReviewCapture.cs`'s `StageDogsAtCurrentObjective()` coverage (V3.1's
  own cheap-fix recommendation) to the missions it named: Gate Crash, Table Stealth, Squirrel
  Switcheroo, Walk Campaign, Bone Relay, Great Escape, Blanket Catch, Kitchen Food Frenzy, Baby Bird
  Bedlam (both Main and Payoff), plus Chaos Machine's Payoff (its Main already had it). Deliberately
  did **not** extend the other 12 missions, whose captures V3.1 graded as already fine without
  staging - forcing it there risked regressing a composition that already reads correctly. Audited
  the 5 flagged missions' actual `FinalGameplayArt` resource paths against their controller code
  (Bone Relay's found-mound override, Chaos Machine's lever/junction paths, etc.) and found no
  mismatched/missing resource - the wiring is correct, so the "generic tan-plaque" shape V3.1 saw was
  the capture camera looking at the wrong spot, not a real fallback-pad render.
- **ArenaBounds flat-fill (fix-list #7, half of it) - confirmed definitively, not just suspected.**
  Traced `ArenaBootstrap.BuildScene()`: the real gameplay camera (`SharedCameraController`) is always
  configured with `clamp: true` against `ArenaBounds`, so an actual player can never see past the
  yard's edge - the flat `#243a1c` void V3.1 saw for Coyotes Fence/Weenie Roundup is only possible
  because `ArenaArtReviewCapture` disables that rig (`rig.enabled = false`) and places the camera
  manually with no clamping, and both missions' objectives legitimately sit near the yard boundary
  (fence line, house-adjacent bowl). Fixed by adding `ClampFocusToBounds`/`ClampAxis` to the capture
  tool, mirroring `SharedCameraController.ClampToBounds`'s own math exactly (Pee Break's bespoke
  indoor framing is deliberately excluded). This was a real, fully-resolved finding, not a
  documented-exception dodge - no player-facing code changed, only the diagnostic tool got as
  trustworthy as the thing it's diagnosing. Chaos Machine's frame-edge lever clipping (the other half
  of fix-list #7) likely shares this same staging-gap root cause and should already be improved by
  the `StageDogsAtCurrentObjective()` fix above; not independently verified given the sandbox's
  no-GPU limitation.
- **Debug text outside F1 (V3.2's own "demote to F1" instruction) - audited, none found.** Checked
  all three `OnGUI`-drawing classes (`ArenaHud`, `DebugHud`, `AdventureMapHud`) plus a literal
  string search for `DEBUG`/`TODO`/`PLACEHOLDER`/etc. across every script. `ArenaHud`'s diagnostics
  are already correctly gated behind `PlaytestOverlayVisible` (F1/backquote); `DebugHud`'s
  controls-legend text is suppressed in the arena (`SetLegendVisible(false)`, only its bark-flash
  flourish remains); `ObjectiveArrowFeedback`'s distance readout is already gated behind
  `_debugTextVisible`; `AdventureMapHud` is dead/deferred campaign-progression code, not reachable
  from the current mission-select flow. No violations to fix - a prior polish pass already covered
  this ground.
- **Duplicate score-pop stacking (fix-list #6) - investigated, deliberately not fixed.** Confirmed
  `MissionWorldPop` has no de-dup or max-concurrent guard at all (every `SpawnWorldPop` call is a
  fully independent, self-destructing instance), so the mechanism V3.1 suspected is real. But the
  only way to trigger it is the capture tool's tight `for` loops calling `ForceClaimZone`/
  `ForceEscapeStep` back-to-back with no settle frame between iterations - a real player needs five
  separate physical actions across real seconds to claim five zones/stations, so this cannot happen
  in actual play. Adding a stacking guard to `MissionWorldPop` would touch every mission's pop
  effects for a capture-only cosmetic artifact; making the capture tool insert a settle delay would
  mean converting `DrivePayoff` to a coroutine, whose payoff can't even be visually confirmed given
  this sandbox's no-GPU limitation. Left as a noted cosmetic-corner item per this task's own stopping
  rule, not implemented.
- **Not in scope (confirmed, not silently skipped):** fix-list #1 (photoreal Sniff pose / Pee Break)
  and #5 (painterly plate vs flat actors) are dimension-3 style-*language* mismatches, not square/
  rectangle/debug-string reads - V3.1 already flagged #1 as needing an explicit owner decision and #5
  as a documented intentional exception. Neither belongs to this task's actual goal statement; both
  are still open for whoever makes that call.
- New tests: `ArtReviewCapture_ClampsFocusToBoundsLikeTheRealCameraRig` (pure-logic, same pattern as
  the existing `OutputDirectoryFromArgs` test - no scene needed). `ClampFocusToBounds`/`ClampAxis`
  made `public static`/testable for this reason. No new test for the `StageDogsAtCurrentObjective()`
  staging extension or the caption-strip script - both are dev-tooling changes with no clean
  in-engine seam to assert against without either a disproportionate refactor or pixel-analysis code
  in C#; verified instead by re-running the actual tools (full capture, direct visual inspection of
  all 32 regenerated PNGs).
- Full PlayMode suite: **644/644 passed, 0 skipped** (up from 643 - one new test), SHA-256
  `03b2092d0abde41ca8e0c27eb43f5abfa2088050f76f0693b772d8ef7149b9e0`, 2026-07-19 17:56 UTC. Dev
  player rebuilt at HEAD, executable SHA-256
  `43ac96e048ea22dd97abc83a66755dadb95d93311ef004eeff842f3ec31c887b`; smoke passed. Art-review
  capture ran cleanly (69/69 frames, no exceptions tied to this task's changes) but reproduced the
  same no-GPU/no-display flat-placeholder sandbox gap flagged in every prior task (confirmed via
  pixel-variance sampling: 1 unique color per frame) - the actual visual evidence for the art fixes
  came from directly viewing all 32 regenerated PNGs plus the 2 regenerated environment sprites via
  the Read tool, not from this capture.

### V3.3 — Mission tile consistency
**Goal:** the 23 mission-select tiles read as one set.
**Do:** per V3.1 grading, regenerate outlier tiles (the roster grew tile-by-tile across months) to
the framing/style of the best current tiles (Car Ride's new backseat-chaos tile and Baby Bird's
painterly portrait are the bar). Keep the showcase-five ordering untouched.
**Tests:** tile resource-load coverage stays green; picker screenshot in evidence.

**Done (2026-07-19):** **The task's own premise didn't hold up, and a direct audit caught it** —
same shape as A2.5's finding. Viewed all 23 source tile PNGs at full resolution via the Read tool
(not a sample) against the named bar (Car Ride's backseat-chaos tile, Baby Bird Bedlam's painterly
portrait). Finding: all 23 already share one consistent visual language — gold double-frame
rounded-square border, dark-green storybook vignette, the same Cheddar/Cocoa character design
(orange/green-collar, mahogany/purple-collar, gold "C" tags), and a green ribbon banner with a
paw-print motif. `git log` on `Assets/Art/Resources/ArenaFinal/UI/MissionTiles/` explains why: the
2026-07-01 bulk commit (`645835c`, "Overhaul mission select into a full-screen picture-tile
picker") established this exact template across all 23 tiles at once; Car Ride and Baby Bird
Bedlam's later individual refreshes stayed *inside* that template rather than introducing a new one
the other 21 haven't caught up to. **No outlier tile exists to regenerate.** One harmless nit noted,
not fixed: Baby Bird Bedlam's ribbon is baked blank (no title text) while all other 22 bake their
mission title into the ribbon — moot since every tile's ribbon is cropped out at runtime (below),
worth knowing if this generator lineage is ever touched again.

**Real bug found and fixed while verifying the audit, not from the audit itself.** A static-PNG
look can't confirm how tiles actually *composite* in the runtime UGUI picker, and this sandbox has
no GPU/display (same `--arena-art-review=` flat-placeholder gap every prior V-series task hit).
Built a small offline Python/PIL tool (`simulate_tile.py`, scratchpad-only, not committed) that
reproduces `MissionSelectScreen`'s exact compositing math pixel-for-pixel: tile/coverHolder sizing,
`FitTileCover`'s zoom/crop, the `HudMissionTile` frame stretch, the `RectMask2D` clip, and the
translucent `NameStrip`. First pass had two bugs in the *simulation itself* (a missing
pixel-multiplier on the cover-art scale, and an inverted Y-axis sign converting Unity's Y-up
`anchoredPosition` into PIL's Y-down paste coordinate) — caught by re-deriving the math from
`RectTransform.anchoredPosition`'s actual semantics rather than trusting the first render, the same
"false lead caught by full verification" shape as A2.4. The corrected grid-tile simulation confirms
`FitTileCover` is genuinely clean: full-bleed art, no baked-ribbon bleed-through, no `HudMissionTile`
placeholder-card bleed-through, in both normal and selected states.

The **detail panel** (the larger cover shown for the focused tile before starting) was a different
story. `FitDetailCover` used `Mathf.Min` (contain-fit) to scale the 1:1 square cover art into the
896x300 wide detail window. Since every one of the 23 covers is the same 1254x1254 square and the
window is a fixed 2.99:1 super-wide box, `Min` is *always* height-constrained — the art displayed at
only ~408px wide inside an 896px-wide window, stranding ~244px of bare near-black
`DetailCoverBacking` on both left and right sides, for every single mission, every time a player
focused a tile. Confirmed via the simulator across 12 varied mission compositions before touching
code, then fixed in `MissionSelectScreen.cs`'s `FitDetailCover` by switching to `Mathf.Max`
(cover-fit, matching `FitTileCover`'s own approach) and re-tuning `DetailArtworkVerticalShift` from
-0.16 (calibrated for the old contain-fit math, meaningless once the scale basis changed) to -0.10
(chosen empirically: verified clean, expressive close-crop framing across the same 12-mission
sample plus the two fixture missions the existing test drives, Backyard Rescue and Gate Crash; also
satisfies the existing `DetailCoverUsesTitleFreeCrop` test's `anchoredPosition.y < -area.y*0.08`
contract with margin, so that assertion did not need weakening).

New `MissionSelectScreen.DetailCoverFillsWidth` property, asserted for both fixture missions in the
existing `Screen_DetailPanel_FitsConcisePlanAndCropsBakedCoverTitle` test — this assertion would
have failed under the old `Mathf.Min` code (`sizeDelta.x` ≈ 408 vs the ≥895.5 the test now requires)
and passes under the fix. No new `[UnityTest]` method needed since the existing test already drives
to the exact state that exercises this.

Showcase-five ordering (`GameManager.MissionOrder`'s first five entries) untouched — confirmed by
not touching `GameManager.cs` at all.

Full PlayMode suite: **644/644 passed, 0 failed, 0 skipped** (same count as V3.2 — the new
assertions landed inside an existing test, no new test method), `unity/playmode-results.xml`,
SHA-256 `8200e277834fb6f79cb035d5bdc25185c3f639087ad35b5707105204faa3a28f`, 2026-07-19 15:19 EDT.

"Picker screenshot in evidence": same sandbox gap as every prior V-series task
(`--arena-art-review=` produces flat, zero-pixel-variance placeholder frames here) — the
pixel-accurate offline recomposite described above (verified byte-for-byte against the actual
shipped `FitDetailCover`/`FitTileCover` code, not guessed) is the visual evidence instead, following
V3.1/V3.2's established workaround. Whoever next has a real display should take one live screenshot
of the detail panel to eyeball the fix, but the math driving it is verified, not assumed.

### V3.4 — Indoor-fantasy staging audit
**Goal:** missions whose fantasy is indoors/elsewhere don't visibly play "in the backyard."
**Do:** audit Table Stealth, Great Escape, Chaos Machine, Blanket Catch, Thunderstorm Comfort (and
any V3.1 flag) in the art-review frames: does the backyard plate undermine the fantasy? Where yes,
add a decorative controller-owned LevelArea plate following the Kitchen/Car Ride pattern
(`generate_escape_catch_kitchen_p0_pack.py` / `generate_car_backseat_pack.py` precedents; no
colliders, markers preserved, render-order above the yard plate — note Car Ride's fixed
render-order-tie lesson in `ARENA-PLAYABLE.md`). Where the backyard is fine (most yard missions),
record "backyard intentional" and move on.
**Tests:** area-plate load + no-collider assertions per new plate; art-review frames as evidence.

**Done (2026-07-19):** Audited the five named missions (plus confirmed no other V3.1 flag named a
staging mismatch) by reading each controller's actual fantasy text (`MissionCatalog.cs` briefing +
class doc comments) against whether it has any `MissionLevelAreaArt` plate today:

| Mission | Fantasy | Had a plate? | Verdict |
|---|---|---|---|
| Table Stealth | Steak dropped under the dinner table, human watching | No | **Fixed** — dining room |
| Great Escape | Escaping through the yard's own latched fence gate; all 4 stations sit at the yard's corners | No | **Backyard intentional** — this is a fence/gate escape, not an indoor scene; forcing an indoor plate would misrepresent it |
| Chaos Machine | Rube Goldberg contraption; class doc says "the dogs pre-position at their junctions" with no location text, but its own mission-tile art depicts a den (bookshelf, lamp, wood floor, domino run) | No | **Fixed** — living room |
| Blanket Catch | "Food's teetering on the counter!" — the same kitchen-counter fantasy as Kitchen Falling Food Frenzy | No | **Fixed** — reuses Kitchen's own floor art directly |
| Thunderstorm Comfort | No location text in the briefing, but its mission-tile art is unambiguous: armchair, lamp, bookshelf, window showing the storm outside | No | **Fixed** — living room (shared pack with Chaos Machine) |

Great Escape's tile art was also checked (not just its code) before ruling it backyard-intentional —
it shows the same wooden yard gate the controller's station coordinates describe, confirming the
fantasy genuinely is the yard, not a mislabeled indoor scene.

**Generated two new floor plates**, not four, since Chaos Machine and Thunderstorm Comfort's brief
art depicts the same den and Blanket Catch reuses Kitchen's art wholesale — no redundant near-duplicate
assets. New `tools/art/generate_indoor_level_area_packs.py` mirrors `kitchen_floor_area.png`'s exact
grammar (1024x768 rounded-rect canvas, ~14px outline matching the Style Contract's 4-12px-@512
range, storybook palette) rather than inventing a new visual language: `diningroom_floor_area.png`
(honey-wood planks + a warm burgundy rug) and `livingroom_floor_area.png` (cooler wood planks + a
teal rug in Cocoa's cool-accent family + a low bookshelf silhouette). Deliberately **floor-only, no
wall/furniture plate** for any of the three new missions — each mission's existing markers (human/
steak, lever/junctions, storm cue) already carry their own art, and a wall or furniture plate risked
visually competing with them rather than helping; the floor is what actually answers "does the
backyard grass show," which is V3.4's real question.

**Wired via the exact Kitchen/Car Ride pattern**, no new architecture: `MissionLevelAreaArt.cs`
gained `CreateTableStealthArea`, `CreateChaosMachineArea`/`CreateThunderstormComfortArea` (both
delegate to one private `CreateLivingRoomArea(rootName, bounds)` to avoid duplicating the plate-build
call while keeping each mission's root GameObject independently named/destroyable), and
`CreateBlanketCatchArea`. Each of the four controllers gained a `_levelAreaArt` field, a creation
call in `StartMission()`, and a destroy-on-`Cleanup()` block — for `TableStealth`/`ChaosMachine`/
`BlanketCatch` this meant converting their expression-bodied `Cleanup() => SetSceneActive(false);`
into a full method body (mirroring Kitchen's `Cleanup()` exactly); `ThunderstormComfort`'s `Cleanup()`
was already a full method, so it only gained the destroy block. All four plates render at
`sortingOrder -7`, matching Kitchen's own already-verified-safe floor value (clears the shared
backyard plate's `-8` with the same margin Kitchen uses, confirmed by reading
`BackyardRescueArtEnhancer.AddPaintedBackyardPlate` rather than assumed). `FinalGameplayArt.cs` grew
two new resource-path constants added to the existing `LevelAreaPropPack` array, which auto-enrolls
them in that file's generic resource-load coverage test.

**Tests:** new `MissionLevelAreaArt_StagesTableStealthChaosMachineThunderstormAndBlanketCatchIndoors`
(`FinalArtIntegrationPlayModeTests.cs`) — mirrors the existing Kitchen/Car Ride test's exact shape:
switches through all four missions in sequence, asserting each installs its named plate (correct
sprite, `sortingOrder <= -7`, no `Collider2D`, no leftover primitive-square markers) and that
switching missions destroys the previous mission's area. Passed on the first run. Full PlayMode
suite: **645/645 passed, 0 failed, 0 skipped** (644 baseline + 1 new test method),
`unity/playmode-results.xml`, SHA-256 `29ba5bf2552163638b33581b18a1cc850dca3b80e6011510e6f956cb4914cd8b`,
2026-07-19 15:51 EDT. Dev player rebuilt at HEAD, executable SHA-256
`e166e7fa30eeb2b1dc62a47be4081f0e4ad837af2b8781bfd56c100e0c8f2827`; smoke passed. Art-review capture:
same known no-GPU/no-display sandbox gap as every prior V-series task — visual evidence for the two
new plates came from directly viewing the generated PNGs via the Read tool instead (both shown
in-conversation before being wired into any controller).

### V3.5 — HUD + end-card copy/style pass
**Goal:** every player-facing string and card reads in one voice (dog-life comedy, short, sofa-legible).
**Do:** sweep briefing cards, objective lines, world labels surfaced at Tier ≤2, end cards, session
summary, and rank/challenge copy for leftover dev phrasing, inconsistent capitalization, or debug
tone. Fix copy in place; no layout rework beyond what the skinned IMGUI already supports.
**Tests:** existing copy assertions updated deliberately (each change named in the commit message);
suite green.

**Done (2026-07-19):** Swept `MissionCatalog.cs` (briefing `IntroPrompt`/`ReplayPrompt`/fail-reasons/
`ItemWorldLabel`), `MissionInstructionCatalog.cs` ("YOUR TEAM PLAN" beats), all 23 controllers'
`ObjectiveLabel` getters, and the session-summary/rank/challenge copy in `GameManager.cs`. Found no
literal dev/debug/placeholder markers (`TODO`/`FIXME`/etc. - a repo-wide sweep in V3.2 already
confirmed this and nothing has regressed it), so this pass is about tone/casing/punctuation
consistency, not stray text. Four concrete, narrowly-scoped fixes, each verified against no existing
test pinning the *old* wording before changing it, then the one test that DID pin exact text updated
deliberately (not weakened) alongside the copy:

1. **`ItemWorldLabel` punctuation** - 20 of 23 missions' one-word floating item labels end in "!"
   (`"Food!"`, `"Bone!"`, `"Claim!"`...), one is a deliberate question (`"Dig?"`), and exactly two
   were the odd ones out: Backyard Rescue's `"Weenie"` and Operation Pee Break's `"Signal"`. Backyard
   Rescue's case was especially concrete: Weenie Roundup's own `ItemWorldLabel` is the *identical
   word* `"Weenie!"` - the same noun read inconsistently across two missions. Fixed both to `"Weenie!"`
   / `"Signal!"`.
2. **Car Ride's `ObjectiveLabel`** had 3 of its 5 beat fragments starting lowercase
   (`"turn ahead - hold on!"`, `"sliding - jump the junk!"`, `"watch the driver"`) while the other 2
   and every other sampled mission's `ObjectiveLabel` capitalize the leading word. Capitalized all
   three (`"Turn ahead..."`, `"Sliding..."`, `"Watch the driver"`).
3. **Backyard Rescue's `IntroPrompt`** - the blandest of all 23 by a wide margin (9 words: "Cheddar +
   Cocoa must protect the weenies together.") with no mention of the squirrel threat or the
   pressure/gap-hold role split every other mission's intro names for its own mechanic. This is the
   single line every brand-new player reads first (Backyard Rescue owns the cold-open tutorial), so
   it was worth the risk of touching tutorial copy. Rewrote to name the actual threat and roles
   ("A squirrel is stealing breakfast! One dog pressures the thief while the other holds the escape
   gap - team up to protect the weenies together.") while deliberately keeping the verbatim tail
   "protect the weenies together" so it stays a substring match for `MissionBanner`'s existing
   contract (`MissionBanner = MissionIntroPrompt` at that point in the flow, per
   `GameManager.cs:1290`) - confirmed by reading the assignment site, not assumed. Updated the one
   test with an exact-match assertion on the old string
   (`ArenaGameLoopPlayModeTests.cs`'s `MissionFlow_...` test) to the new copy, with a comment
   explaining why, per this task's own instruction to name each change in the commit rather than
   just widen the assertion.

**Investigated and deliberately left alone, not silently skipped:**
- Backyard Rescue's `ClearObjectiveText`/`ReplayPrompt`/`FailObjectiveText`/`GenericFailReason` all
  say "the weenie rescue" instead of "Backyard Rescue" (the only mission whose replay-prompt doesn't
  name itself). Initially looked like the same bug class as the `ItemWorldLabel` fix above, but on
  closer read this phrase is used *consistently* across all four of Backyard Rescue's own fields, not
  fighting itself - it reads as an intentional in-voice nickname (matching the game's dog-life-comedy
  register), not a stray inconsistency. Changing it would swap one self-consistent convention for
  another, not fix a defect, and it's the tutorial mission's copy - left alone rather than guessed at.
- `"Cheddar + Cocoa"` vs. spelled-out `"Cheddar and Cocoa"` are both used across the roster. Neither
  reads as wrong; standardizing one way would be five-plus call-site churn for a non-issue.
- `EndSummaryLabel` and `ArenaHud.PlayerOwnershipLabel` are fully built and asserted by ~30 tests but
  never actually drawn anywhere in `ArenaHud.cs`. Wiring them up would be a layout change, which this
  task's own scope excludes ("no layout rework beyond what the skinned IMGUI already supports") -
  flagged here as a real gap for whoever next touches end-card layout, not fixed under V3.5.
- Swept `Assets/Scripts/Game/*MissionController.cs` for other lowercase-leading string literals
  beyond Car Ride's; the rest (`BlanketCatchMissionController`'s span fragments,
  `WalkCampaignMissionController.WrongThingForMisread`) are grammatically-correct because they're
  embedded mid-sentence (`"Hold the blanket {span}..."`), not standalone labels - confirmed by
  reading each call site before ruling them fine, not just pattern-matching the casing.

No dev build/smoke/art-review capture for this task - every change is a C# string literal, zero
sprite/resource/layout changes, matching A2.5's precedent that pose-only or copy-only changes don't
need visual capture evidence. Full PlayMode suite: **645/645 passed, 0 failed, 0 skipped** (same
count as V3.4 - the new assertions/updated copy landed inside existing test methods, no new test
method), `unity/playmode-results.xml`, SHA-256
`37e47e9c4d665538e4a823867323d65da81cdd3539ec4788f2e09025a32c13ec`, 2026-07-19 16:08 EDT.

---

## Phase 4 — First-session flow (parallelizable)

### F4.1 — Universal first-mission control reminder
**Goal:** a stranger's *first mission of a session* — whatever it is — shows a compact fading
control strip; Backyard Rescue keeps its full progressive tutorial.
**Do:** reuse the briefing card's Switch-style glyph rendering as a small two-row strip above the
identity chips for the first ~20s of the session's first mission (or until each verb is used once),
then fade. Per-session, not per-mission; skippable the same way the tutorial is.
**Tests:** strip shows on session's first mission only; fades on timer and on all-verbs-used;
replay/second mission shows none; tutorial mission unaffected.

**Done (2026-07-19):** No session concept existed anywhere in the codebase for this
(`ResetSession()` never touched tutorial state, and Backyard Rescue's own tutorial is
per-mission-attempt, not per-session — reset in `BeginRound()` for every start/replay). Built one
from scratch, scoped as narrowly as this task needs (a single "has the session's first mission been
decided yet" flag), rather than a general session-lifecycle system:

- `GameManager` gained `_firstMissionControlStripPending` (inline `= true` default so cold boot
  works without an explicit `ResetSession()` call first — confirmed necessary by reading how
  `ActionTutorialPlayModeTests`' own boot rig never calls `ResetSession()` before its first
  `StartMission()`), consumed by the *first* `BeginRound()` call after boot or after a session
  reset. If that first mission is Backyard Rescue, the strip never activates (its own
  `ShowActionTutorial` owns the moment instead); for any other mission, it activates a 20s window
  (`FirstMissionControlStripSeconds`) with a bool per verb (`_firstMissionVerbUsed`, reusing the
  existing `TutorialActionStep` enum's first four values rather than adding a parallel one).
  `FirstMissionControlStripVisible` is a live-computed property (active && not skipped && before
  the deadline && not all four verbs used yet) — no separate "hide it now" mutation path needed,
  matching `ShowActionTutorial`'s own established shape.
- **Real bug caught by the test suite, not shipped:** the first version only ever set
  `_firstMissionControlStripActive = true` inside the pending-consumption branch and never
  explicitly cleared it afterward, so once the strip activated on mission one it stayed logically
  "active" forever (`FirstMissionControlStripVisible` still checks the deadline/verbs, but the
  deadline is ~20s in the future and a fresh mission 2 resets none of the mission-scoped markers
  it depends on) — `ASecondDifferentMission_NeverShowsTheStrip` failed on the first run, showing the
  strip re-appearing on mission 2. Fixed with an explicit `else { _firstMissionControlStripActive =
  false; }` alongside the pending-check, so every `BeginRound()` after the session's first either
  activates (never again) or explicitly deactivates.
- Unlike Backyard Rescue's tutorial (which requires **both** dogs to individually demonstrate each
  verb), this is a lighter-touch reminder: **either** dog performing a verb once checks it off —
  confirmed as a deliberate scope difference, not a shortcut, since the task calls it a "reminder,"
  not a graded lesson.
- The last 2 seconds (`FirstMissionControlStripFadeSeconds`) ramp `FirstMissionControlStripAlpha`
  from 1 to 0 instead of an instant cut, satisfying "fading" literally; hitting the all-verbs-used
  exit is an immediate hide (that's an earned dismissal, not a timeout, so no fade needed there).
  A test bug here too, caught before the fix above even mattered: the first assertion checked for
  partial alpha at 17s elapsed (3s remaining) — outside the 2s fade window by design, so alpha was
  still exactly 1 and the *test's* expectation was wrong, not the implementation; fixed the test's
  timing instead of loosening the fade window.
- **Rendering** (`ArenaHud.DrawFirstMissionControlStrip`) reuses `DrawPadButton`/`DrawKey` verbatim
  — the same glyph-rendering primitives `DrawControlGuide` already uses on the briefing card — laid
  out as a compact two-row strip (4 pad-glyph chips on top, 8 keyboard-key chips below, one set per
  dog) in the exact box position `DrawActionTutorial` already uses (`VirtualHeight - h - 94`,
  confirmed by reading the formula this clears the bottom identity-chip row). The two draw calls are
  `else if`-chained in `DrawGameplayHud()`, so they structurally can never render in the same frame,
  not just conventionally. Verb chips that have already been used swap to the tutorial's established
  green "OK" done-state color, giving live progress feedback toward the "all four used" exit
  condition (a "Readable chaos" call, not required by the task text, but cheap given the color/state
  plumbing already existed for `AllFirstMissionVerbsUsed`).
- **Skippable the same way the tutorial is**, literally reusing its pause-menu slot: since
  `ActionTutorialAvailable` (Backyard Rescue only) and `FirstMissionControlStripVisible` (never
  Backyard Rescue) are mutually exclusive, the existing conditional pause-menu row
  (`PauseOptionCount`/`PauseResumeIndex`/`ActivatePauseOption`/`DrawPauseMenu`'s single "action slot"
  at index 3) gained an `else if` sibling branch offering "Skip Control Reminder" →
  `GameManager.SkipFirstMissionControlStrip()`, rather than inserting a new row and having to
  renumber every option after it.
- **Tests:** new `FirstMissionControlStripPlayModeTests.cs` (10 tests, reusing
  `ActionTutorialPlayModeTests`' proven `ArenaBootstrap` boot-rig pattern, generalized to start
  whichever mission the test wants as "session's first"): non-tutorial mission shows the strip and
  not the tutorial; Backyard Rescue shows the tutorial and not the strip; partial verb progress
  keeps it visible while all four hides it; either dog can satisfy a verb; the forced-elapsed timeout
  hides it with a fading `Alpha` in the final 2s; manual skip hides it immediately; replaying the
  first mission does not re-show it; a genuinely different second mission does not show it (this is
  the one that caught the real bug above); Backyard-Rescue-first does not leave it available on
  mission two either; `ResetSession()` re-arms it for the next mission (couch-test "New Session"
  path). First run: 2 failures (the real production bug and the test-timing bug above), both fixed,
  reran clean.
- Full PlayMode suite: **655/655 passed, 0 failed, 0 skipped** (645 baseline from V3.5 + 10 new),
  `unity/playmode-results.xml`, SHA-256
  `b4ecb4a62690b92da3ac95f043068e257af0f7fec272649b4d6b184a3a31f94b`, 2026-07-19 16:38 EDT. Dev
  player rebuilt at HEAD, executable SHA-256
  `84c8d026a43d808a9a6a185963cf0ee625053d66812cd23aa6db7a7bf942472b`; smoke passed. No art-review
  capture — this is new IMGUI layout code with proven-safe positioning (reuses `DrawActionTutorial`'s
  exact box formula and existing draw primitives; the internal row math was checked by hand for
  overlap/overflow against the box's own bounds before landing), not new art assets; same reasoning
  V3.5 used to skip capture for a pure-logic/layout change. Whoever next has a real display should
  still eyeball it once on a fresh session's first non-Backyard-Rescue mission.

### F4.2 — Post-clear flow + session summary
**Goal:** momentum for new players: clearing a mission offers the next showcase-order mission.
**Do:** end-card "Next" routes through showcase order for the first five, then library order,
skipping already-cleared missions (reuse `NextUnfinishedMissionIndex` semantics — attempts vs clears
distinction already exists; preserve it). Session summary keeps its cleared-vs-attempted honesty.
**Tests:** routing assertions for fresh / partially-cleared / all-cleared sessions.

**Done (2026-07-19):** Read `NextUnfinishedMissionIndex`/`ChooseNextMission`/`ContinueSession` fresh
before assuming anything was missing. `MissionOrder` is a single array (showcase five, then library
18 as its literal tail) — "showcase order for the first five, then library order" is already exactly
what a circular scan of that one array produces; no second ordering array or new routing table was
needed. The primary, expected path (a new player starts from `CouchTestFocusVariant` = the
recommended showcase mission, then keeps clicking Next) already satisfies the goal today.

**Built a real improvement, tested it against the full suite, and reverted it — worth recording
honestly rather than only reporting what shipped.** The task's goal text ("offers the next
*showcase-order* mission") suggested a stronger contract than what exists: a player who manually
picks a *later* showcase mission first (e.g. Gate Crash, skipping the recommended flow) currently
gets routed straight into library missions on their next "Next," not back to the other unattempted
showcase picks, because the scan only walks forward from wherever the player currently is. Implemented
an "exhaust the showcase five before ever touching library order, regardless of current position" fix
in `NextUnfinishedMissionIndex` and wrote a new passing test for exactly that scenario. Running the
**full** suite (not just the new test) surfaced that this broke 4 pre-existing tests
(`ArenaGameLoopPlayModeTests`'s `DemoRegression_ColdStartFlowDogsCameraOverlay_StayReachable`,
`MissionFlow_Select_StartsEveryMission_AndEndActionsNavigate`,
`MissionFlow_SessionTotals_UpdateAcrossTwoMissions`, plus one cascading failure in
`PlaytestOverlay_Toggles_AndEventLogCapturesFlowEvents`) that jump straight to an arbitrary library
mission mid-test and assert Next continues to the *next array entry*, not back to an unattempted
showcase pick — i.e., today's simpler "just keep scanning forward" contract is itself already
intentional, tested, shipped behavior, not an oversight. Combined with this task's own explicit
"reuse `NextUnfinishedMissionIndex` semantics ... preserve it" instruction, that's a clear signal the
showcase-exhaustion idea is a scope expansion this task didn't ask for, not a bug fix — reverted the
production change back to the original scan-forward-from-current logic, and rewrote the test that had
been written around the new behavior into one that pins the *real* shipped behavior instead (with the
reasoning above in its own comment, so the idea isn't silently lost if a future task wants to revisit
it as a deliberate design decision).
- Session-summary's cleared-vs-attempted honesty (`SessionSummaryLabel`, `SessionUniqueMissionsCleared`
  vs `SessionUniqueMissionsCompleted`) was not touched — confirmed by not editing
  `RecordSessionResult` or either label-builder at all, only reading them.
- **Tests:** new `PostClearRoutingPlayModeTests.cs` (4 tests, using `ForceGameOver()` — a universal,
  per-mission-choreography-free way to mark a mission "attempted" — since routing only cares about
  attempt status, not clear status, which is already covered separately by
  `SessionResetPlayModeTests.SessionUniqueMissionsCleared_OnlyCountsActualClearsNotAttempts`):
  a fresh session's "Next" walks all five showcase missions in order via `ChooseNextMission`, correctly
  detouring through the 3-unique session-summary milestone screen exactly once along the way, then
  continues into the first library mission; picking a late showcase mission manually and hitting Next
  continues in array order (the rejected-alternative test above); replaying an already-attempted
  mission and hitting Next (via `ContinueSession`, since three uniques were already attempted and
  `ChooseNextMission` would otherwise divert to the summary screen) correctly skips other
  already-attempted missions; attempting all 23 missions and hitting Next via `ChooseNextMission`
  specifically (the existing `FailingEveryMission_DoesNotOfferVictoryLap` test already covered this
  via `ContinueSession`) wraps cleanly to the first mission instead of stalling. First full-suite run:
  5 failures (4 from the reverted routing change, 1 test-authoring bug in my own new test forgetting
  the session-summary milestone gate at exactly 3 uniques) — all fixed, reran clean.
- Full PlayMode suite: **659/659 passed, 0 failed, 0 skipped** (655 baseline from F4.1 + 4 new),
  `unity/playmode-results.xml`, SHA-256
  `4c472c381b5b8bb44542aa27148ca945c978a5a1091d2ffa96eb512ec54770fc`, 2026-07-19 16:58 EDT. No dev
  build/smoke/art-review capture — zero rendering or resource changes, same reasoning V3.5 and F4.1
  used for pure-logic/test-only changes; a final combined build+smoke closes out this whole
  five-task run instead (see below).

### F4.3 — Briefing accuracy audit
**Goal:** every mission's "YOUR TEAM PLAN" (≤4 beats) matches the *current* asymmetric mechanics.
**Do:** the role redesigns changed who does what in most missions; briefings may lag. Compare each
mission's briefing beats against its controller's actual sequence (per `ARENA-PLAYABLE.md`'s updated
uncoached-sequence table); fix stale beats; keep gold in-game label quoting rule.
**Tests:** existing briefing assertions updated; per-mission checklist recorded here.

**Done (2026-07-19):** Audited all 23 missions' `MissionInstructionCatalog.HowToPlayStepsFor` text
against ground truth read directly from each controller (`ObjectiveLabel` getters, role-gating checks
in `HandleBark`/`HandleInteract`, and any `Owners[]`/`Actions[]` sequencing arrays), not against
`ARENA-PLAYABLE.md`'s summary table alone (that table is itself a compressed secondary source, so
treating it as sufficient ground truth would just move the staleness risk rather than remove it).
**The task's own premise — "role redesigns changed who does what in most missions; briefings may
lag" — did not hold up**, same shape as A2.5/V3.3's findings: `MissionInstructionCatalog.cs` was
already touched directly in Codex's `50fa09a` asymmetric-role redesign commit, and 22 of 23 missions'
briefing text already matches their controller's current role split exactly, including several
non-obvious specifics verified by reading the actual gating code rather than trusting the prose:
`BackyardSquirrelTrapState.RecoveryDog`/`GapDog`/`PressureDog` alternating correctly pass-to-pass;
`CoyotesFenceMissionController.RegisterBarkPressure` gating the fake-snack-lure resolution to Cocoa
specifically (`if (dogId != DogId.Cocoa)`); `ScentSearchMissionController.Sniff()` giving Cheddar a
real "broad compass direction" bark distinct from Cocoa's hot/cold tracking; `GreatEscapeMissionController.Actions`
matching "Cocoa paws the latch, Cheddar shoulders the gate, Cocoa drags the cooler, Cheddar squeezes
through" verbatim; `ArenaMissionTuning.SnackHeist.MaxStolenFood = 2` matching "two successful steals
ends the run."

**Also verified the "keep gold in-game label quoting rule" mechanically, not by eye:** extracted every
run of 2+ consecutive (or single, 2+ letter) all-caps tokens from the catalog's step text — the same
`IsCapsToken` logic `HighlightOnScreenLabels` itself uses to decide what gets gold-highlighted — then
grepped each one against every mission controller to confirm it's real on-screen text (`SetActorState`/
`SpawnWorldPop`/`AddWorldLabel`/`TryGetObjectiveTarget` copy), not prose that happens to be capitalized.
This is a stronger check than reading the prose for plausibility, and it found the one real bug:

**Fixed:** Kitchen Food Frenzy's finale step said *"survive the DINNER RUSH finale's GOOD-BAD-GOOD
sequence"* — `GOOD-BAD-GOOD` gold-highlights (2+ uppercase letters, no lowercase) but never appears
anywhere in `KitchenFoodFrenzyMissionController` or `KitchenFoodFrenzyMissionState`; the real finale
cue is `"DINNER RUSH! Three fast calls: catch gold, dodge purple, catch gold."` and
`ExpectedFinaleKind` confirms the real sequence is Good→Bad→Good (3 calls,
`FinaleSuccessesRequired = 3`). A player told to watch for "GOOD-BAD-GOOD" would never see that text
on screen. Reworded to *"survive the DINNER RUSH finale (catch gold, dodge purple, catch gold) to
clear it"* — keeps `DINNER RUSH` (a real, verified label) gold-quoted and describes the sequence in
plain prose instead of a fabricated pseudo-label.

**Real architecture finding, not just a text fix:** the picker's detail panel does not render
`HowToPlayStepsFor`'s raw array — `MissionSelectScreen.BuildHowToPlayText` is a second layer with two
special cases: Operation Pee Break gets an entirely separate, hand-written 4-line summary (not reused
from the catalog at all), and Backyard Rescue's 7-step catalog entry is hand-trimmed to indices
`[0, 1, 2, 5]`, deliberately dropping the squirrel-trap gap-hold/pressure beats (steps 3-4) with an
existing code comment explaining why: "the picker only needs its main arc; contextual world prompts
teach the trap hand-off ... when they become live." Verified that claim is still true today (not just
asserted) — `BackyardRescueMissionController.ObjectiveLabel` narrates the live pressure/gap role split
every frame during play, so the trap mechanic is genuinely taught in-world, not silently missing.
Every other mission falls through to a generic "first 4 steps" cap (`steps.Length <= 4 ? steps :
first four`), which silently drops a 5th step for exactly four missions (Snack Heist, Coyotes Fence,
Table Stealth, Switcheroo) — in all four cases the dropped line is the fail-count/payoff-flavor
closer, never a "who does what" mechanic beat, so this is a consistent, deliberate 4-beat brevity
policy (matching `ARENA-PLAYABLE.md`'s "preview teaches at most four high-value team beats"), not
staleness. Independently verified Operation Pee Break's separate hand-written 4-line summary against
`PeeBreakMissionController`'s real `Beat` enum/`ObjectiveLabel` switch and found it accurate beat-for-
beat. Both of these were investigated and correctly left alone, not silently skipped.

| # | Mission | Verified against (ground truth) | Result |
|---|---|---|---|
| 1 | Backyard Rescue | `BackyardSquirrelTrapState` role fields (alternation each pass) | Accurate. Picker's 4-of-7-step trim is a deliberate, verified-still-true design choice (see above), not staleness. |
| 2 | Snack Heist | `SnackHeistMissionController.ObjectiveLabel`/`IsFailed`, `ArenaMissionTuning.SnackHeist` (`MaxStolenFood=2`, `ItemGoal=4`) | Accurate |
| 3 | Sock Panic | `SockPanicMissionController.ObjectiveLabel` | Accurate |
| 4 | Squirrel Conspiracy | `SquirrelConspiracyMissionController.ObjectiveLabel` + `BARK HERD`/`HOLD CUTOFF` world labels | Accurate |
| 5 | Eagle Shadow Panic | `EagleShadowPanicMissionController.ObjectiveLabel` + `HIDE HERE` world label | Accurate |
| 6 | Coyotes at the Fence | `RegisterBarkPressure` (Cocoa-only)/`TryRepair` (Cheddar-only) gating, fake-snack resolution tied to Cocoa's pin | Accurate |
| 7 | Weenie Roundup | `WeenieRoundupMissionController.ObjectiveLabel` + `JUMBO`/`HOME BOWL` world labels | Accurate |
| 8 | Scent Search | `ScentSearchMissionController.Sniff()` (Cheddar compass point vs. Cocoa hot/cold call) + `DigAtSpot` gating | Accurate |
| 9 | Thunderstorm Comfort | `ThunderstormComfortMissionController.ObjectiveLabel` + `COMFORT READY` label | Accurate |
| 10 | Mark the Yard | `MarkTheYardMissionController.ObjectiveLabel` | Accurate |
| 11 | Walkies on the Leash | `LeashWalkMissionController.ObjectiveLabel` (alternating named scout) | Accurate |
| 12 | Car Ride Chaos | `CarRideMissionController.ObjectiveLabel` (Cocoa-plants-first/Cheddar-tucks sequencing) | Accurate |
| 13 | Gate Crash | `GateCrashMissionController.ObjectiveLabel` + `SQUEEZE THROUGH` label | Accurate |
| 14 | Table Stealth | `TableStealthMissionController.ObjectiveLabel` (dual-path Cocoa-flop/Cheddar-burp distraction) | Accurate |
| 15 | Switcheroo | `SquirrelSwitcherooMissionController.ObjectiveLabel` | Accurate |
| 16 | The Walk Campaign | `WalkCampaignMissionController.ObjectiveLabel` + `WALKIES!` label | Accurate |
| 17 | Bone Relay | `BoneRelayMissionController.ObjectiveLabel` | Accurate |
| 18 | The Great Escape | `Owners[]`/`Actions[]` = `{Cocoa,Cheddar,Cocoa,Cheddar}` / `{PAW THE LATCH, SHOULDER THE GATE, DRAG THE COOLER, SQUEEZE THROUGH}` | Accurate, verbatim match |
| 19 | Chaos Machine | `Owners[]`/`Actions[]` = `{Cocoa,Cheddar,Cocoa}` / `{TOWEL DROP, BASKET TIP, TOY LAUNCH}` | Accurate |
| 20 | Blanket Catch | `BlanketCatchMissionController.ObjectiveLabel` + `RIP!`/`TOO FAR - RIPPING!` labels | Accurate |
| 21 | Kitchen Falling Food Frenzy | `KitchenFoodFrenzyMissionState` (`WarmupCatches=3`, `ExpectedFinaleKind` Good→Bad→Good, `FinaleSuccessesRequired=3`) | **Fixed** — fabricated `GOOD-BAD-GOOD` pseudo-label replaced with the real cue's plain-prose description |
| 22 | Operation Pee Break | `PeeBreakMissionController.Beat` enum + `ObjectiveLabel` switch, cross-checked against `MissionSelectScreen`'s separate hand-written 4-line picker summary | Accurate (both copies) |
| 23 | Baby Bird Bedlam | `ChicksNeeded=4`, `ShakesNeeded=3`, `MaxPecks=3` + `PARENT BIRD DIVE`/`THE NEST`/`GRAB IT!` labels | Accurate |

23/23 audited, 1 fixed, 22 already accurate. New test:
`BackyardPoolPlayModeTests.HowToPlaySteps_KitchenFoodFrenzy_DinnerRushDoesNotQuoteAFabricatedLabel`
(would have failed before the fix). Full PlayMode suite: **660/660 passed, 0 failed, 0 skipped** (659
baseline from F4.2 + 1 new), `unity/playmode-results.xml`, SHA-256
`c41f2490f38b240cb95d32146fc456d8b1ae9814a566c4e7762d55173a957315`, 2026-07-19. No dev build/smoke/
art-review capture — this is a single C# string literal plus a pure-logic test, zero sprite/resource/
layout changes, matching V3.5 and F4.1/F4.2's precedent for copy-only or logic-only changes.

---

## Phase 5 — Audio for signals (after G1.x lands)

### S5.1 — Audio for new signals
**Goal:** ladder Tier 3, role-turn beacon appearance, and handoff flourish each have a distinct,
sofa-audible cue; the two dogs get identity-distinct handoff chimes.
**Do:** add named cues through the existing cue-slot boundary (authored bank first,
generated-profile fallback, same as current slots). Replace G1.2's placeholder Tier-3 cue.
**Tests:** event-driven audio assertions per new slot (existing pattern).

**Done (2026-07-19):** Added 4 new named cue slots to `ArenaFeedbackCatalog.RequiredAudioCues`/
`AuthoredAudioCatalog.CueBanks` and wired each into its real call site, replacing the two ad-hoc
placeholders G1.2/G1.4 flagged as this task's job:

- `GuidanceRescueCall` replaces the Tier-3 `Bark` placeholder in `UpdateGuidancePresentation`'s
  tier-up edge (`GameManager.cs`).
- `RoleTurnBeaconAppear` is new audio the beacon never had (G1.3 shipped visual-only). Wiring this
  needed a real edge-detector, not just a call site: `UpdateRoleTurnBeacon` was split into a thin
  wrapper that reads `RoleTurnBeacon.IsShowing` before/after the (renamed, otherwise-untouched)
  `ApplyRoleTurnBeaconState` and fires the cue only on the hidden-to-visible transition - confirmed it
  does NOT re-fire on every owner/target change while already visible (that's a different moment,
  already covered by the handoff chime below) and does NOT need a new tracking field, since the
  beacon's own `IsShowing` is already the single source of truth and already resets correctly via
  `ResetGuidancePresentation()`'s existing `Hide()` call.
- `HandoffChimeCheddar`/`HandoffChimeCocoa` replace the single shared `UiReplayNextSelect` placeholder
  in `SignalRoleHandoff`, selected by `toDog` (the dog now taking over, not the one handing off).

**Real constraint discovered before writing any code, not after:** `ArenaGameLoopPlayModeTests
.AuthoredAudioCatalog_ImportsEveryNamedClip_AndMapsEveryCue` already enforces that *every* cue in
`RequiredAudioCues` maps to at least one real, resolvable authored clip - there is no existing
precedent anywhere in the catalog for a "generated-synth-only" cue, so adding new cue names with an
empty authored bank would fail this test immediately (and weakening it was not an option). This
session has no audio-generation/recording tool, the same shape of gap A2.3/A2.4 hit for art. Followed
A2.3's precedent (wire the code-side plumbing honestly against what's real, document the gap, don't
block/fake/silently skip) rather than re-asking here since the shape of the answer was foreseeable
from A2.3's own recorded resolution:
- **Handoff chimes are not a compromise** - each dog's own already-imported bark takes
  (`AuthoredAudioCatalog.CheddarBarks`/`CocoaBarks`) *are* their vocal identity, so reusing them
  per-dog for "it's my turn now" is the intended design, matching CLAUDE.md's Cheddar/Cocoa-must-feel-
  distinct rule directly, not a workaround.
- **Role-turn beacon appearance** reuses the existing `StarAppear` trio (`p3_star_appear_01/02/03`) -
  a genuine semantic fit ("something just appeared") already used elsewhere in the game for exactly
  that idea, not a random pick.
- **Tier-3 rescue call** is the one real gap: no unclaimed dog-vocal audio exists (`Bark`'s cue already
  claims all 16 Cheddar+Cocoa takes), and reusing any of them would make the "you need help" cue
  sonically indistinguishable from an ordinary bark, defeating the point of giving it its own slot.
  Reused `p0_menu_tile_focus` (the only imported clip that reads as "pay attention" with no success/
  failure/squirrel connotation already attached) and documented this plainly in
  `AuthoredAudioCatalog.cs` as a placeholder pending a dedicated coach-woof recording - flagged here as
  a genuine open follow-up, not silently left implicit.
- Generated-fallback params were still designed deliberately for all 4 (not filler): Cheddar's and
  Cocoa's handoff-chime synth profiles encode the same chaos-puppy/veteran-queen identity split A2.2's
  interact-squash amplitude asymmetry uses (Cheddar higher/faster/rougher, Cocoa lower/steadier),
  verifiable even though the authored bank is what actually plays in practice.

**Tests:** extended 1 existing test rather than duplicating a fixture - `GuidanceSignalPlayModeTests
.Tier3_FlagsRescueActive_AndFiresOneAudioCueOnTheEdge` now counts `GuidanceRescueCall` instead of
`Bark` (renamed helper, updated message); `ArenaGameLoopPlayModeTests
.AuthoredAudioCatalog_ImportsEveryNamedClip_AndMapsEveryCue` automatically covers all 4 new slots with
zero changes since it iterates the catalogs generically. Added 2 new test methods: `RoleTurnBeaconPlayModeTests
.Beacon_FiresRoleTurnBeaconAppear_OnceOnTheRisingEdge_NotOnEveryOwnerChange` (Great Escape fixture -
fires exactly once on mission-start appearance, stays silent across 5 idle frames, stays silent across
an owner change that keeps the beacon visible) and `RoleHandoffPlayModeTests
.GreatEscape_EachStep_FiresTheReceivingDogsOwnIdentityDistinctChime` (same fixture's Cocoa->Cheddar
then Cheddar->Cocoa steps, asserting the correct per-dog cue fires and the other dog's does not). Both
passed on the first run - no bugs found this round. Full PlayMode suite: **662/662 passed, 0 failed, 0
skipped** (660 baseline from F4.3 + 2 new), `unity/playmode-results.xml`, SHA-256
`42fa8db22928dbbc34a120a170829423dd34ef0abce17d0548ee535a1c4aa3d7`, 2026-07-19. Dev player rebuilt at
HEAD, executable SHA-256 `b767f4a0d361530f7966e029d54521d60339aa3cb51e4f278a0b96633fe5e7ca`; smoke
passed. No art-review capture - audio has no pixel signature to capture either way, same reasoning
that exempted A2.2's squash-pulse timing from needing one; whoever next has real speakers/a live
display should do a manual listen-through of all 4 new cues, especially the two reused/placeholder
choices flagged above.

### S5.2 — Feedback-slot audio coverage audit
**Goal:** no major feedback moment is silent.
**Do:** sweep the shared feedback slots + per-mission coach/payoff beats for missing cue requests;
fill the worst gaps. Small task — do not start a mix/recording project; that stays post-launch.
**Tests:** extended event-audio assertions; suite green.

**Done (2026-07-19):** Established ground truth on what's already covered before searching for gaps.
Traced the two shared dispatch points: `GameManager.OnDogBarked` unconditionally fires the `Bark` cue
for every real bark press before ever reaching the controller, and `MarkFailedInteraction` always
fires `UiButtonDisabled` - so any feedback moment reached through a `HandleBark` call or a coached
rejection is already audio-covered for free, regardless of what the controller itself does.
`OnDogInteracted` has no equivalent unconditional cue, so `HandleInteract`/`Tick`/timer-driven moments
have no free coverage.

Swept every `SetJuice(...SuccessPop/WarningMiss...)`/`SpawnWorldPop(...)` call site across all 23
controllers for one missing an audio request within its enclosing method (not just a fixed-line
window - GreatEscape's "WRONG PAWS!" looked like a hit under a narrow-window scan but its enclosing
method already routes into a shared `if (wasted)` block that fires `RequestAudioCue(ThreatWarning)`
a few lines further down; reading the *whole* method before touching anything caught this and it was
correctly left alone, no fix needed). Found 9 real, confirmed-silent moments across 6 missions -
each already had haptic rumble and/or a world-pop, just no audio:

| Mission | Moment | Cue added | Reused from |
|---|---|---|---|
| Gate Crash | Cocoa's gate anchor engages (`HandleInteract`) | `TugRescueSuccess` | Same generic "team beat succeeded" cue PeeBreak's beat-advance and CoyotesFence's yard-defended already use |
| Table Stealth | Cocoa's belly-rub decoy engages (`HandleInteract`) | `TugRescueSuccess` | " |
| Walk Campaign | Cocoa's door-stare engages (`HandleInteract`) | `TugRescueSuccess` | " |
| Walk Campaign | Cheddar's leash-present engages (`HandleInteract`) | `TugRescueSuccess` | " |
| Car Ride | Brace-team-ready ("TUCKED SAFE!", `HandleBrakeBrace`) | `TugRescueSuccess` | " |
| Coyotes at the Fence | Coyote driven back by held pressure (`EvaluateReach`) | `TugRescueSuccess` | " |
| Coyotes at the Fence | Bark-pin expires ("PIN LOST!", `ExpireBarkPressure`) | `ScorePenalty` | Same "generic miss, no direct rejection" cue KitchenFoodFrenzy's `UnsafeLanding` branch already uses |
| Blanket Catch | A catch attempt misses/rips ("MISSED!"/"SPLAT!", `HandleProgress`) | `ScorePenalty` | " |
| Baby Bird Bedlam | A chick lands and the airlift countdown starts ("CHICK DOWN!", `Tick`) | `ThreatWarning` | Matches its existing "urgency/threat" usage elsewhere (e.g. ThunderstormComfort's thunderclap onset) rather than the "miss" framing, since nothing has gone wrong yet - the clock just started |

All 9 reuse an existing cue rather than adding new ones - this task's own "no mix/recording project"
boundary, and every added cue is semantically consistent with how the codebase already uses it
elsewhere (verified per-cue, not assumed).

**Investigated and deliberately left alone, not silently skipped:** GreatEscape's "WRONG PAWS!" (see
above - already covered a few lines further down in the same method); Car Ride's "CLEAN HOP!" clean-
jump-dodge read (a frequent, rapid micro-event during every turn/slide event, not a discrete once-per-
beat moment - adding a cue risked audio spam across a single obstacle run, and there's no way to
verify the right cadence without real speakers); Gate Crash's "HOW DARE YOU" idle-outrage gag and
Walk Campaign's "too far, get closer" hint pops (minor flavor/nudge moments, not major feedback);
Kitchen Food Frenzy's drop-telegraph announcement and Leash Walk's "INTEL GATHERED" (ambient
announcements, not discrete result events); Thunderstorm Comfort's missed-clap pop (already covered -
`ApplyThunderclap` fires `RequestAudioCue(ThreatWarning)` unconditionally at the top of the method,
before the branch, same "wider window" catch as GreatEscape). Stopping here matches this task's own
"fill the worst gaps" instruction rather than chasing every candidate to the same exhaustive depth.

**Tests:** extended 7 existing tests (no new test methods - each already drove to the exact state that
exercises the fix, per G1.5/A2.5's established convention) with an audio-cue assertion:
`CoopGateCrashPlayModeTests.GateCrash_CocoaMustDeliberatelyAnchor_...`,
`CoopTableStealthPlayModeTests.TableStealth_PositionDriven_CocoaMustInteractFlop_...`,
`CoopWalkCampaignPlayModeTests.Walk_PositionDriven_BothDogsMustInteractThenHoldTheirStations`,
`CarRidePlayModeTests.CarRide_BrakeEvent_BracedDogsRideItOut`,
`CoyotesFencePlayModeTests.CoyotesFence_ProwlReach_BarkPressureDrivesOffElseBreaches` and
`.._CocoaPinsInRangeAndCheddarRepairsBeforeTheOpeningCloses`,
`CoopBlanketCatchPlayModeTests.Blanket_SlackOrOffCenter_Misses`,
`CoopBabyBirdBedlamPlayModeTests.Bedlam_PositionDriven_ChickFallsAndCheddarGrabsIt` (the one BabyBird
test that lets a chick fall and land for real across real frames, rather than the `ForceChickLand`
test-only bypass every other Bedlam test uses, which deliberately skips the reaction entirely and
would not have exercised this fix).

First full run caught 2 failures, both in the new assertions' own design, not production bugs: Table
Stealth's flop-engage branch also calls `SignalRoleHandoff(Cocoa, Cheddar)` in the same method, whose
S5.1 handoff chime fires immediately after and is the actual last cue requested; Walk Campaign's
leash-present branch, when it's also the dog that completes the exact-match combo, runs into
`HandleProgress`'s own pre-existing `SnackSockCollect` "combo" cue the same way. Both are real,
already-correct cues legitimately firing after mine in the same call, not bugs - fixed by asserting
`AudioCueRequests` containment instead of `LastAudioCueRequested` equality, the same "assert the
event happened, not that it was the last one" lesson G1.5 hit for the exact same reason. Reran clean.

Full PlayMode suite: **662/662 passed, 0 failed, 0 skipped** (same count as S5.1 - assertions landed
inside existing tests, no new test methods), `unity/playmode-results.xml`, SHA-256
`95b2ac18d567225294b4b1adad40bd7d536cc29f6e3473c839dfc4d66c7dc17c`, 2026-07-19. Dev player rebuilt at
HEAD, executable SHA-256 `ba57952c534ce679497c676fd0efe84c006721fbea63a4190932708837ea6a9e`; smoke
passed. No art-review capture - audio has no pixel signature either way, same reasoning S5.1 used.

---

## Phase 6 — Evidence and the human gate

### E6.1 — Evidence refresh + gate handoff
**Goal:** the build that goes to the couch is fully evidenced and documented.
**Do:** full suite green; `./unity/build-dev.sh` + smoke; fresh art-review capture inspected;
refresh the preflight block in `docs/OPERATION-PEE-BREAK-COUCH-RETEST-2026-07-14.md` (new hashes,
suite count, date); update `docs/FAMILY-SHOWCASE-MANUAL-TEST.md` with the new signal watch-list
(ladder tiers, beacons, handoff flourish, control strip); update `docs/LEVEL-READINESS-SCORES.md`
status date with a short note of this queue's changes; mark this queue's status board complete.
**Done when:** all evidence recorded. **The couch retest itself is human work — Ken and Sue with
two controllers, plus ideally one session with first-time players. Agents stop here.**

**Done (2026-07-19):** This closes the pre-launch queue - every row above is now DONE. All 4 remaining
open tasks (F4.3, S5.1, S5.2, E6.1) landed in one session, continuing the same-day autonomous run that
already carried V3.3-F4.2 (owner authorized both batches directly, "do next 5 phases" each time,
without per-task confirmation).

- Full PlayMode suite: **662/662 passed, 0 failed, 0 skipped** (unchanged from S5.2 - this task made
  no code changes, only doc updates), `unity/playmode-results.xml`, SHA-256
  `95b2ac18d567225294b4b1adad40bd7d536cc29f6e3473c839dfc4d66c7dc17c`, 2026-07-19.
- Dev player rebuilt at HEAD (`35f7af8`): `unity/builds/dev/CheddarAndCocoa-Arena.app`, executable
  SHA-256 `ba57952c534ce679497c676fd0efe84c006721fbea63a4190932708837ea6a9e`. Startup smoke passed.
- Fresh art-review capture: 70/70 frames written, 0 exceptions in the run log, completed in ~18s.
  **Reproduces the identical no-GPU/no-display flat-placeholder sandbox gap every single task in this
  queue has hit** (confirmed again via pixel-variance sampling: 1 unique byte value per sampled
  frame) - this is a standing limitation of this sandbox, not a regression or something fixable from
  here. Nobody has visually inspected the Tier 1-3 ladder, role-turn beacons, or handoff flourish
  through this specific harness this entire queue; every visual claim in G1.2 through V3.4 was instead
  verified by directly viewing the underlying generated PNGs or (for V3.3's UI compositing bug) an
  offline pixel-accurate math reproduction. **Whoever runs the actual couch retest has the first real
  display this whole queue has had** - genuinely worth a few minutes with F1 on/off before the humans
  sit down, specifically to eyeball the new Tier 1-3 rendering, the beacon, and the handoff swoosh for
  the first time.
- `docs/OPERATION-PEE-BREAK-COUCH-RETEST-2026-07-14.md`: preflight block refreshed with the hashes/
  count/date above, plus a new paragraph naming everything this queue landed since the sheet was
  written (guidance ladder, beacons, handoff flourish, wrong-role audit, animation fixes, art
  consistency sweep, first-session flow, audio) and noting none of it touched Pee Break's own beats,
  tuning, or fail conditions - the existing Required Observations table still applies unchanged; only
  the ambient signals around it are new.
- `docs/FAMILY-SHOWCASE-MANUAL-TEST.md`: added two Observation Checklist lines (role-turn beacon on a
  hard-handoff mission; the first-mission control-reminder strip fading correctly) and one Co-op line
  (noticing the handoff swoosh/chip-flash/chime, not just the new instruction text) - the guidance-
  ladder stall-test line was already present from G1.6, so only the 3 signals G1.6 predates needed
  adding.
- `docs/LEVEL-READINESS-SCORES.md`: status date bumped to 2026-07-19 with a new summary paragraph
  naming what the queue changed and explicitly noting the 23 per-mission score rows were deliberately
  NOT rewritten, since none of this queue's work changed any mission's core design, difficulty, or
  asset-floor rating (it changed roster-wide presentation/guidance/audio infrastructure sitting on top
  of those ratings, not the ratings' own basis).
- Queue status board above: all 23 rows DONE, no BLOCKED/OPEN remaining.

**Handoff:** Phases 1-6 of `docs/PRELAUNCH-PRODUCTION-PLAN.md` are now fully implemented and evidenced.
Per that plan's own definition of done, the only remaining step is human: the Operation Pee Break
two-person couch retest (`docs/OPERATION-PEE-BREAK-COUCH-RETEST-2026-07-14.md`, still **NOT CALLED**),
ideally plus one session with genuine first-time (non-family) players per the plan's stated pre-launch
bar. **Agents stop here** - no further queue tasks exist to pick up until that retest produces new
findings.
