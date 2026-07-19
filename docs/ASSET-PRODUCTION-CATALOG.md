# Asset Production Catalog

Master tracking document for production art.

## Characters

### Cheddar
Required:
- idle
- run
- bark
- tug
- rescue
- celebrate
- sad
- stunned
- swim
- shake

### Cocoa
Required:
- idle
- run
- bark
- tug
- rescue
- celebrate
- sad
- stunned
- swim
- shake

### Squirrel
Required:
- idle - first-test motion strip live
- run - first-test motion strip live
- fake-out - first-test scared/fake-out strip live
- taunt - first-test scared/fake-out strip live
- stash reveal - first-test run/idle mapping live

### Eagle Shadow
Required:
- shadow pass - first-test sweep strip live
- threat marker - first-test attack/sweep strips live

### Coyote
Required:
- patrol - first-test motion strip live
- threaten - first-test motion strip live
- retreat - first-test motion strip live
- lure - first-test threaten mapping live

## Props

Required:
- weenie
- sock
- rope toy
- tennis ball
- bowl
- chew toy
- squirrel stash
- fence gaps
- dirt pile
- pool floaty
- pool noodle
- beach ball
- dog couch
- squishmallows

### Backyard Rescue P0 generated state pack

Live under `unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props/BackyardRescue/`.
These are couch-test generated sprites, not final hand-authored art, but they replace the most
confusing single-state trap marker reads in Backyard Rescue.

Required/live:
- `backyard_trap_gap_open.png` (`512x512`) - escape gap needs the partner to hold it.
- `backyard_trap_gap_held.png` (`512x512`) - correct partner is holding the gap.
- `backyard_trap_gap_fake_route.png` (`512x512`) - recoverable wrong-pressure/open-gap juke.
- `backyard_weenie_targeted.png` (`512x512`) - squirrel is targeting this weenie.
- `backyard_weenie_dropped.png` (`512x512`) - partner-only trap drop.
- `backyard_weenie_saved.png` (`512x512`) - short success state before replacement.
- `backyard_predator_lane_warning.png` (`512x512`) - nonblocking predator sweep lane accent.

### Snack Heist P0 generated state pack

Live under `unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props/SnackHeist/`.
These sprites clarify which snack is actionable during squirrel pressure and which cue belongs to
bark-guarding. The generic snack plate remains the idle collectible read.

Required/live:
- `snack_heist_plate_targeted.png` (`512x512`) - squirrel-selected snack target; pulse/flash when pressure starts.
- `snack_heist_plate_stashed.png` (`512x512`) - short success state before replacement.
- `snack_heist_plate_stolen.png` (`512x512`) - short miss state before replacement.
- `snack_heist_guard_lane.png` (`512x512`) - temporary bark lane between squirrel and snack target.

### Sock Panic P0 generated state pack

Live under `unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props/SockPanic/`.
These sprites replace the unclear basket block states with readable co-op verbs: tip, hold open,
partner dive, fumble, decoy, and save.

Required/live:
- `sock_panic_basket_closed.png` (`512x512`) - idle/tip prompt; can idle bob or arrow pulse.
- `sock_panic_basket_open.png` (`512x512`) - held-open partner-dive window; should wobble while timed.
- `sock_panic_basket_fumble.png` (`512x512`) - one-second failed recovery/readability burst.
- `sock_panic_sock_exposed.png` (`512x512`) - partner-only exposed sock target.
- `sock_panic_sock_decoy.png` (`512x512`) - same-dog decoy/fumble read before hiding.
- `sock_panic_sock_saved.png` (`512x512`) - short success state before recovery.

### Threat / Conspiracy P0 generated state pack

Live under:
- `unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props/SquirrelConspiracy/`
- `unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props/EagleShadow/`
- `unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props/CoyotesFence/`

These sprites replace generic escape-gap, bush, and weak-spot placeholders with mission-specific
state reads for the next three roster levels.

