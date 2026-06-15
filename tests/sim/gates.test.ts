/**
 * M12 — the "needs both dogs" gate primitives. Each is driven headlessly with both dogs and
 * asserted in isolation: pressure pads, boost-jump, and distract+grab.
 */
import { describe, it, expect } from 'vitest';
import { makeDog } from '../../src/state/dog.js';
import type { Pad } from '../../src/state/gameState.js';
import { updatePads, allPadsPressed, canBoost, boostLaunch, isDistracted } from '../../src/systems/gates.js';
import { GATES, JUMP } from '../../src/config/balance.js';

const pad = (x: number, y: number): Pad => ({ x, y, r: GATES.padR, on: false, by: null });

describe('pressure pads (bothOnSpots)', () => {
  it('a dog on a pad presses it; an empty pad reads off', () => {
    const pads = [pad(200, 300), pad(200, 500)];
    const ched = makeDog('cheddar', 200, 300);
    updatePads(pads, [ched]);
    expect(pads[0]!.on).toBe(true);
    expect(pads[0]!.by).toBe('cheddar');
    expect(pads[1]!.on).toBe(false);
  });

  it('opens only when BOTH pads are pressed by DIFFERENT dogs', () => {
    const pads = [pad(200, 300), pad(200, 500)];
    const ched = makeDog('cheddar', 200, 300);
    const coco = makeDog('cocoa', 200, 500);
    updatePads(pads, [ched, coco]);
    expect(allPadsPressed(pads)).toBe(true);
  });

  it('one dog cannot cover two pads (interdependence)', () => {
    // pads close together; only one dog present, nearest to both
    const pads = [pad(200, 300), pad(210, 305)];
    const ched = makeDog('cheddar', 205, 302);
    updatePads(pads, [ched]);
    // both pads may read "on" but by the same dog → not a valid both-dogs press
    expect(allPadsPressed(pads)).toBe(false);
  });

  it('reads not-pressed when a dog steps off', () => {
    const pads = [pad(200, 300), pad(200, 500)];
    const ched = makeDog('cheddar', 200, 300);
    const coco = makeDog('cocoa', 600, 500); // far from its pad
    updatePads(pads, [ched, coco]);
    expect(allPadsPressed(pads)).toBe(false);
  });
});

describe('boost-jump (one dog launches the other)', () => {
  it('is eligible only with a booster on the pad and a grounded jumper nearby', () => {
    const padPt = { x: 400, y: 400 };
    const booster = makeDog('cheddar', 400, 400); // on the pad
    const jumper = makeDog('cocoa', 440, 400); // within reach
    expect(canBoost(booster, jumper, padPt)).toBe(true);

    const farJumper = makeDog('cocoa', 800, 400);
    expect(canBoost(booster, farJumper, padPt)).toBe(false);

    const offPadBooster = makeDog('cheddar', 600, 400);
    expect(canBoost(offPadBooster, jumper, padPt)).toBe(false);
  });

  it('launches the jumper into a longer, higher arc than a solo jump', () => {
    const jumper = makeDog('cocoa', 440, 400);
    boostLaunch(jumper, { x: 600, y: 400 });
    expect(jumper.jumpT).toBeCloseTo(JUMP.duration * GATES.boostJumpMult, 5);
    expect(jumper.jumpT).toBeGreaterThan(JUMP.duration); // taller than a normal jump
    expect(jumper.vx).toBeGreaterThan(0); // launched toward the target
  });
});

describe('distract + grab', () => {
  it('a threat is distracted only while a dog is within range', () => {
    const threat = { x: 500, y: 300 };
    const near = makeDog('cheddar', 520, 300);
    const far = makeDog('cheddar', 900, 300);
    expect(isDistracted(threat, [near])).toBe(true);
    expect(isDistracted(threat, [far])).toBe(false);
  });
});
