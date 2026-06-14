import { describe, it, expect } from 'vitest';
import { makeRng } from '../../src/core/rng.js';

describe('seedable rng', () => {
  it('is deterministic for a given seed', () => {
    const a = makeRng(12345);
    const b = makeRng(12345);
    const seqA = Array.from({ length: 8 }, () => a.next());
    const seqB = Array.from({ length: 8 }, () => b.next());
    expect(seqA).toEqual(seqB);
  });

  it('differs across seeds', () => {
    const a = makeRng(1);
    const b = makeRng(2);
    expect(a.next()).not.toBe(b.next());
  });

  it('stays in [0,1) and respects range/int bounds', () => {
    const r = makeRng(7);
    for (let i = 0; i < 1000; i++) {
      const f = r.next();
      expect(f).toBeGreaterThanOrEqual(0);
      expect(f).toBeLessThan(1);
      const rg = r.range(5, 9);
      expect(rg).toBeGreaterThanOrEqual(5);
      expect(rg).toBeLessThan(9);
      const n = r.int(1, 6);
      expect(n).toBeGreaterThanOrEqual(1);
      expect(n).toBeLessThanOrEqual(6);
    }
  });

  it('pick returns an element and throws on empty', () => {
    const r = makeRng(99);
    expect([10, 20, 30]).toContain(r.pick([10, 20, 30]));
    expect(() => r.pick([])).toThrow();
  });
});
