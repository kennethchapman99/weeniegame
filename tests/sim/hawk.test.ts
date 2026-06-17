/**
 * M13 — "Stay Together" survive mission. Driven headlessly: lasting the time wins (survive
 * inverts the timer), a lone pup caught by a dive fails the mission, and huddling keeps the
 * hawk from committing.
 */
import { describe, it, expect } from 'vitest';
import { makeRng } from '../../src/core/rng.js';
import { makeGameState } from '../../src/state/gameState.js';
import { updateGame, beginScene } from '../../src/state/sceneManager.js';
import { HAWK } from '../../src/config/balance.js';

const DT = 1 / 60;
const noIntent = { ax: 0, ay: 0, arrive: 0 };

interface HawkData {
  x: number;
  y: number;
  state: string;
  targetId: 'cheddar' | 'cocoa';
}

function startHawk(seed = 1) {
  const s = makeGameState(makeRng(seed));
  s.mode = 'coop';
  s.partner = 'human'; // pin the pups where we place them
  s.sceneIdx = 6; // co-op registry: …burrow(4), cleaning(5), hawk(6)
  beginScene(s);
  for (let i = 0; i < 200 && s.phase !== 'play'; i++) updateGame(s, noIntent, false, false, DT);
  return s;
}
const hawk = (s: ReturnType<typeof startHawk>) => s.mission!.data as HawkData;
const step = (s: ReturnType<typeof startHawk>, hold: (s: ReturnType<typeof startHawk>) => void) => {
  hold(s);
  updateGame(s, noIntent, false, false, DT);
};

describe('Stay Together (M13 survive)', () => {
  it('is a survive mission (timeout = success, not fail)', () => {
    const s = startHawk();
    expect(s.mission!.surviveMode).toBe(true);
  });

  it('huddling keeps both pups safe for the whole mission → success', () => {
    const s = startHawk();
    // keep the pups glued together (well inside huddleR) the entire time
    for (let i = 0; i < 60 * 46 && s.mission!.status === 'active'; i++) {
      step(s, (s) => {
        s.dogs.cheddar.x = 460;
        s.dogs.cheddar.y = 430;
        s.dogs.cocoa.x = 470;
        s.dogs.cocoa.y = 430;
      });
    }
    expect(s.mission!.status).toBe('success');
    expect(s.phase).toBe('end');
  });

  it('a lone pup caught by a dive fails the mission', () => {
    const s = startHawk();
    const h = hawk(s);
    // force a dive straight at a stranded Cheddar, and park the hawk on top of him
    h.state = 'dive';
    h.targetId = 'cheddar';
    s.dogs.cheddar.x = 200;
    s.dogs.cheddar.y = 300; // far from Cocoa (no huddle)
    s.dogs.cocoa.x = 800;
    s.dogs.cocoa.y = 500;
    h.x = 200;
    h.y = 300 - HAWK.grabR + 4; // within grab range, not huddled, Cheddar grounded
    step(s, () => {});
    expect(s.mission!.status).toBe('fail');
    expect(s.carriedDog).toBe('cheddar');
  });

  it('a dive is shrugged off if the pups are huddled when it connects', () => {
    const s = startHawk();
    const h = hawk(s);
    h.state = 'dive';
    h.targetId = 'cheddar';
    s.dogs.cheddar.x = 460;
    s.dogs.cheddar.y = 430;
    s.dogs.cocoa.x = 470; // huddled
    s.dogs.cocoa.y = 430;
    h.x = 460;
    h.y = 430 - HAWK.grabR + 4;
    step(s, () => {
      s.dogs.cheddar.x = 460;
      s.dogs.cheddar.y = 430;
      s.dogs.cocoa.x = 470;
      s.dogs.cocoa.y = 430;
    });
    expect(s.mission!.status).toBe('active'); // not caught
  });
});
