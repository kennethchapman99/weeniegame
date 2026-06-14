/**
 * systems/couch.ts — the dog couch: the house's premium hold spot (+5, then a cooldown).
 * Ported from the prototype's couch block of update() + drawCouchGlow. Steal-on-contact and
 * steal-on-wrestle-win are handled like the cuddle spot (movement.ts / wrestle.ts).
 */

import type { GameState } from '../state/gameState.js';
import { addScore, cap } from '../state/gameState.js';
import { busy } from '../state/dog.js';
import { HOUSE } from '../config/balance.js';
import { heartBurst, popup } from './particles.js';
import { visibleRoom } from './house.js';

type G = CanvasRenderingContext2D;

export function updateCouch(s: GameState, dt: number): void {
  const couch = s.couch;
  if (!couch) return;
  if (couch.cool > 0) {
    couch.cool -= dt;
    couch.holder = null;
    couch.prog = 0;
    return;
  }
  const a = s.dogs.cheddar;
  const b = s.dogs.cocoa;
  const eligible = (d: typeof a): boolean =>
    !busy(d) && d.room === couch.room && Math.hypot(couch.x - d.x, couch.y - d.y) < couch.r;
  const inA = eligible(a);
  const inB = eligible(b);
  if (inA && !inB) {
    if (couch.holder !== a.id) {
      couch.holder = a.id;
      couch.prog = 0;
    }
  } else if (inB && !inA) {
    if (couch.holder !== b.id) {
      couch.holder = b.id;
      couch.prog = 0;
    }
  } else if (!inA && !inB) {
    couch.holder = null;
    couch.prog = 0;
  }
  if (couch.holder && ((couch.holder === a.id && inA) || (couch.holder === b.id && inB))) {
    couch.prog += dt;
    if (couch.prog >= HOUSE.couch.hold) {
      const d = couch.holder === a.id ? a : b;
      addScore(s, d, HOUSE.couch.reward);
      heartBurst(s, couch.x, couch.y - 26);
      popup(s, couch.x, couch.y - 70, '+5 THE DOG COUCH', '#ffd98c');
      s.toast = `${cap(d.id)} claims the dog couch 👑`;
      couch.holder = null;
      couch.prog = 0;
      couch.cool = HOUSE.couch.cooldown;
    }
  }
}

export function drawCouchGlow(g: G, s: GameState): void {
  const couch = s.couch;
  if (!couch || visibleRoom(s) !== couch.room) return;
  g.save();
  g.translate(couch.x, couch.y);
  if (couch.cool > 0) {
    g.fillStyle = 'rgba(40,28,18,.45)';
    g.font = '800 12px -apple-system, sans-serif';
    g.textAlign = 'center';
    g.fillText('recharging… ' + Math.ceil(couch.cool) + 's', 0, -couch.r - 14);
  } else {
    const glow = 0.55 + Math.sin(s.elapsedMs * 0.005) * 0.25;
    const ag = g.createRadialGradient(0, 0, 12, 0, 0, couch.r + 30);
    ag.addColorStop(0, `rgba(255,214,140,${0.34 * glow})`);
    ag.addColorStop(1, 'rgba(255,214,140,0)');
    g.fillStyle = ag;
    g.beginPath();
    g.arc(0, 0, couch.r + 30, 0, 7);
    g.fill();
    if (couch.holder && couch.prog > 0) {
      const pct = couch.prog / HOUSE.couch.hold;
      g.strokeStyle = couch.holder === s.playerId ? '#f4d3a4' : '#caa27e';
      g.lineWidth = 6;
      g.lineCap = 'round';
      g.beginPath();
      g.arc(0, 0, couch.r + 10, -Math.PI / 2, -Math.PI / 2 + pct * Math.PI * 2);
      g.stroke();
      g.strokeStyle = 'rgba(0,0,0,.25)';
      g.beginPath();
      g.arc(0, 0, couch.r + 10, -Math.PI / 2 + pct * Math.PI * 2, Math.PI * 1.5);
      g.stroke();
    }
  }
  g.restore();
}
