# Arena Playable — Mission Variety Spike

`unity/CheddarAndCocoa/Assets/Scenes/ArenaScene.unity` is now a small co-op vertical slice instead of a flat treat loop. The scene still builds itself from `ArenaBootstrap`, but the arena can run 20 mission variants through one lightweight mission definition path. A cold start opens a generated in-scene mission select; the newest arrow-select mission is **Kitchen Falling Food Frenzy**.

For current global character art direction, read `docs/ART-DIRECTION.md`. Backyard Mission is the
playable proof of that direction, not the only place the direction applies. For future external
sprite/audio collection before Unity import, use `docs/ASSET-CATALOG.md`.

## Draft and ArenaFinal art pass

The first readable imported-art pass now lives under
`unity/CheddarAndCocoa/Assets/Art/Resources/ArenaDraft/` with Unity-generated `.meta` files. Only
the 11 runtime-loaded badges remain in `Resources`; unused pose/expression/reference sheets live in
`Assets/Art/ReferenceOnly/ArenaDraft/` to keep them out of release builds:

- `Characters/Dogs/`: Cheddar and Cocoa portrait, pose, and expression draft sheets.
- `Characters/Squirrel/`: squirrel reference, character, pose, and expression draft sheets.
- `Characters/Eagle/` and `Characters/Coyote/`: predator reference, pose, and expression draft sheets.
- `Characters/Bunny/`: bunny reference, pose, and expression draft sheets.
- `Props/Backyard/`: backyard prop sheets.
- `UI/`: draft UI kit sheet.
- `VFX/`: draft VFX sheet.

Individual transparent gameplay sprites now live under
`Assets/Art/Resources/ArenaFinal/`; `docs/ART-INTEGRATION-SLICE.md` records their source sheets,
runtime mappings, and review limits. The earlier ArenaDraft imports remain safe reference/fallback
accents:

- Cheddar and Cocoa each carry an imported portrait badge behind the generated long-low body,
  collar, feet, snout, ears, and pose-state pieces.
- Squirrel, rope, and predator actors keep generated role silhouettes while loading squirrel,
  backyard-prop, eagle, and coyote draft badges as background reference accents.
- A noninteractive bunny cameo appears at the yard edge as a placeholder/background prop.
- Bark feedback uses the transparent final ring and bark burst while retaining generated text.
- Pickup, success, warning, rescue, and failure events use distinct short-lived ArenaFinal effects.
- Final dog poses, squirrel/eagle/coyote states, rope, weenies, bushes, rocks, and selected ground
  dressing are live. Generated gameplay objects and colliders remain authoritative.
- Cheddar and Cocoa now play distinct four-frame idle, run, and bark strips. Run has complete eight-way
  coverage using authored east/southeast/south/northeast/north and mirrored west-side travel. Their cadence reinforces
  chaos-pup versus spot-queen identity. Idle, run, and bark preserve all eight facing directions, and tug uses distinct three-frame brace/pull/recover loops;
  stunned, rescued, proud, and sad now use distinct two-frame personality loops. Unsupported actions retain the safe single-pose fallback.
  Weenie Roundup carry now persists visually from pickup through delivery/drop while the separate carried-weenie marker remains authoritative.
- Mission select/end-card HUD keeps IMGUI text controls and draws a subtle imported UI-kit accent.

Still placeholder or deliberately deferred:

- Dog sprites and gameplay effects are extracted draft art rather than final animation-ready assets.
- Snack/sock mission collectibles, range rings, objective arrows, buildings, pond/patio, and most
  large environment districts still rely on generated geometry/text for the primary gameplay read.
- The bunny is decorative only and has no mission logic.
- Menu/UI art remains deferred; the UI kit is still a subtle reference accent.

Final polish still needed:

- Replace duplicate rescued/proud and rope tug/complete source poses when distinct art exists.
- Author animation-ready dog/threat sprites and replace remaining generated environment districts.
- Run a two-player television readability pass; the automated 1920x1080 local/full-yard/action
  capture gate verifies composition but cannot judge player attention.

### Threat/label readability pass (2026-06-21)

