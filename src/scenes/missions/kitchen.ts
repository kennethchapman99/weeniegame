/**
 * scenes/missions/kitchen.ts — "Kitchen Counter Caper", an M13 co-op mission with the dogs'
 * first ASYMMETRIC abilities (the heart of the Kitchen pitch in docs/LEVEL-IDEAS.md, promoted
 * to co-op per docs/COOP-VISION.md).
 *
 *   - CHEDDAR (chaos pup) can chair-leap: a JUMP next to the counter knocks a snack onto the
 *     floor. Only he can reach the counter.
 *   - COCOA (and Cheddar) gobble snacks once they're on the floor.
 *
 * Interdependence + the co-op fantasy: Cheddar supplies (knocks snacks down), the team eats —
 * and a fallen snack is swept up if it sits too long, so one pup can't knock-and-gobble fast
 * enough alone; Cheddar keeps knocking while Cocoa keeps eating. Eat the target to win.
 */

import type { SceneDef } from '../types.js';
import type { GameState } from '../../state/gameState.js';
import { playSound } from '../../state/gameState.js';
import { paintYard } from '../yard.js';
import { KITCHEN } from '../../config/balance.js';
import { startMission, setProgress, addCombined, completeObjective, objective } from '../../systems/mission.js';
import { tryJump } from '../../systems/jump.js';
import { burst, popup } from '../../systems/particles.js';

type G = CanvasRenderingContext2D;

const COUNTER = { x0: 280, x1: 700, y: 252 };
const FOOD_XS = [322, 410, 498, 586, 660];
const FLOOR_Y = 384;

interface FloorFood {
  x: number;
  y: number;
  ttl: number;
}
interface KitchenData {
  onCounter: { x: number; knocked: boolean }[];
  floor: FloorFood[];
  eaten: number;
  knockCD: number;
}

