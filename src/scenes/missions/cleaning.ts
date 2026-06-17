/**
 * scenes/missions/cleaning.ts — "The Cleaning Ladies Are Here", an M13 co-op stealth mission
 * built on the distract+grab primitive (systems/gates.ts `isDistracted`) plus a carry/escort verb.
 *
 * It's cleaning day, so the house is enemy territory: a vacuum patrols back and forth, hoovering
 * up anything in the open — including the favourite toy. One pup has to get in the vacuum's face
 * (within distractR) so it fixates and creeps after the decoy; while it's busy, the OTHER pup
 * carries the toy to the safety of the dog couch. Try to carry the toy past an *un-distracted*
 * vacuum and it's "put away" — knocked from the carrier's mouth, who's stunned for the trouble.
 *
 * Interdependence: the toy can only cross the room while a *teammate* keeps the vacuum lured —
 * one pup can't both decoy the vacuum and run the toy to the couch. Distract / objective split.
 */

import type { SceneDef } from '../types.js';
import type { GameState } from '../../state/gameState.js';
import { playSound } from '../../state/gameState.js';
import type { Dog, DogId } from '../../state/dog.js';
import { paintYard } from '../yard.js';
import { GATES, CLEANING } from '../../config/balance.js';
import { isDistracted } from '../../systems/gates.js';
import { startMission, setProgress, objective } from '../../systems/mission.js';
import { burst, popup } from '../../systems/particles.js';

type G = CanvasRenderingContext2D;

interface CleanData {
  vac: { x: number; y: number; dir: 1 | -1; distracted: boolean };
  toy: { x: number; y: number; carrier: DogId | null; safe: boolean };
  couch: { x: number; y: number; r: number };
}

const COUCH = { x: 180, y: 300, r: CLEANING.couchR };
const TOY0 = { x: 680, y: 470 };
const VAC_Y = 320;

