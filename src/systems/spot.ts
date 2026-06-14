/**
 * systems/spot.ts — the cuddle spot: place, hold-to-score, and draw.
 * Ported from the prototype's placeSpot/drawSpot + the cuddle-spot block of update().
 * (The steal-on-contact path lives in systems/movement.ts `collideDogs`, since it is driven
 * by dog-vs-dog collision.) M2 yard placement; pool deck-band sampling is added in M5.
 */

import type { GameState, Spot } from '../state/gameState.js';
import { addScore } from '../state/gameState.js';
import { busy } from '../state/dog.js';
import { BOUNDS, SPOT } from '../config/balance.js';
import { heartBurst, popup } from './particles.js';

type G = CanvasRenderingContext2D;

export function placeSpot(s: GameState): void {
  const rng = s.rng;
  let x = 0;
  let y = 0;
  let tries = 0;
  const prev = s.spot;
  do {
    x = rng.range(BOUNDS.minX + 70, BOUNDS.maxX - 70);
    y = rng.range(BOUNDS.minY + 50, BOUNDS.maxY - 25);
    tries++;
  } while (tries < 80 && prev && Math.hypot(x - prev.x, y - prev.y) < 240);
  s.spot = { x, y, r: SPOT.radius, holder: null, prog: 0, pulse: 0 };
}

export function updateSpot(s: GameState, dt: number): void {
  const spot = s.spot;
  if (!spot) return;
  const a = s.dogs.cheddar;
  const b = s.dogs.cocoa;
  spot.pulse += dt;
  const inA = !busy(a) && Math.hypot(spot.x - a.x, spot.y - a.y) < spot.r;
  const inB = !busy(b) && Math.hypot(spot.x - b.x, spot.y - b.y) < spot.r;
  if (inA && !inB) {
    if (spot.holder !== a.id) {
      spot.holder = a.id;
      spot.prog = 0;
    }
  } else if (inB && !inA) {
    if (spot.holder !== b.id) {
      spot.holder = b.id;
      spot.prog = 0;
    }
  } else if (!inA && !inB) {
    spot.holder = null;
    spot.prog = 0;
  }
  if (spot.holder && ((spot.holder === a.id && inA) || (spot.holder === b.id && inB))) {
    spot.prog += dt;
    if (spot.prog >= SPOT.hold) {
      const d = spot.holder === a.id ? a : b;
      addScore(s, d, 3);
      heartBurst(s, spot.x, spot.y - 20);
      popup(s, spot.x, spot.y - 60, '+3 coziest dog', '#ffd98c');
      placeSpot(s);
    }
  }
}

export function drawSpot(g: G, s: GameState): void {
  const spot = s.spot;
  if (!spot) return;
  drawSpotShape(g, spot, s.elapsedMs, s.playerId);
}

function drawSpotShape(g: G, spot: Spot, T: number, playerId: string): void {
  g.save();
  g.translate(spot.x, spot.y);
  const glow = 0.55 + Math.sin(T * 0.005) * 0.25;
  // aura
  const ag = g.createRadialGradient(0, 0, 10, 0, 0, spot.r + 26);
  ag.addColorStop(0, `rgba(255,214,140,${0.3 * glow})`);
  ag.addColorStop(1, 'rgba(255,214,140,0)');
  g.fillStyle = ag;
  g.beginPath();
  g.arc(0, 0, spot.r + 26, 0, 7);
  g.fill();
  // bed
  g.save();
  g.scale(1, 0.62);
  const bg = g.createRadialGradient(0, -8, 6, 0, 0, spot.r);
  bg.addColorStop(0, '#a8543f');
  bg.addColorStop(1, '#7d3a2c');
  g.fillStyle = bg;
  g.beginPath();
  g.arc(0, 0, spot.r, 0, 7);
  g.fill();
  g.fillStyle = '#e8c79a';
  g.beginPath();
  g.arc(0, 3, spot.r - 13, 0, 7);
  g.fill();
  g.fillStyle = 'rgba(125,58,44,.25)';
  g.beginPath();
  g.arc(0, 5, spot.r - 24, 0, 7);
  g.fill();
  g.restore();
  // stitching
  g.strokeStyle = 'rgba(60,30,20,.35)';
  g.lineWidth = 1.4;
  g.setLineDash([4, 5]);
  g.save();
  g.scale(1, 0.62);
  g.beginPath();
  g.arc(0, 0, spot.r - 6, 0, 7);
  g.stroke();
  g.restore();
  g.setLineDash([]);
  // progress ring
  if (spot.holder && spot.prog > 0) {
    const pct = spot.prog / SPOT.hold;
    const col = spot.holder === playerId ? '#f4d3a4' : '#caa27e';
    g.strokeStyle = col;
    g.lineWidth = 6;
    g.lineCap = 'round';
    g.beginPath();
    g.arc(0, 0, spot.r + 8, -Math.PI / 2, -Math.PI / 2 + pct * Math.PI * 2);
    g.stroke();
    g.strokeStyle = 'rgba(0,0,0,.25)';
    g.lineWidth = 6;
    g.beginPath();
    g.arc(0, 0, spot.r + 8, -Math.PI / 2 + pct * Math.PI * 2, Math.PI * 1.5);
    g.stroke();
  }
  g.restore();
}
