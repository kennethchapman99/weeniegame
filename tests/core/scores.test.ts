/**
 * core/scores.ts — per-mission best score/stars persistence. Uses a stubbed localStorage so the
 * keep-the-best logic is verified, and confirms it fail-softs when storage is unavailable.
 */
import { describe, it, expect, vi, afterEach } from 'vitest';
import { loadBest, saveBest } from '../../src/core/scores.js';

function memoryStorage() {
  const map = new Map<string, string>();
  return {
    getItem: (k: string) => map.get(k) ?? null,
    setItem: (k: string, v: string) => void map.set(k, v),
  };
}

afterEach(() => vi.unstubAllGlobals());

describe('scores persistence', () => {
  it('returns zeroes when nothing is stored', () => {
    vi.stubGlobal('localStorage', memoryStorage());
    expect(loadBest('mission-gate')).toEqual({ score: 0, stars: 0 });
  });

  it('keeps the best score and best stars across runs', () => {
    vi.stubGlobal('localStorage', memoryStorage());
    expect(saveBest('m', 40, 2)).toEqual({ score: 40, stars: 2 });
    // a worse run doesn't lower either field
    expect(saveBest('m', 25, 1)).toEqual({ score: 40, stars: 2 });
    // a better score and better stars both stick
    expect(saveBest('m', 80, 3)).toEqual({ score: 80, stars: 3 });
    expect(loadBest('m')).toEqual({ score: 80, stars: 3 });
  });

  it('fail-softs (no throw, default value) when storage is unavailable', () => {
    vi.stubGlobal('localStorage', undefined);
    expect(() => saveBest('m', 10, 1)).not.toThrow();
    expect(loadBest('m')).toEqual({ score: 0, stars: 0 });
  });
});
