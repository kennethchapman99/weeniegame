/**
 * systems/tug.ts — the two-dog rope tug-of-war. Ported from the prototype's
 * startTug/tugPull/endTug/updateTug. Both dogs lock into MovementMode 'tug'; mash WRESTLE to
 * pull (AI mashes automatically); first to ±TUG.winAt wins +3 and tumbles the loser; a
 * TUG.stalemate timeout sends the rope flying with no score. Growl bed + strain popups.
 *
 * (BUILD-PLAN M6 watch-out: tug locks both dogs — busy() already includes 'tug', and
 * beginScene clears s.tug.)
 */

import type { GameState, Toy } from '../state/gameState.js';
import { addScore, cap, playSound } from '../state/gameState.js';
import type { Dog } from '../state/dog.js';
import { TUG, SCORE } from '../config/balance.js';
import { burst, popup } from './particles.js';
import { clamp } from '../core/math.js';

const PLAYER_PULL = 1.7; // player's per-tap mash strength (prototype)

/** Lock both dogs onto a rope toy and begin the tug. */
export function startTug(s: GameState, o: Toy): void {
  const a = s.dogs.cheddar;
  const b = s.dogs.cocoa;
  a.mode = 'tug';
  b.mode = 'tug';
  const left = a.x < b.x ? a : b;
  const right = left === a ? b : a;
  o.x = (a.x + b.x) / 2;
  o.y = (a.y + b.y) / 2;
  left.face = 1;
  right.face = -1;
  s.tug = { toy: o, rope: 0, mashA: 0, mashB: 0, growlT: 0, dur: 0 };
  popup(s, o.x, o.y - 58, 'TUG OF WAR!', '#ffd98c');
  s.toast = 'Mash WRESTLE to pull! 🪢';
  playSound(s, 'growl');
}

/** A yank from one dog (player taps WRESTLE; power defaults to the player mash). */
export function tugPull(s: GameState, d: Dog, power = PLAYER_PULL): void {
  if (!s.tug) return;
  if (d.id === 'cheddar') s.tug.mashA += power;
  else s.tug.mashB += power;
  d.jumpT = 0.18; // little lunge
}

function endTug(s: GameState, winner: Dog | null): void {
  const t = s.tug;
  if (!t) return;
  const o = t.toy;
  s.dogs.cheddar.mode = 'free';
  s.dogs.cocoa.mode = 'free';
  if (winner) {
    addScore(s, winner, SCORE.ropeWin);
    playSound(s, 'yip');
    burst(s, o.x, o.y, winner.id === 'cheddar' ? '#f4d3a4' : '#a86d42', 20, 3.4);
    popup(s, o.x, o.y - 50, `${cap(winner.id)} wins tug! +3`, '#ffd98c');
    const loser = winner.id === 'cheddar' ? s.dogs.cocoa : s.dogs.cheddar;
    const kx = loser.x - winner.x || 1;
    const km = Math.abs(kx) || 1;
    loser.vx = (kx / km) * 6;
    loser.mode = 'stunned';
    loser.stunT = 0.5;
  }
  const i = s.toys.indexOf(o);
  if (i >= 0) s.toys.splice(i, 1);
  s.tug = null;
}

export function updateTug(s: GameState, dt: number): void {
  const t = s.tug;
  if (!t) return;
  const a = s.dogs.cheddar;
  const b = s.dogs.cocoa;
  t.dur += dt;
  t.growlT -= dt;

  // AI mashes automatically (Cocoa the veteran pulls a touch harder)
  const aiDog = s.dogs[s.aiId];
  const aiStr = aiDog.id === 'cocoa' ? TUG.aiMash.cocoa : TUG.aiMash.cheddar;
  const yank = aiStr * dt * 8 * s.rng.range(0.6, 1.2);
  if (aiDog.id === 'cheddar') t.mashA += yank;
  else t.mashB += yank;

  // resolve toward whoever mashed more recently; decay both
  const net = t.mashA - t.mashB;
  t.rope += net * 0.012;
  t.mashA *= Math.pow(0.12, dt);
  t.mashB *= Math.pow(0.12, dt);
  t.rope = clamp(t.rope, -1, 1);

  // toy strains toward the leader
  const cx = (a.x + b.x) / 2;
  const cy = (a.y + b.y) / 2;
  t.toy.x = cx + t.rope * (a.x < b.x ? -14 : 14);
  t.toy.y = cy + Math.sin(s.elapsedMs * 0.02) * 2;

  // continuous growl bed + strain mutters
  if (t.growlT <= 0) {
    playSound(s, 'growl');
    t.growlT = s.rng.range(0.5, 0.85);
  }
  if (s.rng.next() < dt * 8) {
    popup(
      s,
      cx + s.rng.range(-20, 20),
      cy - 34,
      ['grrrr', 'rrrf', 'mnrr'][(s.rng.next() * 3) | 0]!,
      'rgba(255,255,255,.8)',
    );
  }

  if (t.rope >= TUG.winAt) {
    endTug(s, a);
    return;
  }
  if (t.rope <= -TUG.winAt) {
    endTug(s, b);
    return;
  }
  if (t.dur > TUG.stalemate) {
    // stalemate — rope flies away, nobody scores
    popup(s, t.toy.x, t.toy.y - 50, 'rope flies away!', '#fff');
    a.mode = 'free';
    b.mode = 'free';
    const i = s.toys.indexOf(t.toy);
    if (i >= 0) s.toys.splice(i, 1);
    s.tug = null;
  }
}
