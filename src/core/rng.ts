/**
 * core/rng.ts — seedable RNG. ALL gameplay randomness routes through an instance of
 * this so the headless sim harness can reproduce runs (CLAUDE.md determinism rule).
 *
 * Algorithm: mulberry32 — tiny, fast, good enough for a game, fully deterministic
 * from a 32-bit seed.
 */

export interface Rng {
  /** next float in [0, 1) */
  next(): number;
  /** float in [a, b) — the prototype's `rnd(a, b)` */
  range(a: number, b: number): number;
  /** integer in [a, b] inclusive */
  int(a: number, b: number): number;
  /** uniform pick from a non-empty array */
  pick<T>(arr: readonly T[]): T;
  /** current 32-bit state, for snapshotting */
  readonly seed: number;
}

export function makeRng(seed: number): Rng {
  let s = seed >>> 0;
  const next = (): number => {
    s |= 0;
    s = (s + 0x6d2b79f5) | 0;
    let t = Math.imul(s ^ (s >>> 15), 1 | s);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
  return {
    next,
    range: (a, b) => a + next() * (b - a),
    int: (a, b) => a + Math.floor(next() * (b - a + 1)),
    pick: <T>(arr: readonly T[]): T => {
      if (arr.length === 0) throw new Error('rng.pick: empty array');
      return arr[Math.floor(next() * arr.length)] as T;
    },
    get seed() {
      return s >>> 0;
    },
  };
}
