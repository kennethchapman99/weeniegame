/**
 * scenes/missions/hawk.ts — "Stay Together", an M13 co-op SURVIVE mission.
 *
 * A hawk circles overhead and dives at whichever pup has strayed from its sibling. Huddle —
 * stay within `huddleR` of each other — and the hawk won't risk it. Drift apart and it commits
 * a dive at the lone pup; if it connects, the pup is carried off and the mission fails. The
 * targeted pup can JUMP to dodge a dive. Last the full time together to win.
 *
 * Interdependence: safety is literally "stick together" — neither pup is safe alone, and one
 * can't huddle by itself. The co-op heart of the whole game, distilled into a survival beat.
 */

import type { SceneDef } from '../types.js';
import type { GameState } from '../../state/gameState.js';
import { playSound } from '../../state/gameState.js';
import type { Dog } from '../../state/dog.js';
import { paintYard } from '../yard.js';
import { HAWK } from '../../config/balance.js';
import { startMission, setProgress, objective } from '../../systems/mission.js';
import { burst, popup } from '../../systems/particles.js';

type G = CanvasRenderingContext2D;

type HawkState = 'circle' | 'dive' | 'retreat';
interface HawkData {
  x: number;
  y: number;
  state: HawkState;
  ang: number;
  t: number; // state timer
  diveIn: number; // countdown to next dive while circling
  targetId: 'cheddar' | 'cocoa';
}

