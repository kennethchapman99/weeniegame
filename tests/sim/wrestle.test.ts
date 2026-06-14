import { describe, it, expect } from 'vitest';
import { makeRng } from '../../src/core/rng.js';
import { makeGameState } from '../../src/state/gameState.js';
import { startGame, updateGame } from '../../src/state/sceneManager.js';
import { doWrestle, canWrestle } from '../../src/systems/wrestle.js';
import { moveDog } from '../../src/systems/movement.js';
import { WRESTLE } from '../../src/config/balance.js';

const DT = 1 / 60;
const noIntent = { ax: 0, ay: 0, arrive: 0 };

function freshPlay(seed = 5) {
  const s = makeGameState(makeRng(seed));
  startGame(s);
  for (let i = 0; i < 120 && s.phase !== 'play'; i++) updateGame(s, noIntent, false, false, DT);
  // park both dogs, clear scene props so only wrestle is under test
  s.toys = [];
  s.spot = null;
  s.sunbeam = null;
  return s;
}

describe('wrestle (M4)', () => {
  it('a winning flip stuns the loser and knocks it back', () => {
    const s = freshPlay();
    s.dogs.cheddar.x = 480;
    s.dogs.cheddar.y = 400;
    s.dogs.cocoa.x = 520;
    s.dogs.cocoa.y = 400;
    // force a win: cheddar winChance 0.70, rng.next() must be < 0.70
    s.rng = makeRng(1); // deterministic; assert on whichever got stunned
    doWrestle(s, s.dogs.cheddar, s.dogs.cocoa);
    const stunned = s.dogs.cheddar.mode === 'stunned' ? s.dogs.cheddar : s.dogs.cocoa;
    const winner = stunned === s.dogs.cheddar ? s.dogs.cocoa : s.dogs.cheddar;
    expect(stunned.mode).toBe('stunned');
    expect(stunned.stunT).toBeCloseTo(WRESTLE.loserStun, 5);
    expect(Math.hypot(stunned.vx, stunned.vy)).toBeGreaterThan(5); // knocked back
    expect(winner.wrestleCD).toBeCloseTo(WRESTLE.cooldown, 5);
  });

  it('stun wears off after loserStun seconds and the dog returns to free', () => {
    const s = freshPlay();
    const d = s.dogs.cocoa;
    d.mode = 'stunned';
    d.stunT = WRESTLE.loserStun;
    const steps = Math.ceil(WRESTLE.loserStun / DT) + 2;
    for (let i = 0; i < steps; i++) moveDog(s, d, 0, 0, DT, 0);
    expect(d.mode).toBe('free');
    expect(d.stunT).toBe(0);
  });

  it('belly-rub immunity blocks the flip entirely', () => {
    const s = freshPlay();
    s.dogs.cheddar.x = 480;
    s.dogs.cheddar.y = 400;
    s.dogs.cocoa.x = 500;
    s.dogs.cocoa.y = 400;
    s.dogs.cocoa.immune = 3;
    doWrestle(s, s.dogs.cheddar, s.dogs.cocoa);
    expect(s.dogs.cocoa.mode).toBe('free'); // not stunned
  });

  it('out of range does not stun (lunge only)', () => {
    const s = freshPlay();
    s.dogs.cheddar.x = 200;
    s.dogs.cheddar.y = 400;
    s.dogs.cocoa.x = 500; // 300px away, beyond range 95
    s.dogs.cocoa.y = 400;
    doWrestle(s, s.dogs.cheddar, s.dogs.cocoa);
    expect(s.dogs.cocoa.mode).toBe('free');
    expect(s.dogs.cheddar.vx).toBeGreaterThan(0); // lunged toward cocoa
  });

  it('cannot wrestle while on cooldown or stunned', () => {
    const s = freshPlay();
    const a = s.dogs.cheddar;
    const b = s.dogs.cocoa;
    a.wrestleCD = 1;
    expect(canWrestle(s, a, b)).toBe(false);
    a.wrestleCD = 0;
    b.mode = 'stunned';
    expect(canWrestle(s, a, b)).toBe(false);
  });

  it('flipping the spot-holder steals the spot', () => {
    const s = freshPlay();
    s.dogs.cheddar.x = 480;
    s.dogs.cheddar.y = 400;
    s.dogs.cocoa.x = 500;
    s.dogs.cocoa.y = 400;
    s.spot = { x: 500, y: 400, r: 52, holder: 'cocoa', prog: 2, pulse: 0 };
    // force cheddar to win so it steals: search a seed where rng.next()<0.70
    for (let seed = 1; seed < 50; seed++) {
      const t = makeGameState(makeRng(999));
      t.phase = 'play';
      t.dogs.cheddar.x = 480;
      t.dogs.cheddar.y = 400;
      t.dogs.cocoa.x = 500;
      t.dogs.cocoa.y = 400;
      t.spot = { x: 500, y: 400, r: 52, holder: 'cocoa', prog: 2, pulse: 0 };
      t.rng = makeRng(seed);
      doWrestle(t, t.dogs.cheddar, t.dogs.cocoa);
      if (t.dogs.cocoa.mode === 'stunned') {
        // cheddar won -> spot should now be cheddar's
        expect(t.spot!.holder).toBe('cheddar');
        expect(t.steals.cheddar).toBe(1);
        return;
      }
    }
    throw new Error('no winning seed found (unexpected)');
  });
});
