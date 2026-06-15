/**
 * M10 — input layer: the pure `computeIntent` key-set gating (P1 WASD vs P2 arrows) and
 * `GamepadSource` slot selection (each player reads its own connected controller).
 */
import { describe, it, expect, vi } from 'vitest';
import { computeIntent } from '../../src/core/input.js';
import { GamepadSource } from '../../src/core/gamepad.js';
import { INPUT } from '../../src/config/balance.js';

const DZ = INPUT.gamepadDeadzone;
const pos = { x: 0, y: 0 };

describe('computeIntent key-set gating', () => {
  it('defaults to both WASD and arrows (solo P1)', () => {
    expect(computeIntent({ d: true }, null, pos)).toMatchObject({ ax: 1, ay: 0 });
    expect(computeIntent({ arrowright: true }, null, pos)).toMatchObject({ ax: 1, ay: 0 });
  });

  it('P1 in two-player mode ignores the arrow keys (they belong to P2)', () => {
    const p1 = computeIntent({ arrowright: true }, null, pos, { arrows: false });
    expect(p1.ax).toBe(0);
    // …but still reads WASD
    expect(computeIntent({ d: true }, null, pos, { arrows: false }).ax).toBe(1);
  });

  it('P2 reads arrows only, not WASD', () => {
    const opts = { wasd: false };
    expect(computeIntent({ arrowup: true }, null, pos, opts)).toMatchObject({ ay: -1 });
    expect(computeIntent({ w: true }, null, pos, opts).ay).toBe(0);
  });
});

/** Minimal stub matching the slice of the Gamepad API GamepadSource reads. */
function stubPad(axes: number[]): Gamepad {
  return { axes, buttons: [] } as unknown as Gamepad;
}

function withGamepads(pads: (Gamepad | null)[], fn: () => void): void {
  vi.stubGlobal('navigator', { getGamepads: () => pads });
  try {
    fn();
  } finally {
    vi.unstubAllGlobals();
  }
}

describe('GamepadSource slot selection (M10 two controllers)', () => {
  it('slot 0 reads the first connected pad, slot 1 the second', () => {
    const p1 = new GamepadSource(0);
    const p2 = new GamepadSource(1);
    // pad A pushes right, pad B pushes left
    withGamepads([stubPad([1, 0]), stubPad([-1, 0])], () => {
      expect(p1.poll(DZ).intent?.ax).toBe(1);
      expect(p2.poll(DZ).intent?.ax).toBe(-1);
    });
  });

  it('slot 1 is inert until a second pad connects (gaps skipped by connection order)', () => {
    const p2 = new GamepadSource(1);
    withGamepads([stubPad([1, 0])], () => {
      expect(p2.poll(DZ).connected).toBe(false);
    });
    // a null hole then one real pad still counts as the *first* connected → slot 1 empty
    withGamepads([null, stubPad([1, 0])], () => {
      expect(p2.poll(DZ).connected).toBe(false);
    });
    // two real pads → slot 1 now reads the second
    withGamepads([stubPad([0, 0]), stubPad([0, 1])], () => {
      expect(p2.poll(DZ).connected).toBe(true);
      expect(p2.poll(DZ).intent?.ay).toBe(1);
    });
  });
});
