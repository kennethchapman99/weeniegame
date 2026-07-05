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

**Update (2026-07-05, later the same day):** the clobbering described in the note below has since been
fixed roster-wide — see "Roster-wide 'reaction sprite never actually rendered' bug class" further down.
`FinalJuiceEffect.Spawn()` now only replaces the previous pop across frames; same-frame spawns (like a
controller's own fail-gag pop immediately followed by `EndRound`'s generic "SAD FLOP REPLAY!") stack
with a vertical offset instead of clobbering each other. Both sprites now actually render. The original
note is kept below for history, but the "out of scope" caveat no longer applies - it shipped.

Note for whoever does the next mission in this pass (historical, see update above): the shared
`EndRound(false)` juice (`"SAD FLOP REPLAY!"`) fires immediately after any controller's own fail-gag
`SetJuice` call in the same frame (`Tick()` and `CheckClear()` run back-to-back), and `FinalJuiceEffect`
only kept one sprite-pop slot alive, so the controller's own juice sprite never actually rendered —
only its `SpawnWorldPop` text survived, since those are independent GameObjects. Test against
`MissionWorldPop` (see `HasWorldPop` in `CarRidePlayModeTests.cs`), not `LastJuiceLabel`, or the
assertion will flake against clobbering that's cosmetic, not a real bug.

Deliberately did **not** touch the `EndRound`→end-card→Replay flow itself: Replay is already
single-button and frame-instant (`R`/`Enter`/`Start`, no scene reload), confirmed by existing tests
that assert `Phase == GameOver` one frame after a forced fail — so "fast re-entry" was already true
roster-wide and didn't need rework.

### Follow-up polish pass (2026-07-05, continued autonomously per owner's "keep polishing" directive)

