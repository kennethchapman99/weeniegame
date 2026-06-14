/**
 * systems/toys.ts — toy spawn, pickup, scoring, and the rope flag.
 * Ported from the prototype's spawnToy/drawToy + the toy block of update().
 *
 * M2 implements the YARD (land) path. The pool floater/deck-band sampler (M5) and the
 * house room-aware sampler (M7) are added in their milestones. The rope "both dogs → TUG"
 * branch is M6; until then a rope toy can be solo-grabbed (+2) by an uncontested dog.
 */

import type { GameState, Toy } from '../state/gameState.js';
import { addScore } from '../state/gameState.js';
import type { Dog } from '../state/dog.js';
import { busy } from '../state/dog.js';
import { BOUNDS, SCORE, TUG } from '../config/balance.js';
import { burst, popup } from './particles.js';
import { rounded } from '../core/math.js';
import { sampleDeckBand } from '../scenes/poolGeometry.js';
import { startTug } from './tug.js';

type G = CanvasRenderingContext2D;
const TOYTYPES = ['ball', 'bone', 'duck'] as const;

export function spawnToy(s: GameState): void {
  const rng = s.rng;

  // Pool: most toys ride a floater; the rest sample the deck bands (never the water). No ropes.
  if (s.sceneKey === 'pool') {
    if (rng.next() < 0.6 && s.floaters.length) {
      const fl = (rng.next() * s.floaters.length) | 0;
      const f = s.floaters[fl]!;
      const ox = rng.range(-f.rx * 0.45, f.rx * 0.45);
      const oy = rng.range(-f.ry * 0.35, f.ry * 0.35);
      s.toys.push({
        x: f.x + ox, y: f.y + oy, room: '', fl, ox, oy,
        tug: false, type: TOYTYPES[(rng.next() * 3) | 0]!, t: rng.range(0, 9), scale: 0,
      });
    } else {
      const p = sampleDeckBand(rng);
      s.toys.push({
        x: p.x, y: p.y, room: '', fl: -1, ox: 0, oy: 0,
        tug: false, type: TOYTYPES[(rng.next() * 3) | 0]!, t: rng.range(0, 9), scale: 0,
      });
    }
    return;
  }

  // Yard land placement: keep clear of the cuddle spot.
  let x = 0;
  let y = 0;
  let ok = false;
  let tries = 0;
  while (!ok && tries++ < 40) {
    x = rng.range(BOUNDS.minX + 30, BOUNDS.maxX - 30);
    y = rng.range(BOUNDS.minY + 20, BOUNDS.maxY - 15);
    ok = !s.spot || Math.hypot(x - s.spot.x, y - s.spot.y) > 110;
  }
  const isTug = rng.next() < TUG.ropeSpawnChance;
  s.toys.push({
    x,
    y,
    room: '',
    fl: -1,
    ox: 0,
    oy: 0,
    tug: isTug,
    type: isTug ? 'rope' : TOYTYPES[(rng.next() * 3) | 0]!,
    t: rng.range(0, 9),
    scale: 0,
  });
}

export function updateToys(s: GameState, dt: number): void {
  const dogs = [s.dogs.cheddar, s.dogs.cocoa];

  s.spawnTimer -= dt;
  const toyCap = 3;
  if (s.spawnTimer <= 0 && s.toys.length < toyCap) {
    spawnToy(s);
    s.spawnTimer = s.rng.range(2.6, 4.2);
  }

  for (const o of s.toys) {
    o.scale += dt * 4;
    if (o.fl >= 0 && s.floaters[o.fl]) {
      o.x = s.floaters[o.fl]!.x + o.ox;
      o.y = s.floaters[o.fl]!.y + o.oy;
    }
  }

  for (let i = s.toys.length - 1; i >= 0; i--) {
    const o = s.toys[i]!;
    if (o.tug) {
      const near = (d: Dog): boolean => !busy(d) && Math.hypot(o.x - d.x, o.y - d.y) < TUG.grabRange;
      // both dogs reach the rope, both free → TUG OF WAR
      if (!s.tug && near(s.dogs.cheddar) && near(s.dogs.cocoa)) {
        startTug(s, o);
        continue;
      }
      // otherwise a single dog grabs it uncontested (+2)
      for (const d of dogs) {
        const opp = d === s.dogs.cheddar ? s.dogs.cocoa : s.dogs.cheddar;
        if (!s.tug && near(d) && !near(opp)) {
          addScore(s, d, SCORE.ropeSolo);
          burst(s, o.x, o.y, '#d96a6a', 12, 2.6);
          popup(s, o.x, o.y - 26, '+2 rope!', '#fff');
          s.toys.splice(i, 1);
          break;
        }
      }
      continue;
    }
    for (const d of dogs) {
      if (!busy(d) && Math.hypot(o.x - d.x, o.y - d.y) < 34) {
        addScore(s, d, SCORE.toy);
        burst(s, o.x, o.y, d.id === 'cheddar' ? '#f4d3a4' : '#a86d42', 12, 2.6);
        popup(s, o.x, o.y - 26, '+1', '#fff');
        s.toys.splice(i, 1);
        break;
      }
    }
  }
}

