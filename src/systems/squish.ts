/**
 * systems/squish.ts — squishmallow naps: settle still on one for ~1.2s → +1, then it respawns.
 * Ported from the prototype's squishmallow update + drawSquish. House-only.
 */

import type { GameState, Squishmallow } from '../state/gameState.js';
import { addScore } from '../state/gameState.js';
import { HOUSE } from '../config/balance.js';
import { HOUSE_MAP } from '../scenes/house/rooms.js';
import { burst, popup } from './particles.js';

type G = CanvasRenderingContext2D;

export function updateSquish(s: GameState, dt: number): void {
  const dogs = [s.dogs.cheddar, s.dogs.cocoa];
  for (const sq of s.squishies) {
    if (!sq.active) {
      sq.respawn -= dt;
      if (sq.respawn <= 0) {
        const slots = HOUSE_MAP.rooms[sq.room]!.squishSlots;
        const p = slots[(s.rng.next() * slots.length) | 0]!;
        sq.x = p.x;
        sq.y = p.y;
        sq.active = true;
        sq.prog = 0;
        sq.who = null;
        sq.hue = s.rng.range(0, 360);
      }
      continue;
    }
    sq.who = null;
    for (const d of dogs) {
      if (d.room !== sq.room || d.transit || d.mode === 'stunned') continue;
      const near = Math.hypot(sq.x - d.x, sq.y - d.y) < 32;
      const still = Math.hypot(d.vx, d.vy) < 0.5;
      if (near && still) {
        sq.who = d.id;
        sq.prog += dt;
        if (s.rng.next() < dt * 3) {
          s.popups.push({ x: sq.x + s.rng.range(-6, 6), y: sq.y - 34, text: 'z', col: '#fff', life: 0.8 });
        }
        if (sq.prog >= HOUSE.squish.napTime) {
          addScore(s, d, 1);
          burst(s, sq.x, sq.y, `hsl(${sq.hue},70%,80%)`, 10, 2);
          popup(s, sq.x, sq.y - 30, '+1 squish nap', '#fff');
          sq.active = false;
          sq.respawn = HOUSE.squish.respawn;
          sq.prog = 0;
        }
        break;
      }
    }
    if (!sq.who) sq.prog = Math.max(0, sq.prog - dt * 2);
  }
}

export function drawSquish(g: G, sq: Squishmallow, T: number): void {
  if (!sq.active) return;
  g.save();
  g.translate(sq.x, sq.y);
  const squash = 1 + Math.sin(T * 0.004 + sq.seed) * 0.04 - (sq.prog > 0 ? 0.18 * Math.min(1, sq.prog / HOUSE.squish.napTime) : 0);
  g.fillStyle = 'rgba(20,14,8,.18)';
  g.beginPath();
  g.ellipse(0, 14, 26, 8, 0, 0, 7);
  g.fill();
  g.scale(1.06, squash);
  const body = g.createRadialGradient(-6, -8, 4, 0, 0, 26);
  body.addColorStop(0, `hsl(${sq.hue},75%,88%)`);
  body.addColorStop(1, `hsl(${sq.hue},55%,72%)`);
  g.fillStyle = body;
  g.beginPath();
  g.ellipse(0, 0, 25, 21, 0, 0, 7);
  g.fill();
  g.beginPath();
  g.ellipse(-13, -16, 7, 8, -0.4, 0, 7);
  g.fill();
  g.beginPath();
  g.ellipse(13, -16, 7, 8, 0.4, 0, 7);
  g.fill();
  g.fillStyle = '#3a2c2c';
  g.beginPath();
  g.arc(-7, -3, 2, 0, 7);
  g.fill();
  g.beginPath();
  g.arc(7, -3, 2, 0, 7);
  g.fill();
  g.strokeStyle = '#3a2c2c';
  g.lineWidth = 1.6;
  g.lineCap = 'round';
  g.beginPath();
  g.arc(0, 2, 4, 0.3, Math.PI - 0.3);
  g.stroke();
  g.fillStyle = `hsla(${sq.hue},80%,98%,.9)`;
  g.beginPath();
  g.ellipse(0, 8, 10, 6, 0, 0, 7);
  g.fill();
  g.restore();
  if (sq.prog > 0) {
    g.strokeStyle = '#fff';
    g.lineWidth = 3.5;
    g.lineCap = 'round';
    g.beginPath();
    g.arc(sq.x, sq.y, 32, -Math.PI / 2, -Math.PI / 2 + (sq.prog / HOUSE.squish.napTime) * Math.PI * 2);
    g.stroke();
  }
}
