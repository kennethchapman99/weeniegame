/**
 * ai/sibling.ts — the AI dog brain. Reads state, returns a steering intent vector; the
 * movement system applies it (AI logic stays out of render — CLAUDE.md / BUILD-PLAN M3).
 *
 * M3 implements the YARD brain: pick the nearest toy, else contest the cuddle spot, else
 * wander, with a sine wobble so paths read as doggy rather than laser-straight. The pool
 * corner-waypoint routing (M5), house cross-room navigation (M7) and predator/event responses
 * (M8) extend this in their milestones.
 *
 * Speed: the AI runs at full player speed. The prototype's `aiFactor` (0.88) is inert because
 * moveDog normalises the intent vector — see config/balance.ts. Owner decision: match prototype.
 *
 * KNOWN LIMITATION (faithful to the prototype's logic): the `spot.holder===d.id` clause makes a
 * spot-holder leave for the nearest toy regardless of distance. If that toy (and the sunbeam) are
 * far while the spot is the nearest attractor, the AI oscillates at the spot's edge — holding
 * briefly, bolting, returning — and scores nothing. In the prototype this is masked by the yard's
 * ambient events (squirrel/treat, M8), which inject near attractors that keep the AI moving and
 * scoring; an active player also disturbs the equilibrium. It surfaces only in an artificial
 * idle-player game and is expected to fade as M8 lands. NOT fixed here to avoid diverging from the
 * spec's AI; revisit with the owner if it persists after M8.
 */

import type { GameState } from '../state/gameState.js';
import { busy } from '../state/dog.js';
import type { Dog } from '../state/dog.js';
import { BOUNDS, JUMP } from '../config/balance.js';
import {
  nearestDeck,
  sideOf,
  CORNERS,
  SIDE_CORNERS,
  RING,
  type Corner,
} from '../scenes/poolGeometry.js';

const YARD_WOBBLE = 0.35;
const POOL_WOBBLE = 0.1;