Picked up the note above: fixed the roster-wide `FinalJuiceEffect` same-frame clobbering bug instead
of leaving it deferred. Same-frame `SetJuice` calls (a controller's own fail/success gag immediately
followed by `EndRound`'s generic `"SAD FLOP REPLAY!"`/`"... POP!"`) now stack with a small vertical
offset instead of the second call destroying the first before Unity ever renders it — so every
mission's own gag sprite actually shows, not just its `SpawnWorldPop` text. Cross-frame spawns still
replace as before (no clutter buildup). One test added
(`ThunderstormComfort_Bolt_JuicePopSurvivesTheEndRoundClobber`, reusing the bolt scenario since it's
exactly this double-`SetJuice` same-frame case). Suite green at `460/460`.

Also fixed a real "scene state reset" gap named by CLAUDE.md's hard-rule list but not actually covered:
`GameManager`'s mission-start dog reset (`SetMode(Free)`/`SetTravelAssist(false)`/etc.) never cleared
`DogController`'s wet-timer overlay (or `Zoomies`/`OnFloater`), so a dog wet from the backyard pool
could carry the wet tint into a brand-new round — even an indoor mission with no pool at all, since
the timer just counts down on its own schedule regardless of what mission is active. Added
`DogController.ResetMissionOverlays()` and call it alongside the other per-dog resets in `StartMission`.
Extended `PoolZone_DogOnFloater_StaysDryAndPoolClosesIndoors` to set the dog wet before switching to
Kitchen and assert `IsWet` is false after. Suite green at `460/460` (test count unchanged — extended an
existing test rather than adding a new one).

Audited for the same class of bug elsewhere (other single-slot "last write wins" feedback channels,
other per-dog overlay fields, `MissionScopedScenery`'s live per-frame mission-variant check) and found
no further instances — `RequestAudioCue`/`SpawnWorldPop`/`ClearMissionPose`/score fields already reset
or layer correctly.

Also closed the `ARENA-PLAYABLE.md` "duplicate rescued/proud and rope tug/complete source poses" note:
`rope_tug.png`/`rope_complete.png` were byte-identical (confirmed via `md5`) because
`export_arena_final.py` cropped the same box for both — `BackyardRescueArtEnhancer.CompleteTug`'s
overlay swap to `RopeComplete` on tug clear had zero visible effect. Fixed with a new
`tools/art/generate_rope_complete_variant.py` (golden tint + sparkle overlay derived from the exported
`rope_tug.png`), and removed the redundant crop from the export table so a future re-run can't recreate
the duplicate. Covered by `RopeTugAndRopeComplete_AreVisuallyDistinct`. Ran the same duplicate-hash scan
(`find .../ArenaFinal -iname "*.png" | xargs md5 | sort | uniq -d`) across the *entire* generated-art
tree and found only two other duplicate groups, both confirmed intentional aliases in the generator
scripts (not bugs) — so rope was the only real "same slot needs to visibly change state" gap in the
whole library. The dog `_proud`/`_rescued` duplicate is real too but never visible in practice
(`ApplyPose` immediately overrides it with genuinely distinct Motion-frame art from
`export_character_outcomes.py`), so left alone.

Also added three running gags from `GAME-DESIGN-BIBLE.md`'s list that were named but never built, all
in `BackyardRescueArtEnhancer` (same purely-decorative risk profile as the existing untested ambient
leaf pop — no scoring or mechanic changes): Cocoa's sunbeam-ownership pose beat (`SunbeamClaimCount`),
the squirrel's villain-monologue mumble (`SquirrelMumbleCount`, explicitly gated off whenever
`ActorSignalBadge.IsShowing` so it never competes with the real steal-warning signal), and the toy-envy
tug tell (`ToyEnvyCount` — fires only when both dogs claim the rope at once and neither is already
`Busy` in a real Tug interaction, so it never masks the actual co-op tug objective; this is the one
genuine two-dog interaction of the three, versus the other two being solo idle beats). Noted all three
in `FAMILY-SHOWCASE-MANUAL-TEST.md`'s Backyard Rescue watch-list for the next couch session. Suite
green at `464/464`.

Added a fourth gag later the same session: the treat-reverence pause (`TreatReverenceCount` —
"dropped food has religious significance"; fires when a dog idles within 2 units of an uncollected
treat, well outside the treat's own 0.6-unit trigger collider, so it can never interfere with real
collection). Noted in the family-showcase watch-list alongside the other three. Suite green at
`471/471`.

And a fifth: the pool-fascination flinch (`PoolFascinationCount` — "the pool is both terrifying and
fascinating"; fires when a dog idles right at `BackyardPoolZone.WaterRect`'s edge while not already
`Swimming`/`Shaking` or inside the rect, so it never competes with the real pool mechanics). Suite
green at `472/472`.

While adding those gags, found a real pre-existing cross-mission bug in the same file:
`BackyardRescueArtEnhancer.ReactToFeedback`/`ReactToScore` had no `ActiveMissionVariant` gate at all
(unlike the new gags, which all correctly check it). Every mission shares
`FeedbackKind.LevelClear`/`GameOver` (set in every `EndRound` call), and `"WEENIE"`/`"SNACK"`/`"SOCK"`
score labels are reused by Weenie Roundup, Snack Heist, Sock Panic, and Blanket Catch — so every one of
those missions' clear/fail/pickup moments was spawning a Backyard-only rope/squirrel/predator sparkle
at wherever those inactive-but-not-null objects happened to be sitting, with no relation to what the
player actually just did. Gated both reactions to `BackyardRescue` while still tracking last-seen
feedback/score values every frame (so switching back into Backyard Rescue later doesn't compare against
a stale value from an intervening mission). Checked the only other 3 files touching
`LastFeedback`/`LastScoreEventLabel`/`OnJuiceFeedback` (`ArenaHud`, `FinalJuiceEffect`, `GameManager`)
and confirmed this was isolated to this one file; also confirmed `ArenaWowSetDressing` (the actually
roster-wide wow layer) is correctly per-mission-scoped already, not the same bug. Covered by
`ReactToFeedbackAndScore_OnlyFireDuringBackyardRescueNotOtherMissions`. Suite green at `465/465`.

Applying the same "does this timed reaction actually expire" lens roster-wide: `BoneRelayMissionController`'s
mound Found/Wrong sprite override (`_moundOverrideArt`) had no expiry at all, unlike the identical
pattern already correct in `GreatEscapeMissionController`/`ChaosMachineMissionController`
(`_stationOverrideUntil`), `BlanketCatchMissionController` (`_fallingOverrideUntil`), and
`SquirrelSwitcherooMissionController`/`GateCrashMissionController`/`TableStealthMissionController`/
`WalkCampaignMissionController`/`MarkTheYardMissionController` (their own `*ReactionUntil` fields, all
verified correctly set/reset/checked). Simulated `CoopScentRelayPuzzle`'s LCG target sequence across
2000 seeds and confirmed BoneRelay's fixed 4-mound/3-find configuration happens to never repeat a
mound index within one round (an incidental property of the RNG constants mod 4, not a documented
guarantee) — so this wasn't currently causing a visible bug, but it was the one sibling controller not
defending against a re-called mound getting stuck on a stale "already dug" sprite. Brought it in line
with the same timed-override pattern. Covered by `Bone_MoundOverrideExpiresInsteadOfStayingStuck`.
Suite green at `466/466`.

### Roster-wide "reaction sprite never actually rendered" bug class (2026-07-05)

Found by asking a new question of the same shape: does a controller's reaction sprite survive long
enough to actually be drawn, or does something hide/destroy/overwrite the same object in the same
synchronous call before Unity renders that frame? This turned up **four** real instances, all fixed:

1. **Kitchen Falling Food Frenzy**: `ResolveCatch`'s `UnsafeLanding` case and `ResolveLetFall`'s
   `DodgedBad`/`MissedGood` cases set `_foodArt` to `KitchenFoodSplat` immediately followed by
   `HideFood()` (`_foodObject.SetActive(false)`) in the same `Tick()` — the falling food just vanished
   instead of visibly splatting. Fixed with `HideFoodAfterSplat()`, a `_foodHideAt` timer checked at
   the top of `Tick()`. Covered by `KitchenFrenzy_SplatSpriteLingersBeforeFoodHides`.
2. **Blanket Catch**: `Tick()` called `HandleProgress()` (sets the Caught/Splat reaction sprite) then
   immediately `SpawnItem()` in the same call, which reset the sprite and repositioned the item back to
   the top before it ever rendered. Trickier than Kitchen's fix: naively delaying `SpawnItem()` would
   leave `_itemY` at/below `CatchLineY`, re-triggering `TryCatch` every frame until respawn. Added a
   `_respawnAt` timer that short-circuits the whole fall/catch block while a reaction is lingering, so
   the resolved item just holds still at the catch line. Covered by
   `Blanket_CaughtSpriteLingersBeforeItemRespawns` (drives a real fall through `Tick()`, since the
   `ForceBlanketCatch` test hook doesn't exercise this code path at all).
3. **Sock Panic / Snack Heist / Backyard Rescue's trap recovery**: all three set a reaction sprite
   directly on a `Treat` (Decoy/Saved/Stashed/Stolen) immediately before calling
   `_context.RecoverCollectible`/`ReplaceCollectible(treat)` — which called `Destroy(treat.gameObject)`
   synchronously. Unity defers actual destruction until after `Update()` but still before that frame
   renders, so none of those sprites ever appeared. Fixed at the root instead of patching three
   controllers separately: `GameManager.RecoverControllerCollectible`/`ReplaceControllerCollectible`
   now disable the treat's `Collider2D` immediately (so a dog re-entering the trigger during the linger
   can't double-collect an already-resolved treat) and call `Destroy(obj, 0.5f)` instead of an
   immediate `Destroy()`. This one fix retroactively covers every current and future call site,
   confirmed by checking `SnackHeistMissionController.StealTarget`'s "Stolen" sprite case (same
   `ReplaceCollectible` call, now automatically correct with no extra changes needed). Covered by
   `SnackHeist_StashedSpriteLingersBeforeTreatDestroyed`.

Checked for any remaining instance by grepping every `Destroy(` call across `GameManager.cs` and every
`*MissionController.cs` — the only other call sites are bulk-cleanup paths (`ClearTreats()`,
`Cleanup()`'s level-area teardown, the generic squirrel-steal-a-treat path with no reaction sprite of
its own) where an immediate destroy is correct. This bug class is now closed roster-wide. Suite green
at `469/469`.

### End-card flavor-text gap (2026-07-05)

Four missions (`BackyardRescueMissionController`, `SnackHeistMissionController`,
`KitchenFoodFrenzyMissionController`, `SockPanicMissionController`) returned `null` for
`OutcomeSummary`, so their end cards fell back to the generic `Outcome.ToString()`
(`"Clear"`/`"Failed"`) instead of a distinct 2-4 word flavor phrase like all 18 other missions get from
`MissionOutcomeSummaryBuilder`. Added inline summaries in the same tone: Backyard Rescue ("Backyard
Secured" / "Squirrel Got Some" / "Still Defending"), Snack Heist ("Stash Secured" / "Squirrel Union
Wins" / "Still Guarding"), Kitchen ("Dinner Rush Survived" / "Kitchen Disaster" / "Still Cooking"), Sock
Panic ("Socks Rescued" / "Laundry Day Chaos" / "Still Diving"). Added a new `SockPanicPlayModeTests.cs`
(this mission had no dedicated test file at all) and fixed 4 now-stale `ArenaGameLoopPlayModeTests.cs`
assertions that checked the old generic fallback text — two of which are only reachable via a forced
game-over that bypasses the controller's own `IsFailed`, so they correctly read as an in-progress
flavor ("Still Defending"/"Still Guarding") rather than a fail phrase. Suite green at `470/470`.

### Running-gag flourishes continue past Backyard Rescue (2026-07-05)

Two more of `GAME-DESIGN-BIBLE.md`'s 12 Running gags shipped after the Backyard Rescue set of five:
the treat-reverence pause ("dropped food has religious significance") and the pool-edge flinch ("the
pool is both terrifying and fascinating"), both in `BackyardRescueArtEnhancer`. Then a sixth gag moved
to a *different* mission for the first time: **Gate Crash**'s `TrySpawnDoorOutrage()` ("Cheddar
believes every closed door is a personal attack") lives directly in
`GateCrashMissionController` rather than a shared enhancer, since Gate Crash has no standalone art
enhancer of its own — Cheddar idling within 2.2 units of the closed, unbraced gate fires a cosmetic
warning-pulse + "HOW DARE YOU" world-pop, gated off whenever the gate is actually held open or mid-snap
reaction. Purely cosmetic, no scoring/mechanic effect, same template as the Backyard Rescue gags (timer
field initialized in `StartMission()`, public `TrySpawnX()` test hook, two-assertion test). Covered by
`GateCrash_DoorOutrageGag_FiresWhenCheddarIdlesAtTheClosedGateAndStaysQuietWhileHeld`. Suite green at
`473/473`. Six of twelve running gags now implemented; remaining candidates worth a similar per-mission
look: "every walk is an intelligence-gathering mission" (Leash Walk checkpoints), "the couch is sacred
land" and "human legs are moving environmental hazards" (no current mission has a literal couch or
walking-human hazard yet), "just one more nail" (no obvious mission fit yet).

Shipped the Leash Walk one next: `LeashWalkMissionController.TrySpawnIntelGathered()` fires a cosmetic
"INTEL GATHERED" pulse+pop when one dog is alone at the current checkpoint and the other is still
lagging behind (both arriving together reads as "we made it", not scouting, so that case stays quiet).
Same template again - cooldown timer initialized off the `0` default in `StartMission()`, public test
hook, two-assertion test (`LeashWalk_IntelGatheredGag_FiresWhenOneDogScoutsAheadAloneAndStaysQuietWhenBothArrive`).
Suite green at `474/474`. **Seven of twelve** running gags now implemented.

### Stale mission-prop overlays bleeding across missions on shared actors (2026-07-05)

Found by asking: for any mission that promotes a `MissionPropArtAttachment` overlay onto one of the
*shared* actors (Squirrel, Predator - reused across most of the 22-mission roster, not recreated per
mission), does anything ever undo that overlay when a different mission reuses the same actor? It did
not. Eagle Shadow Panic's talon-grip sprite (set on the squirrel object during the snatch/rescue beat)
and Coyotes Fence's pinned-gap post sprite (set on the predator object) both dim that actor's generated
fallback body to ~10-14% alpha so the overlay reads as the primary art, via
`MissionPropArtAttachment.CapFallbackAlpha`. Nothing in the codebase ever raised that cap back up or
hid the overlay - `ApplyFallbackAlphaCap` only ever lowers alpha, never restores it. Since the overlay's
`GameObject` lives directly on the shared actor and is never destroyed between missions, playing Eagle
Shadow Panic (or Coyotes Fence) even once and then switching to *any other* squirrel/predator-using
mission left that leftover sprite (and the dimmed body under it) visible for the rest of the session -
including a replay of the *same* mission (Coyotes Fence restarting already showing a previously-pinned
gap before the player re-pins anything).

Fixed at the root: added `MissionPropArtAttachment.ClearOverride()` (hides the overlay, restores full
fallback alpha) and wired it into `GameManager.StartMission()`'s shared per-round reset for
`SquirrelObject` and `PredatorObject` - every mission now starts both actors clean, and any controller
that wants its own overlay lays it down fresh via its existing `Init()`/`SetSprite()` calls (which now
also re-shows the overlay, undoing an earlier clear). Covered by
`EagleShadowPanic_TalonGripOverlay_DoesNotBleedIntoTheNextMission`. Suite green at `475/475`.

### Coyotes Fence gap art not resetting on replay (2026-07-05)

While in the neighborhood of the shared-actor overlay fix, checked whether `CoyotesFenceMissionController`'s
own controller-owned gap markers had the analogous bug within a single mission's replay loop, since its
`StartMission()` didn't reset marker art the way every other multi-marker mission does (`LeashWalkMissionController`
resets every checkpoint back to `LeashWalkCheckpointWaiting`, `BoneRelayMissionController` calls
`ClearMoundOverrides()`). It didn't - `StartMission()` reset all round state (repairs, breaches, active
gap index) but left each gap's promoted sprite exactly as the previous attempt left it. A replay could
start with a gap still showing last run's green "repaired" or red "breached" art before the player had
touched it in the new attempt. Fixed by resetting every gap back to `CoyotesFenceGapOpen` in
`StartMission()`. Covered by
`CoyotesFence_Replay_ResetsGapArtInsteadOfLeavingStaleBreachedOrRepairedSprites`. Suite green at
`476/476`.

