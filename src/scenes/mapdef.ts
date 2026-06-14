/**
 * scenes/mapdef.ts — the declarative MAP-DATA schema (CLAUDE.md / owner request).
 *
 * This is the seam that separates **map data** (geometry, doors, stairs, zones, AI routing —
 * the gameplay truth) from **art** (the per-room background painters). A new room/level is
 * authored by writing a RoomDef + registering a painter; no system logic changes. This is what
 * lets a real floor-plan or photo drop in as data later. See docs/ROOM-SCHEMA.md.
 *
 * Coordinates are WORLD units (960×600). Nothing here draws or mutates game state.
 */

export interface Vec {
  x: number;
  y: number;
}

export interface Rect {
  x: number;
  y: number;
  w: number;
  h: number;
}

/** A collision shape. `rect` uses a fixed inflate radius in pushOut; `circle` carries its own. */
export type Obstacle =
  | { kind: 'rect'; x: number; y: number; w: number; h: number }
  | { kind: 'circle'; x: number; y: number; r: number };

/** A transition between rooms. `stair` traversals use the per-dog stair time (Cheddar faster). */
export interface DoorDef {
  /** trigger zone (entering it starts a transit) */
  rect: Rect;
  /** target room id */
  to: string;
  /** where the dog arrives in the target room */
  spawn: Vec;
  /** label/glow anchor */
  cx: number;
  cy: number;
  stair?: boolean;
  label: string;
}

/** The premium "dog couch" hold spot (lives in exactly one room of a house map). */
export interface CouchDef {
  x: number;
  y: number;
  r: number;
}

/** One room of a multi-room map (a single-room map like the yard uses just one). */
export interface RoomDef {
  id: string;
  label: string;
  /** furniture / wall collision */
  obstacles: Obstacle[];
  /** transitions out of this room */
  doors: DoorDef[];
  /** candidate squishmallow positions (2 active per room, rest are respawn slots) */
  squishSlots: Vec[];
  /** the dog couch, if this room has it */
  couch?: CouchDef;
  /** where the sunbeam may land in this room (defaults to a sensible inset) */
  sunbeamZone?: Rect;
}

/** A multi-room map (the house). Single-room scenes (yard/pool) don't need the graph. */
export interface HouseMap {
  rooms: Record<string, RoomDef>;
  /** AI routing: nextHop[from][goal] = the adjacent room to step toward */
  nextHop: Record<string, Record<string, string>>;
  /** entry state */
  start: { room: string; cheddar: Vec; cocoa: Vec };
}

/** In-flight door/stair traversal carried on a Dog (mode === 'transit'). */
export interface TransitState {
  t: number;
  total: number;
  to: string;
  spawn: Vec;
  stair: boolean;
}

export function inRect(px: number, py: number, r: Rect): boolean {
  return px > r.x && px < r.x + r.w && py > r.y && py < r.y + r.h;
}
