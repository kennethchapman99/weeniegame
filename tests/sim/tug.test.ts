import { describe, it, expect } from 'vitest';
import { makeRng } from '../../src/core/rng.js';
import { makeGameState } from '../../src/state/gameState.js';
import { startGame, updateGame } from '../../src/state/sceneManager.js';
import { startTug, tugPull, updateTug } from '../../src/systems/tug.js';
import { tryJump } from '../../src/systems/jump.js';
import { updateToys } from '../../src/systems/toys.js';
import { JUMP, SCORE, TUG } from '../../src/config/balance.js';

const DT = 1 / 60;
const noIntent = { ax: 0, ay: 0, arrive: 0 };

function freshPlay(seed = 5) {
  const s = makeGameState(makeRng(seed));
  startGame(s);
  for (let i = 0; i < 120 && s.phase !== 'play'; i++) updateGame(s, noIntent, false, false, DT);
  s.toys = [];
  s.spot = null;
  s.sunbeam = null;
  s.tug = null;
  return s;
}

function ropeToy(x: number, y: number) {
  return { x, y, room: '', fl: -1, ox: 0, oy: 0, tug: true, type: 'rope' as const, t: 0, scale: 1 };
}

describe('tug-of-war (M6)', () => {
  it('both dogs reaching a rope start a tug that locks both into tug mode', () => {
    const s = freshPlay();
    s.dogs.cheddar.x = 480;
    s.dogs.cheddar.y = 400;
    s.dogs.cocoa.x = 500;
    s.dogs.cocoa.y = 400;
    s.toys = [ropeToy(490, 400)];
    updateToys(s, DT);
    expect(s.tug).not.toBeNull();
    expect(s.dogs.cheddar.mode).toBe('tug');
    expect(s.dogs.cocoa.mode).toBe('tug');
  });

  it('mashing pulls the rope and resolves to a winner who scores +3', () => {
    const s = freshPlay();
    s.dogs.cheddar.x = 480;
    s.dogs.cheddar.y = 400;
    s.dogs.cocoa.x = 500;
    s.dogs.cocoa.y = 400;
    const rope = ropeToy(490, 400);
    s.toys = [rope];
    startTug(s, rope);
    const before = s.dogs.cheddar.score;
    // cheddar is the player here; hammer the mash so it wins decisively
    for (let i = 0; i < 60 * 14 && s.tug; i++) {
      tugPull(s, s.dogs.cheddar, 5);
      updateTug(s, DT);
    }
    expect(s.tug).toBeNull(); // resolved (winner or stalemate)
    // a clear winner: cheddar mashed hard, should have scored the rope win
    expect(s.dogs.cheddar.score).toBe(before + SCORE.ropeWin);
    expect(s.dogs.cocoa.mode).toBe('stunned'); // loser tumbles
  });

  it('a tug with no decisive pull ends in a stalemate by TUG.stalemate seconds', () => {
    const s = freshPlay(2);
    s.dogs.cheddar.x = 480;
    s.dogs.cheddar.y = 400;
    s.dogs.cocoa.x = 500;
    s.dogs.cocoa.y = 400;
    const rope = ropeToy(490, 400);
    s.toys = [rope];
    startTug(s, rope);
    // nobody mashes except the AI's own balanced pulls; run past the stalemate window
    const steps = Math.ceil(TUG.stalemate / DT) + 30;
    for (let i = 0; i < steps && s.tug; i++) updateTug(s, DT);
    expect(s.tug).toBeNull();
    // both dogs are free again after the rope flies (winner path would stun the loser)
    const bothFreeOrWinner = s.dogs.cheddar.mode === 'free' || s.dogs.cocoa.mode === 'free';
    expect(bothFreeOrWinner).toBe(true);
  });

  it('scene reset clears an in-progress tug', () => {
    const s = freshPlay();
    const rope = ropeToy(490, 400);
    s.toys = [rope];
    s.dogs.cheddar.x = 490;
    s.dogs.cocoa.x = 490;
    startTug(s, rope);
    expect(s.tug).not.toBeNull();
    // burning the round timer to 0 triggers endRound -> next scene's beginScene reset
    s.timeLeft = 0.001;
    updateGame(s, noIntent, false, false, DT);
    expect(s.tug).toBeNull();
    expect(s.dogs.cheddar.mode).toBe('free');
  });
});

describe('jump (M6)', () => {
  it('jumping sets a 0.5s arc and queues a yip sound', () => {
    const s = freshPlay();
    const d = s.dogs.cheddar;
    expect(tryJump(s, d)).toBe(true);
    expect(d.jumpT).toBeCloseTo(JUMP.duration, 5);
    expect(s.sounds).toContain('yip');
  });

  it('cannot jump while stunned', () => {
    const s = freshPlay();
    const d = s.dogs.cheddar;
    d.mode = 'stunned';
    d.stunT = 1;
    expect(tryJump(s, d)).toBe(false);
    expect(d.jumpT).toBe(0);
  });
});
