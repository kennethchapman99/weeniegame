# Pre-Launch Production Plan

> **Status: ACTIVE.** Written 2026-07-17. This is the master plan for taking the current 23-mission
> Unity build from "couch-test response build" to **pre-launch ready**. The execution queue that
> implements this plan is `docs/AGENT-WORK-QUEUE-PRELAUNCH.md`. Neither document adds missions,
> reorders the seven canonical gates, or substitutes for the pending two-human Operation Pee Break
> couch retest (`docs/OPERATION-PEE-BREAK-COUCH-RETEST-2026-07-14.md`).

## What "pre-launch ready" means

New people — not Ken and Sue, people who have never seen the game — can sit down with two
controllers and:

1. **Start playing without a host.** Cold boot → Play Recommended → understand the controls from
   the visual card → play. No verbal coaching required.
2. **Always know what to do next, and which dog does it.** Every mission communicates the current
   step, the owning dog, and the handoff moment through in-world signals (pose, prop state, arrow
   icon, breadcrumb, role beacon) — with an escalation path so a stuck pair gets progressively
   stronger help instead of quitting, and a curious pair still gets to discover.
3. **See consistent, polished cartoon art throughout.** One visual language across missions, HUD,
   and tiles. Generated art is acceptable where it matches the authored style; visible colored-square
   or debug-text first reads are not.
4. **Feel animation, not teleporting state.** Dogs visibly perform their verbs (interact, dig,
   sniff, carry); NPCs act their states; payoffs animate rather than freeze.
5. **Fail funny and recover fast.** Every wrong-role or mistimed attempt produces a readable,
   recoverable gag (largely done — needs a final audit, not a rebuild).

Explicitly **not** required for pre-launch ready: final hand-authored art pipeline, final UI
framework (skinned IMGUI stays), recorded VO/final mix, Steam/meta features, new missions. A few
rough corners are acceptable; unreadable or inconsistent ones are not.

## Where we are (2026-07-17)

- All 23 missions run through `IMissionController` / `MissionControllerRegistry`; full PlayMode
  suite green at 565/565 on the working tree.
- The 2026-07-14 couch test **rejected** the deep slice on: unclear station order/feedback, abstract
  circles, no room toys, numeric bladder, missing control instruction. The response pass shipped
  (hidden circles + arrows/signals, movable toys, BLADDER meter, visual control cards, opening
  explainer). The cold two-human retest has **not run** — that human gate stays open and stays owned
  by Ken and Sue, not agents.
- Codex's finishing pass (uncommitted at time of writing) lands: roster-wide asymmetric role
  redesigns with wrong-role coaching and held live payoffs, quality-ordered showcase selector,
  three-paw scent breadcrumb wayfinding on the shared `TryGetObjectiveTarget` seam, authored-quality
  Car Ride backseat art, and the doc syncs recording all of it. **Nothing in this plan starts until
  that work is committed and the tree is clean.**
- Known placeholder debt (from `ARENA-PLAYABLE.md`): dog/threat sprites are promoted motion frames,
  wow/motif and prop art is generated cartoon PNG, some large districts still lean on generated
  geometry, IMGUI HUD is skinned but not final, audio is authored-cue-bank-plus-generated-fallback.

## The central design problem this plan solves

The couch tests keep saying the same thing in different words: **players don't reliably know which
dog acts next, on what, and when — and when they stall, nothing rescues them.** The Codex pass gives
every mission asymmetric roles, per-dog arrows, breadcrumbs, and wrong-role coaching. What is still
missing is a single, roster-wide **guidance grammar** with a **stall-aware escalation ladder**:

- **Tier 0 — Discovery (always on):** what already exists. Arrow icon, three-paw breadcrumb, prop
  silhouette/state, proximity-gated contextual labels. Quiet enough that players explore.
- **Tier 1 — Nudge (~12s without progress):** the current objective prop pulses, the owning dog's
  arrow and breadcrumbs brighten, the owning dog does a head-turn toward the target.
