/**
 * scenes/poolGeometry.ts — pure pool geometry: water rect, floater hit-test, deck exits,
 * deck-band placement sampler, and the AI's corner-waypoint routing graph. No state mutation,
 * no cross-system imports — so movement / toys / spot / pool can all depend on it without cycles.
 *
 * Ported from the prototype's inPoolRect/onAnyFloater/inWater/nearestDeck, the deck-band
 * sampler in spawnToy/placeSpot, and the corner-routing block of aiThink.
 */

import type { Floater } from '../state/gameState.js';
import type { Rng } from '../core/rng.js';
import type { Point } from '../core/math.js';
import { WORLD } from '../config/balance.js';

const W = WORLD.w;
const H = WORLD.h;

/** The open-water rectangle (BUILD-PLAN M5: placement must never sample inside this). */
export function inPoolRect(x: number, y: number): boolean {
  return x > 120 && x < W - 120 && y > 270 && y < H - 60;
}

/** Is (x,y) on top of any floater? Ellipse test (ry weighted 0.9), matching the prototype. */
export function onAnyFloater(floaters: readonly Floater[], x: number, y: number): boolean {
  for (const f of floaters) {
    const dx = (x - f.x) / f.rx;
    const dy = (y - f.y) / (f.ry * 0.9);
    if (dx * dx + dy * dy < 1) return true;
  }
  return false;
}

/** In open water (in the pool rect but not on a floater) → you're swimming. */
export function inWater(floaters: readonly Floater[], x: number, y: number): boolean {
  return inPoolRect(x, y) && !onAnyFloater(floaters, x, y);
}

/** Closest deck point just outside the water, per side — the swim exit target. */
export function nearestDeck(p: Point): Point {
  const exits: Point[] = [
    { x: 108, y: Math.max(282, Math.min(H - 72, p.y)) },
    { x: W - 108, y: Math.max(282, Math.min(H - 72, p.y)) },
    { x: Math.max(132, Math.min(W - 132, p.x)), y: 258 },
    { x: Math.max(132, Math.min(W - 132, p.x)), y: H - 48 },
  ];
  let best = exits[0]!;
  let bd = 1e9;
  for (const e of exits) {
    const dd = Math.hypot(e.x - p.x, e.y - p.y);
    if (dd < bd) {
      bd = dd;
      best = e;
    }
  }
  return best;
}

/** Sample one of the four deck bands around the water (never inside the water rect). */
export function sampleDeckBand(rng: Rng): Point {
  const band = (rng.next() * 4) | 0;
  if (band === 0) return { x: rng.range(70, W - 70), y: rng.range(232, 260) }; // top
  if (band === 1) return { x: rng.range(70, W - 70), y: rng.range(H - 48, H - 38) }; // bottom
  if (band === 2) return { x: rng.range(62, 110), y: rng.range(280, 540) }; // left
  return { x: rng.range(W - 110, W - 62), y: rng.range(280, 540) }; // right
}

// ---- AI corner-waypoint routing graph (around the deck ring) ----

export type Corner = 'tl' | 'tr' | 'bl' | 'br';
export type Side = 'top' | 'bottom' | 'left' | 'right' | 'water';

export const CORNERS: Record<Corner, Point> = {
  tl: { x: 94, y: 252 },
  tr: { x: W - 104, y: 252 },
  bl: { x: 94, y: H - 51 },
  br: { x: W - 104, y: H - 51 },
};

export const SIDE_CORNERS: Record<Exclude<Side, 'water'>, Corner[]> = {
  top: ['tl', 'tr'],
  bottom: ['bl', 'br'],
  left: ['tl', 'bl'],
  right: ['tr', 'br'],
};

/** Shortest corner-to-corner hop sequences along the deck ring (excludes the start corner). */
export const RING: Record<string, Corner[]> = {
  'tl-tr': ['tr'], 'tr-tl': ['tl'], 'tl-bl': ['bl'], 'bl-tl': ['tl'],
  'tr-br': ['br'], 'br-tr': ['tr'], 'bl-br': ['br'], 'br-bl': ['bl'],
  'tl-br': ['tr', 'br'], 'br-tl': ['bl', 'tl'], 'tr-bl': ['tl', 'bl'], 'bl-tr': ['br', 'tr'],
  'tl-tl': [], 'tr-tr': [], 'bl-bl': [], 'br-br': [],
};

export function sideOf(px: number, py: number): Side {
  if (py <= 272) return 'top';
  if (py >= H - 60) return 'bottom';
  if (px <= 122) return 'left';
  if (px >= W - 122) return 'right';
  return 'water';
}