The same sweep found the identical bug in **Eagle Shadow Panic**: `StartMission()` reset round state
but never reset the cover zones' promoted Safe/Spotted sprite, so a replay after an exposure could
open already showing the red Spotted art on every cover zone until the next sweep resolved. Fixed by
calling `SetCoverArt(FinalGameplayArt.EagleShadowCoverSafe)` in `StartMission()`. Added
`EagleShadowPanicMissionController.CoverResourcePathAt()` +
`GameManager.EagleCoverArtResourcePath()` test hooks. Covered by
`EagleShadowPanic_Replay_ResetsCoverArtInsteadOfStartingAlreadySpotted`.

### Scent Search: re-picked dig spot kept stale cold art (2026-07-05)

`ChooseBuriedSpot()` picks a random active dig spot as the new hiding place but never reset its art.
A spot dug wrong keeps a cold-scent sprite as useful "already checked here" feedback - correct, until
that same spot is randomly re-picked as the new hiding place, where it then misleadingly kept reading
as cold/empty even though the bone was now there. Fixed by resetting the newly-chosen spot's art to
`ScentSearchDigUnknown` inside `ChooseBuriedSpot()`. Added `BuriedSpotIndex`/`DigResourcePathAt()`/
`ForceReselectBuriedSpot()` test hooks. Covered by
`ScentSearch_ReselectedBuriedSpot_ClearsStaleColdArtInsteadOfKeepingIt`. Suite green at `481/481`.

