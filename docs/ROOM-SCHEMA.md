# Room / Map-Data Schema

The seam that lets a new room — or your real house from a floor-plan — drop in as **data**, with
its **art** swappable independently. Map data is gameplay truth (geometry, doors, stairs, zones,
AI routing); painters are pure drawing. Neither knows about the other. Types live in
[`src/scenes/mapdef.ts`](../src/scenes/mapdef.ts).

## The two layers

| Layer | What | Where | Swap effect |
|---|---|---|---|
| **Map data** | `RoomDef` / `HouseMap`: obstacles, doors/stairs, couch/squish/sunbeam zones, nav graph | `scenes/house/rooms.ts` | change geometry without touching art |
| **Art** | `RoomPainter = (g, rng) => void`, one per room, registered by room id | `scenes/house/painters.ts` | re-skin a room without touching gameplay |

Systems (`pushOut`, transit, couch, squish, sunbeam, AI nav) read **only** the map data. The
renderer picks the painter for the visible room. So you can re-trace a room's collision from a
new floor-plan, or repaint it from a new photo, in isolation.

## Coordinates

World units, **960 × 600**, origin top-left. Play bounds `x∈[50,910]`, `y∈[215,555]`. All
positions/sizes are world units (the camera handles DPR/letterboxing).

## RoomDef

```ts
interface RoomDef {
  id: string;            // unique, used by doors `.to`, nav, entity room tags
  label: string;         // shown in the sibling-locator HUD
  obstacles: Obstacle[]; // furniture / walls (collision)
  doors: DoorDef[];      // transitions out of this room
  squishSlots: Vec[];    // candidate squishmallow spots (2 active/room, rest respawn)
  couch?: CouchDef;      // the premium dog-couch hold spot (one room only)
  sunbeamZone?: Rect;    // where the sunbeam may land here
}

type Obstacle =
  | { kind: 'rect'; x; y; w; h }     // fixed inflate radius in pushOut
  | { kind: 'circle'; x; y; r };     // carries its own radius

interface DoorDef {
  rect: Rect;     // walk into this to start a transit
  to: string;     // target room id
  spawn: Vec;     // where you arrive in the target room
  cx; cy: number; // glow/label anchor
  stair?: boolean;// stair traversal uses per-dog stair time (Cheddar faster); else doorTime
  label: string;  // e.g. "hallway", "stairs up"
}
```

## HouseMap (multi-room)

```ts
interface HouseMap {
  rooms: Record<string, RoomDef>;
  nextHop: Record<string, Record<string, string>>; // nextHop[from][goal] = adjacent room to step toward
  start: { room: string; cheddar: Vec; cocoa: Vec };
}
```

`nextHop` is the AI's routing table: from any room, which adjacent room to head into to reach a
goal room. For a 3-room hub layout (foyer central) every non-foyer goal routes via the foyer.

## Authoring a new room from a floor-plan — the workflow

1. You hand over a photo or hand-drawn plan of the room (with a rough scale).
2. I trace it into a `RoomDef`: outline → `obstacles` (walls/furniture), exits → `doors`
   (+ `spawn`/`stair`), and mark couch / squish / sunbeam zones.
3. I add a `RoomPainter` (or inline a bundled background image — **never fetched at runtime**;
   images are build-inlined per the zero-network rule) and register it by the room's `id`.
4. It drops into `scenes/house/rooms.ts` + the painter registry. No system code changes.

## Current house (inferred from the Burnet St site plan)

A real interior plan will replace this; the geometry is a placeholder reasoned from the footprint
(2-storey core + solarium + porch). The graph: **Foyer** (entry + stairs) ⇄ **Family** (the cozy
core, holds the dog couch) via hallway; **Foyer** ⇄ **Solarium** (the bright 1½-storey addition,
where the sunbeam lands) via stairs. Cheddar is faster on the stairs.
