import { describe, it, expect, vi } from 'vitest';
import { stickIntent, pressedEdges, GamepadSource, PAD } from '../../src/core/gamepad.js';
import { INPUT } from '../../src/config/balance.js';

const DZ = INPUT.gamepadDeadzone;

describe('stickIntent', () => {
  it('reads no input inside the deadzone', () => {
    const i = stickIntent(0.1, -0.05, DZ);
    expect(i).toEqual({ ax: 0, ay: 0, arrive: 0 });
  });

  it('preserves stick direction in ax/ay (magnitude ignored downstream)', () => {
    const i = stickIntent(0.8, -0.6, DZ);
    expect(i.ax).toBe(0.8);
    expect(i.ay).toBe(-0.6);
  });

  it('ramps arrive from 0 at the deadzone edge to 1 at full deflection', () => {
    const justInside = stickIntent(DZ + 0.001, 0, DZ);
    expect(justInside.arrive).toBeGreaterThan(0);
    expect(justInside.arrive).toBeLessThan(0.05);

    const full = stickIntent(1, 0, DZ);
    expect(full.arrive).toBeCloseTo(1, 5);
  });

  it('clamps arrive to 1 for over-unit diagonals', () => {
    const i = stickIntent(1, 1, DZ); // magnitude ~1.41
    expect(i.arrive).toBe(1);
  });
});

describe('pressedEdges', () => {
  it('fires only on the press, not while held', () => {
    expect(pressedEdges([false, false], [true, false])).toEqual([true, false]);
    expect(pressedEdges([true, false], [true, false])).toEqual([false, false]);
  });

  it('does not fire on release', () => {
    expect(pressedEdges([true], [false])).toEqual([false]);
  });
});

/** Minimal stub matching the slice of the Gamepad API GamepadSource reads. */
function stubPad(axes: number[], pressed: boolean[]): Gamepad {
  return {
    axes,
    buttons: pressed.map((p) => ({ pressed: p, touched: p, value: p ? 1 : 0 })),
  } as unknown as Gamepad;
}

function withGamepads(pads: (Gamepad | null)[], fn: () => void): void {
  vi.stubGlobal('navigator', { getGamepads: () => pads });
  try {
    fn();
  } finally {
    vi.unstubAllGlobals();
  }
}

describe('GamepadSource', () => {
  it('is inert with no gamepad connected', () => {
    withGamepads([null], () => {
      const r = new GamepadSource().poll(DZ);
      expect(r).toEqual({ intent: null, wrestle: false, jump: false, connected: false });
    });
  });

  it('reports connected + stick intent and edge-detects buttons across polls', () => {
    const src = new GamepadSource();
    const buttons: boolean[] = [false, false, false, false];

    // Frame 1: stick pushed right, A held (wrestle). First poll adopts state, no phantom edge.
    withGamepads([stubPad([1, 0], setBtn(buttons, PAD.wrestle, true))], () => {
      const r = src.poll(DZ);
      expect(r.connected).toBe(true);
      expect(r.intent?.ax).toBe(1);
      expect(r.wrestle).toBe(false); // no edge on the very first poll
    });

    // Frame 2: A still held → no new edge; B newly pressed → jump edge fires.
    withGamepads([stubPad([1, 0], setBtn(buttons, PAD.jump, true))], () => {
      const r = src.poll(DZ);
      expect(r.wrestle).toBe(false);
      expect(r.jump).toBe(true);
    });
  });
});

function setBtn(arr: boolean[], i: number, v: boolean): boolean[] {
  arr[i] = v;
  return arr.slice();
}
