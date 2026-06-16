/**
 * scenes/pool.ts — the pool round: drifting floaters, swim/shake fall-in stakes, deck-band
 * placement. Ported from the prototype's initFloaters/drawFloater/splashIn/startShake + the
 * floater-drift and pool background. Swim/floater SPEED logic lives in systems/movement.ts
 * (it's part of moveDog); splashIn/startShake are the state mutators it calls.
 */

import type { GameState, Floater } from '../state/gameState.js';
import type { Dog } from '../state/dog.js';
import type { SceneDef } from './types.js';
import { WORLD, POOL } from '../config/balance.js';
import { popup } from '../systems/particles.js';
import { updateToys, drawToys } from '../systems/toys.js';
import { updateSpot, drawSpot, placeSpot } from '../systems/spot.js';
import { rounded } from '../core/math.js';

type G = CanvasRenderingContext2D;
const W = WORLD.w;
const H = WORLD.h;

export function initFloaters(s: GameState): void {
  const r = s.rng;
  s.floaters = [
    { x: W * 0.2, y: 330, rx: 78, ry: 46, vx: 0.22, style: 'donut', ph: r.range(0, 6) },
    { x: W * 0.43, y: 430, rx: 88, ry: 52, vx: -0.18, style: 'leaf', ph: r.range(0, 6) },
    { x: W * 0.62, y: 310, rx: 74, ry: 44, vx: 0.16, style: 'ring', ph: r.range(0, 6) },
    { x: W * 0.78, y: 510, rx: 84, ry: 48, vx: -0.24, style: 'donut2', ph: r.range(0, 6) },
    { x: W * 0.9, y: 390, rx: 68, ry: 40, vx: -0.2, style: 'ring', ph: r.range(0, 6) },
  ];
}

export function updateFloaters(s: GameState, dt: number): void {
  for (const f of s.floaters) {
    f.x += f.vx * dt * 60;
    if (f.x < 170 + f.rx * 0.3) f.vx = Math.abs(f.vx);
    if (f.x > W - 170 - f.rx * 0.3) f.vx = -Math.abs(f.vx);
  }
}

/** A dog fell into open water → start swimming. (Called from moveDog.) */
export function splashIn(s: GameState, d: Dog): void {
  d.mode = 'swimming';
  d.stunT = 0;
  d.vx *= 0.25;
  d.vy *= 0.25;
  if (s.spot && s.spot.holder === d.id) {
    s.spot.holder = null;
    s.spot.prog = 0;
  }
  const r = s.rng;
  for (let i = 0; i < 22; i++) {
    const a = r.next() * 7;
    s.particles.push({
      x: d.x + r.range(-16, 16),
      y: d.y + r.range(-4, 14),
      vx: Math.cos(a) * r.range(0.6, 3),
      vy: r.range(-3.4, -1),
      life: r.range(0.7, 1.1),
      size: r.range(2.5, 5.5),
      col: 'rgba(225,245,253,.95)',
    });
  }
  popup(s, d.x, d.y - 44, 'SPLASH!', '#cdeefc');
  s.toast = `${d.id[0]!.toUpperCase() + d.id.slice(1)} fell in! Swim to the side 🌊`;
}

/** A swimming dog reached the deck edge → shake off. (Called from moveDog.) */
export function startShake(s: GameState, d: Dog): void {
  d.mode = 'shaking';
  d.shakeT = POOL.shake;
  d.vx = 0;
  d.vy = 0;
  popup(s, d.x, d.y - 46, 'shake shake shake', '#cdeefc');
}

export function drawFloaters(g: G, s: GameState): void {
  for (const f of s.floaters) drawFloater(g, f, s.elapsedMs);
}

