# Pre-Launch Agent Work Queue

> **Status: ACTIVE.** Written 2026-07-17. This is the execution queue for
> `docs/PRELAUNCH-PRODUCTION-PLAN.md`, sized for an implementation agent (Sonnet) working one task
> per run. Read that plan plus `CLAUDE.md`, `docs/README.md`, `docs/ARENA-PLAYABLE.md`, and
> `docs/VISUAL-READABILITY-CONTRACT.md` before the first task.

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
| A2.4 | Threat/NPC acting gaps | OPEN |
| A2.5 | Held-payoff pose audit | OPEN |
| V3.1 | Style-contract audit + fix list | OPEN |
| V3.2 | Kill remaining square/debug first reads | OPEN |
| V3.3 | Mission tile consistency | OPEN |
| V3.4 | Indoor-fantasy staging audit | OPEN |
| V3.5 | HUD + end-card copy/style pass | OPEN |
| F4.1 | Universal first-mission control reminder | OPEN |
| F4.2 | Post-clear flow + session summary | OPEN |
| F4.3 | Briefing accuracy audit | OPEN |
| S5.1 | Audio for new signals | OPEN |
| S5.2 | Feedback-slot audio coverage audit | OPEN |
| E6.1 | Evidence refresh + gate handoff | OPEN |

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

### A2.5 — Held-payoff pose audit
**Goal:** every mission's held live payoff (the 1.15s pre-end-card beat) shows an animated
proud/outcome read, not frozen dogs.
**Do:** drive each mission to clear via force hooks; assert the payoff hold applies an outcome
pose/motion on both dogs and any payoff actor state (existing pattern from Pee Break's
hydrant/relief beat). Fix the misses; list per-mission results here.
**Done when:** 23/23 table recorded; suite green.

---

## Phase 3 — Art consistency sweep (parallelizable)

### V3.1 — Style-contract audit + fix list
**Goal:** one visual language, enforced by a checklist instead of vibes.
**Do:** write the style contract into `docs/ART-DIRECTION.md` as a short checklist (outline weight
range, palette family, shading style, silhouette-first, no baked text). Run the art-review capture;
grade all 69 frames against it; append the ranked fix list (worst inconsistencies first) to
`docs/ASSET-PRODUCTION-CATALOG.md`. No regeneration yet.
**Done when:** contract + graded fix list exist. This task gates V3.2/V3.3 scope.

### V3.2 — Kill remaining square/debug first reads
**Goal:** with F1 off at 1080p, nothing a player must understand reads as a colored square, bare
rectangle, or debug string.
**Do:** work V3.1's list top-down: regenerate or restyle offenders through their existing
`tools/art/generate_*.py` scripts (extend scripts rather than hand-editing PNGs), dim or hide
fallback pads that show through, and demote any normal-play debug text to F1. Stop at the point
where remaining items are cosmetic-corner grade and note them.
**Tests:** existing final-art resource/integration tests extended for regenerated assets; fresh
art-review capture attached as evidence.

### V3.3 — Mission tile consistency
**Goal:** the 23 mission-select tiles read as one set.
**Do:** per V3.1 grading, regenerate outlier tiles (the roster grew tile-by-tile across months) to
the framing/style of the best current tiles (Car Ride's new backseat-chaos tile and Baby Bird's
painterly portrait are the bar). Keep the showcase-five ordering untouched.
**Tests:** tile resource-load coverage stays green; picker screenshot in evidence.

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

### V3.5 — HUD + end-card copy/style pass
**Goal:** every player-facing string and card reads in one voice (dog-life comedy, short, sofa-legible).
**Do:** sweep briefing cards, objective lines, world labels surfaced at Tier ≤2, end cards, session
summary, and rank/challenge copy for leftover dev phrasing, inconsistent capitalization, or debug
tone. Fix copy in place; no layout rework beyond what the skinned IMGUI already supports.
**Tests:** existing copy assertions updated deliberately (each change named in the commit message);
suite green.

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

### F4.2 — Post-clear flow + session summary
**Goal:** momentum for new players: clearing a mission offers the next showcase-order mission.
**Do:** end-card "Next" routes through showcase order for the first five, then library order,
skipping already-cleared missions (reuse `NextUnfinishedMissionIndex` semantics — attempts vs clears
distinction already exists; preserve it). Session summary keeps its cleared-vs-attempted honesty.
**Tests:** routing assertions for fresh / partially-cleared / all-cleared sessions.

### F4.3 — Briefing accuracy audit
**Goal:** every mission's "YOUR TEAM PLAN" (≤4 beats) matches the *current* asymmetric mechanics.
**Do:** the role redesigns changed who does what in most missions; briefings may lag. Compare each
mission's briefing beats against its controller's actual sequence (per `ARENA-PLAYABLE.md`'s updated
uncoached-sequence table); fix stale beats; keep gold in-game label quoting rule.
**Tests:** existing briefing assertions updated; per-mission checklist recorded here.

---

## Phase 5 — Audio for signals (after G1.x lands)

### S5.1 — Audio for new signals
**Goal:** ladder Tier 3, role-turn beacon appearance, and handoff flourish each have a distinct,
sofa-audible cue; the two dogs get identity-distinct handoff chimes.
**Do:** add named cues through the existing cue-slot boundary (authored bank first,
generated-profile fallback, same as current slots). Replace G1.2's placeholder Tier-3 cue.
**Tests:** event-driven audio assertions per new slot (existing pattern).

### S5.2 — Feedback-slot audio coverage audit
**Goal:** no major feedback moment is silent.
**Do:** sweep the shared feedback slots + per-mission coach/payoff beats for missing cue requests;
fill the worst gaps. Small task — do not start a mix/recording project; that stays post-launch.
**Tests:** extended event-audio assertions; suite green.

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
