# Next Production Slice

> **Status: ACTIVE.** This document is aligned with the depth-first pivot and is not superseded.

## Current status

Kitchen Falling Food Frenzy is implemented and playtest-ready. Its behavior was extracted behind
`IMissionController` on 2026-06-21 with the full 333-test PlayMode suite green. The baseline
readability fix verification remains deferred—not passed—because the owner was unavailable for a
couch session and explicitly authorized architecture work to continue. Operation Pee Break remains
the active authored deep slice and uses the controller boundary. Its first controller-owned
implementation now includes the four exact-combo beats, split-role position stations, recoverable
Teenager misreads, bladder/phone pressure, united-bark door climax, replay reset, and deterministic
PlayMode coverage. The controller boundary now also uses immutable validated context wiring and
atomic controller/definition registrations, with registry consistency coverage. Controller-owned
phone/bladder world meters now expose Beat-3 pressure, the phone drains only while Cocoa is actually
unplugging it, and the door-open climax now reveals a grass/hydrant/relief-sparkle gag instead of
only a generic sunbeam. Cross-mission cleanup/reuse is covered.

Mark the Yard is the first previously-in-`GameManager` mission extracted behind the controller
boundary. `MarkTheYardMissionController` owns zone geometry, claim-by-proximity ticking, the
squirrel reclaimer, scoring/credit/feedback, objective copy, entry staging, cleanup, snapshot, and
the deterministic `ForceClaimZone`/`ForceSquirrelReclaim` hooks. The contract grew by `OutcomeSummary`
(controller-owned end-of-round phrase) and `MissionContext.CreditDog` (MVP tally).

