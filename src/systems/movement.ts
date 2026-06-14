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
import type { GameState } from '../state/gameState.js';
import { cap } from '../state/gameState.js';
import { BOUNDS, SPEED, SPOT, WRESTLE, WORLD, POOL } from '../config/balance.js';
import { clamp } from './../core/math.js';
import { burst, popup } from './particles.js';
import { inPoolRect, inWater, onAnyFloater } from '../scenes/poolGeometry.js';
import { splashIn, startShake } from '../scenes/pool.js';

/**
 * Apply a steering intent to a dog for one fixed step.
 * @param ax,ay  desired direction (un-normalised; magnitude ignored)
 * @param arrive 0..1 arrival-easing factor (1 = full speed, 0 = stop)
 */
export function moveDog(s: GameState, d: Dog, ax: number, ay: number, dt: number, arrive = 1): void {
  const pool = s.sceneKey === 'pool';

  // Shaking (post-swim): rooted, vigorous shake → then dry off and step clear of the edge.
  if (d.mode === 'shaking') {
    d.shakeT -= dt;
    if (d.shakeT <= 0) {
      d.mode = 'free';
      d.shakeT = 0;
      d.dryT = POOL.wetTimer; // freshly dried — AI stays off the floaties for a beat
      const cx = WORLD.w / 2;
      const cy = 405;
      const dx = d.x - cx;
      const dy = d.y - cy;
      const mm = Math.hypot(dx, dy) || 1;
      d.x += (dx / mm) * 30;
      d.y += (dy / mm) * 30;
      d.x = clamp(d.x, BOUNDS.minX, BOUNDS.maxX);
      d.y = clamp(d.y, BOUNDS.minY, BOUNDS.maxY);
    }
    if (s.rng.next() < 0.7) {
      s.particles.push({
        x: d.x + s.rng.range(-26, 26),
        y: d.y + s.rng.range(-26, 6),
        vx: s.rng.range(-2.4, 2.4),
        vy: s.rng.range(-2.6, -0.4),
        life: s.rng.range(0.4, 0.8),
        size: s.rng.range(2, 4),
        col: 'rgba(225,245,253,.95)',
      });
    }
    if (d.bumpCD > 0) d.bumpCD -= dt;
    if (d.wrestleCD > 0) d.wrestleCD -= dt;
    return;
  }

  // Stunned (flipped): no steering — just skid, drift, and count down. (M4)
  if (d.mode === 'stunned') {
    d.stunT -= dt;
    d.vx *= WRESTLE.stunSkidDecay;
    d.vy *= WRESTLE.stunSkidDecay;
    d.x += d.vx * dt * 60;
    d.y += d.vy * dt * 60;
    d.x = clamp(d.x, BOUNDS.minX, BOUNDS.maxX);
    d.y = clamp(d.y, BOUNDS.minY, BOUNDS.maxY);
    if (pool && inWater(s.floaters, d.x, d.y)) splashIn(s, d); // wrestled into the pool!
    if (d.bumpCD > 0) d.bumpCD -= dt;
    if (d.wrestleCD > 0) d.wrestleCD -= dt;
    if (d.stunT <= 0 && d.mode === 'stunned') {
      d.stunT = 0;
      d.mode = 'free';
    }
    return;
  }

  // Tug: both dogs anchored, yanking — tiny strain wobble only. (M6)
  if (d.mode === 'tug') {
    if (d.jumpT > 0) d.jumpT -= dt;
    d.vx *= 0.6;
    d.vy *= 0.6;
    d.x += s.rng.range(-1.1, 1.1);
    d.y += s.rng.range(-0.7, 0.7);
    if (d.bumpCD > 0) d.bumpCD -= dt;
    if (d.wrestleCD > 0) d.wrestleCD -= dt;
    return;
  }
  // transit is owned by M7 (house door/stair traversal)
  if (d.mode === 'transit') return;

  // FREE or SWIMMING → steerable.
  let sp: number = SPEED.free;
  d.onFloater = false;

  if (pool) {
    if (d.mode === 'swimming') {
      sp = SPEED.water;
      if (s.rng.next() < 0.3) {
        s.particles.push({
          x: d.x + s.rng.range(-16, 16),
          y: d.y + s.rng.range(4, 14),
          vx: s.rng.range(-0.6, 0.6),
          vy: s.rng.range(-1.2, -0.3),
          life: 0.7,
          size: s.rng.range(2, 4),
          col: 'rgba(220,240,250,.9)',
        });
      }
      if (!inPoolRect(d.x, d.y)) {
        startShake(s, d); // reached the side → now shaking, rooted this step
        return;
      }
    } else if (onAnyFloater(s.floaters, d.x, d.y)) {
      d.onFloater = true;
      sp = SPEED.floater;
    } else if (inWater(s.floaters, d.x, d.y)) {
      splashIn(s, d); // fell off a floatie
      sp = SPEED.water;
    }
  }

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
  if (d.barkT > 0) d.barkT -= dt;

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
  if (d.dryT > 0) {
    d.dryT -= dt; // drip… drip…
    if (s.rng.next() < dt * 7) {
      s.particles.push({
        x: d.x + s.rng.range(-26, 26),
        y: d.y + s.rng.range(-6, 12),
        vx: s.rng.range(-0.2, 0.2),
        vy: s.rng.range(0.4, 1),
        life: s.rng.range(0.4, 0.7),
        size: s.rng.range(1.6, 2.8),
        col: 'rgba(175,215,235,.85)',
      });
    }
  }
}