- **Tier 2 — Coach (~25s):** the current step's contextual label becomes visible beyond its normal
  proximity gate, and a **role-turn beacon** (small paw badge in the owning dog's identity color)
  appears over the target prop. Partner's HUD chip pulses when the step belongs to the other player.
- **Tier 3 — Rescue (~45s):** the HUD objective line flashes with the owning dog named, plus a
  distinct "coach woof" audio cue. This is the ceiling — never auto-complete a step.

Any real progress signal (positive score gain, objective-copy change, beat/stage advance) resets the ladder to
Tier 0. The ladder is frozen during briefings, explainers, countdowns, held payoffs, end cards, and
pause. Timings are tunable per mission; timing-critical windows may cap at Tier 2. F1 diagnostics
remain unchanged and separate. The ladder state is observable in the F1 overlay and logged, so the
next couch test produces per-mission stall evidence instead of anecdotes.

This one system is the highest-leverage item in the plan: it converts "balance discovery and not
getting stuck" from a hope into a tested mechanism, roster-wide, without touching mission rules.

## Workstreams, in priority order

| # | Workstream | Why it's this priority | Queue phase |
|---|---|---|---|
| 1 | Guidance grammar + stall escalation ladder | Directly fixes the rejection findings' root cause; benefits all 23 missions at once | Phase 1 |
| 2 | Role/handoff signaling (turn beacon, flip flourish, wrong-role audit) | "Which dog, when" is the co-op-specific half of readability | Phase 1 |
| 3 | Animation: verbs and NPC acting | Dogs that visibly perform interact/dig/sniff/carry make signals diegetic instead of UI | Phase 2 |
| 4 | Art consistency sweep | "Consistent and polished throughout" — kill square/debug first reads, unify style, tiles, staging | Phase 3 |
| 5 | First-session flow | Cold-start pickup for strangers: control reminders, next-mission flow, briefing accuracy | Phase 4 |
| 6 | Audio for the new signals + coverage audit | Signals need sound to land from the sofa | Phase 5 |
| 7 | Evidence + gate refresh | Rebuild, re-hash, update manual test docs, hand the couch retest to humans | Phase 6 |

Phases 2–4 are parallelizable after Phase 1 lands; Phase 1 tasks are sequential because they build
one shared system. The queue document orders and sizes every task.

## Hard constraints (carried from CLAUDE.md / active docs, restated for agents)

- Unity project only. Frozen TS/Canvas and prototype stay untouched.
- **Roster frozen at 23 missions.** No new missions, no mission-rule redesigns. This plan is
  presentation, animation, guidance, and polish only. If a task seems to need a rule change, stop
  and flag it instead.
- Mission-specific state stays in controllers; shared services (the guidance ladder, signal
  renderers, audio routing) belong in shared code wired through `GameManager`/`MissionContext`
  seams — never as per-mission `GameManager` branches.
- Full PlayMode suite green after every task; new behavior gets a deterministic test that fails
  without the change. Run `./unity/run-playmode-tests.sh`.
- Visual changes get art-review-harness evidence (`--arena-art-review=<path>`) and a manual
  acceptance note in the relevant doc.
- Generated art continues through the `tools/art/*.py` → `Assets/Art/Resources/ArenaFinal/...`
  pipeline with sources in `ReferenceOnly/`; match the established cartoon style (chunky outline,
  saturated flat fills, soft two-tone shading).
- Bark stays gameplay-relevant; Cheddar reads chaos-puppy, Cocoa reads veteran-queen, in every new
  signal and animation choice.
- The Operation Pee Break two-human retest is the acceptance gate. Automated evidence prepares it;
  only humans can pass it.

## Definition of done for the whole plan

- Every Phase 1–5 queue task closed with its listed evidence.
- A fresh dev build passes smoke, the full suite is green, and the 69-frame art-review capture shows
  no square-first or debug-text first reads at 1080p with F1 off.
- `docs/OPERATION-PEE-BREAK-COUCH-RETEST-2026-07-14.md` preflight refreshed with the new hashes.
- `docs/FAMILY-SHOWCASE-MANUAL-TEST.md` updated so the host script exercises the guidance ladder
  (deliberately stall once, watch the tiers) and the new signals.
- Then: the couch retest with two humans — and a third session with **non-family first-timers** if
  available, since "new people can pick it up" is the pre-launch bar.