Gate Crash followed as the first extracted mission with a non-timeout failure: the contract added
`IsFailed`/`FailReason`, `CheckClear` now ends on clear or fail, and `EndReasonFor` prefers a
controller-supplied fail reason. `GateCrashMissionController` owns its gate/toy markers, hold/squeeze
proximity ticking, snap handling, snapshot, objective copy, and the `ForceGateHold`/`ForceGateCross`
hooks. Table Stealth followed the same hold/sneak puzzle shape behind `TableStealthMissionController`
(exposure-cap failure via `IsFailed`/`FailReason`), then The Ol' Switcheroo behind
`SquirrelSwitcherooMissionController` (bait/raid with backfire-cap failure), then The Walk Campaign
behind `WalkCampaignMissionController` (two-station message with misread-cap failure), then The Great
Escape behind `GreatEscapeMissionController` (alternating-owner contraption chain with botch-cap
failure), then Walkies on the Leash behind `LeashWalkMissionController` (controller-owned checkpoint
route, tether snaps, and snap-cap failure), then Sock Panic behind `SockPanicMissionController`
(controller-owned basket timing, partner-only sock dives, and fumbles), then Car Ride Balance behind
`CarRideMissionController` (controller-owned counter-lean ticking, timed lurches, and spill-cap
failure), then Scent Search behind `ScentSearchMissionController` (controller-owned sniff/dig input,
buried-spot selection, and cold-dig failure), then Weenie Roundup behind
`WeenieRoundupMissionController` (controller-owned carry state, bowl delivery, and fumble recovery).
Squirrel Conspiracy now follows behind `SquirrelConspiracyMissionController` (controller-owned
route/cutoff geometry, herding and taunt state, stash interaction, and failure outcome while using
the shared squirrel actor through the narrow context).
Snack Heist now follows behind `SnackHeistMissionController` (controller-owned collection/steal
state, squirrel pressure, bark defense, and collectible interpretation through narrow pool services).
The arena presentation pass now gives Operation Pee Break controller-owned room/couch/door/phone/
leash set dressing plus smaller silhouette details, Pass-2 phone-attention/battery/charger/door/
leash state props, Teenager comprehension/confusion reads and beat pips, Pee Break-specific end-card
replay copy with dog role credit, backyard
mission-readability districts with continuous fence, house/patio, central lawn, snack, laundry,
route, and back-door cues, and mission-select badges/thumbnails so the picker reads less like a debug
list. The cold-start picker now calls out Operation Pee Break as the couch-test focus, supports
F5/P/Y to highlight it without scrolling through the roster, and the mission briefing/live HUD expose
the P1 Cheddar / P2 Cocoa ownership line for first-minute couch clarity. Mission select and end cards
also surface small replay challenges, with Operation Pee Break explicitly chasing a Pawfect 0-misread
signal. All mission definitions now expose reusable couch-test presentation metadata for role hints,
mechanic families, scene cues, shared Cheddar/Cocoa presentation guidance, and readability flags, and
the picker/playtest overlay now surfaces a per-mission readability gate for the selected or active
mission. Operation Pee Break's picker details now present its authored 480-second timer as an `8m`
deep-slice session. The backyard now follows a one-background rule (2026-07-02): the painted plate
(`yard_backyard_plate_v02`) covers the full 120×68 yard at full opacity as the single background,
the bootstrap rectangles stay only as invisible anchors, and the photo-crop reskin layer is
retired. `ArenaWowSetDressing` keeps a lean authored-art accent layer (snapshot props, attract
parade, mission-reactive spotlight/spark) plus reusable animated motif families for every roster
mission, backed by generated transparent cartoon sprites in `ArenaFinal/Props/Wow`, with the source
atlas retained under `ReferenceOnly/GeneratedWow`; it no longer draws placeholder rectangles over
the plate. Operation Pee Break's
deep-slice room now also overlays generated transparent cartoon prop sprites from
`ArenaFinal/Props/PeeBreak`, with the atlas source retained under
`ReferenceOnly/GeneratedPeeBreakProps`, for the couch, distracted Teenager, phone/charger, open door,
leash, hydrant payoff, bladder gauge, and first misread tennis ball. The room also carries small
animated household details (blanket, socks, chew toy, Teenager hoodie/foot fidget, phone ping) so the
active deep slice reads as a lived-in dog-life scene before labels do the work. Generated Mission
Prop Pack Pass 2 now adds 30 reusable transparent cartoon sprites under `ArenaFinal/Props/Missions`, with
source notes/contact sheet in `ReferenceOnly/GeneratedMissionProps`, and wires the non-Pee roster's
visible focus props, hazards, pickups, and payoff stations through `MissionPropArt` overlays while
leaving shared Cheddar/Cocoa character presentation untouched. The Mission Prop Pack Pass 2
readability cleanup now dims fallback marker pads behind loaded prop sprites, scales down large
success/warning juice sprites, and improves the automated review harness staging for Weenie Roundup,
Walkies on the Leash, and Rube Goldberg/Chaos Machine. The dynamic treat-art stability pass now
preserves mission-specific collectible overlays after the older dynamic weenie enhancer scans spawned
treats, so Snack Heist snack plates, Sock Panic socks, and Blanket Catch falling snacks keep their
generated prop sprites instead of reverting to Backyard weenie art. Targeted final-art coverage
passed at `1/1` on 2026-06-29. Generated Environment Prop Pack now adds 17
transparent cartoon backyard district sprites under `ArenaFinal/Props/Environment`, with source
notes/contact sheet in `ReferenceOnly/GeneratedEnvironmentProps`, and layers them over the patio/back
door, fence rails, Pee Break outdoor route, snack/laundry districts, scent/leash routes, lawn
landmark, pond, shade tree, garden bed, flowers, picnic blanket, sandbox, stepping-stone path, and
eagle/coyote threat lane while preserving the generated nonblocking layout objects. Targeted
PlayMode coverage for final art loading plus backyard environment overlays passed at `2/2` on
2026-06-29 after the pass-2 expansion. Generated Building Prop Pack now adds 3 transparent cartoon
home-exterior sprites under `ArenaFinal/Props/Buildings`, with source notes/contact sheet in
`ReferenceOnly/GeneratedBuildingProps`, and layers the house facade, back-porch entry, and
nonblocking yard shed over the existing backyard house cluster. Targeted building/environment
coverage passed at `11/11` and final-art resource coverage passed at `1/1` on 2026-06-29.
Generated HUD Skin Pack now adds 6 transparent couch-test UI sprites under `ArenaFinal/UI/Hud`,
with source notes/contact sheet in `ReferenceOnly/GeneratedHudSkin`, and uses them for mission
picker panels, mission tiles, badges, button backs, mission briefing, pause, end cards, session
summary, selected-mission showcase, playtest overlay, and debug toggle surfaces while leaving IMGUI
text/hitboxes as the current input layer. Targeted HUD/environment coverage passed at `12/12` and
final-art resource coverage passed at `1/1` on 2026-06-29.
Generated World Label Skin Pack now adds 4 transparent couch-test label sprites under
`ArenaFinal/UI/WorldLabels`, with source notes/contact sheet in
`ReferenceOnly/GeneratedWorldLabelSkin`, and uses them for shared mission object labels, command/
warning labels, and score-pop bursts while preserving the existing `TextMesh` strings as the
gameplay-readable copy. Targeted environment/world-label coverage passed at `13/13` and final-art
resource coverage passed at `1/1` on 2026-06-29.
Generated Arena SFX profiles now replace the previous single tone/noise placeholder wave with named
procedural dog-life cue profiles for bark, team success, crunch collect, squirrel alarm, score
sparkle, penalty thunk, victory fanfare, failure sigh, UI blip, and threat rattle while preserving
the replaceable cue-slot boundary for future authored recordings and mix work. Targeted catalog
profile coverage passed at `1/1` and event-driven audio/rumble coverage passed at `1/1` on
2026-06-29.
Generated Level Area Prop Pack now adds distinct transparent cartoon area plates under
`ArenaFinal/Props/LevelAreas`, with source notes/contact sheet in
`ReferenceOnly/GeneratedLevelAreas`: Kitchen Falling Food Frenzy gets an indoor tile/counter area,
and Car Ride Balance gets a car cabin plus narrow balance lane so it no longer reads like the shared
backyard. The area roots are controller-owned, decorative-only, and preserve existing mission
markers/colliders.
Generated P0 mission-state packs now add state-specific transparent cartoon sprites under each
mission's `ArenaFinal/Props/<Mission>/` folder, replacing more single-state colored-block reads for
trap gaps, guard lanes, basket/sock windows, cutoffs, threat states, cargo, dig patches, storm cues,
yard zones, checkpoints, vehicle lurches, gates, human/steak beats, decoys/stashes, leashes, bone
mounds, escape stations, blankets/snacks, Kitchen food states, and Chaos Machine lever states while
preserving controller-owned markers and deterministic hooks as the gameplay authority. Targeted
final-art integration coverage passed at `8/8` and the full PlayMode suite passed at `400/400` on
2026-07-01.
Generated Gameplay Cue Pack now adds 5 transparent cartoon
cue sprites under `ArenaFinal/UI/Cues`, with source notes/contact sheet in
`ReferenceOnly/GeneratedGameplayCues`, and uses them for dog-mounted objective arrows plus bark, tug,
and rescue range indicators while preserving objective copy and range radii. Targeted final-art and
mission-loop cue coverage passed at `2/2` on 2026-06-29. Generated Dog FX Pack now adds 6
transparent dog-local sprites under `ArenaFinal/VFX/Dog`, with source notes/contact sheet in
`ReferenceOnly/GeneratedDogFx`, and uses them for action particles, Cheddar/Cocoa paw trails, ground
glow, sparks, queen glints, and collar glints instead of white-square-only geometry. Targeted dog-FX
coverage passed at `6/6` on 2026-06-29. Generated Kitchen Cue Pack now adds 4 transparent
falling-food warning sprites under `ArenaFinal/UI/KitchenCues`, with source notes/contact sheet in
`ReferenceOnly/GeneratedKitchenCues`, and uses them for Kitchen gold-food/purple-onion counter
telegraphs plus floor landing warnings. Targeted Kitchen cue coverage passed at `5/5` and the
resource/art contract checks passed at `1/1` each on 2026-06-29. Generated Chaos Machine Prop Pack
now adds 3 transparent Rube Goldberg station sprites under `ArenaFinal/Props/ChaosMachine`, with
source notes/contact sheet in `ReferenceOnly/GeneratedChaosMachineProps`, and uses them for the
towel-drop, basket-tip, and toy-launch junctions while preserving cascade logic. Targeted Chaos
Machine coverage passed at `9/9` and final-art coverage passed at `8/8` on 2026-06-29. The latest
full PlayMode suite after the generated P0 mission-state art pass was green at `400/400` on 2026-07-01, and the
rebuilt macOS dev player generated a valid 66-frame art-review pass at
`unity/builds/art-review-current/arena-art-review-contact-sheet.jpg`. The signal-language pass
(2026-07-02) began retiring shouty world text in favor of icon signals: urgent actor states
(`SetActorState` pulse `0.26+`) now raise a distance-visible, text-free `ActorSignalBadge` drawn
with the authored warning/command label skins, starting with Backyard Rescue's stealing squirrel
and the shared predator warning, while sentence text stays a close-range/debug prompt
(`ActorSignalBadgePlayModeTests`; full suite green at `410/410` on 2026-07-02). Slice 2 raised Mark
the Yard's active steal prowl into the same urgency channel. Slice 3 (2026-07-03) raised the
remaining timed act-now windows into the channel — Sock Panic's open-basket dive window (command
badge), Eagle Shadow's talon-grip rescue (warning badge during Cheddar's wiggle phase, command
badge during Cocoa's pull window), and Car Ride's near-spill tilt at 70%+ (a `SPILL WARNING` state
with a warning badge) — and labels ending in `NOW!` now classify as the authored command skin. The `IMissionController` migration also
completed on 2026-07-02: Eagle Shadow Panic and Coyotes at the Fence were extracted behind the
controller boundary (`MissionContext.PredatorObject`, `IMissionUnitedBarkListener`, static
`Compute*` geometry helpers), so all 22 selectable missions are controller-owned and
`GameManager.BuildMissionDefinition` resolves exclusively through the registry. Placeholder
presentation still needs the second two-player couch acceptance pass.