export const cleaningMission: SceneDef = {
  config: {
    key: 'mission-clean',
    name: 'The Cleaning Ladies Are Here',
    sub: 'Co-op — one pup lures the vacuum while the other carries the toy to the couch.',
    time: 70,
  },
  bgKey: () => 'yard',
  paint: () => paintYard,
  visibleDogs: (s) => [s.dogs.cheddar, s.dogs.cocoa],

  enter(s: GameState): void {
    s.dogs.cheddar.x = 240;
    s.dogs.cheddar.y = 470;
    s.dogs.cocoa.x = 240;
    s.dogs.cocoa.y = 540;
    for (const id of ['cheddar', 'cocoa'] as const) {
      s.dogs[id].mode = 'free';
      s.dogs[id].room = '';
    }
    const data: CleanData = {
      vac: { x: CLEANING.patrol[0], y: VAC_Y, dir: 1, distracted: false },
      toy: { x: TOY0.x, y: TOY0.y, carrier: null, safe: false },
      couch: { ...COUCH },
    };
    startMission(s, {
      key: 'mission-clean',
      title: 'The Cleaning Ladies Are Here',
      objectives: [objective('escort', 'Carry the toy to the dog couch', 0)],
      timeLimit: 70,
      starTime: [25, 45],
      data,
    });
  },

  update(s: GameState): void {
    const m = s.mission;
    if (!m || m.status !== 'active') return;
    const d = m.data as CleanData;
    const dogs = [s.dogs.cheddar, s.dogs.cocoa];

    // the vacuum is distracted only by a dog that ISN'T the one carrying the toy.
    const decoys = dogs.filter((g) => g.id !== d.toy.carrier);
    d.vac.distracted = isDistracted({ x: d.vac.x, y: d.vac.y }, decoys, GATES.distractR);

    if (d.vac.distracted) {
      // fixated: creep toward the nearest decoy (slow, so it stays off the carrier)
      const t = decoys.reduce((a, b) =>
        Math.hypot(b.x - d.vac.x, b.y - d.vac.y) < Math.hypot(a.x - d.vac.x, a.y - d.vac.y) ? b : a,
      );
      d.vac.x += Math.sign(t.x - d.vac.x) * CLEANING.creepSpeed;
    } else {
      // patrol sweep, bouncing at the ends of its range
      d.vac.x += d.vac.dir * CLEANING.patrolSpeed;
      if (d.vac.x <= CLEANING.patrol[0]) {
        d.vac.x = CLEANING.patrol[0];
        d.vac.dir = 1;
      } else if (d.vac.x >= CLEANING.patrol[1]) {
        d.vac.x = CLEANING.patrol[1];
        d.vac.dir = -1;
      }
    }

    // pick up the loose toy
    if (!d.toy.safe && d.toy.carrier === null) {
      const grabber = dogs.find((g) => g.mode === 'free' && Math.hypot(g.x - d.toy.x, g.y - d.toy.y) <= CLEANING.grabR);
      if (grabber) {
        d.toy.carrier = grabber.id;
        popup(s, grabber.x, grabber.y - 28, 'got the toy! 🦴', '#9effa0');
        playSound(s, 'yip');
      }
    }

    // carry it: the toy rides the carrier; deliver to the couch or lose it to the vacuum
    if (d.toy.carrier && !d.toy.safe) {
      const c = s.dogs[d.toy.carrier];
      d.toy.x = c.x;
      d.toy.y = c.y - 16;

      if (!d.vac.distracted && Math.hypot(c.x - d.vac.x, c.y - d.vac.y) <= CLEANING.catchR) {
        // caught with the toy in the open → "put away": drop it, stun the carrier
        d.toy.carrier = null;
        d.toy.x = c.x;
        d.toy.y = c.y;
        c.mode = 'stunned';
        c.stunT = CLEANING.dropStun;
        const dx = c.x - d.vac.x || 1;
        const dy = c.y - d.vac.y || 0.5;
        const ml = Math.hypot(dx, dy) || 1;
        c.vx = (dx / ml) * CLEANING.dropKnockback;
        c.vy = (dy / ml) * CLEANING.dropKnockback;
        popup(s, c.x, c.y - 30, 'put away! 🧹', '#ff9d7a');
        playSound(s, 'screech');
      } else if (Math.hypot(c.x - d.couch.x, c.y - d.couch.y) <= d.couch.r) {
        // safe on the dog couch
        d.toy.safe = true;
        d.toy.x = d.couch.x;
        d.toy.y = d.couch.y - 8;
        burst(s, d.couch.x, d.couch.y, '#9effa0', 16, 2.8);
        popup(s, d.couch.x, d.couch.y - 36, 'rescued! 🛋️', '#9effa0');
        playSound(s, 'yip');
      }
    }

    setProgress(s, 0, d.toy.safe ? 1 : 0);
  },

  coopAi(s: GameState): [number, number] {
    const m = s.mission;
    const d = s.dogs[s.aiId];
    if (!m) return [0, 0];
    const data = m.data as CleanData;
    // if the partner is carrying the toy, run it to the couch; otherwise be the decoy on the vacuum
    if (data.toy.carrier === d.id && !data.toy.safe) return [data.couch.x - d.x, data.couch.y - d.y];
    return [data.vac.x - d.x, data.vac.y - d.y];
  },

  drawWorld(g: G, s: GameState): void {
    const m = s.mission;
    if (!m) return;
    const d = m.data as CleanData;

    // the dog-couch safe zone
    g.save();
    g.strokeStyle = 'rgba(126,208,126,.5)';
    g.setLineDash([7, 7]);
    g.lineWidth = 3;
    g.beginPath();
    g.arc(d.couch.x, d.couch.y, d.couch.r, 0, 7);
    g.stroke();
    g.setLineDash([]);
    g.fillStyle = '#6b4a8a';
    g.fillRect(d.couch.x - 46, d.couch.y - 14, 92, 34); // couch base
    g.fillStyle = '#8a64ad';
    g.fillRect(d.couch.x - 46, d.couch.y - 30, 92, 18); // backrest
    g.restore();
    g.fillStyle = '#cdeccd';
    g.textAlign = 'center';
    g.font = '700 12px -apple-system, sans-serif';
    g.fillText('DOG COUCH', d.couch.x, d.couch.y + 36);

    // the toy (a bone), drawn when loose or safe (it's hidden in the carrier's mouth otherwise)
    if (!d.toy.carrier || d.toy.safe) {
      g.save();
      g.translate(d.toy.x, d.toy.y);
      g.rotate(-0.3);
      g.fillStyle = '#f3ead2';
      g.fillRect(-12, -4, 24, 8);
      for (const ex of [-12, 12] as const) {
        g.beginPath();
        g.arc(ex, -4, 5, 0, 7);
        g.arc(ex, 4, 5, 0, 7);
        g.fill();
      }
      g.restore();
    }

    // the vacuum
    const v = d.vac;
    g.save();
    g.translate(v.x, v.y);
    const alarmed = v.distracted;
    g.fillStyle = '#c9402e'; // canister
    g.beginPath();
    g.ellipse(0, 10, 26, 18, 0, 0, 7);
    g.fill();
    g.fillStyle = '#2b2b30'; // base / nozzle
    g.fillRect(-30, 24, 60, 8);
    g.strokeStyle = '#9aa0a6'; // handle
    g.lineWidth = 5;
    g.lineCap = 'round';
    g.beginPath();
    g.moveTo(10, 2);
    g.quadraticCurveTo(34, -10, 30, -36);
    g.stroke();
    g.restore();
    // status bubble
    g.fillStyle = '#fff';
    g.textAlign = 'center';
    g.font = '700 16px -apple-system, sans-serif';
    g.fillText(alarmed ? '?!' : 'VRRR', v.x, v.y - 24);
  },
};

/** exported for tests/clarity — the dog whose mouth currently holds the toy, if any. */
export function toyCarrier(s: GameState): Dog | null {
  const m = s.mission;
  if (!m || m.key !== 'mission-clean') return null;
  const id = (m.data as CleanData).toy.carrier;
  return id ? s.dogs[id] : null;
}
