import { describe, it, expect } from 'vitest';
import { makeRng } from '../../src/core/rng.js';
import { makeGameState } from '../../src/state/gameState.js';
import { startGame, updateGame } from '../../src/state/sceneManager.js';
import { aiThink } from '../../src/ai/sibling.js';

const DT = 1 / 60;
const noIntent = { ax: 0, ay: 0, arrive: 0 };

function freshPlay(seed = 99) {
  const s = makeGameState(makeRng(seed));
  startGame(s);
  for (let i = 0; i < 120 && s.phase !== 'play'; i++) updateGame(s, noIntent, DT);
  return s;
}

describe('AI sibling (M3)', () => {
  it('steers toward the nearest toy', () => {
    const s = freshPlay();
    const d = s.dogs.cocoa;
    d.x = 480;
    d.y = 400;
    s.toys = [{ x: 800, y: 400, room: '', fl: -1, ox: 0, oy: 0, tug: false, type: 'ball', t: 0, scale: 1 }];
    s.spot = null;
    const [ax] = aiThink(s, d, DT);
    expect(ax).toBeGreaterThan(0); // toward the toy on the right
  });

  it('contests the cuddle spot when no toy is closer, and can score it unattended', () => {
    const s = freshPlay();
    s.toys = []; // remove toys so the spot is the target
    s.spot = { x: 500, y: 400, r: 52, holder: null, prog: 0, pulse: 0 };
    const ai = s.dogs.cocoa;
    ai.x = 470;
    ai.y = 400;
    // park the player far away and out of the spot
    s.dogs.cheddar.x = 100;
    s.dogs.cheddar.y = 540;
    const before = ai.score;
    // suppress fresh toy spawns from changing the AI target mid-test
    for (let i = 0; i < 60 * 6; i++) {
      s.toys = [];
      updateGame(s, noIntent, DT);
    }
    expect(ai.score).toBeGreaterThan(before); // the AI reached and held the spot
  });

  it('AI moves at full speed (0.88 factor is inert — matches prototype)', () => {
    const s = freshPlay();
    const ai = s.dogs.cocoa;
    ai.x = 120;
    ai.y = 400;
    s.toys = [{ x: 900, y: 400, room: '', fl: -1, ox: 0, oy: 0, tug: false, type: 'ball', t: 0, scale: 1 }];
    s.spot = null;
    let peak = 0;
    for (let i = 0; i < 60; i++) {
      s.toys = [{ x: 900, y: 400, room: '', fl: -1, ox: 0, oy: 0, tug: false, type: 'ball', t: 0, scale: 1 }];
      const px = ai.x;
      updateGame(s, noIntent, DT);
      peak = Math.max(peak, ai.x - px);
    }
    // full free speed ~4.4 px/step; if 0.88 were applied it would cap ~3.9.
    expect(peak).toBeGreaterThan(4.0);
  });

  it('falls back to wander when there is no toy or spot', () => {
    const s = freshPlay();
    const d = s.dogs.cocoa;
    s.toys = [];
    s.spot = null;
    d.aiWanderT = 0; // force a wander reroll
    aiThink(s, d, DT);
    // a wander target inside bounds was chosen
    expect(d.aiTx).toBeGreaterThanOrEqual(50);
    expect(d.aiTx).toBeLessThanOrEqual(910);
  });
});