Gameplay-first presentation is now the active content approach: use generated Unity primitives,
labels, role pads, and authored feedback to prove that Operation Pee Break is readable and fun before
returning to realistic backyard/background replacement. The editor-only
`GameplayFirstPlaytestLab` exists for this blockout and acceptance work; it does not add missions or
change the frozen roster.

As of 2026-06-20, `GameManager.cs` is nearly 8,000 lines and declares 21 mission variants. Treat
that as a date-stamped warning, not a permanent metric or a line-count target.

## Canonical work sequence

1. Run a baseline two-player couch playtest of the existing slices. Use two physical controllers
   and include Backyard Rescue, Blanket Catch, and Kitchen Falling Food Frenzy.
2. Address critical playtest findings before architecture or content work.
3. Define `IMissionController` and a narrow `MissionContext` using the ownership boundary in
   `ARCHITECTURE.md`.
4. Extract the existing Kitchen mission first. Preserve behavior and keep the full PlayMode suite
   green before proceeding.
5. Build Operation Pee Break entirely through the new controller structure described in
   `DEEP-SLICE-OPERATION-PEE-BREAK.md`.
6. Run a second two-player couch playtest. This is the deep-slice acceptance gate; automated tests
   cannot substitute for it.
7. Keep the mission roster frozen until that gate passes.

