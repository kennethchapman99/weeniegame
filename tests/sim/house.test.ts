import { describe, it, expect } from 'vitest';
import { makeRng } from '../../src/core/rng.js';
import { makeGameState } from '../../src/state/gameState.js';
import { startGame, updateGame, beginScene } from '../../src/state/sceneManager.js';
import { startTransit, tickTransit, doorTriggers, pushOut } from '../../src/systems/house.js';
import { updateCouch } from '../../src/systems/couch.js';
import { updateSquish } from '../../src/systems/squish.js';
import { HOUSE_MAP } from '../../src/scenes/house/rooms.js';
import { HOUSE } from '../../src/config/balance.js';

const DT = 1 / 60;
const idle = { ax: 0, ay: 0, arrive: 0 };

/** A state sitting in the house round, in the play phase. */
function housePlay(seed = 3) {
  const s = makeGameState(makeRng(seed));
  startGame(s);
  s.sceneIdx = 2; // jump to the house
  beginScene(s);
  for (let i = 0; i < 200 && s.phase !== 'play'; i++) updateGame(s, idle, false, false, DT);
  return s;
}

function stepsToTransit(s: ReturnType<typeof makeGameState>, dogId: 'cheddar' | 'cocoa'): number {
  const d = s.dogs[dogId];
  d.room = 'foyer';
  const stair = HOUSE_MAP.rooms.foyer!.doors.find((dr) => dr.stair)!;
  startTransit(s, d, stair);
  let steps = 0;
  while (d.transit && steps < 600) {
    tickTransit(d, DT);
    steps++;
  }
  return steps;
}

describe('house — stairs (M8)', () => {
  it('Cheddar traverses the stairs faster than Cocoa', () => {
    const s = housePlay();
    const cheddarSteps = stepsToTransit(s, 'cheddar');
    const cocoaSteps = stepsToTransit(s, 'cocoa');
    expect(cheddarSteps).toBeLessThan(cocoaSteps);
    expect(HOUSE.stairTime.cheddar).toBeLessThan(HOUSE.stairTime.cocoa);
  });

  it('a transit drops the dog into the target room', () => {
    const s = housePlay();
    s.sceneKey = 'house';
    const c = s.dogs.cheddar;
    c.room = 'foyer';
    c.mode = 'free';
    const door = HOUSE_MAP.rooms.foyer!.doors.find((dr) => !dr.stair)!; // hallway → family
    c.x = door.rect.x + door.rect.w / 2;
    c.y = door.rect.y + door.rect.h / 2;
    doorTriggers(s);
    expect(c.mode).toBe('transit');
    while (c.transit) tickTransit(c, DT);
    expect(c.room).toBe('family');
  });
});

describe('house — couch & squishmallows (M8)', () => {
  it('holding the dog couch for its hold time scores +5 then goes on cooldown', () => {
    const s = housePlay();
    expect(s.couch).not.toBeNull();
    const couch = s.couch!;
    const c = s.dogs.cheddar;
    c.room = couch.room;
    c.x = couch.x;
    c.y = couch.y;
    c.mode = 'free';
    c.vx = 0;
    c.vy = 0;
    s.dogs.cocoa.room = couch.room === 'foyer' ? 'family' : 'foyer'; // keep the sibling away
    const before = c.score;
    for (let i = 0; i < Math.ceil(HOUSE.couch.hold / DT) + 5; i++) updateCouch(s, DT);
    expect(c.score).toBe(before + HOUSE.couch.reward);
    expect(couch.cool).toBeGreaterThan(0);
  });

  it('napping still on a squishmallow scores +1 and the squish respawns', () => {
    const s = housePlay();
    const sq = s.squishies.find((q) => q.active)!;
    const c = s.dogs.cheddar;
    c.room = sq.room;
    c.x = sq.x;
    c.y = sq.y;
    c.mode = 'free';
    c.vx = 0;
    c.vy = 0;
    s.dogs.cocoa.room = 'foyer';
    s.dogs.cocoa.x = -999;
    const before = c.score;
    for (let i = 0; i < Math.ceil(HOUSE.squish.napTime / DT) + 5; i++) updateSquish(s, DT);
    expect(c.score).toBe(before + 1);
    expect(sq.active).toBe(false); // consumed → respawning
  });
});

describe('house — furniture collision (M8)', () => {
  it('pushOut keeps a dog from sitting inside furniture', () => {
    const s = housePlay();
    const c = s.dogs.cheddar;
    c.room = 'family';
    const o = HOUSE_MAP.rooms.family!.obstacles[0]!; // the big couch (rect)
    // approach from just above the top edge, within the push radius
    c.x = o.kind === 'rect' ? o.x + o.w / 2 : o.x;
    c.y = o.kind === 'rect' ? o.y - 10 : o.y;
    pushOut(c);
    if (o.kind === 'rect') {
      const cx = Math.max(o.x, Math.min(c.x, o.x + o.w));
      const cy = Math.max(o.y, Math.min(c.y, o.y + o.h));
      expect(Math.hypot(c.x - cx, c.y - cy)).toBeGreaterThan(24);
    }
  });
});

describe('house — AI cross-room (M8)', () => {
  it('the AI roams and scores over a full house round (idle player)', () => {
    const s = housePlay(7);
    const aiId = s.aiId;
    const roomsSeen = new Set<string>();
    for (let i = 0; i < 60 * 75 && s.phase === 'play'; i++) {
      updateGame(s, idle, false, false, DT);
      const r = s.dogs[aiId].room;
      if (r) roomsSeen.add(r);
    }
    expect(s.dogs[aiId].score).toBeGreaterThan(0); // it found things to do
    expect(roomsSeen.size).toBeGreaterThan(1); // and moved between rooms
  });
});
