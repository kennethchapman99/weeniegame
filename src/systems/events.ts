/**
 * systems/events.ts — ambient bonuses: squirrel chase (+3, yard fence run), telegraphed treat
 * drop (+2), belly-rub power-up (3s wrestle immunity). Ported from the prototype's
 * spawnSquirrel/spawnTreat/spawnBelly/updateEvents/drawEvents. One event at a time.
 *
 * Scope: yard + house (NOT pool — keeps treats out of the water). Squirrel is yard-only.
 */

import type { GameState, Treat, BellyRub, Squirrel } from '../state/gameState.js';
import { addScore, cap, playSound } from '../state/gameState.js';
import { busy } from '../state/dog.js';
import { WORLD, EVENTS } from '../config/balance.js';
import { burst, heartBurst, popup } from './particles.js';
import { rounded } from '../core/math.js';

type G = CanvasRenderingContext2D;
const W = WORLD.w;

function spawnSquirrel(s: GameState): void {
  const rng = s.rng;
  const dir = rng.next() < 0.5 ? 1 : -1;
  s.squirrel = {
    x: dir > 0 ? -30 : W + 30,
    y: rng.range(216, 232),
    vx: dir * 4.4,
    dir,
    seed: rng.range(0, 9),
    got: false,
  };
  s.toast = '🐿️ SQUIRREL! Go go go!';
  playSound(s, 'yip');
}

function spawnTreat(s: GameState): void {
  s.treat = {
    x: s.rng.range(180, 800),
    y: s.rng.range(280, 520),
    room: '',
    telegraph: 1.1,
    glow: 0,
  };
  s.toast = '🍪 A treat dropped!';
}

function spawnBelly(s: GameState): void {
  s.bellyRub = {
    x: s.rng.range(180, 800),
    y: s.rng.range(280, 520),
    room: '',
    r: 36,
    life: 9,
  };
}

export function updateEvents(s: GameState, dt: number): void {
  if (s.sceneKey === 'pool') return; // events are yard/house only
  const yard = s.sceneKey === 'yard';

  // scheduler — one ambient event at a time
  if (!s.squirrel && !s.treat && !s.bellyRub) {
    s.eventTimer -= dt;
    if (s.eventTimer <= 0 && s.timeLeft > 6) {
      const roll = s.rng.next();
      if (yard && roll < EVENTS.squirrelChance) spawnSquirrel(s);
      else if (roll < EVENTS.treatChance) spawnTreat(s);
      else spawnBelly(s);
      s.eventTimer = s.rng.range(EVENTS.schedule[0], EVENTS.schedule[1]);
    }
  }

  const dogs = [s.dogs.cheddar, s.dogs.cocoa];

  if (s.squirrel) {
    const q = s.squirrel;
    q.x += q.vx * dt * 60;
    for (const d of dogs) {
      if (!busy(d) && !q.got && Math.hypot(q.x - d.x, q.y - d.y) < 40) {
        q.got = true;
        addScore(s, d, EVENTS.squirrelReward);
        playSound(s, 'bark');
        burst(s, q.x, q.y, '#a8835c', 16, 3);
        popup(s, q.x, q.y - 30, `${cap(d.id)} got the squirrel! +3`, '#ffe24a');
      }
    }
    if (q.got || q.x < -60 || q.x > W + 60) s.squirrel = null;
  }

  if (s.treat) {
    const tr = s.treat;
    if (tr.telegraph > 0) {
      tr.telegraph -= dt;
      tr.glow = tr.telegraph;
    } else {
      for (const d of dogs) {
        if (!busy(d) && Math.hypot(tr.x - d.x, tr.y - d.y) < 32) {
          addScore(s, d, EVENTS.treatReward);
          playSound(s, 'yip');
          burst(s, tr.x, tr.y, '#c98a4a', 12, 2.4);
          popup(s, tr.x, tr.y - 26, '+2 treat!', '#ffd98c');
          s.treat = null;
          break;
        }
      }
    }
  }

  if (s.bellyRub) {
    const bl = s.bellyRub;
    bl.life -= dt;
    let taken = false;
    for (const d of dogs) {
      if (!busy(d) && Math.hypot(bl.x - d.x, bl.y - d.y) < bl.r) {
        d.immune = EVENTS.bellyImmunity;
        heartBurst(s, bl.x, bl.y - 10);
        popup(s, bl.x, bl.y - 40, `${cap(d.id)}: belly rub! 🛡️`, '#ffd98c');
        playSound(s, 'yip');
        taken = true;
        break;
      }
    }
    if (taken || bl.life <= 0) s.bellyRub = null;
  }
}

