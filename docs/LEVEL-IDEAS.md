# Level Ideas (idea bank — open for building)

> **Status: OPEN IDEA BANK** (unfrozen 2026-07-22, owner decision). Roster expansion no longer
> waits on a couch-playtest gate; pull any idea below into the Unity project whenever asked, same
> "done" bar as any other mission (compile-clean, PlayMode coverage, manual acceptance notes).

Round/level concepts captured for later. The data-driven scene + (M7) room-schema seams are
designed so a new level is mostly a `SceneDef` + painter + a few systems, not engine surgery.

> **Newest ideas live in the design bible's idea bank or linked detailed briefs**
> ([`GAME-DESIGN-BIBLE.md`](GAME-DESIGN-BIBLE.md), "Level and chapter ideas"), including:
> - **#21 Tick Invasion** — mutual-grooming survival mission; build in progress 2026-07-22.
> - **#22 Skunk Blast Mayhem** (2026-07-22) — a skunk guards a dead-bird treasure; lure/grab heist
>   with a tail-lift spray telegraph, and a getting-skunked fail that turns into a laundry-rubbing
>   de-skunk co-op scramble. Built 2026-07-22.
> - **#23 Burr Maze** (2026-07-22, owner pitch) — top-down hedge maze thick with burrs, patrolled
>   by the neighbor's cat Metal-Gear-Solid style (vision cones, patrol routes, hiding spots);
>   accumulated burrs slow/rustle a dog until the partner holds still to pick them clean. Built
>   2026-07-22 as `BurrMazeMissionController`. Detailed brief:
>   [`LEVEL-IDEA-BURR-MAZE.md`](LEVEL-IDEA-BURR-MAZE.md) ("Operation Sticky Coat").

---

## 🍗 The Kitchen — "food drop"

**Pitch:** Food falls from the kitchen table; the dogs scramble to grab and eat it. Introduces
the first **asymmetric dog abilities** (today the dogs differ only in wrestle odds + stair speed),
which makes "pick your doxie" a real strategic choice.

### Core loop
- Food items drop from the table edge on a telegraph (like the yard treat), land on the floor,
  and can be grabbed. A grabbed item isn't scored instantly — it must be **eaten** (a short
  per-dog "chew/eat" state), and only then does it count.

### Asymmetric abilities (the heart of this level)
| Dog | Eating | Risk | Special |
|---|---|---|---|
| **Cheddar** (chaos puppy) | **Eats faster** — shorter chew time, can clear food quickly | **Sometimes gets sick and barfs** — a barf state that briefly locks him out (and maybe leaves a hazard tile?) | Can **jump onto a chair** to snatch food **right off the table** before it even drops |
| **Cocoa** (veteran) | **Needs a beat to chew** — longer chew time before a grab counts | steadier — no barf risk | (no chair leap; the grown-up plays it safe) |

- **Cheddar's barf:** a random chance on eat (tune like the wrestle reversal odds). While
  barfing he's a `MovementMode` ('sick'/'barf') — rooted, can't grab — the cost of his speed.
  Optional: the barf puddle is a temporary avoid-tile.
- **Cheddar's chair jump:** extends the existing **jump** (M6) — he can leap onto a chair tile
  adjacent to the table and grab table food directly (higher value / earlier than floor food).
  **But a person seated in that chair may swat him off** — a timed hazard: if Cheddar is on the
  chair when the seated human "swats" (telegraphed), he's knocked off (stun/knockback, like a
  predator grab miss). So the chair is high-reward, high-risk, Cheddar-only.

### Entities / systems this needs
- `food` system: table-edge drop scheduler + telegraph, floor landing, grab → per-dog **chew
  timer** → score on finish. (Reuses the treat telegraph + the spot/couch "hold a timer" pattern.)
- Dog states: `'sick'` (Cheddar barf lockout) added to `MovementMode`; a per-dog `chewT` overlay.
- Chair zones (table-adjacent) as **map data** (fits the M7 room schema): chair tile + the
  seated-human swat hazard (telegraph → swat window → knockback), Cheddar-only interaction.
- Balance entries: drop cadence, food value(s), `chewTime: { cheddar, cocoa }`, `barfChance`
  (Cheddar), `barfLockout`, chair-food value, swat telegraph/cooldown.
- AI: Cocoa prioritises safe floor food; chair-leap logic is Cheddar/player-flavoured.

### Open questions for the owner (decide before building)
1. Is this a **4th round** (kitchen) or a **replacement/variant** of the house round?
2. Does barf just lock Cheddar out, or also drop his score / leave a hazard puddle?
3. Is the seated human a fixed obstacle or does it come/go (occupied vs empty chair)?
4. Co-op angle? (e.g., Cocoa can't reach table food, so Cheddar knocks it down for her.)

> Asymmetric abilities are a meaningful design shift — worth a short balance pass + sim once
> built, the same way AI speed / wrestle odds were tuned. Flagged so it's a deliberate choice,
> not a surprise.
