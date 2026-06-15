/**
 * M13 — "Kitchen Counter Caper" co-op mission with asymmetric abilities. Driven headlessly:
 * only Cheddar's chair-leap knocks a snack off the counter, fallen snacks are eaten on contact
 * or swept up if ignored, and gobbling the target wins.
 */
import { describe, it, expect } from 'vitest';
import { makeRng } from '../../src/core/rng.js';
import { makeGameState } from '../../src/state/gameState.js';
import { updateGame, beginScene } from '../../src/state/sceneManager.js';
import { KITCHEN } from '../../src/config/balance.js';

const DT = 1 / 60;
const noIntent = { ax: 0, ay: 0, arrive: 0 };

interface KitchenData {
  onCounter: { x: number; knocked: boolean }[];
  floor: { x: number; y: number; ttl: number }[];
  eaten: number;
}

const COUNTER_Y = 252;
const FLOOR_Y = 384;

function startKitchen(seed = 1) {
  const s = makeGameState(makeRng(seed));
  s.mode = 'coop';
  s.partner = 'human'; // pin the sibling so manual positioning drives the test
  s.sceneIdx = 3;
  beginScene(s);
  for (let i = 0; i < 200 && s.phase !== 'play'; i++) updateGame(s, noIntent, false, false, DT);
  return s;
}
const kit = (s: ReturnType<typeof startKitchen>) => s.mission!.data as KitchenData;
const step = (s: ReturnType<typeof startKitchen>) => updateGame(s, noIntent, false, false, DT);

describe('Kitchen Counter Caper (M13 asymmetric)', () => {
  it("Cheddar's chair-leap knocks a snack onto the floor", () => {
    const s = startKitchen();
    const fx = kit(s).onCounter[0]!.x;
    s.dogs.cheddar.x = fx;
    s.dogs.cheddar.y = COUNTER_Y + 60; // just below the counter
    s.dogs.cheddar.jumpT = 0.4; // mid chair-leap
    // keep Cocoa well clear so she doesn't gobble the drop this step
    s.dogs.cocoa.x = 200;
    s.dogs.cocoa.y = 520;
    step(s);
    expect(kit(s).onCounter[0]!.knocked).toBe(true);
    expect(kit(s).floor.length).toBeGreaterThan(0);
  });

  it('Cocoa cannot knock snacks off the counter (asymmetry)', () => {
    const s = startKitchen();
    const fx = kit(s).onCounter[0]!.x;
    s.dogs.cocoa.x = fx;
    s.dogs.cocoa.y = COUNTER_Y + 60;
    s.dogs.cocoa.jumpT = 0.4; // Cocoa leaps — but she has no chair-leap
    s.dogs.cheddar.x = 200;
    s.dogs.cheddar.y = 520;
    step(s);
    expect(kit(s).onCounter.every((f) => !f.knocked)).toBe(true);
    expect(kit(s).floor.length).toBe(0);
  });

  it('a fallen snack on the floor is gobbled on contact for combined score', () => {
    const s = startKitchen();
    kit(s).floor.push({ x: 500, y: FLOOR_Y, ttl: KITCHEN.floorTtl });
    s.dogs.cocoa.x = 500;
    s.dogs.cocoa.y = FLOOR_Y;
    const before = s.mission!.combinedScore;
    step(s);
    expect(kit(s).floor.length).toBe(0);
    expect(kit(s).eaten).toBe(1);
    expect(s.mission!.combinedScore).toBe(before + KITCHEN.foodScore);
  });

  it('a fallen snack is swept up if it sits too long (no eat)', () => {
    const s = startKitchen();
    kit(s).floor.push({ x: 500, y: FLOOR_Y, ttl: 0.01 });
    s.dogs.cheddar.x = 200; // nobody near it
    s.dogs.cocoa.x = 240;
    step(s);
    expect(kit(s).floor.length).toBe(0);
    expect(kit(s).eaten).toBe(0); // swept, not eaten
  });

  it('gobbling the target number of snacks wins', () => {
    const s = startKitchen();
    const d = kit(s);
    // pre-load the floor with the target and park Cocoa to gobble them one by one
    for (let i = 0; i < KITCHEN.target; i++) {
      d.floor.length = 0;
      d.floor.push({ x: 500, y: FLOOR_Y, ttl: KITCHEN.floorTtl });
      s.dogs.cocoa.x = 500;
      s.dogs.cocoa.y = FLOOR_Y;
      step(s);
    }
    expect(d.eaten).toBe(KITCHEN.target);
    expect(s.mission!.status).toBe('success');
  });
});