export function aiThink(s: GameState, d: Dog, dt: number): [number, number] {
  // swimming: make a beeline for the nearest deck exit (never tread water)
  if (d.mode === 'swimming') {
    const e = nearestDeck(d);
    return [e.x - d.x, e.y - d.y];
  }
  if (d.mode !== 'free') return [0, 0]; // stunned / shaking / transit / tug

  // PREDATOR RESPONSE (yard) — cooperate to survive: rescue a grabbed sibling, dodge + run to
  // the sibling for a united front when targeted, or buddy up early.
  const pred = s.predator;
  if (pred && s.sceneKey === 'yard') {
    const other = d.id === 'cheddar' ? s.dogs.cocoa : s.dogs.cheddar;
    if (s.carriedDog && s.carriedDog !== d.id) {
      return [pred.x - d.x, pred.y + 20 - d.y]; // rush to rescue
    }
    if (pred.targetId === d.id && (pred.state === 'charge' || pred.state === 'dive')) {
      const close = Math.hypot(pred.x - d.x, pred.y - d.y);
      if (close < 70 && d.jumpT <= 0 && d.zoom <= 0 && s.rng.next() < 0.5) {
        d.jumpT = JUMP.duration; // panic hop to dodge
      }
      if (!busy(other)) return [other.x - d.x, other.y - d.y]; // run to the sibling
      const ang = Math.atan2(pred.y - d.y, pred.x - d.x) + Math.PI / 2;
      return [Math.cos(ang) * 100, Math.sin(ang) * 100]; // juke perpendicular
    }
    if (
      (pred.state === 'enter' || pred.state === 'circle') &&
      !busy(other) &&
      Math.hypot(d.x - other.x, d.y - other.y) > 70
    ) {
      return [other.x - d.x, other.y - d.y]; // buddy up before it commits
    }
  }

  const pool = s.sceneKey === 'pool';
  let tx = d.aiTx;
  let ty = d.aiTy;

  // nearest toy. In the pool: skip floater toys while wet (dryT), and only chase a floater
  // toy if the hop is short — the "short-hop-only floater judgment".
  let best: { x: number; y: number } | null = null;
  let bd = 1e9;
  let bestFloat = false;
  for (const o of s.toys) {
    if (pool && o.fl >= 0) {
      if (d.dryT > 0) continue; // just shook off — deck toys only for now
      const f = s.floaters[o.fl];
      if (!f) continue;
      const reach = Math.hypot(f.x - d.x, f.y - d.y) - Math.min(f.rx, f.ry * 0.9);
      if (reach > (d.onFloater ? 40 : 50)) continue; // tight hops only
    }
    const dd = Math.hypot(o.x - d.x, o.y - d.y);
    if (dd < bd) {
      bd = dd;
      best = o;
      bestFloat = pool && o.fl >= 0;
    }
  }

  const sd = s.spot ? Math.hypot(s.spot.x - d.x, s.spot.y - d.y) : 1e9;
  // sunbeam: a weighted, relocating attractor (yard/house only) that keeps the AI roaming
  // instead of pinning on the cuddle spot. Weighting 1.25 matches the prototype.
  const ud = s.sunbeam && !pool ? Math.hypot(s.sunbeam.x - d.x, s.sunbeam.y - d.y) * 1.25 : 1e9;

  if (best && (bd < Math.min(sd, ud) * 0.9 || (s.spot && s.spot.holder === d.id))) {
    tx = best.x;
    ty = best.y;
  } else if (s.spot && sd <= ud) {
    tx = s.spot.x;
    ty = s.spot.y;
    bestFloat = false;
  } else if (s.sunbeam && ud < 1e8) {
    tx = s.sunbeam.x;
    ty = s.sunbeam.y;
    bestFloat = false;
  } else {
    d.aiWanderT -= dt;
    if (d.aiWanderT <= 0) {
      d.aiWanderT = s.rng.range(1, 2.4);
      d.aiTx = s.rng.range(BOUNDS.minX, BOUNDS.maxX);
      d.aiTy = s.rng.range(BOUNDS.minY, BOUNDS.maxY);
    }
    tx = d.aiTx;
    ty = d.aiTy;
    bestFloat = false;
  }

  // Pool: route around the deck ring via corner waypoints rather than swimming across.
  if (pool && !bestFloat) {
    const route = poolRoute(d.x, d.y, tx, ty);
    if (route) {
      tx = route.x;
      ty = route.y;
    }
  }

  const ax = tx - d.x;
  const ay = ty - d.y;
  const wob = Math.sin(s.elapsedMs * 0.003 + d.seed * 3) * (pool ? POOL_WOBBLE : YARD_WOBBLE);
  const c = Math.cos(wob);
  const sn = Math.sin(wob);
  return [ax * c - ay * sn, ax * sn + ay * c];
}

/** Next deck waypoint toward (tx,ty), or null if a direct line stays on the same deck side. */
function poolRoute(
  x: number,
  y: number,
  tx: number,
  ty: number,
): { x: number; y: number } | null {
  const mySide = sideOf(x, y);
  const targetSide = sideOf(tx, ty);
  if (mySide === 'water' || targetSide === 'water' || mySide === targetSide) return null;

  let bestLen = 1e9;
  let bestPath: Corner[] | null = null;
  for (const c1 of SIDE_CORNERS[mySide]) {
    for (const c2 of SIDE_CORNERS[targetSide]) {
      const hops: Corner[] = [c1, ...RING[`${c1}-${c2}`]!];
      let len = Math.hypot(CORNERS[c1].x - x, CORNERS[c1].y - y);
      for (let i = 1; i < hops.length; i++) {
        len += Math.hypot(
          CORNERS[hops[i]!].x - CORNERS[hops[i - 1]!].x,
          CORNERS[hops[i]!].y - CORNERS[hops[i - 1]!].y,
        );
      }
      const last = hops[hops.length - 1]!;
      len += Math.hypot(tx - CORNERS[last].x, ty - CORNERS[last].y);
      if (len < bestLen) {
        bestLen = len;
        bestPath = hops;
      }
    }
  }
  if (!bestPath) return null;
  // head to the first corner we haven't effectively reached yet
  for (const ck of bestPath) {
    if (Math.hypot(CORNERS[ck].x - x, CORNERS[ck].y - y) > 30) return CORNERS[ck];
  }
  return null; // past all corners — direct leg to target along the shared side
}
