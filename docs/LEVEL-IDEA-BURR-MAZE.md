# Level Idea: Burr Maze — Operation Sticky Coat

> **Status: DEFERRED IDEA BANK.** Capture only. Do not implement or add to the mission roster until
> Operation Pee Break passes its second two-player couch-playtest gate and the roster is explicitly
> unfrozen.

## Elevator pitch

A top-down tactical-stealth adventure through a dense backyard burr patch. Cheddar and Cocoa must
cross a shifting plant maze while avoiding patrolling birds, noisy ground hazards, and dive attacks.
Brushing the wrong plants adds burrs to their long coats. The central rule is simple and escalating:
**more burrs cause more burrs** — every burr cluster makes the dog wider, noisier, slower, and more
likely to snag the next plant.

The visual language should evoke a playful dog-scale stealth game: readable patrol cones, bird
shadows, rustling vegetation, safe hiding pockets, short route-planning pauses, and sudden comic
scrambles when a clean infiltration becomes a rolling burr disaster.

## Dog fantasy

Long-haired dachshunds disappear into an overgrown patch for thirty seconds and emerge looking like
walking seed collections. The maze feels enormous from dog height even though it is only one ugly
corner of the yard. Birds are not military guards in fiction; they are territorial backyard bullies
that interpret every rustle as an invitation to investigate.

## Mission objective

Get both dogs from the maze entrance to the far-side exit with the retrieved objective item. The
first version can use a favourite ball or dropped weenie bag as the reason to enter. The objective is
not a collectible hunt: it is a coordinated infiltration, retrieval, and escape.

Recommended structure:

1. **Infiltrate:** learn patrol routes, cover, noisy tiles, and burr plants.
2. **Retrieve:** reach the centre clearing and grab the prized object.
3. **Escape:** patrols intensify, wind shifts move parts of the maze, and accumulated burrs make the
   return route progressively harder.

## Camera and readability

- Fixed or softly following **top-down / high 3/4 camera** so the whole local stealth problem is
  readable without losing the dogs in foliage.
- Birds use clear sight cones on the ground plus a moving shadow before a dive.
- Burr plants pulse or shake before contact; already attached burrs remain visibly readable on each
  dog's silhouette.
- Safe cover pockets have a distinct flattened-grass or leaf-canopy treatment.
- The route should feel maze-like without becoming visually confusing: major junctions need unique
  landmarks, not repeated green corridors.

## Core loop

1. Watch bird routes and environmental timing.
2. Move between cover pockets.
3. Choose a clean longer route or a risky burr-lined shortcut.
4. Use one dog to distract, reveal, hold, or open a route for the other.
5. Stop in safe pockets to groom burrs off before the burden cascades.
6. Reach the objective and escape together.

Core verbs: **hide, sneak, distract, sniff, groom, rescue, carry, bark.**

## Signature mechanic: Burr Cascade

Each dog has a visible **Burr Load** with several readable stages rather than an abstract percentage.

| Stage | Visual state | Gameplay effect |
|---|---|---|
| Clean | normal coat | full speed, quiet movement, normal collision |
| Prickly | a few obvious burr clusters | slight rustle/noise increase |
| Snagged | multiple clusters along ears, chest, belly, and tail | slower turns; larger snag radius; noisy cover entry |
| Burr Beast | heavily covered, silhouette visibly swollen | major slowdown; attracts bird investigation; narrow routes become blocked |
| Tumbleweed | comic critical state | dog gets stuck or rolls after a dive/gust until rescued |

**More burrs cause more burrs:** Burr Load should create a controlled feedback loop.

- Attached burrs increase the dog's effective snag radius near burr plants.
- Higher Burr Load makes dry leaves and stalks produce more noise.
- At high load, squeezing through narrow passages shakes loose nearby seed heads, briefly creating a
  small burr cloud.
- Burr accumulation must remain recoverable. It creates pressure and comedy, not an invisible death
  spiral.

## Mutual grooming

Safe pockets allow one dog to groom burrs from the other using the existing mutual-grooming design
language.

- Grooming reduces the target's Burr Load but transfers a small amount to the groomer.
- The cleaner dog can bark once to make the overloaded dog hold still, widening the effective groom
  window.
- Grooming in unsafe ground risks attracting a bird because both dogs are stationary and noisy.
- Players must decide whether to share the burden, fully clean one dog, or keep moving before patrols
  return.

## Threats and obstacles

### Patrolling birds

Use crows, blue jays, grackles, or other backyard birds rather than another full eagle mission.

- **Ground patrol:** bird hops between perches and scans a cone.
- **Perch patrol:** bird rotates its view from a fence, branch, or garden post.
- **Investigate rustle:** suspicious sound creates a temporary search marker instead of instant
  punishment.
- **Dive attack:** a growing shadow and audio chirp telegraph the strike. Reaching cover avoids it.
- A successful dive can scatter the carried objective, add panic movement, or drive a dog into burr
  plants.

### Environmental obstacles

- **Dry leaves:** noisy floor zones that expand bird suspicion.
- **Thorn arches / narrow tunnels:** clean dogs fit; Burr Beasts do not.
- **Wind gusts:** visibly bend the maze, briefly opening one corridor and closing another.
- **Mud patches:** quiet but slow; useful stealth route with a movement cost.
- **Spiderwebs / sticky stems:** short root or struggle state that the partner can free quickly.
- **Falling seed heads:** triggered by careless movement or bird dives, adding localized burr bursts.
- **Rabbit or squirrel fake-outs:** harmless movement that can pull a bird's attention or startle a
  player into the wrong route.

