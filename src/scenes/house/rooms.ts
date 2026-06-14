/**
 * scenes/house/rooms.ts — the house MAP DATA (pure geometry; no drawing).
 *
 * Inferred from the Burnet St site plan (2-storey core + 1½-storey solarium + porch); a real
 * interior plan will replace these numbers without touching any system. Layout: Foyer is the hub
 * — hallway to the Family room (the cozy core; holds the dog couch), stairs up to the Solarium
 * (the bright addition; where the sunbeam lands). Cheddar is faster on the stairs.
 *
 * Geometry ported from the prototype's HOUSE object (rec → solarium rename).
 */

import type { HouseMap } from '../mapdef.js';

export const HOUSE_MAP: HouseMap = {
  rooms: {
    foyer: {
      id: 'foyer',
      label: 'Foyer',
      obstacles: [
        { kind: 'rect', x: 780, y: 228, w: 140, h: 128 }, // stone fireplace
        { kind: 'rect', x: 380, y: 222, w: 200, h: 46 }, // front-door steps
      ],
      doors: [
        { rect: { x: 38, y: 255, w: 120, h: 130 }, to: 'solarium', spawn: { x: 170, y: 330 }, cx: 98, cy: 320, stair: true, label: 'stairs up' },
        { rect: { x: 902, y: 380, w: 58, h: 140 }, to: 'family', spawn: { x: 110, y: 450 }, cx: 931, cy: 450, label: 'hallway' },
      ],
      squishSlots: [
        { x: 640, y: 480 },
        { x: 300, y: 520 },
        { x: 840, y: 430 },
        { x: 480, y: 300 },
      ],
    },
    family: {
      id: 'family',
      label: 'Family Room',
      obstacles: [
        { kind: 'rect', x: 330, y: 236, w: 330, h: 74 }, // big beige couch
        { kind: 'rect', x: 415, y: 368, w: 165, h: 78 }, // coffee table
        { kind: 'rect', x: 760, y: 402, w: 140, h: 96 }, // leather chair
      ],
      doors: [
        { rect: { x: 0, y: 380, w: 58, h: 140 }, to: 'foyer', spawn: { x: 850, y: 450 }, cx: 29, cy: 450, label: 'hallway' },
      ],
      squishSlots: [
        { x: 730, y: 300 },
        { x: 250, y: 520 },
        { x: 560, y: 520 },
        { x: 870, y: 560 },
      ],
      couch: { x: 185, y: 282, r: 62 }, // ★ THE DOG COUCH
    },
    solarium: {
      id: 'solarium',
      label: 'Solarium',
      obstacles: [
        { kind: 'rect', x: 330, y: 244, w: 300, h: 78 }, // sectional
        { kind: 'rect', x: 560, y: 244, w: 84, h: 170 }, // chaise
        { kind: 'rect', x: 385, y: 392, w: 150, h: 68 }, // dark coffee table
        { kind: 'circle', x: 800, y: 452, r: 56 }, // big blue ottoman
      ],
      doors: [
        { rect: { x: 38, y: 255, w: 120, h: 130 }, to: 'foyer', spawn: { x: 170, y: 330 }, cx: 98, cy: 320, stair: true, label: 'stairs down' },
      ],
      squishSlots: [
        { x: 700, y: 560 },
        { x: 250, y: 480 },
        { x: 880, y: 320 },
        { x: 480, y: 540 },
      ],
      // the bright addition — the sunbeam likes it here
      sunbeamZone: { x: 150, y: 300, w: 670, h: 230 },
    },
  },
  nextHop: {
    foyer: { family: 'family', solarium: 'solarium' },
    family: { foyer: 'foyer', solarium: 'foyer' },
    solarium: { foyer: 'foyer', family: 'foyer' },
  },
  start: { room: 'foyer', cheddar: { x: 480, y: 420 }, cocoa: { x: 620, y: 480 } },
};

export const HOUSE_ROOM_IDS = Object.keys(HOUSE_MAP.rooms);
