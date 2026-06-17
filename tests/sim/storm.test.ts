/**
 * M13 — "The Thunderstorm" co-op comfort/co-regulation SURVIVE mission.
 * Driven headlessly: cuddling drains panic; staying apart raises it; a maxed panic meter fails
 * the mission; sheltered pups shrug off the thunderclap; and weathering the full timer wins.
 */
import { describe, it, expect } from 'vitest';
import { makeRng } from '../../src/core/rng.js';
import { makeGameState } from '../../src/state/gameState.js';
import { beginScene, updateGame } from '../../src/state/sceneManager.js';

const DT = 1 / 60;
const noIntent = { ax: 0, ay: 0, arrive: 0 };

interface StormData {
  panic: { cheddar: number; cocoa: number };
  flash: number;
  boomIn: number;
  strikeX: number;
  shelters: { x: number; y: number; r: number }[];
}

/** Sit at the start of the storm mission (co-op registry index 7), in 'play'. */
function startStorm(seed = 1) {
  const s = makeGameState(makeRng(seed));
  s.mode = 'coop';
  s.partner = 'human'; // pin the sibling so manual positioning drives the test
  s.sceneIdx = 7;
  beginScene(s);
  for (let i = 0; i < 200 && s.phase !== 'play'; i++) updateGame(s, noIntent, false, false, DT);
  return s;
}

const data = (s: ReturnType<typeof startStorm>) => s.mission!.data as StormData;
const step = (s: ReturnType<typeof startStorm>) => updateGame(s, noIntent, false, false, DT);

/** Park both pups huddled inside a shelter, well away from any strike. */
function huddleInShelter(s: ReturnType<typeof startStorm>) {
  const sh = data(s).shelters[0]!;
  s.dogs.cheddar.x = sh.x;
  s.dogs.cheddar.y = sh.y;
  s.dogs.cocoa.x = sh.x + 10;
  s.dogs.cocoa.y = sh.y;
}

describe('The Thunderstorm (M13 comfort/co-regulation)', () => {
  it('is the registered storm survive mission', () => {
    const s = startStorm();
    expect(s.mission!.key).toBe('mission-storm');
    expect(s.mission!.surviveMode).toBe(true);
  });

  it('cuddling drains panic; staying apart raises it', () => {
    const s = startStorm();
    const d = data(s);
    d.panic.cheddar = 0.5;
    d.panic.cocoa = 0.5;
    d.boomIn = 999; // suppress strikes for a clean read
    // huddled together → both calm down
    s.dogs.cheddar.x = 600;
    s.dogs.cheddar.y = 470;
    s.dogs.cocoa.x = 610;
    s.dogs.cocoa.y = 470;
    for (let i = 0; i < 30; i++) {
      d.boomIn = 999;
      step(s);
    }
    expect(d.panic.cheddar).toBeLessThan(0.5);

    // now drag them apart → panic climbs
    const lvl = d.panic.cheddar;
    s.dogs.cocoa.x = 100;
    s.dogs.cocoa.y = 660;
    for (let i = 0; i < 30; i++) {
      d.boomIn = 999;
      step(s);
    }
    expect(d.panic.cheddar).toBeGreaterThan(lvl);
  });

  it('a maxed-out panic meter fails the mission', () => {
    const s = startStorm();
    const d = data(s);
    d.panic.cheddar = 0.99;
    // keep them apart (and away from shelters) so panic ticks over the top
    s.dogs.cheddar.x = 200;
    s.dogs.cheddar.y = 250;
    s.dogs.cocoa.x = 1100;
    s.dogs.cocoa.y = 250;
    for (let i = 0; i < 60 && s.mission!.status === 'active'; i++) {
      d.boomIn = 999; // isolate the alone-rise from thunder
      step(s);
    }
    expect(s.mission!.status).toBe('fail');
    expect(s.phase).toBe('end');
  });

  it('a shelter blunts the thunderclap spike', () => {
    const s = startStorm();
    const d = data(s);
    const sh = d.shelters[0]!;
    // cheddar sheltered at the strike epicenter, cocoa exposed at the same spot
    d.strikeX = sh.x;
    d.flash = 0.5; // already telegraphed → the boom won't re-randomize strikeX
    d.boomIn = DT / 2; // force a boom this step
    s.dogs.cheddar.x = sh.x;
    s.dogs.cheddar.y = sh.y;
    s.dogs.cocoa.x = sh.x; // same x (same proximity) but standing in the open
    s.dogs.cocoa.y = 200; // outside any shelter radius
    step(s);
    expect(d.panic.cheddar).toBeLessThan(d.panic.cocoa);
  });

  it('weathering the full timer wins', () => {
    const s = startStorm();
    const d = data(s);
    // play perfectly: huddled in a shelter the whole time, strikes suppressed
    for (let i = 0; i < 46 * 60 && s.mission!.status === 'active'; i++) {
      huddleInShelter(s);
      d.boomIn = 999;
      d.panic.cheddar = 0;
      d.panic.cocoa = 0;
      step(s);
    }
    expect(s.mission!.status).toBe('success');
    expect(s.mission!.stars).toBe(3); // survive missions award full marks
  });
});
