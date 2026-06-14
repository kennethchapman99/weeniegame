/**
 * state/gameState.ts — the typed top-level game state and the single score-mutation point.
 *
 * `addScore` is the ONLY place score changes (CLAUDE.md / BUILD-PLAN M2): it also owns the
 * zoomies streak hook (3 scores within ZOOMIES.windowMs → turbo), so every scoring path
 * routes through here rather than scattering `score++`.
 */

import type { Dog, DogId } from './dog.js';
import { makeDog } from './dog.js';
import type { Rng } from '../core/rng.js';
import type { SoundId } from '../core/audio.js';
import { ZOOMIES } from '../config/balance.js';

export type Phase = 'title' | 'inter' | 'play' | 'end';

export interface Toy {
  x: number;
  y: number;
  room: string;
  type: 'ball' | 'bone' | 'duck' | 'rope';
  tug: boolean;
  /** floater index this toy rides (pool), or -1 */
  fl: number;
  ox: number;
  oy: number;
  t: number; // bob phase
  scale: number; // grow-in 0..1
}

export interface Spot {
  x: number;
  y: number;
  r: number;
  holder: DogId | null;
  prog: number;
  pulse: number;
}

export interface Floater {
  x: number;
  y: number;
  rx: number;
  ry: number;
  vx: number;
  style: 'donut' | 'leaf' | 'ring' | 'donut2';
  ph: number;
}

export interface Sunbeam {
  x: number;
  y: number;
  room: string;
  r: number;
  relocate: number;
  accA: number; // cheddar bask accumulator
  accB: number; // cocoa bask accumulator
  age: number;
}

export interface Particle {
  x: number;
  y: number;
  vx: number;
  vy: number;
  life: number;
  size: number;
  col: string;
  heart?: boolean;
  mote?: boolean;
}

export interface Popup {
  x: number;
  y: number;
  text: string;
  col: string;
  life: number;
}

export interface Tug {
  toy: Toy;
  rope: number; // -1..+1; + = Cheddar (mashA) winning, - = Cocoa (mashB)
  mashA: number; // Cheddar's recent pull
  mashB: number; // Cocoa's recent pull
  growlT: number; // growl-bed cadence timer
  dur: number; // elapsed (for the stalemate timeout)
}

export interface GameState {
  phase: Phase;
  sceneIdx: number;
  sceneKey: string;
  sceneTime: number;
  timeLeft: number;
  interTimer: number;
  elapsedMs: number; // animation/streak clock (T in the prototype)

  playerId: DogId;
  aiId: DogId;
  dogs: Record<DogId, Dog>;

  toys: Toy[];
  spot: Spot | null;
  sunbeam: Sunbeam | null;
  floaters: Floater[];
  tug: Tug | null;
  particles: Particle[];
  popups: Popup[];
  /** sound requests drained + played by the host each frame (keeps the sim audio-free) */
  sounds: SoundId[];

  spawnTimer: number;
  steals: Record<DogId, number>;

  rng: Rng;
  /** transient one-line notifications (toast); consumed by the renderer/host */
  toast: string | null;
}

export function makeGameState(rng: Rng, playerId: DogId = 'cheddar'): GameState {
  const aiId: DogId = playerId === 'cheddar' ? 'cocoa' : 'cheddar';
  return {
    phase: 'title',
    sceneIdx: 0,
    sceneKey: '',
    sceneTime: 45,
    timeLeft: 45,
    interTimer: 0,
    elapsedMs: 0,
    playerId,
    aiId,
    dogs: {
      cheddar: makeDog('cheddar', 300, 400, 3.2),
      cocoa: makeDog('cocoa', 660, 400, 6.1),
    },
    toys: [],
    spot: null,
    sunbeam: null,
    floaters: [],
    tug: null,
    particles: [],
    popups: [],
    sounds: [],
    spawnTimer: 1.2,
    steals: { cheddar: 0, cocoa: 0 },
    rng,
    toast: null,
  };
}

export const player = (s: GameState): Dog => s.dogs[s.playerId];
export const ai = (s: GameState): Dog => s.dogs[s.aiId];
export const other = (s: GameState, d: Dog): Dog => (d.id === 'cheddar' ? s.dogs.cocoa : s.dogs.cheddar);

export function cap(id: string): string {
  return id[0]!.toUpperCase() + id.slice(1);
}

/** Queue a sound for the host to play this frame (systems stay audio-free + deterministic). */
export function playSound(s: GameState, id: SoundId): void {
  s.sounds.push(id);
}

/** The single score mutation point. Adds n to dog, flashes, and fires the zoomies streak. */
export function addScore(s: GameState, d: Dog, n: number): void {
  d.score += n;
  d.hist.push(s.elapsedMs);
  d.hist = d.hist.filter((t) => s.elapsedMs - t < ZOOMIES.windowMs);
  if (d.hist.length >= ZOOMIES.streak && d.zoom <= 0) {
    d.zoom = ZOOMIES.duration;
    d.hist = [];
    s.popups.push({ x: d.x, y: d.y - 66, text: '⚡ ZOOMIES ⚡', col: '#ffe24a', life: 1 });
    s.toast = `${cap(d.id)} has the ZOOMIES ⚡`;
  }
}