/**
 * Dog-vs-dog separation + cuddle-spot steal-on-contact.
 * Ported from the prototype's dog-collision block. The faster dog is the attacker; a hard
 * enough bump onto the spot-holder steals it, knocks the defender back, and counts a steal.
 * (Couch steals are wired in here too once the couch exists — M7.) Same-room gating is added
 * in M7; M2's single yard room is always "same room".
 */
export function collideDogs(s: GameState): void {
  const d1 = s.dogs.cheddar;
  const d2 = s.dogs.cocoa;
  const dd = Math.hypot(d1.x - d2.x, d1.y - d2.y);
  if (dd >= SPOT.dogCollideDist || dd <= 0.01) return;

  const nx = (d2.x - d1.x) / dd;
  const ny = (d2.y - d1.y) / dd;
  const ov = (SPOT.dogCollideDist - dd) / 2;
  d1.x -= nx * ov;
  d1.y -= ny * ov;
  d2.x += nx * ov;
  d2.y += ny * ov;

  const rel = Math.hypot(d1.vx - d2.vx, d1.vy - d2.vy);
  const sObj = s.spot;
  if (rel > SPOT.stealRelSpeed && sObj && sObj.holder) {
    const sp1 = Math.hypot(d1.vx, d1.vy);
    const sp2 = Math.hypot(d2.vx, d2.vy);
    const atk = sp1 > sp2 ? d1 : d2;
    const def = sp1 > sp2 ? d2 : d1;
    if (
      sObj.holder === def.id &&
      atk.bumpCD <= 0 &&
      Math.hypot(sObj.x - def.x, sObj.y - def.y) < sObj.r + 20
    ) {
      sObj.holder = atk.id;
      sObj.prog = 0;
      atk.bumpCD = SPOT.stealBumpCD;
      s.steals[atk.id]++;
      const m = Math.max(1, Math.hypot(atk.x - def.x, atk.y - def.y));
      def.vx = ((def.x - atk.x) / m) * 7;
      def.vy = ((def.y - atk.y) / m) * 7;
      burst(s, (d1.x + d2.x) / 2, (d1.y + d2.y) / 2, '#f6e0b0', 16, 3.4);
      popup(s, sObj.x, sObj.y - 70, `${cap(atk.id)} stole the spot!`, '#ffd98c');
      s.toast = `${cap(atk.id)} swiped ${cap(def.id)}'s spot 💨`;
    }
  }
}
