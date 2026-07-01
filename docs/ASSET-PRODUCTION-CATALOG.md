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
- `car_ride_level.png` (`512x512`) - stable car baseline.
- `car_ride_lurch_left.png` (`512x512`) - left-tilt lurch state.
- `car_ride_lurch_right.png` (`512x512`) - right-tilt lurch state.
- `car_ride_spill.png` (`512x512`) - spill/fail warning state.
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

Required/live:
- `pee_break_couch.png` (`512x512`) - couch anchor for the Teenager room; idle fabric wobble/pillow bob.
- `pee_break_teenager.png` (`512x512`) - phone-absorbed Teenager; head/thumb/foot animation states.
- `pee_break_phone_charger.png` (`512x512`) - phone/charger boss object; glow, notification ping, battery drain.
- `pee_break_open_door.png` (`512x512`) - door-open success state; panel swing and sunlight reveal.
- `pee_break_leash.png` (`512x512`) - Cheddar leash presentation prop; strap sway/presentation trail.
- `pee_break_hydrant_relief.png` (`512x512`) - outdoor payoff gag; relief sparkle burst.
- `pee_break_bladder_meter.png` (`512x512`) - in-world urgency meter; warning fill/tick shake.
- `pee_break_misread_tennis_ball.png` (`512x512`) - recoverable wrong-idea misread; bounce/shake.

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
