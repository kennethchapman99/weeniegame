/**
 * scenes/missions/burrow.ts — "Buried Treasure", a co-op scent-and-dig mission.
 *
 * Cocoa has the nose: lingering by a suspicious mound reveals which burrows hide treats.
 * Cheddar has the paws: a JUMP on a revealed mound digs the treat loose. Either pup can then
 * collect it, but the reveal→dig chain makes the pups cooperate instead of solo-clearing.
 */

import type { SceneDef } from '../types.js';
import type { GameState } from '../../state/gameState.js';
import { playSound } from '../../state/gameState.js';
import { paintYard } from '../yard.js';
import { BURROW } from '../../config/balance.js';
import { startMission, setProgress, addCombined, completeObjective, objective } from '../../systems/mission.js';
import { burst, popup } from '../../systems/particles.js';
import { tryJump } from '../../systems/jump.js';

type G = CanvasRenderingContext2D;

type BurrowState = 'hidden' | 'revealing' | 'revealed' | 'dug' | 'collected' | 'empty';
interface Burrow {
  x: number;
  y: number;
  treat: boolean;
  state: BurrowState;
  sniff: number;
  seed: number;
}
interface LooseTreat {
  x: number;
  y: number;
  bob: number;
}
interface BurrowData {
  burrows: Burrow[];
  treats: LooseTreat[];
  found: number;
  digCD: number;
}

const BURROWS: Array<Omit<Burrow, 'state' | 'sniff'>> = [
  { x: 170, y: 302, treat: true, seed: 0.2 },
  { x: 330, y: 430, treat: false, seed: 1.1 },
  { x: 482, y: 322, treat: true, seed: 2.0 },
  { x: 642, y: 476, treat: true, seed: 2.8 },
  { x: 780, y: 350, treat: false, seed: 3.6 },
  { x: 825, y: 515, treat: true, seed: 4.4 },
];

