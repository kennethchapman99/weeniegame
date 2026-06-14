/**
 * scenes/house/index.ts — the House round (3 connected rooms). The SceneDef binds the map data
 * (rooms.ts) to the art (painters.ts) and the systems, with room-filtered rendering: only the
 * player's current room (or transit target) is painted and drawn. Reset, door/stair transit and
 * cross-room AI live in systems/house.ts + ai/sibling.ts.
 */

import type { SceneDef } from '../types.js';
import type { GameState } from '../../state/gameState.js';
import { ROOM_PAINTERS } from './painters.js';
import { setupHouse, visibleRoom } from '../../systems/house.js';
import { placeSunbeam, updateSunbeam, drawSunbeam } from '../../systems/sunbeam.js';
import { updateToys, drawToys } from '../../systems/toys.js';
import { updateCouch, drawCouchGlow } from '../../systems/couch.js';
import { updateSquish, drawSquish } from '../../systems/squish.js';
import { updateEvents, drawEvents } from '../../systems/events.js';

type G = CanvasRenderingContext2D;
const FALLBACK = ROOM_PAINTERS.foyer!;

export const houseScene: SceneDef = {
  config: {
    key: 'house',
    name: 'The House',
    sub: 'Round 3 of 3 — race the halls. Steal the dog couch. Cheddar owns the stairs.',
    time: 75,
  },
  bgKey: (s) => `house:${visibleRoom(s)}`,
  paint: (s) => ROOM_PAINTERS[visibleRoom(s)] ?? FALLBACK,
  visibleDogs: (s) => {
    const v = visibleRoom(s);
    return [s.dogs.cheddar, s.dogs.cocoa].filter((d) => !d.transit && d.room === v);
  },
  enter(s: GameState): void {
    setupHouse(s);
    placeSunbeam(s); // lands in a sun room (the solarium)
  },
  update(s: GameState, dt: number): void {
    updateToys(s, dt);
    updateCouch(s, dt);
    updateSquish(s, dt);
    updateSunbeam(s, dt);
    updateEvents(s, dt);
  },
  drawWorld(g: G, s: GameState): void {
    const v = visibleRoom(s);
    drawSunbeam(g, s); // gated to its room internally
    drawCouchGlow(g, s);
    for (const sq of s.squishies) if (sq.room === v) drawSquish(g, sq, s.elapsedMs);
    drawToys(g, s); // filters to the visible room internally
    drawEvents(g, s); // gated to the visible room internally
  },
};
