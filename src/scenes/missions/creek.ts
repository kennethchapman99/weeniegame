/**
 * scenes/missions/creek.ts — "Over the Creek", an M13 co-op mission built on the boost-jump
 * primitive (systems/gates.ts `tryMissionBoost` / `boostLaunch`).
 *
 * A creek walls off the den. One pup braces on the boost pad on the near bank; the other runs up
 * and JUMPS off it — launched clear across the water (a normal jump can't make it). The launched
 * pup lands on the far bank and steps on a pad that drops a log bridge, so the bracing pup can
 * walk across too. Then both reach the den.
 *
 * Interdependence: the first pup can only cross by being boosted (needs the bracer); the second
 * can only cross once the first drops the bridge from the far side.
 */

import type { SceneDef } from '../types.js';
import type { GameState } from '../../state/gameState.js';
import { paintYard } from '../yard.js';
import { GATES } from '../../config/balance.js';
import { updatePads } from '../../systems/gates.js';
import { startMission, completeObjective, setProgress, objective } from '../../systems/mission.js';
import { burst, popup } from '../../systems/particles.js';

type G = CanvasRenderingContext2D;

const CREEK = { cx: 480, hw: 70, y0: 230, y1: 565 };
const GAP = { y0: 360, y1: 448 }; // where the bridge spans
const BOOST_PAD = { x: 372, y: 405 };
const BOOST_TARGET = { x: 620, y: 405 };
const FAR_PAD = { x: 664, y: 300 };
const DEN = { x: 842, y: 470 };
const MARGIN = 24;

interface CreekData {
  bridge: { down: boolean; prog: number };
}

