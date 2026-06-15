/**
 * M13 — "Sneak the Snack" distract+grab mission + the co-op campaign chain.
 * Driven headlessly: a lone pup gets lunged; a distracted cat lets the teammate grab; collecting
 * all treats wins; and a gate-mission success advances to this mission.
 */
import { describe, it, expect } from 'vitest';
import { makeRng } from '../../src/core/rng.js';
import { makeGameState } from '../../src/state/gameState.js';
import { startGame, updateGame, beginScene, advanceCoop, coopHasNext } from '../../src/state/sceneManager.js';

const DT = 1 / 60;
const noIntent = { ax: 0, ay: 0, arrive: 0 };

interface SneakData {
  guard: { x: number; y: number };
  treats: { x: number; y: number; got: boolean }[];
}

/** Sit at the start of the sneak mission (registry index 1), in 'play'. */
function startSneak(seed = 1) {
  const s = makeGameState(makeRng(seed));
  s.mode = 'coop';
  s.sceneIdx = 1;
  beginScene(s);
  for (let i = 0; i < 200 && s.phase !== 'play'; i++) updateGame(s, noIntent, false, false, DT);
  return s;
}

const sneak = (s: ReturnType<typeof startSneak>) => s.mission!.data as SneakData;
const step = (s: ReturnType<typeof startSneak>) => updateGame(s, noIntent, false, false, DT);

describe('Sneak the Snack (M13 distract+grab)', () => {
  it('a lone pup grabbing gets lunged — no treat, gets stunned', () => {
    const s = startSneak();
    const d = sneak(s);
    const t = d.treats[0]!;
    s.dogs.cheddar.x = t.x;
    s.dogs.cheddar.y = t.y; // grabber on the treat
    s.dogs.cocoa.x = 160;
    s.dogs.cocoa.y = 470; // teammate far away → no distraction
    step(s);
    expect(t.got).toBe(false);
    expect(s.dogs.cheddar.mode).toBe('stunned');
  });

  it('grabs the treat when a teammate distracts the cat', () => {
    const s = startSneak();
    const d = sneak(s);
    const t = d.treats[0]!;
    s.dogs.cheddar.x = t.x;
    s.dogs.cheddar.y = t.y; // grabber
    s.dogs.cocoa.x = d.guard.x + 10;
    s.dogs.cocoa.y = d.guard.y; // teammate in the cat's face
    const before = s.mission!.combinedScore;
    step(s);
    expect(t.got).toBe(true);
    expect(s.mission!.combinedScore).toBeGreaterThan(before);
  });

  it('collecting all three treats wins the mission', () => {
    const s = startSneak();
    const d = sneak(s);
    // cocoa permanently distracts the cat
    s.dogs.cocoa.x = d.guard.x + 10;
    s.dogs.cocoa.y = d.guard.y;
    for (const t of d.treats) {
      s.dogs.cheddar.x = t.x;
      s.dogs.cheddar.y = t.y;
      // keep cocoa pinned on the cat each step
      s.dogs.cocoa.x = d.guard.x + 10;
      s.dogs.cocoa.y = d.guard.y;
      step(s);
    }
    step(s); // resolve success
    expect(d.treats.every((t) => t.got)).toBe(true);
    expect(s.mission!.status).toBe('success');
    expect(s.phase).toBe('end');
  });
});

describe('co-op campaign chain', () => {
  it('a gate-mission success advances to the next mission', () => {
    const s = makeGameState(makeRng(1));
    s.mode = 'coop';
    startGame(s); // mission 0 = the gate
    for (let i = 0; i < 200 && s.phase !== 'play'; i++) updateGame(s, noIntent, false, false, DT);
    expect(s.mission!.key).toBe('mission-gate');
    expect(coopHasNext(s)).toBe(true);

    // clear the gate mission
    s.dogs.cheddar.x = 250;
    s.dogs.cheddar.y = 300;
    s.dogs.cocoa.x = 250;
    s.dogs.cocoa.y = 500;
    step(s); // gate opens
    s.dogs.cheddar.x = 820;
    s.dogs.cheddar.y = 395;
    s.dogs.cocoa.x = 820;
    s.dogs.cocoa.y = 415;
    step(s); // both in den → success
    expect(s.mission!.status).toBe('success');

    advanceCoop(s);
    expect(s.sceneIdx).toBe(1);
    expect(s.mission!.key).toBe('mission-sneak');
    expect(coopHasNext(s)).toBe(true); // the creek mission still follows
  });
});
