/**
 * systems/movement.ts — steering, arrival easing, facing, bounds.
 *
 * Ported from the prototype's `moveDog`. M1 implements the FREE-land path only; the
 * pool (swim/float), stun, shake, transit and tug branches are owned by later milestones
 * and are gated on `d.mode`. Critical detail preserved (BUILD-PLAN M1 watch-out):
 * position integrates as `pos += v * dt * 60` — NOT divided down. Movement was ~17x too
 * slow in the prototype's first cut; this is the corrected math.
 */

import type { Dog } from '../state/dog.js';
import { BOUNDS, SPEED } from '../config/balance.js';
import { clamp } from './../core/math.js';

/**
 * Apply a steering intent to a dog for one fixed step.
 * @param ax,ay  desired direction (un-normalised; magnitude ignored)
 * @param arrive 0..1 arrival-easing factor (1 = full speed, 0 = stop)
 */
export function moveDog(d: Dog, ax: number, ay: number, dt: number, arrive = 1): void {
  // Later milestones intercept non-free modes here (swim/stun/shake/transit/tug).
  if (d.mode !== 'free') return;

  let sp = SPEED.free;
  d.onFloater = false;

  // jump arc is cosmetic; movement continues underneath
  if (d.jumpT > 0) d.jumpT -= dt;

  if (d.zoom > 0) {
    d.zoom -= dt;
    sp *= SPEED.zoomMult; // ZOOMIES turbo
    d.trail.push({ x: d.x, y: d.y, life: 1 });
    if (d.trail.length > 14) d.trail.shift();
  } else if (d.trail.length) {
    d.trail.length = 0;
  }
  if (d.immune > 0) d.immune -= dt;

  sp *= clamp(arrive, 0, 1); // ease in near the target — no overshoot jitter

  const lerp = d.zoom > 0 ? SPEED.steerLerpZoom : SPEED.steerLerp;
  const m = Math.hypot(ax, ay);
  if (m > 0.01 && sp > 0.05) {
    ax /= m;
    ay /= m;
    d.vx += (ax * sp - d.vx) * lerp;
    d.vy += (ay * sp - d.vy) * lerp;
  } else {
    d.vx *= SPEED.idleDecay;
    d.vy *= SPEED.idleDecay;
    if (Math.hypot(d.vx, d.vy) < SPEED.idleSnap) {
      d.vx = 0;
      d.vy = 0;
    }
  }

  d.x += d.vx * dt * 60;
  d.y += d.vy * dt * 60;
  if (Math.abs(d.vx) > SPEED.faceFlipThreshold) d.face = d.vx > 0 ? 1 : -1;
  d.x = clamp(d.x, BOUNDS.minX, BOUNDS.maxX);
  d.y = clamp(d.y, BOUNDS.minY, BOUNDS.maxY);

  // zoomies trail fade
  for (const tr of d.trail) tr.life -= dt * 2.2;
  d.trail = d.trail.filter((tr) => tr.life > 0);

  if (d.bumpCD > 0) d.bumpCD -= dt;
  if (d.wrestleCD > 0) d.wrestleCD -= dt;
  if (d.dryT > 0) d.dryT -= dt; // wet→dry (drip particles added in M5)
}
