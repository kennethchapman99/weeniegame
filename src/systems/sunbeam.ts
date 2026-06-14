/**
 * systems/sunbeam.ts — the sunbeam: a relocating bask-for-points spot.
 * Ported from the prototype's placeSunbeam / drawSunbeam + the sunbeam block of update().
 *
 * Present in the yard from round 1 (and the house later) — NOT the pool. Bask solo for +1
 * every SUNBEAM.baskTime; share it and it's a standoff (nobody scores). It relocates every
 * ~9–13s. Beyond scoring, it's a moving attractor the AI navigates — without it the yard AI
 * gets pinned on the cuddle spot (see ai/sibling.ts), so it's load-bearing for AI feel.
 */

import type { GameState, Sunbeam } from '../state/gameState.js';
import { addScore } from '../state/gameState.js';
import { busy } from '../state/dog.js';
import { SUNBEAM, SCORE } from '../config/balance.js';
import { popup } from './particles.js';

type G = CanvasRenderingContext2D;

/** Place (or relocate) the yard sunbeam, kept clear of the cuddle spot. House placement: M7. */
export function placeSunbeam(s: GameState): void {
  if (s.sceneKey === 'pool') {
    s.sunbeam = null;
    return;
  }
  const rng = s.rng;
  let x = 0;
  let y = 0;
  let ok = false;
  let tries = 0;
  while (!ok && tries++ < 50) {
    x = rng.range(160, 800);
    y = rng.range(290, 530);
    ok = !s.spot || Math.hypot(x - s.spot.x, y - s.spot.y) > 170;
  }
  s.sunbeam = {
    x,
    y,
    room: '',
    r: 62,
    relocate: rng.range(SUNBEAM.relocate[0], SUNBEAM.relocate[1]),
    accA: 0,
    accB: 0,
    age: 0,
  };
}

export function updateSunbeam(s: GameState, dt: number): void {
  const sb = s.sunbeam;
  if (!sb) return;
  const rng = s.rng;
  const a = s.dogs.cheddar;
  const b = s.dogs.cocoa;
  sb.age += dt;
  sb.relocate -= dt;
  if (sb.relocate <= 0) {
    popup(s, sb.x, sb.y - 30, 'the sun moved…', '#ffe9b0');
    placeSunbeam(s);
    return;
  }
  const inBeam = (d: typeof a): boolean => !busy(d) && Math.hypot(sb.x - d.x, sb.y - d.y) < sb.r;
  const bA = inBeam(a);
  const bB = inBeam(b);
  if (bA && bB) {
    // standoff — nobody warms up
    sb.accA = Math.max(0, sb.accA - dt);
    sb.accB = Math.max(0, sb.accB - dt);
    if (rng.next() < dt * 1.6) {
      popup(
        s,
        (a.x + b.x) / 2,
        Math.min(a.y, b.y) - 52,
        ['grrr!', 'MY sunbeam!', '*side-eye*'][(rng.next() * 3) | 0]!,
        '#ffb37a',
      );
    }
  } else if (bA || bB) {
    const d = bA ? a : b;
    if (bA) {
      sb.accA += dt;
      sb.accB = Math.max(0, sb.accB - dt);
    } else {
      sb.accB += dt;
      sb.accA = Math.max(0, sb.accA - dt);
    }
    const acc = bA ? sb.accA : sb.accB;
    if (rng.next() < dt * 2) popup(s, d.x + rng.range(-8, 8), d.y - 40, '~', '#ffe9b0');
    if (acc >= SUNBEAM.baskTime) {
      addScore(s, d, SCORE.sunbeam);
      popup(s, sb.x, sb.y - 44, '+1 warm ☀︎', '#ffe9b0');
      if (bA) sb.accA = 0;
      else sb.accB = 0;
    }
  } else {
    sb.accA = Math.max(0, sb.accA - dt);
    sb.accB = Math.max(0, sb.accB - dt);
  }
}

export function drawSunbeam(g: G, s: GameState): void {
  const sb = s.sunbeam;
  if (!sb) return;
  drawSunbeamShape(g, sb, s.elapsedMs, s.rng.next());
}

function drawSunbeamShape(g: G, sb: Sunbeam, T: number, moteRoll: number): void {
  const gl = 0.8 + Math.sin(T * 0.004) * 0.2;
  g.save();
  // slanted light shaft from the top of the frame
  const shaft = g.createLinearGradient(sb.x - 120, 0, sb.x, sb.y);
  shaft.addColorStop(0, 'rgba(255,238,170,0)');
  shaft.addColorStop(1, `rgba(255,238,170,${0.16 * gl})`);
  g.fillStyle = shaft;
  g.beginPath();
  g.moveTo(sb.x - 150, 0);
  g.lineTo(sb.x - 55, 0);
  g.lineTo(sb.x + sb.r * 0.95, sb.y);
  g.lineTo(sb.x - sb.r * 0.95, sb.y);
  g.closePath();
  g.fill();
  // warm pool of light on the floor
  g.translate(sb.x, sb.y);
  g.scale(1, 0.55);
  const pool = g.createRadialGradient(0, 0, 8, 0, 0, sb.r + 14);
  pool.addColorStop(0, `rgba(255,226,140,${0.42 * gl})`);
  pool.addColorStop(0.7, `rgba(255,214,120,${0.22 * gl})`);
  pool.addColorStop(1, 'rgba(255,214,120,0)');
  g.fillStyle = pool;
  g.beginPath();
  g.arc(0, 0, sb.r + 14, 0, 7);
  g.fill();
  g.restore();
  // sun icon (dust motes are spawned by the painter via particles; omitted here to keep draw pure)
  void moteRoll;
  g.fillStyle = 'rgba(120,90,30,.5)';
  g.font = '800 11px -apple-system, sans-serif';
  g.textAlign = 'center';
  g.fillText('☀︎ sunbeam', sb.x, sb.y - sb.r - 8);
}
