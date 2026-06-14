/**
 * scenes/yard.ts — backyard background painter (fence, maple, lawn, flowers, patio).
 * Faithful port of the prototype's `bgYard`. Pure: paints into the given context in WORLD
 * units. Cosmetic scatter (grass tufts, flowers) draws from an injected seedable rng so the
 * backdrop is deterministic and lint-clean (no bare Math.random in src).
 */

import type { Rng } from '../core/rng.js';
import { WORLD, YARD } from '../config/balance.js';
import type { SceneDef } from './types.js';
import type { GameState } from '../state/gameState.js';
import { updateToys, drawToys } from '../systems/toys.js';
import { updateSpot, drawSpot, placeSpot } from '../systems/spot.js';
import { updateSunbeam, drawSunbeam, placeSunbeam } from '../systems/sunbeam.js';
import { updatePredator, drawPredator } from '../systems/predators.js';
import { updateEvents, drawEvents } from '../systems/events.js';

const W = WORLD.w;
const H = WORLD.h;
type G = CanvasRenderingContext2D;

export function paintYard(g: G, rng: Rng): void {
  const rnd = (a: number, b: number): number => rng.range(a, b);

  // late-afternoon sky
  const sg = g.createLinearGradient(0, 0, 0, 230);
  sg.addColorStop(0, '#aecfdd');
  sg.addColorStop(1, '#e8d9b4');
  g.fillStyle = sg;
  g.fillRect(0, 0, W, 230);

  // soft clouds
  g.fillStyle = 'rgba(255,255,255,.55)';
  for (const [cx, cy, r] of [
    [170, 70, 30],
    [210, 80, 40],
    [250, 68, 26],
    [700, 55, 34],
    [745, 66, 42],
    [800, 52, 24],
  ] as const) {
    g.beginPath();
    g.arc(cx, cy, r, 0, 7);
    g.fill();
  }

  // big maple
  g.fillStyle = '#6b4a2e';
  g.fillRect(70, 90, 22, 120);
  g.beginPath();
  g.moveTo(81, 95);
  g.lineTo(60, 60);
  g.lineTo(74, 90);
  g.closePath();
  g.fill();
  for (const [cx, cy, r, c] of [
    [80, 70, 55, '#5d8c46'],
    [45, 95, 40, '#527e3d'],
    [120, 90, 44, '#679a4e'],
    [80, 40, 38, '#71a557'],
  ] as const) {
    g.fillStyle = c;
    g.beginPath();
    g.arc(cx, cy, r, 0, 7);
    g.fill();
  }

  // fence
  g.fillStyle = '#b78c5d';
  for (let x = 0; x < W; x += 46) {
    g.fillRect(x + 4, 138, 30, 76);
    g.beginPath();
    g.moveTo(x + 4, 138);
    g.lineTo(x + 19, 128);
    g.lineTo(x + 34, 138);
    g.closePath();
    g.fill();
  }
  g.fillStyle = '#a3794d';
  g.fillRect(0, 156, W, 10);
  g.fillRect(0, 188, W, 10);
  g.fillStyle = 'rgba(60,40,20,.18)';
  for (let x = 0; x < W; x += 46) g.fillRect(x + 4, 138, 3, 76);

  // lawn
  const lg = g.createLinearGradient(0, 210, 0, H);
  lg.addColorStop(0, '#86b25c');
  lg.addColorStop(1, '#5d8a3e');
  g.fillStyle = lg;
  g.fillRect(0, 210, W, H - 210);

  // mow stripes
  g.fillStyle = 'rgba(255,255,255,.05)';
  for (let i = 0; i < 6; i++) g.fillRect(0, 225 + i * 64, W, 32);

  // grass tufts + flowers
  g.strokeStyle = 'rgba(40,80,30,.5)';
  g.lineWidth = 2;
  g.lineCap = 'round';
  for (let i = 0; i < 120; i++) {
    const x = rnd(20, W - 20);
    const y = rnd(225, H - 30);
    g.beginPath();
    g.moveTo(x, y);
    g.quadraticCurveTo(x + rnd(-3, 3), y - 8, x + rnd(-5, 5), y - 13);
    g.stroke();
  }
  for (let i = 0; i < 14; i++) {
    const x = rnd(40, W - 40);
    const y = rnd(235, H - 40);
    g.fillStyle = ['#f0e15e', '#f1f1f1', '#e98ab4'][i % 3] as string;
    for (let p2 = 0; p2 < 5; p2++) {
      const a = p2 * 1.256;
      g.beginPath();
      g.arc(x + Math.cos(a) * 4, y + Math.sin(a) * 4, 3, 0, 7);
      g.fill();
    }
    g.fillStyle = '#e0a32e';
    g.beginPath();
    g.arc(x, y, 2.4, 0, 7);
    g.fill();
  }

  // patio corner
  g.fillStyle = '#c9b8a0';
  g.beginPath();
  g.moveTo(W, 210);
  g.lineTo(W - 200, 210);
  g.lineTo(W - 130, H);
  g.lineTo(W, H);
  g.closePath();
  g.fill();
  g.strokeStyle = 'rgba(90,70,50,.3)';
  g.lineWidth = 2;
  for (let i = 1; i < 5; i++) {
    g.beginPath();
    g.moveTo(W - 200 + i * 14, 210);
    g.lineTo(W - 130 + i * 7, H);
    g.stroke();
  }

  paintMagnolia(g, rng);
}

