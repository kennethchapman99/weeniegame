/**
 * state/dog.ts — the Dog entity.
 *
 * Per ARCHITECTURE: the mutually-exclusive movement states are modelled as a single
 * discriminated `MovementMode` (not a bag of booleans), with timed effects
 * (jump/zoom/immune/wet) as overlays. `busy(d)` === "mode is not free". This kills the
 * "two states at once" class of bug the prototype was prone to.
 *
 * M1 only ever sets mode 'free'; later milestones (wrestle, pool, tug, house) own the
 * transitions into 'stunned' / 'swimming' / 'shaking' / 'transit' / 'tug'. The fields are
 * defined now so those milestones extend rather than restructure.
 */

import type { DogId } from '../config/dogs.js';

export type { DogId };

export type MovementMode = 'free' | 'stunned' | 'swimming' | 'shaking' | 'transit' | 'tug';

export interface TrailNode {
  x: number;
  y: number;
  life: number;
}

export interface Dog {
  id: DogId;
  x: number;
  y: number;
  vx: number;
  vy: number;
  face: 1 | -1;
  seed: number;
  score: number;

  /** the single source of truth for the exclusive movement state */
  mode: MovementMode;

  // timed overlays (seconds remaining; 0 = inactive)
  jumpT: number; // jump arc
  zoom: number; // zoomies turbo + after-image
  immune: number; // belly-rub wrestle immunity
  dryT: number; // wet coat after a shake
  stunT: number; // remaining stun (while mode === 'stunned')
  shakeT: number; // remaining shake (while mode === 'shaking')

  // cooldowns
  bumpCD: number;
  wrestleCD: number;

  // cosmetic
  trail: TrailNode[];
  onFloater: boolean;

  // house
  room?: string;

  // AI scratch (used from M3 on)
  aiTx: number;
  aiTy: number;
  aiWanderT: number;
}

/** Construct a dog. `seed` is the per-dog animation phase offset (cosmetic, deterministic). */
export function makeDog(id: DogId, x: number, y: number, seed = 0): Dog {
  return {
    id,
    x,
    y,
    vx: 0,
    vy: 0,
    face: 1,
    seed,
    score: 0,
    mode: 'free',
    jumpT: 0,
    zoom: 0,
    immune: 0,
    dryT: 0,
    stunT: 0,
    shakeT: 0,
    bumpCD: 0,
    wrestleCD: 0,
    trail: [],
    onFloater: false,
    aiTx: x,
    aiTy: y,
    aiWanderT: 0,
  };
}

/** Busy = not freely steerable. Wrestle/tug eligibility checks build on this. */
export function busy(d: Dog): boolean {
  return d.mode !== 'free';
}

/** Jump arc height 0..1..0 over JUMP.duration — prototype's `jumpHeight`. */
export function jumpHeight(d: Dog, duration: number): number {
  if (d.jumpT <= 0) return 0;
  const x = 1 - d.jumpT / duration;
  return Math.sin(x * Math.PI);
}
