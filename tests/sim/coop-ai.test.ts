/**
 * M13 — the mission-aware AI partner (solo co-op fallback). With partner='ai' the sibling must
 * cooperate with each mission's objectives: cover the free pad, distract the cat, brace the boost
 * pad, and play its asymmetric kitchen role. Driven headlessly by parking the human player and
 * letting the AI do its job.
 */
import { describe, it, expect } from 'vitest';
import { makeRng } from '../../src/core/rng.js';
import { makeGameState } from '../../src/state/gameState.js';
import { updateGame, beginScene } from '../../src/state/sceneManager.js';
import { GATES } from '../../src/config/balance.js';

const DT = 1 / 60;
const noIntent = { ax: 0, ay: 0, arrive: 0 };

function startSolo(idx: number, seed = 1) {
  const s = makeGameState(makeRng(seed));
  s.mode = 'coop';
  s.partner = 'ai';
  s.sceneIdx = idx;
  beginScene(s);
  for (let i = 0; i < 200 && s.phase !== 'play'; i++) updateGame(s, noIntent, false, false, DT);
  return s;
}
function run(s: ReturnType<typeof startSolo>, n: number, hold: (s: ReturnType<typeof startSolo>) => void) {
  for (let i = 0; i < n; i++) {
    hold(s);
    updateGame(s, noIntent, false, false, DT);
  }
}

describe('mission-aware AI partner (M13 solo co-op)', () => {
  it('Gate: the AI covers the other pad so the gate opens', () => {
    const s = startSolo(0);
    run(s, 220, (s) => {
      s.dogs[s.playerId].x = 250; // player holds pad 0
      s.dogs[s.playerId].y = 300;
    });
    expect(s.mission!.gate!.open).toBe(true);
  });

  it('Sneak: the AI distracts the cat so the player can grab a treat', () => {
    const s = startSolo(1);
    const data = s.mission!.data as { treats: { x: number; y: number; got: boolean }[] };
    // let the AI walk to the cat first (player idles far off)
    run(s, 170, (s) => {
      s.dogs[s.playerId].x = 160;
      s.dogs[s.playerId].y = 470;
    });
    // now the player grabs while the AI keeps the cat busy
    run(s, 12, (s) => {
      s.dogs[s.playerId].x = data.treats[0]!.x;
      s.dogs[s.playerId].y = data.treats[0]!.y;
    });
    expect(data.treats[0]!.got).toBe(true);
  });

  it('Creek: the AI braces the boost pad', () => {
    const s = startSolo(2);
    run(s, 170, (s) => {
      s.dogs[s.playerId].x = 200;
      s.dogs[s.playerId].y = 360;
    });
    const aiDog = s.dogs[s.aiId];
    expect(Math.hypot(aiDog.x - 372, aiDog.y - 405)).toBeLessThan(GATES.padR + 12);
  });

  it('Kitchen: the AI eater gobbles a fallen snack', () => {
    const s = startSolo(3); // player = cheddar (knocker), AI = cocoa (eater)
    (s.mission!.data as { floor: { x: number; y: number; ttl: number }[] }).floor.push({ x: 500, y: 384, ttl: 6 });
    run(s, 140, (s) => {
      s.dogs[s.playerId].x = 360;
      s.dogs[s.playerId].y = 470;
    });
    expect((s.mission!.data as { eaten: number }).eaten).toBeGreaterThanOrEqual(1);
  });

  it('Kitchen: the AI knocker chair-leaps a snack down when it plays Cheddar', () => {
    const s = startSolo(3);
    s.playerId = 'cocoa'; // player picks Cocoa → the AI is Cheddar, the knocker
    s.aiId = 'cheddar';
    run(s, 260, (s) => {
      s.dogs[s.playerId].x = 560;
      s.dogs[s.playerId].y = 470;
    });
    expect((s.mission!.data as { onCounter: { knocked: boolean }[] }).onCounter.some((f) => f.knocked)).toBe(true);
  });
});
