# Puzzle Beats: Barf Pressure Sensor and Dishwasher Catapult

Status: design capture. Not implemented yet.

This document intentionally captures **two separate Kitchen / House Chaos puzzle beats** that can be built independently or later combined into a larger kitchen mission.

1. **Cheddar Barf Pressure Sensor** — Cheddar eats forbidden people food to create a gross weight/key for a pressure sensor.
2. **Dishwasher Catapult / Hold-Down Platform** — one dog pins the springy dishwasher door so both dogs can safely access food; otherwise a dog gets launched.

Do not assume these are one mechanic. Each should stand on its own as a clear co-op puzzle beat.

---

## Puzzle Beat 1: Cheddar Barf Pressure Sensor

### Core fantasy

Cheddar eats people food he absolutely should not eat, gets increasingly dramatic, and barfs onto a floor sensor. The gross puddle becomes the weight/key that opens a route, cabinet, gate, pantry gap, or human attention event.

The joke: Cheddar's terrible decision becomes mechanically correct.

### Co-op lock/key relationship

- **Lock:** a pressure sensor / gross floor switch needs a weighted object, but the dogs cannot carry a normal object onto it yet.
- **Key:** Cheddar can create a temporary gross weight by eating enough people food and barfing in the right place.
- **Partner role:** Cocoa/partner helps guide, block, calm, position, or protect Cheddar so the barf lands on the target instead of becoming a slippery mess.

The beat should feel like: **Cheddar creates the disgusting key, while the partner makes sure the key lands where it matters.**

### Player roles

#### Cheddar role

- Sniffs out forbidden people food.
- Eats enough to charge a **Barf Meter**.
- Gets wobblier / more dramatic as the meter fills.
- Must be aimed or positioned near the sensor before the barf happens.
- Can overeat, barf early, or barf in the wrong place.

#### Cocoa / partner role

- Guides Cheddar toward the sensor.
- Blocks wrong food, bad routes, or human sightlines.
- Barks/cues Cheddar back toward the target.
- Can hold the sensor area clear while Cheddar arrives.
- May be better at reading the right pressure sensor / clean route.

### Beat flow

1. **Discover**
   - Players see a labeled sensor: `GROSS PRESSURE SENSOR`.
   - They see people food nearby but no obvious object to weigh the sensor down.

2. **Charge the gross key**
   - Cheddar eats food to fill `CHEDDAR BARF METER`.
   - Cocoa helps prevent wrong routing or over-snacking.

3. **Positioning tension**
   - Objective changes to `Aim Cheddar at the sensor`.
   - Cheddar wobbles, slows, or becomes harder to steer, but remains playable.

4. **Solution**
   - Cheddar barfs on the pressure sensor.
   - Sensor label changes to `BARF WEIGHT ACCEPTED`.
   - Route/cabinet/gate opens.

5. **Optional escalation**
   - Later beat requires choosing between multiple sensors: one opens progress, one triggers human cleanup, one attracts the cat/squirrel.

### Readable hints

- `PEOPLE FOOD - BAD IDEA?`
- `CHEDDAR BARF METER`
- `AIM CHEDDAR AT SENSOR`
- `GROSS PRESSURE SENSOR`
- `BARF WEIGHT ACCEPTED`
- Success pop: `GROSS KEY!`

### Funny fail states

- **Wrong barf:** Cheddar barfs off-target, creating a slippery puddle.
- **Overeat:** Cheddar gets briefly stunned/dramatic and must be redirected.
- **Cleanup risk:** human notices the wrong puddle and starts a cleanup timer.
- **Partner miss:** Cocoa fails to block or guide, so the barf lands somewhere useless but funny.

Failures should be short, recoverable, and readable.

### Tuning notes

- Barf must be stylized: comic green splat / gross sparkle, not realistic vomit.
- Keep it family-friendly: funny dog gross-out, not body horror.
- Cheddar should be the natural barf-meter dog because people food is his chaos domain.
- Cocoa should still matter through positioning, blocking, timing, or sensor-reading.

### Deterministic test hooks

Future implementation should expose hooks like:

- `ForceCheddarEatPeopleFood(amount)`
- `ForceCheddarBarfOnSensor()`
- `ForceCheddarBarfWrongSpot()`
- `BarfMeter`
- `GrossPressureSensorActive`
- `KitchenRouteUnlocked`

PlayMode tests should cover:

1. Cheddar eating fills Barf Meter;
2. barf on sensor activates the sensor;
3. activated sensor unlocks the route;
4. wrong barf does not unlock the route and creates readable failure;
5. replay resets food, barf puddles, sensor, and route state.