export const kitchenMission: SceneDef = {
  config: {
    key: 'mission-kitchen',
    name: 'Kitchen Counter Caper',
    sub: 'Co-op — Cheddar knocks snacks off the counter, the team gobbles them up.',
    time: 85,
  },
  bgKey: () => 'yard',
  paint: () => paintYard,
  visibleDogs: (s) => [s.dogs.cheddar, s.dogs.cocoa],

  enter(s: GameState): void {
    s.dogs.cheddar.x = 360;
    s.dogs.cheddar.y = 470;
    s.dogs.cocoa.x = 560;
    s.dogs.cocoa.y = 470;
    for (const id of ['cheddar', 'cocoa'] as const) {
      s.dogs[id].mode = 'free';
      s.dogs[id].room = '';
    }
    const data: KitchenData = {
      onCounter: FOOD_XS.map((x) => ({ x, knocked: false })),
      floor: [],
      eaten: 0,
      knockCD: 0,
    };
    startMission(s, {
      key: 'mission-kitchen',
      title: 'Kitchen Counter Caper',
      objectives: [objective('collectTogether', `Gobble ${KITCHEN.target} counter snacks`, 0, KITCHEN.target)],
      timeLimit: 85,
      starTime: [35, 60],
      data,
    });
  },

  update(s: GameState, dt: number): void {
    const m = s.mission;
    if (!m || m.status !== 'active') return;
    const d = m.data as KitchenData;
    if (d.knockCD > 0) d.knockCD -= dt;

    // CHEDDAR ONLY: a chair-leap (airborne) by the counter knocks the nearest snack to the floor.
    const ched = s.dogs.cheddar;
    if (ched.jumpT > 0 && d.knockCD <= 0) {
      let best: { x: number; knocked: boolean } | null = null;
      let bd: number = KITCHEN.leapRange;
      for (const f of d.onCounter) {
        if (f.knocked) continue;
        const dd = Math.hypot(ched.x - f.x, ched.y - COUNTER.y);
        if (dd <= bd) {
          bd = dd;
          best = f;
        }
      }
      if (best) {
        best.knocked = true;
        d.floor.push({ x: best.x, y: FLOOR_Y, ttl: KITCHEN.floorTtl });
        d.knockCD = KITCHEN.knockCD;
        popup(s, best.x, COUNTER.y - 16, 'knocked it down! 🦴', '#ffd98c');
        burst(s, best.x, COUNTER.y, '#caa05a', 10, 2.4);
        playSound(s, 'bark');
      }
    }

    // floor snacks: eaten on contact (either pup), or swept up if they sit too long
    const dogs = [s.dogs.cheddar, s.dogs.cocoa];
    for (let i = d.floor.length - 1; i >= 0; i--) {
      const f = d.floor[i]!;
      f.ttl -= dt;
      const eater = dogs.find((g) => g.mode === 'free' && Math.hypot(g.x - f.x, g.y - f.y) <= KITCHEN.eatR);
      if (eater) {
        d.floor.splice(i, 1);
        d.eaten++;
        addCombined(s, KITCHEN.foodScore);
        burst(s, f.x, f.y, '#f4d3a4', 12, 2.6);
        popup(s, f.x, f.y - 22, 'om nom! 😋', '#9effa0');
        playSound(s, 'yip');
      } else if (f.ttl <= 0) {
        d.floor.splice(i, 1);
        popup(s, f.x, f.y - 18, 'swept up! 🧹', '#ff9d7a');
      }
    }

    setProgress(s, 0, d.eaten / KITCHEN.target);
    if (d.eaten >= KITCHEN.target && !m.objectives[0]!.done) completeObjective(s, 0);
  },

  coopAi(s: GameState): [number, number] {
    const m = s.mission;
    const d = s.dogs[s.aiId];
    if (!m) return [0, 0];
    const data = m.data as KitchenData;
    if (d.id === 'cheddar') {
      // the knocker: go under the nearest counter snack and chair-leap to knock it down
      let best: { x: number; knocked: boolean } | null = null;
      let bd = 1e9;
      for (const f of data.onCounter) {
        if (f.knocked) continue;
        const dd = Math.abs(d.x - f.x);
        if (dd < bd) {
          bd = dd;
          best = f;
        }
      }
      if (!best) return data.floor[0] ? [data.floor[0].x - d.x, data.floor[0].y - d.y] : [0, 0];
      const belowY = COUNTER.y + 52;
      if (Math.abs(d.x - best.x) < 26 && Math.abs(d.y - belowY) < 36 && d.jumpT <= 0) tryJump(s, d);
      return [best.x - d.x, belowY - d.y];
    }
    // the eater: gobble the nearest fallen snack, else wait under the counter for drops
    let f: FloorFood | null = null;
    let bd = 1e9;
    for (const ff of data.floor) {
      const dd = Math.hypot(d.x - ff.x, d.y - ff.y);
      if (dd < bd) {
        bd = dd;
        f = ff;
      }
    }
    if (f) return [f.x - d.x, f.y - d.y];
    return [(COUNTER.x0 + COUNTER.x1) / 2 - d.x, COUNTER.y + 130 - d.y];
  },

  drawWorld(g: G, s: GameState): void {
    const m = s.mission;
    if (!m) return;
    const d = m.data as KitchenData;

    // counter
    g.save();
    g.fillStyle = '#caa884';
    g.fillRect(COUNTER.x0, COUNTER.y - 12, COUNTER.x1 - COUNTER.x0, 16);
    g.fillStyle = '#9c8260';
    g.fillRect(COUNTER.x0, COUNTER.y + 4, COUNTER.x1 - COUNTER.x0, 10);
    g.fillStyle = '#7d6749';
    g.fillRect(COUNTER.x0 + 10, COUNTER.y + 14, 14, 70);
    g.fillRect(COUNTER.x1 - 24, COUNTER.y + 14, 14, 70);
    g.restore();

    // snacks still on the counter
    for (const f of d.onCounter) {
      if (f.knocked) continue;
      drawSnack(g, f.x, COUNTER.y - 20, 1);
    }
    // fallen snacks (blink as they're about to be swept)
    for (const f of d.floor) {
      const blink = f.ttl < 1.6 && Math.sin(s.elapsedMs * 0.02) < 0 ? 0.4 : 1;
      drawSnack(g, f.x, f.y, blink);
    }

    // little asymmetry hint
    g.fillStyle = 'rgba(255,255,255,.5)';
    g.textAlign = 'center';
    g.font = '700 12px -apple-system, sans-serif';
    g.fillText('Cheddar: JUMP by the counter to knock snacks down', (COUNTER.x0 + COUNTER.x1) / 2, COUNTER.y - 42);
  },
};

function drawSnack(g: G, x: number, y: number, alpha: number): void {
  g.save();
  g.globalAlpha = alpha;
  g.fillStyle = '#caa05a';
  g.beginPath();
  g.arc(x, y, 9, 0, 7);
  g.fill();
  g.fillStyle = '#7a5a2e';
  for (const [dx, dy] of [[-3, -2], [3, -1], [0, 3]] as const) {
    g.beginPath();
    g.arc(x + dx, y + dy, 1.6, 0, 7);
    g.fill();
  }
  g.restore();
}
