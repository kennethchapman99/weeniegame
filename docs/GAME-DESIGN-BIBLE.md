# Cheddar & Cocoa Game Design Bible

This document is the durable creative source of truth for the Cheddar & Cocoa couch co-op game. Claude, Codex, and any future implementation agent should read this before proposing or implementing new gameplay.

> **Status: ACTIVE CREATIVE NORTH STAR.** The level and mechanic libraries below are idea banks,
> not an active production queue. The mission roster is frozen until Operation Pee Break passes its
> second two-player couch playtest. Current execution order lives in `NEXT-PRODUCTION-SLICE.md`.

## Active depth-first sequence

1. Run a baseline two-player couch playtest of the existing slices.
2. Address critical playtest findings.
3. Define `IMissionController` and a narrow `MissionContext`.
4. Extract the existing Kitchen mission first, keeping all PlayMode tests green.
5. Build Operation Pee Break entirely through the new controller structure.
6. Run a second couch playtest as the deep-slice acceptance gate.
7. Keep the mission roster frozen until that gate passes.

Architecture work is deliberately narrow and incremental. `GameManager` retains orchestration,
mission selection, session flow, and shared-service wiring; controllers own mission-specific
lifecycle and snapshots. Do not use a target line count as the definition of done.

## North star

Build a joyful, personal, replayable couch co-op game where Sue and Ken play as Cheddar and Cocoa through exaggerated dog-life adventures inspired by the best parts of premium co-op games: clear teamwork, funny asymmetry, constant novelty, readable chaos, and mechanics that change before they get stale.

This is not just a dog skin on a generic platformer. The fun should come from asking: **what would Cheddar and Cocoa believe is the most important mission in the world right now?**

## Core design pillars

1. **Dog-life fantasy first**
   - Every level should feel like something real dogs would care about: food, toys, couch territory, sunbeams, smells, walks, squirrels, threats, humans, blankets, pee breaks, vet betrayal.
   - The world should be human-sized and dog-interpreted: chairs are towers, couch cushions are kingdoms, falling food is divine intervention, cleaning day is an invasion.

2. **Co-op verbs over generic tasks**
   - Anchor every idea in one or more of these verbs: **chase, rescue, steal, distract, defend, comfort, carry, tug, hide, sniff, bark**.
   - A level is not ready if the objective is only "collect X things." Add pressure, role split, risk, timing, or comedy.

3. **Readable chaos**
   - The game should be silly and busy, but players must always understand the current objective, danger, and next useful action.
   - Use labels, pings, animation, camera nudges, audio cues, and HUD callouts aggressively until authored art can carry the clarity.

4. **Asymmetric dog personalities**
   - Cheddar and Cocoa should feel different mechanically and comedically.
   - Cheddar: chaos puppy, faster burst/zoomies, food-obsessed, squeezes into dumb places, burp/stun comedy, worse impulse control.
   - Cocoa: veteran queen, steadier, better at holding territory, stronger in tug/anchor moments, better at calm tasks, royal glare, sunbeam/couch ownership energy.

5. **Mechanic-of-the-level freshness**
   - Each chapter should introduce a specific mechanic, explore it, then move on.
   - Avoid building one giant system before the game is fun. Prefer small, high-personality vertical slices.

6. **Personal jokes become mechanics**
   - Inside jokes are strongest when they affect play. Example: a phone-absorbed teenager is not just dialogue; it becomes a co-op puzzle where the dogs manipulate the environment to trigger a pee break.

7. **Co-op puzzle magic**
   - Every substantial mission should include at least one authored co-op puzzle beat where one dog creates an opening and the other dog turns it into progress.
   - The target feeling is: **I can do my dog thing, but only you can make it matter.**
   - Parallel collection, shared timers, and both-dogs-in-a-circle gates are not enough by themselves. Add role locks, split information, deception, misdirection, sequential cause/effect, role reversal, or funny failure.
   - Detailed design doctrine lives in `docs/COOP-PUZZLE-DESIGN.md` and should be treated as required reading for new mission prompts.

## Co-op puzzle magic standard

Cheddar & Cocoa should borrow the best lesson from premium two-player adventure games without copying their themes: the delight comes from constantly discovering a new way the other player is the missing piece.

A good Cheddar & Cocoa puzzle beat is short, physical, readable, and dog-authentic:

- one dog can reach, smell, see, hold, distract, bait, or survive something the other cannot;
- the other dog has the action that converts that opening into progress;
- success changes the world visibly;
- failure teaches the solution through comedy, not punishment;
- the beat escalates or flips roles before it becomes repetitive.

