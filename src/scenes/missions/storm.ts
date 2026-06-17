/**
 * scenes/missions/storm.ts — "The Thunderstorm", an M13 co-op SURVIVE mission introducing the
 * shared-panic / co-regulation primitive (reusable later for the vet, nail grinder, big dogs…).
 *
 * Each pup has its own panic meter. Thunder strikes are telegraphed by a lightning flash, then a
 * boom spikes the panic of any pup caught in the open near the strike — duck under a shelter (the
 * table, the deck chair, the blanket nest) to ride it out. A pup left alone slowly works itself
 * up; the ONLY way panic comes back down is to huddle close and comfort each other. If EITHER
 * pup's panic maxes out, both bolt and the mission fails. Weather the whole storm together to win.
 *
 * Interdependence: neither pup can calm down alone — draining panic requires the sibling within
 * cuddle range, so surviving is a shared act of co-regulation. The emotional heart of the game.
 */

import type { SceneDef } from '../types.js';
import type { GameState } from '../../state/gameState.js';
import { playSound } from '../../state/gameState.js';
import { paintYard } from '../yard.js';
import { STORM } from '../../config/balance.js';
import { startMission, setProgress, objective } from '../../systems/mission.js';
import { heartBurst, popup } from '../../systems/particles.js';

type G = CanvasRenderingContext2D;

interface Shelter {
  x: number;
  y: number;
  r: number;
}
interface StormData {
  panic: { cheddar: number; cocoa: number };
  flash: number; // lightning flash brightness 0..1 (telegraph + boom)
  boomIn: number; // countdown to next strike
  strikeX: number; // epicenter of the pending/active strike
  shelters: Shelter[];
}

const SHELTERS: Shelter[] = [
  { x: 250, y: 430, r: 74 }, // the table
  { x: 1000, y: 430, r: 74 }, // the deck chair
  { x: 640, y: 580, r: 80 }, // the blanket nest
];

