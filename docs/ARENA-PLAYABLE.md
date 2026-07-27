# Arena Playable — Mission Variety Spike

> **Status: ACTIVE OPERATIONAL REFERENCE.** Use this for controls, current slice behavior, and the
> baseline couch-playtest protocol. It does not authorize mission expansion or direct
> `GameManager` branches.

`unity/CheddarAndCocoa/Assets/Scenes/ArenaScene.unity` is now a co-op proving ground instead of a
flat treat loop. As of 2026-07-15, the arena exposes 23 mission variants. A cold start opens a
generated in-scene mission select; every mission runs through the controller boundary. The library
leads with the five most developed slices: **Operation Pee Break**, **Kitchen Falling Food
Frenzy**, **Car Ride Chaos**, **Baby Bird Bedlam**, and **Gate Crash**. This quality order is
presentation-only; deterministic mission modifiers use mission identity rather than selector
position, so reordering does not retune the levels.

For current global character art direction, read `docs/ART-DIRECTION.md`. Backyard Mission is the
playable proof of that direction, not the only place the direction applies. For future external
sprite/audio collection before Unity import, use `docs/ASSET-CATALOG.md`.

### Couch test 2026-07-24 fixes (CF3.1)

A Word-doc couch report from a 10am session flagged issues across the main menu, controls, live
Pee Break play, Kitchen Falling Food Frenzy, and Car Ride Chaos. Fixed in one pass, plus the
"readable chaos" themes that showed up more than once (unexplained icons, text too small, debug
counters leaking into player-facing copy, redundant buttons):

- **Mission select screen** (`MissionSelectScreen.cs`): removed the "Cheddar + Cocoa Adventures" /
  "START HERE..." title banner and the "Pick an adventure, grab two controllers..." zero-state
  greeting (both flagged "Remove this"). Recolored the "Start Recommended Adventure" pill from a
  low-contrast tan/gold fill to the same dark-teal + gold-outline language the rest of the HUD skin
  already uses (`tools/art/generate_hud_skin_pack.py`). Dropped `TileArtworkZoom` from 1.35x to
  1.15x so tile art stops over-cropping characters off the right edge (kept above the ~1.09x floor
  that still crops the baked title ribbon). Removed the "RECOMMENDED • " prefix under tiles - the
  accent color and the detail panel's own RECOMMENDED tag already carry that. Collapsed the
  competing "Play Recommended" (jumps away to Pee Break) / "Start {Mission}" buttons into one
  full-width primary action per screen; the Y-button shortcut still reaches the recommended
  adventure from anywhere. Investigated the reported blank "YOUR TEAM PLAN" for Pee Break - the
  chip-row content and its PlayMode coverage both show it populated; not reproduced from source, so
  left as-is pending a real repro.
- **Controls** (`GamepadPlayerInput.cs`, `ArenaHud.cs`): P2 (Cocoa) jump/wrestle were bound to
  Right-Ctrl/Right-Alt, which many MacBook keyboards don't have (or intercept). Remapped to
  Slash/Quote - present on every keyboard, sit right next to the arrow keys P2 already moves with,
  and aren't OS modifier keys. Fixed the controller-diagram legend where "INTERACT" (the longest
  action word) overlapped/clipped the neighboring buttons' labels: wider button spacing plus a
  dedicated smaller non-wrapping label style (`_padLabel`, 13pt) instead of reusing 16pt `_small`.