export const burrowMission: SceneDef = {
  config: {
    key: 'mission-burrow',
    name: 'Buried Treasure',
    sub: 'Co-op — Cocoa sniffs out hidden treats, Cheddar digs them up with a jump.',
    time: 90,
  },
  bgKey: () => 'yard',
  paint: () => paintYard,
  visibleDogs: (s) => [s.dogs.cheddar, s.dogs.cocoa],

  enter(s: GameState): void {
    s.dogs.cheddar.x = 250;
    s.dogs.cheddar.y = 520;
    s.dogs.cocoa.x = 185;
    s.dogs.cocoa.y = 470;
    for (const id of ['cheddar', 'cocoa'] as const) {
      s.dogs[id].mode = 'free';
      s.dogs[id].room = '';
    }
    const data: BurrowData = {
      burrows: BURROWS.map((b) => ({ ...b, state: 'hidden', sniff: 0 })),
      treats: [],
      found: 0,
      digCD: 0,
    };
    startMission(s, {
      key: 'mission-burrow',
      title: 'Buried Treasure',
      objectives: [objective('collectTogether', `Find ${BURROW.target} buried treats`, 0, BURROW.target)],
      timeLimit: 90,
      starTime: [32, 56],
      data,
    });
  },

  update(s: GameState, dt: number): void {
    const m = s.mission;
    if (!m || m.status !== 'active') return;
    const data = m.data as BurrowData;
    if (data.digCD > 0) data.digCD -= dt;

    const cocoa = s.dogs.cocoa;
    for (const b of data.burrows) {
      if (b.state === 'dug' || b.state === 'collected' || b.state === 'empty') continue;
      const sniffing = cocoa.mode === 'free' && Math.hypot(cocoa.x - b.x, cocoa.y - b.y) <= BURROW.sniffR;
      if (sniffing) {
        b.sniff = Math.min(BURROW.sniffTime, b.sniff + dt);
        b.state = b.sniff >= BURROW.sniffTime ? 'revealed' : 'revealing';
        if (b.state === 'revealed' && !b.treat) b.state = 'empty';
      } else if (b.state === 'revealing') {
        b.sniff = Math.max(0, b.sniff - dt * 0.5);
        if (b.sniff <= 0) b.state = 'hidden';
      }
    }

    const cheddar = s.dogs.cheddar;
    if (cheddar.jumpT > 0 && data.digCD <= 0) {
      const b = data.burrows.find(
        (burrow) => burrow.state === 'revealed' && Math.hypot(cheddar.x - burrow.x, cheddar.y - burrow.y) <= BURROW.digR,
      );
      if (b) {
        b.state = 'dug';
        data.digCD = BURROW.digCD;
        data.treats.push({ x: b.x, y: b.y - 20, bob: b.seed });
        popup(s, b.x, b.y - 34, 'dug it up! 🦴', '#ffd98c');
        burst(s, b.x, b.y, '#8c6a3e', 14, 2.8);
        playSound(s, 'bark');
      }
    }

    const dogs = [s.dogs.cheddar, s.dogs.cocoa];
    for (let i = data.treats.length - 1; i >= 0; i--) {
      const t = data.treats[i]!;
      t.bob += dt * 5;
      if (dogs.some((d) => d.mode === 'free' && Math.hypot(d.x - t.x, d.y - t.y) <= BURROW.collectR)) {
        data.treats.splice(i, 1);
        data.found++;
        const b = data.burrows.find((burrow) => burrow.state === 'dug' && Math.hypot(burrow.x - t.x, burrow.y - (t.y + 20)) < 1);
        if (b) b.state = 'collected';
        addCombined(s, BURROW.treatScore);
        popup(s, t.x, t.y - 18, 'treasure! ✨', '#9effa0');
        burst(s, t.x, t.y, '#f4d3a4', 12, 2.5);
        playSound(s, 'yip');
      }
    }

    setProgress(s, 0, data.found / BURROW.target);
    if (data.found >= BURROW.target && !m.objectives[0]!.done) completeObjective(s, 0);
  },

  coopAi(s: GameState): [number, number] {
    const m = s.mission;
    const d = s.dogs[s.aiId];
    if (!m) return [0, 0];
    const data = m.data as BurrowData;
    const treat = data.treats[0];
    if (treat) return [treat.x - d.x, treat.y - d.y];
    if (d.id === 'cocoa') {
      const b = data.burrows.find((burrow) => burrow.state === 'hidden' || burrow.state === 'revealing');
      return b ? [b.x - d.x, b.y - d.y] : [0, 0];
    }
    const b = data.burrows.find((burrow) => burrow.state === 'revealed');
    if (b) {
      if (Math.hypot(d.x - b.x, d.y - b.y) < BURROW.digR * 0.65 && d.jumpT <= 0) tryJump(s, d);
      return [b.x - d.x, b.y - d.y];
    }
    return [s.dogs.cocoa.x - d.x, s.dogs.cocoa.y - d.y];
  },

  drawWorld(g: G, s: GameState): void {
    const m = s.mission;
    if (!m) return;
    const data = m.data as BurrowData;
    for (const b of data.burrows) drawBurrow(g, b);
    for (const t of data.treats) drawTreat(g, t.x, t.y + Math.sin(t.bob) * 3);
    g.fillStyle = 'rgba(255,255,255,.55)';
    g.textAlign = 'center';
    g.font = '700 12px -apple-system, sans-serif';
    g.fillText('Cocoa sniffs suspicious mounds · Cheddar JUMPS to dig revealed treasure', 480, 222);
  },
};

function drawBurrow(g: G, b: Burrow): void {
  g.save();
  g.translate(b.x, b.y);
  g.fillStyle = b.state === 'empty' ? 'rgba(80,70,55,.35)' : '#7a5631';
  g.beginPath();
  g.ellipse(0, 0, 34, 18, 0, 0, 7);
  g.fill();
  g.fillStyle = 'rgba(35,24,15,.35)';
  g.beginPath();
  g.ellipse(0, 2, 22, 10, 0, 0, 7);
  g.fill();
  if (b.state === 'revealing') {
    g.strokeStyle = '#f4d3a4';
    g.lineWidth = 4;
    g.beginPath();
    g.arc(0, 0, 42, -Math.PI / 2, -Math.PI / 2 + (b.sniff / BURROW.sniffTime) * Math.PI * 2);
    g.stroke();
  }
  if (b.state === 'revealed') {
    g.fillStyle = '#ffe24a';
    g.font = '800 22px -apple-system, sans-serif';
    g.textAlign = 'center';
    g.fillText('!', 0, -24);
  }
  if (b.state === 'empty') {
    g.fillStyle = '#ddd';
    g.font = '16px -apple-system, sans-serif';
    g.textAlign = 'center';
    g.fillText('×', 0, -18);
  }
  g.restore();
}

function drawTreat(g: G, x: number, y: number): void {
  g.save();
  g.fillStyle = '#caa05a';
  g.beginPath();
  g.roundRect(x - 18, y - 7, 36, 14, 6);
  g.fill();
  for (const dx of [-18, 18]) {
    g.beginPath();
    g.arc(x + dx, y, 8, 0, 7);
    g.fill();
  }
  g.restore();
}
