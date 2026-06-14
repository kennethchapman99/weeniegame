/**
 * systems/wrestle.ts — flip your sibling. Ported from the prototype's doWrestle + dust + the
 * AI wrestle trigger. Centralises wrestle ELIGIBILITY (busy/immune/range gates) so later
 * systems (tug lock, belly-rub immunity, pool dunk) extend one place (BUILD-PLAN M4 watch-out).
 *
 * Pool-dunk knockback (M5) and couch-steal-on-win (M7) are gated and added in their milestones.
 */

import type { GameState } from '../state/gameState.js';
import { cap } from '../state/gameState.js';
import type { Dog } from '../state/dog.js';
import { WRESTLE } from '../config/balance.js';
import { popup } from './particles.js';

/** Can `att` start a wrestle on `def` right now? Centralised gate. */
export function canWrestle(s: GameState, att: Dog, def: Dog): boolean {
  if (s.phase !== 'play') return false;
  if (att.mode !== 'free' || def.mode !== 'free') return false; // stun/swim/shake/transit/tug
  if (att.wrestleCD > 0) return false;
  return true;
}

/**
 * Attempt a wrestle from att onto def. Handles immunity, lunge-if-just-out-of-range, the
 * reversal roll, stun/knockback, and spot steal-on-win.
 */
export function doWrestle(s: GameState, att: Dog, def: Dog): void {
  if (!canWrestle(s, att, def)) return;

  // belly-rub immunity blocks the flip
  if (def.immune > 0) {
    if (att.id === s.playerId) {
      popup(s, def.x, def.y - 46, 'too cozy to flip!', '#9effa0');
      att.wrestleCD = WRESTLE.immuneBlockedCD;
    }
    return;
  }

  // just out of range: lunge toward the sibling (player only), small cooldown
  const d = Math.hypot(att.x - def.x, att.y - def.y);
  if (d > WRESTLE.range) {
    if (att.id === s.playerId) {
      const dx = def.x - att.x;
      const dy = def.y - att.y;
      const m = Math.hypot(dx, dy) || 1;
      att.vx = (dx / m) * WRESTLE.lungeSpeed;
      att.vy = (dy / m) * WRESTLE.lungeSpeed;
      att.wrestleCD = WRESTLE.whiffCooldown;
    }
    return;
  }

  att.wrestleCD = WRESTLE.cooldown;
  const winChance = WRESTLE.winChance[att.id];
  const win = s.rng.next() < winChance;
  const w = win ? att : def;
  const l = win ? def : att;

  const mx = (att.x + def.x) / 2;
  const my = (att.y + def.y) / 2;
  dust(s, mx, my);

  l.mode = 'stunned';
  l.stunT = WRESTLE.loserStun;
  const kx = l.x - w.x || 1;
  const ky = l.y - w.y || 0.5;
  const km = Math.hypot(kx, ky) || 1;
  l.vx = (kx / km) * WRESTLE.knockback;
  l.vy = (ky / km) * WRESTLE.knockback;
  w.vx *= WRESTLE.winnerDamp;
  w.vy *= WRESTLE.winnerDamp;

  // steal the cuddle spot if the loser was holding it
  if (
    s.spot &&
    s.spot.holder === l.id &&
    Math.hypot(s.spot.x - l.x, s.spot.y - l.y) < s.spot.r + WRESTLE.stealReach
  ) {
    s.spot.holder = w.id;
    s.spot.prog = 0;
    s.steals[w.id]++;
    popup(s, s.spot.x, s.spot.y - 70, `${cap(w.id)} took the spot!`, '#ffd98c');
  }

  if (win) {
    popup(s, mx, my - 58, `${cap(w.id)} flipped ${cap(l.id)}!`, '#fff');
    s.toast = `${cap(w.id)} wins the wrestle 🐾`;
  } else {
    popup(s, mx, my - 58, 'REVERSAL!', '#ff9d7a');
    s.toast = `${cap(w.id)} reversed it! 💢`;
  }
}

/** The AI starts trouble: shoves you off the spot, or just wrestles like a real sibling. */
export function maybeAiWrestle(s: GameState, dt: number): void {
  const ai = s.dogs[s.aiId];
  const p = s.dogs[s.playerId];
  if (!canWrestle(s, ai, p)) return;
  if (Math.hypot(ai.x - p.x, ai.y - p.y) >= WRESTLE.aiRange) return;
  const wantsSpot = s.spot && s.spot.holder === s.playerId;
  if (wantsSpot || s.rng.next() < dt * WRESTLE.aiRandomRate) doWrestle(s, ai, p);
}

export function dust(s: GameState, x: number, y: number): void {
  const r = s.rng;
  for (let i = 0; i < 18; i++) {
    const a = r.next() * 7;
    s.particles.push({
      x: x + r.range(-10, 10),
      y: y + r.range(-8, 8),
      vx: Math.cos(a) * r.range(0.4, 2.4),
      vy: Math.sin(a) * r.range(0.4, 2.0) - 0.8,
      life: r.range(0.7, 1.1),
      size: r.range(4, 9),
      col: 'rgba(214,196,168,.85)',
    });
  }
  for (let i = 0; i < 5; i++) {
    s.particles.push({
      x: x + r.range(-16, 16),
      y: y - r.range(10, 30),
      vx: r.range(-0.8, 0.8),
      vy: r.range(-1.8, -0.6),
      life: 1,
      size: r.range(3, 5),
      col: '#fff',
    });
  }
}

/** Whether the player's WRESTLE control should show as on-cooldown. */
export function wrestleOnCooldown(s: GameState): boolean {
  const p = s.dogs[s.playerId];
  return p.wrestleCD > 0.55 || p.mode === 'stunned';
}
