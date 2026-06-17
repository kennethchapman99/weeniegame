/**
 * Buried Treasure co-op mission. Cocoa reveals suspicious mounds by sniffing; only Cheddar's
 * jump digs revealed treats loose; collecting the target number of dug treats wins.
 */
import { describe, it, expect } from 'vitest';
import { makeRng } from '../../src/core/rng.js';
import { makeGameState } from '../../src/state/gameState.js';
import { updateGame, beginScene } from '../../src/state/sceneManager.js';
import { BURROW } from '../../src/config/balance.js';

const DT = 1 / 60;
const noIntent = { ax: 0, ay: 0, arrive: 0 };

interface Burrow {
  x: number;
  y: number;
  treat: boolean;
  state: 'hidden' | 'revealing' | 'revealed' | 'dug' | 'collected' | 'empty';
  sniff: number;
}
interface BurrowData {
  burrows: Burrow[];
  treats: { x: number; y: number; bob: number }[];
  found: number;
}

function startBurrow(seed = 1) {
  const s = makeGameState(makeRng(seed));
  s.mode = 'coop';
  s.partner = 'human';
  s.sceneIdx = 4;
  beginScene(s);
  for (let i = 0; i < 200 && s.phase !== 'play'; i++) updateGame(s, noIntent, false, false, DT);
  return s;
}
const data = (s: ReturnType<typeof startBurrow>) => s.mission!.data as BurrowData;
const step = (s: ReturnType<typeof startBurrow>) => updateGame(s, noIntent, false, false, DT);

describe('Buried Treasure co-op mission', () => {
  it('Cocoa sniffing a treat mound reveals it', () => {
    const s = startBurrow();
    const b = data(s).burrows.find((burrow) => burrow.treat)!;
    s.dogs.cocoa.x = b.x;
    s.dogs.cocoa.y = b.y;
    for (let i = 0; i < Math.ceil((BURROW.sniffTime + 0.05) / DT); i++) step(s);
    expect(b.state).toBe('revealed');
  });

  it('Cheddar jump-digs a revealed mound into a loose treat', () => {
    const s = startBurrow();
    const b = data(s).burrows.find((burrow) => burrow.treat)!;
    b.state = 'revealed';
    b.sniff = BURROW.sniffTime;
    s.dogs.cheddar.x = b.x + 50;
    s.dogs.cheddar.y = b.y;
    s.dogs.cheddar.jumpT = 0.3;
    s.dogs.cocoa.x = 100;
    s.dogs.cocoa.y = 520;
    step(s);
    expect(b.state).toBe('dug');
    expect(data(s).treats.length).toBe(1);
  });

  it('Cocoa cannot dig a revealed mound', () => {
    const s = startBurrow();
    const b = data(s).burrows.find((burrow) => burrow.treat)!;
    b.state = 'revealed';
    b.sniff = BURROW.sniffTime;
    s.dogs.cocoa.x = b.x;
    s.dogs.cocoa.y = b.y;
    s.dogs.cocoa.jumpT = 0.3;
    s.dogs.cheddar.x = 100;
    s.dogs.cheddar.y = 520;
    step(s);
    expect(b.state).toBe('revealed');
    expect(data(s).treats.length).toBe(0);
  });

  it('collecting the target number of dug treats wins', () => {
    const s = startBurrow();
    const d = data(s);
    const treatBurrows = d.burrows.filter((b) => b.treat).slice(0, BURROW.target);
    for (const b of treatBurrows) {
      d.treats.push({ x: b.x, y: b.y - 20, bob: 0 });
      s.dogs.cocoa.x = b.x;
      s.dogs.cocoa.y = b.y - 20;
      step(s);
    }
    expect(d.found).toBe(BURROW.target);
    expect(s.mission!.status).toBe('success');
  });
});
