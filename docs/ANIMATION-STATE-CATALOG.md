# Animation State Catalog

Purpose: define required animation states before final asset production.

## A2.1 audit (2026-07-18) — ground truth vs. this list

Ground-truth method: inventoried every PNG under
`Assets/Art/Resources/ArenaFinal/Characters/Dogs/{Cheddar,Cocoa}/Motion/` (identical coverage for
both dogs) and traced the full render path: `DogReadabilityFeedback.ApplyPose()` sets a static
single-frame fallback via `ArenaDogPoseSprites.For()` → `FinalDogPoseArt.Load()` (Resources-backed;
falls back further to a draft pose-atlas crop only if that's also missing) on every pose *change*;
then every frame `AnimatePose()` → `AnimateAuthoredMotion()` tries to override it with a real
direction+time-indexed motion-strip frame via `CharacterMotionArt`, and if the exact facing is
missing, retries the same clip/frame at **E-facing with `flipX` mirroring** before giving up. Only
if even the E-facing frame is missing does the static fallback from `ApplyPose()` stay visible
indefinitely. `FinalDogPoseArt.PoseSuffix()` and `ArenaDogPoseSprites.RectFor()` both have **no case
for `Dig`/`Carry`/`Swim`/`Jump`** — all four silently default to `"idle"`/the Idle crop rect, so
*if* a pose ever reaches that final fallback tier, what actually renders is a plain standing-idle
image, not a distinct hold-frame for that pose.

Only `DogReadabilityFeedback.Pose` values can be authored/animated at all today: `Idle, Run, Bark,
Tug, Dig, Carry, Stunned, Rescued, Proud, Sad, Swim, Jump` (12 values). Any mission verb without a
`Pose` mapping renders as whatever pose was already active — no distinct read exists in code for it,
full stop; that is the `missing` category below, not merely a low-frame-count `static fallback`.
Also found: `CharacterMotionArt.Clip` additionally declares `Herd`, `Hide`, `Comfort` — none of the
three has assets on disk *or* a case in `CharacterMotionArt.TryClip(Pose, out Clip)`, so they are
dead enum values, unreachable from any pose today (flagged for whoever eventually builds real
sniff/hide/comfort art — the enum slot is already there).

### Per-state annotated table (Cheddar and Cocoa are identical)

| State | Frames × directions on disk | Classification | Notes |
|---|---|---|---|
| idle | 4 × 5 (E,N,NE,S,SE) | **authored strip** | Full coverage; W/NW/SW covered by `flipX` mirror of E/NE/SE — effectively 8/8. |
| run | 4 × 1 storybook primary; 4 × 5 legacy fallback | **authored storybook strip (2026-07-23)** | `Pose.Run` maps to `Clip.RunStorybook`: V02-sheet-locked E poses mirrored west, with 10 fps Cheddar / 8 fps Cocoa timing. Missing storybook art retries the complete prior directional `Clip.Run` set. |
| bark | 2 × 1 storybook primary; 4 × 5 legacy fallback | **authored storybook strip (2026-07-23)** | `Pose.Bark` now maps to `Clip.BarkStorybook`: a two-pose side-on cycle, mirrored west, with 10 fps Cheddar / 7 fps Cocoa timing. Missing storybook art retries the prior `Clip.Bark` directional frame. The legacy set remains complete and load-tested. |
| dig | 4 × 2 (E,S) | **reused strip (E-facing, mirrored)** | N/NE/SE requests retry at E and mirror — still a real 4-frame dig animation, just not truly directional. Used by 1 mission controller (`ShowDig`, Scent Search / Bone Relay dig verb). |
| carry | 2 × 3 (E,N,S) | **reused strip (E-facing, mirrored)** | NE/SE reuse E, mirrored. `SetCarrying(true)` used in 2 controllers. |
| tug | 3 × 1 (E only) | **reused strip (E-facing, mirrored)** | Every non-E facing reuses the same 3 E frames, mirrored for west-facing. Used in 2 controllers (shared rope-tug + `ShowTug(faceDir)` two-dog-flank variant). |
| proud | 2 × 1 (E only) | **reused strip (E-facing, mirrored)** | Heavily used — `ShowProudBrief()` is the roster-wide generic "nice work" beat, called from 13 files. Single-direction art is a reasonable trade for a brief celebratory flash. |
| rescued | 2 × 1 (E only) | **reused strip (E-facing, mirrored)** | 1 controller. |
| sad | 2 × 1 (E only) | **reused strip (E-facing, mirrored)** | 1 controller (also backs `ShowPanic()`'s brief flinch, used in 7 files for thunderclap-style scares). |
| stunned | 2 × 1 (E only) | **reused strip (E-facing, mirrored)** | Wrestle-loss / grabbed read. |
| swim | none | **static fallback (= plain idle image)** | `Pose.Swim` has no `Clip` mapping (`TryClip` returns false) and no `FinalDogPoseArt`/`ArenaDogPoseSprites` case — a swimming dog currently renders as a standing-idle sprite with zero visual distinction, even though the backyard pool mechanic is live gameplay. |
| jump | none | **static fallback (= plain idle image)** | Same gap as swim — `Pose.Jump` renders as plain idle. |
| interact (any mission's accept) | n/a | **fixed (A2.2, 2026-07-18)** | `GameManager.OnDogInteracted` now plays a squash-and-pop cosmetic tween on a genuine Interact acceptance, roster-wide, via one shared hook. Not a `Pose`/authored-art fix — a tween layered on top of whatever pose is already showing. |
| sniff | 4 × 2 (E, S) | **authored strip (A2.4, 2026-07-18)** | `Pose.Sniff`/`Clip.Sniff` exist and `ScentSearchMissionController.Sniff()` calls `ShowSniff()` on both dogs' tracking beats. A2.3 wired the code path with no art (owner-supplied reference boards weren't available that session); A2.4 got hand-produced `cheddar_sniff_east_south_v01.png`/`cocoa_sniff_east_south_v01.png` boards from the owner and extracted real E/S frames through a new `export_character_sniff.py` (W covered by the existing E-facing mirror, same pattern as dig). |
| groom (Tick Invasion) | 2 × 1 (E only) | **authored mission clip (2026-07-23 sheet correction)** | `CharacterMotionArt.Clip.Groom` is driven by controller-owned `DogGroomAnimation` after a successful mutual groom. It temporarily overrides the shared dog-art renderer rather than adding a global `Pose.Groom`; W is mirrored. Cheddar plays at 7 fps and Cocoa at 5.5 fps. Cocoa's pair was regenerated against her V02 sheet to remove noncanonical cream markings. |
| comfort (as a distinct gesture) | n/a | **missing (aliased)** | `ShowComfort()` calls `ForcePose(Pose.Proud, 0.5f)` — a real gameplay signal fires, but it visually reads as "proud," not "comforting." |
| dramatic flop (Table Stealth) | n/a | **missing** | No `Pose.Flop`; Cocoa's belly-flop distraction has no distinct pose while `_flopEngaged` is true. |
| beg | n/a | **missing** | No `Pose.Beg` anywhere in the roster. |
| head tilt | n/a | **missing** | No dedicated pose or gesture. |
| paw tap | n/a | **missing** | No dedicated pose or gesture (A2.2's planned Interact micro-animation is the natural home for this). |
| push / pull | n/a | **missing** | Distinct from tug; no separate pose exists (tug's rope-pulling read is the closest analog but isn't reused for generic push/pull). |
| hide | n/a | **missing (enum only)** | `CharacterMotionArt.Clip.Hide` exists but is unreachable (no `Pose.Hide`, no assets). |
| united bark | n/a | **reused (Bark)** | No separate pose; plays the same Bark strip as a solo bark, distinguished only by score/juice feedback, not the dog's own animation. |
| grabbed | n/a | **reused (Stunned)** | No dedicated pose; predator-grab sequences read through the existing Stunned art. |
| scared | n/a | **reused (Sad)** | `ShowPanic()` is literally `ForcePose(Pose.Sad, 0.6f)` — a brief flinch reusing the Sad art, not a distinct scared read. |
| wet | n/a | **missing (separate system)** | Handled by `DogController`'s wet-timer/tint overlay (`ResetMissionOverlays`), not the `Pose` enum at all — real but architecturally outside this catalog's scope. |
| trapped / held / sleepy | n/a | **missing** | No pose or gesture found for any of the three in current mission code. |

### Ranked gap list (signal impact — verbs missions require players to read, worst first)

1. **Interact** (A2.2's scope) — every one of the 23 missions' Interact-accept moments has zero
   distinct dog-side animation. Highest leverage: touches the entire roster through one shared
   input→controller acceptance path, not 23 call sites.
2. **Sniff** — Scent Search's entire core verb had no visual read; the mission's whole premise was
   invisible on the dog. **Resolved A2.4 (2026-07-18)** — see the outcomes note below.
3. **Carry** — partial direction coverage (E/N/S only) is a smaller gap than the fully-missing verbs
   above but affects every carry-a-weenie/carry-a-bone beat across the roster.
4. **Comfort** — currently aliased to Proud, actively misleading (a nuzzle reading as a celebration)
   rather than merely absent.
5. **Dramatic flop** (Table Stealth) — single-mission impact, but it's that mission's headline
   comedy beat and currently has no distinct pose at all.
6. **Beg** — no mission currently requires it, but it's on the original wishlist and worth a decision
   (build vs. formally drop) rather than leaving it silently unimplemented.
7. **Swim / Jump** — both fall to a plain-idle static image. Swim affects the backyard pool mechanic;
   Jump's mission-code usage should be re-verified before prioritizing art for it (worth confirming
   the underlying jump *mechanic* is actually reachable by players before commissioning jump frames).

**A2.2–A2.4 scope check:** A2.2 (Interact micro-animation) is confirmed correctly scoped against
gap #1. A2.3 ("signal-critical verb strips: dig/sniff/carry") should be **amended** — dig already has
working (if mirrored) authored art via the E-facing-retry path, so it is not a blocking gap the way
sniff and carry are; sniff and comfort deserve at least equal priority within that task. A2.4
("threat/NPC acting gaps") is untouched by this audit — it covers non-dog actors, out of this pass's
Dog-only scope.

### A2.2 / A2.3 outcomes (2026-07-18)

**A2.2 shipped in full** — gap #1 (Interact) is closed via a code-only tween (`GameManager.
OnDogInteracted` → `DogReadabilityFeedback.ShowInteractAccepted()`), not new authored art; see
`docs/ARENA-PLAYABLE.md`'s "Interact micro-animation" entry.

**A2.3 shipped code-side only — new authored art was not produced this pass.** The character-motion
pipeline documented above (§1 of the original A2.1 research) is a two-step process: an external
image-generation step produces a hand-approved reference board under
`ReferenceOnly/GeneratedCharacterMotion/`, *then* a `tools/art/export_character_*.py` script crops
it into runtime frames. Every board that exists today (28 of them, per
`tools/art/character_motion_manifest.json`) was produced by that first step through a tool this
agent session did not have access to (no image-generation capability in this toolset). Rather than
silently skip the task or fabricate placeholder art, the owner chose (when asked) "code-side prep
only": `Pose.Sniff`/`Clip.Sniff` now exist end-to-end (enum values, `TryClip`, `FrameAtTime`,
`FallbackPose`, `PoseCopy`, `DogReadabilityFeedback.ShowSniff()`), and
`ScentSearchMissionController.Sniff()` calls it on both dogs' tracking beats (Cheddar's direction
hint, Cocoa's ongoing WARM/COLD tracking — the exact hot-patch discovery still plays the bigger
`ShowProudBrief()` celebration instead, unchanged). Until a reference board is produced through the
external tool and a matching `export_character_sniff.py` extractor is written (following
`export_character_dig.py`'s shape - see the original A2.1 research notes for the exact technique),
`Pose.Sniff` renders via the same static-idle fallback `Pose.Swim`/`Pose.Jump` already use — a known,
already-documented gap, not a new one. The moment art lands, no further code changes are needed.

**Update, A2.4 (2026-07-18): the Sniff art gap above is now closed.** The owner produced
`cheddar_sniff_east_south_v01.png`/`cocoa_sniff_east_south_v01.png` (hand-generated externally,
matching the existing dig boards' 2×4 E/S grid format and character model) and dropped them into
`ReferenceOnly/GeneratedCharacterMotion/`. Wrote `tools/art/export_character_sniff.py` (mirrors
`export_character_dig.py`) to extract them, exactly as this note predicted would be needed. The
extraction surfaced one real bug worth remembering for any future board: two of Cheddar's four
east-facing cells came out with a small disconnected fur-wisp artifact in the corner (a neighboring
cell's ear/tail tip bleeding past the crop boundary) — fixed generically in the new script with a
largest-connected-component filter that zeroes any foreground blob that isn't the dominant one,
rather than hand-tuning crop margins per frame. `Pose.Sniff` now renders real authored motion, no
longer the Swim/Jump-style static fallback. `docs/AGENT-WORK-QUEUE-PRELAUNCH.md`'s A2.4 entry has
full evidence.

**Carry's missing NE/SE diagonals were not promoted** — same tooling gap, and lower priority than
sniff per the ranked list above. `docs/AGENT-WORK-QUEUE-PRELAUNCH.md`'s A2.3 entry has the full
evidence and should be consulted before anyone picks this back up to produce the actual art.

### A2.4 outcomes (2026-07-18) — threat/NPC acting gaps

Traced every `SetActorState(...)` label the coyote/squirrel mission controllers actually send
through `ThreatMotionArt.TryInfer`'s keyword table (`ThreatReadabilityAnimator.SetLabelState` →
`TryInfer`), not just the abstract state names in this doc. Ground truth turned out to differ from
what the table above assumed:

- **Coyote fake-snack lure was falling all the way to Patrol, not reusing Threaten as documented.**
  `CoyotesFenceMissionController.TriggerFakeSnack()` sends `"FAKE SNACK BAIT - CHEDDAR, NO!"` /
  `"...IGNORE IT!"` — neither contains the literal substring `"LURE"` the keyword table checked for,
  so the coyote calmly patrol-paced during its most predatory bait moment. **Fixed**: added a
  `"BAIT"` keyword alongside `"LURE"`.
- **The final "coyote retreats" defeat state had the same bug.**
  `CompleteFinalPressure()` sends `"COYOTE RETREATS - YARD DEFENDED!"`, which doesn't contain
  `"DRIVEN BACK"` or `"BLOCKED"` (the only two Retreat keywords) — it also fell to Patrol, despite
  the label saying "retreats." **Fixed**: added a `"RETREAT"` keyword to the Retreat branch.
- **Squirrel taunt reused the cowering Scared clip for what is a gleeful, successful escape**, not a
  scared reaction — `SquirrelConspiracyMissionController`'s taunt counter fires when the squirrel
  evades a cutoff (3 taunts fails the mission), the mirror image of getting caught, not of being
  startled. Reusing "cowering tremble" reads backwards, the same class of bug as A2.1's
  Comfort-aliased-to-Proud finding. **Fixed**: moved `"TAUNT"` from the Scared bucket to the Run
  bucket (bounding scurry) — reuses existing authored art, no new art needed.
- **The headline finding, caught by a full-suite regression, not by inspection: the squirrel branch's
  `!upper.Contains("SQUIRREL")` guard looked like a bug but turned out to be load-bearing.** The
  guard returns `false` (no inference at all) unless the label literally contains the word
  "squirrel." First instinct was to remove it, since Eagle's and Coyote's branches have no equivalent
  guard and 5 of `SquirrelConspiracyMissionController`'s own 6 labels (`HERDED`, `ROUTE n/CONTROLS`,
  `STASH REVEALED`, `CONSPIRACY CRACKED`, `TAUNT`) never said the word "squirrel" — so `TryInfer`
  returned `false` and `ThreatReadabilityAnimator.SetAuthoredActive(false)` fired, silently disabling
  the authored motion strip for nearly that entire mission and falling back to the static placeholder
  cutout for most of its runtime. Removing the guard broke two *other* already-green tests in
  `FinalArtIntegrationPlayModeTests.cs`: the guard is the intentional mechanism that keeps Coyotes
  Fence's repurposed shared-squirrel-actor "dirt/weak-spot" marker from idle-breathing like a live
  squirrel when it's really just a prop. **Correct fix, applied instead: added the literal word
  "SQUIRREL" to those 5 label strings in `SquirrelConspiracyMissionController.cs`** (they're shown to
  players as the actor's world-label text too, so this also makes the on-screen copy clearer, e.g.
  "SQUIRREL HERDED - NEEDS COCOA CUTOFF!") — fixes the real mission bug at its source without
  weakening a guard another mission genuinely depends on. Also fixed the same bug's cousin:
  `"SQUIRREL GOT A WEENIE!"` (BackyardRescue's theft-success beat — does say "squirrel" so it wasn't
  disabled, but didn't match the Steal keyword list) fell to Idle instead of the grabby Steal clip its
  sibling `"SQUIRREL STOLE A SNACK!"` correctly gets — added `"WEENIE"` to the Steal keywords.
- **"Stash guard" needed no separate fix once the label-text fix above landed.** The only call site
  matching that concept, now `"SQUIRREL STASH REVEALED - SNIFF + INTERACT!"`, resolves to the default
  Idle clip (perky perch, small breath-rise + tail-flick) — a legitimate "watchful guard" read, not a
  mismatch, same category as A2.1's finding that Dig/Carry's E-mirror retry "still plays a real
  animation everywhere." Before the fix it wasn't animating at all; now it correctly does.
- **Human/Teenager NPCs have zero animated motion of any kind, not a keyword-mapping gap.**
  `GameManager.AddThreatAnimator` only attaches `ThreatReadabilityAnimator` when `art.ObjectName` is
  `"Squirrel"` or `"Predator Warning"` — any other actor gets `ThreatMotionArt.Actor.Unknown` and no
  animator at all, so Table Stealth's human and Walk Campaign's teenager NPCs render as static
  cutouts full stop. Fixing this for real needs a new `Actor` case, a new `Clip` set, and authored
  reference boards through the same external image-generation step A2.3 couldn't reach — flagged as
  the largest remaining threat/NPC gap, explicitly not attempted this pass (same tooling
  constraint, not a keyword fix).
- All four code fixes (coyote lure/retreat keywords, squirrel taunt remap, the squirrel guard-clause
  removal, the weenie-theft keyword) reuse clips that already have authored art (Threaten, Retreat,
  Run, Steal) — no new art, no tooling-gap decision point needed this time.
- **Tests**: `ThreatLabelInferencePlayModeTests.cs` (new) pins `TryInfer` against the exact
  production label strings (not synthetic keywords) for both the fixed and already-correct cases,
  so a future label-text edit that silently breaks the mapping again gets caught.

### A2.5 outcomes (2026-07-19) — held-payoff pose audit

Audited the 1.15s `SuccessHoldSeconds`/`DoorOpenPayoffSeconds` hold every mission plays before the
end card, checking whether it forces `Pose.Proud` on both dogs (via
`DogReadabilityFeedback.ShowProudBrief()`) or leaves them on whatever pose they last had (Idle, in
practice — "frozen dogs"). Ground truth differed from the task brief's own framing:

- **Operation Pee Break, cited in the task brief as the reference-good pattern, was itself one of the
  misses.** Its "hydrant/relief beat" only animates the **scene** (hydrant `SetActive`, three
  `AnimateReliefSparkle` sparkles) — `Tick()` short-circuits to `AdvanceSuccessHold` the instant
  `DoorOpen` is true, which never touches dog pose, so the dogs sat static through "Relief zoomies!"
  Fixed by posing both dogs in `StageDogsForDoorOpenPayoff()`, which fires exactly when the hold
  starts.
- **14 of 23 missions had no pose call at all** at their completion trigger: Snack Heist, Scent
  Search, Thunderstorm Comfort, Mark the Yard, Gate Crash, Table Stealth, Squirrel Switcheroo, Walk
  Campaign, Bone Relay, Great Escape, Chaos Machine, Blanket Catch, Baby Bird Bedlam, and Pee Break
  (above). Thunderstorm Comfort is the interesting case: it *does* call `ShowComfort()` continuously
  while huddling, but that loop lives in the pre-clear branch of `Tick()` and gets skipped once
  `_cleared` is set, so the pose decays back to Idle mid-hold rather than never firing.
- **2 missions posed only one of the two dogs** at the finale: Weenie Roundup (the jumbo delivery
  poses the hauler but not the partner who steadied it) and Kitchen Food Frenzy (poses the catching
  dog but not the calling dog on every catch, including the finale).
- **6 missions were already correct**: Sock Panic, Squirrel Conspiracy, Eagle Shadow Panic, Coyotes
  at the Fence, Leash Walk, Car Ride — all already call `ShowProudBrief()` on both dogs at (or right
  before) the hold trigger.
- **Backyard Rescue has no held-payoff beat at all**, not a pose gap. It predates the
  `IMissionController` migration's success-hold pattern: `GameManager.CheckClear()`'s legacy
  `hasItems && hasPredator && hasTug` branch calls `EndRound(true)` the instant the last condition is
  met, with no `IsPresentingSuccessfulOutcome` hold to pose during. Adding one is a mission-timing
  change, out of a pose-only audit's scope — flagged, not attempted.
- All fixes reuse the existing `ShowProudBrief()` idiom (`Pose.Proud`, 1.1s — already the convention
  in the 6 correct missions) rather than introducing a new pose; no new art needed.
- **Tests**: added a `DogReadabilityFeedback.Pose.Proud` assertion (both dogs, during the hold,
  before `ForceFinishSuccessPresentation()`) into each of the 16 fixed missions' existing clear-path
  PlayMode tests. See `docs/AGENT-WORK-QUEUE-PRELAUNCH.md`'s A2.5 entry for the full 23-row table.

## Dog Locomotion

Cheddar and Cocoa both need:
- idle
- walk
- run
- zoomies
- skid
- turn
- jump / hop
- land
- swim
- shake off

## Dog Actions

- bark
- united bark
- tug
- carry
- push
- pull
- sniff
- dig
- hide
- rescue
- comfort
- beg
- head tilt
- paw tap
- dramatic flop

## Dog Status

- stunned
- grabbed
- scared
- wet
- proud
- sad
- trapped
- held
- sleepy

## Squirrel

- idle
- run
- route turn
- fake-out
- steal
- stash guard
- taunt
- escape

Current Unity first-test coverage: Resources-backed runtime strips exist for idle, run, steal, and
scared/fake-out reads under `ArenaFinal/Characters/Squirrel/Motion/`. Remaining named states can map
to those strips until final animation boards exist.

## Coyote

- patrol
- test fence
- threaten
- lure
- retreat

Current Unity first-test coverage: Resources-backed runtime strips exist for patrol, threaten, and
retreat under `ArenaFinal/Characters/Coyote/Motion/`. Test-fence reads correctly reuse threaten
(**fixed, A2.4, 2026-07-18**: the fake-snack lure and the final defeated-retreat state were actually
falling through to Patrol, not Threaten as this line assumed - see the A2.4 section below).

## Eagle

- sweep / shadow pass
- attack / snatch

Current Unity first-test coverage: Resources-backed runtime strips exist for sweep and attack under
`ArenaFinal/Characters/Eagle/Motion/`.

## Human/NPC

- walk
- turn
- notice
- distracted
- annoyed
- put-away
- vacuum
- hold dog

## Rule

Every new animation must support gameplay readability at small 2.5D camera scale.