Also noteworthy: mid-session another concurrent process (codex, confirmed by the owner) landed
`7f9c81b Wire authored audio cues into Unity arena` directly on `main` in this same working directory.
Pushed immediately alongside this session's own commits with no conflicts - `main` and `origin/main`
stayed in sync throughout, one shared build with both the new audio system and this session's fixes.

## Architecture guardrails

- `GameManager` owns orchestration, mission selection, session flow, and shared-service wiring.
- Controllers own mission-specific setup, state, ticking, input handling, cleanup, outcome, and
  snapshots.
- Mission definitions and controller registration live outside `GameManager`.
- Migration is one mission at a time and test-green after every extraction.
- Completion is defined by ownership and behavior, not an arbitrary line-count target.

Broad roadmaps, backlog items, progression work, and additional mission ideas are deferred until
the second couch-playtest gate passes.

### Roster-wide screen shake wired up (2026-07-05)

`SharedCameraController` has carried a fully-implemented, decaying screen-shake system since it was
built (`AddShake`, consumed every `LateUpdate`), but nothing ever called it - `GameManager` held no
reference to the camera at all. Wired `GameManager.SetSharedCamera()` (called once from
`ArenaBootstrap` after `game.Init()`) and a `RequestShake()` helper into the single shared `EndRound()`
path, so every mission's clear/fail gets a small cosmetic camera kick roster-wide (bigger on fail than
clear, matching the existing rumble intensity split) with zero mission-controller changes needed.
Covered by `GateCrash_ClearAndFail_BothKickTheSharedCameraShake`. Suite green at `482/482`.