## Dog asymmetry

### Cheddar

- Faster burst movement between cover pockets.
- Can squeeze through the smallest clean-coat gaps.
- Builds Burr Load faster because he charges through vegetation and his fluffy ears/tail snag first.
- Stronger bark distraction: excellent at pulling a bird's cone away, but likely to create too much
  suspicion if spammed.
- At critical load, his failure animation becomes a ridiculous rolling burr tumbleweed.

### Cocoa

- Reads bird patrol direction and upcoming wind changes earlier through clearer UI tells.
- Moves more quietly and accumulates burrs more slowly.
- Better groomer: removes a wider burr cluster per pass and steadies Cheddar faster.
- Can hold a flexible plant gate or flattened-grass cover open while Cheddar passes.
- At critical load, she becomes a furious, dignified burr-covered loaf who refuses to move until
  assisted.

## Co-op Puzzle Beat: The Double-Blind Crossing

### Setup

The centre corridor is watched by two birds from opposite sides. A movable patch of broad leaves can
hide only one dog at a time, and the objective item sits beyond the crossing.

### Roles

- **Dog A — Decoy/spotter:** exposes themselves briefly to pull one bird's sight cone and calls the
  safe timing.
- **Dog B — infiltrator:** moves under the leaf cover, crosses the blind zone, and retrieves the item.

### Lock/key dependency

- The infiltrator cannot see the second bird's full patrol route from under cover.
- The spotter can see the route but cannot retrieve the item while maintaining the distraction.
- The item only comes free after the spotter barks at the right perch and the infiltrator grabs during
  the resulting investigation window.

### Readable hints

- Bird heads and ground cones snap toward the latest bark/rustle.
- The safe lane receives a brief moving highlight when both birds are looking away.
- The infiltrator's cover rustles harder as Burr Load rises, clearly shrinking the safe timing window.

### Funny failure

A mistimed crossing causes a dive that knocks the infiltrator directly through a hanging seed head.
They emerge covered in burrs, lose the item, and must be groomed while the birds argue over the
commotion.

### World-state change

Success knocks the broad-leaf cover down into a permanent shortcut for the escape phase. Failure
leaves the seed head broken and spreads extra burr zones onto the return route.

### Test hooks

- Force each bird into a known patrol phase.
- Set each dog's Burr Load directly.
- Trigger or suppress a bark investigation.
- Verify the item cannot release outside the valid distraction window.
- Verify success opens the shortcut.
- Verify failure adds a burr burst and leaves the mission recoverable.

## Escalation and role reversal

The escape phase should change the stealth problem rather than simply reverse the map.

- The dog carrying the objective moves more slowly and cannot use the strongest bark distraction.
- Wind reshapes two corridors.
- Birds become suspicious faster after discovering the missing object.
- If one dog is heavily burred, the cleaner dog must lead and create safe grooming stops.
- Mid-escape, the carrier may need to drop the item and become the decoy while the partner takes over,
  forcing an explicit role reversal.

## Fail-forward rules

- Bird detection raises alert and triggers investigation/dive pressure; it should not immediately
  reset the whole mission.
- Burr overload creates a rescue problem, not instant game over.
- A Tumbleweed dog can still wiggle to indicate direction while the free dog barks, grooms, or pushes
  them into cover.
- Full failure occurs only if both dogs are immobilized in exposed ground or the objective is lost to
  repeated bird grabs for too long.

## Scoring and replayability

Possible scoring signals:

- both dogs escape;
- objective recovered;
- low combined Burr Load at exit;
- no full bird alerts;
- successful partner grooming/rescue;
- clean role swap during escape;
- optional risky shortcut used;
- time bonus only after readability and co-op behaviour are proven fun.

Modifiers:

- stronger wind;
- denser burr plants;
- faster bird patrols;
- fewer safe grooming pockets;
- “Cheddar magnet coat” — Cheddar gains burrs faster;
- “Royal passage” — Cocoa can open one unique clean route.

## Ten-second showcase clip

Cheddar tries a shortcut, instantly snowballs from three burrs into a full rolling tumbleweed, a bird
dives and misses, Cocoa steps out of cover with visible disgust, barks him still, rips off one giant
burr cluster, and the pair barely slide under a leaf tunnel as the patrol cone sweeps over them.

## Simplest PlayMode-testable slice

Build only after the roster gate opens:

- one small grid maze;
- two bird patrol cones with investigate and dive states;
- clean, noisy, burr, mud, cover, and exit tiles;
- five-stage Burr Load per dog;
- one mutual-groom interaction;
- one carried objective;
- the Double-Blind Crossing puzzle;
- deterministic patrol, burr, grooming, success, and fail-forward assertions.

Do not begin with procedural maze generation, realistic foliage simulation, or complex bird flock AI.
The authored route and readable stealth timing are the level.

## Primary risks

- **Visibility:** dense foliage can hide the dogs and undermine couch readability. Keep vegetation
  translucent, cut away, or flattened around each dog.
- **Punishing feedback loop:** the Burr Cascade can become hopeless. Cap acceleration and guarantee
  recoverable grooming pockets.
- **Generic stealth:** patrol cones alone are not enough. Burr accumulation, grooming transfer,
  dog-specific movement, and the role-reversal escape must remain central.
- **Waiting:** stealth can produce dead time. Patrol cycles should be short, manipulable through bark
  and rustle, and rarely require standing still for more than a few seconds.
- **Overlapping bird identity:** avoid recreating Eagle Shadow Panic. These birds investigate sound and
  guard maze routes; they are patrol systems, not a giant aerial predator survival set-piece.