Required/live:
- `squirrel_conspiracy_cutoff_open.png` (`512x512`) - active cutoff route; should pulse while waiting.
- `squirrel_conspiracy_cutoff_held.png` (`512x512`) - partner is holding the cutoff zone.
- `squirrel_conspiracy_cutoff_fakeout.png` (`512x512`) - short fake-out/wrong bark state.
- `squirrel_conspiracy_stash_revealed.png` (`512x512`) - stash exposed after route control.
- `squirrel_conspiracy_stash_cracked.png` (`512x512`) - stash found/conspiracy cracked.
- `eagle_shadow_cover_safe.png` (`512x512`) - safe hide cover; should breathe/pulse during sweep.
- `eagle_shadow_cover_spotted.png` (`512x512`) - exposure/open-ground warning burst.
- `eagle_shadow_talon_grip_closed.png` (`512x512`) - Cheddar wiggle prompt.
- `eagle_shadow_talon_grip_open.png` (`512x512`) - Cocoa pull window.
- `eagle_shadow_talon_grip_freed.png` (`512x512`) - partner rescue success.
- `coyotes_fence_gap_open.png` (`512x512`) - active fence weak spot.
- `coyotes_fence_gap_pinned.png` (`512x512`) - coyote bark-pinned; partner can fill dirt.
- `coyotes_fence_gap_repaired.png` (`512x512`) - filled/secured gap.
- `coyotes_fence_gap_breached.png` (`512x512`) - breach warning/failure state.
- `coyotes_fence_fake_snack.png` (`512x512`) - lure state for fake snack bait.

### Adventure P0 generated state pack

Live under:
- `unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props/WeenieRoundup/`
- `unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props/ScentSearch/`
- `unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props/Thunderstorm/`
- `unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props/MarkTheYard/`

These sprites replace generic weenie, bowl, mound, territory-zone, storm, and squirrel markers
with readable mission-state art for the next four roster levels.

Required/live:
- `weenie_roundup_loose.png` (`512x512`) - loose yard weenie; should wiggle when near a dog.
- `weenie_roundup_carried.png` (`512x512`) - cargo above the carrying dog.
- `weenie_roundup_dropped.png` (`512x512`) - fumbled weenie recovery target.
- `weenie_roundup_bowl_empty.png` (`512x512`) - home bowl before delivery.
- `weenie_roundup_bowl_progress.png` (`512x512`) - bowl with delivered weenies.
- `weenie_roundup_bowl_full.png` (`512x512`) - clear-state bowl.
- `scent_search_dig_unknown.png` (`512x512`) - untested sniff patch.
- `scent_search_scent_hot.png` (`512x512`) - hot-scent patch after close sniff.
- `scent_search_scent_cold.png` (`512x512`) - cold/wrong dig patch.
- `scent_search_bone_found.png` (`512x512`) - found bone success flash before the patch hides.
- `thunderstorm_cloud_waiting.png` (`512x512`) - storm waiting cue over the huddle zone.
- `thunderstorm_thunderclap.png` (`512x512`) - clap impact state.
- `thunderstorm_comfort_huddle.png` (`512x512`) - dogs are close enough to comfort.
- `thunderstorm_storm_cleared.png` (`512x512`) - survived-storm clear state.
- `mark_yard_zone_unclaimed.png` (`512x512`) - neutral claimable territory.
- `mark_yard_zone_claimed.png` (`512x512`) - dog-claimed territory.
- `mark_yard_zone_stolen.png` (`512x512`) - squirrel re-marked territory.
- `mark_yard_squirrel_watch.png` (`512x512`) - squirrel watching/prowling state.
- `mark_yard_squirrel_steal.png` (`512x512`) - squirrel actively stealing a mark.

### Home Trip P0 generated state pack

Live under:
- `unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props/LeashWalk/`
- `unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props/CarRide/`
- `unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props/GateCrash/`
- `unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props/TableStealth/`

These sprites replace generic checkpoint, vehicle, gate, toy, human, and steak placeholders with
state-specific readable props for the next four roster levels.

