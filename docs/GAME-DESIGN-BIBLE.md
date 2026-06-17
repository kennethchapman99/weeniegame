# Cheddar & Cocoa Game Design Bible

This document is the durable creative source of truth for the Cheddar & Cocoa couch co-op game. Claude, Codex, and any future implementation agent should read this before proposing or implementing new gameplay.

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

## Level and chapter ideas

### 1. The Great Backyard Squirrel Conspiracy

Co-op chase/herding level.

- Squirrel runs along fence, under chairs, across pool equipment, into trees.
- One dog pressures; the other cuts off escape routes.
- Barking too early causes squirrel fake-outs.
- Hidden objective: find squirrel stash of stolen kibble/acorns.
- Failure: squirrel reaches taunt branch and drops leaves.

Core verbs: chase, bark, herd, defend.

### 2. Eagle Shadow Panic

Survival/rescue level.

- A shadow sweeps over the yard.
- Dogs hide under deck chairs, tables, cabana, human legs.
- One dog distracts while the other rescues a toy/treat.
- Final phase: united-front bark circle.

Core verbs: hide, rescue, bark, defend.

### 3. Coyotes at the Fence

Defensive patrol level.

- Coyote tests weak spots in the yard.
- Dogs patrol left/right fence gaps.
- One barks while the other kicks dirt/fills holes.
- If dogs split too far, coyote targets the isolated dog.
- Boss gag: fake snack lure that Cheddar absolutely believes.

Core verbs: defend, rescue, bark, distract.

### 4. Pool Floaty Disaster

Precision/platforming level.

- Dogs hop between floaties, towels, pool noodles, deck chairs.
- Falling in triggers swim/wet/shake recovery.
- One dog pushes a floaty into position for the other.
- Wet dog makes indoor floors slippery afterward.

Core verbs: balance, rescue, carry, shake.

### 5. The Forbidden Pool Toy

Toy retrieval mission.

- Favourite toy lands on pool float.
- Dogs cannot simply jump straight in.
- Move pool noodles, knock beach balls, ride floaties.
- Cocoa anchors; Cheddar leaps.
- Bonus: avoid pool robot patrol.

Core verbs: retrieve, balance, push, rescue.

### 6. Cleaning Day Invasion

Stealth/chaos level.

- Avoid vacuum, mop, bucket, moved chairs.
- Rescue toys before they are put away.
- Track muddy pawprints without getting caught.
- Hide under couch during vacuum pass.
- Steal a dropped cleaning cloth and drag it to the dog couch.
- One dog distracts cleaner while the other sneaks.

Core verbs: hide, steal, distract, rescue.

### 7. Operation Pee Break: Teenager Phone Rescue

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

Core verbs: distract, carry, bark, manipulate.

### 8. Kitchen Falling Food Frenzy

Arcade collection with danger filtering.

- Humans cook/eat; food falls from counters/table.
- Catch good food, dodge dangerous or gross food.
- One dog acts as table scout, one as floor sweeper.
- Hot food lands with warning circles.
- Combo for catching crumbs before they hit floor.

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

Social stealth.

- Maximize snack drops without getting banished.
- Stand near soft-hearted humans.
- Avoid strict human line-of-sight.
- Use head tilt, paw tap, dramatic sigh, tiny whine.
- Cocoa uses royal stare; Cheddar causes chaos under table.

Core verbs: distract, beg, sneak, time.

### 10. The Dog Couch War

Territory-control level/minigame.

- Dog couch is sacred land.
- Sunbeam moves around the room.
- Squishmallows spawn as temporary nap buffs.
- One dog defends couch while other gathers toys.
- Humans sit down and become moving hazards.

Core verbs: hold, steal, wrestle, defend.

### 11. The Great Toy Search

House exploration/scent mission.

- A specific toy is hidden somewhere in the house.
- Dogs follow scent trails.
- Wrong toys are decoys.
- Trail gets confused by laundry, shoes, food, other dog scent.
- One dog sniffs while the other moves obstacles.

Possible toy names:
- The Honky Pig;
- The Dead Squirrel;
- The Disgusting Rope;
- The One They Both Suddenly Care About;
- The Toy Nobody Loved Until Today.

Core verbs: sniff, search, carry, steal.

### 12. Vet Appointment: The Betrayal

Multi-stage comedic horror.

Stages:
- car ride balance;
- waiting room giant-dog avoidance;
- scale stand-still puzzle;
- exam table escape;
- nail grinder boss.

Core verbs: calm, distract, escape, rescue.

### 13. Nail Clip Nightmare

Boss/minigame.