Use these puzzle families repeatedly across levels:

- **Hold-and-release:** Cocoa anchors a blanket/leash/pool noodle while Cheddar crosses, jumps, steals, or squeezes through.
- **Distract-and-sneak:** one dog draws a human/vacuum/squirrel/coyote attention cone while the other steals, rescues, or repositions.
- **Smell-and-act:** one dog reads scent information while the other digs, carries, opens, or blocks.
- **Bait-and-switch:** one dog tempts an enemy or human into the wrong route while the other sets the trap.
- **Long-dog geometry:** dachshund body, leash, blanket, ramp, or toy becomes a bridge, wedge, pulley, shield, or catch net.
- **Timed double action:** Dog A opens a short window; Dog B finishes inside it.
- **Rescue-as-puzzle:** the stuck dog still contributes by wiggling, leaning, whining, or revealing timing while the free dog changes the environment.
- **Social manipulation:** humans are puzzle systems; dogs combine leash, shoe, bark, stare, charger, toy, and blocked hallway to make humans do dog-relevant things.

New mission prompts should include a **Co-op Puzzle Beat** section naming the beat, roles, lock/key dependency, hints, funny failure, world-state change, and test hooks.

## Existing playable direction to preserve

The current Unity direction already has a strong base: Cheddar and Cocoa in a Backyard Mission arena, shared scoring, breakfast/weenie recovery, squirrel stealing pressure, predator warning/attack/rescue, rope/tug objective, united bark, modifiers, stars, and PlayMode tests. Future work should build on this rather than replace it.

Preserve:
- two-player couch co-op as the heart of the game;
- Cheddar/Cocoa identity and asymmetry;
- bark as a core verb, not just feedback;
- rescue/defense/teamwork as required mission actions;
- short replayable rounds with stars/modifiers;
- deterministic test hooks for new mission logic.

## Core mechanics library

### Bark

Bark should have contextual uses:
- scare or interrupt squirrel;
- trigger united-front defense;
- rescue grabbed/stunned partner;
- distract humans;
- wake teenagers;
- rhythm-match against threats;
- create comedy feedback through WOOF labels, rings, camera shake, and audio.

Bark should not become a spam button. Use timing windows, cooldowns, positioning, or rhythm.

### United bark

Both dogs bark near each other within a short timing window.

Uses:
- scare predators;
- interrupt squirrel longer;
- wake a sleeping/phone-absorbed human;
- open sound/reactive puzzle objects;
- calm panic during thunder/vet/vacuum scenes.

### Scent trail

One or both dogs can reveal trails for food, toys, humans, cozy spots, or danger.

Variants:
- Cheddar smells food better.
- Cocoa smells toys, cozy spots, or human routines better.
- Scent gets confused near laundry, shoes, cleaning supplies, or pool water.

### Tug/carry/shared object

Shared object interactions should create laughter and coordination:
- rope tug;
- dragging a leash to a human;
- moving a pool noodle bridge;
- hauling a big toy;
- carrying stolen food without dropping it;
- pulling a blanket.

### Rescue

One dog gets grabbed, stuck, wet, stunned, trapped, hidden, or held. The other must rescue through bark, tug, distraction, or proximity.

Use rescue often because it creates natural co-op drama.

### Wet/swim/shake state

Pool/water creates temporary state changes:
- wet dog moves slower or slips;
- indoor floors become slippery;
- shake-off becomes a comedic area effect;
- humans react if the wet dog jumps on furniture.

### Mischief meter

Doing bad/funny dog things builds power but risks human intervention.

Examples:
- steal sock;
- bark at nothing;
- drag towel;
- jump on couch wet;
- knock pillow;
- leave pawprints.

### Panic/calm meter

Useful for vet, thunder, nail grinder, vacuum, giant dogs.

One dog can comfort the other by staying close, cuddling, or performing a calming rhythm.

### Leash physics

Connected movement creates comedy:
- wrap/unwrap around trees, poles, legs, mailboxes;
- one dog pulls toward smell, one toward destination;
- too much tension annoys human;
- synchronized trot creates bonus.

### Long-dog bridge

Because dachshunds. One dog stretches across a small gap/object while the other crosses. The bridge dog has patience/stamina and can collapse comedically.

### Mutual grooming

