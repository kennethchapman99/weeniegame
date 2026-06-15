/**
 * M13 — "Over the Creek" boost-jump mission. Driven headlessly: a grounded pup can't cross the
 * creek, an airborne (boosted) pup flies over, a real boost-launch carries the player across, the
 * far pad drops the bridge, and the mission resolves to success.
 */
import { describe, it, expect } from 'vitest';
import { makeRng } from '../../src/core/rng.js';
import { makeGameState } from '../../src/state/gameState.js';
import { updateGame, beginScene } from '../../src/state/sceneManager.js';
import type { DogCmd } from '../../src/core/input.js';

const DT = 1 / 60;
const noIntent = { ax: 0, ay: 0, arrive: 0 };
const idleP2: DogCmd = { intent: noIntent, wrestle: false, jump: false };

interface CreekData {
  bridge: { down: boolean; prog: number };
}

const RIGHT = 480 + 70; // creek.cx + creek.hw

/** Sit at the start of the creek mission (registry index 2), in 'play', two-player so the
 *  sibling is a still human (no AI wandering off the boost pad). */
function startCreek(seed = 1) {
  const s = makeGameState(makeRng(seed));
  s.mode = 'coop';
  s.partner = 'human';
  s.sceneIdx = 2;
  beginScene(s);
  for (let i = 0; i < 200 && s.phase !== 'play'; i++) updateGame(s, noIntent, false, false, DT, idleP2);
  return s;
}

const creek = (s: ReturnType<typeof startCreek>) => s.mission!.data as CreekData;
const step = (s: ReturnType<typeof startCreek>, jump = false) =>
  updateGame(s, noIntent, false, jump, DT, idleP2);

describe('Over the Creek (M13 boost-jump)', () => {
  it('a grounded pup cannot wade across the creek', () => {
    const s = startCreek();
    s.dogs.cheddar.x = 460; // in the water strip, on the near side
    s.dogs.cheddar.y = 405;
    s.dogs.cheddar.jumpT = 0;
    step(s);
    expect(s.dogs.cheddar.x).toBeLessThan(480 - 70); // shoved back to the near bank
  });

  it('an airborne pup sails over the creek (not blocked)', () => {
    const s = startCreek();
    s.dogs.cheddar.x = 480; // mid-creek
    s.dogs.cheddar.y = 405;
    s.dogs.cheddar.jumpT = 0.5; // in the air
    step(s);
    expect(s.dogs.cheddar.x).toBeGreaterThan(480 - 70);
    expect(s.dogs.cheddar.x).toBeLessThan(RIGHT); // still over the water, not ejected
  });

  it('a boost-launch carries the player clear across to the far bank', () => {
    const s = startCreek();
    // booster (cocoa) braces on the boost pad; jumper (cheddar, P1) stands beside it
    s.dogs.cocoa.x = 372;
    s.dogs.cocoa.y = 405;
    s.dogs.cheddar.x = 400;
    s.dogs.cheddar.y = 405;
    step(s, true); // P1 presses jump next to the booster → launch
    expect(s.dogs.cheddar.jumpT).toBeGreaterThan(0); // airborne
    for (let i = 0; i < 80; i++) step(s); // ride the arc out
    expect(s.dogs.cheddar.x).toBeGreaterThan(RIGHT);
    expect(s.mission!.objectives[0]!.done).toBe(true);
  });

  it('the far pad drops the bridge', () => {
    const s = startCreek();
    s.dogs.cheddar.x = 664; // on the far pad
    s.dogs.cheddar.y = 300;
    step(s);
    expect(creek(s).bridge.down).toBe(true);
    expect(s.mission!.objectives[1]!.done).toBe(true);
  });

  it('clearing all three objectives wins the mission', () => {
    const s = startCreek();
    s.dogs.cheddar.x = 620; // crossed
    s.dogs.cheddar.y = 405;
    step(s);
    s.dogs.cheddar.x = 664; // drop the bridge
    s.dogs.cheddar.y = 300;
    step(s);
    s.dogs.cheddar.x = 842; // both into the den
    s.dogs.cheddar.y = 470;
    s.dogs.cocoa.x = 842;
    s.dogs.cocoa.y = 455;
    step(s);
    expect(s.mission!.status).toBe('success');
    expect(s.phase).toBe('end');
  });
});