export function drawToys(g: G, s: GameState): void {
  for (const o of s.toys) drawToy(g, o, s.elapsedMs);
}

function drawToy(g: G, o: Toy, T: number): void {
  g.save();
  g.translate(o.x, o.y);
  const sc = Math.min(1, o.scale);
  g.scale(sc, sc);
  g.translate(0, Math.sin(o.t + T * 0.004) * 2);
  // shadow
  g.fillStyle = 'rgba(20,14,8,.22)';
  g.beginPath();
  g.ellipse(0, 11, 12, 4, 0, 0, 7);
  g.fill();
  if (o.type === 'ball') {
    const rg = g.createRadialGradient(-3, -4, 2, 0, 0, 11);
    rg.addColorStop(0, '#ff8a7a');
    rg.addColorStop(1, '#c43d33');
    g.fillStyle = rg;
    g.beginPath();
    g.arc(0, 0, 10, 0, 7);
    g.fill();
    g.strokeStyle = 'rgba(255,255,255,.6)';
    g.lineWidth = 2;
    g.beginPath();
    g.arc(0, 0, 10, -2.4, -0.8);
    g.stroke();
  } else if (o.type === 'bone') {
    g.fillStyle = '#f3ecdd';
    g.strokeStyle = '#cfc3a9';
    g.lineWidth = 1.5;
    rounded(g, -10, -3.5, 20, 7, 3.5);
    g.fill();
    g.stroke();
    for (const sx of [-10, 10])
      for (const sy of [-4, 4]) {
        g.beginPath();
        g.arc(sx, sy, 4.6, 0, 7);
        g.fill();
        g.stroke();
      }
  } else if (o.type === 'rope') {
    g.rotate(0.3 + Math.sin(o.t + T * 0.003) * 0.1);
    g.lineCap = 'round';
    g.strokeStyle = '#d9b38a';
    g.lineWidth = 7;
    g.beginPath();
    g.moveTo(-14, 0);
    g.lineTo(14, 0);
    g.stroke();
    g.strokeStyle = '#b98a5c';
    g.lineWidth = 3;
    for (let i = -12; i <= 12; i += 4) {
      g.beginPath();
      g.moveTo(i, -3);
      g.lineTo(i + 2, 3);
      g.stroke();
    }
    g.fillStyle = '#e8d2b0';
    g.beginPath();
    g.arc(-14, 0, 5, 0, 7);
    g.fill();
    g.beginPath();
    g.arc(14, 0, 5, 0, 7);
    g.fill();
    g.strokeStyle = '#e8d2b0';
    g.lineWidth = 1.4;
    for (const ex of [-18, 18]) {
      for (let a = -0.6; a <= 0.6; a += 0.3) {
        g.beginPath();
        g.moveTo(ex * 0.78, 0);
        g.lineTo(ex, Math.sin(a) * 5);
        g.stroke();
      }
    }
  } else {
    // duck — NOTE: faithful to the prototype, the head reuses the leftover shadow fill
    // (rgba(20,14,8,.22)); the prototype never sets a body colour here, so the duck head
    // renders near-invisible. Preserved deliberately (spec parity); flagged for the owner.
    g.beginPath();
    g.arc(7, -7, 5.5, 0, 7);
    g.fill();
    g.fillStyle = '#e98a2b';
    g.beginPath();
    g.moveTo(11, -7);
    g.lineTo(17, -5.4);
    g.lineTo(11, -3.8);
    g.closePath();
    g.fill();
    g.fillStyle = '#222';
    g.beginPath();
    g.arc(8.4, -8.4, 1.3, 0, 7);
    g.fill();
  }
  g.restore();
}
