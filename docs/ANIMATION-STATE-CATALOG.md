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
| run | 4 × 5 (E,N,NE,S,SE) | **authored strip** | Same full coverage as idle. |
| bark | 4 × 5 (E,N,NE,S,SE) | **authored strip** | Same full coverage; frame-capped at index 3 regardless of elapsed time. |
| dig | 4 × 2 (E,S) | **reused strip (E-facing, mirrored)** | N/NE/SE requests retry at E and mirror — still a real 4-frame dig animation, just not truly directional. Used by 1 mission controller (`ShowDig`, Scent Search / Bone Relay dig verb). |
| carry | 2 × 3 (E,N,S) | **reused strip (E-facing, mirrored)** | NE/SE reuse E, mirrored. `SetCarrying(true)` used in 2 controllers. |
| tug | 3 × 1 (E only) | **reused strip (E-facing, mirrored)** | Every non-E facing reuses the same 3 E frames, mirrored for west-facing. Used in 2 controllers (shared rope-tug + `ShowTug(faceDir)` two-dog-flank variant). |
| proud | 2 × 1 (E only) | **reused strip (E-facing, mirrored)** | Heavily used — `ShowProudBrief()` is the roster-wide generic "nice work" beat, called from 13 files. Single-direction art is a reasonable trade for a brief celebratory flash. |
| rescued | 2 × 1 (E only) | **reused strip (E-facing, mirrored)** | 1 controller. |
| sad | 2 × 1 (E only) | **reused strip (E-facing, mirrored)** | 1 controller (also backs `ShowPanic()`'s brief flinch, used in 7 files for thunderclap-style scares). |
| stunned | 2 × 1 (E only) | **reused strip (E-facing, mirrored)** | Wrestle-loss / grabbed read. |
| swim | none | **static fallback (= plain idle image)** | `Pose.Swim` has no `Clip` mapping (`TryClip` returns false) and no `FinalDogPoseArt`/`ArenaDogPoseSprites` case — a swimming dog currently renders as a standing-idle sprite with zero visual distinction, even though the backyard pool mechanic is live gameplay. |
| jump | none | **static fallback (= plain idle image)** | Same gap as swim — `Pose.Jump` renders as plain idle. |
| interact (any mission's accept) | n/a | **missing** | No `Pose` value exists for "interacting" at all; pressing Interact changes only the prop/world state, never the dog's own pose. This is exactly what A2.2 is scoped to fix. |
| sniff | n/a | **missing** | No `Pose.Sniff`; Scent Search's whole premise is sniffing and has no distinct dog read for it today. |
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
2. **Sniff** — Scent Search's entire core verb has no visual read; the mission's whole premise is
   invisible on the dog.
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
retreat under `ArenaFinal/Characters/Coyote/Motion/`. Test-fence and lure reads currently reuse
threaten.

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