export function drawEvents(g: G, s: GameState): void {
  if (s.squirrel) drawSquirrel(g, s.squirrel, s.elapsedMs);
  if (s.treat) drawTreat(g, s.treat);
  if (s.bellyRub) drawBelly(g, s.bellyRub, s.elapsedMs);
}

function drawSquirrel(g: G, q: Squirrel, T: number): void {
  g.save();
  g.translate(q.x, q.y);
  g.scale(q.dir, 1);
  const hop = Math.abs(Math.sin(T * 0.02 + q.seed)) * 4;
  g.translate(0, -hop);
  g.fillStyle = 'rgba(20,14,8,.25)';
  g.beginPath();
  g.ellipse(0, 12 + hop, 12, 3, 0, 0, 7);
  g.fill();
  g.fillStyle = '#8a6b48';
  g.beginPath();
  g.moveTo(-8, 2);
  g.quadraticCurveTo(-24, -6, -18, -20);
  g.quadraticCurveTo(-10, -26, -4, -18);
  g.quadraticCurveTo(-10, -8, -6, 2);
  g.closePath();
  g.fill();
  g.fillStyle = '#9c7a54';
  g.beginPath();
  g.ellipse(2, 2, 10, 8, 0, 0, 7);
  g.fill();
  g.beginPath();
  g.arc(11, -4, 5.5, 0, 7);
  g.fill();
  g.fillStyle = '#6e5238';
  g.beginPath();
  g.moveTo(9, -9);
  g.lineTo(8, -15);
  g.lineTo(13, -10);
  g.closePath();
  g.fill();
  g.fillStyle = '#222';
  g.beginPath();
  g.arc(13, -5, 1.4, 0, 7);
  g.fill();
  g.restore();
}

function drawTreat(g: G, tr: Treat): void {
  if (tr.telegraph > 0) {
    g.strokeStyle = `rgba(255,210,120,${0.8 * tr.glow})`;
    g.lineWidth = 3;
    g.beginPath();
    g.arc(tr.x, tr.y, 10 + tr.telegraph * 40, 0, 7);
    g.stroke();
  }
  g.save();
  g.translate(tr.x, tr.y);
  g.fillStyle = 'rgba(20,14,8,.2)';
  g.beginPath();
  g.ellipse(0, 7, 9, 3, 0, 0, 7);
  g.fill();
  g.fillStyle = '#b8763e';
  rounded(g, -8, -6, 16, 12, 4);
  g.fill();
  g.fillStyle = '#7a4a24';
  for (const [cx, cy] of [
    [-3, -2],
    [3, -3],
    [0, 2],
    [-4, 3],
    [4, 1],
  ] as const) {
    g.beginPath();
    g.arc(cx, cy, 1.4, 0, 7);
    g.fill();
  }
  g.restore();
}

function drawBelly(g: G, bl: BellyRub, T: number): void {
  const gl = 0.6 + Math.sin(T * 0.006) * 0.25;
  g.save();
  g.translate(bl.x, bl.y);
  g.scale(1, 0.6);
  const ag = g.createRadialGradient(0, 0, 6, 0, 0, bl.r + 10);
  ag.addColorStop(0, `rgba(180,230,255,${0.4 * gl})`);
  ag.addColorStop(1, 'rgba(180,230,255,0)');
  g.fillStyle = ag;
  g.beginPath();
  g.arc(0, 0, bl.r + 10, 0, 7);
  g.fill();
  g.restore();
  g.fillStyle = 'rgba(60,90,120,.6)';
  g.font = '800 11px -apple-system, sans-serif';
  g.textAlign = 'center';
  g.fillText('🖐️ belly rub', bl.x, bl.y - bl.r - 6);
}
