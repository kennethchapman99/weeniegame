import { describe, it, expect } from 'vitest';
import { computeViewport, screenToWorld, worldToScreen } from '../../src/core/camera.js';
import { WORLD } from '../../src/config/balance.js';

describe('camera viewport math', () => {
  it('caps dpr at 3', () => {
    expect(computeViewport(960, 600, 5).dpr).toBe(3);
    expect(computeViewport(960, 600, 2.5).dpr).toBe(2.5);
    expect(computeViewport(960, 600, 0).dpr).toBe(1); // 0 -> fallback 1
  });

  it('letterboxes by the limiting axis and centres', () => {
    // Wider-than-world viewport: limited by height, pillarboxed (offX > 0)
    const wide = computeViewport(1600, 600, 1);
    expect(wide.scale).toBeCloseTo(1, 6);
    expect(wide.offX).toBeGreaterThan(0);
    expect(wide.offY).toBeCloseTo(0, 6);

    // Taller-than-world (portrait): limited by width, letterboxed (offY > 0)
    const tall = computeViewport(480, 1200, 2);
    expect(tall.scale).toBeCloseTo(0.5, 6);
    expect(tall.offY).toBeGreaterThan(0);
  });

  it('round-trips screen<->world across viewport sizes and dpr (the foundational invariant)', () => {
    const cases: Array<[number, number, number]> = [
      [390, 844, 3], // mobile portrait, dpr 3
      [1440, 900, 2], // desktop
      [960, 600, 1], // exact
      [1600, 600, 1.5], // pillarboxed
    ];
    const points = [
      { x: 0, y: 0 },
      { x: WORLD.w, y: WORLD.h },
      { x: 480, y: 300 },
      { x: 137.5, y: 412.3 },
    ];
    for (const [vw, vh, dprIn] of cases) {
      const v = computeViewport(vw, vh, dprIn);
      for (const p of points) {
        const s = worldToScreen(p, v);
        const back = screenToWorld(s.x, s.y, v);
        expect(back.x).toBeCloseTo(p.x, 6);
        expect(back.y).toBeCloseTo(p.y, 6);
      }
    }
  });
});
