/**
 * scenes/missions/sneak.ts — "Sneak the Snack", an M13 co-op mission built on the distract+grab
 * primitive (systems/gates.ts `isDistracted`).
 *
 * A grumpy cat guards a stash of treats. While ONE pup gets in the cat's face (within distractR),
 * it fixates on the taunt and the OTHER pup can sneak a treat. Grab a treat with no teammate
 * distracting the cat and it lunges — stun + knockback, no treat. Grab all three to win.
 *
 * Interdependence: a treat is only claimable by a dog while its *teammate* is distracting the
 * guard — one dog alone can't both taunt and grab.
 */

import type { SceneDef } from '../types.js';
import type { GameState } from '../../state/gameState.js';
import { playSound } from '../../state/gameState.js';
import type { Dog } from '../../state/dog.js';
import { paintYard } from '../yard.js';
import { GATES, SNEAK } from '../../config/balance.js';
import { isDistracted } from '../../systems/gates.js';
import { startMission, setProgress, addCombined, objective } from '../../systems/mission.js';
import { burst, popup } from '../../systems/particles.js';

type G = CanvasRenderingContext2D;

interface Treat {
  x: number;
  y: number;
  got: boolean;
}
interface SneakData {
  guard: { x: number; y: number; face: 1 | -1; lunge: number };
  treats: Treat[];
}

const GUARD = { x: 740, y: 300 };

export const sneakMission: SceneDef = {
  config: {
    key: 'mission-sneak',
    name: 'Sneak the Snack',
    sub: 'Co-op — one pup distracts the cat while the other grabs the treats.',
    time: 80,
  },
  bgKey: () => 'yard',
  paint: () => paintYard,
  visibleDogs: (s) => [s.dogs.cheddar, s.dogs.cocoa],

  enter(s: GameState): void {
    s.dogs.cheddar.x = 160;
    s.dogs.cheddar.y = 360;
    s.dogs.cocoa.x = 160;
    s.dogs.cocoa.y = 470;
    for (const id of ['cheddar', 'cocoa'] as const) {
      s.dogs[id].mode = 'free';
      s.dogs[id].room = '';
    }
    const data: SneakData = {
      guard: { x: GUARD.x, y: GUARD.y, face: -1, lunge: 0 },
      treats: [
        { x: 660, y: 360, got: false },
        { x: 820, y: 370, got: false },
        { x: 740, y: 450, got: false },
      ],
    };
    startMission(s, {
      key: 'mission-sneak',
      title: 'Sneak the Snack',
      objectives: [objective('collectTogether', 'Sneak all 3 treats past the cat', 0, 3)],
      timeLimit: 80,
      starTime: [25, 45],
      data,
    });
  },

  update(s: GameState): void {
    const m = s.mission;
    if (!m || m.status !== 'active') return;
    const d = m.data as SneakData;
    const dogs = [s.dogs.cheddar, s.dogs.cocoa];

    if (d.guard.lunge > 0) d.guard.lunge -= 1 / 60;
    // the cat faces the nearest dog (flavour)
    const near = dogs.reduce((a, b) =>
      Math.hypot(b.x - d.guard.x, b.y - d.guard.y) < Math.hypot(a.x - d.guard.x, a.y - d.guard.y) ? b : a,
    );
    d.guard.face = near.x < d.guard.x ? -1 : 1;

    for (const t of d.treats) {
      if (t.got) continue;
      const grabber = dogs.find((g) => g.mode === 'free' && Math.hypot(g.x - t.x, g.y - t.y) <= SNEAK.grabR);
      if (!grabber) continue;
      // is a *teammate* (not the grabber) keeping the cat busy?
      const distractor = dogs.filter((g) => g !== grabber);
      if (isDistracted({ x: d.guard.x, y: d.guard.y }, distractor, GATES.distractR)) {
        t.got = true;
        addCombined(s, SNEAK.treatScore);
        burst(s, t.x, t.y, '#ffd98c', 12, 2.6);
        popup(s, t.x, t.y - 24, 'sneaky! 🍪', '#9effa0');
        playSound(s, 'yip');
      } else if (d.guard.lunge <= 0 && Math.hypot(grabber.vx, grabber.vy) < SNEAK.settleSpeed) {
        // only pounce on a dog SETTLED on the treat — a pup just running past is safe
        guardLunge(s, d.guard, grabber);
      }
    }

    const got = d.treats.filter((t) => t.got).length;
    setProgress(s, 0, got / d.treats.length);
  },

  coopAi(s: GameState): [number, number] {
    const m = s.mission;
    const d = s.dogs[s.aiId];
    if (!m) return [0, 0];
    const data = m.data as SneakData;
    // park on the cat to keep it distracted, so the player can sneak the treats
    return [data.guard.x - d.x, data.guard.y - d.y];
  },

  drawWorld(g: G, s: GameState): void {
    const m = s.mission;
    if (!m) return;
    const d = m.data as SneakData;

    // treats
    for (const t of d.treats) {
      if (t.got) continue;
      g.save();
      g.fillStyle = '#caa05a';
      g.beginPath();
      g.arc(t.x, t.y, 9, 0, 7);
      g.fill();
      g.fillStyle = '#7a5a2e';
      for (const [dx, dy] of [[-3, -2], [3, -1], [0, 3]] as const) {
        g.beginPath();
        g.arc(t.x + dx, t.y + dy, 1.6, 0, 7);
        g.fill();
      }
      g.restore();
    }

    // the guard cat
    const cat = d.guard;
    g.save();
    g.translate(cat.x, cat.y);
    g.scale(cat.face, 1);
    const alarmed = cat.lunge > 0;
    g.fillStyle = alarmed ? '#7a7a82' : '#8a8a92';
    g.beginPath();
    g.ellipse(0, 6, 30, 20, 0, 0, 7); // body
    g.fill();
    g.beginPath();
    g.arc(20, -8, 15, 0, 7); // head
    g.fill();
    g.beginPath(); // ears
    g.moveTo(10, -18);
    g.lineTo(14, -32);
    g.lineTo(20, -20);
    g.moveTo(24, -20);
    g.lineTo(30, -32);
    g.lineTo(32, -18);
    g.fill();
    g.fillStyle = alarmed ? '#ffe24a' : '#222';
    g.beginPath();
    g.arc(22, -9, alarmed ? 3 : 2, 0, 7); // eye
    g.fill();
    g.strokeStyle = '#8a8a92'; // tail
    g.lineWidth = 6;
    g.lineCap = 'round';
    g.beginPath();
    g.moveTo(-26, 4);
    g.quadraticCurveTo(-44, -2, -38, -20);
    g.stroke();
    g.restore();
    // status bubble
    g.fillStyle = '#fff';
    g.textAlign = 'center';
    g.font = '700 16px -apple-system, sans-serif';
    g.fillText(alarmed ? '!' : 'z', cat.x + 6, cat.y - 30);
  },
};

function guardLunge(s: GameState, guard: { x: number; y: number; lunge: number }, dog: Dog): void {
  guard.lunge = SNEAK.lungeCD;
  dog.mode = 'stunned';
  dog.stunT = SNEAK.lungeStun;
  const dx = dog.x - guard.x || 1;
  const dy = dog.y - guard.y || 0.5;
  const mlen = Math.hypot(dx, dy) || 1;
  dog.vx = (dx / mlen) * SNEAK.lungeKnockback;
  dog.vy = (dy / mlen) * SNEAK.lungeKnockback;
  popup(s, dog.x, dog.y - 30, 'HSSSS! 🐱', '#ff9d7a');
  playSound(s, 'screech');
}