Required/live:
- `leash_walk_checkpoint_waiting.png` (`512x512`) - active walk-together checkpoint.
- `leash_walk_checkpoint_reached.png` (`512x512`) - checkpoint success flash before hiding.
- `leash_walk_snap_warning.png` (`512x512`) - taut-leash warning on the current checkpoint.
- `backseat_cabin_shell.png` (`1280x768`) - authored full backseat interior framing plate with
  leather doors/armrests/handles, front headrests, an aligned transparent windshield opening, and
  an opaque outer perimeter that prevents backyard bleed.
- `backseat_bench.png` (`1280x384`) - textured, deliberately low-contrast leather gameplay lane.
- `backseat_windshield_scenery.png` (`1280x256`) - fully opaque passing-neighborhood strip with
  byte-identical first/last columns for the one-plate scroll wrap and SpriteMask contract.
- `car_dashboard_driver.png` (`512x384`) - authored driver, wheel, and mirror-eye actor; telegraphs
  turns and brakes while keeping badge space clear.
- `seat_cooler.png` (`512x512`) - blue/ivory chunky sliding hazard with reinforced danger corners.
- `seat_toy_bin.png` (`512x512`) - red overstuffed sliding hazard with dog-toy personality.
  All six plus the refreshed `MissionTiles/carride.png` derive from tracked source masters through
  `tools/art/generate_car_backseat_pack.py`. Source notes and the required before/after contact sheet
  live in `Assets/Art/ReferenceOnly/GeneratedCarBackseat/`.
  (The old `car_ride_level/lurch_left/lurch_right/spill` balance sprites and the LevelAreas
  `car_interior_cabin`/`car_balance_lane` plates are retired with the 2026-07-14 backseat redesign.)
- `gate_crash_gate_closed.png` (`512x512`) - closed gate/hold prompt.
- `gate_crash_gate_held.png` (`512x512`) - Cocoa is holding the gate.
- `gate_crash_gate_snap.png` (`512x512`) - snap warning after release during squeeze.
- `gate_crash_toy_waiting.png` (`512x512`) - Cheddar squeeze target.
- `gate_crash_toy_claimed.png` (`512x512`) - squeeze-through success state.
- `table_stealth_human_watching.png` (`512x512`) - human watching the table.
- `table_stealth_human_distracted.png` (`512x512`) - Cocoa distraction opens the steak lane.
- `table_stealth_human_spotted.png` (`512x512`) - human spots Cheddar.
- `table_stealth_human_caught.png` (`512x512`) - too many exposures/failure state.
- `table_stealth_steak_available.png` (`512x512`) - steak target before distraction.
- `table_stealth_steak_sneak_progress.png` (`512x512`) - steak lane is open/progressing.
- `table_stealth_steak_gone.png` (`512x512`) - steak stolen success state.

### Coop Tricks P0 generated state pack

Live under:
- `unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props/SquirrelSwitcheroo/`
- `unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props/WalkCampaign/`
- `unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props/BoneRelay/`

These sprites replace generic decoy, stash, human, leash, scent-post, and mound placeholders with
state-specific readable props for the next three co-op puzzle levels.

Required/live:
- `switcheroo_decoy_guarded.png` (`512x512`) - squirrel is guarding; Cheddar should feint.
- `switcheroo_decoy_chased.png` (`512x512`) - squirrel is committed to the decoy.
- `switcheroo_decoy_backfire.png` (`512x512`) - over-bait/backfire state.
- `switcheroo_stash_guarded.png` (`512x512`) - stash is still unsafe.
- `switcheroo_stash_open.png` (`512x512`) - Cocoa raid window.
- `switcheroo_stash_raided.png` (`512x512`) - successful stash raid.
- `walk_campaign_human_confused.png` (`512x512`) - human has no clear message yet.
- `walk_campaign_human_getting_it.png` (`512x512`) - exact dog combo is being held.
- `walk_campaign_human_misread.png` (`512x512`) - incomplete/wrong signal misread.
- `walk_campaign_human_walkies.png` (`512x512`) - walk successfully earned.
- `walk_campaign_human_gave_up.png` (`512x512`) - too many mixed signals/failure state.
- `walk_campaign_leash_waiting.png` (`512x512`) - leash waiting for Cheddar's presentation.
- `walk_campaign_leash_presented.png` (`512x512`) - Cheddar is presenting the leash.
- `walk_campaign_leash_grabbed.png` (`512x512`) - human accepted the leash.
- `bone_relay_scent_post_idle.png` (`512x512`) - Cocoa has not called the scent yet.
- `bone_relay_scent_post_called.png` (`512x512`) - Cocoa has revealed the correct mound.
- `bone_relay_mound_unknown.png` (`512x512`) - look-alike mound before scent call.
- `bone_relay_mound_called.png` (`512x512`) - correct mound after Cocoa's call.
- `bone_relay_mound_wrong.png` (`512x512`) - wrong/blind dig feedback.
- `bone_relay_mound_found.png` (`512x512`) - found bone success state.

