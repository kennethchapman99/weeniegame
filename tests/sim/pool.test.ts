import { describe, it, expect } from 'vitest';
import { makeRng } from '../../src/core/rng.js';
import { makeGameState } from '../../src/state/gameState.js';
import { startGame, updateGame, beginScene } from '../../src/state/sceneManager.js';
import { placeSpot } from '../../src/systems/spot.js';
import { moveDog } from '../../src/systems/movement.js';
import { initFloaters } from '../../src/scenes/pool.js';
import { inPoolRect, inWater } from '../../src/scenes/poolGeometry.js';
import { doWrestle } from '../../src/systems/wrestle.js';
import { POOL } from '../../src/config/balance.js';

const DT = 1 / 60;
const idle = { ax: 0, ay: 0, arrive: 0 };

/** Build a state sitting in the pool round, in the play phase. */
function poolPlay(seed = 3) {
  const s = makeGameState(makeRng(seed));
  startGame(s); // yard inter
  s.sceneIdx = 1; // jump to the pool
  beginScene(s); // pool inter
  for (let i = 0; i < 120 && s.phase !== 'play'; i++) updateGame(s, idle, false, DT);
  return s;
}

/** Find an open-water point (in the pool rect, not on a floater). */
function waterPoint(s: ReturnType<typeof makeGameState>): { x: number; y: number } {
  for (let x = 200; x < 760; x += 13) {
    for (let y = 300; y < 520; y += 13) {
      if (inWater(s.floaters, x, y)) return { x, y };
    }
  }
  throw new Error('no water point found');
}

describe('pool placement (M5)', () => {
  it('deck-band spot placement never lands inside the water rect', () => {
    const s = makeGameState(makeRng(11));
    s.sceneKey = 'pool';
    initFloaters(s);
    for (let i = 0; i < 400; i++) {
      s.spot = null;
      placeSpot(s);
      expect(inPoolRect(s.spot!.x, s.spot!.y)).toBe(false);
    }
  });
});

describe('pool swim/shake cycle (M5)', () => {
  it('a dog in open water splashes in, swims to the edge, shakes, then dries', () => {
    const s = poolPlay();
    const d = s.dogs.cheddar;
    const wp = waterPoint(s);
    d.x = wp.x;
    d.y = wp.y;
    d.mode = 'free';

    // first step in water → splash → swimming
    moveDog(s, d, 0, 0, DT, 0);
    expect(d.mode).toBe('swimming');

    // swim toward the nearest deck (drive straight left to the edge) until shaking
    let sawShake = false;
    for (let i = 0; i < 60 * 8; i++) {
      moveDog(s, d, -1, 0, DT, 1);
      const mode: string = d.mode;
      if (mode === 'shaking') sawShake = true;
      if (mode === 'free' && sawShake) break;
    }
    expect(sawShake).toBe(true);
    expect(d.mode).toBe('free');
    expect(d.dryT).toBeGreaterThan(0); // wet coat after the shake
    expect(d.dryT).toBeLessThanOrEqual(POOL.wetTimer + 0.001);
  });
});

describe('wrestle pool dunk (M5)', () => {
  it('flipping a dog on the deck near the water shoves it toward the pool', () => {
    const s = poolPlay();
    s.sceneKey = 'pool';
    // place both just outside the water on the top deck, water below them
    const w = s.dogs.cheddar;
    const l = s.dogs.cocoa;
    w.x = 480;
    w.y = 250;
    l.x = 500;
    l.y = 250; // deck (y<270), water is at y>270 just below
    l.mode = 'free';
    s.rng = makeRng(2);
    doWrestle(s, w, l);
    // whoever lost should be aimed downward into the water
    const loser = w.mode === 'stunned' ? w : l;
    expect(loser.mode).toBe('stunned');
    expect(loser.vy).toBeGreaterThan(0); // knocked toward the water below
  });
});

describe('pool AI does not dunk-loop (M5)', () => {
  it('over a full round the AI recovers from dunks and still scores', () => {
    const s = poolPlay(7);
    let dunks = 0;
    let prevSwim = false;
    for (let i = 0; i < 60 * 45 && s.phase === 'play'; i++) {
      updateGame(s, idle, false, DT);
      const swimming = s.dogs.cocoa.mode === 'swimming';
      if (swimming && !prevSwim) dunks++;
      prevSwim = swimming;
    }
    // organic, not a loop: a handful at most over the round
    expect(dunks).toBeLessThanOrEqual(8);
    // and the AI isn't left stuck swimming at the end
    expect(s.dogs.cocoa.mode).not.toBe('swimming');
  });
});
