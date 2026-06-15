/**
 * M10 — local two-player. These drive `updateGame` with a synthetic P2 command and assert the
 * sibling dog is steered by P2 (not the AI), that solo still falls back to the AI brain, and
 * that the human-only systems (wrestle lunge, tug auto-mash) switch correctly with `partner`.
 */
import { describe, it, expect } from 'vitest';
import { makeRng } from '../../src/core/rng.js';
import { makeGameState, isHuman } from '../../src/state/gameState.js';
import { startGame, updateGame } from '../../src/state/sceneManager.js';
import { startTug } from '../../src/systems/tug.js';
import type { DogCmd } from '../../src/core/input.js';

const DT = 1 / 60;
const noIntent = { ax: 0, ay: 0, arrive: 0 };
const idleP2: DogCmd = { intent: { ax: 0, ay: 0, arrive: 0 }, wrestle: false, jump: false };

/** A fresh yard round, quiet (no toys/spot/sunbeam) so only inputs move the dogs. */
function freshPlay(partner: 'human' | 'ai', seed = 5) {
  const s = makeGameState(makeRng(seed));
  s.partner = partner;
  startGame(s);
  for (let i = 0; i < 120 && s.phase !== 'play'; i++) updateGame(s, noIntent, false, false, DT, idleP2);
  s.toys = [];
  s.spot = null;
  s.sunbeam = null;
  // keep the yard quiet: no predator/event interference with the input-routing assertions
  s.predatorTimer = 999;
  s.eventTimer = 999;
  return s;
}

function move(intent: { ax: number; ay: number; arrive: number }): DogCmd {
  return { intent, wrestle: false, jump: false };
}

function ropeToy(x: number, y: number) {
  return { x, y, room: '', fl: -1, ox: 0, oy: 0, tug: true, type: 'rope' as const, t: 0, scale: 1 };
}

describe('two-player input routing (M10)', () => {
  it('isHuman marks only P1 in solo, and both dogs in co-op', () => {
    const solo = makeGameState(makeRng(1)); // partner defaults to 'ai'
    expect(isHuman(solo, solo.playerId)).toBe(true);
    expect(isHuman(solo, solo.aiId)).toBe(false);

    const coop = makeGameState(makeRng(1));
    coop.partner = 'human';
    expect(isHuman(coop, coop.playerId)).toBe(true);
    expect(isHuman(coop, coop.aiId)).toBe(true);
  });

  it('P2 steers the sibling dog in the commanded direction', () => {
    const s = freshPlay('human');
    const sib = s.dogs[s.aiId];
    sib.x = 480;
    sib.y = 400;
    const x0 = sib.x;
    // P2 drives right at full deflection for half a second
    for (let i = 0; i < 30; i++) updateGame(s, noIntent, false, false, DT, move({ ax: 1, ay: 0, arrive: 1 }));
    expect(sib.x).toBeGreaterThan(x0 + 10);
  });

  it('the AI does NOT drive the sibling when partner is human (idle P2 = idle dog)', () => {
    const s = freshPlay('human');
    const sib = s.dogs[s.aiId];
    sib.x = 480;
    sib.y = 400;
    // tempt the AI with a toy it would normally chase — but P2 is idle, so it must not move
    s.toys = [{ x: 200, y: 300, room: '', fl: -1, ox: 0, oy: 0, tug: false, type: 'ball', t: 0, scale: 1 }];
    const x0 = sib.x;
    const y0 = sib.y;
    for (let i = 0; i < 60; i++) updateGame(s, noIntent, false, false, DT, idleP2);
    expect(Math.hypot(sib.x - x0, sib.y - y0)).toBeLessThan(3);
  });

  it('solo still falls back to the AI brain (sibling chases a toy with no P2)', () => {
    const s = freshPlay('ai');
    const sib = s.dogs[s.aiId];
    sib.x = 480;
    sib.y = 400;
    s.toys = [{ x: 200, y: 300, room: '', fl: -1, ox: 0, oy: 0, tug: false, type: 'ball', t: 0, scale: 1 }];
    for (let i = 0; i < 60; i++) updateGame(s, noIntent, false, false, DT); // no p2 arg
    // it moved toward the toy (left/up)
    expect(sib.x).toBeLessThan(480);
  });

  it('P2 can wrestle and flip P1 when adjacent', () => {
    const s = freshPlay('human', 7);
    const p1 = s.dogs[s.playerId];
    const p2 = s.dogs[s.aiId];
    p1.x = 500;
    p1.y = 400;
    p2.x = 512;
    p2.y = 400; // within WRESTLE.range
    // P2 presses wrestle; with both free and in range, someone gets stunned (win or reversal)
    updateGame(s, noIntent, false, false, DT, { intent: noIntent, wrestle: true, jump: false });
    const someoneStunned = p1.mode === 'stunned' || p2.mode === 'stunned';
    expect(someoneStunned).toBe(true);
  });
});

describe('two-player tug (M10)', () => {
  it('with no AI auto-mash, the only side that mashes wins the tug', () => {
    const s = freshPlay('human', 3);
    const a = s.dogs.cheddar;
    const b = s.dogs.cocoa;
    a.x = 480;
    a.y = 400;
    b.x = 500;
    b.y = 400;
    const rope = ropeToy(490, 400);
    s.toys = [rope];
    startTug(s, rope);
    const cocoaBefore = b.score;
    // Only P2/Cocoa hammers WRESTLE; Cheddar idles. Auto-mash is off in co-op, so Cocoa must win.
    // (playerId defaults to cheddar, so the sibling is cocoa — drive its wrestle via p2.)
    for (let i = 0; i < 60 * 14 && s.tug; i++) {
      const p2Cmd: DogCmd = { intent: noIntent, wrestle: true, jump: false };
      updateGame(s, noIntent, false, false, DT, p2Cmd);
    }
    expect(s.tug).toBeNull();
    expect(b.score).toBe(cocoaBefore + 3); // SCORE.ropeWin
    expect(a.mode).toBe('stunned'); // the idle dog loses and tumbles
  });
});
