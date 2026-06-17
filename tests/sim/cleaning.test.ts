/**
 * M13 — "The Cleaning Ladies Are Here" distract + carry/escort mission.
 * Driven headlessly: a pup picks up the loose toy; carrying it past an un-distracted vacuum gets
 * it "put away" (dropped + stunned); but with a teammate luring the vacuum the toy reaches the
 * couch and the mission succeeds.
 */
import { describe, it, expect } from 'vitest';
import { makeRng } from '../../src/core/rng.js';
import { makeGameState } from '../../src/state/gameState.js';
import { beginScene, updateGame } from '../../src/state/sceneManager.js';
import { CLEANING } from '../../src/config/balance.js';

const DT = 1 / 60;
const noIntent = { ax: 0, ay: 0, arrive: 0 };

interface CleanData {
  vac: { x: number; y: number; dir: 1 | -1; distracted: boolean };
  toy: { x: number; y: number; carrier: 'cheddar' | 'cocoa' | null; safe: boolean };
  couch: { x: number; y: number; r: number };
}

/** Sit at the start of the cleaning mission (co-op registry index 5), in 'play'. */
function startClean(seed = 1) {
  const s = makeGameState(makeRng(seed));
  s.mode = 'coop';
  s.partner = 'human'; // pin the sibling so manual positioning drives the test
  s.sceneIdx = 5;
  beginScene(s);
  for (let i = 0; i < 200 && s.phase !== 'play'; i++) updateGame(s, noIntent, false, false, DT);
  return s;
}

const data = (s: ReturnType<typeof startClean>) => s.mission!.data as CleanData;
const step = (s: ReturnType<typeof startClean>) => updateGame(s, noIntent, false, false, DT);

describe('The Cleaning Ladies Are Here (M13 distract + carry)', () => {
  it('is the registered cleaning mission', () => {
    const s = startClean();
    expect(s.mission!.key).toBe('mission-clean');
  });

  it('a free pup on the loose toy picks it up', () => {
    const s = startClean();
    const d = data(s);
    s.dogs.cheddar.x = d.toy.x;
    s.dogs.cheddar.y = d.toy.y;
    s.dogs.cocoa.x = 240; // far, irrelevant
    s.dogs.cocoa.y = 540;
    step(s);
    expect(d.toy.carrier).toBe('cheddar');
  });

  it('carrying past an un-distracted vacuum drops the toy and stuns the carrier', () => {
    const s = startClean();
    const d = data(s);
    // cheddar grabs the toy
    s.dogs.cheddar.x = d.toy.x;
    s.dogs.cheddar.y = d.toy.y;
    s.dogs.cocoa.x = 1100; // nowhere near the vacuum → no distraction
    s.dogs.cocoa.y = 600;
    step(s);
    expect(d.toy.carrier).toBe('cheddar');
    // shove the carrier right onto the vacuum
    s.dogs.cheddar.x = d.vac.x;
    s.dogs.cheddar.y = d.vac.y;
    step(s);
    expect(d.toy.carrier).toBeNull(); // put away
    expect(s.dogs.cheddar.mode).toBe('stunned');
    expect(d.toy.safe).toBe(false);
  });

  it('a teammate luring the vacuum lets the carrier deliver the toy and win', () => {
    const s = startClean();
    const d = data(s);
    // cheddar grabs the toy
    s.dogs.cheddar.x = d.toy.x;
    s.dogs.cheddar.y = d.toy.y;
    s.dogs.cocoa.x = d.vac.x; // cocoa decoys the vacuum
    s.dogs.cocoa.y = d.vac.y;
    step(s);
    expect(d.toy.carrier).toBe('cheddar');
    // run the toy to the couch (cocoa keeps the vacuum lured)
    s.dogs.cheddar.x = d.couch.x;
    s.dogs.cheddar.y = d.couch.y;
    s.dogs.cocoa.x = d.vac.x;
    s.dogs.cocoa.y = d.vac.y;
    step(s);
    expect(d.toy.safe).toBe(true);
    step(s); // resolve success
    expect(s.mission!.status).toBe('success');
    expect(s.phase).toBe('end');
  });

  it('the vacuum patrols within its sweep range when un-distracted', () => {
    const s = startClean();
    const d = data(s);
    s.dogs.cheddar.x = 240; // both pups out of distract range
    s.dogs.cheddar.y = 600;
    s.dogs.cocoa.x = 300;
    s.dogs.cocoa.y = 620;
    for (let i = 0; i < 60; i++) {
      step(s);
      expect(d.vac.x).toBeGreaterThanOrEqual(CLEANING.patrol[0] - 0.01);
      expect(d.vac.x).toBeLessThanOrEqual(CLEANING.patrol[1] + 0.01);
    }
  });
});