Also flagged (not acted on - larger/more consequential than a single-file fix, spawned as separate
review tasks): the whole `Assets/Scripts/Hazards/`, `Assets/Scripts/Interactions/`,
`Assets/Scripts/Minigames/`, `Assets/Scripts/Objectives/` folders are confirmed-orphaned
pre-`IMissionController`-migration scaffolding (zero references from any mission controller,
`GameManager`, scene, or test - confirmed via exhaustive grep and `docs/UNITY-MISSIONS-PORT.md`'s own
"historical, do not resume" banner). Separately, all **10** `Coop*Beat.cs` MonoBehaviour driver classes
(one per co-op puzzle primitive - `CoopHoldReleaseBeat`, `CoopBaitSwitchBeat`, etc.) are also confirmed
dead: every mission controller drives its puzzle directly from its own `Tick()` instead of through the
generic Beat wrapper, so each Beat class is referenced only by its own equally-dead test file (the
underlying 9 `Coop*Puzzle.cs` primitives themselves are alive and actively used directly - only the
`Beat` wrapper layer, plus the one exception `CoopDistractSneakPuzzle.cs`/`Beat.cs` pair which is dead
in both forms, are the cleanup candidates).

Follow-up: gave a **flawless** clear (0 mistakes) a bigger shake than a scrappy one - same
fail-shakes-harder-than-clear asymmetry, since a flawless clear is objectively the best outcome and
deserves more enthusiasm than a clear with a mid-run fumble. Covered by
`GateCrash_FlawlessClear_ShakesHarderThanAScrappyClear`. Suite green at `483/483`.

