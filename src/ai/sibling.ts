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
import type { Dog } from '../state/dog.js';
import { BOUNDS } from '../config/balance.js';

const YARD_WOBBLE = 0.35;

export function aiThink(s: GameState, d: Dog, dt: number): [number, number] {
  if (d.mode !== 'free') return [0, 0];

  let tx = d.aiTx;
  let ty = d.aiTy;

  // nearest toy
  let best: { x: number; y: number } | null = null;
  let bd = 1e9;
  for (const o of s.toys) {
    const dd = Math.hypot(o.x - d.x, o.y - d.y);
    if (dd < bd) {
      bd = dd;
      best = o;
    }
  }

  const sd = s.spot ? Math.hypot(s.spot.x - d.x, s.spot.y - d.y) : 1e9;
  // sunbeam is a (weighted, relocating) attractor — keeps the AI roaming instead of pinning
  // on the cuddle spot. Weighting 1.25 matches the prototype.
  const ud = s.sunbeam ? Math.hypot(s.sunbeam.x - d.x, s.sunbeam.y - d.y) * 1.25 : 1e9;

  if (best && (bd < Math.min(sd, ud) * 0.9 || (s.spot && s.spot.holder === d.id))) {
    tx = best.x;
    ty = best.y;
  } else if (s.spot && sd <= ud) {
    tx = s.spot.x;
    ty = s.spot.y;
  } else if (s.sunbeam && ud < 1e8) {
    tx = s.sunbeam.x;
    ty = s.sunbeam.y;
  } else {
    d.aiWanderT -= dt;
    if (d.aiWanderT <= 0) {
      d.aiWanderT = s.rng.range(1, 2.4);
      d.aiTx = s.rng.range(BOUNDS.minX, BOUNDS.maxX);
      d.aiTy = s.rng.range(BOUNDS.minY, BOUNDS.maxY);
    }
    tx = d.aiTx;
    ty = d.aiTy;
  }

  const ax = tx - d.x;
  const ay = ty - d.y;
  const wob = Math.sin(s.elapsedMs * 0.003 + d.seed * 3) * YARD_WOBBLE;
  const c = Math.cos(wob);
  const sn = Math.sin(wob);
  return [ax * c - ay * sn, ax * sn + ay * c];
}