/** The magnolia — the squirrels' hideout (modelled on the real backyard tree). */
function paintMagnolia(g: G, rng: Rng): void {
  const m = YARD.magnolia;
  // ground shadow
  g.fillStyle = 'rgba(20,40,15,.18)';
  g.beginPath();
  g.ellipse(m.x, m.trunkBaseY + 8, m.canopyR * 0.8, 16, 0, 0, 7);
  g.fill();
  // trunk
  g.fillStyle = '#6e4a30';
  g.fillRect(m.x - 9, m.y, 18, m.trunkBaseY - m.y);
  g.strokeStyle = 'rgba(40,26,16,.4)';
  g.lineWidth = 2;
  g.beginPath();
  g.moveTo(m.x - 2, m.y + 8);
  g.lineTo(m.x - 2, m.trunkBaseY);
  g.stroke();
  // a couple of low branches into the canopy
  g.strokeStyle = '#6e4a30';
  g.lineWidth = 7;
  g.lineCap = 'round';
  g.beginPath();
  g.moveTo(m.x, m.y + 20);
  g.lineTo(m.x - 34, m.y - 6);
  g.moveTo(m.x, m.y + 30);
  g.lineTo(m.x + 30, m.y - 2);
  g.stroke();
  // lush rounded canopy
  for (const [ox, oy, r, c] of [
    [0, -8, m.canopyR, '#5f9148'],
    [-40, 6, m.canopyR * 0.7, '#558540'],
    [40, 2, m.canopyR * 0.72, '#69a050'],
    [-6, -40, m.canopyR * 0.62, '#73aa58'],
  ] as const) {
    g.fillStyle = c;
    g.beginPath();
    g.arc(m.x + ox, m.y + oy, r, 0, 7);
    g.fill();
  }
  // big creamy magnolia blossoms dotted through the leaves
  for (let i = 0; i < 16; i++) {
    const a = rng.range(0, 7);
    const rr = rng.range(8, m.canopyR - 12);
    const bx = m.x + Math.cos(a) * rr;
    const by = m.y - 10 + Math.sin(a) * rr * 0.8;
    g.fillStyle = ['#fbeef2', '#f7e2ea', '#fff6f2'][i % 3]!;
    for (let p = 0; p < 5; p++) {
      const pa = p * 1.256;
      g.beginPath();
      g.ellipse(bx + Math.cos(pa) * 4, by + Math.sin(pa) * 4, 3.4, 2.2, pa, 0, 7);
      g.fill();
    }
    g.fillStyle = '#e8c86a';
    g.beginPath();
    g.arc(bx, by, 1.8, 0, 7);
    g.fill();
  }
}

/** The backyard round: toys, cuddle spot, sunbeam, zoomies, predators + ambient events. */
export const yardScene: SceneDef = {
  config: {
    key: 'yard',
    name: 'The Backyard',
    sub: 'Round 1 of 3 — zoomies, squirrels & predators. Stick together!',
    time: 45,
  },
  bgKey: () => 'yard',
  paint: () => paintYard,
  visibleDogs: (s) => [s.dogs.cheddar, s.dogs.cocoa],
  enter(s: GameState): void {
    for (const id of ['cheddar', 'cocoa'] as const) {
      const d = s.dogs[id];
      d.room = '';
      d.mode = 'free';
    }
    placeSpot(s);
    placeSunbeam(s);
  },
  update(s: GameState, dt: number): void {
    updateToys(s, dt);
    updateSpot(s, dt);
    updateSunbeam(s, dt);
    updatePredator(s, dt);
    updateEvents(s, dt);
  },
  drawWorld(g: G, s: GameState): void {
    drawSunbeam(g, s);
    drawSpot(g, s);
    drawToys(g, s);
    drawEvents(g, s);
    drawPredator(g, s);
  },
};