Dogs stand close and groom each other to remove a status (ticks, mud, burrs). The groom action
reduces the target's status but slightly transfers it to the groomer — players must coordinate
so neither gets overwhelmed. Bark from the clean dog holds the dirty dog still for a wider/faster
groom pass. First used in Tick Invasion (#21).

## Level and chapter ideas (deferred idea bank)

> **DEFERRED:** These concepts remain creative inventory. Do not interpret their numbering or
> detail as permission to build them before the active deep-slice gate passes.
>
> **Status tags (added 2026-07-22):** the numbering here is pitch order, not a build tracker — some
> of these were built long ago and just never got marked. Each entry below now carries one of:
> - **[BUILT]** — shipped as a mission in the current 23-mission roster (see
>   [`LEVEL-READINESS-SCORES.md`](LEVEL-READINESS-SCORES.md)); named where the shipped mechanic
>   evolved from the pitch.
> - **[DEFERRED]** — not built; still fair game once the roster unfreezes.
>
> Built-but-unlabeled note: several *other* shipped missions (Snack Heist, Sock Panic, Weenie
> Roundup, Mark the Yard, Gate Crash, Table Stealth, Squirrel Switcheroo, Bone Relay, Great Escape,
> Chaos Machine, Blanket Catch, Baby Bird Bedlam) do not correspond to any pitch below — they were
> invented during implementation, not drawn from this list.

### 1. The Great Backyard Squirrel Conspiracy

**[BUILT]** — shipped as `SquirrelConspiracy` / "The Great Backyard Squirrel Conspiracy" mission;
also tracked as a completed Codex goal ([`CODEX-GOAL-SQUIRREL-CONSPIRACY.md`](CODEX-GOAL-SQUIRREL-CONSPIRACY.md)).

Co-op chase/herding level.

- Squirrel runs along fence, under chairs, across pool equipment, into trees.
- One dog pressures; the other cuts off escape routes.
- Barking too early causes squirrel fake-outs.
- Hidden objective: find squirrel stash of stolen kibble/acorns.
- Failure: squirrel reaches taunt branch and drops leaves.
- Co-op puzzle upgrade: one dog baits the squirrel into a false route while the other blocks the real escape gap; the stash only reveals after both routes are controlled.

Core verbs: chase, bark, herd, defend.

### 2. Eagle Shadow Panic

**[BUILT]** — shipped as `EagleShadowPanic`; also tracked as a completed Codex goal
([`CODEX-GOAL-EAGLE-SHADOW.md`](CODEX-GOAL-EAGLE-SHADOW.md)).

Survival/rescue level.

- A shadow sweeps over the yard.
- Dogs hide under deck chairs, tables, cabana, human legs.
- One dog distracts while the other rescues a toy/treat.
- Final phase: united-front bark circle.
- Co-op puzzle upgrade: Cocoa holds a patio umbrella/tablecloth open as temporary shade while Cheddar dashes through the shade window, then the next sweep reverses the safe role.

Core verbs: hide, rescue, bark, defend.

### 3. Coyotes at the Fence

**[BUILT]** — shipped as `CoyotesFence`; also tracked as a completed Codex goal
([`CODEX-GOAL-COYOTES-FENCE.md`](CODEX-GOAL-COYOTES-FENCE.md)).

Defensive patrol level.

- Coyote tests weak spots in the yard.
- Dogs patrol left/right fence gaps.
- One barks while the other kicks dirt/fills holes.
- If dogs split too far, coyote targets the isolated dog.
- Boss gag: fake snack lure that Cheddar absolutely believes.
- Co-op puzzle upgrade: bark-pin, dirt-fill, then tug a board/hose into place before the coyote returns; each step needs a different dog role.

Core verbs: defend, rescue, bark, distract.

### 4. Pool Floaty Disaster

**[DEFERRED]** — not built as its own mission. Backyard Rescue has picked up generic wet/mud beats
(see [`LEVEL-READINESS-SCORES.md`](LEVEL-READINESS-SCORES.md) row 1), but the floaty/noodle
platforming puzzle below has not been implemented.

Precision/platforming level.

- Dogs hop between floaties, towels, pool noodles, deck chairs.
- Falling in triggers swim/wet/shake recovery.
- One dog pushes a floaty into position for the other.
- Wet dog makes indoor floors slippery afterward.
- Co-op puzzle upgrade: one dog anchors a pool noodle bridge while the other crosses, then the wet crossing dog must shake water onto a stuck floaty to make it slide.

Core verbs: balance, rescue, carry, shake.

### 5. The Forbidden Pool Toy

**[DEFERRED]** — not built.

Toy retrieval mission.

- Favourite toy lands on pool float.
- Dogs cannot simply jump straight in.
- Move pool noodles, knock beach balls, ride floaties.
- Cocoa anchors; Cheddar leaps.
- Bonus: avoid pool robot patrol.
- Co-op puzzle upgrade: one dog lures the pool robot away with ripples/barks while the other nudges the floaty route into place.

Core verbs: retrieve, balance, push, rescue.

### 6. Cleaning Day Invasion

**[DEFERRED]** — explicitly held back (see "Running gags" below: "a full new mission idea, not an
ambient gag; stays in the level-idea bank above until the roster unfreezes").

Stealth/chaos level.

- Avoid vacuum, mop, bucket, moved chairs.
- Rescue toys before they are put away.
- Track muddy pawprints without getting caught.
- Hide under couch during vacuum pass.
- Steal a dropped cleaning cloth and drag it to the dog couch.
- One dog distracts cleaner while the other sneaks.
- Co-op puzzle upgrade: Cheddar creates a noisy decoy with a toy basket while Cocoa sneaks through the vacuum's blind side to rescue the sacred toy.

Core verbs: hide, steal, distract, rescue.

### 7. Operation Pee Break: Teenager Phone Rescue

**[BUILT]** — shipped as `OperationPeeBreak`, the current active deep slice (see
[`DEEP-SLICE-OPERATION-PEE-BREAK.md`](DEEP-SLICE-OPERATION-PEE-BREAK.md)); this is the mission the
roster freeze is gating on.

Co-op human-manipulation puzzle.

Goal: get the teenager off the phone and make them take the dogs outside.

Tasks:
- knock leash off hook;
- drag shoe to door;
- bark in rhythm near teenager;
- paw at phone charger;
- bring toy, then reject toy, then stare at door;
- one dog blocks hallway while the other whines dramatically.

Meter: Bladder Emergency.

Co-op puzzle upgrade: one dog manipulates the charger/phone attention while the other assembles the walk evidence at the door; the teenager only understands when both signals line up.

Core verbs: distract, carry, bark, manipulate.

### 8. Kitchen Falling Food Frenzy

**[BUILT]** — shipped as `KitchenFoodFrenzy`, the first behavior-preserving mission-controller
extraction.

Arcade collection with danger filtering.

- Humans cook/eat; food falls from counters/table.
- Catch good food, dodge dangerous or gross food.
- One dog acts as table scout, one as floor sweeper.
- Hot food lands with warning circles.
- Combo for catching crumbs before they hit floor.
- Co-op puzzle upgrade: Cheddar can trigger drops from the chair/counter route while Cocoa catches or blocks bad items on the floor; some food must be nudged into a safe landing zone before it can be eaten.

Good drops:
- pancake;
- chicken;
- cheese;
- weenies;
- mystery delicious thing.

Bad drops:
- onion;
- spicy food;
- broccoli;
- medicine;
- hot pan drip;
- gross vegetable.

Core verbs: catch, dodge, communicate, score.

### 9. Dinner Table Begging Boss Fight

**[DEFERRED]** — not built as pitched. The shipped `TableStealth` mission is a different loop
(steal-a-steak sneak while the human is distracted, not begging/pity-mode softening), so it does
not count as this idea being built.

Social stealth.

- Maximize snack drops without getting banished.
- Stand near soft-hearted humans.
- Avoid strict human line-of-sight.
- Use head tilt, paw tap, dramatic sigh, tiny whine.
- Cocoa uses royal stare; Cheddar causes chaos under table.
- Co-op puzzle upgrade: Cocoa locks a soft-hearted human into pity mode while Cheddar moves under the table to redirect dropped food away from the strict human's sightline.

Core verbs: distract, beg, sneak, time.

### 10. The Dog Couch War

**[DEFERRED]** — not built. (Considered as a running-gag flourish for Pee Break and explicitly
declined for that mission — see "Running gags" below — but never built as its own level.)

Territory-control level/minigame.

- Dog couch is sacred land.
- Sunbeam moves around the room.
- Squishmallows spawn as temporary nap buffs.
- One dog defends couch while other gathers toys.
- Humans sit down and become moving hazards.
- Co-op puzzle upgrade: one dog lures a human off the couch while the other drags a blanket/toy to reserve the sacred spot before the sunbeam moves.

Core verbs: hold, steal, wrestle, defend.

### 11. The Great Toy Search

**[DEFERRED]** — not built as pitched. The shipped `ScentSearch` mission uses the same track+dig
core verb but a different premise (find a bone among dig mounds via hot/cold calls, not a hidden
named toy with scent-trail decoys), so it does not count as this idea being built.

House exploration/scent mission.

- A specific toy is hidden somewhere in the house.
- Dogs follow scent trails.
- Wrong toys are decoys.
- Trail gets confused by laundry, shoes, food, other dog scent.
- One dog sniffs while the other moves obstacles.
- Co-op puzzle upgrade: Cheddar detects food-contaminated decoy trails while Cocoa identifies the true toy/human scent; players must compare clues before digging into the wrong pile.

Possible toy names:
- The Honky Pig;
- The Dead Squirrel;
- The Disgusting Rope;
- The One They Both Suddenly Care About;
- The Toy Nobody Loved Until Today.

Core verbs: sniff, search, carry, steal.

### 12. Vet Appointment: The Betrayal

**[DEFERRED]** — not built.

Multi-stage comedic horror.

Stages:
- car ride balance;
- waiting room giant-dog avoidance;
- scale stand-still puzzle;
- exam table escape;
- nail grinder boss.

Co-op puzzle upgrade: held/stressed dog can wiggle, lean, or whine to expose timing while the free dog distracts staff, drags towel, or knocks treats to open the escape/rescue window.

Core verbs: calm, distract, escape, rescue.

### 13. Nail Clip Nightmare

**[DEFERRED]** — not built.

Boss/minigame.

- One dog is held.
- Held dog plays timing game to pull paws back.
- Free dog distracts by barking, knocking treats, dragging towel, tangling leash.
- Boss phases: clippers, grinder, paw inspection, "just one more nail."
- Co-op puzzle upgrade: the held dog chooses which paw to protect while the free dog creates one of several distractions; wrong distraction protects the wrong paw.

Core verbs: distract, rescue, rhythm, panic.

### 14. Car Ride Chaos

**[BUILT]** — shipped as `CarRide` / "Car Ride Chaos", redesigned 2026-07-14 into a full backseat
stage (see [`ARENA-PLAYABLE.md`](ARENA-PLAYABLE.md) "Car Ride Chaos").

Balance/window-smell level.

- Dogs slide left/right with turns.
- Window sniff gives points but increases danger.
- Bark at motorcycles, mail trucks, dogs, walkers.
- One dog balances while other collects window smells.
- Co-op puzzle upgrade: one dog sees incoming lurch cues through the window while the other must counterweight first; sliding toys/snacks tempt the wrong movement.

Meter: Smell Data Download.

Core verbs: balance, bark, collect, dodge.

### 15. Walkies: Leash Entanglement Simulator

**[BUILT]** — shipped as `LeashWalk` / "Walkies on the Leash", evolved into a tethered
checkpoint-scout-and-call mission rather than literal tree/pole wrap-and-unwrap (see
[`ARENA-PLAYABLE.md`](ARENA-PLAYABLE.md) "Walkies on the Leash").

Movement puzzle.

- Dogs connected by leash physics.
- Wrap/unwrap around trees, human legs, poles, mailboxes.
- Other dogs trigger bark meter.
- Perfect teamwork earns synchronized trot bonus.
- One dog wants destination; other wants one blade of grass for 14 seconds.
- Co-op puzzle upgrade: leash wrapping becomes a tool for opening gates, redirecting the human, or pulling objects, but must be undone before tension annoys everyone.

Core verbs: pull, untangle, bark, sniff.

### 16. Thunderstorm Blanket Fort

**[BUILT]** — shipped as `ThunderstormComfort`, evolved into an active reassure/answer/huddle
mission rather than a blanket-gathering fort (see [`ARENA-PLAYABLE.md`](ARENA-PLAYABLE.md)
"Thunderstorm Comfort").

Comfort/co-regulation level.

- Lights flicker.
- Thunder shockwaves interrupt movement.
- Dogs gather blankets/toys and find safe spots.
- One dog calms the other by staying close.
- Final objective: reach Sue/Ken/blanket nest.
- Co-op puzzle upgrade: one dog maintains the comfort huddle while the other fetches the currently needed comfort object; thunder changes which object matters next.

Core verbs: hide, comfort, carry, survive.

### 17. Amazon Delivery Defense

**[DEFERRED]** — not built.

Doorbell chaos level.

- Doorbell triggers frenzy.
- Dogs race to front door.
- Need enough barking to defend house, not so much they get locked away.
- Package may contain toy, treats, or boring human thing.
- Co-op puzzle upgrade: one dog barks through the window to hold the delivery driver's attention while the other verifies/smells whether the package is worth defending or ignoring.

Core verbs: bark, defend, control, chase.

### 18. Dog Ramp / Back-Safety Challenge

**[DEFERRED]** — not built.

Movement discipline level.

- Dogs should use ramps/stairs correctly.
- Cheddar wants to launch himself like an idiot.
- Cocoa uses stairs with queenly patience.
- Bonus for safe landings and synchronized movement.
- Co-op puzzle upgrade: Cocoa can model/hold the safe path while Cheddar must be intercepted or redirected away from unsafe jumps.

Core verbs: navigate, wait, time, protect.

### 19. Halloween Costume Escape

**[DEFERRED]** — not built.

Comedy movement level.

- Dogs escape costumes or use costume powers.
- Cheddar gets stuck in ridiculous outfit.
- Cocoa gets royal costume buff.
- Movement changes based on costume.
- Co-op puzzle upgrade: one costume blocks one dog but enables a weird tool for the other, forcing players to decide whether to escape or use the humiliation.

Core verbs: wiggle, escape, use, chase.

### 20. Secret Ravine / Dog Dream Adventure

**[DEFERRED]** — not built.

Fantasy expansion.

- Backyard opens into a magical ravine/dog imagination world.
- Dogs become heroic versions of themselves.
- Real-life toys become legendary artifacts.
- Boss can be absurd neighborhood villain or mythic squirrel.
- Co-op puzzle upgrade: real-life dog objects become legendary co-op tools — leash lasso, squeaky relic, blanket shield, long-dog bridge — each requiring both dogs to unlock the punchline.

Core verbs: adventure, chase, rescue, defend.

### 21. Tick Invasion: The Backyard Is Lost

**[DEFERRED]** — not built; remains deferred until the second couch-playtest gate passes (see
"Current production priority" below).

The backyard has exploded with ticks. They crawl onto both dogs continuously and the infestation
level climbs fast — the only way to fight back is for each dog to groom the other one, picking
ticks off before they overwhelm.

**Dog fantasy:** Every dog owner's nightmare made ridiculous. The dogs are their own first
responders, and the intimacy of mutual grooming becomes an actual survival mechanic.

**What Cheddar does differently:** Chaos puppy energy — Cheddar charges around attracting more
ticks per second than Cocoa but also grooms faster. High risk, high output.

**What Cocoa does differently:** Veteran queen composure — Cocoa accumulates ticks more slowly,
spots the worst infestations first, and her groom action clears a wider area (long-dog advantage).

**Co-op puzzle beat:** Players must position close enough to groom each other. If one dog is
overloaded and panics, they run erratically and become harder to lock down — the other dog must
catch up, bark to calm them, and hold still long enough to groom. Pure communication pressure.

**Pool as a risk/reward option:** Either dog can dive into the pool to knock off ticks instantly
— but they emerge slow and soggy, their wet coat collects ticks at double the rate for ten seconds,
and dragging a soaked Cheddar to safety while Cocoa is also being swarmed is exactly the kind of
disaster that makes couch co-op legendary.

**Mechanics sketch:**
- Tick count per dog is a visible meter (like panic meter). Zero is safe; full is game over for
  that dog.
- Standing close to partner triggers the groom interaction (same cuddle radius pattern as
  ThunderstormComfort) — reduces their tick count, slightly increases your own.
- Pool jump: instant tick reset but wet-slow penalty + doubled tick accumulation rate for ~10s.
  Cocoa gets out faster (she's done this before). Cheddar belly-flops and is useless for longer.
- Bark while grooming extends the reach — Cocoa's call holds Cheddar still so the groom connects.
- Fail: either dog hits max ticks. Cheddar fails messily. Cocoa fails with dignified
  disappointment.

**Twist / complication:** Mid-round, a Super Tick (boss tick?) targets one dog exclusively and
can't be groomed off — the affected dog must pool-dip while the other holds the line solo.

**Funny fail state:** Cheddar, completely covered, doing zoomies with ticks flying everywhere.
Cocoa sitting perfectly still, absolutely furious, while the meter maxes.

**Running gag tie-in:** "The pool is both terrifying and fascinating." Cheddar voluntarily cannonballs
in to get clean, immediately regrets it.

Core verbs: groom, comfort, rescue, carry, position.

### 22. Skunk Blast Mayhem

**[DEFERRED]** — newly earmarked 2026-07-22; not built.

A skunk has planted itself in the backyard, guarding something the dogs desperately want — a
prized **dead bird** (a treasure of the highest order). The dogs can't just charge in: the skunk
faces whoever it hears, and the moment its **tail lifts**, anything in front of it is about to get
blasted. The only way to the treasure is a two-dog con: one dog **lures** the skunk's attention
away while the other **snatches** the prize during the opening. Botch the timing and a dog gets
hilariously **skunked** — and now the mission stops being about the bird and becomes about getting
that dog clean before anyone can try again.

**Dog fantasy:** The forbidden backyard treasure guarded by a walking stink-bomb, plus the specific
domestic comedy of a skunked dog frantically rubbing itself all over the clean laundry while its
partner runs supply. Every dog owner has lived some version of "the dog found something dead and
also got sprayed." This makes both halves of that nightmare into mechanics.

**What Cheddar does differently:** Chaos-puppy energy makes him the natural **bait** — loud,
reckless, great at yanking the skunk's attention, but he over-commits and is far more likely to
still be in the blast cone when the tail goes up. High draw, high risk.

**What Cocoa does differently:** Veteran composure — Cocoa reads the skunk's tail tells earliest and
is the reliable **snatcher** (clean grab, gets out before the window closes). She also drags laundry
with authority. The grown-up runs the heist; the puppy runs the distraction.

**Co-op puzzle beat (the heist):** The skunk turns to face the **louder** dog. The lure dog has to
hold its attention (bark/movement) while the grab dog approaches from behind — but the **tail-lift
telegraph** is a shared danger clock: when it rises, *both* dogs must read it and bail, or whoever's
in the cone eats the spray. The prize only comes free if the lure holds *and* the grab lands inside
the same safe window. Pure "call it out / trust your partner" pressure.

**Co-op puzzle beat (the de-skunk scramble — the twist):** A sprayed dog becomes **STINKY**: slowed,
and the skunk can smell them coming, so they're useless as a lure until clean. They have to run
**into the house** and **rub/wrestle a laundry pile** to de-skunk — but each rub **uses up** that
laundry (it goes from fresh to funky). The **clean** dog becomes the supply line, **dragging fresh
laundry** out of the basket to the rubbing spot faster than the stinky dog burns through it. Roles
fully reverse: the hero grabber is now hauling towels while the disaster puppy scrubs. Only once
clean can the pair re-attempt the skunk.

**Mechanics sketch:**
- Skunk faces the loudest dog; **facing cone** is the danger zone. **Tail-lift telegraph** →
  short windup → **spray** (a cloud that skunks anything in the cone).
- Guarded object (dead bird) can only be grabbed while the skunk is turned away and not winding up.
- **Stinky** status: movement debuff + "skunk can smell you" (can't lure) + a visible stink meter
  per dog. Cleared only by rubbing on fresh laundry.
- **Laundry economy:** piles/basket of fresh laundry that deplete as they're rubbed; the clean dog
  drags fresh pieces from the basket to the stinky dog. Run out and you're stuck airing out slowly.
- Fail-forward, not game-over: getting skunked costs the de-skunk detour, not the run.

**Funny fail state:** Both dogs skunked at once, the whole yard a stink cloud, and the two of them
inside frantically rolling in the last towel while the laundry runs out — Cheddar belly-rubbing with
his legs in the air, Cocoa grimly grinding her shoulder into a sock like it's her job.

**Running gag tie-ins:** "Dropped food has religious significance" (the dead bird is sacred
treasure); "every toy becomes valuable only when the other dog wants it" (both suddenly need the
bird the instant the skunk guards it).

**Why it's a strong build candidate:** clear verbs, a readable shared telegraph (tail lift), a funny
recoverable failure that *becomes* the second co-op puzzle, hard role-reversal between lure/grab and
scrubber/supplier, and a fail loop that forces communication both ways. Fits the idea-bank
acceptance bar below.

Core verbs: distract, steal, rescue, carry, wrestle.

## 1v1 minigames

Use 1v1 minigames to break up co-op without changing the heart of the game.

- **Tug-of-War Supreme**: mash/rhythm/fake-outs/stamina.
- **Sunbeam King/Queen**: hold moving sunbeam zone.
- **Couch Claim**: control couch for 10 seconds.
- **Treat Drop Duel**: catch good drops, avoid bad drops.
- **Zoomies Tag**: tag transfers zoomies; furniture as drift corners.
- **Toy Hoarder**: collect toys into your bed; steal from other pile.
- **Best Bark Battle**: rhythm bark patterns against mailman/vacuum/coyote.
- **Who Can Be More Pathetic**: begging pose competition.
- **Blanket Burrow Race**: navigate under blankets with limited visibility.
- **Squirrel Dash**: squirrel is uncatchable; points for route/style.

## Running gags

Status as of 2026-07-05 — 7 of 12 shipped as small, purely cosmetic ambient flourishes (no scoring or
mechanic effect), each on a cooldown so they read as personality, not noise. See
`docs/NEXT-PRODUCTION-SLICE.md`'s running log for the exact commits.

- Cheddar believes every closed door is a personal attack. **Implemented**: `GateCrashMissionController.TrySpawnDoorOutrage()`.
- Cocoa has legal ownership of all sunbeams. **Implemented**: `BackyardRescueArtEnhancer.TrySpawnSunbeamClaim()`.
- Every toy becomes valuable only when the other dog wants it. **Implemented**: `BackyardRescueArtEnhancer.TrySpawnToyEnvy()`.
- Human legs are moving environmental hazards. Not yet — no current mission has an actual *moving*
  human/teenager to dodge (Walk Campaign, Table Stealth, and Pee Break's teenager are all stationary
  props at fixed zones). Would need a new mission or a rework of an existing human actor to fit; not
  worth forcing onto a stationary prop.
- "Just one more nail" is final-boss dialogue. Not yet — no obvious current-mission fit (would suit a
  future home-repair/construction-themed mission).
- The squirrel has villain monologues nobody understands. **Implemented**: `BackyardRescueArtEnhancer.TrySpawnSquirrelMumble()`.
- Cleaning day is an enemy invasion. Deliberately deferred — a full new mission idea, not an ambient
  gag; stays in the level-idea bank above until the roster unfreezes.
- The pool is both terrifying and fascinating. **Implemented**: `BackyardRescueArtEnhancer.TrySpawnPoolFascination()`.
- The couch is sacred land. Considered for Operation Pee Break (which has real, unused decorative couch
  scenery) and declined: the teenager sits almost exactly on top of the couch's position, so a
  couch-claim gag would be visually indistinguishable from a teenager-approach gag, and Pee Break is
  the frozen couch-test-#4 acceptance-gate mission — not worth the risk there for a cosmetic flourish.
- Dropped food has religious significance. **Implemented**: `BackyardRescueArtEnhancer.TrySpawnTreatReverence()`.
- Every walk is an intelligence-gathering mission. **Implemented**: `LeashWalkMissionController.TrySpawnIntelGathered()`.
- A phone-absorbed teenager is an NPC with broken AI. Already the core premise of Operation Pee Break
  itself (the Teenager's `TeenPresentationState`/distraction logic *is* this joke) rather than needing a
  separate ambient flourish on top of it.

## Current production priority

The active order is the seven-step depth-first sequence at the top of this document. Kitchen Falling
Food Frenzy is already implemented; it is the first behavior-preserving controller extraction, not
the next mission to build. Operation Pee Break is the only authorized new deep slice after that
extraction is green.

Juice, identity, readability, and co-op-puzzle improvements are driven by critical findings from
the baseline couch playtest. Cleaning Day, Tick Invasion, other level ideas, broad content passes,
and mission roster expansion remain deferred until the second couch-playtest gate passes.

## Implementation guardrails for agents

When implementing any new idea:

1. Preserve the current working Unity project and PlayMode tests.
2. Add deterministic test hooks for new mission logic.
3. Keep scene/object generation simple until authored assets are deliberately introduced.
4. Prefer one small playable slice over broad unplayable architecture.
5. Every new mechanic needs:
   - clear player verb;
   - visible feedback;
   - failure/recovery path;
   - at least one co-op interaction;
   - at least one authored co-op puzzle beat for substantial missions;
   - a manual acceptance checklist;
   - automated PlayMode coverage where feasible.
6. New mission prompts must include a **Co-op Puzzle Beat** section with player roles, lock/key dependency, readable hints, funny fail, world-state change, and test hooks.
7. Do not erase the prototype/docs history.
8. Update this document or a linked design doc when adding a major mechanic or level.

## Acceptance criteria for a "great" new level idea

A level idea is strong enough to build when it answers:

- What dog fantasy is this level delivering?
- What are Cheddar and Cocoa each doing differently?
- What forces players to communicate?
- What is the co-op puzzle beat where one dog creates an opening and the other turns it into progress?
- What is the twist, role reversal, deception, or complication before the beat gets stale?
- What can go wrong in a funny way?
- What is the 10-second video clip that would make Sue laugh?
- What is the simplest version that can be PlayMode tested?
