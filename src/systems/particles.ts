/**
 * systems/particles.ts — bursts, hearts, and floating score popups.
 * Ported from the prototype's burst/heartBurst/popup + their update/draw. Cosmetic only;
 * spawn randomness uses the injected rng (deterministic, lint-clean).
 */

import type { GameState } from '../state/gameState.js';
import type { Rng } from '../core/rng.js';

type G = CanvasRenderingContext2D;

export function burst(s: GameState, x: number, y: number, col: string, n = 14, spd = 3): void {
  const r: Rng = s.rng;
  for (let i = 0; i < n; i++) {
    const a = r.next() * 7;
    s.particles.push({
      x,
      y,
      vx: Math.cos(a) * r.range(0.5, spd),
      vy: Math.sin(a) * r.range(0.5, spd) - 1,
      life: 1,
      size: r.range(2, 5),
      col,
    });
  }
}

export function heartBurst(s: GameState, x: number, y: number): void {
  const r = s.rng;
  for (let i = 0; i < 8; i++) {
    s.particles.push({
      x: x + r.range(-12, 12),
      y: y + r.range(-8, 8),
      vx: r.range(-0.6, 0.6),
      vy: r.range(-2.4, -1.2),
      life: 1,
      size: r.range(5, 9),
      col: '#e76a6a',
      heart: true,
    });
  }
}

export function popup(s: GameState, x: number, y: number, text: string, col: string): void {
  s.popups.push({ x, y, text, col, life: 1 });
}

export function updateParticles(s: GameState, dt: number): void {
  for (let i = s.particles.length - 1; i >= 0; i--) {
    const p = s.particles[i]!;
    p.x += p.vx;
    p.y += p.vy;
    p.vy += p.heart ? 0.015 : p.mote ? 0 : 0.08;
    p.life -= dt * 1.4;
    if (p.life <= 0) s.particles.splice(i, 1);
  }
  for (let i = s.popups.length - 1; i >= 0; i--) {
    const p = s.popups[i]!;
    p.y -= dt * 26;
    p.life -= dt * 0.7;
    if (p.life <= 0) s.popups.splice(i, 1);
  }
}

export function drawParticles(g: G, s: GameState): void {
  for (const p of s.particles) {
    g.globalAlpha = Math.max(0, p.life);
    g.fillStyle = p.col;
    if (p.heart) {
      g.save();
      g.translate(p.x, p.y);
      g.scale(p.size / 10, p.size / 10);
      g.beginPath();
      g.moveTo(0, 3);
      g.bezierCurveTo(-6, -3, -3, -8, 0, -4);
      g.bezierCurveTo(3, -8, 6, -3, 0, 3);
      g.fill();
      g.restore();
    } else {
      g.beginPath();
      g.arc(p.x, p.y, p.size, 0, 7);
      g.fill();
    }
  }
  g.globalAlpha = 1;
}

export function drawPopups(g: G, s: GameState): void {
  for (const p of s.popups) {
    g.globalAlpha = Math.max(0, Math.min(1, p.life * 1.6));
    if (p.burst) {
      drawBarkBurst(g, p.x, p.y, p.text, p.col, p.life, p.rot ?? 0);
      continue;
    }
    g.textAlign = 'center';
    g.font = '800 17px -apple-system, sans-serif';
    g.lineWidth = 4;
    g.strokeStyle = 'rgba(30,20,10,.7)';
    g.strokeText(p.text, p.x, p.y);
    g.fillStyle = p.col;
    g.fillText(p.text, p.x, p.y);
  }
  g.globalAlpha = 1;
}

/** A comic-book "WOOF!" speech burst: jagged starburst + chunky text that pops in then shrinks. */
function drawBarkBurst(
  g: G,
  x: number,
  y: number,
  text: string,
  col: string,
  life: number,
  rot: number,
): void {
  // pop in fast (life 1→.8), then drift/shrink. Clamp life so a stray value can't blow up.
  const lf = Math.max(0, Math.min(1, life));
  const pop = lf > 0.8 ? (1 - lf) / 0.2 : 1;
  const scale = (0.6 + pop * 0.55) * (0.7 + lf * 0.4);
  g.save();
  g.translate(x, y);
  g.rotate(rot);
  g.scale(scale, scale);
  // jagged starburst behind the text
  const spikes = 11;
  g.beginPath();
  for (let i = 0; i < spikes * 2; i++) {
    const ang = (i / (spikes * 2)) * Math.PI * 2;
    const r = i % 2 === 0 ? 42 : 26;
    const px = Math.cos(ang) * r;
    const py = Math.sin(ang) * r;
    if (i === 0) g.moveTo(px, py);
    else g.lineTo(px, py);
  }
  g.closePath();
  g.fillStyle = '#ffd23a';
  g.fill();
  g.lineWidth = 3;
  g.strokeStyle = '#3a2a10';
  g.stroke();
  // chunky text
  g.textAlign = 'center';
  g.textBaseline = 'middle';
  g.font = '900 20px Georgia, serif';
  g.lineWidth = 5;
  g.strokeStyle = '#3a2a10';
  g.strokeText(text, 0, 1);
  g.fillStyle = col;
  g.fillText(text, 0, 1);
  g.textBaseline = 'alphabetic';
  g.restore();
}