function drawFloater(g: G, f: Floater, T: number): void {
  const bob = Math.sin(T * 0.0035 + f.ph) * 4;
  g.save();
  g.translate(f.x, f.y + bob);
  g.save();
  g.scale(1, 0.62);
  // water shadow
  g.fillStyle = 'rgba(10,40,60,.28)';
  g.beginPath();
  g.arc(4, 10, f.rx, 0, 7);
  g.fill();
  if (f.style === 'leaf') {
    const lg = g.createRadialGradient(0, -10, 8, 0, 0, f.rx);
    lg.addColorStop(0, '#9fce6e');
    lg.addColorStop(1, '#5e9e44');
    g.fillStyle = lg;
    g.beginPath();
    g.arc(0, 0, f.rx, 0, 7);
    g.fill();
    g.strokeStyle = 'rgba(40,90,35,.5)';
    g.lineWidth = 3;
    g.beginPath();
    g.moveTo(-f.rx + 12, 0);
    g.lineTo(f.rx - 12, 0);
    g.stroke();
    for (let i = -2; i <= 2; i++) {
      g.beginPath();
      g.moveTo(i * 22, 0);
      g.lineTo(i * 22 + 14, -f.rx * 0.55);
      g.stroke();
    }
  } else {
    const cols =
      f.style === 'donut'
        ? ['#f2a7c3', '#d96a96']
        : f.style === 'ring'
          ? ['#8fd4e8', '#4ba3c4']
          : ['#f6c46a', '#dd9034'];
    const rg = g.createRadialGradient(-10, -14, 6, 0, 0, f.rx);
    rg.addColorStop(0, cols[0]!);
    rg.addColorStop(1, cols[1]!);
    g.fillStyle = rg;
    g.beginPath();
    g.arc(0, 0, f.rx, 0, 7);
    g.fill();
    g.fillStyle = '#2d6f93';
    g.beginPath();
    g.arc(0, 4, f.rx * 0.42, 0, 7);
    g.fill();
    g.fillStyle = 'rgba(255,255,255,.22)';
    g.beginPath();
    g.arc(0, 2, f.rx * 0.46, 0, 7);
    g.arc(0, 5, f.rx * 0.4, 0, 7, true);
    g.fill();
    g.strokeStyle = 'rgba(255,255,255,.5)';
    g.lineWidth = 4;
    g.lineCap = 'round';
    for (let i = 0; i < 6; i++) {
      const a = i * 1.05 + f.ph;
      g.beginPath();
      g.moveTo(Math.cos(a) * f.rx * 0.66, Math.sin(a) * f.rx * 0.66);
      g.lineTo(Math.cos(a) * f.rx * 0.84, Math.sin(a) * f.rx * 0.84);
      g.stroke();
    }
  }
  g.restore();
  g.strokeStyle = 'rgba(255,255,255,.35)';
  g.lineWidth = 3;
  g.beginPath();
  g.ellipse(0, -f.ry * 0.18, f.rx * 0.7, f.ry * 0.32, 0, Math.PI * 1.1, Math.PI * 1.9);
  g.stroke();
  g.restore();
}

export function paintPool(g: G): void {
  // deck
  const dg = g.createLinearGradient(0, 0, 0, H);
  dg.addColorStop(0, '#d8c5a6');
  dg.addColorStop(1, '#bda380');
  g.fillStyle = dg;
  g.fillRect(0, 0, W, H);
  g.strokeStyle = 'rgba(110,85,60,.35)';
  g.lineWidth = 2;
  for (let y = 40; y < H; y += 52) {
    g.beginPath();
    g.moveTo(0, y);
    g.lineTo(W, y);
    g.stroke();
  }
  for (let x = 60; x < W; x += 120) {
    g.beginPath();
    g.moveTo(x, 0);
    g.lineTo(x, H);
    g.stroke();
  }
  // pool coping
  g.fillStyle = '#eee6d4';
  rounded(g, 104, 254, W - 208, H - 300, 34);
  g.fill();
  g.fillStyle = 'rgba(120,100,70,.25)';
  rounded(g, 104, 254, W - 208, 8, 4);
  g.fill();
  // water
  const wg = g.createLinearGradient(0, 270, 0, H - 60);
  wg.addColorStop(0, '#69bcd8');
  wg.addColorStop(0.5, '#3a8fb8');
  wg.addColorStop(1, '#236788');
  g.fillStyle = wg;
  rounded(g, 120, 270, W - 240, H - 330, 26);
  g.fill();
  // towel + lounge chair flavor (top deck)
  g.fillStyle = '#d96a6a';
  rounded(g, 150, 60, 120, 60, 8);
  g.fill();
  g.fillStyle = 'rgba(255,255,255,.5)';
  for (let i = 0; i < 4; i++) g.fillRect(150, 68 + i * 14, 120, 5);
  g.fillStyle = '#7d8c96';
  rounded(g, 640, 46, 180, 70, 12);
  g.fill();
  g.fillStyle = '#93a4af';
  rounded(g, 648, 54, 164, 26, 8);
  g.fill();
}

export const poolScene: SceneDef = {
  config: {
    key: 'pool',
    name: 'The Pool',
    sub: 'Round 2 of 3 — floaters only. Water is SLOW.',
    time: 45,
  },
  bgKey: () => 'pool',
  paint: () => paintPool,
  visibleDogs: (s) => [s.dogs.cheddar, s.dogs.cocoa],
  enter(s: GameState): void {
    for (const id of ['cheddar', 'cocoa'] as const) {
      const d = s.dogs[id];
      d.room = '';
      d.mode = 'free';
    }
    initFloaters(s);
    placeSpot(s); // pool branch samples deck bands
    s.sunbeam = null; // no sunbeam in the pool
  },
  update(s: GameState, dt: number): void {
    updateFloaters(s, dt);
    updateToys(s, dt);
    updateSpot(s, dt);
  },
  drawWorld(g: G, s: GameState): void {
    drawFloaters(g, s);
    drawSpot(g, s);
    drawToys(g, s);
  },
};
