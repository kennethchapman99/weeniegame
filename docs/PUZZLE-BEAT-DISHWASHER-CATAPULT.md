# Puzzle Beat: Dishwasher Catapult / Barf Pressure Plate

Status: design capture. Not implemented yet.

This is a high-personality co-op puzzle beat for a future Kitchen / House Chaos mission. It turns gross dog comedy into a real lock/key puzzle: Cheddar has to eat forbidden people food until he barfs onto a pressure sensor, while the other dog controls a springy dishwasher door so both dogs can access the food safely.

## Core fantasy

The dogs discover that the dishwasher door is not just furniture. It is a dangerous launch platform, a food ramp, and a pressure mechanism. To get the forbidden people food and unlock the next route, they must coordinate instead of both rushing the snack.

The joke: Cheddar thinks eating mystery people food is the plan. He is technically right, but only because barf becomes the key.

## Co-op lock/key relationship

- **Lock:** a pressure sensor / gross floor switch only activates when weighted by Cheddar's barf puddle.
- **Key A:** Cheddar must eat enough people food to fill a **Barf Meter**.
- **Key B:** Cocoa or the partner dog must keep the dishwasher door pinned down so the dogs can safely reach the food.
- **Failure:** if nobody pins the dishwasher door, the spring snaps it up and launches the dog standing on it.

The beat should feel like: **Cheddar creates the disgusting key, but Cocoa creates the safe opportunity.**

## Player roles

### Cheddar role

- Sniffs out the forbidden food.
- Eats enough to charge the Barf Meter.
- Gets wobblier / more dramatic as the meter fills.
- Barfs onto a target sensor when positioned correctly.
- Can be tempted to keep eating, causing a bad barf location or temporary stun.

### Cocoa / partner role

- Jumps or stands on the dishwasher door to hold it down.
- Stabilizes the platform long enough for Cheddar to reach/eat.
- Can release intentionally to catapult a dog or object if the puzzle later requires it.
- Uses steadier control to keep Cheddar from being launched or eating from the wrong spot.

## Beat flow

1. **Discover**
   - Dogs enter kitchen.
   - Forbidden food is visible on/inside the dishwasher or nearby counter edge.
   - The dishwasher door is partly open and spring-loaded.

2. **First mistake / laugh**
   - One dog runs onto the dishwasher alone.
   - Door snaps up and launches the dog backward.
   - HUD/objective teaches: `Pin the dishwasher door!`

3. **Co-op access**
   - Cocoa stands/jumps on the dishwasher door to hold it down.
   - Cheddar crosses the held door to eat people food.
   - If Cocoa leaves too early, Cheddar is catapulted.

4. **Gross key creation**
   - Cheddar eats enough to fill Barf Meter.
   - Objective changes to `Aim Cheddar at the pressure sensor`.
   - Cheddar movement becomes slightly wobbly but still playable.

5. **Pressure plate solution**
   - Cheddar barfs on the sensor.
   - Barf puddle weighs down / activates the gross pressure plate.
   - A cabinet, baby gate, pantry gap, or next kitchen route opens.

6. **Optional escalation**
   - Later version requires using the dishwasher catapult on purpose: Cocoa releases the door to launch a toy/weenie into a basket, or to fling Cheddar onto a rug route.

## Readable hints

- Dishwasher door label: `SPRINGY! HOLD DOWN`
- Food label: `PEOPLE FOOD - BAD IDEA?`
- Barf Meter label: `CHEDDAR BARF METER`
- Sensor label before solution: `GROSS PRESSURE SENSOR`
- Sensor label after solution: `BARF WEIGHT ACCEPTED`
- Warning pop: `DISHWASHER LAUNCH!`
- Success pop: `GROSS KEY!`

## Funny fail states

- **Catapult fail:** dog gets launched, lands stunned, and the food remains out of reach.
- **Wrong barf fail:** Cheddar barfs off-target, creating a slippery puddle that must be avoided or cleaned up by time.
- **Overeat fail:** Cheddar ignores the sensor, eats too much, and needs Cocoa to bark/guide him back.
- **Solo-player anti-pattern fail:** if one dog tries to do everything, the platform launches or the barf misses.

Failures should be short, recoverable, and funny. No long punishment.

## Gameplay tuning notes

- Barf should be stylized and non-realistic: a comic green splat / gross sparkle, not realistic vomit.
- Keep it family-friendly: funny dog gross-out, not body-horror.
- Cheddar should be better/faster at charging the Barf Meter because people food is his chaos domain.
- Cocoa should be better at stabilizing the dishwasher door because she is steadier and more controlled.
- Do not make Cocoa useless while holding. Give her micro-decisions: hold, release, bark-warning, reposition, or block Cheddar from wrong food.

## Deterministic test hooks

Future implementation should expose hooks like:

- `ForceDishwasherPinned(dog)`
- `ForceDishwasherLaunch(dog)`
- `ForceCheddarEatPeopleFood(amount)`
- `ForceCheddarBarfOnSensor()`
- `ForceCheddarBarfWrongSpot()`
- `IsDishwasherPinned`
- `BarfMeter`
- `GrossPressureSensorActive`
- `KitchenRouteUnlocked`

PlayMode tests should cover:

1. unpinned dishwasher launches dog;
2. pinned dishwasher allows food access;
3. Cheddar eating fills Barf Meter;
4. barf on sensor unlocks route;
5. wrong barf does not unlock route and creates readable failure;
6. replay resets food, barf, sensor, door, and route state.

## Best mission fit

Primary fit: **Kitchen Falling Food Frenzy** or a new House Chaos mission.

Possible mission names:

- Dishwasher Catapult
- The Forbidden Plate Rinse
- Cheddar's Bad Idea
- Kitchen Gross Key
- The Barf Button

## Why this belongs

This beat hits the co-op puzzle doctrine:

- one dog creates the key;
- the other dog creates the safe access window;
- the world changes visibly;
- failure is funny and readable;
- Cheddar/Cocoa identity matters;
- the mechanic is gross, personal, dog-authentic, and memorable.