export const creekMission: SceneDef = {
  config: {
    key: 'mission-creek',
    name: 'Over the Creek',
    sub: 'Co-op — boost a pup across, drop the bridge, both reach the den.',
    time: 95,
  },
  bgKey: () => 'yard',
  paint: () => paintYard,
  visibleDogs: (s) => [s.dogs.cheddar, s.dogs.cocoa],

  enter(s: GameState): void {
    s.dogs.cheddar.x = 200;
    s.dogs.cheddar.y = 360;
    s.dogs.cocoa.x = 200;
    s.dogs.cocoa.y = 450;
    for (const id of ['cheddar', 'cocoa'] as const) {
      s.dogs[id].mode = 'free';
      s.dogs[id].room = '';
    }
    startMission(s, {
      key: 'mission-creek',
      title: 'Over the Creek',
      objectives: [
        objective('reachTogether', 'Boost a pup across the creek', 4),
        objective('gate', 'Drop the log bridge', 4),
        objective('reachTogether', 'Get both pups to the den', 10),
      ],
      timeLimit: 95,
      starTime: [28, 50],
      pads: [{ x: FAR_PAD.x, y: FAR_PAD.y, r: GATES.padR, on: false, by: null }],
      goal: { x: DEN.x, y: DEN.y, r: GATES.goalR, cheddar: false, cocoa: false },
      boostPad: { x: BOOST_PAD.x, y: BOOST_PAD.y },
      boostTarget: { x: BOOST_TARGET.x, y: BOOST_TARGET.y },
      data: { bridge: { down: false, prog: 0 } } as CreekData,
    });
  },

  update(s: GameState, dt: number): void {
    const m = s.mission;
    if (!m || m.status !== 'active') return;
    const d = m.data as CreekData;
    const dogs = [s.dogs.cheddar, s.dogs.cocoa];
    const rightOf = CREEK.cx + CREEK.hw;

    // 0) a pup made it across (grounded on the far bank)
    if (!m.objectives[0]!.done) {
      const crossed = dogs.some((g) => g.jumpT <= 0 && g.x > rightOf);
      setProgress(s, 0, crossed ? 1 : 0);
      if (crossed) {
        completeObjective(s, 0);
        popup(s, rightOf + 40, CREEK.y0 + 40, 'across! 🐾', '#9effa0');
      }
    }

    // 1) the far pad drops the bridge (only reachable from the far bank)
    updatePads(m.pads, dogs);
    if (!m.objectives[1]!.done && m.pads[0]!.on) {
      d.bridge.down = true;
      completeObjective(s, 1);
      popup(s, CREEK.cx, GAP.y0 - 14, 'BRIDGE DOWN! 🌉', '#ffd98c');
      burst(s, CREEK.cx, (GAP.y0 + GAP.y1) / 2, '#caa05a', 16, 2.8);
    }
    if (d.bridge.down && d.bridge.prog < 1) d.bridge.prog = Math.min(1, d.bridge.prog + dt * 3);

    // the creek is impassable on foot — airborne pups fly over; grounded ones use the bridge
    for (const g of dogs) {
      if (g.jumpT > 0) continue; // mid-launch: sailing over
      if (g.x > CREEK.cx - CREEK.hw && g.x < rightOf) {
        const onBridge = d.bridge.down && g.y > GAP.y0 && g.y < GAP.y1;
        if (onBridge) continue;
        if (g.x < CREEK.cx) {
          g.x = CREEK.cx - CREEK.hw - MARGIN;
          if (g.vx > 0) g.vx = 0;
        } else {
          g.x = rightOf + MARGIN;
          if (g.vx < 0) g.vx = 0;
        }
      }
    }

    // 2) both pups in the den
    if (m.goal) {
      m.goal.cheddar = dist(s.dogs.cheddar, DEN) < m.goal.r;
      m.goal.cocoa = dist(s.dogs.cocoa, DEN) < m.goal.r;
      const both = m.goal.cheddar && m.goal.cocoa;
      setProgress(s, 2, both ? 1 : (Number(m.goal.cheddar) + Number(m.goal.cocoa)) / 2);
      if (both && !m.objectives[2]!.done) completeObjective(s, 2);
    }
  },

  drawWorld(g: G, s: GameState): void {
    const m = s.mission;
    if (!m) return;
    const d = m.data as CreekData;

    // creek water
    g.save();
    const wg = g.createLinearGradient(0, CREEK.y0, 0, CREEK.y1);
    wg.addColorStop(0, '#5fa8d6');
    wg.addColorStop(1, '#3f7fb0');
    g.fillStyle = wg;
    g.fillRect(CREEK.cx - CREEK.hw, CREEK.y0, CREEK.hw * 2, CREEK.y1 - CREEK.y0);
    g.strokeStyle = 'rgba(255,255,255,.25)';
    g.lineWidth = 2;
    for (let y = CREEK.y0 + 18; y < CREEK.y1; y += 34) {
      g.beginPath();
      g.moveTo(CREEK.cx - CREEK.hw + 8, y + Math.sin(s.elapsedMs * 0.004 + y) * 3);
      g.lineTo(CREEK.cx + CREEK.hw - 8, y + Math.cos(s.elapsedMs * 0.004 + y) * 3);
      g.stroke();
    }
    g.restore();

    // bridge planks across the gap (slide in as prog → 1)
    if (d.bridge.down) {
      g.save();
      g.globalAlpha = d.bridge.prog;
      g.fillStyle = '#9b6b3e';
      for (let x = CREEK.cx - CREEK.hw; x < CREEK.cx + CREEK.hw; x += 16) {
        g.fillRect(x + 2, GAP.y0, 12, GAP.y1 - GAP.y0);
      }
      g.strokeStyle = 'rgba(60,40,20,.5)';
      g.lineWidth = 3;
      g.strokeRect(CREEK.cx - CREEK.hw, GAP.y0, CREEK.hw * 2, GAP.y1 - GAP.y0);
      g.restore();
    }

    // den / goal
    if (m.goal) {
      const done = m.goal.cheddar && m.goal.cocoa;
      g.save();
      g.fillStyle = done ? 'rgba(120,200,120,.30)' : 'rgba(244,211,164,.22)';
      g.beginPath();
      g.arc(m.goal.x, m.goal.y, m.goal.r, 0, 7);
      g.fill();
      g.strokeStyle = done ? '#7ed07e' : '#f4d3a4';
      g.lineWidth = 3;
      g.setLineDash([8, 6]);
      g.stroke();
      g.setLineDash([]);
      g.fillStyle = '#fff';
      g.textAlign = 'center';
      g.font = '800 13px -apple-system, sans-serif';
      g.fillText('🏠 DEN', m.goal.x, m.goal.y + 4);
      g.restore();
    }

    // boost pad (springboard)
    g.save();
    g.fillStyle = '#e08a3c';
    g.beginPath();
    g.ellipse(BOOST_PAD.x, BOOST_PAD.y, 34, 18, 0, 0, 7);
    g.fill();
    g.strokeStyle = '#a85f20';
    g.lineWidth = 3;
    g.stroke();
    g.fillStyle = '#fff';
    g.textAlign = 'center';
    g.font = '800 18px -apple-system, sans-serif';
    g.fillText('⤴', BOOST_PAD.x, BOOST_PAD.y + 6);
    g.restore();

    // far pad (bridge trigger)
    const fp = m.pads[0]!;
    g.save();
    g.fillStyle = fp.on ? 'rgba(126,208,126,.5)' : 'rgba(200,160,90,.35)';
    g.beginPath();
    g.arc(fp.x, fp.y, fp.r, 0, 7);
    g.fill();
    g.strokeStyle = fp.on ? '#7ed07e' : '#caa05a';
    g.lineWidth = 3;
    g.beginPath();
    g.arc(fp.x, fp.y, fp.r, 0, 7);
    g.stroke();
    g.fillStyle = '#fff';
    g.font = '700 14px -apple-system, sans-serif';
    g.fillText(fp.on ? '✓' : '🌉', fp.x, fp.y + 5);
    g.restore();
  },
};

function dist(d: { x: number; y: number }, p: { x: number; y: number }): number {
  return Math.hypot(d.x - p.x, d.y - p.y);
}