---

## Puzzle Beat 2: Dishwasher Catapult / Hold-Down Platform

### Core fantasy

The dishwasher door is a springy ramp/platform. If one dog jumps on it alone, it snaps upward and launches them. If one dog holds it down, both dogs can use it to reach forbidden food or cross a kitchen obstacle.

The joke: the dishwasher is both a trap and a tool.

### Co-op lock/key relationship

- **Lock:** food or route is only reachable by crossing/using the dishwasher door.
- **Key:** one dog must pin the dishwasher door down long enough for the other dog to move/eat/cross.
- **Failure:** if the platform is not pinned, the spring door launches the dog standing on it.

The beat should feel like: **one dog makes the dangerous appliance safe, while the other dog exploits the opening.**

### Player roles

#### Holding dog role

- Jumps or stands on the dishwasher door to keep it down.
- Must stay committed long enough for the partner to cross/eat/retrieve.
- Can release intentionally if a later puzzle requires a launch.
- Better suited to Cocoa because she is steadier and more controlled, but not necessarily hard-locked.

#### Moving dog role

- Crosses the pinned dishwasher door.
- Eats/retrieves forbidden food or reaches a new platform.
- Risks being launched if the holder leaves early.
- Cheddar is naturally funny here because he rushes the food and gets catapulted when the platform is not stabilized.

### Beat flow

1. **Discover**
   - Dishwasher door is partly open and labeled `SPRINGY!`.
   - Food is visible just beyond it.

2. **First mistake / laugh**
   - One dog runs onto the door alone.
   - Door snaps up and launches the dog backward.
   - HUD/objective teaches: `Pin the dishwasher door!`

3. **Co-op access**
   - One dog holds the door down.
   - Partner crosses/eats/retrieves.
   - If the holder leaves too early, partner launches.

4. **Success**
   - Both dogs get access to the food/route.
   - Optional reward: `DISHWASHER STABILIZED!`

5. **Optional escalation**
   - Later version uses the catapult intentionally: launch a toy, food, or dog onto a rug/counter/landing zone.

### Readable hints

- Dishwasher door label: `SPRINGY! HOLD DOWN`
- Warning pop: `DISHWASHER LAUNCH!`
- Hold cue: `HOLDING DOOR`
- Partner cue: `CROSS NOW!`
- Success pop: `DISHWASHER STABILIZED!`

### Funny fail states

- **Catapult fail:** dog gets launched, lands stunned, and the food remains out of reach.
- **Early release:** holder leaves too early and partner launches mid-crossing.
- **Greedy rush:** Cheddar rushes the food before Cocoa pins the platform and gets launched.
- **Wrong launch:** later intentional-launch puzzle fires the wrong dog/object.

Failures should be short, recoverable, and comedic.

### Tuning notes

- This should be a physical comedy platform beat, not a gross-out beat.
- Launch should be readable and funny: squash/stretch, arc, yelp/bark pop, brief stun.
- Do not make the holding dog bored. Give them micro-decisions: hold, release, bark-warning, reposition, or choose intentional launch timing.
- It can be used in Kitchen Falling Food Frenzy as a one-off beat or expanded into a full Kitchen appliance puzzle chain.

### Deterministic test hooks

Future implementation should expose hooks like:

- `ForceDishwasherPinned(dog)`
- `ForceDishwasherReleased()`
- `ForceDishwasherLaunch(dog)`
- `IsDishwasherPinned`
- `DishwasherDoorAngle`
- `DogLaunchedByDishwasher`
- `FoodRouteAccessible`

PlayMode tests should cover:

1. unpinned dishwasher launches dog;
2. pinned dishwasher allows partner access;
3. early release launches partner;
4. both dogs can eat/retrieve after successful stabilization;
5. replay resets dishwasher angle, launched/stunned state, food access, and route state.

---

## Best mission fit

Primary fit: **Kitchen Falling Food Frenzy** or a new House Chaos mission.

Possible mission names:

- Dishwasher Catapult
- Cheddar's Bad Idea
- The Barf Button
- Kitchen Gross Key
- The Forbidden Plate Rinse
- Appliance Betrayal

## Why these belong

Both beats hit the co-op puzzle doctrine, but they do it differently:

- **Barf Pressure Sensor** is a gross lock/key puzzle: create and place the key.
- **Dishwasher Catapult** is a physical platform puzzle: stabilize a dangerous route.

They can be separate missions, separate rooms, or two stages in one larger kitchen mission. Keep them independent until implementation proves each is fun on its own.
