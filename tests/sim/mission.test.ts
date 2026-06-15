/**
 * M12 — the co-op mission framework + the minimal "Through the Gate" mission, driven headlessly:
 * the gate opens only with both dogs on the pads, both must reach the den, the mission resolves
 * to success (with stars + combined score) or fails on the timer, and objective completion is a
 * single, idempotent mutation.
 */
import { describe, it, expect } from 'vitest';
import { makeRng } from '../../src/core/rng.js';
import { makeGameState } from '../../src/state/gameState.js';
import { startGame, updateGame } from '../../src/state/sceneManager.js';
import { completeObjective, addCombined } from '../../src/systems/mission.js';

const DT = 1 / 60;
const noIntent = { ax: 0, ay: 0, arrive: 0 };

/** A co-op game sat at the start of the gate mission (phase 'play'). */
function startGateMission(seed = 1) {
  const s = makeGameState(makeRng(seed));
  s.mode = 'coop';
  s.partner = 'human'; // pin the sibling (no cooperative AI) so manual positioning is authoritative
  startGame(s);
  for (let i = 0; i < 200 && s.phase !== 'play'; i++) updateGame(s, noIntent, false, false, DT);
  return s;
}

function step(s: ReturnType<typeof startGateMission>) {
  updateGame(s, noIntent, false, false, DT);
}

describe('mission framework wiring (M12)', () => {
  it('co-op mode installs the gate mission with two active objectives', () => {
    const s = startGateMission();
    expect(s.phase).toBe('play');
    expect(s.mission).not.toBeNull();
    expect(s.mission!.objectives).toHaveLength(2);
    expect(s.mission!.status).toBe('active');
    expect(s.mission!.gate!.open).toBe(false);
  });

  it('versus mode is unaffected (no mission installed)', () => {
    const s = makeGameState(makeRng(1)); // mode defaults to 'versus'
    startGame(s);
    for (let i = 0; i < 200 && s.phase !== 'play'; i++) updateGame(s, noIntent, false, false, DT);
    expect(s.mission).toBeNull();
    expect(s.sceneKey).toBe('yard');
  });
});

describe('the gate: needs both dogs', () => {
  it('stays shut with only one dog on a pad', () => {
    const s = startGateMission();
    s.dogs.cheddar.x = 250;
    s.dogs.cheddar.y = 300; // on pad 0
    s.dogs.cocoa.x = 140;
    s.dogs.cocoa.y = 470; // nowhere near pad 1
    for (let i = 0; i < 30; i++) step(s);
    expect(s.mission!.gate!.open).toBe(false);
    expect(s.mission!.objectives[0]!.done).toBe(false);
  });

  it('latches open when both dogs press the pads together', () => {
    const s = startGateMission();
    s.dogs.cheddar.x = 250;
    s.dogs.cheddar.y = 300; // pad 0
    s.dogs.cocoa.x = 250;
    s.dogs.cocoa.y = 500; // pad 1
    step(s);
    expect(s.mission!.objectives[0]!.done).toBe(true);
    expect(s.mission!.gate!.open).toBe(true);
    // …and stays latched after they step off
    s.dogs.cheddar.x = 140;
    s.dogs.cocoa.x = 140;
    step(s);
    expect(s.mission!.gate!.open).toBe(true);
  });

  it('a closed gate is a wall — dogs cannot cross to the right', () => {
    const s = startGateMission();
    s.dogs.cheddar.x = 515; // shoved past the barrier line (520) while it's closed
    s.dogs.cheddar.y = 400;
    step(s);
    expect(s.dogs.cheddar.x).toBeLessThanOrEqual(520 - 24 + 0.001);
  });
});

describe('mission outcome', () => {
  it('succeeds when the gate is open and both pups reach the den (combined score + stars)', () => {
    const s = startGateMission();
    // open the gate
    s.dogs.cheddar.x = 250;
    s.dogs.cheddar.y = 300;
    s.dogs.cocoa.x = 250;
    s.dogs.cocoa.y = 500;
    step(s);
    expect(s.mission!.gate!.open).toBe(true);
    // both into the den
    s.dogs.cheddar.x = 820;
    s.dogs.cheddar.y = 395;
    s.dogs.cocoa.x = 820;
    s.dogs.cocoa.y = 415;
    step(s);
    expect(s.mission!.status).toBe('success');
    expect(s.phase).toBe('end');
    // 5 (gate) + 10 (den) + a time bonus for finishing fast
    expect(s.mission!.combinedScore).toBeGreaterThanOrEqual(15);
    expect(s.mission!.stars).toBe(3); // finished well under the 3★ time
  });

  it('fails when the timer runs out before the objectives are met', () => {
    const s = startGateMission();
    s.mission!.timeLimit = 0.25; // force a quick fail
    for (let i = 0; i < 30 && s.mission!.status === 'active'; i++) step(s);
    expect(s.mission!.status).toBe('fail');
    expect(s.phase).toBe('end');
  });
});

describe('single-point mutation discipline', () => {
  it('completeObjective is idempotent — reward counts once', () => {
    const s = startGateMission();
    const before = s.mission!.combinedScore;
    completeObjective(s, 0);
    const after = s.mission!.combinedScore;
    completeObjective(s, 0); // second call must not re-award
    expect(s.mission!.combinedScore).toBe(after);
    expect(after).toBe(before + s.mission!.objectives[0]!.reward);
  });

  it('addCombined is the single combined-score accumulator', () => {
    const s = startGateMission();
    const before = s.mission!.combinedScore;
    addCombined(s, 7);
    expect(s.mission!.combinedScore).toBe(before + 7);
  });
});
