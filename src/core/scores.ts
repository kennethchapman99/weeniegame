/**
 * core/scores.ts — per-mission best combined-score + best star rating, persisted to
 * localStorage. Host-only (kept out of the deterministic sim). Fail-soft: where storage is
 * blocked (e.g. the chat artifact sandbox) reads return the default and writes are dropped, so
 * the game still runs — it just won't remember between sessions.
 */

export interface Best {
  score: number;
  stars: number;
}

const KEY = (mission: string): string => `cc-best-${mission}`;

/** The stored best for a mission (zeroes if none / storage unavailable). */
export function loadBest(mission: string): Best {
  try {
    const raw = globalThis.localStorage?.getItem(KEY(mission));
    if (raw) {
      const b = JSON.parse(raw) as Partial<Best>;
      return { score: b.score ?? 0, stars: b.stars ?? 0 };
    }
  } catch {
    /* ignore */
  }
  return { score: 0, stars: 0 };
}

/** Record a run; keeps the best score + best stars seen. Returns the (new) best. */
export function saveBest(mission: string, score: number, stars: number): Best {
  const prev = loadBest(mission);
  const next: Best = { score: Math.max(prev.score, score), stars: Math.max(prev.stars, stars) };
  try {
    globalThis.localStorage?.setItem(KEY(mission), JSON.stringify(next));
  } catch {
    /* ignore */
  }
  return next;
}