### Chaos Machine generated prop pack

Live under `unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props/ChaosMachine/`.
These sprites replace samey generic junction markers with readable Rube-Goldberg stations while the
controller keeps the lever, assist windows, labels, and misfire timing.

Required/live:
- `chaos_lever_ready.png` (`512x512`) - idle pull lever with warning tape and dog-proof handle; can wobble or blink when ready.
- `chaos_lever_running.png` (`512x512`) - pulled/running lever with green indicator and motion lines; swaps in while cascade is active.
- `chaos_junction_towel_drop.png` (`512x512`) - Cocoa-owned towel-drop junction; gears can spin and towel can drop.
- `chaos_junction_basket_tip.png` (`512x512`) - Cheddar-owned basket-tip junction; basket can rock/tip.
- `chaos_junction_toy_launch.png` (`512x512`) - Cocoa-owned toy-launch junction; toy launch streaks can pulse.

### Operation Pee Break generated prop pack

Live under `unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props/PeeBreak/`.
These sprites make the deep-slice room read as a couch-bound teenager situation rather than a block
diagram. Fine-grained beat, label, room-detail, misread, and payoff coverage lives in
`PeeBreakPlayModeTests`.

Required full-room states:

- `pee_break_living_room_plate.png` (`1672x941`, 16:9) - painterly warm living-room foundation for
  the full couch-play frame. It establishes walls, floor, window light, rug, room depth, and the
  closed front door; there is no active separate closed-door sprite. Controller-owned actors and
  state props stay layered above it. When loaded, the old wall/wood-floor/baseboard/window/
  side-table/door-slab renderers are hidden fallback geometry rather than visible colored blocks;
  the full-arena foundation stays behind the plate only to prevent the backyard from leaking into
  extreme camera framing.
- `pee_break_living_room_success_plate.png` (`1672x941`, 16:9) - matched success-state room plate
  that replaces the base plate after the final united bark. It embeds the open architectural doorway
  and standing Teenager so the payoff cannot read as disconnected floating overlays.

Controller overlays and conditional fallbacks:

- `pee_break_teenager.png` (`512x512`) - active pre-success, phone-absorbed seated Teenager/beanbag;
  hidden when the success plate supplies the standing pose.
- `pee_break_phone_charger.png` (`512x512`) - progressive Beat-3-only phone/charger focus; it enlarges
  for the charger gambit and disappears from the success payoff.
- `pee_break_couch.png` (`512x512`) - fallback-only furniture if the seated Teenager art is missing;
  normally hidden because that character sprite already includes its seat.
- `pee_break_open_door.png` (`512x512`) - fallback-only open-door state if the full success plate is
  unavailable; normally hidden when the authored success room loads.
- `pee_break_leash.png` (`512x512`) - Cheddar leash presentation prop; strap sway/presentation trail.
- `pee_break_hydrant_relief.png` (`512x512`) - outdoor payoff gag; relief sparkle burst.
- `pee_break_bladder_meter.png` (`512x512`) - in-world urgency meter; warning fill/tick shake.
- `pee_break_misread_tennis_ball.png` (`512x512`) - recoverable wrong-idea misread; bounce/shake.

- Provenance: the paired base and success room plates were generated as raster production candidates
  with the built-in image-generation workflow, then selected and integrated as matched closed/open
  room states against the existing game art.

### Escape / Catch / Kitchen P0 generated state pack