export const stormMission: SceneDef = {
  config: {
    key: 'mission-storm',
    name: 'The Thunderstorm',
    sub: 'Co-op — huddle close to calm each other; duck under shelter when thunder strikes. Survive!',
    time: 45,
  },
  bgKey: () => 'yard',
  paint: () => paintYard,
  visibleDogs: (s) => [s.dogs.cheddar, s.dogs.cocoa],

  enter(s: GameState): void {
    s.dogs.cheddar.x = 600;
    s.dogs.cheddar.y = 470;
    s.dogs.cocoa.x = 680;
    s.dogs.cocoa.y = 470;
    for (const id of ['cheddar', 'cocoa'] as const) {
      s.dogs[id].mode = 'free';
      s.dogs[id].room = '';
    }
    const data: StormData = {
      panic: { cheddar: 0, cocoa: 0 },
      flash: 0,
      boomIn: 3,
      strikeX: 640,
      shelters: SHELTERS.map((sh) => ({ ...sh })),
    };
    startMission(s, {
      key: 'mission-storm',
      title: 'The Thunderstorm',
      objectives: [objective('survive', 'Weather the storm together (45s)', 0)],
      timeLimit: 45,
      surviveMode: true,
      data,
    });
  },

  update(s: GameState, dt: number): void {
    const m = s.mission;
    if (!m || m.status !== 'active') return;
    const d = m.data as StormData;
    const a = s.dogs.cheddar;
    const b = s.dogs.cocoa;
    const cuddling = Math.hypot(a.x - b.x, a.y - b.y) <= STORM.cuddleR;

    setProgress(s, 0, Math.min(0.999, m.elapsed / m.timeLimit)); // tickMission finishes it at 100%

    if (d.flash > 0) d.flash = Math.max(0, d.flash - dt * 1.6);

    // strike cadence: telegraph with a flash, then boom
    d.boomIn -= dt;
    if (d.boomIn <= STORM.flashLead && d.flash < 0.4) {
      d.flash = 0.5; // lightning telegraph
      d.strikeX = s.rng.range(160, 1120);
    }
    if (d.boomIn <= 0) {
      d.flash = 1;
      playSound(s, 'screech'); // thunderclap stand-in
      for (const id of ['cheddar', 'cocoa'] as const) {
        const dog = s.dogs[id];
        const sheltered = d.shelters.some((sh) => Math.hypot(dog.x - sh.x, dog.y - sh.y) <= sh.r);
        const near = 1 - Math.min(1, Math.abs(dog.x - d.strikeX) / STORM.spikeFalloff);
        let spike = STORM.panicSpike * near;
        if (sheltered) spike *= 1 - STORM.shelterShield;
        d.panic[id] = Math.min(1, d.panic[id] + spike);
      }
      d.boomIn = s.rng.range(STORM.boomEvery[0], STORM.boomEvery[1]);
    }

    // co-regulation: cuddling drains both pups' panic; apart, each pup works itself up
    for (const id of ['cheddar', 'cocoa'] as const) {
      d.panic[id] = cuddling
        ? Math.max(0, d.panic[id] - STORM.comfortDrain * dt)
        : Math.min(1, d.panic[id] + STORM.aloneRise * dt);
    }
    if (cuddling && s.rng.next() < dt * 1.5) heartBurst(s, (a.x + b.x) / 2, (a.y + b.y) / 2 - 20);

    // either pup maxing out → both bolt → fail
    const maxed = (['cheddar', 'cocoa'] as const).find((id) => d.panic[id] >= 1);
    if (maxed) {
      m.status = 'fail';
      popup(s, s.dogs[maxed].x, s.dogs[maxed].y - 40, 'panicked! ⚡', '#ff5a5a');
      playSound(s, 'screech');
    }
  },

  coopAi(s: GameState): [number, number] {
    // the partner sticks to the player to keep them cuddling (and so calm)
    const d = s.dogs[s.aiId];
    const p = s.dogs[s.playerId];
    return [p.x - d.x, p.y - d.y];
  },

  drawWorld(g: G, s: GameState): void {
    const m = s.mission;
    if (!m) return;
    const d = m.data as StormData;
    const a = s.dogs.cheddar;
    const b = s.dogs.cocoa;
    const cuddling = Math.hypot(a.x - b.x, a.y - b.y) <= STORM.cuddleR;

    // shelters (safe rings + a simple blanket/table shape)
    for (const sh of d.shelters) {
      const occupied = [a, b].some((dog) => Math.hypot(dog.x - sh.x, dog.y - sh.y) <= sh.r);
      g.save();
      g.strokeStyle = occupied ? 'rgba(126,208,126,.55)' : 'rgba(150,170,210,.4)';
      g.setLineDash([7, 7]);
      g.lineWidth = 3;
      g.beginPath();
      g.arc(sh.x, sh.y, sh.r, 0, 7);
      g.stroke();
      g.setLineDash([]);
      g.fillStyle = 'rgba(70,60,90,.45)';
      g.beginPath();
      g.ellipse(sh.x, sh.y, sh.r * 0.7, sh.r * 0.42, 0, 0, 7);
      g.fill();
      g.restore();
    }
    g.fillStyle = '#cdd6ec';
    g.textAlign = 'center';
    g.font = '700 11px -apple-system, sans-serif';
    for (const sh of d.shelters) g.fillText('SHELTER', sh.x, sh.y + sh.r + 14);

    // cuddle link between the pups when comforting
    if (cuddling) {
      g.save();
      g.strokeStyle = 'rgba(255,150,180,.5)';
      g.lineWidth = 4;
      g.setLineDash([5, 5]);
      g.beginPath();
      g.moveTo(a.x, a.y);
      g.lineTo(b.x, b.y);
      g.stroke();
      g.restore();
    }

    // per-pup panic bars above their heads
    for (const dog of [a, b]) {
      const p = d.panic[dog.id];
      const w = 44;
      const x = dog.x - w / 2;
      const y = dog.y - 48;
      g.fillStyle = 'rgba(0,0,0,.45)';
      g.fillRect(x - 2, y - 2, w + 4, 9);
      g.fillStyle = p > 0.66 ? '#ff5a5a' : p > 0.33 ? '#ffc24a' : '#7ed07e';
      g.fillRect(x, y, w * p, 5);
    }

    // storm overlay: a dim, rain-streaked wash that flares white on a strike
    g.save();
    g.fillStyle = 'rgba(20,24,40,0.32)';
    g.fillRect(0, 0, 1280, 720);
    if (d.flash > 0) {
      g.fillStyle = `rgba(255,255,255,${0.5 * d.flash})`;
      g.fillRect(0, 0, 1280, 720);
      if (d.flash > 0.8) {
        // the bolt
        g.strokeStyle = `rgba(255,255,240,${d.flash})`;
        g.lineWidth = 3;
        g.beginPath();
        g.moveTo(d.strikeX, 0);
        g.lineTo(d.strikeX - 24, 120);
        g.lineTo(d.strikeX + 14, 150);
        g.lineTo(d.strikeX - 18, 300);
        g.stroke();
      }
    }
    g.restore();
  },
};
