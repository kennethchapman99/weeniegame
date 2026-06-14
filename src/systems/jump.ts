/**
 * systems/jump.ts — the hop. Ported from the prototype's playerJump: a 0.5s sin arc (render
 * in drawDog via jumpHeight) used to dodge predators, leap, and add flair. Allowed while free
 * or mid-tug; the AI triggers its own jumps in its predator/dodge logic (M8).
 */

import type { GameState } from '../state/gameState.js';
import { playSound } from '../state/gameState.js';
import type { Dog } from '../state/dog.js';
import { JUMP } from '../config/balance.js';

/** Start a jump if eligible. Returns true if it fired. */
export function tryJump(s: GameState, d: Dog): boolean {
  if (d.mode !== 'free' && d.mode !== 'tug') return false;
  if (d.jumpT > 0) return false; // already mid-hop
  d.jumpT = JUMP.duration;
  playSound(s, 'yip');
  return true;
}