Live under:
- `unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props/GreatEscape/`
- `unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props/BlanketCatch/`
- `unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props/KitchenFrenzy/`

These sprites replace the remaining mission prop color-block reads for Great Escape, Blanket Catch,
and Kitchen Falling Food Frenzy. They are generated couch-test art and should be replaced one-for-one
by final hand-authored sprites only after the playable timing is stable.

Required/live:
- `great_escape_station_waiting.png` (`512x512`) - inactive contraption station; idle gear wobble would make it read alive.
- `great_escape_station_cheddar_active.png` (`512x512`) - Cheddar-owned active step; paw beacon can pulse.
- `great_escape_station_cocoa_active.png` (`512x512`) - Cocoa-owned active step; paw beacon can pulse.
- `great_escape_station_completed.png` (`512x512`) - completed station; green check can pop once.
- `great_escape_station_fumble.png` (`512x512`) - wrong dog/order fumble; red X can shake.
- `great_escape_station_settle.png` (`512x512`) - dawdle regression; blue rewind ring can rotate.
- `blanket_catch_slack.png` (`512x512`) - dogs are too close; sagging blanket should bob downward.
- `blanket_catch_taut.png` (`512x512`) - catch-ready span; blanket edge can shimmer.
- `blanket_catch_ripping.png` (`512x512`) - overstretched/ripping; tear should shake.
- `blanket_catch_caught.png` (`512x512`) - snack caught on blanket; success pop before next snack.
- `blanket_snack_falling.png` (`512x512`) - incoming snack; falling streaks can scroll.
- `blanket_snack_caught.png` (`512x512`) - snack landed safely; short catch flash.
- `blanket_snack_splat.png` (`512x512`) - missed snack; splat spreads briefly.
- `kitchen_counter_ready.png` (`512x512`) - Cheddar counter route ready for bark.
- `kitchen_counter_barked.png` (`512x512`) - bark-knock telegraph; sound burst should expand.
- `kitchen_safe_bowl_empty.png` (`512x512`) - Cocoa safe bowl target.
- `kitchen_safe_bowl_catch.png` (`512x512`) - successful bowl catch; check mark can pop.
- `kitchen_food_good_falling.png` (`512x512`) - gold food to catch in the bowl.
- `kitchen_food_bad_falling.png` (`512x512`) - purple onion/bad food to dodge.
- `kitchen_food_splat.png` (`512x512`) - floor splat for misses/dodges.

## UI

### Mission-selection production replacements (2026-07-13)

Live under `unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/UI/MissionTiles/`.

- `babybirdbedlam.png` (`1254x1254`) - painterly Baby Bird Bedlam portrait matching the dark-green/
  gold storybook framing of the adjacent Adventure Library cards. The illustration identifies
  Cheddar's chick-feast role and Cocoa's parent-bird defense role, keeps its lower name strip free of
  baked text, and lets TMP own the accessible mission title. It replaces the former flat placeholder
  at the same runtime path.
- Provenance: this tile was generated as a raster production candidate with the built-in
  image-generation workflow, then selected and integrated against the existing game art. The
  retired deterministic Baby Bird generator writes only
  `Assets/Art/ReferenceOnly/GeneratedMissionTiles/babybirdbedlam_placeholder.png`; it cannot overwrite
  the runtime portrait.

These assets remove high-visibility placeholders for the next couch test, but they do not close the
remaining final-animation, character-state, or authored-UI work.

Required:
- mission card
- replay button
- next mission button
- mission select button
- score panel
- objective banner
- star icon
- rank badge
- summary panel

## VFX

Required:
- bark burst
- bark ring
- pickup sparkle
- score gain
- score loss
- warning alert
- success burst
- fail puff

## Audio

Required:
- bark variations
- happy bark
- warning bark
- squirrel taunt
- success sting
- fail sting
- score pop
- pickup

## Production Rule

No final art should be created until the mechanic using it is playable and tested.

## V3.1 Style-Contract Audit (2026-07-19)

