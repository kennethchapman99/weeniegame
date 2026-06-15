/**
 * scenes/missions/gate.ts — "Through the Gate", the M12 minimal co-op mission.
 *
 * The canonical interdependence beat: two pressure pads on the left open a latching gate that
 * walls off the yard; one dog can't cover both pads, so the pups MUST split up to open it, then
 * regroup and both reach the den on the right. Objectives:
 *   0. Open the gate together  (both pads pressed at once → latches the gate)
 *   1. Get both pups to the den (both inside the goal zone, once the gate is open)
 *
 * Reuses the backyard backdrop; the pads/gate/den are world props drawn on top.
 */

import type { SceneDef } from '../types.js';
import type { GameState } from '../../state/gameState.js';
import { paintYard } from '../yard.js';
import { GATES } from '../../config/balance.js';
import { updatePads, allPadsPressed } from '../../systems/gates.js';
import { startMission, completeObjective, setProgress, objective } from '../../systems/mission.js';
import { burst, popup } from '../../systems/particles.js';

type G = CanvasRenderingContext2D;

const GATE_X = 520;
const GATE_Y0 = 230;
const GATE_Y1 = 565;
const GATE_MARGIN = 24; // how far left of the barrier a dog is held while it's closed

export const gateMission: SceneDef = {
  config: {
    key: 'mission-gate',
    name: 'Through the Gate',
    sub: 'Co-op — split up to open the gate, then get both pups to the den.',
    time: 90,
  },
  bgKey: () => 'yard',
  paint: () => paintYard,
  visibleDogs: (s) => [s.dogs.cheddar, s.dogs.cocoa],

  enter(s: GameState): void {
    // both pups start on the left, below the pads
    s.dogs.cheddar.x = 140;
    s.dogs.cheddar.y = 360;
    s.dogs.cocoa.x = 140;
    s.dogs.cocoa.y = 470;
    for (const id of ['cheddar', 'cocoa'] as const) {
      s.dogs[id].mode = 'free';
      s.dogs[id].room = '';
    }
    startMission(s, {
      key: 'mission-gate',
      title: 'Through the Gate',
      objectives: [
        objective('gate', 'Open the gate together', 5),
        objective('reachTogether', 'Get both pups to the den', 10),
      ],
      timeLimit: 90,
      starTime: [22, 40],
      pads: [
        { x: 250, y: 300, r: GATES.padR, on: false, by: null },
        { x: 250, y: 500, r: GATES.padR, on: false, by: null },
      ],
      gate: { x: GATE_X, y0: GATE_Y0, y1: GATE_Y1, open: false, prog: 0 },
      goal: { x: 820, y: 400, r: GATES.goalR, cheddar: false, cocoa: false },
    });
  },

  update(s: GameState, dt: number): void {
    const m = s.mission;
    if (!m || m.status !== 'active') return;
    const dogs = [s.dogs.cheddar, s.dogs.cocoa];

    // 0) pressure pads → latch the gate open (needs both dogs)
    updatePads(m.pads, dogs);
    if (m.gate && !m.gate.open) {
      // progress shows how close they are (one pad = 0.5, both = open)
      const pressed = m.pads.filter((p) => p.on).length;
      setProgress(s, 0, allPadsPressed(m.pads) ? 1 : pressed / (m.pads.length * 2));
      if (m.objectives[0]?.done) {
        m.gate.open = true;
        popup(s, m.gate.x, m.gate.y0 + 30, 'GATE OPEN! 🐾', '#ffd98c');
        burst(s, m.gate.x, (m.gate.y0 + m.gate.y1) / 2, '#f4d3a4', 18, 3);
      }
    }
    if (m.gate?.open && m.gate.prog < 1) m.gate.prog = Math.min(1, m.gate.prog + dt * GATES.gateOpenRate);

    // closed gate is a solid wall — hold dogs on the left side
    if (m.gate && !m.gate.open) {
      for (const d of dogs) {
        if (d.x > m.gate.x - GATE_MARGIN) {
          d.x = m.gate.x - GATE_MARGIN;
          if (d.vx > 0) d.vx = 0;
        }
      }
    }

    // 1) both pups in the den (only counts once the gate is open)
    if (m.goal) {
      m.goal.cheddar = dist(s.dogs.cheddar, m.goal) < m.goal.r;
      m.goal.cocoa = dist(s.dogs.cocoa, m.goal) < m.goal.r;
      const both = m.goal.cheddar && m.goal.cocoa;
      if (m.gate?.open) {
        setProgress(s, 1, both ? 1 : (Number(m.goal.cheddar) + Number(m.goal.cocoa)) / 2);
        if (both && !m.objectives[1]?.done) completeObjective(s, 1);
      }
    }
  },

  coopAi(s: GameState): [number, number] {
    const m = s.mission;
    const d = s.dogs[s.aiId];
    if (!m) return [0, 0];
    // gate shut: cover the pad the player ISN'T going for (one dog can't cover both)
    if (m.gate && !m.gate.open && m.pads.length >= 2) {
      const p = s.dogs[s.playerId];
      const near0 = Math.hypot(p.x - m.pads[0]!.x, p.y - m.pads[0]!.y);
      const near1 = Math.hypot(p.x - m.pads[1]!.x, p.y - m.pads[1]!.y);
      const target = near0 < near1 ? m.pads[1]! : m.pads[0]!;
      return [target.x - d.x, target.y - d.y];
    }
    if (m.goal) return [m.goal.x - d.x, m.goal.y - d.y]; // then regroup at the den
    return [0, 0];
  },

  drawWorld(g: G, s: GameState): void {
    const m = s.mission;
    if (!m) return;

    // den / goal zone
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

    // gate barrier (slides up as it opens)
    if (m.gate) {
      const open = m.gate.prog;
      const fullH = m.gate.y1 - m.gate.y0;
      const h = fullH * (1 - open);
      g.save();
      g.fillStyle = m.gate.open ? 'rgba(150,110,70,.45)' : '#8a6a44';
      g.fillRect(m.gate.x - 7, m.gate.y0, 14, h);
      // bars
      g.strokeStyle = 'rgba(60,40,20,.5)';
      g.lineWidth = 2;
      for (let y = m.gate.y0 + 12; y < m.gate.y0 + h; y += 22) {
        g.beginPath();
        g.moveTo(m.gate.x - 7, y);
        g.lineTo(m.gate.x + 7, y);
        g.stroke();
      }
      g.restore();
    }

    // pressure pads
    for (const p of m.pads) {
      g.save();
      g.fillStyle = p.on ? 'rgba(126,208,126,.5)' : 'rgba(200,160,90,.35)';
      g.beginPath();
      g.arc(p.x, p.y, p.r, 0, 7);
      g.fill();
      g.strokeStyle = p.on ? '#7ed07e' : '#caa05a';
      g.lineWidth = 3;
      g.beginPath();
      g.arc(p.x, p.y, p.r, 0, 7);
      g.stroke();
      g.fillStyle = '#fff';
      g.textAlign = 'center';
      g.font = '700 16px -apple-system, sans-serif';
      g.fillText(p.on ? '✓' : '◉', p.x, p.y + 5);
      g.restore();
    }

    // a faint connector reminding players the pads drive the gate
    if (m.gate && !m.gate.open) {
      g.save();
      g.strokeStyle = 'rgba(255,255,255,.12)';
      g.lineWidth = 2;
      g.setLineDash([4, 8]);
      for (const p of m.pads) {
        g.beginPath();
        g.moveTo(p.x, p.y);
        g.lineTo(m.gate.x, (m.gate.y0 + m.gate.y1) / 2);
        g.stroke();
      }
      g.setLineDash([]);
      g.restore();
    }
  },
};

function dist(d: { x: number; y: number }, p: { x: number; y: number }): number {
  return Math.hypot(d.x - p.x, d.y - p.y);
}