- One dog is held.
- Held dog plays timing game to pull paws back.
- Free dog distracts by barking, knocking treats, dragging towel, tangling leash.
- Boss phases: clippers, grinder, paw inspection, "just one more nail."

Core verbs: distract, rescue, rhythm, panic.

### 14. Car Ride Chaos

Balance/window-smell level.

- Dogs slide left/right with turns.
- Window sniff gives points but increases danger.
- Bark at motorcycles, mail trucks, dogs, walkers.
- One dog balances while other collects window smells.

Meter: Smell Data Download.

Core verbs: balance, bark, collect, dodge.

### 15. Walkies: Leash Entanglement Simulator

Movement puzzle.

- Dogs connected by leash physics.
- Wrap/unwrap around trees, human legs, poles, mailboxes.
- Other dogs trigger bark meter.
- Perfect teamwork earns synchronized trot bonus.
- One dog wants destination; other wants one blade of grass for 14 seconds.

Core verbs: pull, untangle, bark, sniff.

### 16. Thunderstorm Blanket Fort

Comfort/co-regulation level.

- Lights flicker.
- Thunder shockwaves interrupt movement.
- Dogs gather blankets/toys and find safe spots.
- One dog calms the other by staying close.
- Final objective: reach Sue/Ken/blanket nest.

Core verbs: hide, comfort, carry, survive.

### 17. Amazon Delivery Defense

Doorbell chaos level.

- Doorbell triggers frenzy.
- Dogs race to front door.
- Need enough barking to defend house, not so much they get locked away.
- Package may contain toy, treats, or boring human thing.

Core verbs: bark, defend, control, chase.

### 18. Dog Ramp / Back-Safety Challenge

Movement discipline level.

- Dogs should use ramps/stairs correctly.
- Cheddar wants to launch himself like an idiot.
- Cocoa uses stairs with queenly patience.
- Bonus for safe landings and synchronized movement.

Core verbs: navigate, wait, time, protect.

### 19. Halloween Costume Escape

Comedy movement level.

- Dogs escape costumes or use costume powers.
- Cheddar gets stuck in ridiculous outfit.
- Cocoa gets royal costume buff.
- Movement changes based on costume.

Core verbs: wiggle, escape, use, chase.

### 20. Secret Ravine / Dog Dream Adventure

Fantasy expansion.

- Backyard opens into a magical ravine/dog imagination world.
- Dogs become heroic versions of themselves.
- Real-life toys become legendary artifacts.
- Boss can be absurd neighborhood villain or mythic squirrel.

Core verbs: adventure, chase, rescue, defend.

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

- Cheddar believes every closed door is a personal attack.
- Cocoa has legal ownership of all sunbeams.
- Every toy becomes valuable only when the other dog wants it.
- Human legs are moving environmental hazards.
- "Just one more nail" is final-boss dialogue.
- The squirrel has villain monologues nobody understands.
- Cleaning day is an enemy invasion.
- The pool is both terrifying and fascinating.
- The couch is sacred land.
- Dropped food has religious significance.
- Every walk is an intelligence-gathering mission.
- A phone-absorbed teenager is an NPC with broken AI.

## Near-term build priorities

Do not jump straight to full art production. The next best work is to make the existing vertical slice feel great, readable, and funny while preserving test coverage.

Recommended order:

1. **Juice and readability pass on current Backyard Mission**
   - better placeholder dog animation;
   - bark timing feedback;
   - clear objective arrows/callouts;
   - more satisfying squirrel/predator feedback;
   - better round-end feedback.

2. **Dog identity pass**
   - distinct Cheddar/Cocoa silhouettes, colors, idle poses, movement feel;
   - visible personality states: zoomies, wet, stunned, barking, tugging, proud, sad.

3. **Kitchen Falling Food Frenzy vertical slice**
   - compact, high-replay, funny, easy to test;
   - introduces catch/dodge and good-food/bad-food filtering.

4. **Cleaning Day stealth/chaos prototype**
   - moving hazards, toy rescue, hide/distract roles.

5. **Operation Pee Break puzzle prototype**
   - uniquely personal co-op puzzle; strong identity differentiator.

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
   - a manual acceptance checklist;
   - automated PlayMode coverage where feasible.
6. Do not erase the prototype/docs history.
7. Update this document or a linked design doc when adding a major mechanic or level.

## Acceptance criteria for a "great" new level idea

A level idea is strong enough to build when it answers:

- What dog fantasy is this level delivering?
- What are Cheddar and Cocoa each doing differently?
- What forces players to communicate?
- What can go wrong in a funny way?
- What is the 10-second video clip that would make Sue laugh?
- What is the simplest version that can be PlayMode tested?
