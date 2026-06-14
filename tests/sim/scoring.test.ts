import { describe, it, expect } from 'vitest';
import { makeRng } from '../../src/core/rng.js';
import { makeGameState, addScore } from '../../src/state/gameState.js';
import { startGame, updateGame, sceneDefs } from '../../src/state/sceneManager.js';
import { spawnToy, updateToys } from '../../src/systems/toys.js';
import { updateSpot } from '../../src/systems/spot.js';
import { ZOOMIES, SCORE, SPOT } from '../../src/config/balance.js';

const DT = 1 / 60;
const noIntent = { ax: 0, ay: 0, arrive: 0 };

function freshPlay() {
  const s = makeGameState(makeRng(1234));
  startGame(s); // -> inter
  // advance past the interstitial into play
  for (let i = 0; i < 120 && s.phase !== 'play'; i++) updateGame(s, noIntent, false, DT);
  return s;
}

describe('scene flow', () => {
  it('title -> inter -> play through every registered round -> end', () => {
    const s = makeGameState(makeRng(7));
    expect(s.phase).toBe('title');
    startGame(s);
    expect(s.phase).toBe('inter');
    // play long enough to clear every round (each ~45-75s + interstitial)
    let playRounds = 0;
    let wasPlay = false;
    for (let i = 0; i < 60 * 300 && s.phase !== 'end'; i++) {
      updateGame(s, noIntent, false, DT);
      if (s.phase === 'play' && !wasPlay) playRounds++;
      wasPlay = s.phase === 'play';
    }
    expect(s.phase).toBe('end');
    expect(playRounds).toBe(sceneDefs().length); // entered each registered round once
  });

  it('the round timer counts down during play', () => {
    const s = freshPlay();
    const t0 = s.timeLeft;
    for (let i = 0; i < 60; i++) updateGame(s, noIntent, false, DT);
    expect(s.timeLeft).toBeLessThan(t0);
  });
});

describe('toys (M2)', () => {
  it('a dog standing on a toy picks it up and scores +1', () => {
    const s = freshPlay();
    s.toys = [];
    s.dogs.cheddar.x = 480;
    s.dogs.cheddar.y = 400;
    spawnToyAt(s, 480, 400, false);
    const before = s.dogs.cheddar.score;
    updateToys(s, DT);
    expect(s.dogs.cheddar.score).toBe(before + SCORE.toy);
    expect(s.toys.find((t) => !t.tug)).toBeUndefined();
  });

  it('an uncontested rope toy is solo-grabbed for +2', () => {
    const s = freshPlay();
    s.toys = [];
    s.dogs.cheddar.x = 300;
    s.dogs.cheddar.y = 400;
    s.dogs.cocoa.x = 900; // far away, cannot contest
    s.dogs.cocoa.y = 220;
    spawnToyAt(s, 300, 400, true);
    const before = s.dogs.cheddar.score;
    updateToys(s, DT);
    expect(s.dogs.cheddar.score).toBe(before + SCORE.ropeSolo);
  });
});

describe('cuddle spot (M2)', () => {
  it('holding the spot for SPOT.hold seconds scores +3 and relocates', () => {
    const s = freshPlay();
    s.spot = { x: 480, y: 400, r: SPOT.radius, holder: null, prog: 0, pulse: 0 };
    s.dogs.cheddar.x = 480;
    s.dogs.cheddar.y = 400;
    s.dogs.cocoa.x = 100; // out of the spot
    s.dogs.cocoa.y = 540;
    const before = s.dogs.cheddar.score;
    const steps = Math.ceil(SPOT.hold / DT) + 5;
    for (let i = 0; i < steps; i++) updateSpot(s, DT);
    expect(s.dogs.cheddar.score).toBe(before + SCORE.spot);
    // relocated to a new spot
    expect(s.spot && (s.spot.x !== 480 || s.spot.y !== 400)).toBe(true);
  });
});

describe('central addScore + zoomies hook', () => {
  it('three scores within the window trigger zoomies', () => {
    const s = makeGameState(makeRng(2));
    const d = s.dogs.cheddar;
    expect(d.zoom).toBe(0);
    addScore(s, d, 1);
    addScore(s, d, 1);
    addScore(s, d, 1);
    expect(d.zoom).toBeGreaterThan(0);
    expect(d.zoom).toBe(ZOOMIES.duration);
  });

  it('spread-out scores do NOT trigger zoomies', () => {
    const s = makeGameState(makeRng(2));
    const d = s.dogs.cheddar;
    addScore(s, d, 1);
    s.elapsedMs += ZOOMIES.windowMs + 1;
    addScore(s, d, 1);
    s.elapsedMs += ZOOMIES.windowMs + 1;
    addScore(s, d, 1);
    expect(d.zoom).toBe(0);
  });
});

function spawnToyAt(s: ReturnType<typeof makeGameState>, x: number, y: number, tug: boolean) {
  void spawnToy; // spawnToy is RNG-placed; tests need exact positions, so push directly
  s.toys.push({ x, y, room: '', fl: -1, ox: 0, oy: 0, tug, type: tug ? 'rope' : 'ball', t: 0, scale: 1 });
}