Grades the roster against the five-point checklist added to `ART-DIRECTION.md`'s new "Style
Contract" section: (1) outline weight, (2) palette family, (3) shading style, (4) silhouette-first,
(5) no baked text. **No regeneration in this pass** — this is the audit + ranked fix list that gates
V3.2 (kill remaining square/debug first reads) and V3.3 (mission tile consistency) scope.

### Evidence and a methodology caveat

A fresh dev build was cut at `9f478ff` (executable SHA-256
`8b9dbfb16d5f923d5f59157c28be5d40c0f81da129f4de30379567d004ad65dd`), smoke-tested clean, and run
through `--arena-art-review=`. It completed cleanly in ~25s and wrote 69/69 frames — better than the
A2.4 session, which hung — but every frame sampled at zero color variance: the same "no GPU/display in
this sandbox" gap flagged in P0.1/G1.2/G1.3/G1.4/A2.4 reproduced again today. Those frames carry no
visual information and cannot be graded.

**Graded from `unity/builds/art-review-guidance-current/` instead** — a 2026-07-16 capture confirmed
by direct pixel-variance sampling to contain real rendered content (hundreds to low-thousands of
unique sampled colors per frame), and already matching the current 23-mission/69-frame roster. Two
assets shipped after that capture (A2.4's authored Sniff strips, G1.3's role-turn beacon) were graded
directly from their source PNGs instead, since no frame of them exists in any completed capture. Where
a mission's own capture frame turned out not to show its real objective prop at all (see finding #3
below), that mission was also re-graded directly from its registered source PNG rather than left
ungraded. No PlayMode run was needed — this task changed no code, matching the A2.1 precedent.

### Ranked fix list (worst first)

1. **Off-style photoreal renders break "same two dogs everywhere" mid-mission.** Cheddar and Cocoa's
   idle/run/bark/tug art is flat, cel-shaded, thick-outlined cartoon. Two things render them
   completely differently: the new Sniff pose (`Characters/Dogs/{Cheddar,Cocoa}/Motion/*_sniff_*.png`,
   shipped in A2.4) is a soft-shaded, unoutlined, painterly-realistic illustration — same characters,
   incompatible style, and it can appear mid-Scent-Search right next to their cartoon idle frame. All
   of Operation Pee Break (room plates, dog renders, the "OPERATION: PEE" title card) is the same
   violation at mission scale. Pee Break's treatment is a known, deliberate deep-slice production
   choice (`docs/DEEP-SLICE-OPERATION-PEE-BREAK.md`), not an accident — but a contract that exists to
   replace vibes with a checklist should still name unreconciled drift even when the drift was
   intentional. Highest severity: this is the one rule (dimension 3, and implicitly dimension 1 since
   the photoreal art has no outline at all) that most directly undercuts the game's central visual
   promise. **Recommend:** decide explicitly whether Sniff and Pee Break are permanent, documented
   style exceptions (like the painterly ground-plate exception already is) or a fix target for V3.2 —
   right now the contract has no exception carved for them, so they read as failures, not choices.
2. **Baked text in shipped runtime sprites**, contradicting the shared TextMesh/TMP label path every
   other mission uses correctly (compare "HOME BOWL 1/5" in Weenie Roundup, "BLADDER URGENCY" in Pee
   Break — both real dynamic labels, not violations). Confirmed by direct pixel inspection of the
   registered PNGs:
   - `Props/GateCrash/gate_crash_gate_held.png` — "GATE HELD" baked in.
   - `Props/TableStealth/table_stealth_human_distracted.png` — "DISTRACTED BY COCOA" baked in.
   - `Props/SquirrelSwitcheroo/switcheroo_stash_open.png` — "RAID WINDOW" baked in.
   - `Props/WalkCampaign/walk_campaign_human_walkies.png` — "WALKIES!" baked in.
   - `Props/BoneRelay/bone_relay_mound_found.png` — "BONE FOUND" baked in.
   - `tools/art/generate_environment_prop_pack.py:47` (`house_patio()`) bakes "GO" into the doormat
     prop; line 107 (`leash_route()`) bakes checkpoint numerals 1–5 into the waypoint markers — this
     one is confirmed actually rendering in play, visible in the captured
     `11-leash-walk-payoff.ppm` frame, not just sitting unused in a reference board.
   The four generator scripts with a `draw.text` call that turned out to be contact-sheet-caption-only
   (harmless, never shipped) are `generate_escape_catch_kitchen_p0_pack.py`,
   `generate_mission_prop_pack.py`, and `normalize_pee_break_prop_pack.py` — checked and cleared so
   the next pass doesn't re-flag them. The five per-state files above came from a Home Trip/Coop
   Tricks-era generator script no longer present in `tools/art/` (one-shot, already run, since
   removed) — graded from the shipped PNG directly since the source script is gone. **Recommend:** a
   full sweep of every `Props/*/*.png` for baked text before V3.2 closes; this pass sampled, it did not
   exhaustively check all ~150 registered files.