export const hawkMission: SceneDef = {
  config: {
    key: 'mission-hawk',
    name: 'Stay Together',
    sub: 'Co-op — huddle close so the hawk can’t pick off a lone pup. Survive!',
    time: 45,
  },
  bgKey: () => 'yard',
  paint: () => paintYard,
  visibleDogs: (s) => [s.dogs.cheddar, s.dogs.cocoa],

  enter(s: GameState): void {
    s.dogs.cheddar.x = 440;
    s.dogs.cheddar.y = 430;
    s.dogs.cocoa.x = 520;
    s.dogs.cocoa.y = 430;
    for (const id of ['cheddar', 'cocoa'] as const) {
      s.dogs[id].mode = 'free';
      s.dogs[id].room = '';
    }
    const data: HawkData = { x: 480, y: 120, state: 'circle', ang: 0, t: 0, diveIn: 2.5, targetId: 'cheddar' };
    startMission(s, {
      key: 'mission-hawk',
      title: 'Stay Together',
      objectives: [objective('survive', 'Survive the hawk (45s)', 0)],
      timeLimit: 45,
      surviveMode: true,
      data,
    });
  },

  update(s: GameState, dt: number): void {
    const m = s.mission;
    if (!m || m.status !== 'active') return;
    const h = m.data as HawkData;
    const a = s.dogs.cheddar;
    const b = s.dogs.cocoa;
    const apart = Math.hypot(a.x - b.x, a.y - b.y);
    const huddled = apart < HAWK.huddleR;

    setProgress(s, 0, Math.min(0.999, m.elapsed / m.timeLimit)); // tickMission finishes it at 100%

    if (h.state === 'circle') {
      h.ang += HAWK.circleSpeed * dt;
      h.x = 480 + Math.cos(h.ang) * HAWK.circleR;
      h.y = 150 + Math.sin(h.ang) * HAWK.circleR * 0.5;
      h.diveIn -= dt;
      if (h.diveIn <= 0 && !huddled) {
        // commit: target the pup farther from its sibling
        h.targetId = distToOther(s, a) > distToOther(s, b) ? 'cheddar' : 'cocoa';
        h.state = 'dive';
        h.t = 0;
        s.toast = '🦅 The hawk dives — huddle!';
        playSound(s, 'screech');
      } else if (h.diveIn <= 0) {
        h.diveIn = s.rng.range(HAWK.diveEvery[0], HAWK.diveEvery[1]); // they're huddled — bide time
      }
    } else if (h.state === 'dive') {
      h.t += dt;
      const tg = s.dogs[h.targetId];
      const dx = tg.x - h.x;
      const dy = tg.y - h.y;
      const mlen = Math.hypot(dx, dy) || 1;
      h.x += (dx / mlen) * HAWK.diveSpeed;
      h.y += (dy / mlen) * HAWK.diveSpeed;
      const close = Math.hypot(tg.x - h.x, tg.y - h.y);
      if (close < HAWK.grabR) {
        if (tg.jumpT > 0 || huddled) {
          // dodged (mid-jump) or the pups closed ranks just in time — the hawk peels off
          popup(s, tg.x, tg.y - 40, huddled ? 'too close to grab!' : 'dodged!', '#9effa0');
          h.state = 'retreat';
          h.t = 0;
        } else {
          // caught a lone pup → carried off → mission failed
          m.status = 'fail';
          s.carriedDog = tg.id;
          burst(s, tg.x, tg.y, '#caa05a', 20, 3.2);
          popup(s, tg.x, tg.y - 40, 'got caught! 🦅', '#ff5a5a');
          playSound(s, 'screech');
        }
      } else if (h.t > 2.2) {
        h.state = 'retreat'; // overshot
        h.t = 0;
      }
    } else {
      // retreat: climb back up, then resume circling
      h.t += dt;
      h.y -= 60 * dt;
      if (h.t > HAWK.retreat) {
        h.state = 'circle';
        h.diveIn = s.rng.range(HAWK.diveEvery[0], HAWK.diveEvery[1]);
      }
    }
  },

  coopAi(s: GameState): [number, number] {
    // the partner sticks to the player to keep the huddle tight
    const d = s.dogs[s.aiId];
    const p = s.dogs[s.playerId];
    return [p.x - d.x, p.y - d.y];
  },

  drawWorld(g: G, s: GameState): void {
    const m = s.mission;
    if (!m) return;
    const h = m.data as HawkData;
    const a = s.dogs.cheddar;
    const b = s.dogs.cocoa;
    const huddled = Math.hypot(a.x - b.x, a.y - b.y) < HAWK.huddleR;

    // huddle ring between the pups (green when safe, amber when exposed)
    const mx = (a.x + b.x) / 2;
    const my = (a.y + b.y) / 2;
    g.save();
    g.strokeStyle = huddled ? 'rgba(126,208,126,.5)' : 'rgba(255,160,90,.45)';
    g.lineWidth = 3;
    g.setLineDash([7, 7]);
    g.beginPath();
    g.arc(mx, my, HAWK.huddleR / 2, 0, 7);
    g.stroke();
    g.setLineDash([]);
    g.restore();

    // shadow under the hawk (telegraphs a dive)
    g.fillStyle = 'rgba(0,0,0,.18)';
    g.beginPath();
    g.ellipse(h.x, Math.min(560, h.y + 240), 22, 8, 0, 0, 7);
    g.fill();

    // the hawk
    g.save();
    g.translate(h.x, h.y);
    const diving = h.state === 'dive';
    g.fillStyle = diving ? '#6b4a2e' : '#7a5638';
    g.beginPath();
    g.ellipse(0, 0, 16, 9, 0, 0, 7); // body
    g.fill();
    const flap = Math.sin(s.elapsedMs * 0.02) * (diving ? 6 : 12);
    g.beginPath(); // wings
    g.moveTo(0, 0);
    g.lineTo(-30, -flap);
    g.lineTo(-6, 4);
    g.moveTo(0, 0);
    g.lineTo(30, -flap);
    g.lineTo(6, 4);
    g.fill();
    g.fillStyle = '#caa05a';
    g.beginPath();
    g.moveTo(16, 0);
    g.lineTo(24, -2);
    g.lineTo(16, 3);
    g.fill(); // beak
    g.restore();
  },
};

function distToOther(s: GameState, d: Dog): number {
  const o = d.id === 'cheddar' ? s.dogs.cocoa : s.dogs.cheddar;
  return Math.hypot(d.x - o.x, d.y - o.y);
}