- **Operation Pee Break live play** (`PeeBreakMissionController.cs`): every world-label marker
  (DOOR, LEASH, CHARGER, MISREAD, etc.) was rendering at fontSize 9 - far smaller than every other
  mission's 16-24pt labels - matching "can't read the text on it." Bumped to 16pt. Un-skewed the
  misread accent ring (was -28deg, read as a stray, unexplained shape - "this skewed white halo...
  I have no idea what it's for"). Removed the raw `MISREADS {n}` counter from the top HUD
  objective line (internal recovery counter with no cap/explanation leaking into player text);
  `Misreads` still drives scoring/outcome, it just no longer prints itself.
- **Kitchen Falling Food Frenzy** (`GameManager.cs`, `KitchenFoodFrenzyMissionController.cs`): found
  the actual bug behind "no food falls despite both dogs barking - not sure how to start."
  `KitchenFoodFrenzyMissionController.HandleBark` already sets a specific, actionable cue on a
  failed bark ("Cheddar must reach the COUNTER..."), but `GameManager.OnDogBarked`'s generic
  "solo WOOF" fallback overwrote it the same frame whenever the dogs aren't huddled - which in
  Kitchen they never are, by design (Cheddar owns the counter, Cocoa guards the bowl 13+ units
  away). `OnDogBarked` now tracks whether the controller already changed `LastCue` during
  `HandleBark` and skips the generic fallback when it did. Separately, added a Kitchen-scoped ledge
  so dogs can't just walk onto the counter/cupboard: `KitchenFoodFrenzyMissionController.Tick` now
  pushes a grounded dog back to the counter's edge unless it's currently jumping; jumping across the
  edge "mounts" the counter (matching the existing requirement that Cheddar stand right there to
  bark) and free movement resumes until the dog leaves the zone. Covered by
  `KitchenFrenzy_DogsAreGroundLockedAtTheCounterUnlessJumping`.
- **Car Ride Chaos** (`CarRideMissionController.cs`, `DogReadabilityFeedback.cs`): the brake-brace
  top HUD line read identically whether or not Cocoa had already planted, so Cheddar's player had
  no on-screen sign it was their turn ("Not clear to me how cocoa 'Plants' so cheddar can duck
  behind?"). `ObjectiveLabel` now switches to "Cocoa's PLANTED - Cheddar, get beside her and
  Interact to tuck in!" the moment she braces. Covered by
  `CarRide_BrakeEvent_ObjectiveLabelUpdatesOnceCocoaPlants`. Nearly doubled the jump arc's visual
  rise (0.4 -> 0.75 units) so jumping reads as a clear hop instead of "almost no animation." Not
  fixed this pass: the reported whitespace under Cheddar/Cocoa's sprites is a uniform ~24px
  transparent export margin baked into the source PNGs (already pivot-compensated for correct
  foot placement) - a source-art re-trim, not a code bug; flagged for the next art pass.

Manual acceptance: open the mission picker cold and confirm no title banner/greeting line remain,
tiles show more of the art without a right-edge crop, and only one Start button shows per mission.
Plug in a keyboard-only P2 and confirm Slash jumps / Quote wrestles with no controller attached.
Play Operation Pee Break and confirm every world label is legible from a normal couch distance and
the top HUD line never shows a raw misread count. Play Kitchen Falling Food Frenzy: bark once from
off the counter and confirm a specific on-screen reason appears (not a generic "solo WOOF"); try
walking straight onto the counter and confirm it blocks until you jump. Play Car Ride Chaos through
a brake event and confirm the top line changes the instant Cocoa braces, and that jumps over the
sliding junk read as a clear hop.

### Couch test 2026-07-24 10am fixes (CF3.2)

The 10am couch-test Word doc flagged three more issues, all in Bone Detail and Skunk Blast Mayhem
(the CF3.1 pass above covered the rest of that same report - mission select, controls, Pee Break,
Kitchen, Car Ride):

- **Skunk Blast Mayhem renders an eagle, not a skunk** ("no idea what is happening - we need an
  animated skunk not an eagle"). Root cause: this mission reuses the shared `PredatorObject`
  (`SkunkBlastMayhemMissionController._skunkObj = context.PredatorObject`), and
  `GameManager.AddThreatAnimator` hardcodes that object's `ThreatReadabilityAnimator` default actor
  to `Eagle` - there was no `Skunk` case in `ThreatMotionArt.Actor` at all, so every one of this
  mission's actor-state labels fell through to the Eagle default and played its banking/sweep
  frames. Added `Actor.Skunk`, keyword detection in `TryInfer` (`SKUNK` or `TAIL UP` - the tail-lift
  danger telegraph doesn't say the word "skunk"), frame rates, and three procedural poses (calm
  guard sway, building tail-lift shiver, hunched retreat) in `ThreatMotionPose`. Generated a
  placeholder 512x384 flat-vector skunk motion pack (`tools/art/generate_skunk_motion_pack.py`,
  12 frames across Patrol/Threaten/Retreat) matching the existing mission-prop flat-icon style -
  **not** the painted Eagle/Coyote/Squirrel character tier, which was produced through the
  project's separate AI image-generation pipeline (`docs/CHARACTER-MOTION-GENERATION.md`) this
  script doesn't attempt to replicate. Promoting the skunk to that same painted fidelity is a
  follow-up. Covered by `ThreatLabelInferencePlayModeTests.SkunkLabels_*`,
  `ThreatMotionFidelityPlayModeTests.SkunkPoses_*`, and
  `CoopSkunkBlastMayhemPlayModeTests.SkunkBlast_PredatorObject_RendersAsSkunkNotEagle`.
- **Bone Detail's dig button did nothing** ("Interact does nothing - it should dig here"). Root
  cause: `BoneRelayMissionController` dug mounds automatically on proximity (`Tick()` compared
  distance every frame) while its world label reads "DIG?" - a button-prompt shape with no button
  behind it. Converted digging to an explicit `IMissionInteractionController.HandleInteract`
  action (Cheddar Interacts within range of the called mound), matching the same
  Interact-to-act pattern Gate Crash and Skunk Blast Mayhem already use, instead of adding a
  redundant auto-trigger under an affordance that promises a button. Covered by the updated
  `CoopBoneRelayPlayModeTests.Bone_PositionDriven_ReadAtPostThenDigTheCall` (now presses Interact
  instead of relying on proximity) and the rest of that suite, unchanged.
- **World labels are tiny with a weak background plate** ("the location is a grey square with TINY
  words, totally wrong way to signal to users... fix this as a pattern throughout the game"),
  called out on both the Bone Detail dig markers and its scent post. This is the single shared
  `GameManager.AddWorldLabel`/`WorldLabelSkin` call path every mission's identity markers go
  through (DIG?, GATE, TOY, CHECKPOINT, SCENT POST, LEVER, and the rest), not a one-off. Scaled the
  label transform 35% (0.08 -> 0.108) and proportionally scaled `WorldLabelSkin.ResizeForText`'s
  background-plate constants by the same factor, so the plate grows in lockstep with the now-bigger
  text instead of the text overflowing a plate sized for the old scale. This is a first pass on
  size/legibility only; the deeper "strong indicator animations or look/feel" ask (background
  contrast, iconography beyond the existing paw/command skins) is a separate follow-up.
  `ActorSignalBadge`'s per-target pulsing icon (already used for "this is the active objective
  right now") is unaffected and unchanged.
- **Gate Crash's gate wasn't visible in the couch-test screenshot** and one other "weak set assets"
  screenshot could not be matched to any current Bone Detail sprite. Not fixed this pass: the gate
  art (`gate_crash_gate_closed.png`) loads correctly and is covered by a passing
  `FinalArtIntegrationPlayModeTests` assertion, so this doesn't reproduce from source - needs a
  live re-check on a fresh build before diagnosing further. **Both root-caused and fixed in CF3.3
  below** - the couch-test doc's own screenshots turned out to hold the missing repro evidence.

Manual acceptance: start Skunk Blast Mayhem and confirm the guarded bird's skunk renders as a
black-and-white skunk (calm sway, then a rising tail during the danger telegraph, then a hunched
retreat once the bird is grabbed) - never a banking eagle. Start Bone Relay/The Bone Detail, have
Cocoa bark the scent call, walk Cheddar onto the called mound, and confirm walking alone does
nothing but pressing Interact digs it. Walk up to any world-label marker (DIG?, GATE, CHECKPOINT,
etc.) and confirm the text is legibly bigger than before at a normal couch distance.

### Gate Crash's invisible gate + Bone Relay's stray sandbox (CF3.3, 2026-07-25)

CF3.2 above left two couch-test items unresolved because static source reading alone didn't
reproduce them. Re-opening the two "Couch test July24 10am" Word docs and extracting their
embedded screenshots (rather than just their text) supplied the missing repro evidence for both:

- **Gate Crash's gate never rendered - only its `GATE` label and paw-progress meter did.**
  `GateCrashMissionController.BuildScene()` gives the gate marker a deliberately non-uniform scale,
  `(1.4, 4, 1)`, to get a tall doorway-shaped gameplay hitbox - the same trick
  `TableStealthMissionController` uses for its human. But Gate Crash then attached the real art with
  plain `MissionPropArt.AttachObject`, which applies a uniform local scale that gets multiplied by
  whatever non-uniform scale the parent marker has. The result: the promoted gate sprite rendered at
  roughly 0.036 x 0.104 world units - a sub-pixel speck no couch-test player could ever see, while
  the label/meter (which don't inherit the marker's art scale) rendered normally, making it look like
  "the gate is just missing." `TableStealthMissionController` had already solved this exact problem
  with `MissionPropArt.AttachObjectAtWorldWidth`, which cancels the marker's non-uniform scale and
  renders the overlay at an authored world width instead - Gate Crash just never got that fix.
  Switched both the gate and its toy to `AttachObjectAtWorldWidth` (3.8 and 1.8 world units). Added
  `AssertWorldWidthAndProportions("GateCrashGate", ...)` /
  `AssertWorldWidthAndProportions("GateCrashToy", ...)` to `FinalArtIntegrationPlayModeTests` so a
  regression here fails a test instead of only a screenshot.
- **A "weak set asset" screenshot near Bone Relay** (a tan crate with an oval, a red diagonal
  stripe, and a blue triangle) turned out not to be Bone Relay art at all - it's the "Sandbox" yard
  background decoration (`tools/art/generate_environment_prop_pack.py`'s `sandbox()`, exported as
  `yard_sandbox.png`), which renders unconditionally in every mission (unlike Snack/Laundry/
  threat-lane overlays, which are gated to their own mission). Its placeholder sat at world
  `(10.8, 11.6)` (`ArenaBootstrap.cs`), close enough to Bone Relay's NE mound `(12, 6)` and scent
  post `(0, 9)` that it landed in-frame during real Bone Relay play. Moved it further into the same
  NE yard corner, to `(19.2, 19)`, clear of Bone Relay's action zone; `BackyardEnvironmentPlayModeTests`
  doesn't assert an exact position so this didn't need a test change, and the existing
  `AssertEnvironmentOverlay("Sandbox", ...)` coverage still passes.

819/819 PlayMode tests green after both fixes (full suite, not just the touched files).

Manual acceptance: start Gate Crash and confirm an actual wooden gate/fence renders where the
`GATE` label points, both closed and held. Start Bone Relay/The Bone Detail and confirm no
unexplained tan crate/sandbox prop appears near the scent post or the NE mound.

### Control card waits for explicit accept (CF1.1, 2026-07-20)

The 2026-07-20 couch retest found the fixed-duration card (checklist #4/#5): "The control card
dismisses itself after a period of time - I need time to look at it and accept it."
`MissionBriefingVisible` is no longer time-based. The card now stays up - game fully frozen, the
same freeze the sniff beat already used - until either player presses Bark or Interact, or grabs
the first collectible ("accept"). Accepting drops the card and hands off into the existing timed
sniff-around beat unchanged (`LeadInSniffSeconds`, 2.5s), which still ends early on any further
deliberate dog verb exactly as before - accept never skips straight to live play by itself. The
card's own footer now reads **BARK OR INTERACT WHEN READY** instead of "...TO START EARLY" (there
is no longer a timer to beat). This supersedes the "Bark during the card" line of the
"Sniff-around lead-in pacing beat (2026-07-04)" acceptance check below and the "five-second
opening card" / "normal duration" card mentions in the Operation Pee Break entry just below this
one - all now describe the same uncapped accept-then-sniff hand-off, not a fixed 5s window.

`GameManager.LeadInSecondsOverride` (the test/dev seam) additionally auto-accepts the card whenever
its value is `<= 0`, mirroring how that same value already force-skips the opening presentation, so
the legacy deterministic PlayMode suite (which defaults the seam to 0) still reaches live play on
the same frame as before. A positive override (used by `LeadInPlayModeTests` to pin the freeze
contract itself) still requires a real accept input before the card drops.

Manual acceptance check: start any mission and wait at least 10-15 seconds doing nothing - the card
must still be up, the timer must not move, and the yard/props must stay visibly parked (pre-CF1.1
this auto-dismissed at 5s; it no longer does). Press Bark or Interact - the card drops and the
`SNIFF AROUND! GO IN n - BARK TO GO NOW` countdown begins with the game still frozen. A second
Bark/Interact during that countdown still fires `GO!` instantly and starts the timer, same as
before.

### Comprehension meter mirrored in screen-space HUD (CF1.2, 2026-07-20)

The 2026-07-20 couch retest found (checklist #8, FAIL): "The progress meter disappears when the
dogs move to bottom part of screen." The `TEENAGER GETS IT` comprehension meter was a child of the
Teenager sprite; the shared camera follows the dogs, so moving toward the room's lower half scrolled
the Teenager and his meter off-screen exactly when players most needed to see whether their signal
was landing.

`IMissionBeatProgressHud` (`IMissionController.cs`, modeled on the existing `IMissionPressureHud`)
lets a controller mirror a beat-progress readout into the compact top-bar HUD the same way Pee
Break's `BLADDER EMERGENCY` pressure meter already does. `PeeBreakMissionController` implements it:
label `TEENAGER GETS IT (BEAT n/4)`, value the same clamped `_puzzle.Comprehension / <that beat's
required comprehension>` ratio that already drives the world-anchored fill (`ArenaHud` and the
controller now share one property, `ProgressNormalized` - `UpdateTeenagerProgressRead()` reads it
instead of recomputing the ratio locally, so the two readouts cannot drift apart), hidden once the
door opens. The world-anchored track is unchanged and still renders when visible; this is a mirror,
not a replacement.

Pee Break is also the only mission where two top-bar meters are visible at once (bladder pressure
never turns off; beat progress stays visible for the whole mission until the door opens). Rather
than grow the shared 102px top bar - pinned by `ActionTutorialPlayModeTests`'s
`ProductionHud_KeepsObjectiveAndPlayerIdentityReadableWithoutCoveringThePlayfield` and the Visual
Readability Contract's Shared Player-Facing HUD Rule - `ArenaHud` renders both meters at a shorter,
wider compact size (`BuildStackedPressureMeterRect`/`BuildStackedProgressMeterRect`: 27px tall each
with a 3px gap, versus the normal 38px solo meter) that still fits entirely inside the unchanged top
bar. A single meter of either kind (every other mission on the current roster) keeps the original
full-size layout and renders pixel-identical to before this change; only Pee Break's simultaneous-
pair case uses the compact pair.

Manual acceptance check: start Operation Pee Break and move both dogs toward the bottom of the room
during any beat - the top bar must keep showing both **BLADDER EMERGENCY** and **TEENAGER GETS IT
(BEAT n/4)** stacked, non-overlapping, matching the world-anchored meter above the Teenager whenever
he is also on-screen. The beat-progress row must disappear once the door opens.

### Teenager reflects beat progression (CF1.4, 2026-07-20)

The 2026-07-20 couch retest found (checklist #8b): "Ideally after each phase, the teenager looks a
little different, or shifts positions to indicate we're on the 'next subproblem'."
`UpdateTeenPresentationState` already drove `TeenState` from moment-to-moment dog signals (misread,
confusion, partial combo, `DoorOpen`), but never read `_beatIndex`, so the Teenager's steady-state
look was identical in Beat 1 and Beat 3.

A new read-only `PeeBreakMissionController.BeatLook` enum (`PhoneAbsorbedIdle`,
`PhoneLoweredStillFeet`, `UprightBatteryPanic`, `HalfStandingDoorGlance` - one per beat, clamped)
drives a per-beat baseline layered **under** the existing `TeenState` deltas in the same per-part
render block (`_teenagerThumbs`/`_teenagerHead`/`_teenagerHoodie`/`_teenagerFootWiggle`) - both
still combine exactly as the task required, and `TeenState.StandingSuccess`'s existing 1.26x hoodie
stretch is untouched (it overrides the beat baseline instead of stacking with it):

- **Beat 1 (DoorStare):** unchanged from before this change.
- **Beat 2 (LeashMessage):** the phone sits visibly lower in his hands; the idle foot-tap stops.
- **Beat 3 (ChargerGambit):** he sits more upright, and his own hoodie/head posture visibly tenses
  as the phone battery actually drains - not only the existing phone-mounted
  `_phoneBatteryFill`/`_phoneChargeBolt`/`_phoneDeadSlash` props.
- **Beat 4 (UnitedBark):** half-standing (more upright again), periodically glancing toward the
  door between phone checks.

Completing a beat (`AdvanceBeat()`) also fires a one-shot transition flourish so the *moment* of
advancing reads, not just the new steady state: a `"NEXT!"` world pop at the Teenager plus a brief
timed window (`_beatTransitionOhBubbleUntil`, `BeatTransitionOhBubbleSeconds` = 0.6s) that flashes
the existing `"OH!"` bubble even outside its normal UnitedBark/dead-phone conditions. Both are
skipped on the final beat-4-completing-the-mission transition, which already has its own distinct
`RELIEF ZOOMIES!` pop/juice/rumble sequence - no redundant or clashing flourish fires on the actual
climax.

Manual acceptance check: start Operation Pee Break and step through all four beats. The Teenager's
phone should sit noticeably lower from Beat 2 on and his idle foot-tap should stop; entering Beat 3
he should sit up straighter and visibly tense as the phone dies; in Beat 4 he should sit half-up and
glance toward the door between phone checks. Each time a beat completes, watch for a brief "NEXT!"
pop by the Teenager and a flash of his "OH!" bubble even though nothing else on screen justifies
it yet - that flourish must NOT repeat at the final door-open climax, which keeps its existing
"RELIEF ZOOMIES!" payoff untouched.

### Door stare anchors to the floor (CF1.6, 2026-07-20)

The 2026-07-20 couch retest found (checklist #6, Unclear): "It's weird that the door stare has to
happen by climbing up the wall?" - `captures/2026-07-20/pee-break-door-stare-wall-climb.png` shows
both dogs rendered lying ON the door. `_doorPosition` is the door ART's own center, drawn tall up
the back wall (scale `(x, 4, 1)`), so standing within `StationRange` of it visually read as climbing
the door.

The `DoorStare` stimulus (`BuildActiveSet`), the united-bark "at the door" proximity check
(`HandleBark`), Cocoa's entry placement (`StageDogsForEntry`), her objective-arrow/breadcrumb target
(`TryGetObjectiveTarget`), the `cocoaAtDoor` read that drives marker color/station badges/label copy,
and the door label's proximity gate now all anchor to the new `PeeBreakMissionController.DoorStareAnchor`
property - the existing doormat prop's world position (`_doorMat`, a child of the door art offset
toward its visual base; its local Y offset against the door's fixed Y-scale of 4 keeps the anchor
stable whether the door is open or closed). The door ART itself - position, scale, open/closed
animation - is completely unchanged; only what point gameplay measures Cocoa's stare *from* moved.
The gap between the old center and the new floor anchor is close to 2.5 units, comfortably more than
`StationRange` (2.25), so this is a real relocation, not a cosmetic tweak. Cocoa also picks up a
grounded, facing-the-door idle pose (reusing the existing `ShowGuidanceNudge` read - no new art)
while she holds the stare.

Two cosmetic text pops keyed to the door were judgment calls: the united-bark **WOOF + WOOF!**
callout moved down to the new anchor (it co-occurs with both dogs actually standing there for the
climax), while the door-open **RELIEF ZOOMIES!** pop and the post-clear celebration staging stay
keyed to the door's own position, since by that point the dogs have already been explicitly staged
well outside/below it for the payoff and neither reads as "at the stare spot" anymore either way.

Manual acceptance check: start Operation Pee Break and walk Cocoa toward the door. She should settle
at floor level in front of the door - not partway up its face - before the close-range **HOLD DOOR
STARE** prompt appears and the door/arrow read as "on target." Confirm the same grounded read holds
in Beat 2 (stare + leash) and Beat 4 (stare + leash + bark), where the door station is reused.

### Title-card crop + team-plan visual chips (CF1.9, 2026-07-20)

The 2026-07-20 couch retest's freeform feedback on `pee-break-title-card.png`: the mission-select
detail cover "seems to crop down most of the image... detail is lost," and "the Team plan should
ideally show little visual clips of what things will actually look like on-screen if possible, just
the important ones."

**Crop.** `FitDetailCover`'s cover-fit math was unchanged (still `Mathf.Max` "cover" fit, still fills
the 896-wide letterbox edge to edge - the V3.3 no-backing-strip contract is untouched), but its two
tuning constants were needlessly aggressive: `DetailArtworkZoom` overzoomed the already-covering
scale by 36%, and `DetailCoverHeight` (300px) left little vertical letterbox to begin with. Verified
offline first (this sandbox has no GPU to render the picker) with a throwaway PIL script that
reproduces the exact scale/anchoredPosition math - including the `Mathf.Max` cover-fit, the zoom
applied to that same cover scale (not a separate bolt-on multiplier), and the Y-axis flip converting
Unity's Y-up `anchoredPosition` to PIL's Y-down paste offset - rendered against the real generated
cover art for Operation Pee Break plus five other missions (Kitchen Falling Food Frenzy, Car Ride,
Baby Bird Bedlam, Gate Crash, Backyard Rescue). The old 300px/1.36x combination showed only ~18% of
the square cover art's area (matching the couch-test screenshot almost pixel-for-pixel, including
the "TOP SECRET / BATHROOM MISSION" sign cropped mid-word at both edges); `DetailArtworkZoom` down to
1.05 (a small 5% margin above the exact cover-fit scale, so no bare backing strip can show through a
rounding edge) plus `DetailCoverHeight` up to 340 (taller by 40px, stolen from the text pane below -
`Screen_DetailPanel_FitsConcisePlanAndCropsBakedCoverTitle`'s existing text-fit assertion for
Backyard Rescue's longest plan still passes at the new, smaller how-to-play rect) together show ~34%
of the art - almost double, verified for all six rendered missions. `DetailCoverFillsWidth` and
`DetailCoverUsesTitleFreeCrop` both keep large margin at the new values; neither needed to change.
`FitTileCover`'s small grid-tile constants are untouched - the tile viewport is already close to
square (unlike the wide detail letterbox), so it was never the severe crop the owner saw, and its
own title-free-crop margin has much less headroom to spare.

**Team-plan chips.** The detail panel's "YOUR TEAM PLAN" block used to be a single opaque TMP text
block for every mission. `MissionSelectScreen` now also holds a fixed pool of four (icon + text) chip
rows sharing that same rect. A new data-driven lookup, `TeamPlanChipsFor(variant)` (parallel to the
existing `PreviewStepsFor` bullet array; null/empty entries mean "no chip for this bullet," a
non-null array with at least one entry means "this mission uses chips"), returned null for every
mission except Operation Pee Break at the time this section was first written. When a mission has
chip data, the single text block hides and the per-bullet rows show instead - one small icon per
bullet loaded from the same `FinalGameplayArt` resources gameplay itself uses, plus that bullet's own
text at the same font-floor policy. When a mission has no chip data, the single text block stays
active and every chip row stays inactive - pixel-identical to before this task. Operation Pee Break's
four chips: the door (`PeeBreakOpenDoor`, Beat 1 door stare), the leash (`PeeBreakLeash`, Beat 2
presented leash), the phone charger (`PeeBreakPhoneCharger`, Beat 3 unplug), and the shared
`BarkBurst` VFX sprite for Beat 4's united-bark finish - Pee Break has no dedicated bark-prop art, but
`BarkBurst` is not a placeholder guess: it's the exact sprite `BarkEffect.Spawn()` renders over a dog
every time any dog barks in any mission, including Pee Break's own climax, so it is already a
recognized on-screen object by the time a player reaches Beat 4. `DetailHowToPlayText`/
`DetailHowToFontFloor` (read by existing tests) stay correct regardless of which path is showing,
since the underlying text component's content/sizing is set unconditionally before the chip-vs-text
choice is made.

Covered by three new `MissionSelectScreenPlayModeTests`: a crop-regression test pinning the new,
larger visible-area fraction; a chip test asserting all four Pee Break rows are active with the
correct sprite, text, and top-to-bottom order; and a fallback test asserting a chip-less mission
(Gate Crash, then Kitchen Falling Food Frenzy after round-tripping through Pee Break) renders
text-only with every chip row inactive. Full suite green at `679/679` (676 -> 679).

**CF2.8 (2026-07-21) rolled this same system out roster-wide.** `TeamPlanChipsFor` now returns real
chip data for 21 of the 22 other missions - every one whose `PreviewStepsFor` bullets matched 2+
sprites already loaded by that mission's own `*MissionController.cs` (grepped per mission, not
guessed, so each chip is a genuine preview of the real on-screen signal). Backyard Rescue is the one
documented holdout: its previewed bullets (collect weenies / bark the squirrel / predator warning
huddle / rope-tug finish) match only ONE existing sprite (`BackyardWeenieTargeted`) because the
shared squirrel/predator/rope actors render with generated draft-art parts in this controller, never
a promoted `FinalGameplayArt` override - fewer than 2 genuine matches, so per the fallback contract it
stays text-only rather than padding out with invented icons. Because Gate Crash and Kitchen Food
Frenzy now have real chip data too, `MissionSelectScreenPlayModeTests.Screen_TeamPlanChips_
ChipLessMissionRendersTextOnly` now uses Backyard Rescue as the roster's canonical "stays text-only"
example. Crop verification: a fresh throwaway PIL simulation of the same `FitDetailCover` math
rendered all 23 missions' detail covers (not just 6) - every one lands in the identical 28.3%-64.5%
vertical band of the square source art (the crop math is uniform regardless of image content) and
every mission's two dogs read clearly centered in that band, so no per-mission crop-bias override was
needed roster-wide. Full suite green at `698/698` (694 -> 698, 4 new tests: one roster-wide loop
asserting every mission's chip rows resolve or gracefully fall back to text plus three per-mission
spot checks - a fully-matched 4/4 mission (Sock Panic), a mission with a null in the MIDDLE of its
chip array (Coyotes Fence), and a mission with a shorter-than-bullet-count chip array (Snack Heist)).

Manual acceptance check: open mission select on Operation Pee Break - the detail cover should show
noticeably more of the illustration (the "TOP SECRET BATHROOM MISSION" sign and the glowing paw path
should both be fully readable, not cut off at the edges), and the team plan should show four small
icons (door, leash, charger, bark burst) beside their matching bullet lines instead of plain text.
Page through the rest of the roster - all missions except Backyard Rescue should now show 2-4 small
icons beside their team-plan bullets, each a real prop/state sprite from that mission (e.g. Sock
Panic's basket/sock states, Gate Crash's gate/toy states); Backyard Rescue alone should still show
plain text with no icons.

### Roster audit: auto-dismissing info UI (CF2.1, 2026-07-21)

The CF1.1 briefing-card fix only covered one surface; this roster-wide audit (
`docs/AGENT-WORK-QUEUE-COUCHFIX.md`'s CF2.1 section has the full findings table) checked every
OTHER mission-facing surface that could gate comprehension on a timer. Backyard Rescue's
`ActionTutorial` already passes (completion-gated, no timer at all, existing pause Skip/Replay);
the mission-start `MissionBanner` string turned out to be unrendered dead code (no `ArenaHud` code
ever displays it - test-only state left over from before the CF1.1 card took over); Operation Pee
Break's opening video explainer ends on its own natural completion (or an explicit skip), not a
comprehension-gating timer.

One genuine gap: the F4.1 first-mission control strip (20s + 2s fade, an intentionally *ambient*
reminder, not a required read) had no way back once it timed out, was skipped, or had all four
verbs used - a permanent dead end for the rest of the session. `GameManager` gained
`FirstMissionControlStripAvailable` (true for any active mission other than Backyard Rescue,
independent of whether the strip is currently showing) and `ReplayFirstMissionControlStrip()`
(re-arms a fresh 20s window, clears prior verb-used marks). The pause menu's control-strip row now
uses `FirstMissionControlStripAvailable` to decide whether to show at all, and toggles between
**Skip Control Reminder** (while showing) and **Show Control Reminder** (once gone) - the same
toggle shape the Backyard Rescue tutorial row already used for Skip/Replay Tutorial.

Covered by six new tests: five in `FirstMissionControlStripPlayModeTests.cs` (availability gating
against Backyard Rescue's own tutorial slot; re-summon after skip, after natural timeout, and after
all verbs used; a no-op guard so a stray call can never override Backyard Rescue's tutorial slot)
plus one in the new `FirstMissionControlStripPauseMenuPlayModeTests.cs` driving the fix through
real pause-menu input (an injected keyboard device navigating and confirming through `ArenaHud`'s
actual `Update()`/`ActivatePauseOption`, not a direct method call). Full suite green at `685/685`
(679→685).

Manual acceptance check: start any mission other than Backyard Rescue, let the control-strip
reminder time out (or press **Skip Control Reminder** from pause), then open pause again - a
**Show Control Reminder** button should now be there instead; selecting it brings the strip back
at full strength (not a stale fade), with no verbs pre-marked used.

### Operation Pee Break couch-feedback response (2026-07-14)

The latest human run found five usability gaps: unclear station order/reaction, abstract large-circle
targets, no optional room play, numeric bladder presentation, and no visual start-of-level control
guide. The Unity response is deliberately narrow:

- large station renderers stay off in normal play even when a nearby prompt appears. Dog-local
  arrows, small command signals, contextual labels, and prop/Teenager pulses now carry the route;
  F1 still exposes the authored geometry for observers;
- entering a newly correct signal pulses the relevant prop and the Teenager, while completing a beat
  gives the Teenager a stronger reaction before the next objects become active;
- the room includes a movable tennis ball and squeaky toy. Either dog can press Interact nearby to
  bat one across the floor; toy play is controller-owned and never changes score or progress;
- the compact top HUD shows a persistent, number-free **BLADDER EMERGENCY** fill bar. The objective
  sentence and normal bladder world label no longer print percentages;
- every mission's five-second opening card now devotes its lower half to a visual two-player keyboard
  diagram and Nintendo Switch-style controller diagram: Y/West bark, X/North interact, A/East jump,
  B/South wrestle.

The deep-slice gate remains closed until a second cold run confirms these changes with two humans.

Operation Pee Break now begins with the supplied 10-second 1280×720 animated explainer before its
normal visual controls card. The mission controller owns video preparation, playback, skip, and
cleanup through the optional opening-presentation boundary; the shared orchestrator only freezes
mission time and dog movement while any controller-owned opening is active. Either player can Bark
or Interact to skip the video without also dismissing the controls card. The MP4 ships at
`Assets/StreamingAssets/OperationPeeBreak/operation_pee_break_intro.mp4`, retains its AAC audio, and
respects the game's audio toggle once prepared. Manual check: start Pee Break from mission select,
watch the full explainer once, confirm the dogs and timer do not move, then replay and skip with each
player's Bark and Interact buttons. In all cases the controls card must appear next for its normal
duration before the sniff/ready beat. Playback uses Unity's built-in `com.unity.modules.video`
module; no third-party runtime dependency was added.

### Cross-roster couch-readability audit (2026-07-14)

The first post-Pee-Break audit batch replaces changing percentages with controller-owned visual
meters in **Gate Crash** (squeeze), **Table Stealth** (steak sneak), **The Walk Campaign** (human
comprehension), **Car Ride Chaos** (live slide force), and **Thunderstorm Comfort** (panic). Their
objective and actor copy now says what the dogs should do and reserves the continuously changing
value for the shared meter. Operation Pee Break's in-world phone uses the existing battery fill plus
`CHARGING` / `DRAINING` / `LOW` / `DEAD` states instead of printing a percentage. The first complete
suite after this batch passed `538/538`.

The completed audit also makes promoted station-pad geometry debug-only while leaving prop art,
character reactions, command signals, arrows, and contextual labels visible in ordinary play.
**Backyard Rescue** now reports its final TEAM TUG through the same controller-owned meter instead
of percentage copy. **Baby Bird Bedlam** gains a controller-owned oak/nest silhouette so its first
co-op destination reads before either dog moves. A deterministic audit now cold-starts every one of
the 23 registered controllers and requires an immediate objective, role/mechanic/scene briefing,
sequenced instructions, at least one visible world-integrated cue, no raw percentage objective copy,
and no visible debug-only pad fill. After adding deterministic coverage for the controller-owned
opening-video handoff and skip path, the final complete suite passed `542/542`.

The opening controls card is shared deliberately: every mission shows P1 Cheddar and P2 Cocoa
keyboard clusters plus Nintendo Switch-style Y/X/A/B action mapping before the timer begins. Optional
movable play was added only to the previously empty Pee Break room (tennis ball and squeaky toy).
The other arenas already contain mission-themed objects that move, carry, fall, fumble, scatter, or
react; adding generic toys beside those authored interactions would make their objectives less clear.

The 2026-07-16 roster-wide wayfinding pass adds a literal dog-life scent read to the same shared
objective-target seam: at most three faint, dog-colored paw breadcrumbs animate along the first few
metres of each role's heading, disappear inside the interaction zone, and never reveal a complete
route. The F1 overlay retains the exact action/distance copy. Standalone review also found and fixed
Car Ride's cabin/background render-order tie; its opaque cabin, scrolling windshield, and bench now
cover the backyard plate and decorations. Final evidence after both changes: `552/552` PlayMode tests
passed, the macOS development player rebuilt and passed startup smoke, and the packaged art-review
harness produced all 69 expected 1920×1080 frames with no logged exceptions.

#### Consistent two-player couch retest

Run this same check on every row below with F1 off first, then briefly turn F1 on to verify the hidden
authoring pads remain available to an observer:

1. From the opening card, each player can identify their dog, movement keys, and Bark/Interact/Jump/
   Wrestle mapping. The mission timer must still be paused.
2. Without coaching, both players can say the immediate goal and their different jobs within ten
   seconds. The next beat becomes clear when the current one completes.
3. Approaching, attempting, activating, and completing the main interaction each produces visible
   world feedback through a prop, character, light, signal, arrow, label, sound, or animation.
4. No oversized target circle is needed in normal play. F1 may reveal the authored pads, ranges, and
   diagnostics without changing mission state.
5. Any changing pressure, balance, panic, battery, bladder, or tug value reads as a meter or world
   state, not a changing number. Counts such as `2/5` remain valid for discrete collected objects.
6. At least one deliberate mistake is funny, legible, recoverable, and does not strand either dog.
   Replay returns all cues and props to their opening state.
7. Bark changes gameplay, the two dogs retain distinct jobs, and one dog must create an opening that
   the other turns into progress. Optional props must never block or impersonate that route.

| Mission | Uncoached sequence to validate | Couch-visible proof and recovery check |
| --- | --- | --- |
| Backyard Rescue | recover food; alternate gap-holder and squirrel-pressure roles; survive predator; both tug | squirrel/gap/rope/predator reactions, TEAM TUG meter; fake route and wrong-dog bounce recover |
| Snack Heist | Cheddar steals while Cocoa bark-guards the squirrel | wrong roles coach cleanly; the watched final snack and held stash payoff enforce the handoff |
| Sock Panic | Cocoa anchors the basket while Cheddar dives for and returns the sock | basket/sock pulse and fumble feedback; broken hold, wrong grab, or timeout resets the beat |
| Squirrel Conspiracy | Cheddar herds into Cocoa's cutoff, then Cocoa cracks the stash | escaped solo herds, squirrel/cutoff/stash reactions, and recoverable taunts |
| Eagle Shadow Panic | both hide, Cheddar wiggles, Cocoa pulls, both united-bark | sweep/cover/talon reactions; open-ground exposure and mistimed pulls recover clearly |
| Coyote Fence Defense | Cocoa bark-pins, Cheddar repairs, both finish with united bark | timed pin, moving gap, lure, and breach feedback make every recovery legible |
| Weenie Roundup | split four small carries, then Cocoa steadies Cheddar's jumbo haul | visible cargo, recoverable separation fumble, bowl reaction, and held live payoff |
| Scent Search | Cheddar points broadly, Cocoa tracks/calls, Cheddar digs | directional bark, hot/cold search, recoverable role coaching, and held cache reveal |
| Thunderstorm Comfort | Cocoa reassures, Cheddar answers, both hold the huddle | ordered bark window, PANIC meter, recoverable missed clap, and held storm-passed payoff |
| Mark the Yard | Cheddar interacts to mark; Cocoa barks the reclaim squirrel away | contact alone cannot claim; zone/squirrel reactions, recoverable steal, and held all-marked payoff |
| Walkies on the Leash | stay together through ordered checkpoints | route stones, active checkpoint, and leash snap; separation can be repaired |
| Car Ride Chaos | jump sliding junk; Cocoa anchors and Cheddar tucks for brakes | cabin tilt, SLIDE FORCE, partnered brake wayfinding, recoverable flings, and held arrival |
| Gate Crash | Cocoa Interacts to anchor the opening while Cheddar squeezes through | proximity alone does not open the gate; gate/toy/dog reactions plus SQUEEZE THROUGH meter; leaving snaps and requires a deliberate re-anchor; claimed-toy payoff holds before the result card |
| Table Stealth | Cocoa Interact-flops so Cheddar sneaks, or Cheddar Bark-burps so Cocoa sneaks | proximity alone cannot flop; sustained and burst routes swap the stealing role; human/steak/dog reactions plus STEAK SNEAK meter; exposures recover; stolen-steak payoff holds before results |
| Switcheroo | Cheddar Bark-baits and peels away; Cocoa Interacts once at the exposed stash | proximity alone does nothing; decoy/stash reactions, guarded bonk, readable backfire/reset, and held cracked-stash payoff |
| The Walk Campaign | Cocoa Interact-stares while Cheddar Interact-presents the leash | proximity alone does nothing; leaving breaks the pose; HUMAN GETS IT meter, escalating wrong-item gags, recoverable misreads, and held WALKIES payoff |
| Bone Relay | Cocoa reaches the scent post and barks the call; Cheddar Interacts to dig the signalled mound | proximity alone cannot reveal or dig; post/called-mound/bone reactions, recoverable wrong dig, and held three-bone payoff |
| The Great Escape | alternate owner-only Interacts through the four ordered stations | proximity alone does nothing; named action/owner signals, wrong-paws CLANK, responsive settle-back redo, first-clear scoring, and held FREE DOGS payoff |
| Chaos Machine | Cheddar Interact-pulls; named owners Interact-fire timed junctions while partners pre-position | proximity alone does nothing; distinct cause/effect props, wrong-paws coaching, exact jam/re-pull recovery, tactile stages, and held toy-launch payoff |
| Blanket Catch | spread taut; Cocoa barks to call each drop; both slide under it | blanket tension, called-drop, catch/splat reactions, and held full-blanket payoff; early bark/rip recover safely |
| Kitchen Falling Food Frenzy | Cheddar calls drops; Cocoa catches food and dodges onions; clear dinner rush | counter telegraph, bowl/food reactions, and distinct YUM/DODGED/miss feedback |
| Operation Pee Break | charge phone, break gaze, present leash, united-bark at door | station props and Teenager reactions, BLADDER/phone meters, two movable toys; circles are F1-only |
| Baby Bird Bedlam | reach the oak/nest, react to the fall, grab/shake while partner defends | oak/nest/chick/parent reactions; pecks interrupt without making the rescue unwinnable |

These checks establish automated and documented readiness for a two-person retest. They do not claim
that the second human couch-playtest gate passed; attention, laughter, confusion, and recovery still
need to be observed with two players sharing the screen.

### Pre-couch-test player-facing hardening (2026-07-13)

The current build removes developer-first clutter from the normal family-facing path before the
pending Ken/Sue Operation Pee Break couch test:

- Mission select now opens as **Cheddar + Cocoa Adventures**, marks Operation Pee Break as
  **START HERE** / **RECOMMENDED**, offers one-click **Play Recommended**, and keeps the full
  23-choice roster in a paged **Adventure Library**. Its preview teaches at most four high-value
  team beats; readiness/debug controls no longer compete with the player choice.
- The gameplay HUD is a compact top objective/progress/timer/score bar plus persistent bottom
  **P1 CHEDDAR** and **P2 COCOA** chips. Each chip reports `PAD READY`, `PAD LOST`, `KEYS`, or
  `CONNECT PAD`; the old duplicate control wall and ordinary-play diagnostics are hidden.
- Backyard Rescue teaches one verb at a time in the order Bark, Interact, Jump, Wrestle. Cheddar
  and Cocoa have separate prompt cells, and the lesson advances only after both players try the
  current action. The pause menu can skip or replay the lesson.
- Pause is navigable with D-pad/arrows plus A/Enter, and exposes player comfort toggles for audio,
  rumble, and camera shake alongside Resume, Mission Select, and Quit.
- F1/backquote remains the explicit observer entry to diagnostics. Team feedback now pulses both
  bound player pads, and disconnect recovery binds only an unclaimed replacement pad instead of
  handing one dog's still-connected controller to the other dog.
- Operation Pee Break now uses paired production-sized painterly 16:9 room plates. The base plate
  embeds the closed front door; the success plate replaces it with the open doorway and standing
  Teenager. Procedural room blocks remain hidden fallbacks, and a full-arena foundation stays
  underneath solely to prevent a backyard leak at extreme shared-camera framing.
  Baby Bird Bedlam's flat placeholder picker image is replaced by a matching painterly mission
  portrait; its regeneration helper writes only to `ReferenceOnly` so it cannot overwrite runtime
  art.
- The Pee Break united-bark climax now holds its live open-door/hydrant relief scene for 1.15
  seconds before the end card. `IMissionSuccessPresentationController` freezes timeout pressure
  during that earned-success hold, so a last-second clear cannot turn into a failure while players
  are seeing the payoff.

This is a pre-test hardening pass, not gate acceptance. The Ken/Sue Operation Pee Break playthrough
and laugh log remain required before the mission-roster freeze can lift.

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
- Bark feedback uses the transparent final ring and bark burst; support text is reserved for
  contextual/debug review instead of production staging.
- Pickup, success, warning, rescue, and failure events use distinct short-lived ArenaFinal effects.
- Final dog poses, squirrel/eagle/coyote states, rope, weenies, bushes, rocks, and selected ground
  dressing are live. Squirrel, eagle, and coyote now also have lightweight runtime motion strips
  selected from the same actor-state labels that drive player guidance: squirrel idle/run/steal/
  scared, eagle sweep/attack, and coyote patrol/threaten/retreat. Generated gameplay objects and
  colliders remain authoritative.
- Runtime backyard dressing now includes nonblocking mission-readability layers: an eagle sweep band,
  coyote fence pressure lane, continuous fence rails, a house/patio/back-door cluster with an
  Operation Pee Break outdoor payoff path, scent trail patches, leash route stones, route dashes, and
  clearer snack/laundry districts. Couch test #2: mission-specific cues (threat lanes, scent patches,
  leash stones, pee path) are `MissionScopedScenery`-gated and render only during their own mission;
  the stretched lawn-landmark X panel and the screen-wide predator lane-warning banner are retired.
  These are background cues only; controller-owned markers, colliders, and objective arrows remain
  the source of gameplay truth.
- Operation Pee Break now carries controller-owned, nonblocking interior set dressing: room floor,
  couch back/seat, side table, phone glow, charger cord, door frame, leash hook, hallway rug, misread
  prop, and a door-open sunbeam beat. A second silhouette pass adds couch arms/cushion lines, table
  legs, Teenager head/hair/legs/thumbs, phone screen/reflection, door panels/knob, outlet/plug
  details, and always-visible hanging leash pieces. Pass 2 adds pillows/cup/hoodie/AirPod,
  phone-attention and door-attention beams, question/OH emote bubbles, in-world phone battery fill,
  charged/dead phone states, plugged/unplugged charger pieces, presented-leash trail, visible
  Teenager comprehension/confusion progress, four beat pips, open-door outdoor view/panel, and
  bladder urgency accents. Pass 3 adds a climax-only grass patch, hydrant gag, and relief sparkles
  behind the open door so the payoff reads as a dog-life joke rather than a generic state change.
  Pass 4 adds generated transparent cartoon props under
  `Assets/Art/Resources/ArenaFinal/Props/PeeBreak/`: couch, distracted Teenager, phone/charger,
  open door, leash, hydrant relief payoff, bladder gauge, and misread tennis ball. That pass supplied
  the replacement candidates; Pass 5 below assigns their final runtime roles, while controller-owned
  markers remain the source of gameplay truth. The controller-owned
  room now also adds lived-in dog/Teenager details (blanket, socks, chew toy, hoodie/foot silhouette,
  phone notification ping) with subtle idle animation so the scene reads as a characterful household
  moment without adding colliders or mission rules. Pass 5 adds
  `pee_break_living_room_plate.png`, a painterly 16:9 base room sized for the gameplay camera, and
  `pee_break_living_room_success_plate.png`, its matched success state. The base plate embeds the
  closed front door; there is no active separate closed-door sprite. When the base loads, the
  primitive wall/wood-floor/baseboard/window/side-table/door-slab renderers stay disabled as
  fallback-only geometry. The controller-owned stations, state props, and collision logic remain
  authoritative above it. Before success, the seated Teenager and leash sprites suppress their
  block-built silhouettes; the standalone phone/charger appears only for the actionable charger
  gambit, and the standalone couch stays fallback-only because the Teenager art includes its seat.
  On success, the second plate replaces the base and embeds the open doorway plus standing Teenager;
  the isolated open-door sprite remains fallback-only if that full success plate cannot load.
- Cheddar and Cocoa now play distinct four-frame idle, run, and bark strips. Run has complete eight-way
  coverage using authored east/southeast/south/northeast/north and mirrored west-side travel. Their cadence reinforces
  chaos-pup versus spot-queen identity. Idle, run, and bark preserve all eight facing directions, and tug uses distinct three-frame brace/pull/recover loops;
  stunned, rescued, proud, and sad now use distinct two-frame personality loops. Unsupported actions retain the safe single-pose fallback.
  Weenie Roundup carry now persists visually from pickup through delivery/drop while the separate carried-weenie marker remains authoritative.
- Threat actors now follow the same Resources-based motion pattern at a smaller scope. `ThreatMotionArt`
  provides stable frame paths and `ThreatReadabilityAnimator` swaps a child sprite renderer on the
  squirrel/predator actors without changing gameplay transforms, range indicators, or labels. Marker
  states that reuse the squirrel actor, such as coyote weak spots, intentionally fall back to
  generated marker art instead of showing the wrong animal; the eagle talon-grip marker shows eagle
  attack frames (the talons are the eagle's).
- `ThreatReadabilityAnimator` is the single owner of squirrel/eagle/coyote character art. The actor
  root transform stays uniformly scaled: the placeholder rig's non-uniform `BodyScale` lives on a
  `PlaceholderBody` child, so authored motion frames, mission prop attachments, world labels, and
  range rings no longer render squashed/skewed. The frames draw at full opacity sized by the slot's
  `RootScale`, and `BackyardRescueArtEnhancer` no longer layers semi-transparent squirrel/eagle
  reference overlays over the animated characters (it keeps the couch-readable ground shadows and
  transient beat VFX). Manual acceptance check: start Backyard Rescue and force a predator warning
  (F5 hotkeys/playtest overlay) — the eagle should read as one solid, correctly-proportioned animated
  character with no ghost second eagle and no vertical squash.
- Threat animation fidelity pass (2026-07-04). `ThreatMotionPose` adds a pure procedural pose layer
  over the four-frame strips so every threat state carries between-frame life instead of sliding as
  a cutout: coyote patrol gets a footfall gait bounce and pace sway, threaten coils back then snaps
  forward with a volume-preserving lunge stretch, retreat leans away on nervous back-pedal steps;
  squirrel run becomes a bounding hop with an airborne lean, steal ducks into grabby snatches,
  scared cowers low with a fast side-to-side tremble; the eagle's glide lift is now synced to its
  wing-flap cycle instead of a free-running clock, and the talon attack drops into a vertical dive
  stretch. `ThreatReadabilityAnimator` also crossfades each strip frame into the next through a
  `ThreatMotionBlend` child renderer (ease-in alpha, so frames stay crisp then melt into the next),
  removing the hard 4-frame snap. All poses are bounded (offset ≤ 0.2, lean ≤ 20°, stretch ≤ 1.15
  and volume-preserving) and live entirely on the authored child; actor roots, colliders, labels,
  and range rings are untouched. Covered by `ThreatMotionFidelityPlayModeTests`. Manual acceptance
  check: in Coyotes at the Fence the coyote should visibly prowl-bounce on patrol and snap toward
  the fence when threatening; in Backyard Rescue the squirrel should bound in hop arcs and tremble
  when scared off; the eagle's wingbeats, lift, and frame transitions should read as one smooth
  flight rather than four snapping cutout poses.
- The remaining IMGUI HUDs (`ArenaHud`, `AdventureMapHud`) scale from a 1920x1080-referenced virtual
  space via `GUI.matrix` (`ArenaHud.UiScaleFor`, never below 1x), so fixed pixel font sizes stay
  readable on retina/4K displays instead of shrinking with the physical pixel count. Manual acceptance check:
  on a retina/4K screen, the in-mission objective, pause copy, and adventure-map text should be
  comfortably readable from the couch; on a 1080p or smaller window the layout is unchanged.
- **Mission select is now a UGUI Canvas + TextMeshPro screen (2026-07-02; player-facing pass
  2026-07-13)**, the project's first
  UGUI surface (`MissionSelectScreen`, built entirely in code by `ArenaBootstrap` like everything
  else — the scene file is unchanged). A `CanvasScaler` (1920x1080 reference, Expand) renders TMP
  vector text crisply at any resolution, fixing the blurry stretched-bitmap text the IMGUI picker
  had on retina/4K. The left side is a paged picture-tile grid — 4 columns x 3 rows per page, 23
  missions across 2 pages — where each tile shows the mission's illustrated cover
  (`Assets/Art/Resources/ArenaFinal/UI/MissionTiles/`, via `FinalGameplayArt.LoadMissionTile`), its
  accent stripe, badge code, name, and NEW/RETRY status, with a page indicator underneath. Baked
  title ribbons are cropped behind one consistent TMP name strip, so a tile never shows its title
  twice. The grid shape lives on `GameManager`
  (`MissionSelectGridColumns`/`MissionSelectTilesPerPage`) so
  keyboard/gamepad navigation and the visible tiles can never disagree: up/down wraps within the
  current page's column, left/right walks the tile order and flips pages at page edges, and the
  F5/P/Y couch-test, F6-F9 showcase, and 1-9/0 quick-start shortcuts are unchanged
  (`TickFlowInput` untouched). The header now calls the screen **Cheddar + Cocoa Adventures** and
  names Operation Pee Break as **START HERE**. The right-hand detail panel keeps the large cover,
  badge, name, one-line premise, a maximum four-line **YOUR TEAM PLAN** preview, and the replay
  challenge. **Play Recommended** starts Operation Pee Break in one click; the selected mission has
  its own Start button. Player-facing readiness and Highlight Couch Test controls are removed.
  Couch test #4 (2026-07-04)
  rebalanced the split: the grid gives up 100px to the detail panel (946/896). The current detail
  cover deliberately zooms within its mask to crop the lower baked title ribbon while preserving the
  primary illustration (tiles keep aspect-fill), and the
  description/team-plan blocks TMP-auto-size into their rects (max 22pt, floor 19-20pt) so a long
  mission preview cannot print over the challenge or buttons
  (`Screen_DetailPanel_FitsConcisePlanAndCropsBakedCoverTitle`). In-mission HUD, pause, end
  cards, and session summary still draw through IMGUI `ArenaHud`. End cards echo the next replay
  target, or call out that the challenge was beaten on a flawless run. PlayMode coverage:
  `MissionSelectScreenPlayModeTests` (canvas/TMP setup, per-page tiles, paging, detail panel,
  button flow) plus the updated grid-navigation assertions in `ArenaGameLoopPlayModeTests`. Manual
  acceptance check: on a retina/4K display the tile names and detail text should be pin-sharp (no
  bitmap scaling), arrows/D-pad should move the highlight through the visible grid, pushing right
  from the last tile of page 1 should flip to page 2, and Enter/Start should begin the highlighted
  mission.
- Mission result/end-state HUD now uses one shared full-screen readable overlay for every mission:
  a dark dimmed backdrop suppresses gameplay/HUD behind it, a large opaque centered card carries the
  `MISSION COMPLETE` / `MISSION FAILED` / `SESSION COMPLETE` headline, score/stars/best/reason are
  grouped as large text, and Replay / Next Mission / Mission Select use large high-contrast buttons
  with secondary control hints underneath.
- The Adventure Map flow now uses image-backed level presentation instead of text-only location and
  mission rows. Location cards draw authored `ArenaFinal` environment/prop thumbnails, and mission
  cards draw the same generated `ArenaFinal/UI/MissionTiles` cover art used by the arena mission
  picker. PlayMode coverage asserts that every exposed adventure-map location and mission preview
  loads a real imported sprite and never falls back to `SpriteShapeCache.WhiteSquare`.
- Every mission definition now carries shared couch-test presentation metadata: role hint, mechanic
  family, scene cue, reusable Cheddar/Cocoa presentation guidance, and required readability flags.
  The mission picker exposes the mechanic/scene line and the opening briefing exposes role copy, so
  all selectable levels introduce their co-op job, visual context, and reusable dog presentation in
  the same structure before play begins.
- The F1 playtest overlay retains a per-mission readability gate. It reports `READY` only when the
  active mission has the required objective, score, role, warning, replay, and Cheddar/Cocoa identity
  affordances represented in its definition. This developer status was deliberately removed from
  the player-facing mission picker on 2026-07-13.
- **One-background rule (2026-07-02).** The painted backyard plate
  (`yard_backyard_plate_v02`) now covers the entire 120×68 yard at full opacity and is the single
  background. The bootstrap district rectangles stay in the hierarchy only as fully invisible
  anchors for authored overlays and gameplay staging, the photo-crop reskin layer
  (`ActualPhotoReskin*`) and the translucent runtime-art accents (`ActualArt*`) are retired, and
  the Eagle Shadow cover bushes get authored `bush` art so hide zones stay readable. PlayMode
  coverage asserts the invariant directly: `PaintedPlateIsSoleYardBackground` (plate alpha 1.0,
  full-yard coverage, zero visible runtime-primitive rectangles in the environment) and
  `ArenaWowSetDressing.PlaceholderRectCount == 0`.
- `ArenaWowSetDressing` now installs automatically in `ArenaScene` and adds a cosmetic-only
  presentation layer over the painted plate: two authored snapshot props, an attract prop parade,
  and a mission-reactive spotlight/spark pair reskinned with authored glow/sparkle art. It also
  swaps reusable animated motif families for the selected or active mission: couch-to-door Pee
  Break props, food-heist staging, threat-watch lanes, adventure-route cues, or backyard dog
  props, all using generated transparent cartoon sprite assets under
  `Assets/Art/Resources/ArenaFinal/Props/Wow/`. The former square-based ambience (background
  bands, fence string lights, pawprint runway, sparkles, grass blades, fireflies, welcome mat)
  was removed by the one-background rule: placeholder rectangles no longer layer over the painted
  yard plate.
- `DogShowcasePolish` now attaches through `DogReadabilityFeedback` and gives Cheddar/Cocoa distinct
  dog-local presentation flourishes: Cheddar gets faster warm chaos-comet sparks, Cocoa gets calmer
  teal queen glints, and both receive a soft ground glow/collar glint that animates with movement,
  bark pulse, and zoomies. This is child-renderer-only polish; the dog roots, colliders, Rigidbody2D
  movement, bark logic, and mission controllers remain authoritative.
- Generated Mission Prop Pack Pass 2 now promotes 30 deterministic transparent cartoon prop sprites
  under `Assets/Art/Resources/ArenaFinal/Props/Missions/`, with the contact sheet and source notes
  retained in `Assets/Art/ReferenceOnly/GeneratedMissionProps/`. `MissionPropArt` overlays these on
  controller-owned markers for Snack Heist, Sock Panic, Squirrel Conspiracy, Eagle/Coyote backyard
  threat targets, Weenie Roundup, Scent Search, Mark the Yard, Leash Walk, Car Ride, Gate Crash,
  Table Stealth, Squirrel Switcheroo, Walk Campaign, Bone Relay, Great Escape, Rube Goldberg, Blanket
  Catch, Kitchen/Food, and the Backyard squirrel-trap gap. Existing colliders, labels, range pads,
  objective arrows, and Cheddar/Cocoa character presentation remain shared and authoritative. The
  older colored fallback pads are now alpha-capped behind loaded prop sprites so generated cartoon
  art, dog poses, and objective labels carry the first read. Shared mission prop overlays also add a
  subtle proximity affordance: props sit normally at distance, then pulse/tint when Cheddar or Cocoa
  enters staging range, and clear the cue again when the dogs leave.
- Generated P0 state packs now add mission-specific transparent cartoon sprite states under each
  mission's `Assets/Art/Resources/ArenaFinal/Props/<Mission>/` folder, covering Backyard trap
  redirects, Snack Heist target/guard-lane reads, Sock Panic basket/sock states, Squirrel
  Conspiracy cutoff fakeouts, Eagle talon/cover states, Coyote fence states, Weenie Roundup cargo,
  Scent Search hot/cold/found patches, Thunderstorm comfort cues, Mark the Yard zone/squirrel
  states, Leash Walk checkpoints, Car Ride backseat set (driver, cooler, toy bin), Gate Crash gate/toy states, Table
  Stealth human/steak states, Switcheroo decoy/stash states, Walk Campaign human/leash states, Bone
  Relay scent/mound states, Great Escape station states, Blanket Catch blanket/snack states, Kitchen
  counter/bowl/food states, and Chaos Machine lever states. These remain couch-test generated art;
  controller-owned markers, colliders, labels, and deterministic hooks remain authoritative.
- Table Stealth and Walk Campaign now give their human NPC props a minimal acting/readability layer:
  contextual actor labels, tint, and pulse states distinguish watching/confused idle, distraction or
  comprehension, spotted/misread reactions, success, and fail beats while preserving mission logic
  ownership in the controllers. These are couch-test state changes, not final character animation.
- Generated Environment Prop Pack now promotes 17 deterministic transparent cartoon backyard
  district sprites under `Assets/Art/Resources/ArenaFinal/Props/Environment/`, with the contact
  sheet and source notes retained in `Assets/Art/ReferenceOnly/GeneratedEnvironmentProps/`.
  `BackyardRescueArtEnhancer` layers these over the patio/back door, fence rails, Pee Break outdoor
  route, snack table, laundry corner, scent trail, leash route, lawn landmark, pond, shade tree,
  garden bed, flowers, picnic blanket, sandbox, stepping-stone path, eagle/coyote threat lane, and
  mission-district markers while dimming the older square district renderers behind them. The
  environment overlays are cosmetic-only and add no colliders.
- Generated Building Prop Pack now promotes 3 deterministic transparent cartoon home-exterior
  sprites under `Assets/Art/Resources/ArenaFinal/Props/Buildings/`, with the contact sheet and
  source notes retained in `Assets/Art/ReferenceOnly/GeneratedBuildingProps/`.
  `BackyardRescueArtEnhancer` layers the home facade and back-porch entry over the existing
  house/patio and back-door objects, plus a nonblocking yard shed accent, so the house cluster reads
  as a home exterior instead of stacked district geometry.
- Generated HUD Skin Pack now promotes 6 deterministic transparent couch-test UI sprites under
  `Assets/Art/Resources/ArenaFinal/UI/Hud/`, with the contact sheet and source notes retained in
  `Assets/Art/ReferenceOnly/GeneratedHudSkin/`. `ArenaHud` uses them for mission picker panels,
  mission tiles, selected-row highlights, badges, skinned buttons, mission briefing, pause, end
  cards, session summary, selected-mission showcase, playtest overlay, and the debug mode toggle
  while leaving IMGUI text and hitboxes authoritative.
- Generated World Label Skin Pack now promotes 4 deterministic transparent couch-test label sprites
  under `Assets/Art/Resources/ArenaFinal/UI/WorldLabels/`, with the contact sheet and source notes
  retained in `Assets/Art/ReferenceOnly/GeneratedWorldLabelSkin/`. `WorldLabelSkin` attaches through
  the shared `AddWorldLabel` and score-pop paths so mission object labels, command/warning labels,
  and transient score pops draw cartoon bubble/ribbon/warning/burst skins behind their existing
  `TextMesh` strings instead of reading as raw floating placeholder text. `WorldLabelVisibility`
  now keeps shared mission and actor-state labels hidden until a dog is near the object; the F1
  playtest overlay restores all mission labels for review while score pops stay visible as
  short-lived feedback.
- Generated Gameplay Cue Pack now promotes 5 deterministic transparent cartoon cue sprites under
  `Assets/Art/Resources/ArenaFinal/UI/Cues/`, with the contact sheet and source notes retained in
  `Assets/Art/ReferenceOnly/GeneratedGameplayCues/`. Dog-mounted objective arrows now use a paw
  arrow sprite in production and reserve support text for the F1 playtest overlay; actionable bark,
  tug, and rescue ranges use distinct generated ring sprites instead of generic range geometry and
  reserve labels like `BARK RANGE`, `BOTH DOGS`, and `RESCUE BARK` for the F1 overlay. Existing
  objective copy, labels, target selection, and range radii remain gameplay-authoritative.
- Generated Dog FX Pack now promotes 6 deterministic transparent dog-local sprites under
  `Assets/Art/Resources/ArenaFinal/VFX/Dog/`, with the contact sheet and source notes retained in
  `Assets/Art/ReferenceOnly/GeneratedDogFx/`. Dog action particles, Cheddar/Cocoa paw trails,
  dog-local ground glow, sparks, queen glints, and collar glints now use generated transparent
  sprites instead of white-square-only geometry, while movement timing, action phases, and colliders
  remain unchanged.
- Generated Kitchen Cue Pack now promotes 4 deterministic transparent falling-food warning sprites
  under `Assets/Art/Resources/ArenaFinal/UI/KitchenCues/`, with the contact sheet and source notes
  retained in `Assets/Art/ReferenceOnly/GeneratedKitchenCues/`. Kitchen Falling Food Frenzy now
  swaps gold-food and purple-onion counter telegraphs plus landing circles through
  `MissionPropArtAttachment`, preserving the controller-owned warning markers and labels while
  removing the generic warning sprite as the primary gameplay read.
- Generated Chaos Machine Prop Pack now promotes 3 deterministic transparent Rube Goldberg junction
  sprites under `Assets/Art/Resources/ArenaFinal/Props/ChaosMachine/`, with the contact sheet and
  source notes retained in `Assets/Art/ReferenceOnly/GeneratedChaosMachineProps/`. The Rube Goldberg
  now gives the towel-drop, basket-tip, and toy-launch stations distinct generated prop art while
  preserving controller-owned markers, owner labels, objective targets, cascade timing, and stall
  recovery behavior.
- Generated Level Area Prop Pack promotes deterministic transparent cartoon area sprites under
  `Assets/Art/Resources/ArenaFinal/Props/LevelAreas/` (Kitchen) and
  `Assets/Art/Resources/ArenaFinal/Props/CarRide/` (backseat set), with contact sheets retained in
  `Assets/Art/ReferenceOnly/`. Kitchen Falling Food Frenzy stages an indoor tile/counter area
  behind the controller-owned counter and safe bowl, and Car Ride Chaos stages a full backseat set
  (cabin shell, bench lane, sprite-masked scrolling windshield scenery) that covers the camera
  frame entirely, so nothing reads like the generic backyard. The 2026-07-14 authored-art refresh
  adds layered leather/trim depth, a texture-calmed bench, a seamless opaque neighborhood strip,
  a personality-rich mirror-eye driver, chunky outlined cooler/toy-bin hazards, and a new
  backseat-chaos mission tile. The whole set tilts during turns.
  These are decorative mission-owned roots with no colliders.

Still placeholder or deliberately deferred:

- Dog and threat sprites and gameplay effects are extracted draft art or promoted motion frames rather
  than final animation-ready assets.
- The new wow/motif assets are generated cartoon PNGs, not a final hand-authored art pipeline; they
  are acceptable couch-test art but still need later art direction review.
- Debug/readiness text overlays and some large environment districts
  still intentionally rely on generated geometry/text for the primary gameplay read. The major
  patio, back-door, route, snack, laundry, threat, scent, leash, fence, lawn, pond, shade-tree,
  garden, picnic, sandbox, stepping-stone, house facade, back porch, yard shed, objective-arrow,
  bark-range, tug-range, rescue-range, Kitchen falling-food telegraph, Kitchen landing-warning,
  Chaos Machine junction station, Kitchen indoor area, Car Ride backseat cabin set, HUD
  panel/tile/badge/button, mission world-label bubble/ribbon/warning/burst, and dog-local
  paw-trail/polish cues now have generated cartoon sprite overlays, but
  debug/readiness text affordances are still not final UI or environment art.
- The Pass-2 mission props are generated cartoon PNGs, not a final hand-authored environment/prop
  pipeline; they are acceptable couch-test art and retain dimmed colored pads underneath as readable
  interaction targets.
- Operation Pee Break's room now uses generated transparent couch-test prop sprites, but those are
  not final hand-authored environment or character animation assets. The older colored pads remain
  underneath as readable interaction targets.
- The bunny is decorative only and has no mission logic.
- Final menu/UI system remains deferred; the current couch menu still uses IMGUI text and hitboxes,
  now skinned with generated couch-test HUD sprites rather than production UI components.

Final polish still needed:

- ~~Replace duplicate rescued/proud and rope tug/complete source poses when distinct art exists.~~
  Investigated 2026-07-05: the rope pair was a real, currently-visible bug — `export_arena_final.py`
  cropped the identical box for `rope_tug.png`/`rope_complete.png` (props.png only has one rope
  illustration), so `BackyardRescueArtEnhancer.CompleteTug`'s overlay swap to `RopeComplete` on tug
  clear had zero visual effect. Fixed via `tools/art/generate_rope_complete_variant.py`, which derives
  a golden-tinted/sparkle variant from the exported `rope_tug.png`; the export script no longer
  (re-)exports an identical `rope_complete.png`. Covered by
  `RopeTugAndRopeComplete_AreVisuallyDistinct`. The dog rescued/proud pair is a genuine source-file
  duplicate too (same crop box in the export table), but is **not** currently visible: `ApplyPose`
  assigns that static single-image fallback and then immediately calls `AnimateAuthoredMotion` in the
  same call, which overrides it with the real directional Motion-frame art — and distinct Motion art
  for Rescued vs Proud already exists (`export_character_outcomes.py`, sourced from a real
  `{dog}_outcomes_east_v01.png` sheet with four distinct clips). Left as-is; only worth revisiting if
  that static fallback path is ever reached in a context that skips `AnimateAuthoredMotion`.
- ~~Fix the rope tug overlay rendering as a barely-visible speck instead of the final art.~~ Couch
  report 2026-07-12: "the rope isn't using our nice visual asset." `BackyardRescueArtEnhancer`'s
  overlay used a leftover local scale of `0.035` on a ~1.7-unit-unscaled `rope_tug.png`, rendering it
  at ~`0.06` world units next to the `1.47`-wide generated placeholder it was meant to replace — so
  players only ever saw the placeholder bars. Rescaled to `0.88` so the illustrated rope spans the
  same footprint as the two-dog tug marker. Covered by
  `RopeOverlay_RendersAtALegibleSizeNotATinySpeck`.
- Replace generated motion-derived dog/threat frames with final animation-ready sprites and replace
  remaining generated environment districts.
- Run a two-player television readability pass; the automated 1920x1080 local/full-yard/action
  capture gate verifies composition but cannot judge player attention.
- Run the second human couch pass; the then-current generated P0 mission-state art pass was
  `400/400` PlayMode tests green on 2026-07-01. See the pre-couch-test hardening entry and current
  handoff evidence for later changes.

### Jump wired up as a real contextual action (2026-07-12)

Couch report: "there's only a bark action - we need a contextual action button that can do things
like jump, eat, shake, etc." Investigation found jump (gamepad B) was read into
`DogController.MoveIntent` every frame but never consumed by `Tick` — a fully dead button, same
class of bug as the rope-art fix above. Interact (Y) already does real work when a mission target is
in range (e.g. Backyard Rescue's rope tug); jump is now the real always-available fallback verb:

- `DogController.Jump()` starts a `tuning.jumpDuration` arc (`JumpHeight01`, `sin(pi * t/duration)`),
  gated the same way wrestle/tug/interact are (no-op while `Busy`, i.e. Stunned/Swimming/Shaking/
  Transit/Tug). `Tick` now resolves `intent.jump` every frame alongside bark/interact.
- `DogReadabilityFeedback` gets a `Pose.Jump` (`"HOP!"`), and `DogActionFeedback` gets a `Jump` case
  in its per-dog personality table — Cheddar a quick popcorn hop, Cocoa a slower deliberate bound,
  matching every other action's Cheddar-chaos/Cocoa-control asymmetry.
- Keyboard fallback: P1 Left-Shift, P2 Slash (previously jump had no keyboard binding at all; P2
  jump was originally bound to Right-Ctrl, remapped 2026-07-24 - see the couch-fix entry below).

Covered by `JumpActionPlayModeTests` (arc ramps and lands, blocked while Busy, consumed from
`MoveIntent`) and the `Jump` case added to `Profiles_PreserveDistinctCheddarAndCocoaIdentity`.

### Interact micro-animation (A2.2, 2026-07-18)

Pressing Interact now visibly does something on the acting dog, not just the target prop. One shared
choke point (`GameManager.OnDogInteracted`, where `IMissionInteractionController.HandleInteract`'s
result is already checked) plays `DogReadabilityFeedback.ShowInteractAccepted()` — a brief
squash-and-pop scale pulse (0.22s, a single damped sine bump) layered on top of whatever pose is
already showing, so it reads correctly even mid-tug or mid-dig without interrupting `CurrentPose`.
Cheddar's bounce (0.2 amplitude) is bigger and snappier than Cocoa's (0.12), matching the
chaos-puppy/veteran-queen identity split used everywhere else in this file.

It only fires on a genuine acceptance: `MarkFailedInteraction` (the shared call every wrong-role/
too-far coach beat already uses) sets a one-shot flag checked right after `HandleInteract` returns,
so a coached rejection never also plays the accepted-Interact squash even though several controllers'
rejection branches return `true` from `HandleInteract` (to avoid an unrelated generic-fallback
clobber found in G1.5). No per-controller changes needed — every one of the 23 missions' Interact
paths gets this for free through the one shared dispatch point.

No new authored art (per the task's own allowed fallback) — a code-only tween on the existing
`_authoredPose` transform's scale, applied in `ApplyPersonalityMotion` (the actual last writer of
that transform each frame; the two earlier per-pose scale-setters get overwritten by it every frame,
so multiplying the squash in anywhere upstream of that method would have been silently discarded).

Manual check: press Interact on the correct dog for any mission's accept action (e.g. Gate Crash's
Cocoa-anchors-the-gate) and confirm a quick, snappy squash-pop on that dog only; press it on the
wrong dog and confirm the coaching pop-up fires but the dog itself does *not* also squash.

### Wrestle wired up as a real minimal verb (2026-07-13)

The A-button had the identical dead-input bug as jump (`intent.wrestle` read every frame, never
consumed) — `docs/BUILD-PLAN.md`/`docs/MECHANICS.md` describe a full lunge/flip minigame (reversal
odds, knockback, spot-steal, dust VFX), so it wasn't a one-line fix like jump was. Owner chose the
smallest tested version: make the button do something real now, skip the cuddle-spot/couch steal
(this arena doesn't have that system) and the dust VFX for later.

- `DogController.Wrestle()` gates on its own eligibility (`Busy`, on-cooldown) and fires `OnWrestle`;
  it only knows about itself, not its sibling, so `GameManager.OnDogWrestled` resolves the actual
  attempt — mirrors the prototype's `canWrestle`/`doWrestle` split in `src/systems/wrestle.ts`.
- Gates ported from the prototype: defender `Busy` (mid-tug/swim/stun/transit) or belly-rub `Immune`
  blocks the flip; out of `DogTuning.wrestleRange` (1.8 world units, tuned alongside
  `RescueBarkRange`/`TugInteractDistance` rather than a literal conversion of the prototype's 95px)
  whiffs. Any resolved/whiffed/blocked attempt sets a per-dog cooldown on the attacker
  (`ApplyWrestleCooldown`) so the button can't be spammed.
- The actual flip: an asymmetric reversal-odds coin flip using the attacker's own
  `wrestleWinChance` (Cocoa 0.78 / Cheddar 0.70, already authored in `ArenaBootstrap`) decides the
  winner; the loser gets `DogController.ApplyWrestleStun` (a timed `Stunned` mode that self-expires,
  unlike the predator-grab stun which needs a rescue bark) plus a knockback away from the winner, and
  the winner's velocity is damped on the flip.
- No new readability pose needed — `Stunned` already renders via the existing `Pose.Stunned` path.
- Keyboard fallback: P1 Q, P2 Quote (previously wrestle had no keyboard binding at all, same gap
  jump had; P2 wrestle was originally bound to Right-Alt, remapped 2026-07-24 - see the couch-fix
  entry below).

Not yet ported (tracked as further follow-up, not needed for the button to be real): cuddle-spot/dog-
couch steal-on-win — no such system exists in this arena yet, so there's nothing to steal. Dust VFX
and the near-miss lunge landed in the next entry below.

### Gamepad proof for all four action buttons, plus a progressive first-level tutorial (2026-07-13)

Jump and wrestle were only proven end-to-end for keyboard/direct-`Tick()` input (see the two entries
above); the only *virtual gamepad* proof in the suite was movement (both pads) and bark (X/buttonWest)
in `ControllerCoopPlayModeTests`. That left jump (B/buttonEast), wrestle (A/buttonSouth), and interact
(Y/buttonNorth) unverified on an actual controller path — the same class of gap that let jump/wrestle
sit dead for a release. `ControllerCoopPlayModeTests` now adds
`Gamepad_JumpButton_MakesTheDogJump`, `Gamepad_InteractButton_FiresOnInteract`, and
`Gamepad_WrestleButton_FiresOnWrestle`, each injecting a virtual `Gamepad` button press through the
real `GamepadPlayerInput` → `DogController` path (same pattern as the existing bark test). All four
action buttons now have real controller-press proof, not just keyboard or direct-intent tests.

Separately, there was no player-facing tutorial — new players had to piece together bark/interact/
jump/wrestle from a small always-on text line. Backyard Rescue (the original first mission and still
a dedicated low-pressure practice slice)
now teaches one action at a time in a temporary on-screen card above the player identity chips:

- Four action-colored Switch-style glyphs (Y/West bark, X/North interact, A/East jump, B/South wrestle) rendered as
  two player cells — P1 Cheddar (orange accent bar) and P2 Cocoa (cyan accent bar) — each paired with
  that player's keyboard fallback key, so a couch tester never has to translate "West button" into
  "which key do I press."
- `CurrentTutorialAction` advances strictly Bark -> Interact -> Jump -> Wrestle. Only the action
  currently on screen can record progress, so random later-button presses do not silently skip a
  lesson.
- Progress is stored per dog. One player's cell changes to `DONE`, but the next action does not
  replace the prompt until the partner has tried it too. A whiffed current-action wrestle still
  counts because the tutorial is teaching input discovery, not mission success.
- `ShowActionTutorial` is limited to active Backyard Rescue play and hides after both players learn
  all four actions. Restarting Backyard Rescue re-arms it; **Skip Tutorial** and **Replay Tutorial**
  are available from pause without restarting the mission.

Covered by `ActionTutorialPlayModeTests` (HUD footprint, Backyard-only visibility, separate player
progress, ordered action unlocks, skip/replay, camera-shake comfort toggle, and restart reset) plus
the controller action-path tests above.

### Wrestle dust VFX and the near-miss lunge (2026-07-13)

The two smaller follow-ups flagged when wrestle first landed (a plain whiff felt like a dead button,
and a resolved flip had no impact VFX):

- **Dust burst on resolution**: `DogFeedbackAction` gains a `Wrestle` case (`DogActionFeedback.cs`),
  styled like every other action pair — Cheddar's is a fast, wide "SCRAMBLE DUST" burst, Cocoa's a
  heavier, shorter "GRAPPLE DUST" puff — and reuses the existing per-dog particle/sprite pipeline
  (`ChaosSpark`/`QueenGlint`), no new art needed. `GameManager.OnDogWrestled` triggers it on **both**
  dogs (winner and loser) right after the stun/knockback/damp calls, since both dogs are within
  `wrestleRange` (1.8 units) at the moment of impact — a burst from each dog's own position reads as
  one shared dust-up. The existing `FLIP!`/`REVERSAL!` world-pop text at the midpoint is unchanged;
  this just adds the visual scuffle underneath it.
- **Near-miss lunge**: a whiff (just out of `wrestleRange`) now calls
  `DogController.ApplyWrestleLunge(towardSibling)` — a one-shot velocity kick at the new
  `DogTuning.wrestleLungeSpeed` (9.8 units/sec, preserving the prototype's near-1:1
  `lungeSpeed:knockback` ratio rather than deriving from base speed). This does **not** need a new
  movement mode or override window: the dog stays in `Free` mode, and `Tick()`'s existing
  `MoveTowards`-based velocity resolution already rate-limits how fast player input pulls the
  velocity back, so the kick is visibly a forward dart before it settles — the same mechanism that
  already smooths ordinary acceleration/deceleration, not a new one.

The only remaining unported wrestle piece is cuddle-spot/couch steal-on-win, which stays blocked on
this arena not having that system yet (noted above). Covered by
`Wrestle_ResolvesWhenDogsAreClose_EmitsDustParticlesOnBothDogs`,
`Wrestle_WhiffsWhenDogsAreFarApart_LungesAttackerTowardDefender`, and a new
`DogFeedbackAction.Wrestle` case added to `Profiles_PreserveDistinctCheddarAndCocoaIdentity`. Suite
green at `529/529`.

### Jump actually reads as a hop, and Wrestle/Interact whiffs stop reading as dead buttons (2026-07-25)

Owner report: jump barely lifted the sprite off the ground, and pressing Wrestle or Interact with
nothing to act on (out of range, wrong role, defender busy/immune) played no animation at all — only
the resolved/accepted case had any juice, so a miss looked identical to not pressing the button.

- **Jump height is now tuned, not hardcoded**: the visual arc offset was a flat `JumpHeight01 * 0.4f`
  regardless of dog. `DogTuning` gains `jumpHeight` (world units), and `DogReadabilityFeedback.
  ApplyPersonalityMotion` reads it per-dog instead. Cheddar (`jumpHeight = 1.15`, `jumpDuration =
  0.42s`) pops noticeably higher and snappier; Cocoa (`jumpHeight = 0.8`, `jumpDuration = 0.58s`)
  bounds lower and more deliberately — the same chaos-puppy/controlled-queen split as every other
  action pair, now also true of the arc itself and not just its squash timing.
- **Landing squash**: `DogReadabilityFeedback` edge-detects the `IsJumping` `true -> false`
  transition once per hop and plays a brief (0.18s) wide-and-flat touchdown squash on the authored
  pose, so the landing reads as an impact instead of the sprite just snapping back to idle scale. A
  small continuous peak scale bump (up to 1.1x at `JumpHeight01 = 1`) sells "leaping toward camera."
- **Ground shadow**: a shrinking, fading ellipse appears under the dog for the duration of the hop,
  reusing `PoolRuntimeArt.WaterBand()` (already built for the pool waterline) instead of authoring new
  shadow art. This is the main fix for "doesn't look like it's actually jumping" — without a shadow,
  a few tenths of a world unit of lift reads as noise; with one, the height is legible at a glance.
- **Wrestle whiffs get their own beat**: `DogFeedbackAction` gains `WrestleMiss` — a quick,
  particle-free swipe-and-recoil, distinct from the resolved `Wrestle` action's full dust burst.
  `GameManager.OnDogWrestled` triggers it on the attacker for all three ways an attempt can go
  nowhere (defender `Busy`, defender `Immune`, out of `wrestleRange`), so every wrestle press now
  visibly does *something* on the attacker even when there's nothing to flip.
- **Interact gets a resolved/missed split**: `DogFeedbackAction` gains `Interact` (a snappier,
  particled grab/press beat layered under the existing squash-and-pop read — `ShowInteractAccepted`
  now triggers both) and `InteractMiss` (a small particle-free reach). `MarkFailedInteraction` — the
  single choke point every wrong-role/too-far/no-target coach beat already calls — now also plays
  `ShowInteractMissed()` on that dog, so the audio-only "denied" cue finally has a physical read to
  go with it.

All four new/changed actions keep the file's Cheddar-snappier/Cocoa-heavier asymmetry (distinct
signature, timing, and kick per dog); the two miss variants are deliberately particle-free so they
can never be mistaken for a hit.

Covered by: `Jump_RisesToTheTunedHeight_AndShowsAGroundShadowWhileAirborne`,
`Jump_Landing_PlaysATouchdownSquash` (`JumpActionPlayModeTests`);
`Wrestle_WhiffsWhenDogsAreFarApart_PlaysAMissReadOnTheAttacker`,
`Wrestle_AgainstABusyDefender_PlaysAMissReadOnTheAttacker` (`WrestleActionPlayModeTests`); the
`Interact`/`InteractMiss` assertions added to the existing Gate Crash fixtures in
`InteractMicroAnimationPlayModeTests`; and `MissProfiles_HaveNoParticles_ButStayDistinctPerDog` plus
`Trigger_BeginsTheSequence_ForNewTransientActions` (`DogActionFeedbackPlayModeTests`).

Manual check: jump either dog and confirm a visibly higher hop with a shrinking ground shadow and a
touchdown squash on landing, and that Cheddar's hop reads snappier/higher than Cocoa's. Press Wrestle
with no one in range (or at a stunned/immune partner) and confirm the attacker still visibly swipes
instead of doing nothing. Press Interact on the wrong dog or far from any target and confirm a small
reach/miss beat plays alongside the existing "denied" audio cue.

### Sniff-around lead-in pacing beat (2026-07-04)

> Historical note: the "briefing card is up (`IntroPromptSeconds`, 5s)" framing and the "Bark during
> the card: card drops instantly, `GO!` pops... and the timer starts" acceptance line below record
> the 2026-07-04 fix's fixed-duration card. The "Control card waits for explicit accept (CF1.1,
> 2026-07-20)" entry above supersedes both: the card no longer expires on a timer, and a bark/interact
> while it is up now only accepts it (drops the card, hands off into the still-frozen sniff beat)
> rather than firing `GO!` and starting the timer immediately. The sniff-beat mechanics described
> below (discovery aids, the countdown label, the frozen mission clock, `LeadInSecondsOverride`) are
> otherwise unchanged.

Couch test #4 feedback: levels started "hot" — the briefing card covered the yard for its first
five seconds while timers and threats were already running underneath it. Every mission now opens
with a frozen discovery window:

- While the briefing card is up (`IntroPromptSeconds`, 5s) and for a short open-yard sniff beat
  after it drops (`LeadInSniffSeconds`, 2.5s), the round clock, shared predator/squirrel pressure,
  and the mission controller's schedule all hold still. Dogs can roam, swim, and read the yard;
  objective arrows, travel assist, and range rings stay live as discovery aids.
- The HUD shows `SNIFF AROUND! GO IN n - BARK TO GO NOW` once the card drops; the briefing card
  itself carries a "Yard is paused for a look around" hint.
- Any deliberate dog verb skips straight to GO: a bark (counted, no other effect), an interact
  (no missed-interaction penalty), or scooping the first collectible (banks normally). GO fires a
  juice pop, score-gain chirp, rumble, and a gold `GO!` world pop over each dog.
- Controllers see a frozen mission clock (`GameManager.MissionNow` feeds `MissionContext.Now` and
  `Tick`'s `now`), so schedules anchored in `StartMission` keep their full delays after GO.
  `GameManager.LeadInSecondsOverride` is the test/dev seam (the PlayMode suite runs with it at 0;
  `LeadInPlayModeTests` re-enables it to pin the contract).

Manual acceptance check: start any mission — the timer must not move while the briefing card is up
or during the sniff countdown, and the waiting squirrel/props should be visibly parked. Bark during
the card: card drops instantly, `GO!` pops over both dogs, and the timer starts. Replay the mission
and confirm an immediate bark gets you playing within a second.

### Couch test #2 readability + presentation fixes (2026-07-03)

> Historical note: the picker/button wording below records the 2026-07-03 fix. The current
> 4x3-paged **Cheddar + Cocoa Adventures** screen and **Play Recommended** flow are defined in the
> 2026-07-13 hardening entry above; there is no player-facing Highlight Couch Test button now.

The second couch playtest reported five presentation defects. All are fixed and tested:

1. **Level picture cropped beyond recognition.** 2. **Description/instructions too small.**
   3. **Unreadable bottom buttons.** All three mission-select readability items were resolved by
   the UGUI + TMP `MissionSelectScreen` rebuild (see the mission-select section above): vector TMP
   text is crisp at any resolution, the detail panel shows the large cover with big name/premise
   text, and the then-current Start / Highlight Couch Test controls were full-size UGUI buttons.
   The 2026-07-13 pass replaced those controls with player-facing Start/Play Recommended. An interim IMGUI
   single-column redesign of the old picker was superseded by that rebuild and removed.
4. **Nonsense background scenery.** The `yard_lawn_landmarks` icon was stretched across a quarter
   of the yard (the giant salmon X panel), and `yard_threat_lane` was stretched into two full-width
   trapezoid strips in every mission. The lawn-landmarks overlay and the screen-wide
   `backyard_predator_lane_warning` banner are removed; threat lanes, scent patches, leash stones,
   and the pee path are now `MissionScopedScenery`-gated to render only during their own mission.
5. **Eagle "animation" was a scale pulse.** `MissionActorFeedback` ballooned frame-animated actors
   by up to +/-45% root scale, drowning the 4-frame wing strips. The scale pulse is suppressed while
   `ThreatReadabilityAnimator` owns the visual; the eagle instead gets a small authored-child glide
   bob, and the wing/run frames carry the motion.

6. **In-game HUD and briefing unreadable over the yard** (follow-up report from the same couch
   sitting). The always-on gameplay HUD was bare white text floating on bright green, and the
   mission briefing was a translucent frame whose text spilled past the card. All always-on HUD
   blocks now sit on dark contrast bands, `_mid`/`_small` gameplay text moved up to 20px/16px, and
   the briefing is an opaque dark card (same treatment as the result overlay) with an accent
   stripe, 24px GOAL text, and a pure layout function (`ArenaHud.BuildMissionBriefingLayout`) so
   containment is testable.

Covered by `MissionSelectScreenPlayModeTests` (picker readability),
`MissionSpecificSceneryCues_OnlyShowDuringTheirMission`,
`EagleThreat_AnimatesWithWingFramesNotBodyScalePulse`, and
`MissionBriefingCard_ContainsAllTextRowsWithoutOverlap`; the paged-grid d-pad walk is asserted
in `MissionFlow_Select_StartsEveryMission_AndEndActionsNavigate`.

Current manual acceptance check: open mission select — the zoomed cover must keep its primary dogs/
scene readable while cropping the baked title ribbon, the description and **YOUR TEAM PLAN** text
must be comfortably readable, and the Start button must be large. (The "Title-card crop + team-plan
visual chips (CF1.9, 2026-07-20)" entry above reduced how much of the cover this crop actually
removes and added icon chips to Operation Pee Break's team plan; both are refinements of this same
zoomed-cover/team-plan contract, not a replacement of it.) Start any mission — every line of the
top HUD block and the objective block must sit on a dark band and read clearly over the yard, and
the mission briefing must be an opaque card with nothing spilling past its edges. Start Backyard Rescue — the yard should
show only the painted plate plus real props (no X panel, no trapezoid lanes, no scent dots), and
the eagle must fly with flapping wings instead of growing and shrinking. Start Eagle Shadow Panic /
Scent Search and confirm the sweep band / scent patches come back for their own mission.

### Couch test #3 presentation + pool pass (2026-07-03)

> Historical note: the full HOW TO PLAY catalog remains available to mission logic and tests, but
> the current picker presents at most four **YOUR TEAM PLAN** beats so this wall-of-text issue cannot
> return on the selection screen.

The third couch feedback list reported seven items. All are addressed and tested:

1. **HOW TO PLAY was a wall of text.** `MissionInstructionCatalog.HowToPlayStepsFor` now returns
   short bullet steps per mission (the old `HowToPlayFor` paragraph is the joined steps, kept for
   compatibility). The UGUI mission-select detail panel renders them as bullet lines
   (`MissionSelectScreen.BuildHowToPlayText`), and every on-screen
   label (SQUIRREL STEALING - BARK!, ESCAPE GAP, HIDE HERE...) is quoted verbatim and highlighted
   gold/bold via `HighlightOnScreenLabels` so players recognize the live label when it appears.
2. **"Weird stuff around" / 6. generated scenery clashing with the painted plate.** The generated
   snack-table and laundry-corner district art is now `MissionScopedScenery`-gated to Snack Heist /
   Sock Panic only; the flat vector shade-tree, canopy, and garden-bed overlays are retired (the
   painted plate already paints those landmarks), and the wow-layer's mid-yard bouncing prop dupes
   (`WowBackyardPropsParade`, `WowAdventurePropsEncore`) are removed.
3. **Fake collectibles.** The floating `WowMissionSpark` — pickup-sparkle art that literally draws
   a golden bone with sparkle rays — hovered mid-yard and read as an item you could never pick up.
   Removed; the mission-tinted ground glow is the only ambient accent left in the yard.
4. **Name text over the dogs.** The `DebugHud` floating CHEDDAR/COCOA tags are gone (authored dog
   art carries identity); only the transient WOOF! bark flash remains. The identity/pose label on
   the dog itself stays debug-only (playtest overlay).
5. **Flat eagle.** `ThreatReadabilityAnimator` now banks the eagle into its vertical travel
   (±18°, mirrored with facing) and breathes the sprite scale against the glide bob (higher =
   smaller/farther, lower = bigger/closer), so the flight reads with depth instead of a cutout.
7. **The HUGE pool.** `BackyardPoolZone` ports the frozen TS pool (scenes/pool.ts +
   poolGeometry.ts + movement.ts): a 34x23 open-water rect in the top-left yard quadrant (~11% of
   the whole yard), three drifting tinted floaties you can run across (slight speed bonus), open
   water flips a dog into the slow `Swimming` mode with splash/ripple feedback, and exiting at the
   deck edge roots the dog in a `Shaking` beat (0.9s) before it comes out `IsWet` (4.5s drip
   timer). The pool is open in the 11 yard-staged missions and closed indoors (Kitchen, Car Ride,
   Pee Break...); leaving the yard force-dries the dogs (scene-state reset rule). One Scent Search
   dig mound moved right of the deck so digging never happens underwater. Treats can still land in
   the water — swimming out or scampering the floaties to fetch a floating weenie is the fantasy,
   not a bug.

Stretch fixes from the same pass: the pool's water plate is the photo-derived
`yard_photo_pool_patio` art from the real yard (640x420, near-native aspect for the 34x23 water
rect) instead of the stretched pond sprite, and swimming dogs get a dedicated `Pose.Swim` — the
dry-land run/idle frame strips disengage, the art squashes low in the water and sinks toward the
waterline, and each dog paddles in character (Cheddar `FRANTIC DOGGY-PADDLE` churn vs Cocoa
`STATELY PADDLE` glide, `DogMotionPersonality`). Bark still outranks the swim pose so a paddling
dog can flash WOOF.

Couch test #4 fixes (2026-07-04, live findings from Ken's sitting):

1. **Water where the art shows water.** The plate used to be scaled so the *whole photo* (patio
   border included) covered the water rect — dogs visibly standing on concrete were "swimming".
   The sprite is now scaled so the photo's blue-water region (54.8% x 58.3% of the image,
   `BackyardPoolZone.PhotoWaterFraction*`) equals the gameplay `WaterRect` exactly; the photo's
   patio lands outside it as real dry deck.
2. **Floaties are pool donuts.** The floaties were drawn with the BarkRing paw-print VFX sprite
   and read as giant bark targets ("I don't know what this thing in the pool is"). They are now
   procedurally drawn inner tubes (`PoolRuntimeArt.Donut` — colored tube, seam wedges, gloss arc).
3. **Dogs look wet.** Swimming dunks the dog art pool-blue behind a translucent waterline band
   (`DogReadabilityFeedback.TickWaterLook`); through the deck shake and the 4.5s wet timer the art
   stays visibly damp, then dries back to full color.

Covered by `BackyardPoolPlayModeTests` (pure geometry + swim/shake/wet transitions + indoor
closure + bullet-step and highlight assertions + eagle depth pose + swim-pose/water-plate
assertions + the couch-test-#4 water-region/donut/wet-look contracts) and the updated
`BackyardEnvironmentPlayModeTests` / `BackyardArtEnhancerScenePlayModeTests` scenery assertions.

Manual acceptance check: open mission select — **YOUR TEAM PLAN** uses no more than four beats, with
the squirrel warning shown in gold exactly as it appears in-game. Start Backyard Rescue — no name text floats
over the dogs; the top-left quadrant is dominated by the pool (the real pool-patio art, not a
stretched pond); run a dog across a floatie (slightly faster), walk off it (splash, dog visibly
slows to a swim and paddles low in the water instead of running), swim to
any edge (dog roots and shakes with droplets, then steps clear). Confirm the sparkle-bone prop and
snack-table/laundry art are gone from the yard during Backyard Rescue, then start Snack Heist /
Sock Panic and confirm their district art returns. Trigger the eagle (Backyard Rescue predator
warning or Eagle Shadow Panic) — it should bank into climbs/dives and swell/shrink subtly with its
glide instead of sliding around as a flat cutout.

### Automated art-review cleanup pass (2026-06-29)

The 66-frame `--arena-art-review` pass at
`unity/builds/art-review-current/arena-art-review-contact-sheet.jpg` was regenerated from the macOS
dev player after the showcase scenery and dog-personality polish pass. The pass confirmed that every
roster mission renders at 1920x1080 with 66/66 nonblank captures and that the previously weak review
shots now show their active dog/objective beats:

- Weenie Roundup main/payoff now stage Cheddar and Cocoa at the current weenie/bowl objective instead
  of reviewing an empty home-bowl frame.
- Walkies on the Leash main/payoff now stage both dogs at the active checkpoint before the forced
  checkpoint state change.
- The Rube Goldberg/Chaos Machine main frame now stages the dogs at the active junction after the
  cascade starts, so the co-op coverage beat is visible.
- Car Ride, Pee Break, Kitchen, and other success/failure beats use smaller juice sprites, reducing
  coverage of dogs and mission props while keeping feedback readable.
- Mission prop fallback pads and the ambient Cheddar/Cocoa wow duo are deliberately quieter behind
  the generated prop and character sprites.

Remaining visual limits from the pass: debug/readiness text, IMGUI overlays, and some environment
districts still use generated geometry as gameplay/readability primitives. Range rings, objective
arrows, Kitchen falling-food telegraphs, Kitchen landing warnings, Chaos Machine junction stations,
mission world-label skins, score-pop burst skins, and the house/back-porch/shed cluster now have
generated cue/prop/UI sprites, but they are still couch-test assets rather than final UI/VFX art.

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
  and shrank slightly so the name/pose text floated above the character instead of obscuring it.
  Later passes hid that support text in normal play; it is now an F1 diagnostic only.

Covered by `ArenaGameLoopPlayModeTests.ThreatActors_AreOnScreenAndReadable_NotOffscreenSpinningBlobs`.

### Full-screen picture-tile mission select (2026-07-01)

> Historical implementation record. This interim IMGUI 2x11 picker was replaced by the UGUI/TMP
> 4x3 paged screen on 2026-07-02 and then simplified for players on 2026-07-13. Do not use the layout
> or button instructions in this subsection as current acceptance criteria.

The mission picker was a small centered dialog (900px wide, sized to its row count) with only a
3-letter color badge per mission and a single shared text description below the grid. This interim pass filled
nearly the full viewport (`ArenaHud.DrawMissionSelect`, `FitPanel` clamped to `Screen.width/height`)
and is split into two areas:

- **Left: the 2-column, 11-row mission grid.** Each row now shows a generated picture-tile
  thumbnail (`Assets/Art/Resources/ArenaFinal/UI/MissionTiles/<variant>.png`, loaded through
  `FinalGameplayArt.LoadMissionTile`) next to its name, key number, and status, with a colored
  accent stripe matching the mission's badge color and a bright outline on the selected row.
  Keyboard/gamepad grid navigation (`GameManager.SelectMissionGridStep`) is unchanged — still a
  fixed 2 columns x 11 rows — so this is a presentation-only change.
- **Right: the mission detail panel.** Shows a large picture of the selected mission, its name, a
  generated one-line premise, and a grounded "HOW TO PLAY" readout that names the mission's real
  on-screen labels, markers, and dog verbs (both come from the new `MissionInstructionCatalog`,
  cross-checked against this doc and `docs/COOP-PUZZLE-PRIMITIVES.md` rather than invented copy),
  plus the existing per-mission replay challenge and readability gate, and the Start/Highlight
  Couch Test buttons.

Every mission variant has a matching generated picture tile and catalog entry, asserted by
`BackyardEnvironmentPlayModeTests.MissionSelect_HasPictureTileAndInstructionsForEveryMission`. The
full-viewport contract is asserted by the updated
`MissionSelectPanel_FitsAllMissionRowsAndReadableGoal`.

The acceptance check for this interim layout is retired. Use the current 4x3 UGUI/TMP mission-select
criteria in the 2026-07-13 hardening entry instead.

### First-minute objective readability pass (2026-06-21)

The first playtest also reported that it was not clear what to do even when some mechanics were
visible. Two presentation defects made the existing mission copy easy to miss:

- The 21-mission picker was only 458 pixels tall even though its 11 rows plus selected-mission
  details require 714 pixels at the authored spacing. The picker now sizes from its row count, and
  the selected mission gets a wrapped 70-pixel `GOAL` block instead of a clipped 34-pixel line.
- The in-round intro reused a single 34-pixel banner for mission prompts that can span several
  sentences. The first seconds of every mission now show a dedicated goal card with the mission
  premise, the current first objective, and the controller verbs: follow the dog arrows, move,
  bark, and interact. Mission timing and rules are unchanged.

Covered by `MissionSelectPanel_FitsAllMissionRowsAndReadableGoal` and the opening-briefing assertion
in `BackyardMission_Objectives_Hazards_Tug_Clear_AndRestart`.

### Kitchen controller extraction (2026-06-21)

Kitchen Falling Food Frenzy now runs through the registered `IMissionController` boundary. Its
definition, setup/reset, actors, tick/input behavior, outcome state, objective guidance, cleanup,
deterministic hooks, and runtime snapshot are no longer owned by `GameManager`. Existing public test
hooks remain as compatibility forwarders. The behavior-preserving extraction passed the full 333
test PlayMode suite.

## Cold-start flow

The expanded yard stages both dogs within a short run of each mission's first meaningful action
instead of resetting every mission at the same generic center point. A deterministic PlayMode contract
keeps both dogs within 12 world units of that entry target. Dog-mounted objective arrows cover all
mission-specific verbs (hide, herd, repair, carry, sniff, mark, comfort, and leash checkpoints), show
distance, and scale up during strategic camera zoom so navigation remains readable across the 120x68
yard.

When an active objective is more than 28 world units away, that dog's objective arrow enables a
modest `1.55x` trail-travel speed assist. The identity label changes to `TRAIL READY`/`TRAIL SPRINT`,
the observer guidance state adds `[TRAIL SPRINT]`, and normal close-control speed returns inside 20
units. That label text is visible only with diagnostics; the arrow/motion cue carries normal play.
The hysteresis prevents flicker and keeps bark, tug, pickup, dig, hide, and leash interactions on
their existing precision tuning.

The shared camera treats its authored maximum zoom as the 16:9 target, then derives a safe wider
ceiling for narrower and portrait windows. This keeps both dogs framed at maximum separation across
32:9, 16:9, 4:3, and 9:16 aspect contracts. Cheddar/Cocoa authored art and pose reads scale with
strategic zoom; debug identity labels follow the same scale only when the F1 overlay is open.

All blocking HUD panels now share an eight-pixel safe-area contract. Mission select, pause, end-card,
session summary, and the playtest overlay shrink to the current viewport instead of clipping fixed
640-900 pixel layouts on narrow desktop windows. The shared mission result overlay additionally keeps
the card at roughly couch-readable full-modal scale on normal playtest displays instead of returning
to the old small translucent panel.

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

The gameplay HUD now reserves one compact 102-pixel top bar for mission name, current objective,
objective progress, timer, and score. Persistent bottom-corner identity chips keep **P1 CHEDDAR**
and **P2 COCOA** visible without covering the central play space and report each player's current
input source (`PAD READY`, `PAD LOST`, `KEYS`, or `CONNECT PAD`). Detailed route/counter/event state
remains in the opt-in F1 observer overlay instead of competing with the objective during ordinary
play.

1. Open `unity/CheddarAndCocoa` in Unity 6 LTS, open `Assets/Scenes/ArenaScene.unity`, and press Play. `ArenaScene` is also the scripted local build entry point.
2. The mission picker appears immediately, filling nearly the full window. Use **arrow keys** or the
   gamepad **D-pad** to highlight a mission, then press **Enter**, gamepad **Start**, or gamepad
   **South** to start. Keyboard **1-9 and 0** directly starts the original first ten missions; use
   arrow/D-pad selection for later missions.
   - All 23 mission variants use the same 4-column x 3-row paged picture-tile grid. The right-hand
     detail panel shows a large crop that keeps the primary illustration and removes the baked title
     ribbon, plus the premise, replay challenge, and at most four **YOUR TEAM PLAN** beats. The
     complete art set lives under
     `Assets/Art/Resources/ArenaFinal/UI/MissionTiles/`.
   - The header recommends Operation Pee Break as **START HERE**. Choose **Play Recommended** for a
     one-click start, or select its tile and choose **Start Recommended Adventure**.
   - For the family showcase runbook in `docs/FAMILY-SHOWCASE-MANUAL-TEST.md`, the host can press
     **F7/B** to highlight Backyard Rescue, **F6/K** to highlight Kitchen Falling Food Frenzy,
     **F8/W** to highlight Weenie Roundup, **F9/L** to highlight Walkies on the Leash, or **F5/P**
     to highlight Operation Pee Break.
   - For the current couch-test focus, keyboard **F5**/**P** or gamepad **North/Y** also highlights
     **Operation Pee Break**; press **Start/South** to launch after using that shortcut.
   - Each tile shows `NEW`, `RETRY`, `CLEARED`, or `FLAWLESS` plus its session-best score; the detail
     panel shows round time and objective size.
   - The header keeps missions played, unique adventures explored, and flawless clears visible
     before the next choice.
3. Read the selected mission's team plan, then start it. The opening goal card repeats the premise,
   first objective, roles, and P1/P2 ownership. During play the compact HUD keeps only mission,
   objective/progress, timer, score, and the two player/input chips visible; diagnostics and event
   history stay behind F1.
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

The HUD objective and dog-local arrows name the current pressure dog, route the partner to the visible **ESCAPE GAP** marker (its hold instruction lives on the badge and HUD line), and switch to **RECOVER DROP / PARTNER ONLY** after a redirect. Barking with the wrong dog or before the gap is held makes the squirrel take a comic fake route and loop back. If the pressure dog touches its own dropped weenie, the weenie bounces away with a **HOT POTATO! PARTNER ONLY!** cue; the drop remains live, so no trap mistake hard-fails the mission.

Unique scoring/events include **+50 WEENIE SAVED**, **+25 SQUIRREL SCARED**, **+300 PREDATOR YEETED**, **+250 PARTNER RESCUE**, **+200 TUG COMPLETE**, and **+500 LEVEL CLEAR** plus time bonus.

Deterministic hooks are `ForceBackyardTrapRedirect(pressureDog, gapHeld)` and `ForceBackyardTrapRecovery(dog)`. `BackyardSquirrelTrapPlayModeTests` covers wrong-role/open-gap recovery, partner-only pickup, role reversal, completion, event logging, and replay reset. Backyard clear now additionally requires both trap recoveries; the existing collect, squirrel-steal limit, predator, and tug requirements are unchanged.

Manual acceptance check: start Backyard Rescue with **1**. Confirm the blue escape-gap marker and the initial Cheddar/Cocoa role copy are readable. Let the squirrel target a weenie, park Cocoa in the gap, and bark near the squirrel as Cheddar. Confirm the squirrel redirects and drops the weenie. Touch it first as Cheddar and confirm the comic bounce is recoverable; collect it as Cocoa and confirm the guidance swaps. Repeat with Cocoa pressuring and Cheddar holding/recovering. Then finish the original food, predator, and tug objectives and confirm the mission clears. Also verify barking before the holder reaches the gap produces a fake route without losing the run.

### Snack Heist

Snack Heist is a compact asymmetric protect-and-collect mission using the existing item and squirrel systems without predator or tug requirements. Cheddar must stash `4` forbidden snacks while Cocoa guards the squirrel lane; the squirrel can steal only `2` within `80` seconds.

Readable differences:

- Collectibles use generated snack-plate prop sprites with dimmed fallback markers and **Snack!**
  labels. A delayed art-stability check verifies the dynamic treat enhancer does not replace them
  with Backyard weenie art after spawn.
- Objective text starts as **Cheddar: Stash snacks 0/4**. Cheddar's first stash immediately starts a readable squirrel heist so Cocoa receives an authored guard opening.
- Only Cocoa can interrupt an active steal. Cheddar's mouth-full guard bark and Cocoa's snack inspection produce harmless, funny role coaching without consuming the snack or losing the run.
- The watched final snack does not bank until Cocoa has stopped at least one active heist, guaranteeing the protect-then-steal co-op handoff.
- Squirrel pressure uses snack-specific labels like **SQUIRREL SNACK HEIST - BARK!** and **SQUIRREL STOLE A SNACK!**.
- Unique scoring/events include **+60 SNACK STASHED**, **+35 SNACK GUARD BARK**, **-90 SNACK THIEF**, and **SNACK HEIST CLEAR**.
- The final stash holds live for `1.15` seconds with **STASH SECURED!**, mission-win audio, rumble, and the defeated squirrel-union state before the clear card.
- Clear banner: **SNACK STASH SAVED!**.
- Fail reason calls out the squirrel union escaping with forbidden snacks.

Manual check: start Snack Heist from mission select. Touch a plate as Cocoa and confirm **QUALITY CONTROL** leaves it available. Stash the first plate as Cheddar, confirm the squirrel immediately targets another plate, then try Cheddar's nearby bark and confirm **MOUTH FULL!** does not cancel the steal. Bark near the active squirrel as Cocoa, confirm **+35 SNACK GUARD BARK**, then finish with Cheddar. Confirm the final snack cannot bank before Cocoa's guard, the held **STASH SECURED!** payoff is readable, and the end card follows. On replay, let or force the squirrel to steal twice and confirm the flavored fail copy.

### Sock Panic

Sock Panic is now built around the **Anchor and Dive** co-op puzzle beat. Cocoa uses her veteran-queen steadiness to interact beside the laundry basket, tip it open, and remain within the hold range. Cheddar uses his chaos-puppy sock obsession to dive onto the exposed sock within `6` seconds. If Cocoa leaves the basket before the dive, it immediately flops shut. They must rescue `5` socks before the `55` second timer expires, with squirrel, predator, and rope actors disabled.

Readable differences:

- A generated slatted **LAUNDRY BASKET** is the shared puzzle object; hidden socks only become visible after a tip.
- Objective arrows split by identity: Cocoa sees **INTERACT TO TIP** / **HOLD BASKET**, while Cheddar sees **WAIT TO DIVE** / **DIVE FOR SOCK**.
- Objective text tracks returned socks and fumbles, then calls out the temporary partner-dive window.
- Unique scoring/events include **+20 BASKET TIPPED**, **+40 PARTNER SOCK DIVE**, **-15 DECOY SOCK FUMBLE**, and **SOCK PANIC CLEAR**.
- The fifth dive holds a short live **SOCK MOUNTAIN RESCUED!** proud-pose payoff before the **SOCKS SORTED!** clear screen.
- Time failure reason: **Laundry order returned before the final sock was rescued.**

Funny failure/recovery: if Cocoa abandons the hold, grabs the sock herself, or lets the opening time out, the sock is exposed as a decoy/fumble and the basket flops shut. Nothing is permanently lost; Cocoa can anchor it again while Cheddar resets for another ridiculous dive.

Manual check: press **3** and confirm Cheddar cannot open the basket. Move Cocoa to the basket and Interact, keep her beside it, then use Cheddar to collect the exposed sock. Confirm **+20 BASKET TIPPED**, **+40 PARTNER SOCK DIVE**, and `1/5` returned. Intentionally walk Cocoa away, grab with Cocoa, and let one opening time out; confirm each produces a recoverable **-15 DECOY SOCK FUMBLE** and resets to Cocoa's **INTERACT TO TIP** state. Rescue the fifth sock and verify the live sock-mountain payoff precedes the end screen. Replay and confirm socks, fumbles, basket state, and score reset.

### Squirrel Conspiracy

Squirrel Conspiracy is the first production-mission slice using `HerdingMissionState`. Cheddar uses chaos-puppy pressure to **BARK HERD** the suspicious squirrel while Cocoa uses her territory-holding steadiness on the active **HOLD CUTOFF** zone. Each route step requires that physical two-dog handoff: Cheddar's solo herd attempt does not advance control or reveal the stash. Four completed cutoffs expose the evidence, then Cocoa must reach the stash and Interact while Cheddar guards the culprit. Fake-outs and taunts remain recoverable until the squirrel reaches `3` taunts.

Readable differences:

- The squirrel stays active as the primary objective instead of stealing spawned collectibles.
- Per-dog objective arrows keep Cheddar on **BARK HERD** and Cocoa on **HOLD CUTOFF**, then switch Cocoa to **INTERACT STASH** and Cheddar to **GUARD SQUIRREL**.
- Objective text tracks route/control progress, escaped solo herds, stash reveal, and taunt pressure.
- Unique scoring/events include **GOOD HERD**, **CUTOFF**, **DOUBLE BARK BLOCK**, **FAKE OUT**, **STASH FOUND**, and **CONSPIRACY CRACKED** from the production score catalog.
- Cocoa cracking the stash holds a short live proud-pose payoff before the **CONSPIRACY CRACKED!** clear screen.
- Fail reason calls out the squirrel taunting the yard into a misinformation spiral.

Manual check: press **4**. Bark as Cocoa and confirm she cannot replace Cheddar's pressure role. Bark as Cheddar while Cocoa is away from the cutoff and confirm the squirrel escapes without increasing control. Put Cocoa on **HOLD CUTOFF**, bark near the squirrel as Cheddar, and confirm one **CUTOFF** advances the route. Complete four handoffs, confirm Cheddar cannot inspect the stash, then Interact there as Cocoa and verify the live payoff precedes **Conspiracy Cracked**. Let or force three taunts and confirm fail/replay resets the herding counters.

### Eagle Shadow Panic

Eagle Shadow Panic is the second production-mission slice using `ThreatSweepMissionState`. A sweeping eagle shadow crosses the whole yard; Cheddar and Cocoa must both be inside cover when each pass resolves. After two clean hides the eagle snatches Cheddar: he Interacts to wiggle the talon grip open, Cocoa gets close and Interacts to pull during the short window, and three successful handoffs free him. They then huddle for a united-front bark circle to drive the eagle off. Three exposures caught in the open end the run.

Readable differences:

- The predator actor is the **EAGLE SHADOW SWEEP** threat and the shared secondary actor carries the authored open/closed **TALON GRIP** states during Cheddar's rescue (no spawned collectibles or squirrel-steal loop).
- Objective text moves through three phases: both dogs hide (`safe hides x/2, exposures x/3`), Cheddar-wiggle/Cocoa-pull rescue, and the final `United-front bark circle`.
- The rescue objective only opens after `2` genuine covered passes; standing in open ground away from the eagle's endpoint no longer grants a free safe hide.
- Unique scoring/events include **SAFE HIDE**, **SHADOW DISTRACTED**, **EAGLE SPOOK** (exposure penalty), **TOY RESCUED**, **UNITED FRONT**, and **SHADOW PANIC CLEAR**.
- The final united bark holds a short live proud-pose/eagle-retreat payoff before **EAGLE DRIVEN OFF!**; the end summary reads **Backyard Defenders**.
- Fail reason calls out the eagle shadow catching dogs in the open too many times (**Shadow Trouble** summary).

In real-time play the hide phase is spatial: three labeled **HIDE HERE** cover zones sit in the yard and the eagle shadow physically sweeps left/right across the field. Because one pass traverses the full play band, every dog outside cover when the pass completes is exposed. Both dogs safely covered scores one hide; two clean hides open the rescue.

The deterministic test/pacing hooks are `ForceEagleShadowSafeHide()`, `ForceEagleShadowExposure()`, `ForceEagleShadowSweepPass()`, `ForceEagleShadowRescue(dog)`, and `ForceEagleShadowUnitedFront()` (`EagleCoverZones` exposes the cover positions); in normal play the final united-front phase also resolves through the existing huddled united-bark path.

Manual check: press **5**. Leave either dog in open ground through a completed sweep—even away from the eagle's final edge position—and confirm it records an exposure. Put both dogs in cover for two passes. Confirm Cheddar is pinned in the talons, Cocoa cannot pull from far away, a Cocoa pull without Cheddar's fresh wiggle misses recoverably, and three timed wiggle/pull handoffs free him. Huddle and bark together, verify the live eagle-retreat payoff precedes **Backyard Defenders**, then force three exposures and confirm replay resets the counters and all cover art to calm Safe states.

### Coyotes at the Fence

Coyotes at the Fence is the third production-mission slice using `PatrolDefenseMissionState`. Cocoa uses her territory-queen steadiness to get within bark range and **BARK-PIN** the coyote, creating a `2.25`-second opening. Cheddar uses his digging energy to reach the active **WEAK SPOT** and Interact before that opening closes. Roles are enforced: the same dog cannot create and cash out the opening, and an across-the-yard bark does nothing. A late **fake snack lure** targets Cheddar's impulse control and is defused by Cocoa's bark. After three repaired gaps, both dogs bark down the final coyote push. Three breaches end the run.

Readable differences:

- The predator actor is repurposed as the **COYOTE AT THE FENCE** and the squirrel actor as the moving **WEAK SPOT / FILL DIRT** marker (no spawned collectibles or squirrel-steal loop).
- Objective arrows split by identity: Cocoa tracks **BARK-PIN COYOTE** while Cheddar sees **WAIT FOR COCOA PIN**, then **INTERACT: FILL DIRT** during the live opening.
- Objective text moves through patrol (`fence gap n, repairs x/3, breaches x/3`), timed pin (`Cheddar fill NOW`), lure, and final-push (`both dogs bark together`) states.
- Wrong-role, out-of-range, late, and no-pressure attempts are rejected with actionable recovery copy.
- Unique scoring/events include **FENCE HELD**, **DIRT FILLED**, **COYOTE BLOCKED**, **FAKE SNACK BAIT**, **COYOTE BREACH** (penalty), and **YARD DEFENDED**.
- The final united bark holds a short live proud-pose/coyote-retreat payoff before **YARD DEFENDED!**; the clear summary reads **Fence Guardians**, while a breach fail reads **Needs More Patrols**.

In real-time play the coyote physically prowls toward the active **WEAK SPOT** (one of four labeled fence gaps around the yard). Cocoa's live bark pin drives it back if it reaches the gap; an unguarded gap breaches. After a breach, both the logical target and visible weak-spot marker advance together to the next gap.

The deterministic test/pacing hooks are `ForceCoyoteBarkPressure(dog)`, `ForceCoyoteRepair(dog)`, `ForceCoyoteBreach()`, `ForceCoyoteFakeSnack()`, `ForceCoyoteProwlReach()`, and `ForceCoyoteFinalBlock()` (`FenceGaps` exposes the gap positions); in normal play barking pins the coyote and the final push also resolves through the existing huddled united-bark path.

Manual check: press **6**. Confirm Cheddar's bark and Cocoa's dirt-fill attempt cannot replace their partner's role, and that Cocoa cannot pin from outside bark range. Bark nearby as Cocoa, then Interact at the weak spot as Cheddar before the opening closes. Let one pin expire and confirm Cheddar gets recovery guidance instead of progress. Force one breach and verify the active weak-spot marker moves to the new logical gap. Repair three gaps, huddle and bark together, and verify the live coyote-retreat payoff precedes **Fence Guardians**. Confirm three breaches fail and replay resets counters and every gap to neutral Open art.

### Weenie Roundup

Weenie Roundup is a shared-object **carry + steady** mission using `CarryRoundupMissionState`. Four small **WEENIE** markers preserve the quick split-yard ferry loop. The fifth is a visibly larger **JUMBO WEENIE**: Cocoa must stand beside it to steady the load before Cheddar can grab it, then the dogs must remain together through the haul to the **HOME BOWL**. Deliver all five before the `85` second timer expires.

Readable differences:

- No squirrel/predator/tug; the opening loop is pick-up → carry → deliver across the whole yard, followed by one authored co-op haul.
- Cocoa cannot carry the jumbo and Cheddar cannot lift it without Cocoa nearby. During transit, Cocoa's objective points back to Cheddar so the steadying job stays legible.
- A carried weenie rides above the dog. If the pair separates for roughly a second during the jumbo haul, it bounces back into the yard with explicit retry guidance; ordinary deterministic fumbles remain recoverable too.
- Objective text tracks deliveries and loose count, then switches through `FINAL JUMBO`, `JUMBO HAUL`, and the live bowl-full payoff.
- Unique scoring/events include **WEENIE GRABBED**, **WEENIE DELIVERED**, **FUMBLED WEENIE** (penalty), and **ROUNDUP COMPLETE**.
- Clear banner: **BOWL FILLED!**; end summary on a clear reads **Weenie Wranglers**, with fumbles reading **Butterpaws**.

The deterministic test/pacing hooks are `ForceWeeniePickup(dog)`, `ForceWeenieDeliver(dog)`, and `ForceWeenieDrop(dog)` (`BowlPosition` exposes the bowl); in normal play pickup/deliver/drop are driven by dog proximity, Cocoa's jumbo support range, pair separation, and the bowl.

Manual check: press **7** and split the dogs to ferry the four small weenies. At the jumbo, confirm Cocoa alone is told to steady, Cheddar alone cannot lift, and Cocoa standing beside it creates Cheddar's grab. Separate during the haul and verify the jumbo fumbles harmlessly, then retry, stay together into the bowl, and confirm the live `BOWL FULL!` beat precedes **Weenie Wranglers**.

Pickup now produces `WEENIE GRABBED!`, a proud carrier pose, collection cue, and light rumble. A drop
produces `FUMBLE!` at the bounced weenie, a worried flinch, warning cue, and stronger rumble before
the chase resumes.

### Scent Search

Scent Search is an asymmetric **track + call + dig** mission using `ScentSearchMissionState`. Six **DIG?** mounds are scattered across the yard; one hides a bone. Cheddar's excited bark gives a broad compass direction but cannot reveal the mound. Cocoa moves between patches and barks for the precise `COLD / WARM / RED HOT` read; a red-hot bark marks her exact call. Cheddar then reaches that glowing mound and Interacts to dig. Find three bones before four genuinely wrong called digs (or the timer) end the run.

Readable differences:

- No squirrel/predator/tug; the loop is Cheddar direction → Cocoa heat search/call → Cheddar dig.
- Bark remains useful for both dogs but returns identity-specific information. Interact belongs to Cheddar's digging role.
- Cheddar digging before Cocoa's call or Cocoa trying to dig produces explicit role guidance without wasting a cold-dig life. After a call, choosing a different mound is a real scored miss.
- Objective arrows keep Cocoa on active scent patches, point an uncalled Cheddar back toward Cocoa, and then send Cheddar directly to the called mound.
- Objective text tracks bones and cold digs, then holds the final found mound live as a bone-cache payoff before clear.
- Unique scoring/events include **HOT SNIFF**, **BONE DUG UP**, **COLD DIG** (penalty), and **SEARCH COMPLETE**.
- Clear banner: **BONES UNEARTHED!**; end summary reads **Master Sniffers** on a clean clear, **Dug Up The Whole Yard** when there were wasted digs.

The deterministic test/pacing hooks are `ForceScentSniff(dog)`, `ForceScentDigCorrect(dog)`, and `ForceScentDigWrong(dog)` (`DigSpots`, `BuriedSpotIndex`, and `CalledSpotIndex` expose the current search); normal play enforces the Cocoa-call/Cheddar-dig handoff through bark, position, and Interact.

Manual check: press **8**. Bark as Cheddar and confirm he reports only a compass direction. Try digging before a call and confirm no cold-dig life is spent. Move Cocoa between mounds, barking until `RED HOT` marks her call; confirm Cocoa cannot dig it, then bring Cheddar to the glowing mound and Interact. Repeat for three bones and confirm the live `BONE CACHE` reveal precedes **Master Sniffers**.

A cold dig now adds a worried dog flinch, warning cue, and short rumble to the existing `COLD!` pop
and score penalty. The failed read is visible, audible, and physical before the player chooses another
mound.

### Thunderstorm Comfort

Thunderstorm Comfort is an active **reassure + answer + huddle** mission using the existing `PanicMeter`. Cheddar (chaos puppy) still spooks harder than Cocoa (veteran queen), but proximity alone no longer clears the level. Before each periodic thunderclap the dogs huddle, Cocoa barks the steady reassurance opener, Cheddar answers within the short window, and both hold the physical huddle until the clap lands. Only a prepared clap advances the five-clap goal; max panic still makes a dog bolt and fail.

Readable differences:

- No collect/squirrel loop; each beat is an active communication sequence: huddle → Cocoa bark → Cheddar bark → hold.
- The storm actor and objective distinguish `COCOA BARKS FIRST`, Cheddar's answer window, `COMFORT READY`, a missed comfort, and the storm-passed payoff.
- Wrong bark order coaches harmlessly. An unprepared or separated clap raises panic and counts as an exposed mistake but does not advance progress, so the pair can recover on the next clap.
- Prepared comfort softens the asymmetric panic spike; continued huddling drains accumulated panic between claps.
- Unique scoring/events include **CLAP WEATHERED**, **COMFORT HUDDLE**, and **STORM PASSED**.
- Clear banner: **STORM WEATHERED!**; end summary reads **Weathered The Storm** on a clear, **Spooked By Thunder** on a bolt.

The deterministic test/pacing hooks are `ForceThunderclap()` and `ForceComfortStep(seconds)` (`ComfortPrepared`, `Panic`, and `ThunderstormState` expose the live sequence); normal play consumes Cocoa/Cheddar bark input inside the controller, fires claps on a timer, and drains panic while the dogs remain within cuddle range.

Manual check: press **9** and first park both dogs together without barking; confirm the clap says `TOO QUIET`, raises panic, and does not advance. Bark as Cheddar first and confirm Cocoa-first guidance. Then bark Cocoa→Cheddar while huddled, see `COMFORT READY`, and hold through the clap. Break the huddle once after preparing and confirm `TOO FAR` remains recoverable. Prepare/weather five claps and confirm the live `STORM PASSED!` scene precedes **Weathered The Storm**; any missed comfort should prevent **FLAWLESS**.

### Mark the Yard

Mark the Yard is a **territory-control** mission using `TerritoryMissionState`. Five territory zones
are spread across the yard. Entering one prepares it, but contact alone does not claim it: a player
must press **Interact** to perform the mark and turn it green. Each mark activates the squirrel's
reclaim prowl. Cheddar is presented as the route runner while Cocoa tracks the thief and **BARKS**
within range to drive it back to the yard edge, buying a short opening for the next mark. Both dogs
retain recovery access to the zones. Hold all five simultaneously to earn the live all-marked payoff;
the timer expiring with the squirrel chipping away is the fail.

Readable differences:

- The squirrel is repurposed as a territory rival that steals zones back rather than stealing food.
- The reclaim squirrel is now a generated/animated actor: idle/watch while waiting, a brief scared
  reaction when a dog marks territory, and a steal animation when it re-marks a zone.
- Cocoa's close bark visibly repels the squirrel, delays its next reclaim, credits her defense role,
  and points Cheddar toward the next marking opening; an out-of-range bark is harmless guidance.
- Zones recolor grey→green when held and flash "SQUIRREL STOLE IT!" when re-marked.
- Objective text tracks zones held and how many the squirrel has stolen back.
- Unique scoring/events include **ZONE MARKED**, **ZONE STOLEN** (penalty), and **YARD MARKED**.
- Clear banner: **YARD CLAIMED!**; end summary reads **Yard Is Ours** on a clear, **Squirrel Keeps Stealing It** when the squirrel chipped in.

The deterministic test/pacing hooks include `ForceClaimZone(dog)`, `ForceMarkInteraction(dog, zone)`,
`ForceMarkYardDefenseBark(dog)`, and `ForceSquirrelReclaim()`. Targeted
`MarkTheYardPlayModeTests` passed `9/9` on 2026-07-16. In normal play Interact claims a nearby zone,
Cocoa's bark repels a nearby squirrel, and the squirrel otherwise re-marks on a timer.

Manual check: press **0**, move Cheddar into a zone and confirm standing alone does not claim it;
press Interact and see it turn green. Move Cocoa near the incoming squirrel and bark to knock it back,
then use the opening to mark another zone. Confirm a stolen zone can be re-marked and the fifth mark
holds the live yard-owned scene before **Yard Is Ours**.

### Walkies on the Leash

Walkies on the Leash is a **tethered-coordination** mission using `LeashWalkMissionState` — the eleventh mission, reachable by arrow-selecting past Mark the Yard (the number row 1-9/0 maps to the first ten). The two dogs share one leash and must walk through four **CHECKPOINT** markers in order. The named scout alternates between Cheddar and Cocoa: that dog reaches the marker and **BARKS** the route call, creating the opening for both dogs to stand on the checkpoint together and bank it. Passive overlap does not advance the route. If they drift more than the leash length apart, it snaps taut (a rate-limited penalty); four snaps fail the walk. The dogs start side by side so the leash is slack — communicating who scouts and who follows is the challenge.

Readable differences:

- No squirrel/predator; the loop is paired movement under a distance constraint.
- Checkpoints light up in sequence; only the current one is active.
- Objective text tracks the current checkpoint and snap count.
- Unique scoring/events include **CHECKPOINT**, **LEASH SNAP** (penalty), and **WALK COMPLETE**.
- Clear banner: **WALK COMPLETE!**; end summary reads **Best Walk Ever** on a clear, **Tangled Leash** when the leash snapped.

The deterministic test/pacing hooks are `ForceReachCheckpoint()` and `ForceLeashSnap()` (`LeashCheckpoints` exposes the positions); in normal play both are driven by the dogs' positions and the distance between them.

Manual check: arrow to Walkies on the Leash, send the named scout to each checkpoint, and confirm the wrong dog's bark and passive two-dog overlap do not bank it. Have the named scout bark, bring the partner onto the marker, and verify the scout alternates on the next checkpoint. Confirm drifting apart snaps the leash. Reaching all four holds a short live **BEST WALK EVER!** payoff before the clear screen.

Checkpoint success now gives both dogs a brief proud pose alongside the score pop. A leash snap
produces a midpoint `LEASH SNAP!` warning, threat cue, rumble, and synchronized worried flinch before
normal walking resumes.

### Car Ride Chaos

Car Ride Chaos is the **vehicle** mission using `CarRideMissionState` — the twelfth mission (arrow-select), redesigned (2026-07-14) from the old average-position balance meter into a proper backseat stage. The dogs ride the rear bench home through a scripted ride of **seven road events** (turns and brake slams). The whole cabin set — shell, bench, and a sprite-masked scrolling windshield scenery strip — replaces the backyard for this mission, and the set visibly **tilts** while the car corners.

The three verbs:

- **Turns** (telegraphed `LEFT/RIGHT TURN AHEAD`): the cabin tilts and everything slides toward the outside of the turn. Cheddar slides hardest (chaos-puppy multiplier 1.25×), Cocoa least (0.85×). Getting pinned against the downhill door mid-turn is a **DOOR SQUISH!** tumble.
- **Sliding junk**: the loose **COOLER** and **TOY BIN** slide across the bench faster than any dog. A grounded dog in their path takes a **BONK!** tumble (with knockback stun); a **jumping** dog clears them with a `CLEAN HOP!` credit.
- **Brakes** (telegraphed `BRAKES AHEAD`): Cocoa must Interact first to plant (`COCOA PLANTS!`). Cheddar then moves within the partner-brace range and Interacts to tuck behind her (`TUCKED SAFE!`). The pair must remain together until the stop; Cocoa banks `ANCHORED!` and Cheddar banks `TUCKED SAFE!`, while a missing/broken handoff flings the exposed dog forward. Outside brake telegraphs, either dog can still brace against ordinary turn slide — but claws do not stop a sliding cooler, so jump those.

Co-op layer: each scripted brake now contains a mandatory Cocoa-anchor → nearby Cheddar-tuck handoff, with dog-specific objective arrows and recovery guidance. A **united bark** during cruise/telegraph additionally makes the driver ease off the gas for the next event (gentler tilt, slower junk), once per event. Five tumbles fails the ride; a clean event pops **SMOOTH!** with the proud pack pose, and finishing all seven banks **RIDE COMPLETE** before the held home-arrival scene.

Readable differences:

- No squirrel/predator loop; the dashboard **driver actor** telegraphs every event through its signal badge (calm `cruising` label between events, warning badge during telegraphs).
- Unique scoring/events: **SMOOTH!** (clean event), **BRACED** (per dog per brake), **TUMBLE** (penalty), **RIDE COMPLETE**.
- Clear banner: **MADE IT HOME!**; summaries read **Smooth Riders** (clean clear), **Home With Bruises** (clear with tumbles), **Car Sick** (failed).

Deterministic test hooks: `ForceCarEventSurvived()` / `ForceCarTumble(dog)` on GameManager, plus controller-level `ForceBeginRoadEvent(kind)`, `ForceResolveRoadEvent()`, `ForceBrace(dog)`, and `ForceTurnSlide(dt, kind)` (headless frame time can't accumulate slide distance, so slide/bonk/squish physics are tested through fixed-dt slide steps).

Manual check: arrow to Car Ride Chaos and confirm (1) the backyard is fully covered by the cabin set and the windshield scenery scrolls, (2) a telegraphed turn tilts the cabin and slides both dogs and the junk — with Cheddar visibly outsliding Cocoa, (3) jumping over the sweeping cooler avoids the bonk, (4) Cheddar-first and far-away brake attempts coach without arming, Cocoa Interact creates the anchor, nearby Cheddar Interact creates `TUCKED SAFE`, and breaking the pair before the stop flings Cheddar but recovers next brake, and (5) a united bark mid-cruise draws the `easing up` driver response and a gentler next event. Finish all seven and confirm the live `WE'RE HOME!` beat precedes the result card.

> **Mechanic-module coverage:** with Car Ride Chaos, all nine `ProductionMechanicModule` values (Herding, ThreatSweep, PatrolDefense, SharedObject, TerritoryControl, ScentSearch, RhythmPanic, VehicleBalance, LeashPhysics) keep at least one playable, tested mission.

### Kitchen Falling Food Frenzy

Kitchen Falling Food Frenzy is a compact counter-to-floor relay. Cheddar is the counter scout: he must reach the marked **COUNTER** route and **bark** to knock the next item loose. The bark first reveals a generated gold-food or purple-onion counter telegraph plus a matching pulsing floor landing cue, then releases the item after a readable delay. Cocoa is the floor sweeper: she tracks gold food into the marked **SAFE BOWL** to score, while purple onions should be dodged and allowed to splat.

- Dog-local arrows and the team route line always name the current jobs: **BARK-KNOCK FOOD / GUARD THE BOWL**, then **RESET AT COUNTER / CATCH GOLD IN BOWL** or **DODGE PURPLE** during the telegraph and fall.
- Good catches build a score combo. Missing good food, catching outside the bowl, eating an onion, or attempting the partner's role breaks momentum but remains recoverable.
- Gold food cues mean catch; purple onion cues mean dodge. Cocoa gets distinct proud/flinch poses, audio, pop text, and controller rumble for catch, dodge, splat, and gross-out outcomes.
- After three warm-up catches, a short **DINNER RUSH** finale calls a fixed **GOOD → BAD → GOOD** sequence with shorter telegraphs and faster falls. Each call still requires Cheddar's bark and Cocoa's correct catch/dodge response. A mistake retries the current call, so the finale adds pressure without an unrecoverable fail state. Five good catches plus the finale onion dodge clear the mission.
- The Kitchen stations now occupy a compact 13-unit vertical stage inside the arena. At the 16:9 couch target the shared camera needs at most `12` orthographic units to frame both stations, keeping both dogs and the landing lane readable without split-screen.
- No squirrel, predator, tug, or generic collectible loop runs during this mission.

Deterministic hooks are `ForceKitchenTelegraph(dog, kind)`, `ForceKitchenReleaseTelegraph()`, `ForceKitchenDrop(kind)`, `ForceKitchenCatch(dog, intoSafeZone)`, and `ForceKitchenLetFall()`. `KitchenFoodFrenzyMissionStateTests` covers bark/telegraph ownership, busy-state rejection, recoverable outcomes, finale sequencing, completion, and reset. `KitchenFoodFrenzyPlayModeTests` covers mission wiring, generated telegraph sprite swaps, warning objects, compact camera framing, good/bad feedback paths, the dinner-rush clear, and replay reset.

Manual acceptance check: arrow-select **Kitchen Falling Food Frenzy** with two local players. Confirm the initial shared camera keeps both dogs, the counter, and bowl readable. Move Cheddar into the counter route and bark; verify no item falls merely from standing there. Confirm the colored counter flash and landing circle appear before the item releases. For gold food, intercept it with Cocoa while she is inside the bowl and confirm **YUM**, proud pose, score, audio, and light rumble. For a purple onion, move Cocoa clear and let it splat; confirm **DODGED**, proud feedback, and no lost progress. Let one good item splat and deliberately eat one onion; confirm the warning/flinch feedback is distinct and both calls can be retried. After three warm-up catches, confirm **DINNER RUSH** announces **GOOD → BAD → GOOD**, uses visibly tighter timing, and still waits for a Cheddar bark before every call. Complete the three calls and confirm **KITCHEN CLEARED!**. Replay and confirm food, telegraphs, finale progress, combo, and feedback reset.

### Operation Pee Break

Operation Pee Break is the first controller-native deep slice. It uses four escalating exact-message
beats: Cocoa's door stare; Cocoa's stare plus Cheddar presenting the leash; the role-flipped charger
gambit where Cheddar blocks the hallway while Cocoa unplugs the charger; and the final stare, leash,
and near-door united bark. The mission picker presents it as an `8m` couch-test focus slice rather
than a short arcade round. Controller-owned world meters expose bladder pressure and phone battery,
while the Teenager changes pose/state with each beat. The phone now drains only while Cocoa actively
holds the unplug station and visibly powers down when the charger gambit succeeds. Wrong or
  incomplete signals reset comprehension without failing the mission. A painterly 16:9 base room
  plate now establishes the interior at full camera scale and owns the closed front door; there is
  no active separate closed-door sprite. The former procedural wall, wood floor, baseboard, window,
  side table, door frame, and door slab are fallback-only when that asset loads. The human-scale
  distracted Teenager and leash remain active controller-owned overlays before success. The
  standalone couch stays fallback-only because the Teenager art includes its seat, and the
  standalone phone/charger appears only during the actionable Beat-3 gambit. After the final bark, a
  matched success room plate replaces the base and embeds the open doorway plus standing Teenager;
  the isolated open-door sprite is fallback-only if that success plate is absent. Controller-owned
  markers keep gameplay authority, and older child silhouettes stay alive for lifecycle coverage
  without rendering through loaded replacements. Pass 2 originally added lived-in and state-change details: couch
pillows, side-table cup, Teenager hoodie/AirPod/thumb animation, phone-attention and door-attention
beams, question/OH bubbles, a phone battery fill plus charged/dead states, plugged/unplugged charger
ends, presented-leash trail, in-world comprehension/confusion progress bars, four beat pips, bladder
urgency accents, an open-door outdoor-view panel, and a climax-only grass/hydrant/relief-sparkle
payoff. The Teenager is staged at human scale relative to the dachshunds: the seated overlay owns
phone-idle, annoyed-glance, and distracted-again reads, while the success plate owns the standing
pose. Production station pads remain hidden throughout normal play, including when a dog approaches;
close-range explanatory labels can appear without restoring the large circle. Dog-local arrows,
small command signals, and prop/Teenager reaction pulses carry the active order. The F1 playtest
overlay restores the full debug labels/rings for review. Beat 3 and Beat 4 still mirror the Beat 2
missing-partner hints through close-range prompts: if only one dog is doing the charger or final
door/leash job, the station and Teenager labels name the absent partner action before players need
to parse the HUD. The
backyard shell now gives the door payoff context with a visible house/patio district, back-door
window/knob, outdoor path, central lawn, route dashes, snack district plate/crumbs, and laundry
line/sock cues. The end card now uses Pee Break-specific replay copy: clean runs earn **Pee Break
Pawfect**, one-misread recoveries become **Outside, Eventually**, and role completions credit the
dogs so the MVP line reinforces Cocoa/Cheddar contributions instead of generic score text. Recovered
  runs also surface **Replay target: Pawfect signal - 0 misreads** so the first couch test has an
  obvious rematch goal. The final united bark now leaves the live room visible for a 1.15-second
  controller-owned relief beat before the end card; timeout pressure is suspended after success has
  been earned.

Two optional controller-owned floor toys now make exploration playful between puzzle beats: either
dog can use Interact near the tennis ball or squeaky toy to bat it across the room. They have no
score or mission-state effect. The normal HUD reads bladder pressure as a persistent colored
**BLADDER EMERGENCY** bar without a percentage.

**CF1.8 (finding #10, UNVERIFIED: "Didn't see this working or not...")** made the toys
discoverable without giving them any mechanical weight: the first time either dog gets within
`ToyInteractRange + 1` of either toy (whichever is approached first, once per mission), a one-shot
world pop reads **"TOYS! (just for fun)"** with a wiggle pulse on that toy, and it never fires
again that mission - it is one reassuring aside, not a repeated nag per prop. Independently, an
untouched toy (not currently mid-kick) gets a gentle idle scale/tint wobble roughly every 10
seconds so both toys read as interactive at rest even before that discovery moment. Both cues are
purely cosmetic - they never touch toy position/velocity, and a kicked toy's physics (from
`AdvanceToy`) always takes priority over the idle wobble.

Manual acceptance check: arrow-select **Operation Pee Break** with two local players and keep the F1
playtest overlay off for the first cold read. Confirm Beat 1 reads from the seated phone-absorbed
Teenager, the base plate's closed door, and hanging leash before any large label appears, with no
duplicate couch, phone, or door sprite. Confirm the painterly room fills the camera with no backyard
leak or wall/wood-floor/window rectangles showing through. Move Cocoa near the door
and Cheddar near the watch spot to verify only close-range prompts appear and no large colored circle
returns. Confirm the door/Teenager visibly pulse when Cocoa supplies the correct stare, then confirm
the beat advances from Cocoa's stare alone. During the cold run, press **F4** the first time either player asks
"what do I do?" so the playtest overlay log captures the active objective, team guidance, and dog
positions. In Beat 2, verify neither the stare nor leash alone solves the message.
Confirm the Teenager and station prompts name the missing partner job (**NEEDS LEASH TOO** /
**NEEDS STARE TOO**) only when players are close enough or the debug overlay is enabled.
Intentionally bark early until the Teenager misreads the request and confirm the striped tennis-ball
**WRONG IDEA / TRY DOG JOBS** gag is readable and the run continues. In Beat 3, have Cheddar leave the
hallway while Cocoa holds the charger and confirm comprehension falls; then hold both roles to
advance. Confirm Cheddar-only blocking calls out **NEEDS COCOA CHARGER**, Cocoa-only unplugging calls
out **NEEDS CHEDDAR BLOCK**, the phone stays charged while Cheddar blocks alone, drains while Cocoa
unplugs it, and reaches **DEAD** with an empty battery fill when the gambit succeeds; the dead-phone slash and unplugged plug should
replace the charged/plugged read. In Beat 4, confirm door-only/leash-only setup calls out the missing
partner, then hold the door/leash positions and bark both dogs within the timing window. Confirm the
base room swaps cleanly to the authored success plate with its open doorway and standing Teenager,
plus the sunbeam, grass/hydrant relief gag, and celebratory sparkles. The mission clears, the end card
shows Pee Break-specific result copy plus dog contribution credit, and replay resets beat, bladder,
battery, misreads, and door state. The open-door/hydrant scene must remain live long enough to read
before the end card replaces it. Before solving, walk a dog near either toy for the first time and confirm the one-shot **"TOYS!
(just for fun)"** pop and wiggle appear, then confirm it does not repeat on later approaches to
either toy; also confirm an untouched toy gets a subtle idle wobble/glint after sitting still a
while. Have each dog bat one of the two toys and confirm it moves without advancing a beat.
Confirm the top HUD bladder bar fills without showing a changing percentage in the objective, and confirm the opening card makes both keyboard layouts and the
Switch-style Y/X/A/B mapping understandable without verbal coaching.

Automated pre-couch gate: run both Pee Break PlayMode rehearsals before handing controllers to
players:

- `PeeBreakPlayModeTests.ObserverRehearsal_ColdPathSurfacesBeatOneBeatTwoAndFirstEarlyBarkMisread`
- `PeeBreakPlayModeTests.LiveRehearsal_BeatThreeBeatFourUsePositionsBarksAndRecoveries`

Together they verify the couch-readiness surfaces from cold read through the live-input climax:

- Start state includes the loaded full-frame base room plate, including its baked closed door, with
  primitive wall/wood-floor/window/door-slab renderers hidden and the leak-prevention foundation
  behind it. The seated Teenager and leash are visible; the duplicate couch and isolated open-door
  fallback remain hidden on the normal asset path. The progressive phone/charger waits for Beat 3,
  while the success plate and sunbeam/outdoor grass/hydrant/sparkle payoff wait for the climax.
- Beat 1 shows Cheddar's **WATCH COCOA / NO BARK** arrow, Cocoa's **HOLD DOOR STARE** arrow, the
  visible Cheddar watch pad, Teenager **SCROLLING** feedback, and F4 overlay guidance.
- Beat 2 partial success shows Cocoa locked at the door, the Teenager/station **NEEDS LEASH TOO**
  missing-partner hint, and a cold-read recorder entry with the Beat 2 objective and team guidance.
- Beat 2 full success shows both door/leash stations ready, Teenager **GETTING IT**, then advances
  to the charger-gambit arrows and world labels.
- The first premature bark after Beat 2 produces the recoverable **TENNIS BALL / WRONG IDEA / TRY
  DOG JOBS** gag, keeps the mission in progress, and records the active charger-gambit guidance.
- Beat 3 is rehearsed through dog positions: Cocoa alone on the charger drains the phone without
  solving, leaving the charger stops drain, charger outlet/plug details are present, Cheddar leaving
  the hallway drops comprehension, holding both roles advances to the final setup, and the phone/plug
  show dead/unplugged state changes. Cheddar-only and Cocoa-only partial states name the missing
  partner job in world labels, while correct and wrong signals visibly change the Teenager
  comprehension/confusion read before the HUD is needed.
- Beat 4 is rehearsed through the real bark event path: Cocoa holds the door, Cheddar holds the
  leash, door-only/leash-only setup names the missing partner, both dogs bark near the door, the
  sustained united-bark read opens the door, the controller shows the sunbeam plus grass/hydrant/
  sparkle payoff through the 1.15-second success hold before end-screen cleanup, the end card names
  the Pawfect/0-misread replay target or beaten challenge, and replay resets the door, bladder,
  phone, misreads, arrows, and world labels.

### Baby Bird Bedlam

Baby Bird Bedlam (2026-07-12, owner-requested) is the roster's Feast-and-Fend slice: chicks tumble
out of the big oak nest and the dogs' prey drive takes over. Roles are hard-locked to keep the
Cheddar/Cocoa identity split honest. **Cheddar is the feaster** - he grabs each landed chick
(Tug/Rescue near the `GRAB IT!` marker) and shakes it down (`Tug/Rescue x3`, the last shake is a
cartoon `GULP!`). While his mouth is full he cannot defend himself. **Cocoa is air defense** - when
`PARENT BIRD DIVE - SNATCH INBOUND!` flashes on the parent-bird actor (the shared predator actor with
eagle motion art), she must get under the diving parent and **bark** inside its dive window to repel
it. An un-repelled dive lands a `PECKED!` hit: Cheddar drops the chick, it flutters home, and three
pecks fail the run. A grounded chick nobody grabs for 8 seconds is `AIRLIFTED!` back to the nest (a
score/mistake sting, not a fail). Eating 4 chicks clears the mission; gulping the final shake mid-dive
cancels the dive with nothing left to save. Scoring: `CHICK NABBED` +25, `CHICK GULPED` +150 (credits
Cheddar), `PARENT REPELLED` +125 (credits Cocoa), `PECKED` -50, `CHICK AIRLIFTED` -25,
`NEST FEAST COMPLETE` +500. Deterministic logic lives in `CoopFeastGuardPuzzle`
(grab/shake/dive/repel/airlift lifecycle) behind `BabyBirdBedlamMissionController`; the mission-select
tile is now a painterly production portrait matching the other Adventure Library cards. The former
flat generator writes only `ReferenceOnly/GeneratedMissionTiles/babybirdbedlam_placeholder.png`, so
running it cannot replace the runtime portrait.

Wrong-role inputs stay recoverable under pressure: Cocoa inspecting a grounded chick leaves it for
Cheddar, Cheddar's mouth-full bark cannot repel a dive, and Cocoa barking outside repel range coaches
her underneath the parent without consuming the active dive window.

Manual acceptance check: select **Baby Bird Bedlam** with two local players. Confirm the nest, first
falling chick, and `THE NEST` label read cold; Cheddar's arrow points at the nest, then the falling
chick (`CHICK INCOMING`), then the grounded chick (`GRAB THE CHICK`), while Cocoa's arrow tracks
Cheddar (`GUARD THE SKY` / `GUARD THE FEAST`). Grab a chick and confirm each shake pops `SHAKE!` with
rumble and the third pops `GULP!` with the eating audio cue. Let a dive land once: confirm the
`DIVING!`/`PECKED!` reads, the camera jolt, and that the chick despawns and a fresh one drops. Repel a
dive with Cocoa's bark in range and confirm `REPELLED!` plus the parent retreating to the perch.
Ignore a grounded chick for 8 seconds and confirm the `AIRLIFTED!` gag. Fail with three pecks and
confirm the end card reads **Pecked Out Of The Yard** with the dive-bomb fail reason; clear and
confirm **Nest Feast** (or **Nest Feast, No Feathers Lost** flawless) plus MVP credit naming a dog.

### Skunk Blast Mayhem

Skunk Blast Mayhem (2026-07-22, idea-bank #22) is the roster's lure-and-snatch heist slice: a skunk
guards a prized dead bird and the shared predator actor stands in for it (reusing its transform and
collision the same way Baby Bird Bedlam's parent bird reused the eagle actor). Roles are hard-locked.
**Cheddar is the lure** - only his bark within range holds the skunk's attention (`LureActive`);
Cocoa barking there gets coached back into staying quiet. **Cocoa is the snatcher** - only she can
Interact at the guarded bird to secure it, and only while the skunk is turned away (mid-lure) and its
tail is down. The **tail-lift telegraph** is a shared danger clock, not a per-dog stat: it fires on
its own clock regardless of who is luring, and whoever is still within blast range when the window
closes gets **SKUNKED** - Cheddar's blast radius (3.4) is wider than Cocoa's (2.0), so he is
mechanically the one more likely to still be in the cone if the pair reacts late. A skunked dog goes
**STINKY**: it can no longer lure or snatch until it rubs itself clean at the laundry pile (`Interact`
near the pile, `RubsToClean` = 3 hits), while the clean partner hauls fresh laundry from the basket to
the pile to keep the supply from running dry. Run the whole economy (pile + basket) completely dry and
a stinky dog still clears eventually through a slow passive air-dry trickle - a costly detour, never a
dead end. Getting skunked, even both dogs at once, is fail-forward: the only hard fail condition is the
shared round timeout. Scoring: `CLEAN BAIL` +60 (team), `SKUNKED` -60 per dog, `FRESH AGAIN` +80 (team,
per dog cleaned), `LAUNDRY HAULED` +20 (team), `BIRD SECURED` +500 (team, clears the mission).
Deterministic logic lives in `CoopSkunkHeistPuzzle` (telegraph/spray/grab/rub/haul/air-dry lifecycle)
behind `SkunkBlastMayhemMissionController`; the tail-lift danger clock is also mirrored through the
shared HUD pressure meter (`IMissionPressureHud`) so both dogs can read it without staring at the
skunk's sprite. The mission-select tile is a generated greybox placeholder
(`tools/art/generate_skunk_blast_mayhem_tile.py`) per the gameplay-first greybox pivot, not yet a
painterly production portrait.

Wrong-role inputs stay recoverable: Cocoa's bark near the skunk coaches her to stay quiet instead of
lures; Cheddar's Interact on the guarded bird coaches him to let Cocoa make the grab; a stinky dog's
bark/Interact for its old role coaches toward the laundry pile instead of silently failing.

Manual acceptance check: select **Skunk Blast Mayhem** with two local players. Confirm the skunk,
guarded bird, laundry basket, and laundry pile read cold, and Cheddar's arrow points at the skunk
(`BARK TO LURE`) while Cocoa's tracks the bird (`WAIT FOR THE LURE` / `SNATCH THE BIRD`). Bark near the
skunk with Cheddar and confirm the `LURED!` pop and Cocoa's arrow copy changing. Let the tail-lift
telegraph fire once with both dogs clear of blast range and confirm the `CLEAN BAIL!` pop; let it fire
again with a dog in range and confirm the `SKUNKED!` pop, the camera jolt, and that dog's arrow
re-routing to the laundry pile. Rub clean at the pile and confirm the `FRESH AGAIN!` pop and the arrow
returning to the heist. Complete a lure-then-snatch and confirm `BIRD SECURED!`, both dogs' proud pose,
and MVP credit naming Cocoa.

### Tick Invasion

Tick Invasion (2026-07-22, idea-bank #21) is the roster's mutual-grooming survival slice: the
backyard has exploded with ticks and both dogs accumulate them continuously and asymmetrically -
**Cheddar's chaos-puppy energy** attracts ticks faster (0.026/s) but his groom action clears more per
hit (high risk, high output); **Cocoa's veteran composure** accumulates slower (0.017/s) and her
groom reaches from a wider radius (2.6 vs Cheddar's 1.8 - long-dog advantage). The only defense is
`Interact`-to-groom a nearby partner: it reduces their ticks and transfers a small amount to the
groomer, the resource-transfer pressure the design calls for. A dog whose ticks cross the erratic
threshold (0.62) goes **ERRATIC** and resists a normal groom until the calm partner barks them still
(opens a brief hold window so the very next groom connects) or their ticks drop back under the
threshold on their own. Either dog can `Interact` at the backyard **pool** for an instant reset to
zero, but emerges wet: double tick accumulation for a per-dog recovery window during which they
cannot effectively groom anyone - Cocoa's window (6s) is shorter than Cheddar's (10s; he belly-flops
and is useless for longer). Partway through the round a **Super Tick** locks onto one dog exclusively:
grooming cannot touch it from either direction (the targeted dog is too overwhelmed to groom their
partner either, so the partner has zero groom relief during the window), and only a pool dive clears
it. Unlike Skunk Blast Mayhem, this mission has a real hard fail: either dog's ticks maxing out ends
the run immediately, with a distinct per-dog reason (Cheddar's messy zoomie collapse vs Cocoa's
dignified, furious stillness). Clearing means surviving the full round (82 of the shared 100s) without
either dog ever maxing out. Scoring: `GROOMED CLEAN` +25 (team, per landed groom), `TICK OVERLOAD` -50
per erratic crossing, `POOL DIVE` +15 (team), `SUPER TICK SHAKEN OFF` +150 (team), `INFESTATION
CLEARED` +500 (team, clears the mission). Deterministic logic lives in `CoopTickInvasionPuzzle`
(tick accumulation/groom/bark/pool-dive/Super-Tick/survive lifecycle) behind
`TickInvasionMissionController`; both dogs' live tick percentages are mirrored through the shared HUD
pressure meter (`IMissionPressureHud`, surfacing whichever dog currently reads worse) plus small
per-dog world labels so the meter never depends on staring at one sprite. A readable "worst
infestation" callout (Cocoa's veteran read spotting the more-covered dog first, per her queen-composure
trait) fires whenever the two dogs' tick levels diverge enough to matter. The pool marker reuses the
existing dog-bowl prop art (no bespoke groom/pool/Super-Tick art yet) and the mission-select tile is a
generated greybox placeholder (`tools/art/generate_tick_invasion_tile.py`) per the gameplay-first
greybox pivot, not yet a painterly production portrait.

Manual acceptance check: select **Tick Invasion** with two local players. Confirm both dogs' tick
labels read `TICKS 0%` cold and the pressure HUD reads `Cheddar TICKS 0%`. Stand close and Interact to
groom and confirm the target's percentage visibly drops while the groomer's ticks up slightly. Let a
dog's ticks climb unchecked (no grooming) until `ERRATIC!` pops and confirm a bare groom attempt is
coached back ("too erratic to lock down"); bark near the erratic dog and confirm the groom now lands.
Dive the pool and confirm the instant reset pop and the wet tint on that dog's tick label. Let the
round reach its Super Tick trigger and confirm the `SUPER TICK!` pop, that dog's groom attempts failing
in both directions, and a pool dive clearing it. Let a dog's ticks max out and confirm the end card
reads a per-dog fail reason (Cheddar messy vs Cocoa dignified); clear a full round with steady mutual
grooming and confirm `INFESTATION CLEARED!` plus MVP credit naming a dog.

### Burr Maze

Burr Maze (2026-07-22, idea-bank #23) is the roster's first patrol-stealth slice: a hedge maze at the
back of the yard, patrolled by the neighbor's territorial cat (the shared predator actor, reused the
same way Skunk Blast Mayhem's skunk and Baby Bird Bedlam's parent bird reused it). The cat sweeps a
facing-direction **vision cone** (angle + max distance, `CoopBurrMazePuzzle.IsInCone`) along its patrol
lane; a dog lingering in the cone for a brief dwell (not instant - `NoticeDwellThreshold`) gets
**SPOTTED** - both dogs are swept back to the last banked checkpoint, with **zero burr penalty and no
lost progress**. This is a genuine third failure pattern in this roster, distinct from Skunk Blast
Mayhem's fail-forward de-skunk detour and Tick Invasion's hard fail: a pure stealth-retry loop.
**Cheddar's** reckless bramble charges cake him in burrs much faster (0.16/s vs Cocoa's 0.07/s) and his
baseline notice-dwell accrues faster too (louder, easier to spot, 1.25x vs 1.0x) with a shorter warning
window before detection; **Cocoa's** veteran read gives her an earlier "about to be spotted" telegraph,
and she accrues burrs and notice-dwell more slowly. A dog whose burr meter crosses 0.55 is **CAKED**: a
real movement-speed penalty (`DogController.SetSpeedPenalty`, 0.6x) plus a widened notice-dwell
accrual (1.6x) - a genuine status effect, never a fail condition on its own. Burrs only come off via a
partner-held **burr-pick** (`Interact`, gradual while held, mirroring Gate Crash's toggle-on/
auto-release hold shape): both dogs must stay stationary, in range, and unhidden, or the hold breaks -
and a patrol detection interrupts it outright. Ducking into a **HIDE HERE** bush hard-resets a dog's
in-progress notice-dwell, the core MGS-style tool. Barking while NOT hidden **lures** the cat's
attention toward the barking dog's position for a few seconds, at the cost of that dog's own exposure -
it deliberately does not reset the barker's own dwell. Partway through the round (20s) a second, faster
**kitten** patrol activates on a distinct real route (vertical vs the cat's horizontal lane), forcing
the pair to route around whichever patrol currently covers the "safe" path. Scoring: `CHECKPOINT
REACHED` +90 (team, per checkpoint), `BURRS PICKED CLEAN` +60 (team), `MAZE CLEARED` +500 (team, clears
the mission). Deterministic logic lives in `CoopBurrMazePuzzle` (cone-notice math, burr accrual/
reduction, checkpoint state, patrol-route state) behind `BurrMazeMissionController`; both dogs' live
burr percentages are mirrored through the shared HUD pressure meter (`IMissionPressureHud`, surfacing
whichever dog reads worse, pre-empted by an "ABOUT TO BE SPOTTED!" callout when either dog nears
detection). Hiding bushes reuse the existing `Bush` prop art (the same cover fiction Backyard Rescue's
environment pass uses it for); bramble patches reuse the generic `Grass` patch art tinted into a
bramble read (first use in the roster); the cat/kitten patrol on the shared/generated predator actor
art with no bespoke sprite yet, and the mission-select tile is a generated greybox placeholder
(`tools/art/generate_burr_maze_tile.py`) per the gameplay-first greybox pivot, not yet a painterly
production portrait.

Manual acceptance check: select **Burr Maze** with two local players. Confirm the cat patrol, its
bushes, bramble patches, and hedge-corner checkpoints read cold, and both dogs' `BURRS 0%` labels show
clean. Stand ahead of the cat's sweep and confirm the pressure HUD escalates to `ABOUT TO BE SPOTTED!`
before a `SPOTTED!` pop sends both dogs back to the last checkpoint - not a mission fail. Duck into a
`HIDE HERE` bush mid-sweep and confirm the notice resets. Walk Cheddar through a bramble patch and
confirm his `BURRS` percentage climbs faster than Cocoa's under the same exposure; let him cross the
caked threshold and confirm he visibly slows and rustles wider. Stand both dogs still and Interact to
pick a caked partner clean and confirm the meter drops over a held few seconds; walking away or getting
spotted mid-pick should cleanly interrupt it. Bark near the cat while not hidden and confirm the
`LURED!` cue and the cat's attention visibly swinging that way. Reach the round's 20s mark and confirm
the second, faster kitten patrol appears on its own route. Reach the final checkpoint and confirm
`MAZE CLEARED!` plus MVP credit naming a dog.

Historical automated baseline: Unity 6000.0.65f1 batch PlayMode run passed `400/400` tests on
2026-07-01 after the generated P0 mission-state art pass. The targeted presentation
coverage inside `PeeBreakPlayModeTests`, `BackyardEnvironmentPlayModeTests`,
`ArenaGameLoopPlayModeTests`, `KitchenFoodFrenzyPlayModeTests`, and
`CoopChaosMachinePlayModeTests`, and `FinalArtIntegrationPlayModeTests` now asserts Pass-2 room state
props, outdoor payoff context, backyard districts, route cues, mission badge metadata, cosmetic wow
set dressing, dog-local personality polish, generated environment overlays, generated objective/range
cue sprites, generated dog-local FX sprites, generated Kitchen falling-food cue sprites, generated
Chaos Machine junction prop sprites, generated building/home-exterior sprites, generated HUD skin
sprites, generated world-label skin sprites, generated arena SFX profiles, mission-specific
collectible prop sprite stability after the dynamic treat enhancer scan, generated P0 mission-state
prop packs, and squirrel/eagle/coyote motion resources plus runtime actor switching.

For gameplay-first iteration, generate the editor-only lab with
**Cheddar & Cocoa > Gameplay First > Build Generated Playtest Lab**. Use it to adjust primitive room
layout, role pads, co-op beat lanes, and couch-playtest notes before investing more time in realistic
backgrounds or photo-derived scene dressing.

## Level scale and camera

The backyard is built at **120 x 68 world units**. A runtime dachshund is roughly 2 units long, so a dog occupies about **1.7% of the property width** instead of reading as a giant character in a single-screen demo box. The dogs spawn near the center (`±10, 0`) and mission routes, hide zones, fence gaps, dig sites, loose weenies, territory zones, and leash checkpoints are distributed using normalized coordinates across 70–94% of the yard width.

Because this is one-screen couch co-op, the shared camera is a **dynamic clamped follow-cam with two meaningful modes**. When the dogs regroup it uses a local scrolling view (`7.5` ortho, about 27 units wide at 16:9), revealing less than one quarter of the property width. When they split up it can pull back to a strategic full-yard view (`34` ortho), keeping both players visible. Patio/house, pond, garden, shade tree, central lawn, picnic, sandbox, stepping-stone path, snack/laundry districts, mission route dashes, and fence dressing divide the property into recognizable districts so the extra traversal space is not an empty green plane.

Outdoor level scale is now a production contract: dogs should remain at or below 2% of a major level's width, close framing must require scrolling, and important mission geometry must span most of the playable bounds. Pool and other future outdoor levels should follow this contract rather than inherit the frozen Canvas prototype's oversized dog-to-feature ratio.

## Objective

The round can end in **LevelClear** or **GameOver**, and either result can be restarted. Current pacing is hand-tuned for a first two-player playtest: `90 / 70 / 55` second mission timers, a 5-second mission intro banner, delayed first squirrel pressure, an ~25-second predator telegraph in Backyard Rescue, and a short but readable tug charge so both players have to stay committed for a moment.

The opening briefing says **P1 CHEDDAR • P2 COCOA** and invites either bark to skip the discovery
beat. During play, the bottom identity chips keep player ownership and the current control source
visible while the progressive Backyard Rescue card teaches Switch-style Y/X/A/B (or each player's keyboard
fallback) one action at a time. The top HUD carries only mission, objective/progress, timer, and
score. For the first few seconds, the squirrel is **WAITING**, the predator is **OFFSCREEN**, and
the rope waits for both dogs. This is intentional: players should first read their spawn, dog
identity, first objective arrows, and shared fantasy before threat or diagnostic text competes for
attention.

The end loop is intentionally simple: players see current score, the latest score swing, a short reason line, session totals, and Replay / Next Mission / Mission Select actions. Score deltas now appear both as a brief HUD pop and as small world text near the action so cause/effect is easier to read during chaos. The exposed deterministic state includes `CurrentFlow`, `MissionSelectVisible`, `SelectedMissionVariant`, `Score`, `LastScoreDelta`, `LastScoreEventLabel`, `LastScorePopLabel`, `ScorePopVisible`, `ObjectiveLabel`, `Outcome`, `EndRank`, `EndSummaryLabel`, `EndReasonLabel`, `ReplayPromptVisible`, `EndReplayAvailable`, `EndNextMissionAvailable`, `EndMissionSelectAvailable`, `SessionMissionsPlayed`, `SessionTotalScore`, `SessionStarsEarned`, `SessionUniqueMissionsCompleted`, `SessionSummaryLabel`, `LastJuiceFeedback`, and `LastJuiceLabel`.

## Controls

| Player | Dog | Controller | Keyboard | Bark | Interact | Jump | Wrestle |
| --- | --- | --- | --- | --- | --- | --- | --- |
| P1 | Cheddar | Gamepad slot 0 | WASD | Space / Y (West) | E / X (North) | Left Shift / A (East) | Q / B (South) |
| P2 | Cocoa | Gamepad slot 1 | Arrow keys | Enter / Y (West) | Right Shift / X (North) | Right Ctrl / A (East) | Right Alt / B (South) |

Mission flow controls:

- Mission select: keyboard **arrow keys** or gamepad **D-pad** move through the visible 4x3 page;
  up/down wraps within a page column and left/right walks the tile order across page edges.
  **Enter**, **Space**, gamepad **Start**, or gamepad **South** starts; **1-9 and 0** starts a mission
  directly. **Play Recommended** starts Operation Pee Break in one click.
- During a run: keyboard **1-9 and 0** still restarts the arena into Backyard Rescue, Snack Heist, Sock Panic, Squirrel Conspiracy, Eagle Shadow Panic, Coyotes at the Fence, Weenie Roundup, Scent Search, Thunderstorm Comfort, Mark the Yard, Walkies on the Leash, or Car Ride Balance (arrow-select) for quick manual comparison.
- During a run: **Escape** or gamepad **Start** pauses. Use D-pad/arrows to choose and A/Enter to
  activate. The pause card toggles Audio, Rumble, and Camera Shake; can skip/replay the Backyard
  tutorial; and can Resume, return safely to Mission Select, or Quit. B/Start resumes, and pausing
  freezes mission time and dog input.
- End screen: **R / Enter / Start / South** replays; **N / Right Arrow / Right Shoulder / D-pad Right** advances; **M / Escape / East / D-pad Left** returns to mission select.
- Session Summary: **Enter**, **Space**, **Start**, **South**, or **Right Shoulder** continues to the next unfinished mission; **M**, **Escape**, or gamepad **East** returns to mission select. Actually clearing every mission in the current roster (not just attempting each one) changes Continue to an explicit **Victory Lap** that wraps to mission one; **New Session** clears all local results.
- Playtest Mode: press **F1** / **`** to reveal the compact top-right observer overlay. The
  bottom-left Playtest Mode button exists only while diagnostics are already visible, so it cannot
  clutter a player's cold read. The overlay does not pause or block normal play.
- Comfort/observer toggles: the pause card exposes Audio, Rumble, and Camera Shake. Observer
  hotkeys **F2** and **F3** still toggle audio and rumble directly; both default to on.

## First playtest protocol

Use this protocol before changing tuning again:

1. Open `unity/CheddarAndCocoa` in Unity 6 LTS, open `Assets/Scenes/ArenaScene.unity`, and press
   Play. Leave Playtest Mode off for the players' cold read; the observer may press F1 later when
   state evidence is needed.
2. Start on mission select. Do not explain the game at first; let the two players try to identify their dogs, choose a mission, read the P1 Cheddar / P2 Cocoa briefing line, move, bark, and react.
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

- The new room/mission portraits and many gameplay props are coherent generated production
  candidates, but character/prop animation and final mix are still incomplete.
- The progressive action tutorial is deliberately limited to Backyard Rescue; other missions still
  depend on their team-plan preview, opening briefing, staged props, state changes, and contextual
  arrows/prompts.
- Keyboard two-player controls share one keyboard, so controller play is the better test if two gamepads are available.
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
7. Launch the built app, start each mission from mission select, and press **F1** / **`** to verify
   the observer overlay is available while remaining absent from ordinary play.

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

Manual check: press **F1** during mission select and during play. Confirm the overlay and its
bottom-left close toggle appear only after that request, do not hide the dogs or end buttons, and
update after collecting an item, barking, missing an interaction, forcing a fail/clear, replaying,
and choosing Next Mission. The overlay also reports whether audio and rumble are enabled plus the
latest requested cue/request name.

F1 also restores the full world-label map, actor-state support text, dog-arrow support text, and
range-ring support text. With the overlay off, mission labels created through the shared label path
should behave as close-range contextual prompts rather than permanent objective text, mission actors
should communicate mainly through silhouette/sprite state/color/pulse/motion, dog-mounted objective
arrows should guide primarily through the generated icon, and bark/tug/rescue rings should read
through their icon cue rather than explanatory copy.

Urgent actor states additionally raise an icon-only **actor signal badge** (2026-07-02): when a
`SetActorState` call authors an urgency pulse of `0.26+` (squirrel actively stealing, predator
warning/attack, coyote breach/final push, eagle sweep, Mark the Yard's steal prowl), `ActorSignalBadge` shows the authored
`world_label_warning`/`world_label_command` skin sprite above the actor with **no text**, visible at
any distance and independent of the close-range text contract. Calm/aftermath states (waiting,
dropped, trapped, yeeted, retreats) stay below the threshold and never raise it. Manual acceptance
check: start Backyard Rescue, park both dogs at the fence far from the squirrel, and wait for (or
F5-force) a steal — the warning/command icon should bob over the squirrel and read from couch
distance with no sentence text; trap or bark-scare the squirrel and the icon should drop
immediately. Force a predator warning and confirm the warning icon rides the shadow until the
huddle double-bark yeets it.

The 2026-07-03 pass raised the remaining timed act-now windows into the same urgency channel, and
labels ending in `NOW!` now classify as the command skin: Sock Panic's open-basket dive window
raises a command badge until the sock is grabbed or the basket flops shut; Eagle Shadow's talon-grip
rescue raises a warning badge while Cheddar wiggles and swaps to a command badge during Cocoa's
cracked-grip pull window. Car Ride's redesigned driver actor now raises the warning badge for turn
and brake telegraphs while the shared `SLIDE FORCE` meter mirrors actual cabin tilt. Manual
acceptance check: tip the basket and watch the command icon from across the yard; get snatched in
Eagle Shadow and confirm the warning→command icon swap tracks the wiggle/pull rhythm; in Car Ride,
confirm the driver badge appears before the set tilts and the meter rises with the turn slide.

The 2026-07-04 pass extended the same distance signal to **station markers** (they have no
`MissionActorFeedback` pulse channel, so mission controllers drive
`ActorSignalBadge.SetStationSignal` directly when a marker becomes or stops being the active
objective): The Great Escape's active chain station carries a command badge that follows the chain;
The Rube Goldberg's lever carries a command badge until pulled, the live cascade's current junction
carries it while rolling, and a misfire swaps the jammed junction to the warning skin while the
lever re-raises its re-pull command; The Bone Detail alternates the command badge between the scent
post (while Cocoa owes a sniff) and the called mound (after the reveal); Squirrel Conspiracy's
active hold-cutoff zone carries a command badge; and Backyard Rescue's escape gap carries a command
badge that drops while the gap dog is actually standing in it; Walkies on the Leash's current
checkpoint carries a command badge that walks the route; and Operation Pee Break's beat stations
(door stare, watch pad, leash, hallway block, charger) each raise a command badge during their beat
that drops while their dog is actually holding the spot. The two-station bait/hold puzzles follow
the same alternation: Gate Crash signals the gate until Cocoa braces it, then the toy while the
squeeze window is open; Table Stealth signals the human until a distraction runs, then the steak
while the sneak window is live; The Ol' Switcheroo signals the decoy until the squirrel commits,
then the stash during the raid window; The Walk Campaign signals each half of the exact-combo
message until its dog is sending it. Weenie Roundup's home bowl rides the actor pulse channel
instead: a weenie in transit raises a `BRING IT NOW!` command badge over the bowl that drops on
delivery or fumble.

Gate Crash now preserves its earned climax in the live world for `1.15` seconds before the shared
result card appears. During that controller-owned hold, the squeeze meter hides, the toy swaps to
its claimed state, the objective credits both roles, and **TOY RESCUED!** appears at Cheddar's side
of the gate. Timeout pressure freezes through the payoff, and replay resets the hold alongside the
gate, toy, squeeze progress, and snap count.

The opening is now a deliberate authored handoff instead of a proximity trigger. Cocoa must reach
the brace pad and press **Interact**; only then can Cheddar build squeeze progress. Walking Cocoa
away snaps the gate, erases the partial crossing, and requires another Interact to re-anchor. A
Cheddar-first or out-of-range attempt gives role/range coaching without a penalty. Snaps carry an
immediate threat cue and rumble, while the rescued-toy payoff carries a distinct mission-win cue and
shared rumble.

Table Stealth now turns both advertised distractions into live, asymmetric player verbs. Cocoa must
reach the human and press **Interact** to commit a sustained belly-rub flop, then remain nearby while
Cheddar sneaks. Alternatively, Cheddar can **Bark** beside the human to produce a short burp-cloud
opening for Cocoa at the steak. Wrong-role, out-of-range, and cooldown attempts coach without an
instant penalty; sneaking while the human is truly watching still produces a recoverable exposure,
threat cue, and rumble. A successful steal holds the steak-gone/human-distracted gag for `1.15`
seconds with mission-win audio and shared rumble before the result card.

The Ol' Switcheroo now requires an explicit two-verb deception. Cheddar reaches the decoy and
**Barks** to start the feint, then physically peels away once the squirrel commits; Cocoa reaches the
stash and presses **Interact** for one raid during that window. Merely standing on either station no
longer advances the puzzle. An early raid produces a recoverable guarded **BONK**, while holding the
feint too long produces a threat-backed backfire and full reset. Each clean raid credits both roles,
and the third holds the raided-stash/chased-decoy gag for `1.15` seconds with mission-win feedback
before results.

The Walk Campaign now makes both halves of its social con deliberate. Cocoa presses **Interact** at
the door to lock into her stare, while Cheddar presses **Interact** at the leash to present it; the
human's comprehension rises only while both dogs remain beside their engaged station. Proximity
alone does nothing, and leaving breaks that pose until its dog Interacts again. Incomplete messages
now escalate through specific wrong-item gags (food bowl, bath towel, vacuum) with threat audio and
rumble. A clean message earns a `1.15`-second live **WALKIES!** payoff with the human and grabbed
leash before results.

The Great Escape now treats every contraption step as a deliberate action instead of a trigger
volume. The glowing station names both its owner and dog-authentic action; that dog must arrive and
press **Interact**. A wrong dog can only fumble by deliberately trying the active station, producing
a visible **WRONG PAWS!** clank with audio and rumble. If the chain settles backward, the restored
step responds normally when repeated, but first-clear scoring prevents farming already-completed
links. The final squeeze holds all four completed stations and **FREE DOGS!** for `1.15` seconds
before results.

Chaos Machine now turns the automatic-looking cascade into a deliberate timed relay. Cheddar must
reach the lever and press **Interact**, handing the first live window to Cocoa; each junction then
requires its displayed owner to arrive and **Interact** before the three-second timer expires. The
off-duty dog's arrow routes forward to pre-position at the next junction. Proximity alone cannot
pull or fire anything, and wrong-paws attempts coach without consuming the window. A miss jams at
the exact towel/basket/toy step with threat feedback; Cheddar re-pulls to resume. The final toy
launch holds the completed three-prop cascade and **GLORIOUS CHAOS!** for `1.15` seconds before
results.

The Bone Detail now makes its advertised scent relay literal. Cocoa must reach the scent post and
**BARK** to reveal the real mound; standing nearby prepares the read but no longer completes her
role automatically. Cheddar remains the only digger, so Cocoa creates the information opening and
Cheddar turns it into progress. An out-of-range bark points Cocoa back to the post, while a blind or
wrong dig remains visible and recoverable. The third find holds the live called-mound/bone-stash
payoff for `1.15` seconds before the result card. Targeted `CoopBoneRelayPlayModeTests` passed
`10/10` on 2026-07-16.

Blanket Catch now turns its symmetric positioning exercise into a dog-authentic handoff. Both dogs
first create the taut catch surface; Cocoa's bark then calls each snack down from the counter, and
the pair slides the blanket midpoint under it. Barking while the blanket is slack is a visible,
harmless **BLANKET FIRST!** mistake rather than a wasted hidden input. The fifth catch holds the live
full-blanket pose for `1.15` seconds before the result card, with timeout pressure frozen through the
payoff. Replay resets the called-drop state, catch reactions, rips, and payoff hold. Targeted
`CoopBlanketCatchPlayModeTests` passed `9/9` on 2026-07-16.

The two predator-defense missions completed the roster (2026-07-04): Eagle
Shadow Panic's cover pads each carry a command badge during the hide phase (the eagle actor itself
carries the threat warning), a pad's badge drops while a dog is tucked inside its cover radius, and
all pads go quiet once the snatch/rescue beat moves the urgency to the talons; Coyotes at the
Fence's active weak spot carries a warning badge while the coyote is loose, flips to a command
badge once it is bark-pinned (partner: fill dirt now), moves with the coyote after each repair or
breach, and goes quiet for the united-bark final push. Manual acceptance check: from couch distance
with F1 off, each of these missions should show exactly one bobbing go-here icon over the place the
team must act next (two in a jammed Rube Goldberg: warning at the jam, command at the lever; one
per dog in Pee Break's split-role beats and Walk Campaign's combo message; all open covers in Eagle
Shadow's hide phase), and the icon should hand off immediately when the objective moves or the
station is held.

With the badges covering every mission, the follow-up 2026-07-04 pass **retired the close-range
instruction text the badges made redundant**. Station world labels are now identity-only nouns
(GATE, TOY, DECOY, STASH, HUMAN, STEAK, LEASH, LEVER, SCENT POST, ESCAPE GAP, static DIG?) —
who-does-what-now, progress percentages, and counters live exclusively on the badge handoffs, the
state sprite swaps, and the HUD objective line, per the Finding 4 recipe (state = sprite, urgency =
badge, text = one HUD line). Deliberately kept: The Great Escape station and Rube Goldberg junction
`{WHO}: {ACTION}` maps (that split information IS those puzzles and appears nowhere else), Operation
Pee Break's pairing-state text (NEEDS COCOA STARE etc. — unique needs-partner information, and the
deep slice stays untouched ahead of the couch-test-#4 verdict), terminal gag states (STEAK GONE!,
HUMAN GAVE UP - MIXED SIGNALS!, WALKIES!), and the short zone nouns the instruction catalog references verbatim
(HIDE HERE, WEAK SPOT, HOLD CUTOFF, CHECKPOINT). Manual acceptance check: walk up to the Gate Crash
gate, the Switcheroo decoy, or the Bone Detail scent post — the close-range text should read as a
calm name tag while the bobbing icon, sprite state, and top HUD line tell you what to do; nothing
in-world should shout a sentence at you.

## Authored audio/rumble checks

The arena now has replaceable feedback slots in `ArenaFeedbackCatalog`, plus a light looping
procedural backyard music bed that follows the F2 audio toggle. Event cues first load imported
authored MP3 banks from `Assets/Audio/Resources/AuthoredSfx/` through `AuthoredAudioCatalog`: bark,
team success, collect/gulp, squirrel chatter/escape/stunned, score gain, penalty, win/star, fail,
UI focus/confirm/open/close/disabled, acceleration skid, bunny hop, toy squeak, and threat cues all
use named files from the couch-test audio folder before falling back to generated procedural SFX.
Cheddar/Cocoa bark impacts also use their own dog-local bark banks.

Automated evidence: authored audio catalog coverage now asserts every imported MP3 loads and is
reachable from a runtime cue bank; dog-local bark coverage asserts Cheddar and Cocoa use their
identity-specific MP3 banks. Event-driven audio and rumble checks still verify cue requests,
toggles, and music muting.

Manual audio check:

- Start any mission and bark. Confirm a short comic bark cue plays.
- Collect a weenie/snack/sock. Confirm a small score-gain cue plus collect cue plays.
- Let the squirrel steal or force a fail. Confirm a lower warning/penalty cue plays.
- Complete rescue/tug or clear a mission. Confirm the success/win cue is brighter than the penalty cue.
- Use Replay, Next Mission, and Mission Select. Confirm a small UI blip plays.
- Press **F2** and repeat bark/collect. Confirm normal gameplay continues while authored audio is muted.

### Camera shake checks (2026-07-05)

`SharedCameraController.AddShake()` kicks a brief cosmetic jolt via `GameManager.RequestShake` (mission
clear/fail in `EndRound`) and via `MissionContext.RequestShake` (a mission controller's own in-round
impact). Manual check:

- Clear any mission. Confirm a light camera shake plays under the end card; clear it with zero mistakes
  and confirm the shake reads noticeably stronger (flawless) than a scrappy clear.
- Fail any mission. Confirm the shake reads stronger than either clear variant.
- Play Gate Crash and let the gate snap shut on Cheddar mid-squeeze. Confirm a small, immediate shake
  fires right at the snap - separate from and smaller than the later end-of-round shake - and that
  several snaps in one run don't feel like the camera is constantly rattling.
- Force the shared predator to grab a dog (Backyard Rescue, Coyotes Fence, or Eagle Shadow Panic).
  Confirm the "YOINKED!" moment also kicks an immediate shake, not just the rumble/score-penalty juice.

### Dog-local action audio tuning (2026-06-20)

The Backyard dog-local action layer was checked separately from the arena-wide cue slots.
The repeatable two-player check drives Cheddar and Cocoa at the same time, verifies their action
signatures, and exercises bark over an active carry bed. It is an in-engine mix/concurrency check;
the authored-SFX pass still needs a physical two-person television listening session for final mix.

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

Manual rumble/reconnect check with two gamepads:

- Bark gives a small pulse to both distinct bound player pads; rescue, tug completion, predator
  defense, and mission win give both a stronger pulse.
- Squirrel pressure/steal, predator hit, and mission fail give both pads a short warning pulse.
- Pause, switch **Rumble** off, and repeat bark/rescue/fail. Confirm gameplay continues with no motor
  response; switch it back on for the couch test.
- Disconnect one controller while the sibling pad stays connected. Confirm only that dog's chip
  changes from `PAD READY` to the short `PAD LOST` state (its keyboard fallback remains available);
  connect a replacement and confirm it controls the unbound dog without stealing the sibling's pad.
- No controller connected is expected to be a safe no-op.

Cheddar is the chaos puppy and Cocoa is the steadier veteran. Both read as long, low miniature
dachshunds with visible head, long snout, floppy ear, tiny feet, tail, collar, and expression
markers. Cheddar reads as **CHEDDAR CHAOS PUP** with a golden body, cream chest/toe identity,
red-orange collar, faster wag, and more explosive motion. Cocoa reads as **COCOA SPOT QUEEN** with a
uniform deep-chocolate body, warm-brown tonal points, teal collar, steadier expression, and tiny
queen marker. V02 model sheets are the identity authority for new authored frames; older geometric
fallbacks may retain their historical chest-patch shape.

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

Manual first-20-seconds check: start the scene with two players and do nothing for three seconds.
Confirm no squirrel steal happens yet, the intro banner is visible, the HUD objective says to save
weenies, the P1/P2 chips and Cheddar/Cocoa character reads are clear without floating names, and the
arrows are primarily helping players find breakfast/weenies. Move each dog left and right and confirm
the small intent marker appears only while running and does not compete with the dog art or objective.
After GO, the first squirrel pressure may begin on its authored delay depending on modifier; the
predator should not compete until the later warning.

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

During a predator grab, the grabbed dog shows a temporary rescue range ring. This is meant to explain
rescue distance without adding permanent clutter; the **RESCUE BARK** support label appears only in
the playtest/debug overlay.

## Rope/Tug shared-object mechanic

The labeled, pulsing **Rope/Tug** object is a required co-op objective. Its placeholder is a horizontal yellow/brown striped tug rope with visible ends so it is not confused with food. Either dog can interact near the rope for progress, but the main completion path is both dogs standing together at the rope to charge the tug meter. Finishing tug awards a major score bonus and is required for LevelClear.

Manual readability check: after early food recovery, the HUD objective and objective arrows should switch to **BOTH TUG** while dogs are away from the rope. If only one dog reaches the rope, the rope label should call out **WAITING FOR CHEDDAR** or **WAITING FOR COCOA** and the HUD should say both dogs must commit together. When both dogs stand on the rope, their labels briefly read **TUG!**, the rope label changes to **BOTH DOGS TUGGING - KEEP PULLING!**, the TEAM TUG meter fills, and completion flips the rope label to **ROPE COMPLETE!** with a **TUG POP!** world pop.

After enough food recovery makes tug the next likely bottleneck, the rope shows a temporary tug
range ring. It uses the current `1.6` tug-together radius, hides on non-tug missions, and reserves
the **BOTH DOGS** support label for the playtest/debug overlay.

## United bark

Bark remains visible through expanding bark rings and a short comic **BARK!** burst, but now affects gameplay:

- scares or interrupts the squirrel;
- resolves the predator warning/attack when both dogs are close and timed;
- rescues a grabbed/stunned dog when the partner is close;
- awards teamwork score with a cooldown so it cannot be spammed every frame.

Manual readability check: each bark should pop the dog into a **WOOF!** pose label and show both the expanding bark ring and comic bark burst. A solo bark away from targets gives a joking solo-bark cue and sets `LastJuiceFeedback` to `BarkBurst`. A successful united bark gives a **DOUBLE WOOF** cue and is protected for a short moment so the second same-moment bark does not visually downgrade it back to solo feedback. During predator warning, the united bark should drive the predator away immediately.

## Handoff flip flourish (G1.4)

`MissionContext.SignalRoleHandoff(fromDog, toDog)` is a shared, roster-wide signal a mission
controller calls at its own existing mid-mission moment where the acting role passes from one dog
to the other — a short baton-swoosh sprite arcs from the dog who just finished to the dog who acts
next, both HUD identity chips flash briefly, and one placeholder audio cue plays (`ui_replay_next_select`;
a dedicated "coach woof"-style cue is a later audio task). It adds no new mission rules — controllers
fire it at the same call site where the flip already happens.

Wired into: Great Escape and Chaos Machine's owner alternation (each contraption step/junction
completed), Scent Search's hot-call moment (Cocoa calls it, Cheddar digs), Table Stealth's route
swap (Cheddar's burp opens Cocoa's sneak window and vice versa), Weenie Roundup's jumbo-haul lift
(Cocoa steadies, Cheddar carries), and Blanket Catch's called drop (Cocoa calls it, both dogs slide
under). **Not** wired into Operation Pee Break's Beat-3 charger flip — both dogs get new,
non-swapped roles simultaneously there (Cheddar leash→hallway, Cocoa stare→charger), which is not a
handoff between the two of them the way the other six sites are.

Manual readability check: trigger any of the six wired moments and confirm the swoosh visibly travels
between the two dogs (not just pops at a fixed point) and both HUD chips flash together, not just one.

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

Every mission variant now carries its own explicit thresholds in `ArenaMissionTuning` (2026-07-10:
the last 7 that silently inherited Backyard Rescue's numbers got entries derived from their actual
achievable score ranges, guarded by a fall-through regression test). Treat the new numbers as a
starting point — the next couch test is the real calibration pass.

LevelClear displays a 1-3 star rating based on final score, the center banner reads **BACKYARD SAVED! [rank]**, and both dogs hold a **PROUD!** pose. GameOver displays **MISSION FAILED! [rank]**, applies the game-over penalty, and both dogs hold a **SAD FLOP** pose. The end card includes `Outcome: Score - Rank`, one short funny `EndReasonLabel`, the last score swing, stars, session totals, and Replay / Next Mission / Mission Select actions.

Manual score/replay readability check: during both clear and fail, confirm the final dog poses are still visible behind/around the end card, the score swing label remains signed and cause-first, the funny rank and reason line are readable, and Replay / Next Mission / Mission Select are all usable without a mouse. The MVP line should name Cheddar or Cocoa after any run with at least one successful beat in every mission — "MVP: awaiting dog heroics" on a scoring run means that mission's `CreditDog` wiring regressed (all 26 current missions credit successful actions; the original 22 were audited on 2026-07-10, and Baby Bird Bedlam, Skunk Blast Mayhem, Tick Invasion, and Burr Maze each followed the same controller boundary since).

## Non-developer playtest script

Use this when handing the prototype to someone who has not seen the code:

1. Start `ArenaScene` and say nothing. Confirm the player can identify that the first screen is mission select and can start a mission using keyboard or controller only.
2. Ask them to read the selected mission's team plan and opening briefing out loud. Confirm they can
   identify P1/P2, the first goal, and the recommended Operation Pee Break path without seeing
   readiness/debug language.
3. Have them play **Backyard Rescue** until either clear or fail. Confirm each player can follow the
   separate Bark -> Interact -> Jump -> Wrestle tutorial cells, and that the current mission,
   objective, identity chips, score pop, and end choices remain readable.
4. On the end screen, ask them to choose **Next Mission** without using a mouse. Confirm the next unfinished mission starts and shows its own briefing.
5. Have them finish **Snack Heist** and **Sock Panic** by any outcome. Confirm the session totals increment after each ended mission.
6. After all three missions have ended, choose **Next / Session Summary** and confirm missions played, total score, stars, and ranks are readable.
7. Return to mission select and replay any mission. Confirm session totals are not persistent across stopping Play mode.

## Two-player playtest script

Use this short script before starting a new level:

1. Start `ArenaScene` with two players and read the opening at gameplay zoom. Confirm both players can answer: who am I, where is my first weenie, what is the shared mission?
2. Before moving, confirm Cheddar and Cocoa are distinct without reading only text: long low bodies, Cheddar's golden/red chaos read, Cocoa's chocolate/teal spot-queen read, visible snouts, ears, tiny feet, and collars.
3. Move both dogs and complete each Backyard action lesson with both players. Confirm a completed
   player cell says `DONE` while waiting for the partner, later actions do not skip ahead, and the
   card retires after Wrestle. Also confirm Cheddar's run reads like **CHAOS ZOOM**, Cocoa's like
   **SPOT PATROL**, and both bark poses still pop without covering objective arrows.
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
- The compact HUD preserves the objective and both P1/P2 input-source chips without covering the
  central play area.
- Backyard Rescue advances its four action lessons only after both players complete the current
  verb; pause can skip/replay the lesson.
- The shared camera initializes and keeps both dogs framed.
- F1 diagnostics stay absent until explicitly opened and do not block normal mission flow.
- Pause settings can toggle audio, dual-pad rumble, and camera shake with a controller; major events
  still request their named feedback cues when enabled.
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

## Roster-wide character motion and ambient scenery pass (2026-07-26)

All active missions inherit three new V02-identity-locked, four-frame east-facing strips for both
dogs: jump, wrestle/play-pounce, and generic accepted Interact. Jump frames track the real hop arc;
a resolved wrestle briefly poses both dogs before the loser transitions to the existing stun; and
accepted generic interactions use a paw tap/boop while authored dig/sniff/tug reads remain stronger.
West-facing action mirrors east. Cheddar overshoots with loose chaos-puppy timing; Cocoa stays
planted and queenly.

Shared authored scenery now receives restrained deterministic ambient motion on render-only
children. Outdoor flowers/bushes/laundry sway, actionable scent/threat/payoff scenery breathes with
a small glow, and static yard/building art plus every mission-owned indoor/car area plate receives a
subtle light drift. Operation Pee Break's controller-owned living-room, success-room, and window
plates use that same treatment, covering the bespoke interior outside the shared area-art builder.
No gameplay root, collider, objective anchor, mission controller state, or camera bound moves.

Manual acceptance:

- In any outdoor mission, jump with each dog and confirm a readable crouch → airborne stretch/tuck
  → landing sequence, with Cocoa's bound visibly more controlled than Cheddar's.
- Put the dogs together and wrestle. Both should visibly enter the play-pounce/tumble strip before
  exactly one dog settles into the stunned read. A far-away whiff still shows the attacker's pounce.
- Trigger an ordinary accepted Interact and confirm the acting dog performs the paw-tap strip. Dig,
  sniff, and tug interactions must keep their more specific animation instead.
- Watch yard flowers/bushes and a live scent/threat cue for several seconds, then start Kitchen,
  Table Stealth, Chaos Machine, Thunderstorm Comfort, Blanket Catch, Car Ride, and Operation Pee
  Break. Scenery should feel gently alive without props drifting off their anchors, scenery
  covering dogs, or the room pulsing distractingly.

Automated evidence: full PlayMode suite **821/821 passed, 0 skipped** on 2026-07-26. Motion-resource
coverage requires all 24 distinct 512x384 frames; action tests pin jump synchronization, two-dog
wrestle acting, generic-versus-specific Interact priority, identity-asymmetric procedural motion,
and collider-free outdoor/indoor scenery animation. The rebuilt macOS player passed startup smoke,
and its `--arena-art-review` run wrote all **78/78** start/main/payoff frames; the review contact
sheet is `captures/artwork-scenery-pass-2026-07-26/arena-art-review-contact-sheet.jpg`.

## Visual readability checklist

Use this after any placeholder-art, authored-art, or sprite import change:

- Cheddar and Cocoa still read as different long, low dachshunds at gameplay zoom without relying
  only on text labels.
- Cheddar keeps golden/red chaos reads and canonical cream chest/toe tips; Cocoa keeps a uniform
  chocolate/teal read with warm-brown tonal points and no cream/white body patches. Historical
  geometric fallbacks are not an identity reference.
- Dog pose states are still distinct: idle, run, bark, tug, stunned, rescued, proud, sad.
- Bark ring/text, generated objective cue arrows, score pops, and playtest overlay remain readable
  but do not hide the dogs or core mission objects.
- Each dog's current role target emits at most three faint, dog-colored paw breadcrumbs near the
  dog. They suggest a heading, stop well before a distant destination, and vanish in the target
  zone; full route text remains behind F1 so exploration and couch communication still matter.
- The 102-pixel top HUD and bottom P1/P2 chips leave the central action field open; the Backyard
  tutorial is temporary and never replaces the current objective.
- Movement lean, squash/stretch, and paw trails are readable during motion but do not become constant noise.
- Camera framing keeps all three current missions readable from mission select without changing the orthographic backyard direction.
- Temporary bark, rescue, and tug cue rings appear only in actionable states and hide on mission select, end screens, and non-relevant missions.
- Weenie, snack, and sock collectibles are visually distinct from each other and from the squirrel
  and rope.
- Squirrel, predator, and rope expose their expected replacement slots from
  `ArenaArtCatalog` and keep their current role reads: thief, threat, shared tug prop.
- Predator labels now explicitly say **SHADOW! HUDDLE + DOUBLE BARK!** during warning,
  **YOINKED [DOG] - PARTNER BARK!** during rescue pressure, and **DOUBLE WOOF YEETED SHADOW**
  after success. Rope labels now call out **ROPE NEEDS BOTH DOGS**, **BOTH DOGS TUGGING X%**, and
  **ROPE COMPLETE! TEAM CHOMP!** so a silent observer can verify huddle, rescue, and tug intent
  without opening the inspector.
- Mission select remains legible through its UGUI/TMP picture grid and team-plan panel; IMGUI end
  actions and session summary retain readable high-contrast cards.
- Operation Pee Break starts on the painterly base interior plate with no procedural wall/wood-floor/
  window blocks showing through. Its baked closed door, seated Teenager, and leash read without
  duplicate couch/phone/door sprites; success swaps to the matched open-door/standing-Teenager room
  plate with the controller-owned payoff accents above it.
- Mission select and the shared arena backdrop should visibly shift with the selected/active mission
  through badge color, spotlight, and the matching reusable motif family; no level should feel like a
  blank generic arena before play begins.

## Known limitations

- Mission actors still use generated gameplay silhouettes, simple state motion, color/pulse changes,
  imported DRAFT badges, and close-range/debug support text. The imported sheets are not final
  transparent gameplay sprites, and stronger authored actor-state animation is still needed.
- Mission select is runtime-generated UGUI/TMP with generated mission portraits and HUD-skin
  elements; end actions and session summary remain IMGUI. These are coherent couch-test surfaces,
  not final authored production UI.
- The playtest overlay and event log are debug/playtest aids only. They are not analytics, persistence, telemetry, or player-facing production UI.
- Snack Heist remains an architecture proof inside the existing arena, now with stable generated
  snack-plate collectible art. Sock Panic has a complete first-pass co-op lock/key beat and generated
  basket/sock prop art, but both missions still need final authored environment and UI treatment.
- Dog identity art, pose labels, collars, expression markers, prop silhouettes, and objective cue
  arrows are generated couch-test assets. They are intentionally readable and easy to delete once
  authored sprites/animation exist.
- The squirrel and predator use intentionally simple movement/state rules so the PlayMode tests remain deterministic.
- The intro, bark, squirrel, predator, tug, clear, fail, score-pop, and objective feedback are still text/scale/couch-test-audio driven; they are designed to be replaced by final authored animation and mix later.
- Audio cues now use imported authored MP3 banks from named dog-life slots with generated fallback,
  and rumble is a simple best-effort pulse fanned out to both bound player pads. Neither is balanced,
  platform-tuned, or final feedback.
- `ForceSquirrelStealAttempt()` exists as a deterministic PlayMode test hook and is not intended as a player-facing control.
- Scoring is intentionally flat and session-local only. There is no save file, leaderboard, unlock economy, or persistent progression yet.
- The end rank is based only on final score and clear/fail state; it does not yet account for style, dog-specific contributions, or advanced co-op medals.
- Tug is proximity/progress based, not a full physics rope.
- Predator targeting and modifier selection are seeded but still prototype-simple.
- The scene now has basic procedural sound cues, an arena `AudioListener`, and simple placeholder animation, but real prefab art, authored animation, better SFX, and richer rescue/tug feel are still future work.
- Camera feel is tuned for the current generated backyard arena only. A larger authored level may need level-specific camera anchors or bounds.
- Paw trails and range rings now use generated cue sprites, but neither is a final VFX style.

## Test coverage

`unity/Assets/Tests/PlayMode/ArenaGameLoopPlayModeTests.cs` loads ArenaScene and verifies mission select initialization, centralized tuning defaults, movement/camera tuning defaults, mission balance invariants, starting each mission through the new flow, end-screen Replay / Next Mission / Mission Select availability, session totals across multiple missions, session summary after all three variants have ended, dogs, independent movement response, camera component/config, interaction range indicator state, mission state, intro prompt/banner, deterministic objective labels, delayed first squirrel steal window, initial score state, item recovery scoring, HUD score-pop state, world score/miss/success pops, playtest overlay state, playtest event log entries, squirrel steal/scare labels and score events, solo/united bark feedback and scoring, bark burst state, predator defense scoring, failed predator hit and rescue scoring, tug waiting/together feedback and scoring, LevelClear score/rank/summary/reason, GameOver score/rank/summary/reason, replay prompt visibility, restart reset state, exposed modifier state, dog identity labels, dog pose labels, movement intent labels/arrows, objective-arrow labels, shared generated scent-breadcrumb roots and art, generated arena audio listener, required replaceable audio cue slots, expected major-event audio cue requests, expected rumble request names, and audio/rumble suppression toggles. `FinalArtIntegrationPlayModeTests` also verifies the trail is capped at three hints for a far target, hides its explanatory text in normal play, and clears inside the interaction zone.

The same test file also verifies **Snack Heist** and **Sock Panic** can initialize, update objective labels, score unique mission events, reach clear/fail outcomes, and expose replay state. `ControllerCoopPlayModeTests` still verifies the baseline two-pad movement/bark proof and now clears scene objects before constructing its own bootstrap so it does not accidentally inspect leftover ArenaScene dogs from previous tests.

The pre-couch-test hardening adds `ActionTutorialPlayModeTests` coverage for the compact HUD,
Backyard-only ordered per-player lessons, skip/replay, camera-shake comfort control, and restart;
`CouchFeedbackPlayModeTests` covers rumble fan-out to both bound pads; and
`ControllerCoopPlayModeTests` covers safe replacement-pad binding without stealing the connected
sibling pad. Mission-select coverage also asserts the Recommended/Adventure Library player-facing
copy, four-beat preview cap, readable type floors, and title-ribbon crop.


## Backyard Pack: Squirrel Conspiracy (2026-06-18)

- Added `GameManager.MissionVariant.SquirrelConspiracy` / **The Great Backyard Squirrel Conspiracy** to the Arena mission select and mission order.
- The mission uses `HerdingMissionState` for deterministic route progress, herd/cutoff counts, fake-outs, taunts, stash reveal, and stash found clear state.
- Four deterministic route nodes now pair with four generated cutoff-zone markers. Only the current route's zone is visible, objective arrows split the dogs into herd/cutoff roles, and the markers reset to route one on replay.
- Gameplay loop: dogs bark near the squirrel to score `GOOD HERD`, split positioning to score `CUTOFF`, lose points on early/far `FAKE OUT`, reveal the stash after four controls with `DOUBLE BARK BLOCK`, then interact with the revealed stash for `STASH FOUND` and `CONSPIRACY CRACKED`.
- Fail path: repeated squirrel taunts or timer expiry fails the mission; replay resets route, stash, score, and outcome state.
- Production helpers are now part of the live path where relevant: `MissionRankCalculator`, `ScoreEventCatalog`, `MissionRuntimeSnapshot`, `MissionSeedGenerator`, and `MissionOutcomeSummaryBuilder`.
- `DemoReadinessGate` is surfaced in the F1 playtest overlay so the packaged backyard acceptance contract is visible during diagnostics.