3. **A generic/fallback-looking prop marker appears instead of the real registered art** in at least 5
   missions' captured frames — Bone Relay, Chaos Machine, Blanket Catch, Kitchen Food Frenzy, and Baby
   Bird Bedlam all show an identical tan rounded-plaque + oval + blue-triangle shape that matches
   *none* of those missions' actual cataloged art (directly compared against
   `bone_relay_mound_found.png`, `blanket_catch_taut.png`, and `chaos_lever_running.png` from source —
   all completely different). Traced a likely mechanism in `ArenaArtReviewCapture.cs`: only 3 of 23
   missions (Weenie Roundup, Leash Walk, Chaos Machine) call `StageDogsAtCurrentObjective()` before
   capturing; for the other 18, camera and dog placement fall back to `TryGetObjectiveTarget` or
   `ArenaBounds.center`, and when that lands away from the real prop, whatever's nearest gets
   captured instead. This reads as a capture-tooling gap rather than a runtime art regression — the
   codebase does have a documented generated-fallback path (`MissionPropArt.DimGeneratedFallback`/
   `FindFallbackRenderer`) that fires when named art fails to resolve, so a live-display session should
   confirm which one this actually is before treating it as closed either way. **Recommend:** extend
   `DriveMainInteraction`/`DrivePayoff` with `StageDogsAtCurrentObjective()` calls for the missions
   that lack one (cheap, mirrors the 3 that already do it) — this alone would make every future
   art-review capture dramatically more useful for grading, independent of whether today's specific
   shape is a real bug.