Direct fixes for first-playtest confusion ("can't tell what to do; squirrel/shadow never seen;
spinning props are unreadable; the dog's name text covers the dog"):

- **Props no longer spin.** `MissionActorFeedback` previously rotated actors continuously (squirrel
  `80`deg/s, rope `45`deg/s), so they read as unidentifiable rotating blobs. The authored spin intent
  is now a small, bounded life-wobble (≤`7`deg) around the resting pose; silhouettes stay recognizable
  and labels stay upright.
- **The waiting squirrel is on-screen.** Backyard Rescue parked the idle squirrel in the far `120x68`
  yard corner, outside the close camera, so players never saw the threat the HUD/arrows referenced.
  It now perches at a visible spot (`11, 7`) just inside the close view, still out of instant bark
  range of the `±10,0` dog spawns.
- **The eagle shadow sweeps overhead.** Eagle Shadow Panic swept the shadow along the far top fence
  (`y≈32`), far above the dogs' play band, so the "shadow" was never seen. It now sweeps at
  `EagleSweepHeight` (`y=0.5`) across the dogs and cover zones; the snatch/rescue beat also resolves
  inside the play band (`0, 6`) instead of off-screen. Exposure remains x-column based, so the hide
  mechanic is unchanged.
- **The dog identity label clears the dog.** `DogLabel` moved from `y=0.95` (on the body) to `y=1.5`
  and shrank slightly so the name/pose text floats above the character instead of obscuring it.

Covered by `ArenaGameLoopPlayModeTests.ThreatActors_AreOnScreenAndReadable_NotOffscreenSpinningBlobs`.

## Cold-start flow

The expanded yard stages both dogs within a short run of each mission's first meaningful action
instead of resetting every mission at the same generic center point. A deterministic PlayMode contract
keeps both dogs within 12 world units of that entry target. Dog-mounted objective arrows cover all
mission-specific verbs (hide, herd, repair, carry, sniff, mark, comfort, and leash checkpoints), show
distance, and scale up during strategic camera zoom so navigation remains readable across the 120x68
yard.

When an active objective is more than 28 world units away, that dog's objective arrow enables a
modest `1.55x` trail-travel speed assist. The identity label changes to `TRAIL READY`/`TRAIL SPRINT`,
the shared per-dog guidance line adds `[TRAIL SPRINT]`, and normal close-control speed returns inside
20 units. The hysteresis prevents flicker and keeps bark, tug, pickup, dig, hide, and leash
interactions on their existing precision tuning.

The shared camera treats its authored maximum zoom as the 16:9 target, then derives a safe wider
ceiling for narrower and portrait windows. This keeps both dogs framed at maximum separation across
32:9, 16:9, 4:3, and 9:16 aspect contracts. Cheddar/Cocoa identity labels scale with strategic zoom
so their names and current emotional poses remain legible when the yard view pulls back.

All blocking HUD panels now share an eight-pixel safe-area contract. Mission select, pause, end-card,
session summary, and the playtest overlay shrink to the current viewport instead of clipping fixed
640-900 pixel layouts on narrow desktop windows.

End-card rivalry copy now reflects which dog made more objective plays: Cheddar earns the `Chaos
Crown`, Cocoa becomes `Queen of the Yard`, and equal contributions produce a quiet nose-to-nose draw.
Flawless copy uses the same contribution result, reinforcing their distinct chaos/steady identities
without adding mid-mission UI noise.

Short-lived world pops for scores, rescues, tug results, safe hides, misses, and objective beats now
scale with strategic camera zoom (capped at `3.2x`). Split players can still read what just happened
without changing collision sizes or the close-camera presentation.

Direct mission switches now clear long-travel speed, proud/sad pose overrides, score/outcome state,
inactive actor visibility, and prior mechanic counters before the new intro is exposed. A sequential
PlayMode regression covers failure-to-replay and leash-to-car transitions without scene reloads.

The gameplay HUD now mirrors world-arrow guidance in one compact team route line, for example
`Cheddar: HERD SQUIRREL 18m • Cocoa: HERD SQUIRREL 24m`. It updates from the same contextual targets
and reports `ON TARGET` inside interaction range, so split players can coordinate without hunting for
small world text.

1. Open `unity/CheddarAndCocoa` in Unity 6 LTS, open `Assets/Scenes/ArenaScene.unity`, and press Play. `ArenaScene` is also the scripted local build entry point.
2. The mission picker appears immediately. Use **Up/Down** or gamepad **D-pad** to highlight a mission, then press **Enter**, **Space**, gamepad **Start**, or gamepad **South** to start. Keyboard **1-9 and 0** directly starts the original first ten missions; use arrow/D-pad selection for later missions including Kitchen Falling Food Frenzy.
   - All 20 missions use the adaptive two-column picker.
   - Each tile shows `NEW`, `RETRY`, `CLEARED`, or `FLAWLESS` plus its session-best score; the selected detail line shows round time and objective size.
   - The header keeps missions played/tried, total score, and flawless clears visible before the next choice.
3. Read the one-line mission briefing at the start of the round. The HUD keeps the current mission name, objective, score, timer, controls, modifier, and latest score event visible during play.
4. When a mission ends, choose **Replay**, **Next Mission**, or **Mission Select** with the on-screen buttons, keyboard, or gamepad:
   - **R / Enter / Start / South** replays the current mission.
   - **N / Right Arrow / Right Shoulder / D-pad Right** starts the next unfinished mission.
   - **M / Escape / East button / D-pad Left** returns to mission select.
5. After every three newly tried mission variants (3/6/9/12), the Next action opens a simple **Session Summary** with missions played, total score, stars, and ranks earned. Choose **Continue Session** (Enter/Start) to launch the next unfinished mission, **Mission Select** to choose freely, or **New Session** to reset local stats. Between milestones, Next Mission continues directly without repeating the summary.
   - Rank history is bounded to the three most recent results with an `+N earlier` count, preventing replays and a full 12-mission run from overflowing the summary panel.

No progress is saved; the session loop is intentionally local and lightweight.

## Mission variants

### Backyard Rescue

This is the existing mission loop and remains the default when `ArenaScene` starts. Clear the mission by completing all required objectives before the `90` second timer expires:

1. Recover enough **Breakfast/Weenies** (`6` items in the current prototype).
2. Keep the **Squirrel** from stealing too many items (`3` stolen food ends the run).
3. Resolve the **Predator Warning / Predator Attack** with a united-front bark or a rescue.
4. Complete the **Rope/Tug** shared-object objective.
5. Complete the authored **Squirrel Trap** twice. Pass one assigns Cheddar to bark-pressure the squirrel while Cocoa holds the marked escape gap; the redirect drops the targeted weenie, and only Cocoa can recover it. Pass two reverses the roles: Cocoa pressures while Cheddar holds the gap and recovers the drop.

The HUD objective and dog-local arrows name the current pressure dog, route the partner to the visible **ESCAPE GAP - HOLD HERE** marker, and switch to **RECOVER DROP / PARTNER ONLY** after a redirect. Barking with the wrong dog or before the gap is held makes the squirrel take a comic fake route and loop back. If the pressure dog touches its own dropped weenie, the weenie bounces away with a **HOT POTATO! PARTNER ONLY!** cue; the drop remains live, so no trap mistake hard-fails the mission.

Unique scoring/events include **+50 WEENIE SAVED**, **+25 SQUIRREL SCARED**, **+300 PREDATOR YEETED**, **+250 PARTNER RESCUE**, **+200 TUG COMPLETE**, and **+500 LEVEL CLEAR** plus time bonus.

Deterministic hooks are `ForceBackyardTrapRedirect(pressureDog, gapHeld)` and `ForceBackyardTrapRecovery(dog)`. `BackyardSquirrelTrapPlayModeTests` covers wrong-role/open-gap recovery, partner-only pickup, role reversal, completion, event logging, and replay reset. Backyard clear now additionally requires both trap recoveries; the existing collect, squirrel-steal limit, predator, and tug requirements are unchanged.

Manual acceptance check: start Backyard Rescue with **1**. Confirm the blue escape-gap marker and the initial Cheddar/Cocoa role copy are readable. Let the squirrel target a weenie, park Cocoa in the gap, and bark near the squirrel as Cheddar. Confirm the squirrel redirects and drops the weenie. Touch it first as Cheddar and confirm the comic bounce is recoverable; collect it as Cocoa and confirm the guidance swaps. Repeat with Cocoa pressuring and Cheddar holding/recovering. Then finish the original food, predator, and tug objectives and confirm the mission clears. Also verify barking before the holder reaches the gap produces a fake route without losing the run.

### Snack Heist

Snack Heist is a compact protect-and-collect mission using the existing item and squirrel systems without predator or tug requirements. Cheddar and Cocoa must stash `4` forbidden snacks before the squirrel steals `2` within `80` seconds.

Readable differences:

- Collectibles are round orange snack placeholders with plate/crumb markers and **Snack!** labels.
- Objective text starts as **Stash snacks 0/4**.
- Squirrel pressure uses snack-specific labels like **SQUIRREL SNACK HEIST - BARK!** and **SQUIRREL STOLE A SNACK!**.
- Unique scoring/events include **+60 SNACK STASHED**, **+35 SNACK GUARD BARK**, **-90 SNACK THIEF**, and **SNACK HEIST CLEAR**.
- Clear banner: **SNACK STASH SAVED!**.
- Fail reason calls out the squirrel union escaping with forbidden snacks.

Manual check: press **2**, collect one snack, confirm **+60 SNACK STASHED** and **Stash snacks 1/4**. Let or force the squirrel to steal twice and confirm GameOver/replay uses Snack Heist copy.

### Sock Panic

Sock Panic is now built around the **Tip and Dive** co-op puzzle beat. One dog interacts beside the laundry basket to tip it open; the other dog has `6` seconds to dive onto the exposed sock. The basket-tipper cannot collect their own exposed sock, so players must alternate or deliberately keep their roles. Cheddar and Cocoa must return `5` socks before the `55` second timer expires, with squirrel, predator, and rope actors disabled.

Readable differences:

- A generated slatted **LAUNDRY BASKET** is the shared puzzle object; hidden socks only become visible after a tip.
- Objective arrows show **TIP BASKET**, then split into **HOLD BASKET** and **DIVE FOR SOCK** roles.
- Objective text tracks returned socks and fumbles, then calls out the temporary partner-dive window.
- Unique scoring/events include **+20 BASKET TIPPED**, **+40 PARTNER SOCK DIVE**, **-15 DECOY SOCK FUMBLE**, and **SOCK PANIC CLEAR**.
- Clear banner: **SOCKS SORTED!**.
- Time failure reason: **Laundry order returned before the final sock was rescued.**

Funny failure/recovery: if the basket-tipper grabs the sock or the opening times out, the sock is exposed as a decoy/fumble and the basket flops shut. Nothing is permanently lost; either dog can tip it again.

Manual check: press **3**, move one dog to the basket and interact, then use the partner to collect the exposed sock. Confirm **+20 BASKET TIPPED**, **+40 PARTNER SOCK DIVE**, and `1/5` returned. Intentionally grab with the basket-tipper and let one opening time out; confirm both produce **-15 DECOY SOCK FUMBLE** and reset to **TIP BASKET**. Replay and confirm socks, fumbles, basket state, and score reset.

### Squirrel Conspiracy

Squirrel Conspiracy is the first production-mission slice using `HerdingMissionState`. Cheddar and Cocoa must herd the suspicious squirrel around its route, earn coordinated cutoff progress, survive fake-outs/taunts, reveal the hidden stash, then interact to crack the case before the squirrel reaches `3` taunts. Each route step now activates one visible **HOLD CUTOFF** zone: the dog nearest the squirrel receives **BARK HERD** guidance while the partner is routed to that zone. A cutoff scores only when the partner is actually holding the active zone; ordinary separation no longer counts.

Readable differences:

- The squirrel stays active as the primary objective instead of stealing spawned collectibles.
- Objective text tracks route/control progress, stash reveal, and taunt pressure.
- Unique scoring/events include **GOOD HERD**, **CUTOFF**, **DOUBLE BARK BLOCK**, **FAKE OUT**, **STASH FOUND**, and **CONSPIRACY CRACKED** from the production score catalog.
- Clear banner: **CONSPIRACY CRACKED!**.
- Fail reason calls out the squirrel taunting the yard into a misinformation spiral.

Manual check: press **4**, bark near the squirrel from useful positions until controls reach `4/4`, then move to the revealed stash and interact. Confirm the end summary says **Conspiracy Cracked**. Let or force three taunts and confirm fail/replay reset the herding counters.

### Eagle Shadow Panic

Eagle Shadow Panic is the second production-mission slice using `ThreatSweepMissionState`. A sweeping eagle shadow threatens the yard; Cheddar and Cocoa must hide in cover during sweeps, then split roles so one dog distracts while the other rescues a stranded toy, then huddle for a united-front bark circle to drive the eagle off. Three exposures (caught in the open) ends the run.

Readable differences:

- The predator actor is repurposed as the **EAGLE SHADOW SWEEP** threat and the squirrel actor is repurposed as the stranded **TOY** to rescue (no spawned collectibles or squirrel-steal loop).
- Objective text moves through three phases: hide (`safe hides x/2, exposures x/3`), distracted rescue (`rescue the toy in the open`), and the final `United-front bark circle`.
- The rescue objective only opens after `2` safe hides; the toy rescue then unlocks the united-front phase.
- Unique scoring/events include **SAFE HIDE**, **SHADOW DISTRACTED**, **EAGLE SPOOK** (exposure penalty), **TOY RESCUED**, **UNITED FRONT**, and **SHADOW PANIC CLEAR**.
- Clear banner: **EAGLE DRIVEN OFF!**; end summary on a clear reads **Backyard Defenders**.
- Fail reason calls out the eagle shadow catching dogs in the open too many times (**Shadow Trouble** summary).

In real-time play the hide phase is spatial: three labeled **HIDE HERE** cover zones sit in the yard and the eagle shadow physically sweeps left/right across the field. On each sweep pass, a dog caught in the shadow column and not inside a cover zone is exposed; otherwise the dogs score a safe hide. Two clean hides open the rescue.

The deterministic test/pacing hooks are `ForceEagleShadowSafeHide()`, `ForceEagleShadowExposure()`, `ForceEagleShadowSweepPass()`, `ForceEagleShadowRescue(dog)`, and `ForceEagleShadowUnitedFront()` (`EagleCoverZones` exposes the cover positions); in normal play the final united-front phase also resolves through the existing huddled united-bark path.

Manual check: press **5**, hide twice to open the rescue objective, interact near the toy to rescue it, then bring both dogs together and bark to complete the united front. Confirm the end summary says **Backyard Defenders**. Force three exposures and confirm fail/replay reset the threat-sweep counters.

### Coyotes at the Fence

Coyotes at the Fence is the third production-mission slice using `PatrolDefenseMissionState`. A coyote tests weak spots along the yard fence; one dog must **bark-pin** the coyote while the partner **fills the weak spot** — fills only progress while bark pressure is held, forcing a role split. A late **fake snack lure** tempts whichever dog is closer (a Cheddar-specific gag) and is defused by barking instead of taking the bait. After enough weak spots are filled, both dogs bark down the final coyote push. Three breaches end the run.

Readable differences:

- The predator actor is repurposed as the **COYOTE AT THE FENCE** and the squirrel actor as the moving **WEAK SPOT / FILL DIRT** marker (no spawned collectibles or squirrel-steal loop).
- Objective text moves through patrol (`fence gap n, repairs x/3, breaches x/3`), pinned (`partner fill the weak spot now`), lure (`ignore the fake snack lure`), and final-push (`both dogs bark together`) states.
- A fill attempt with no partner bark pressure is rejected as a missed interaction, so the role split is mechanically enforced.
- Unique scoring/events include **FENCE HELD**, **DIRT FILLED**, **COYOTE BLOCKED**, **FAKE SNACK BAIT**, **COYOTE BREACH** (penalty), and **YARD DEFENDED**.
- Clear banner: **YARD DEFENDED!**; end summary on a clear reads **Fence Guardians**, on a breach fail reads **Needs More Patrols**.

In real-time play the coyote physically prowls toward the active **WEAK SPOT** (one of four labeled fence gaps around the yard). If the dogs are holding bark pressure when it arrives it is driven back; an unguarded gap breaches.

The deterministic test/pacing hooks are `ForceCoyoteBarkPressure(dog)`, `ForceCoyoteRepair(dog)`, `ForceCoyoteBreach()`, `ForceCoyoteFakeSnack()`, `ForceCoyoteProwlReach()`, and `ForceCoyoteFinalBlock()` (`FenceGaps` exposes the gap positions); in normal play barking pins the coyote and the final push also resolves through the existing huddled united-bark path.

Manual check: press **6**, bark to pin the coyote, then interact at the weak spot to fill it; repeat three times, then bring both dogs together and bark to block the final push. Confirm the end summary says **Fence Guardians**. Force three breaches and confirm fail/replay reset the patrol counters.

### Weenie Roundup

Weenie Roundup is a shared-object **carry** mission using `CarryRoundupMissionState` — the first level built around the carry dog-verb and the large yard. Five **WEENIE** markers are scattered across the field and a **HOME BOWL** sits in the back corner. A free dog that walks onto a loose weenie picks it up and carries it; reaching the bowl delivers it. Deliver all five before the `85` second timer expires. Both dogs can carry in parallel, so the fastest clear splits the yard between them.

Readable differences:

- No squirrel/predator/tug; the loop is pick-up → carry → deliver across the whole yard.
- A carried weenie rides above the dog; a fumble (`ForceWeenieDrop`) bounces it a couple units away so the dog has to chase it down again.
- Objective text tracks deliveries and loose count, switching to "Carry the weenie to the HOME BOWL" while carrying.
- Unique scoring/events include **WEENIE GRABBED**, **WEENIE DELIVERED**, **FUMBLED WEENIE** (penalty), and **ROUNDUP COMPLETE**.
- Clear banner: **BOWL FILLED!**; end summary on a clear reads **Weenie Wranglers**, with fumbles reading **Butterpaws**.

The deterministic test/pacing hooks are `ForceWeeniePickup(dog)`, `ForceWeenieDeliver(dog)`, and `ForceWeenieDrop(dog)` (`BowlPosition` exposes the bowl); in normal play pickup/deliver/drop are driven by dog proximity to weenies and the bowl.

Manual check: press **7**, walk a dog onto a weenie to grab it, carry it to the bowl, and repeat (split the dogs up) until the bowl shows 5/5. Confirm the end summary says **Weenie Wranglers**.

Pickup now produces `WEENIE GRABBED!`, a proud carrier pose, collection cue, and light rumble. A drop
produces `FUMBLE!` at the bounced weenie, a worried flinch, warning cue, and stronger rumble before
the chase resumes.

### Scent Search

Scent Search is a **sniff + dig** mission using `ScentSearchMissionState` — the info-gathering verbs the other levels don't use. Six **DIG?** mounds are scattered across the yard; one hides a bone. **Bark to sniff**: the closer the sniffing dog is to the buried bone, the hotter the readout (`RED HOT` / `WARM` / `COLD`). **Interact to dig** the mound you think is hot; the right one yields a bone and re-buries the next one elsewhere, a cold one wastes a dig. Find three bones before four cold digs (or the timer) end the run.

Readable differences:

- No squirrel/predator/tug; the loop is sniff-for-heat → dig-the-hot-spot.
- Bark is repurposed as the sniff/scent read; interact is the dig.
- Objective text tracks bones found and cold digs.
- Unique scoring/events include **HOT SNIFF**, **BONE DUG UP**, **COLD DIG** (penalty), and **SEARCH COMPLETE**.
- Clear banner: **BONES UNEARTHED!**; end summary reads **Master Sniffers** on a clean clear, **Dug Up The Whole Yard** when there were wasted digs.

The deterministic test/pacing hooks are `ForceScentSniff(dog)`, `ForceScentDigCorrect(dog)`, and `ForceScentDigWrong(dog)` (`DigSpots` exposes the mound positions); in normal play sniff/dig are driven by barking and interacting near the mounds.

Manual check: press **8**, bark near a mound to read the heat, move toward hotter readings, and interact to dig. Confirm digging the hot mound says **BONE DUG UP** and three finds clear with **Master Sniffers**.

A cold dig now adds a worried dog flinch, warning cue, and short rumble to the existing `COLD!` pop
and score penalty. The failed read is visible, audible, and physical before the player chooses another
mound.

### Thunderstorm Comfort

Thunderstorm Comfort is a **comfort / panic co-regulation** mission using the existing `PanicMeter` primitive — and it leans hard into Cheddar/Cocoa identity: Cheddar (chaos puppy) spooks harder at each clap than Cocoa (veteran queen). A storm throws periodic **thunderclaps** that spike both dogs' panic; the only way panic comes down is the two dogs **huddling close** to comfort each other. Weather five claps without either dog's panic maxing out (which makes them bolt and fails the run).

Readable differences:

- No collect/squirrel loop; the whole mission is positioning — stay close through each clap, drift apart and panic climbs.
- A STORM CLOUD indicator flashes "HUDDLE!" on each clap; the HUD objective shows claps weathered and current panic %.
- Unique scoring/events include **CLAP WEATHERED**, **COMFORT HUDDLE**, and **STORM PASSED**.
- Clear banner: **STORM WEATHERED!**; end summary reads **Weathered The Storm** on a clear, **Spooked By Thunder** on a bolt.

The deterministic test/pacing hooks are `ForceThunderclap()` and `ForceComfortStep(seconds)` (`Panic` and `ThunderstormState` expose the live values); in normal play claps fire on a timer and `PanicMeter.Step` drains panic while the dogs are within cuddle range.

Manual check: press **9**, keep both dogs close together, and ride out the claps; confirm panic falls while huddled and rises when split, and that five weathered claps clear with **Weathered The Storm**.

### Mark the Yard

Mark the Yard is a **territory-control** mission using `TerritoryMissionState`. Five **CLAIM** zones are spread across the yard; a dog standing in a zone marks it (turns green). But the squirrel periodically **re-marks** the claimed zone nearest to it, so the dogs must split up and cover ground to hold every zone at once. Win the moment all five are claimed simultaneously; the timer expiring (with the squirrel chipping away) is the fail.

Readable differences:

- The squirrel is repurposed as a territory rival that steals zones back rather than stealing food.
- Zones recolor grey→green when held and flash "SQUIRREL STOLE IT!" when re-marked.
- Objective text tracks zones held and how many the squirrel has stolen back.
- Unique scoring/events include **ZONE MARKED**, **ZONE STOLEN** (penalty), and **YARD MARKED**.
- Clear banner: **YARD CLAIMED!**; end summary reads **Yard Is Ours** on a clear, **Squirrel Keeps Stealing It** when the squirrel chipped in.

The deterministic test/pacing hooks are `ForceClaimZone(dog)` and `ForceSquirrelReclaim()` (`TerritoryZones` exposes the zone positions); in normal play claiming is driven by a dog standing in a zone and the squirrel re-marks on a timer.

Manual check: press **0**, split the dogs to stand in different zones until all five glow green at once; confirm the squirrel steals one back if you leave it too long, and that holding all five clears with **Yard Is Ours**.

### Walkies on the Leash

Walkies on the Leash is a **tethered-coordination** mission using `LeashWalkMissionState` — the eleventh mission, reachable by arrow-selecting past Mark the Yard (the number row 1-9/0 maps to the first ten). The two dogs share one leash and must walk through four **CHECKPOINT** markers in order; both dogs have to stand on the current checkpoint together to bank it. If they drift more than the leash length apart, it snaps taut (a rate-limited penalty); four snaps fail the walk. The dogs start side by side so the leash is slack — staying close as they cross the yard is the whole challenge.

Readable differences:

- No squirrel/predator; the loop is paired movement under a distance constraint.
- Checkpoints light up in sequence; only the current one is active.
- Objective text tracks the current checkpoint and snap count.
- Unique scoring/events include **CHECKPOINT**, **LEASH SNAP** (penalty), and **WALK COMPLETE**.
- Clear banner: **WALK COMPLETE!**; end summary reads **Best Walk Ever** on a clear, **Tangled Leash** when the leash snapped.

The deterministic test/pacing hooks are `ForceReachCheckpoint()` and `ForceLeashSnap()` (`LeashCheckpoints` exposes the positions); in normal play both are driven by the dogs' positions and the distance between them.

Manual check: arrow to Walkies on the Leash, keep both dogs close as you walk to each checkpoint in turn, and confirm drifting apart snaps the leash. Reaching all four clears with **Best Walk Ever**.

Checkpoint success now gives both dogs a brief proud pose alongside the score pop. A leash snap
produces a midpoint `LEASH SNAP!` warning, threat cue, rumble, and synchronized worried flinch before
normal walking resumes.

### Car Ride Balance

Car Ride Balance is a **vehicle-balance** mission using `CarBalanceMissionState` — the twelfth mission (arrow-select), and the one that completes coverage of every mechanic module in the design library. The dogs ride in the back of a car that lurches side to side; the live tilt runs from fully-left (-1) to fully-right (+1), and the dogs lean it by which side of centre they stand on. To stay level they must sit opposite the current tilt. Ride out six lurches without tipping (four spills fails the drive).

Readable differences:

- No squirrel/predator loop; it's continuous balance management under periodic lurches.
- The car indicator shows the current tilt direction and percentage; the objective reads LEVEL / tipping LEFT / tipping RIGHT.
- Unique scoring/events include **STEADIED**, **SPILL** (penalty), and **RIDE COMPLETE**.
- Clear banner: **MADE IT HOME!**; end summary reads **Smooth Riders** on a clear, **Car Sick** when the car spilled.

The deterministic test/pacing hooks are `ForceCarLurch()` and `ForceCarSpill()` (`CarBalance` exposes the live tilt); in normal play lurches fire on a timer and the dogs' average side leans the car.

Manual check: arrow to Car Ride Balance, and as the car tilts one way, move both dogs to the opposite side to bring it level; ride out six lurches to clear with **Smooth Riders**.

Each survived lurch now produces a shared `STEADIED!` pop, success cue, rumble, and proud pack pose.
A spill produces `CAR SPILL!`, a threat cue, stronger rumble, and synchronized worried flinch so the
cause of lost balance reads before the next lurch.

> **Mechanic-module coverage:** with Car Ride Balance, all nine `ProductionMechanicModule` values (Herding, ThreatSweep, PatrolDefense, SharedObject, TerritoryControl, ScentSearch, RhythmPanic, VehicleBalance, LeashPhysics) now have at least one playable, tested mission.

### Kitchen Falling Food Frenzy

Kitchen Falling Food Frenzy is a compact counter-to-floor relay. Cheddar is the counter scout: he must reach the marked **COUNTER** route and **bark** to knock the next item loose. The bark first reveals a colored counter flash and pulsing floor landing circle, then releases the item after a readable delay. Cocoa is the floor sweeper: she tracks gold food into the marked **SAFE BOWL** to score, while purple onions should be dodged and allowed to splat.

- Dog-local arrows and the team route line always name the current jobs: **BARK-KNOCK FOOD / GUARD THE BOWL**, then **RESET AT COUNTER / CATCH GOLD IN BOWL** or **DODGE PURPLE** during the telegraph and fall.
- Good catches build a score combo. Missing good food, catching outside the bowl, eating an onion, or attempting the partner's role breaks momentum but remains recoverable.
- Gold placeholder food means catch; purple placeholder onion means dodge. Cocoa gets distinct proud/flinch poses, audio, pop text, and controller rumble for catch, dodge, splat, and gross-out outcomes.
- After three warm-up catches, a short **DINNER RUSH** finale calls a fixed **GOOD → BAD → GOOD** sequence with shorter telegraphs and faster falls. Each call still requires Cheddar's bark and Cocoa's correct catch/dodge response. A mistake retries the current call, so the finale adds pressure without an unrecoverable fail state. Five good catches plus the finale onion dodge clear the mission.
- The Kitchen stations now occupy a compact 13-unit vertical stage inside the arena. At the 16:9 couch target the shared camera needs at most `12` orthographic units to frame both stations, keeping both dogs and the landing lane readable without split-screen.
- No squirrel, predator, tug, or generic collectible loop runs during this mission.

Deterministic hooks are `ForceKitchenTelegraph(dog, kind)`, `ForceKitchenReleaseTelegraph()`, `ForceKitchenDrop(kind)`, `ForceKitchenCatch(dog, intoSafeZone)`, and `ForceKitchenLetFall()`. `KitchenFoodFrenzyMissionStateTests` covers bark/telegraph ownership, busy-state rejection, recoverable outcomes, finale sequencing, completion, and reset. `KitchenFoodFrenzyPlayModeTests` covers mission wiring, warning objects, compact camera framing, good/bad feedback paths, the dinner-rush clear, and replay reset.

Manual acceptance check: arrow-select **Kitchen Falling Food Frenzy** with two local players. Confirm the initial shared camera keeps both dogs, the counter, and bowl readable. Move Cheddar into the counter route and bark; verify no item falls merely from standing there. Confirm the colored counter flash and landing circle appear before the item releases. For gold food, intercept it with Cocoa while she is inside the bowl and confirm **YUM**, proud pose, score, audio, and light rumble. For a purple onion, move Cocoa clear and let it splat; confirm **DODGED**, proud feedback, and no lost progress. Let one good item splat and deliberately eat one onion; confirm the warning/flinch feedback is distinct and both calls can be retried. After three warm-up catches, confirm **DINNER RUSH** announces **GOOD → BAD → GOOD**, uses visibly tighter timing, and still waits for a Cheddar bark before every call. Complete the three calls and confirm **KITCHEN CLEARED!**. Replay and confirm food, telegraphs, finale progress, combo, and feedback reset.

## Level scale and camera

The backyard is built at **120 x 68 world units**. A runtime dachshund is roughly 2 units long, so a dog occupies about **1.7% of the property width** instead of reading as a giant character in a single-screen demo box. The dogs spawn near the center (`±10, 0`) and mission routes, hide zones, fence gaps, dig sites, loose weenies, territory zones, and leash checkpoints are distributed using normalized coordinates across 70–94% of the yard width.

Because this is one-screen couch co-op, the shared camera is a **dynamic clamped follow-cam with two meaningful modes**. When the dogs regroup it uses a local scrolling view (`7.5` ortho, about 27 units wide at 16:9), revealing less than one quarter of the property width. When they split up it can pull back to a strategic full-yard view (`34` ortho), keeping both players visible. Patio, pond, garden, shade tree, picnic, sandbox, stepping-stone path, and fence dressing divide the property into recognizable districts so the extra traversal space is not an empty green plane.

Outdoor level scale is now a production contract: dogs should remain at or below 2% of a major level's width, close framing must require scrolling, and important mission geometry must span most of the playable bounds. Pool and other future outdoor levels should follow this contract rather than inherit the frozen Canvas prototype's oversized dog-to-feature ratio.

## Objective

The round can end in **LevelClear** or **GameOver**, and either result can be restarted. Current pacing is hand-tuned for a first two-player playtest: `90 / 70 / 55` second mission timers, a 5-second mission intro banner, delayed first squirrel pressure, an ~25-second predator telegraph in Backyard Rescue, and a short but readable tug charge so both players have to stay committed for a moment.

The opening HUD banner says: **Cheddar + Cocoa must protect the weenies together.** For the first few seconds, the squirrel is labeled **Squirrel: WAITING**, the predator is **Predator: OFFSCREEN**, and the rope is **Rope/Tug - BOTH DOGS**. This is intentional: players should first read their spawn, dog identity, first weenie arrows, and shared fantasy before the first threat competes for attention.

The end loop is intentionally simple: players see current score, the latest score swing, a short reason line, session totals, and Replay / Next Mission / Mission Select actions. Score deltas now appear both as a brief HUD pop and as small world text near the action so cause/effect is easier to read during chaos. The exposed deterministic state includes `CurrentFlow`, `MissionSelectVisible`, `SelectedMissionVariant`, `Score`, `LastScoreDelta`, `LastScoreEventLabel`, `LastScorePopLabel`, `ScorePopVisible`, `ObjectiveLabel`, `Outcome`, `EndRank`, `EndSummaryLabel`, `EndReasonLabel`, `ReplayPromptVisible`, `EndReplayAvailable`, `EndNextMissionAvailable`, `EndMissionSelectAvailable`, `SessionMissionsPlayed`, `SessionTotalScore`, `SessionStarsEarned`, `SessionUniqueMissionsCompleted`, `SessionSummaryLabel`, `LastJuiceFeedback`, and `LastJuiceLabel`.

## Controls

| Player | Dog | Controller | Keyboard | Bark | Interact |
| --- | --- | --- | --- | --- | --- |
| P1 | Cheddar | Gamepad slot 0 | WASD | Space / X button | E / Y button |
| P2 | Cocoa | Gamepad slot 1 | Arrow keys | Enter / X button | Right Shift / Y button |

Mission flow controls:

- Mission select: keyboard **arrow keys** or gamepad **D-pad** move in the visible two-column grid (up/down stays in a column; left/right crosses columns); **Enter**, **Space**, gamepad **Start**, or gamepad **South** starts; **1-9 and 0** starts a mission directly.
- During a run: keyboard **1-9 and 0** still restarts the arena into Backyard Rescue, Snack Heist, Sock Panic, Squirrel Conspiracy, Eagle Shadow Panic, Coyotes at the Fence, Weenie Roundup, Scent Search, Thunderstorm Comfort, Mark the Yard, Walkies on the Leash, or Car Ride Balance (arrow-select) for quick manual comparison.
- During a run: **Escape** or gamepad **Start** pauses. The pause card can resume, return safely to
  mission select, or quit the packaged player; pausing freezes mission time and dog input.
- End screen: **R / Enter / Start / South** replays; **N / Right Arrow / Right Shoulder / D-pad Right** advances; **M / Escape / East / D-pad Left** returns to mission select.
- Session Summary: **Enter**, **Space**, **Start**, **South**, or **Right Shoulder** continues to the next unfinished mission; **M**, **Escape**, or gamepad **East** returns to mission select. Finishing all 12 changes Continue to an explicit **Victory Lap** that wraps to mission one; **New Session** clears all local results.
- Playtest Mode: click the bottom-left **Playtest Mode: On/Off** button or press **F1** / **`**. It toggles a compact top-right diagnostics overlay and does not pause or block normal play.
- Placeholder feedback toggles: **F2** toggles generated audio cues; **F3** toggles gamepad rumble requests. Both default to on.

## First playtest protocol

Use this protocol before changing tuning again:

1. Open `unity/CheddarAndCocoa` in Unity 6 LTS, open `Assets/Scenes/ArenaScene.unity`, press Play, and turn **Playtest Mode** on.
2. Start on mission select. Do not explain the game at first; let the two players try to identify their dogs, choose a mission, move, bark, and react.
3. Watch silently for the first run unless they are fully blocked by hardware/input confusion.
4. After the first end screen, ask short questions, then have them use **Replay** and **Next Mission** without a mouse.
5. Run the core three missions at least once: **Backyard Rescue**, **Snack Heist**, and **Sock Panic**. Also run **Squirrel Conspiracy** when validating the production herding slice.
6. After all three have ended, use **Next / Session Summary** and confirm the players understand the session totals.
7. Write down confusion points using the playtest overlay and event log rather than fixing during the session.

What to observe:

- Can each player identify whether they are Cheddar or Cocoa without only reading labels?
- Do players understand the current objective label before the first squirrel/predator pressure arrives?
- Does bark feel like a useful verb, especially for squirrel, united-front defense, and rescue?
- Does the rope/tug objective cause communication or just confusion?
- Do score pops and world pops explain why score changed?
- Do players naturally find Replay, Next Mission, and Mission Select?
- Are failures funny/recoverable, or do they feel arbitrary?
- Does the camera keep both dogs, objectives, and hazards readable?

Ask testers:

1. What did you think the goal was when the mission started?
2. Which dog were you, and how could you tell?
3. When did barking feel useful?
4. Was there a moment where you did not know what to do next?
5. What did you think the squirrel was doing?
6. What did you think the predator warning wanted from both players?
7. Did tugging the rope feel cooperative?
8. Did the end screen make Replay and Next Mission clear?
9. Which mission would you replay first?
10. What was funny or personal, and what felt generic?

Known rough edges for this playtest:

- Placeholder art and generated tones are still prototype-only.
- There is no final tutorial; the objective label, arrows, labels, and playtest observer have to carry clarity.
- Keyboard two-player controls share one keyboard, so controller play is the better test if two gamepads are available.
- The visible Playtest Mode button is an IMGUI debug control and may overlap a tiny corner of the play space.
- Local session totals reset when Play mode or the app exits.
- Event logs are in-memory only; there is no file export yet.

## Local Mac build path

Use this only for a quick couch playtest build; no signing, installer, store, or distribution work is needed.

From the repo root:

```sh
./unity/build-dev.sh
```

Expected result:

- Unity runs in batch mode with `CheddarAndCocoa.EditorTools.ArenaDevBuild.BuildDevMac`.
- The only scene in the build player options is `Assets/Scenes/ArenaScene.unity`.
- The output app is written to `unity/builds/dev/CheddarAndCocoa-Arena.app`.
- The script ends with `Development build ready: .../unity/builds/dev/CheddarAndCocoa-Arena.app`.

Manual fallback if batch mode is unavailable:

1. Open Unity Hub and launch `unity/CheddarAndCocoa` with Unity 6 LTS.
2. Open `Assets/Scenes/ArenaScene.unity`.
3. Confirm `File -> Build Profiles...` (or `File -> Build Settings...` on older Unity UI) includes `Assets/Scenes/ArenaScene.unity`; it is already listed in `ProjectSettings/EditorBuildSettings.asset`.
4. Select **macOS** as the target platform and enable **Development Build**.
5. Choose **Build** or **Build And Run**.
6. Save the output somewhere local and ignored, for example `unity/builds/dev/CheddarAndCocoa-Arena.app`.
7. Launch the built app, start each mission from mission select, and press **F1** / **`** or the bottom-left Playtest Mode button to verify the overlay is available.

Before handing off a build, run:

```sh
./unity/run-playmode-tests.sh
```

For one command that checks project structure, scene wiring, PlayMode tests, and the local dev build:

```sh
./unity/validate-demo.sh
```

Use `./unity/validate-demo.sh --skip-build` when validating on a machine that can run tests but does not have macOS build support installed.

## Playtest/debug visibility

`GameManager` owns a lightweight in-memory `PlaytestEventLog` for the current Unity Play session. The log is exposed through `PlaytestLog`, `PlaytestEvents`, and `LastPlaytestEvent`, so PlayMode tests or a temporary inspector can read it without scraping UI. Events are numbered and deterministic rather than timestamped. Captured event types include mission select/start, objective changes, bark, collection, squirrel pressure/scare/steal, tug/rescue, score deltas, clear/fail, replay, next mission, overlay toggles, and session summary.

The playtest overlay shows mission, flow/phase, timer, score, last score event, current objective, fail pressure, Cheddar/Cocoa positions, current-round friction counters, failures by mission, session totals, outcome/rank, and the latest event. Friction counters are intentionally small and local: `BarksUsed`, `FailedInteractions`, `ObjectiveChangeCount`, `MissionDurationSeconds`, `MissionReplayCount`, and `FailuresForMission(...)`. They are meant to flag confusion for a human observer, not to become analytics.

Manual check: press **F1** during mission select and during play, or click the bottom-left Playtest Mode button. Confirm the overlay appears in the top-right, does not hide the dogs or end buttons, and updates after collecting an item, barking, missing an interaction, forcing a fail/clear, replaying, and choosing Next Mission. The overlay also reports whether audio and rumble are enabled plus the latest requested cue/request name.

## Audio/rumble placeholder checks

The arena now has replaceable generated feedback slots in `ArenaFeedbackCatalog`, plus a light
looping procedural backyard music bed that follows the F2 audio toggle. The event cues remain
placeholder tones/noises and rumble pulses; authored bark recordings, final mixing, and final
haptics are still required.

Manual audio check:

- Start any mission and bark. Confirm a short comic bark cue plays.
- Collect a weenie/snack/sock. Confirm a small score-gain cue plus collect cue plays.
- Let the squirrel steal or force a fail. Confirm a lower warning/penalty cue plays.
- Complete rescue/tug or clear a mission. Confirm the success/win cue is brighter than the penalty cue.
- Use Replay, Next Mission, and Mission Select. Confirm a small UI blip plays.
- Press **F2** and repeat bark/collect. Confirm normal gameplay continues while generated audio is muted.

### Dog-local action audio tuning (2026-06-20)

The Backyard dog-local procedural layer was checked separately from the arena-wide placeholder cues.
The repeatable two-player check drives Cheddar and Cocoa at the same time, verifies their action
signatures, and exercises bark over an active carry bed. It is an in-engine mix/concurrency check;
the final authored-SFX pass still needs a physical two-person television listening session.

Concrete findings and resulting profile changes:

- Cheddar and Cocoa were already separated reliably by pitch and harmonic content. Their impact
  fundamentals remain distinct across bark (`237.6 / 147.6 Hz`), tug (`508.95 / 318.6 Hz`), carry
  (`572.4 / 360.45 Hz`), rescue (`635.85 / 402.3 Hz`), and zoomies (`699.3 / 444.15 Hz`).
- Rescue, tug, carry, and zoomies previously shared the same `0.50` impact volume, so the critical
  rescue read had no priority. Impact volumes are now rescue `0.66`, bark `0.58`, tug `0.40`, carry
  `0.30`, and zoomies `0.27`.
- All sustained actions previously looped at `0.30`, which let two dogs' continuous beds mask bark
  and rescue. Tug/carry/zoomies sustain volumes are now `0.15 / 0.09 / 0.12`, with longer loop
  grains to reduce repetition.
- Sustained cue restart cooldown is now `1.2s`. A bark or rescue can override the visible action
  without stacking a second tug/carry/zoomies loop when the dog returns to that action.
- The dog-local limit is now two voices per dog instead of three. Simultaneous Cheddar/Cocoa carry
  plus bark peaks at four local voices while preserving both dogs' bark impacts; a six-voice local
  pile-up is no longer possible.
- One-shot cooldowns now match importance and expected cadence: bark `0.30s`, tug `0.40s`, carry
  `0.50s`, rescue `0.70s`, and zoomies `0.80s`.

Automated acceptance is in `DogProceduralAudioPlayModeTests`: both identities cover all five action
profiles, action-volume priority is explicit, sustained restart throttling is checked for both dogs,
and the combined two-dog voice ceiling is asserted. No mission state, scoring, objective, input, or
movement tuning changed in this pass.

Manual rumble check with a gamepad:

- Bark gives a small pulse.
- Rescue, tug completion, predator defense, and mission win give a stronger pulse.
- Squirrel pressure/steal, predator hit, and mission fail give a short warning pulse.
- Press **F3** and repeat bark/rescue/fail. Confirm normal gameplay continues with rumble disabled.
- No controller connected is expected to be a safe no-op.

Cheddar is the chaos puppy and Cocoa is the steadier veteran. The placeholder sprites are still simple generated shapes, but the dogs now have a reusable global identity direction proven in the arena: both read as long, low miniature dachshunds with visible head, long snout, floppy ear, tiny feet, tail, collar, and expression markers. Cheddar reads as **CHEDDAR CHAOS PUP** with a golden body, red collar, bright chaos tuft/flash, faster wag, and more explosive bark/proud motion. Cocoa reads as **COCOA SPOT QUEEN** with a chocolate body, teal collar, cream chest, spot markings, steadier expression, and tiny queen marker. Idle, run, bark, tug, stunned, rescued, proud, and sad states are exposed through body squash/rotation, tail/head/ear motion, color-shifted labels, and deterministic PlayMode assertions.

Current pose labels:

- Cheddar idle/run: **WIGGLE READY** / **CHAOS ZOOM**.
- Cocoa idle/run: **QUEEN READY** / **SPOT PATROL**.
- Shared action states: **WOOF!**, **TUG!**, **STUNNED**, **RESCUED!**, **PROUD!**, **SAD FLOP**.

Each dog also gets a small objective arrow that appears only when useful. It points to the next actionable target and hides when the dog is already close enough, so it should guide without becoming permanent screen noise. Each dog also has a tiny movement intent marker while running: Cheddar uses a red chaos arrow and Cocoa uses a teal veteran arrow. The intent marker is deliberately smaller than collars/labels and exists only to keep facing/movement readable at gameplay zoom. `FacingIntentLabel` exposes the deterministic left/right direction for tests.

Movement now has a first feel pass instead of instant velocity snaps. Cheddar is a little faster and more impulsive; Cocoa is slightly steadier, brakes harder, and cuts more cleanly. Both dogs accelerate, decelerate, snap tiny drift to a stop, lean into motion, squash/stretch subtly while running, and leave short-lived accent-color paw trail placeholders. These are prototype readability marks, not final effects.

Current movement defaults in ArenaScene:

- Cheddar: base speed `6.2` prototype units (`7.44` Unity units/sec after conversion), acceleration `34`, deceleration `31`, turn response `46`, zoomies multiplier `1.85`.
- Cocoa: base speed `5.9` prototype units (`7.08` Unity units/sec after conversion), acceleration `29`, deceleration `39`, turn response `52`, zoomies multiplier `1.75`.
- Both: input deadzone `0.25`, stop snap `0.08` Unity units/sec, and run feedback threshold `0.22`.

Manual movement feel check: move each dog from rest, reverse direction, circle around a weenie, and release the stick/keys near the rope. Confirm the dogs feel quick and cute, Cheddar reads a little more chaotic, Cocoa reads more controlled, and neither dog slides past small targets in a frustrating way. The paw trails should appear only while moving and should not compete with objective arrows, labels, or score pops.

The shared camera remains a 2D orthographic backyard camera matching `docs/ART-DIRECTION.md`. It frames dog bounds using horizontal/vertical margins and ranges from local scrolling exploration to a strategic full-yard co-op view.

Current camera defaults in ArenaScene:

- Initial orthographic size `8.0`.
- Min/max orthographic size `7.5 / 34`.
- Horizontal/vertical framing margin `5.0 / 4.0`.
- Follow/zoom lerp `9 / 7`.
- Bounds clamping is on so local exploration never reveals void outside the fence.

Manual camera check: play Backyard Rescue, Snack Heist, and Sock Panic from mission select. Confirm the dogs, nearest collectibles, squirrel pressure, rope, predator warning, score pop area, and end card are readable without losing the 2.5D/isometric-ish backyard feel. The HUD can occupy the top of the screen, but it should not hide the active dog work or mission props.

The HUD objective label is derived from game state, not hand-authored timing. Current objective states include **Save weenies X/6**, **Bark to scare squirrel**, **Huddle + bark at the shadow**, **Rescue Cheddar/Cocoa**, **Both dogs tug the rope**, **Clear the yard together**, and the clear/fail replay states.

Current arrow priorities are:

1. **BARK RESCUE / PARTNER BARK** during predator grabs.
2. **HUDDLE + BARK** during predator warning.
3. **BARK SQUIRREL** while the squirrel is actively stealing.
4. **BOTH TUG** once enough food has been recovered to make rope coordination the next likely bottleneck.
5. **WEENIE** for nearest breakfast recovery during normal play.

Manual first-20-seconds check: start the scene with two players and do nothing for three seconds. Confirm no squirrel steal happens yet, the intro banner is visible, the HUD objective says to save weenies, both dog identity labels are readable, and the arrows are primarily helping players find breakfast/weenies. Move each dog left and right and confirm the small intent marker appears only while running and does not compete with the identity label. By roughly 6-8 seconds the first squirrel pressure may begin depending on modifier, and the predator should not compete until the later warning.

## Squirrel pressure

A visible, labeled **Squirrel** periodically picks a breakfast/weenie and runs to steal it. The placeholder now has a tail/nose/eye silhouette so it reads as a small thief before labels are considered. If it reaches the item, the squirrel escapes with food, the team loses score, and the stolen-food counter rises. A single nearby bark interrupts/scares the squirrel briefly; a united bark scares it longer and adds teamwork score.

Current squirrel labels/cues:

- **SQUIRREL STEALING - BARK!** when it has picked a target.
- **SQUIRREL DROPPED IT!** after a successful solo bark scare.
- **SQUIRREL HID FROM DOUBLE WOOF** after a united bark scare.
- **SQUIRREL GOT A WEENIE!** when players miss the steal.

Manual readability check: let the squirrel start stealing once, then bark near it with one dog and confirm the HUD objective changes to **Bark to scare squirrel**, the label/cue clearly changes from stealing to dropped/scared, and a small **DROP!** world pop appears. Let it steal once on a later run and confirm the label says it got a weenie, the score shows a signed negative pop, a **MISS! -WEENIE** world pop appears near the squirrel, the stolen counter rises, and the fail state is distinct if stolen food reaches `3`.

When the squirrel is actively stealing, a temporary **BARK RANGE** ring appears around it. The ring uses the same placeholder ring language as bark feedback and hides outside the actionable state.

## Predator scare

Once per round, a **Predator Warning** telegraphs danger and targets one dog. The placeholder is a dark/red wing-and-eye shadow, not a normal arena object. If both dogs are close together and bark within the united-bark timing window, the predator is driven away for a large score reward. If the team fails the warning, **Predator Attack** grabs/stuns the target dog. The other dog can rescue by coming close and barking; failure costs score/time pressure but does not instantly end the game.

Manual readability check: when the warning starts, the HUD objective says **Huddle + bark at the shadow**, the predator label becomes **HUDDLE + BARK!**, and both dogs' arrows should point toward each other with **HUDDLE + BARK** if they are separated. A successful huddle bark should show **DOUBLE WOOF drove the predator away!**, create a **DOUBLE WOOF!** success pop, and move the predator to **PREDATOR YEETED** offscreen. During an attack, the grabbed dog should read **STUNNED**, the HUD objective should say **Rescue Cheddar/Cocoa**, the partner arrow should say **BARK RESCUE**, and a successful partner bark should show a distinct rescue cue plus **RESCUED!** world pop. The rescue cue is deliberately separate from united bark feedback.

During a predator grab, the grabbed dog shows a temporary **RESCUE BARK** range ring. This is meant to explain rescue distance without adding permanent clutter.

## Rope/Tug shared-object mechanic

The labeled, pulsing **Rope/Tug** object is a required co-op objective. Its placeholder is a horizontal yellow/brown striped tug rope with visible ends so it is not confused with food. Either dog can interact near the rope for progress, but the main completion path is both dogs standing together at the rope to charge the tug meter. Finishing tug awards a major score bonus and is required for LevelClear.

Manual readability check: after early food recovery, the HUD objective and objective arrows should switch to **BOTH TUG** while dogs are away from the rope. If only one dog reaches the rope, the rope label should call out **WAITING FOR CHEDDAR** or **WAITING FOR COCOA** and the HUD should say both dogs must commit together. When both dogs stand on the rope, their labels briefly read **TUG!**, the rope label changes to **BOTH TUGGING X%**, and completion flips the rope label to **ROPE COMPLETE!** with a **TUG POP!** world pop.

After enough food recovery makes tug the next likely bottleneck, the rope shows a temporary **BOTH DOGS** range ring. It uses the current `1.6` tug-together radius and hides on non-tug missions.

## United bark

Bark remains visible through expanding bark rings and a short comic **BARK!** burst, but now affects gameplay:

- scares or interrupts the squirrel;
- resolves the predator warning/attack when both dogs are close and timed;
- rescues a grabbed/stunned dog when the partner is close;
- awards teamwork score with a cooldown so it cannot be spammed every frame.

Manual readability check: each bark should pop the dog into a **WOOF!** pose label and show both the expanding bark ring and comic bark burst. A solo bark away from targets gives a joking solo-bark cue and sets `LastJuiceFeedback` to `BarkBurst`. A successful united bark gives a **DOUBLE WOOF** cue and is protected for a short moment so the second same-moment bark does not visually downgrade it back to solo feedback. During predator warning, the united bark should drive the predator away immediately.

## Scoring, ranks, and replay

The score model is deliberately readable and arcade-simple. Score changes appear as short HUD labels and world pops like **+100 UNITED BARK** or **-50 SQUIRREL GOT ONE**:

- breakfast/weenie recovery: **+50 WEENIE SAVED**;
- single-dog squirrel scare: **+25 SQUIRREL SCARED**;
- united bark teamwork: **+100 UNITED BARK**;
- predator defended: **+300 PREDATOR YEETED**;
- rescue after failed predator attack: **+250 PARTNER RESCUE**;
- tug objective: **+200 TUG COMPLETE**;
- LevelClear: **+500 LEVEL CLEAR** plus `5 x remaining seconds`;
- squirrel steal: **-50 SQUIRREL GOT ONE** or **-80 SQUIRREL GOT ONE** during Pancake Panic;
- predator hit after missed warning: **-150 PREDATOR HIT**;
- GameOver: **-100 GAME OVER**.

Ranks are deterministic and intentionally funny. Thresholds are mission-specific so shorter compact missions can still award sensible stars:

- **Backyard Rescue**: Pawfect Yard `1500+`, Backyard Heroes `1050+`, Snack Survivors `350+`.
- **Snack Heist**: Pawfect Yard `950+`, Backyard Heroes `700+`, Snack Survivors `250+`.
- **Sock Panic**: Pawfect Yard `800+`, Backyard Heroes `600+`, Snack Survivors `200+`.
- **Needs More Bark** — any score below the mission's survivor threshold.

LevelClear displays a 1-3 star rating based on final score, the center banner reads **BACKYARD SAVED! [rank]**, and both dogs hold a **PROUD!** pose. GameOver displays **MISSION FAILED! [rank]**, applies the game-over penalty, and both dogs hold a **SAD FLOP** pose. The end card includes `Outcome: Score - Rank`, one short funny `EndReasonLabel`, the last score swing, stars, session totals, and Replay / Next Mission / Mission Select actions.

Manual score/replay readability check: during both clear and fail, confirm the final dog poses are still visible behind/around the end card, the score swing label remains signed and cause-first, the funny rank and reason line are readable, and Replay / Next Mission / Mission Select are all usable without a mouse.

## Non-developer playtest script

Use this when handing the prototype to someone who has not seen the code:

1. Start `ArenaScene` and say nothing. Confirm the player can identify that the first screen is mission select and can start a mission using keyboard or controller only.
2. Ask them to read the controls line and mission briefing out loud. Confirm they can answer: how do I move, bark, tug/rescue, replay, and go next?
3. Have them play **Backyard Rescue** until either clear or fail. Confirm they understand the current mission name, objective label, dog identities, score pop, and end choices.
4. On the end screen, ask them to choose **Next Mission** without using a mouse. Confirm the next unfinished mission starts and shows its own briefing.
5. Have them finish **Snack Heist** and **Sock Panic** by any outcome. Confirm the session totals increment after each ended mission.
6. After all three missions have ended, choose **Next / Session Summary** and confirm missions played, total score, stars, and ranks are readable.
7. Return to mission select and replay any mission. Confirm session totals are not persistent across stopping Play mode.

## Two-player playtest script

Use this short script before starting a new level:

1. Start `ArenaScene` with two players and read the opening at gameplay zoom. Confirm both players can answer: who am I, where is my first weenie, what is the shared mission?
2. Before moving, confirm Cheddar and Cocoa are distinct without reading only text: long low bodies, Cheddar's golden/red chaos read, Cocoa's chocolate/teal spot-queen read, visible snouts, ears, tiny feet, and collars.
3. Move both dogs and bark with each. Confirm Cheddar's run reads like **CHAOS ZOOM**, Cocoa's like **SPOT PATROL**, tiny facing/intent arrows appear only during movement, and both bark poses still pop without covering objective arrows.
4. Let the first squirrel steal attempt start. Have one player bark near it; confirm the tail/nose/eye squirrel silhouette, HUD objective, steal/scare labels, and drop/miss pops make cause and effect obvious.
5. Recover at least three weenies. Confirm the weenie bun/mustard marker reads as food and arrows switch toward **BOTH TUG** without feeling noisy once players are close to targets.
6. Send only one dog to the rope. Confirm the striped rope object and waiting-for-partner label make the required cooperation obvious.
7. Send both dogs to the rope. Confirm both dog pose labels and rope progress communicate a shared tug.
8. On the predator warning, first try the correct huddle + bark. Restart and then intentionally fail the warning once to see the grab/rescue path. Confirm the predator reads as a red/dark wing-and-eye threat, the grabbed dog reads **STUNNED**, and the partner rescue arrow is more important than decorative motion.
9. Watch score event labels and world pops during each major action: weenie, squirrel scare/steal, united bark, predator hit/defense, rescue, tug, clear, and fail.
10. Finish a clear run and a failed run. Confirm the clear/fail banners, proud/sad dog poses, final score, funny rank, one-line reason, and replay instructions are impossible to miss.
11. Press **R**, **Enter**, gamepad **Start**, gamepad **South**, or the **Replay** button. Confirm the run resets to score `0`, `Outcome: InProgress`, no replay prompt, and the intro banner returns.

## Round modifiers

Each restart deterministically selects one seeded modifier for tests/HUD:

- **Squirrel Trouble** — squirrel acts faster.
- **Zoomies Surge** — periodic dog speed bursts make control livelier.
- **Pancake Panic** — stolen food hurts more, representing faster pressure buildup.

## Tuning guide

Mission, camera, and interaction-range tuning is centralized in `unity/CheddarAndCocoa/Assets/Scripts/Game/ArenaMissionTuning.cs`. Per-dog movement feel fields live on `unity/CheddarAndCocoa/Assets/Scripts/Data/DogTuning.cs` and the current ArenaScene runtime values are assigned in `ArenaBootstrap` until authored dog tuning assets exist. Adjust those files first for playtest balancing; avoid scattering one-off numbers in `GameManager`.

Current key defaults:

- Timers: Backyard Rescue `90s`, Snack Heist `70s`, Sock Panic `55s`.
- Rewards: item scores `50 / 60 / 40`, united bark `+100`, predator defended `+300`, partner rescue `+250`, tug complete `+200`, clear bonus `+500`, time bonus `5 x remaining seconds`.
- Penalties: Backyard squirrel `-50`, Snack squirrel `-90`, Pancake Panic squirrel `-80`, predator hit `-150`, game over `-100`.
- Squirrel pressure: first steal delay `9.0s` (`7.0s` on Squirrel Trouble), repeat delay `3.4s` (`2.2s` on Squirrel Trouble), move speed `1.9`.
- Bark/rescue/tug: united bark window `0.8s`, united bark range `3.0`, single bark squirrel range `4.0`, rescue bark range `2.0`, tug together distance `1.6`, tug charge `0.5` per second, interact tug bump `0.2`.
- Camera: initial ortho `8`, min/max ortho `7.5 / 34`, horizontal/vertical margins `5 / 4`, follow/zoom lerp `9 / 7`.
- Arena dog movement: Cheddar base speed `6.2`, acceleration `34`, deceleration `31`, turn response `46`, zoomies `1.85`; Cocoa base speed `5.9`, acceleration `29`, deceleration `39`, turn response `52`, zoomies `1.75`; both use input deadzone `0.25`, stop snap `0.08`, and run-feedback threshold `0.22`.
- Range hints: squirrel bark ring `4.0`, rescue bark ring `2.0`, tug together ring `1.6`.
- Mission counts: Backyard Rescue spawns `5` items and needs `6` recoveries because collected items respawn; Snack Heist spawns/needs `4`; Sock Panic spawns/needs `5`.

After tuning, run:

```sh
./unity/run-playmode-tests.sh
```

Expected pass signal:

- the shell script exits `0`;
- the Unity log includes a test summary with `failed=0`;
- `unity/playmode-results.xml` is written.

Expected failure signal:

- the shell script exits non-zero;
- the Unity log or XML names the failing PlayMode test;
- if the editor cannot start because of licensing, open Unity Hub, sign in once, and re-run the command.

Known non-fatal Unity log noise:

- Unity may print package import, domain reload, asset refresh, or graphics-device messages during batch mode.
- Generated audio clips, IMGUI overlays, placeholder sprites, and PlayMode event-log messages are expected prototype output.
- The important failure signals are compiler errors, Safe Mode, test failures, missing `ArenaScene`, missing `ArenaBootstrap`, or a failed build result.

The PlayMode tests assert tuning defaults, movement feel defaults, independent dog movement, camera config, interaction range indicator state, the 30-90 second mission timing target, reachable top-rank scoring, cold-start mission select, all three mission ids, replay/next/select/session-summary reachability, distinct Cheddar/Cocoa identity tuning and art slots, overlay state, and deterministic playtest log entries.

## Build/share readiness

For a local development build, use Unity 6 LTS and run:

```sh
./unity/build-dev.sh
```

Output location: `unity/builds/dev/CheddarAndCocoa-Arena.app`.

For the full local demo validation pass, run:

```sh
./unity/validate-demo.sh
```

That script confirms the Unity project path exists, `ArenaScene` exists and is listed in `EditorBuildSettings.asset`, `ArenaScene` contains the `ArenaBootstrap` GameObject and script reference, PlayMode tests pass, and the local development build can be created. Use `--skip-build` only when the machine is intentionally test-only.

No installer, signing, notarization, icon, store packaging, external analytics, or distribution polish is expected for this slice.

Demo readiness checklist:

- `./unity/run-playmode-tests.sh` passes with `failed=0`.
- `./unity/build-dev.sh` creates `unity/builds/dev/CheddarAndCocoa-Arena.app`.
- Cold start opens mission select, not a live round.
- Backyard Rescue, Snack Heist, and Sock Panic start from mission select.
- Replay, Next Mission, Mission Select, and Session Summary are reachable without a mouse.
- Cheddar and Cocoa spawn with distinct identity/readability slots and asymmetric tuning.
- The shared camera initializes and keeps both dogs framed.
- Playtest Mode can be toggled and does not block normal mission flow.
- Audio and rumble can be toggled with **F2** / **F3**, and major events still request their named placeholder feedback cues when enabled.
- Keyboard and gamepad controls match the Controls table above.
- Known limitations below are acceptable for this demo handoff.

## Five-minute playtest instructions

Start from `ArenaScene`, press Play, and hand controls to two players without explaining the code. Ask them to play Backyard Rescue, then use **Next Mission** to reach Snack Heist and Sock Panic, and finally open Session Summary after all three have ended. Turn on the **F1** overlay only when the observer needs to inspect state; leave it off for first-read usability.

Observe whether players can identify their dog, start a mission, read the current objective, recover from squirrel/predator/tug pressure, understand clear/fail, replay, and use Next Mission without a mouse. Watch especially for moments where score pops or objective arrows are missed during chaos.

Suggested feedback questions:

- Which dog were you, and how could you tell without reading only the name label?
- What did bark do in each mission?
- When did you know what to do next?
- Did the squirrel/predator/timer feel fair, too slow, or too punishing?
- Did Replay, Next Mission, and Session Summary make sense?
- What was funny or personal, and what felt like generic collecting?

## Visual readability checklist

Use this after any placeholder-art, authored-art, or sprite import change:

- Cheddar and Cocoa still read as different long, low dachshunds at gameplay zoom without relying
  only on text labels.
- Cheddar keeps golden/red chaos reads, including the current generated chaos tuft/flash/bolt; Cocoa keeps chocolate/teal spot-queen reads, including the current generated cream chest, extra spot, and larger crown marker.
- Dog pose states are still distinct: idle, run, bark, tug, stunned, rescued, proud, sad.
- Bark ring/text, objective arrows, score pops, and playtest overlay remain readable but do not hide
  the dogs or core mission objects.
- Movement lean, squash/stretch, and paw trails are readable during motion but do not become constant noise.
- Camera framing keeps all three current missions readable from mission select without changing the orthographic backyard direction.
- Temporary bark, rescue, and tug range rings appear only in actionable states and hide on mission select, end screens, and non-relevant missions.
- Weenie, snack, and sock collectibles are visually distinct from each other and from the squirrel
  and rope.
- Squirrel, predator, and rope expose their expected replacement slots from
  `ArenaArtCatalog` and keep their current role reads: thief, threat, shared tug prop.
- Predator labels now explicitly say **SHADOW! HUDDLE + DOUBLE BARK!** during warning,
  **YOINKED [DOG] - PARTNER BARK!** during rescue pressure, and **DOUBLE WOOF YEETED SHADOW**
  after success. Rope labels now call out **ROPE NEEDS BOTH DOGS**, **BOTH DOGS TUGGING X%**, and
  **ROPE COMPLETE! TEAM CHOMP!** so a silent observer can verify huddle, rescue, and tug intent
  without opening the inspector.
- Mission select, end actions, and session summary remain legible with placeholder IMGUI styling.

## Known limitations

- Mission actors still use generated gameplay silhouettes/text labels as the primary read, with
  imported DRAFT badges layered in for first-pass character/art reference. The imported sheets are
  not final transparent gameplay sprites.
- Mission select, end actions, and session summary are generated IMGUI placeholders with a small
  imported UI-kit accent, not authored production UI.
- The playtest overlay and event log are debug/playtest aids only. They are not analytics, persistence, telemetry, or player-facing production UI.
- Snack Heist remains an architecture proof inside the existing arena. Sock Panic now has a complete first-pass co-op lock/key beat, but still uses generated basket/sock placeholder art.
- Dog identity art, pose labels, collars, expression markers, prop silhouettes, and objective arrows are generated placeholders. They are intentionally readable and easy to delete once authored sprites/animation exist.
- The squirrel and predator use intentionally simple movement/state rules so the PlayMode tests remain deterministic.
- The intro, bark, squirrel, predator, tug, clear, fail, score-pop, and objective feedback are still text/scale/audio-placeholder driven; they are designed to be replaced by authored animation/SFX later.
- Audio cues are generated placeholder clips from named slots, and rumble is a simple best-effort gamepad pulse. Neither is mixed, balanced, platform-tuned, or authored final feedback.
- `ForceSquirrelStealAttempt()` exists as a deterministic PlayMode test hook and is not intended as a player-facing control.
- Scoring is intentionally flat and session-local only. There is no save file, leaderboard, unlock economy, or persistent progression yet.
- The end rank is based only on final score and clear/fail state; it does not yet account for style, dog-specific contributions, or advanced co-op medals.
- Tug is proximity/progress based, not a full physics rope.
- Predator targeting and modifier selection are seeded but still prototype-simple.
- The scene now has basic procedural sound cues, an arena `AudioListener`, and simple placeholder animation, but real prefab art, authored animation, better SFX, and richer rescue/tug feel are still future work.
- Camera feel is tuned for the current generated backyard arena only. A larger authored level may need level-specific camera anchors or bounds.
- Paw trails and range rings are placeholder geometry/text. They are useful for playtest readability, not a final VFX style.

## Test coverage

`unity/Assets/Tests/PlayMode/ArenaGameLoopPlayModeTests.cs` loads ArenaScene and verifies mission select initialization, centralized tuning defaults, movement/camera tuning defaults, mission balance invariants, starting each mission through the new flow, end-screen Replay / Next Mission / Mission Select availability, session totals across multiple missions, session summary after all three variants have ended, dogs, independent movement response, camera component/config, interaction range indicator state, mission state, intro prompt/banner, deterministic objective labels, delayed first squirrel steal window, initial score state, item recovery scoring, HUD score-pop state, world score/miss/success pops, playtest overlay state, playtest event log entries, squirrel steal/scare labels and score events, solo/united bark feedback and scoring, bark burst state, predator defense scoring, failed predator hit and rescue scoring, tug waiting/together feedback and scoring, LevelClear score/rank/summary/reason, GameOver score/rank/summary/reason, replay prompt visibility, restart reset state, exposed modifier state, dog identity labels, dog pose labels, movement intent labels/arrows, objective-arrow labels, generated arena audio listener, required replaceable audio cue slots, expected major-event audio cue requests, expected rumble request names, and audio/rumble suppression toggles.

The same test file also verifies **Snack Heist** and **Sock Panic** can initialize, update objective labels, score unique mission events, reach clear/fail outcomes, and expose replay state. `ControllerCoopPlayModeTests` still verifies the baseline two-pad movement/bark proof and now clears scene objects before constructing its own bootstrap so it does not accidentally inspect leftover ArenaScene dogs from previous tests.


## Backyard Pack: Squirrel Conspiracy (2026-06-18)

- Added `GameManager.MissionVariant.SquirrelConspiracy` / **The Great Backyard Squirrel Conspiracy** to the Arena mission select and mission order.
- The mission uses `HerdingMissionState` for deterministic route progress, herd/cutoff counts, fake-outs, taunts, stash reveal, and stash found clear state.
- Four deterministic route nodes now pair with four generated cutoff-zone markers. Only the current route's zone is visible, objective arrows split the dogs into herd/cutoff roles, and the markers reset to route one on replay.
- Gameplay loop: dogs bark near the squirrel to score `GOOD HERD`, split positioning to score `CUTOFF`, lose points on early/far `FAKE OUT`, reveal the stash after four controls with `DOUBLE BARK BLOCK`, then interact with the revealed stash for `STASH FOUND` and `CONSPIRACY CRACKED`.
- Fail path: repeated squirrel taunts or timer expiry fails the mission; replay resets route, stash, score, and outcome state.
- Production helpers are now part of the live path where relevant: `MissionRankCalculator`, `ScoreEventCatalog`, `MissionRuntimeSnapshot`, `MissionSeedGenerator`, and `MissionOutcomeSummaryBuilder`.
- `DemoReadinessGate` is surfaced in the F1 playtest overlay so the packaged backyard acceptance contract is visible during diagnostics.