## Couch test #4 runbook (the open acceptance gate, ready as of 2026-07-03)

Status: sequence steps 1-5 are complete on `main` (all 22 missions run through
`IMissionController` via `MissionControllerRegistry`; suite 428/428 green at `d096e4e`). Couch
tests #2 and #3 produced presentation feedback but no accept/reject verdict on the deep slice, so
**step 6 has never formally passed**. One sitting (Ken + Sue, two controllers) covers both halves:

**Half 1 — verify the couch-test-#3 fixes (10 min):**

1. Mission select: HOW TO PLAY reads as short bullets, with on-screen labels (e.g.
   `SQUIRREL STEALING - BARK!`) shown in gold exactly as they appear in-game.
2. Backyard Rescue: no CHEDDAR/COCOA name text floats over the dogs (WOOF! flash still fires).
3. Backyard Rescue: no floating sparkle-bone fake collectible anywhere in the yard.
4. Backyard Rescue: yard shows only the painted plate + real props (no snack-table/laundry art);
   start Snack Heist / Sock Panic and confirm their district art returns.
5. Eagle (Backyard Rescue predator warning or Eagle Shadow Panic): flaps its wing frames, banks
   into climbs/dives, and swells/shrinks subtly with its glide — no balloon scale-pulse.
6. Pool loop: run a dog across a drifting floatie (slightly faster) → step off (splash, dog slows
   and paddles low in the water — not the run animation) → swim to any edge (rooted shake with
   droplets, then steps out wet). The water is the real pool-patio art, not a stretched pond.
7. Mission briefing + in-game HUD: opaque dark cards/bands, all text readable from the sofa.