4. **Silhouette-first failures driven by the same gap as #3.** With their real objective prop
   effectively absent from the frame, Gate Crash, Table Stealth, Squirrel Switcheroo, and Walk
   Campaign's captures read as near-empty lawn with only a stray warning-triangle or flower icon —
   fails the cover/hide test outright, since there is nothing mission-specific to name. (Graded from
   source PNGs instead per the methodology note; the shipped art itself is fine on this dimension —
   see #2's finding that it's fine on style but fails on baked text instead.)
5. **Roster-wide shading-language tension between painterly ground plates and flat cel-shaded
   actors.** The Photo-Inspired Backyard Reskin Layer's soft-textured grass/stone/wood plates
   (Backyard Rescue, Squirrel Conspiracy, Coyotes Fence, Car Ride's leather interior) sit directly
   under thick-outlined flat-cartoon dogs and props every time. This is the documented, intentional
   exception in the new contract's dimension 3 — flagged here not as a violation but because it's the
   single most *pervasive* visual tension in the roster by frame count (visible to some degree in 18+
   of 23 missions), and worth a deliberate look at whether the exception's boundary (plates yes,
   characters/props no) is actually reading as intended at couch distance rather than as two different
   games glued together. Car Ride and Kitchen Food Frenzy integrate it best; Backyard Rescue's painted
   rocks/pavers against flat dog sprites is the starkest example.
6. **Duplicate score-pop icons stack instead of distributing.** Mark the Yard and Great Escape's
   payoff frames both show 4–5 identical green checkmark icons stacked in a vertical column at one
   point rather than at each claimed zone/station. Very likely an artifact of the capture script
   calling `ForceClaimZone`/`ForceEscapeStep` in a tight loop with no per-call settle frame — not
   reachable at normal human input speed — but worth a quick check for a missing max-stack guard on
   the checkmark pop effect while someone is in that code for #3.
7. **Minor camera-composition gaps**, noted for awareness rather than scored against the style
   contract itself: Chaos Machine's main frame clips its active lever/junction prop at the bottom
   frame edge; Coyotes Fence and Weenie Roundup show a flat dark-green fill covering roughly half the
   frame, likely the undecorated `ArenaBounds` fallback color showing past a photo-reskin plate's
   edge rather than a graded-art issue.

### Per-mission grading summary

Dimension numbers match the `ART-DIRECTION.md` checklist (1 outline, 2 palette, 3 shading, 4
silhouette-first, 5 no baked text). "Capture" = graded from the 2026-07-16 frames; "Source" = graded
directly from the registered PNG because the capture didn't show the real prop (see finding #3).

| # | Mission | Graded from | Worst dimension | Severity | Fix-list ref |
|---|---|---|---|---|---|
| 1 | Backyard Rescue | Capture | 3 (painterly plate vs flat actors) | Med | #5 |
| 2 | Snack Heist | Capture | 3 | Low-Med | #5 |
| 3 | Sock Panic | Capture | 4 (very sparse, tiny icons) | Med | #4-adjacent |
| 4 | Squirrel Conspiracy | Capture | 3 + minor warning-icon clutter | Med | #5 |
| 5 | Eagle Shadow Panic | Capture | 3 | Low | #5 |
| 6 | Coyotes Fence | Capture | 4 (large flat void, see #7) | Med-High | #7 |
| 7 | Weenie Roundup | Capture (staged) | 4 (void in payoff, see #7) | Med | #7 |
| 8 | Scent Search | Capture + source (Sniff pose) | 3 (Sniff art, see #1) | High | #1 |
| 9 | Thunderstorm Comfort | Capture | 4 (sparse) | Med | #4-adjacent |
| 10 | Mark the Yard | Capture | 4 (sparse) + stacked icons | Med | #6 |
| 11 | Leash Walk | Capture | 5 — baked "GO" + checkpoint numerals confirmed rendering | High | #2 |
| 12 | Car Ride | Capture | 3, best-integrated in roster | Low | — |
| 13 | Gate Crash | Source (capture blank) | 5 — baked "GATE HELD" | High | #2, #3 |
| 14 | Table Stealth | Source (capture blank) | 5 — baked "DISTRACTED BY COCOA" | High | #2, #3 |
| 15 | Squirrel Switcheroo | Source (capture blank) | 5 — baked "RAID WINDOW" | High | #2, #3 |
| 16 | Walk Campaign | Source (capture blank) | 5 — baked "WALKIES!" | High | #2, #3 |
| 17 | Bone Relay | Source (capture showed #3's fallback shape) | 5 — baked "BONE FOUND" | High | #2, #3 |
| 18 | Great Escape | Capture (partial) + source | 4 (capture gap) + stacked icons; source art itself clean | Med-High | #3, #6 |
| 19 | Chaos Machine | Capture (staged) | 4 (fallback shape in payoff) + minor clipping | Med | #3, #7 |
| 20 | Blanket Catch | Source (capture showed #3's fallback shape) | 4 (capture gap only); source art clean, no baked text | Med | #3 |
| 21 | Kitchen Food Frenzy | Capture | 4 (fallback shape briefly visible) | Low-Med | #3 |
| 22 | Operation Pee Break | Capture | 3 — severe, see #1 | Highest | #1 |
| 23 | Baby Bird Bedlam | Capture (partial, #3's fallback shape present) | 4 (capture gap) | Med | #3 |

**Not found:** no violations of dimension 1 (outline weight) or dimension 2 (palette family) at the
severity of the items above — outline widths sampled from `tools/art/*.py` generator scripts cluster
4–12px at a 512px canvas as the new contract specifies, and every mission stays inside the documented
warm/earth-tone-plus-teal-accent family. Both checks are worth re-running once V3.2 regenerates the
items above, since new art can introduce what today's roster doesn't have.
