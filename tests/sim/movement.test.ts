import { describe, it, expect } from 'vitest';
import { makeDog } from '../../src/state/dog.js';
import { moveDog } from '../../src/systems/movement.js';
import { computeIntent } from '../../src/core/input.js';
import { BOUNDS, SPEED } from '../../src/config/balance.js';

const DT = 1 / 60;

/** Drive a dog toward a touch target for N steps, as the real update loop would. */
function dragToward(target: { x: number; y: number }, steps: number) {
  const d = makeDog('cheddar', 300, 400, 0);
  for (let i = 0; i < steps; i++) {
    const intent = computeIntent({}, target, d);
    moveDog(d, intent.ax, intent.ay, DT, intent.arrive);
  }
  return d;
}

describe('movement (M1)', () => {
  it('moves toward a touch target and arrives near it without overshooting', () => {
    const target = { x: 700, y: 400 };
    const d = dragToward(target, 240); // ~4s
    expect(d.x).toBeGreaterThan(650);
    expect(Math.abs(d.x - target.x)).toBeLessThan(15);
    expect(Math.abs(d.y - target.y)).toBeLessThan(15);
  });

  it('reaches roughly the right cruise speed (NOT 17x too slow)', () => {
    // Far target, no arrival damping: peak per-step displacement should approach
    // free speed * dt * 60 = 4.4 px/step, an order of magnitude above the old bug (~0.26).
    const d = makeDog('cheddar', 100, 400, 0);
    let peak = 0;
    for (let i = 0; i < 60; i++) {
      const px = d.x;
      const intent = computeIntent({}, { x: 900, y: 400 }, d);
      moveDog(d, intent.ax, intent.ay, DT, intent.arrive);
      peak = Math.max(peak, d.x - px);
    }
    expect(peak).toBeGreaterThan(3.5);
    expect(peak).toBeLessThanOrEqual(SPEED.free + 0.01);
  });

  it('does not jitter on the spot once parked at the target', () => {
    const target = { x: 500, y: 400 };
    const d = dragToward(target, 300);
    // Run more steps sitting on target; record max movement per step.
    let maxStep = 0;
    for (let i = 0; i < 120; i++) {
      const px = d.x;
      const py = d.y;
      const intent = computeIntent({}, target, d);
      moveDog(d, intent.ax, intent.ay, DT, intent.arrive);
      maxStep = Math.max(maxStep, Math.hypot(d.x - px, d.y - py));
    }
    expect(maxStep).toBeLessThan(0.05); // velocity hard-stopped: effectively still
    expect(d.vx).toBe(0);
    expect(d.vy).toBe(0);
  });

  it('keyboard input drives full-speed (arrive=1) movement', () => {
    const d = makeDog('cheddar', 300, 400, 0);
    const intent = computeIntent({ d: true }, null, d); // hold right
    expect(intent).toMatchObject({ ax: 1, ay: 0, arrive: 1 });
    for (let i = 0; i < 30; i++) moveDog(d, 1, 0, DT, 1);
    expect(d.x).toBeGreaterThan(300);
    expect(d.face).toBe(1);
  });

  it('flips facing past the threshold and clamps to play bounds', () => {
    const d = makeDog('cheddar', 300, 400, 0);
    for (let i = 0; i < 20; i++) moveDog(d, -1, 0, DT, 1); // go left
    expect(d.face).toBe(-1);
    // Drive into the corner; must clamp inside BOUNDS.
    for (let i = 0; i < 600; i++) moveDog(d, -1, -1, DT, 1);
    expect(d.x).toBeGreaterThanOrEqual(BOUNDS.minX);
    expect(d.y).toBeGreaterThanOrEqual(BOUNDS.minY);
  });
});