**Live-feedback additions landed mid-test (2026-07-04):** the couch-test-#4 sitting already
produced fixes on `main` (pool water alignment, donut floaties, wet-dog tint, detail-panel fit —
`799c277`) plus a pacing request: levels should open with a short discovery beat. That shipped as
the sniff-around lead-in (see `docs/ARENA-PLAYABLE.md`): the round clock and all threat schedules
hold still through the briefing card + a 2.5s open-yard countdown, and any bark/interact/grab
skips straight to GO. Verify during the remaining sitting: timer frozen until GO, and a bark
during the briefing card starts play instantly.

**Half 2 — the actual gate:** play Operation Pee Break start to finish (it is the couch-test
focus shortcut on the picker). Keep the laugh log from `docs/FAMILY-SHOWCASE-MANUAL-TEST.md`
running throughout — not just a defect list, every laugh/surprise/quote in the moment. Then call
it, one of:

- **ACCEPTED** — record the date here and the roster/roadmap freeze lifts, or
- **REJECTED** — list exactly what failed; that list becomes the next work queue.

Verdict: _pending_. Half 2 has not been played yet — this requires Ken + Sue on the couch with two
controllers; it cannot be run or simulated from the terminal. Report back what happened (pass/fail
on Pee Break end-to-end, plus the laugh log) and this line gets the date and verdict.

### Roster-wide funny-failure audit (2026-07-05, provisional — done ahead of the Half 2 verdict)

With the owner's explicit go-ahead to start this work before the couch-test-#4 gate closes: audited
every controller-owned mission's `IsFailed` trip point for the depth bar's "funny failure everywhere"
rule (`DESIGN-REVIEW-2026-06.md`). Most of the roster already clears it — GateCrash, TableStealth,
CarRide, LeashWalk, Eagle Shadow Panic, Snack Heist, Squirrel Conspiracy, Coyotes Fence, and the
squirrel-steal path all already fire a `SetJuice`/`SpawnWorldPop`/`RequestAudioCue` gag on the exact
event that trips the fail cap, not just a silent counter increment. The one real gap was
**Thunderstorm Comfort**: `PanicMeter.Maxed` was a passively-computed threshold with no broadcast
anywhere, so a dog's panic could silently cross 1.0 and the only sign was the generic end card a
beat later. Fixed in `ThunderstormComfortMissionController` (`CheckBolt()`): fires a one-shot
"{DOG} BOLTED!" world-pop, cue, and audio the exact frame either pup's panic maxes, whether that
happens from a thunderclap spike or passive drift while apart. Covered by
`ThunderstormComfort_Bolt_FiresADistinctGagNotJustTheEndCard`; full suite green at `459/459`.

Note for whoever does the next mission in this pass: the shared `EndRound(false)` juice
(`"SAD FLOP REPLAY!"`) fires immediately after any controller's own fail-gag `SetJuice` call in the
same frame (`Tick()` and `CheckClear()` run back-to-back), and `FinalJuiceEffect` only keeps one
sprite-pop slot alive, so the controller's own juice sprite never actually renders — only its
`SpawnWorldPop` text survives, since those are independent GameObjects. Test against `MissionWorldPop`
(see `HasWorldPop` in `CarRidePlayModeTests.cs`), not `LastJuiceLabel`, or the assertion will flake
against clobbering that's cosmetic, not a real bug. Fixing that clobbering itself would be a much
bigger, roster-wide `EndRound`/`FinalJuiceEffect` change — out of scope for a per-mission pass.

Deliberately did **not** touch the `EndRound`→end-card→Replay flow itself: Replay is already
single-button and frame-instant (`R`/`Enter`/`Start`, no scene reload), confirmed by existing tests
that assert `Phase == GameOver` one frame after a forced fail — so "fast re-entry" was already true
roster-wide and didn't need rework.

## Architecture guardrails

- `GameManager` owns orchestration, mission selection, session flow, and shared-service wiring.
- Controllers own mission-specific setup, state, ticking, input handling, cleanup, outcome, and
  snapshots.
- Mission definitions and controller registration live outside `GameManager`.
- Migration is one mission at a time and test-green after every extraction.
- Completion is defined by ownership and behavior, not an arbitrary line-count target.

Broad roadmaps, backlog items, progression work, and additional mission ideas are deferred until
the second couch-playtest gate passes.
