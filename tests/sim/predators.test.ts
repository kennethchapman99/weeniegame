import { describe, it, expect } from 'vitest';
import { makeRng } from '../../src/core/rng.js';
import { makeGameState } from '../../src/state/gameState.js';
import { startGame, updateGame } from '../../src/state/sceneManager.js';
import { spawnPredator, unitedFront, updatePredator } from '../../src/systems/predators.js';
import { updateEvents } from '../../src/systems/events.js';
import { EVENTS, YARD } from '../../src/config/balance.js';

const DT = 1 / 60;
const noIntent = { ax: 0, ay: 0, arrive: 0 };

function yardPlay(seed = 5) {
  const s = makeGameState(makeRng(seed));
  startGame(s);
  for (let i = 0; i < 120 && s.phase !== 'play'; i++) updateGame(s, noIntent, false, false, DT);
  s.toys = [];
  s.spot = null;
  s.sunbeam = null;
  s.predator = null;
  s.squirrel = null;
  s.treat = null;
  s.bellyRub = null;
  return s;
}

describe('predators (M7)', () => {
  it('a lone, still dog gets grabbed and carried off (−1)', () => {
    const s = yardPlay(1);
    // isolate the target far from its sibling so no united front
    const target = s.dogs.cheddar;
    target.x = 480;
    target.y = 400;
    target.score = 5;
    s.dogs.cocoa.x = 60;
    s.dogs.cocoa.y = 540;
    // force a coyote locked onto the lone, still target
    let grabbed = false;
    for (let spawn = 0; spawn < 6 && !grabbed; spawn++) {
      s.predator = null;
      spawnPredator(s);
      if (s.predator!.kind !== 'coyote') continue;
      s.predator!.targetId = 'cheddar';
      for (let i = 0; i < 60 * 8 && s.predator; i++) {
        target.x = 480; // hold still (don't dodge), sibling stays away
        target.y = 400;
        s.dogs.cocoa.x = 60;
        s.dogs.cocoa.y = 540;
        updatePredator(s, DT);
        // a stunned/carried target means it was caught
        if (s.carriedDog === 'cheddar') grabbed = true;
      }
    }
    expect(grabbed).toBe(true);
    expect(target.score).toBeLessThan(5); // carry-off penalty applied
  });

  it('two huddled dogs form a united front that flips an approaching predator to flee', () => {
    const s = yardPlay(2);
    s.dogs.cheddar.x = 470;
    s.dogs.cheddar.y = 400;
    s.dogs.cocoa.x = 500; // < unitedFrontRange apart, both free
    s.dogs.cocoa.y = 400;
    expect(unitedFront(s)).toBe(true);
    spawnPredator(s);
    // place the predator within scare range of the huddle
    s.predator!.x = 500;
    s.predator!.y = 420;
    s.predator!.state = s.predator!.kind === 'coyote' ? 'enter' : 'circle';
    updatePredator(s, DT);
    expect(s.predator!.state).toBe('flee');
    // both dogs let out a bark (animation overlay armed)
    expect(s.dogs.cheddar.barkT).toBeGreaterThan(0);
    expect(s.dogs.cocoa.barkT).toBeGreaterThan(0);
  });

  it('predators never persist outside the yard', () => {
    const s = yardPlay(3);
    spawnPredator(s);
    expect(s.predator).not.toBeNull();
    s.sceneKey = 'pool';
    updatePredator(s, DT);
    expect(s.predator).toBeNull();
  });
});

describe('ambient events (M7)', () => {
  it('a dog on the squirrel scores +3', () => {
    const s = yardPlay(4);
    s.squirrel = { x: 300, y: 240, vx: 4.4, dir: 1, seed: 0, got: false, mode: 'run', climbT: 0 };
    s.dogs.cheddar.x = 300;
    s.dogs.cheddar.y = 240;
    const before = s.dogs.cheddar.score;
    updateEvents(s, DT);
    expect(s.dogs.cheddar.score).toBe(before + EVENTS.squirrelReward);
  });

  it('an untagged squirrel scampers up the magnolia and escapes (no points)', () => {
    const s = yardPlay(4);
    // start it right at the magnolia trunk so it climbs immediately
    s.squirrel = {
      x: YARD.magnolia.x,
      y: YARD.magnolia.trunkBaseY,
      vx: 0,
      dir: 1,
      seed: 0,
      got: false,
      mode: 'run',
      climbT: 0,
    };
    // keep both dogs far away so it can't be tagged
    s.dogs.cheddar.x = 80;
    s.dogs.cheddar.y = 540;
    s.dogs.cocoa.x = 880;
    s.dogs.cocoa.y = 540;
    const before = s.dogs.cheddar.score + s.dogs.cocoa.score;
    for (let i = 0; i < 60 * 3 && s.squirrel; i++) updateEvents(s, DT);
    expect(s.squirrel).toBeNull(); // vanished up the tree
    expect(s.dogs.cheddar.score + s.dogs.cocoa.score).toBe(before); // nobody scored
  });

  it('a dog on a landed treat picks it up for +2', () => {
    const s = yardPlay(4);
    s.treat = { x: 480, y: 400, room: '', telegraph: 0, glow: 0 };
    s.dogs.cheddar.x = 480;
    s.dogs.cheddar.y = 400;
    const before = s.dogs.cheddar.score;
    updateEvents(s, DT);
    expect(s.dogs.cheddar.score).toBe(before + EVENTS.treatReward);
    expect(s.treat).toBeNull();
  });

  it('a belly rub grants 3s wrestle immunity', () => {
    const s = yardPlay(4);
    s.bellyRub = { x: 480, y: 400, room: '', r: 36, life: 9 };
    s.dogs.cheddar.x = 480;
    s.dogs.cheddar.y = 400;
    updateEvents(s, DT);
    expect(s.dogs.cheddar.immune).toBeCloseTo(EVENTS.bellyImmunity, 5);
    expect(s.bellyRub).toBeNull();
  });

  it('no ambient events in the pool', () => {
    const s = yardPlay(4);
    s.sceneKey = 'pool';
    s.eventTimer = -10;
    updateEvents(s, DT);
    expect(s.squirrel).toBeNull();
    expect(s.treat).toBeNull();
    expect(s.bellyRub).toBeNull();
  });
});