### Full build + smoke validation, not just editor PlayMode (2026-07-05)

Beyond the headless editor PlayMode suite (483/483 through this point), also ran the actual packaged
verification path: `./unity/build-dev.sh` produced a fresh macOS development build with every fix and
gag from this pass included, and `./unity/smoke-player.sh` confirmed the packaged standalone player
boots and initializes cleanly (no crash-on-startup). This exercises a different failure surface than
editor tests - Safe Mode entry, build-time asset stripping, and player runtime init are all things the
editor test runner doesn't catch. Executable SHA-256:
`af3c32216057aefe2fa65894e3c0301873724e96ab7fdb2b6d53ce2de3f44680`.

Followed up with the full release-candidate gate: `./unity/build-release.sh` produced
`CheddarAndCocoa-Demo.app`, its `Info.plist` matches expected values (name "Cheddar and Cocoa",
identifier `com.kennethchapman.cheddarandcocoa`, version `0.1.0`), and `./unity/smoke-player.sh`
confirmed clean startup on the release configuration too (a different build path than dev - separate
opportunity for stripping/config issues to surface, and it came back clean). Release executable
SHA-256: `1a70a90431c49d49212b569d07cff55e4958a0006e2406b8b7a55ba7720d2adb`. Both the dev and release
build paths are now confirmed green alongside the editor PlayMode suite for this session's cumulative
work.

