/**
 * systems/house.ts — house orchestration: furniture collision (pushOut), door/stair transit,
 * per-scene setup, door triggers, and room-visibility helpers. Reads only the map data
 * (scenes/house/rooms.ts) — no art. Ported from the prototype's pushOut/startTransit/
 * tickTransit/setupHouse + the doorway-trigger block.
 */

import type { GameState } from '../state/gameState.js';
import type { Dog } from '../state/dog.js';
import { inRect } from '../scenes/mapdef.js';
import { HOUSE_MAP } from '../scenes/house/rooms.js';
import { HOUSE, BOUNDS } from '../config/balance.js';
import { popup } from './particles.js';

const RECT_PUSH = 26;
const CIRCLE_PUSH = 24;

/** The room the player currently sees (their room, or where they're heading mid-transit). */
export function visibleRoom(s: GameState): string {
  const p = s.dogs[s.playerId];
  return p.transit ? p.transit.to : (p.room ?? 'foyer');
}

/** Push a dog out of any furniture in its room. */
export function pushOut(d: Dog): void {
  if (!d.room) return;
  const room = HOUSE_MAP.rooms[d.room];
  if (!room) return;
  for (const o of room.obstacles) {
    if (o.kind === 'rect') {
      const cx = Math.max(o.x, Math.min(d.x, o.x + o.w));
      const cy = Math.max(o.y, Math.min(d.y, o.y + o.h));
      let dx = d.x - cx;
      let dy = d.y - cy;
      let dd = Math.hypot(dx, dy);
      if (dd < RECT_PUSH) {
        if (dd < 0.01) {
          dx = 0;
          dy = -1;
          dd = 1;
        }
        d.x = cx + (dx / dd) * RECT_PUSH;
        d.y = cy + (dy / dd) * RECT_PUSH;
      }
    } else {
      const dx = d.x - o.x;
      const dy = d.y - o.y;
      const dd = Math.hypot(dx, dy);
      const R = o.r + CIRCLE_PUSH;
      if (dd < R && dd > 0.01) {
        d.x = o.x + (dx / dd) * R;
        d.y = o.y + (dy / dd) * R;
      }
    }
  }
}

/** (Re)initialise the house on scene entry — the single explicit reset for this round. */
export function setupHouse(s: GameState): void {
  const start = HOUSE_MAP.start;
  for (const id of ['cheddar', 'cocoa'] as const) {
    const d = s.dogs[id];
    d.room = start.room;
    d.transit = null;
    d.mode = 'free';
  }
  s.dogs.cheddar.x = start.cheddar.x;
  s.dogs.cheddar.y = start.cheddar.y;
  s.dogs.cocoa.x = start.cocoa.x;
  s.dogs.cocoa.y = start.cocoa.y;
  s.spot = null; // house uses the couch instead of the cuddle spot

  // the dog couch (whichever room declares it)
  s.couch = null;
  for (const room of Object.values(HOUSE_MAP.rooms)) {
    if (room.couch) {
      s.couch = { room: room.id, x: room.couch.x, y: room.couch.y, r: room.couch.r, holder: null, prog: 0, cool: 0 };
      break;
    }
  }

  // 2 active squishmallows per room (rest of the slots are respawn candidates)
  s.squishies = [];
  for (const room of Object.values(HOUSE_MAP.rooms)) {
    const slots = room.squishSlots.slice();
    for (let i = 0; i < HOUSE.squish.perRoom && slots.length; i++) {
      const idx = (s.rng.next() * slots.length) | 0;
      const slot = slots.splice(idx, 1)[0]!;
      s.squishies.push({
        room: room.id,
        x: slot.x,
        y: slot.y,
        active: true,
        respawn: 0,
        prog: 0,
        who: null,
        hue: s.rng.range(0, 360),
        seed: s.rng.range(0, 9),
      });
    }
  }
}

export function startTransit(s: GameState, d: Dog, door: { to: string; spawn: { x: number; y: number }; stair?: boolean; cx: number; cy: number }): void {
  d.transit = {
    t: 0,
    total: door.stair ? HOUSE.stairTime[d.id] : HOUSE.doorTime,
    to: door.to,
    spawn: door.spawn,
    stair: !!door.stair,
  };
  d.mode = 'transit';
  if (s.couch && s.couch.holder === d.id) {
    s.couch.holder = null;
    s.couch.prog = 0;
  }
  if (door.stair) {
    popup(
      s,
      door.cx + 40,
      door.cy - 30,
      d.id === 'cheddar' ? 'Cheddar ZOOMS the stairs!' : 'Cocoa takes the stairs…',
      '#fff',
    );
  }
}

/** Advance a dog's transit; on arrival, drop it into the target room. Returns true while transiting. */
export function tickTransit(d: Dog, dt: number): boolean {
  if (!d.transit) return false;
  d.transit.t += dt;
  if (d.transit.t >= d.transit.total) {
    d.room = d.transit.to;
    d.x = d.transit.spawn.x;
    d.y = d.transit.spawn.y;
    d.vx = 2;
    d.vy = 0;
    d.transit = null;
    d.mode = 'free';
  }
  return true;
}

/** A free dog standing in a doorway starts a transit. */
export function doorTriggers(s: GameState): void {
  for (const id of ['cheddar', 'cocoa'] as const) {
    const d = s.dogs[id];
    if (d.transit || d.mode !== 'free' || !d.room) continue;
    const room = HOUSE_MAP.rooms[d.room];
    if (!room) continue;
    for (const door of room.doors) {
      if (inRect(d.x, d.y, door.rect)) {
        startTransit(s, d, door);
        break;
      }
    }
  }
}

/** House toy placement: random room, clear of furniture / couch / doors. */
export function sampleHouseToy(s: GameState): { x: number; y: number; room: string } {
  const ids = Object.keys(HOUSE_MAP.rooms);
  let x = 480;
  let y = 400;
  let room = ids[0]!;
  let ok = false;
  let tries = 0;
  while (!ok && tries++ < 60) {
    room = ids[(s.rng.next() * ids.length) | 0]!;
    x = s.rng.range(BOUNDS.minX + 40, BOUNDS.maxX - 40);
    y = s.rng.range(BOUNDS.minY + 30, BOUNDS.maxY - 15);
    const def = HOUSE_MAP.rooms[room]!;
    ok = !def.obstacles.some((o) =>
      o.kind === 'rect'
        ? inRect(x, y, { x: o.x - 22, y: o.y - 22, w: o.w + 44, h: o.h + 44 })
        : Math.hypot(x - o.x, y - o.y) < o.r + 30,
    );
    if (ok && s.couch && room === s.couch.room && Math.hypot(x - s.couch.x, y - s.couch.y) < s.couch.r + 40) ok = false;
    if (ok && def.doors.some((dr) => inRect(x, y, dr.rect))) ok = false;
  }
  return { x, y, room };
}
