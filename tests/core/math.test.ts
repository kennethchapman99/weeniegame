import { describe, it, expect } from 'vitest';
import { clamp, dist, shade, lerp } from '../../src/core/math.js';

describe('math helpers', () => {
  it('clamps', () => {
    expect(clamp(5, 0, 10)).toBe(5);
    expect(clamp(-3, 0, 10)).toBe(0);
    expect(clamp(99, 0, 10)).toBe(10);
  });

  it('lerps', () => {
    expect(lerp(0, 10, 0.5)).toBe(5);
    expect(lerp(10, 20, 0)).toBe(10);
  });

  it('measures distance', () => {
    expect(dist({ x: 0, y: 0 }, { x: 3, y: 4 })).toBe(5);
  });

  it('shades hex toward black/white and clamps channels', () => {
    expect(shade('#808080', 0)).toBe('#808080');
    expect(shade('#000000', -50)).toBe('#000000'); // clamps at 0
    expect(shade('#ffffff', 50)).toBe('#ffffff'); // clamps at 255
    expect(shade('#102030', 16)).toBe('#203040');
  });
});