### Missing AdventureMapScene load smoke test (2026-07-05)

`AdventureMapScene.unity` was the only scene listed in `EditorBuildSettings.asset` that no PlayMode
test ever loaded by name - `AdventureProgressService`/`AdventureMapController` are well covered via
direct instantiation (`AdventureMapControllerPlayModeTests`, `AdventureProgressionPlayModeTests`), but
nothing proved the actual scene bootstrap (`AdventureMapBootstrap` → `AdventureMapHud`) builds without
error when the scene really loads, unlike `ArenaScene` and `ControllerTestScene` which both already
have scene-load tests. Added `AdventureMapScene_LoadsAndBuildsTheMapHudWithoutError`. Suite green at
`484/484`.

### Missing AdventureArenaProgressBridge coverage (2026-07-05)

Cross-referencing every class in the Adventure meta-progression loop against `Assets/Tests/` found
`AdventureArenaProgressBridge.cs` was the one piece with zero direct coverage of any kind - its
neighbors (`AdventureMapController`, `AdventureProgressService`) are both well unit-tested via direct
instantiation, but nothing proved the bridge itself actually starts a queued mission when a real Arena
round is loaded, or records the result (attempts/clears/best score, location unlock state) back to
progress afterward. Added `Bridge_StartsQueuedMissionAndRecordsClearResultToProgress`, which builds the
bridge manually against `AdventureProgressService.CreateInMemoryForTests()` (bypassing the scene-load
hook so the real save file is never touched), queues Car Ride for Front Yard - a real catalog pairing
from `AdventureLocationCatalog`, not an arbitrary one - clears it via `ForceCarLurch()`, and asserts the
bridge recorded the clear and left the always-unlocked Backyard unlocked. Suite green at `485/485`.

### GateCrash snap now kicks an immediate camera shake (2026-07-05)

`SharedCameraController.cs` carried a stale TODO: "route addShake() calls here from hits/lands/
predators." The screen shake was wired only for mission clear/fail (`GameManager.EndRound`) - no single
in-mission impact ever kicked it. Added `MissionContext.RequestShake` (a thin wrapper around
`GameManager.RequestShake`, the same counters `ArenaHud`'s existing camera-shake tests already read) so
mission controllers can request a small, immediate jolt for a single sharp impact without waiting for
the round to end. Wired the first consumer into `GateCrashMissionController.HandleSnaps()`: the gate
slamming shut now jolts the camera at `0.12` magnitude, distinctly smaller than the end-of-round
clear/fail shakes (`0.18`-`0.32`) so a run with several snaps doesn't feel like the whole camera is
constantly rattling. Updated the now-stale TODO comment to describe both call sites. Corrected
`GateCrash_ClearAndFail_BothKickTheSharedCameraShake`'s expected shake count from `2` to `6` (the fail
loop's four snaps each add their own jolt on top of the end-of-round fail shake) and added
`GateCrash_Snap_KicksAnImmediateShakeBeforeTheRoundEnds` to pin the new in-mission behavior. Suite green
at `486/486`.

### Predator-attack "yoinked" hit now kicks a camera shake too (2026-07-05)

The old TODO named "hits/lands/predators" as the intended shake sources. `GateCrash`'s snap covered the
"hits" case; `GameManager.StartPredatorAttack()` - the shared predator-grab sequence used by
`BackyardRescue`, `CoyotesFence`, and `EagleShadowPanic` - is the actual "predators" case, and it
already fires a score penalty, a "YOINKED!" world pop, and a rumble, but never shook the camera. Since
this method already lives inside `GameManager` (unlike the mission-controller-only `MissionContext.
RequestShake` plumbing GateCrash needed), this was a one-line `RequestShake(0.2f)` call alongside the
existing `RequestRumble("predator_penalty", ...)`. Extended the existing predator-attack assertions in
`BackyardMission_Objectives_Hazards_Tug_Clear_AndRestart` (`ArenaGameLoopPlayModeTests.cs`) to pin
`ShakeRequestCount == 1` right after the hit. Suite green at `486/486`.

### Victory Lap / "Backyard legends!" no longer fires on a session of failures (2026-07-05)

`SessionAllMissionsCompleted` - which gates the "Victory Lap" continue-button label and the "Backyard
legends! Cheddar + Cocoa finished every mission." session-summary headline - was driven by
`SessionUniqueMissionsCompleted`, a count of missions *attempted* at least once regardless of outcome.
A player who failed every single mission in the roster would still be told they were legends who
finished everything. The existing test proved it: `CompleteFullMissionSession_OffersExplicitVictoryLap`
force-failed every mission via `ForceGameOver()` and asserted the victory messaging fired anyway.

Added `SessionUniqueMissionsCleared`, tracked from the existing `_sessionClearedMissions` array (already
correctly populated only on a real `Outcome.Clear`, previously used only for the mission-select tile's
CLEARED/RETRY status text). `SessionAllMissionsCompleted` and the summary's "X/Y finished" readout now
key off the cleared count. Left `SessionUniqueMissionsCompleted` alone for its other, correctly-"attempt"
-based uses: the mission-select "X/Y tried" label and `NextUnfinishedMissionIndex`'s routing (picking
which mission "Continue"/"Next" should jump to isn't a victory claim, so attempt-tracking is still right
there). Replaced the misleading test with `FailingEveryMission_DoesNotOfferVictoryLap` (proves failing
the whole roster does NOT trigger the victory messaging, while the attempt-based "Continue" routing
still wraps to the first mission) and added `SessionUniqueMissionsCleared_OnlyCountsActualClearsNotAttempts`
(a mixed fail+real-clear case, using Gate Crash's existing hold+cross clear sequence). Suite green at
`487/487`.

### Per-mission challenge flavor text for the remaining 16 missions (2026-07-05)

`GameManager.MissionChallengeLabelFor` (the "Challenge:" line on the mission-select detail panel) only
had bespoke copy for 6 of 22 missions; the rest fell back to a generic "Challenge: clear clean for
FLAWLESS" line. Added specific text for the other 16, each matched to that mission's actual
fail-condition mechanics from `MissionInstructionCatalog`/each controller's own constants: Snack Heist's
steal limit, Sock Panic's dive count, Weenie Roundup's fumbles, Scent Search's wrong digs, Thunderstorm
Comfort's claps, Mark the Yard's reclaims, Leash Walk's snaps, Car Ride's spills, Gate Crash's snaps,
Table Stealth's spotted state, Squirrel Switcheroo's backfires, Walk Campaign's misreads, Bone Relay's
wasted digs, Great Escape's fumbles, Chaos Machine's misfires, Blanket Catch's rips. Added a completeness
test asserting every mission variant now returns its own line rather than the generic fallback. Suite
green at `487/487`.
