/**
 * state/sceneManager.ts — round flow: registry, per-scene reset, interstitials, timer, end.
 * Ported from the prototype's startGame/beginScene/endRound. Phase machine:
 *   title → inter → play → (inter → play …) → end
 *
 * Scenes are a registry; M2 registers only the yard. M5 pushes the pool and M7 the house —
 * the flow logic here never changes. `beginScene` is the single explicit (re)init point per
 * scene entry (CLAUDE.md: no scattered resets).
 */

import type { GameState } from './gameState.js';
import type { Intent } from '../core/input.js';
import type { SceneDef } from '../scenes/types.js';
import { makeDog } from './dog.js';
import { moveDog, collideDogs } from '../systems/movement.js';
import { updateParticles } from '../systems/particles.js';
import { player, ai } from './gameState.js';
import { aiThink } from '../ai/sibling.js';
import { SPEED } from '../config/balance.js';
import { yardScene } from '../scenes/yard.js';

/** Ordered, registered rounds. Later milestones push pool/house. */
const REGISTRY: SceneDef[] = [yardScene];

export function sceneDefs(): readonly SceneDef[] {
  return REGISTRY;
}

function currentScene(s: GameState): SceneDef {
  return REGISTRY[s.sceneIdx] ?? REGISTRY[0]!;
}

/** Fresh game from the title screen: reset both dogs and start at the first round. */
export function startGame(s: GameState): void {
  s.dogs.cheddar = makeDog('cheddar', 300, 400, 3.2);
  s.dogs.cocoa = makeDog('cocoa', 660, 400, 6.1);
  s.dogs.cocoa.face = -1;
  s.steals = { cheddar: 0, cocoa: 0 };
  s.sceneIdx = 0;
  beginScene(s);
}

/** Enter the round at s.sceneIdx — the one explicit reset per scene. */
export function beginScene(s: GameState): void {
  const def = currentScene(s);
  s.sceneKey = def.config.key;
  s.sceneTime = def.config.time;
  s.timeLeft = def.config.time;
  s.toys = [];
  s.particles = [];
  s.popups = [];
  s.spawnTimer = 1.2;
  s.toast = null;
  for (const id of ['cheddar', 'cocoa'] as const) {
    const d = s.dogs[id];
    d.mode = 'free';
    d.zoom = 0;
    d.immune = 0;
    d.dryT = 0;
    d.trail = [];
    d.hist = [];
  }
  def.enter(s);
  s.phase = 'inter';
  s.interTimer = 1.6;
}

export function endRound(s: GameState): void {
  s.sceneIdx++;
  if (s.sceneIdx < REGISTRY.length) {
    beginScene(s);
  } else {
    s.phase = 'end';
  }
}

/**
 * Advance the whole game one fixed step. Owns phase transitions and the ordered system
 * pipeline for the active round. Player intent comes from the input layer; AI movement is
 * wired in M3.
 */
export function updateGame(s: GameState, intent: Intent, dt: number): void {
  s.elapsedMs += dt * 1000; // animation/streak clock advances in every phase

  if (s.phase === 'inter') {
    s.interTimer -= dt;
    if (s.interTimer <= 0) s.phase = 'play';
    return;
  }
  if (s.phase !== 'play') return;

  // player movement
  moveDog(player(s), intent.ax, intent.ay, dt, intent.arrive);

  // AI sibling — full speed (aiFactor 0.88 is inert; see balance.ts / owner decision)
  const aiDog = ai(s);
  const [aax, aay] = aiThink(s, aiDog, dt);
  const aiTd = Math.hypot(aax, aay);
  moveDog(aiDog, aax, aay, dt, Math.min(1, (aiTd - SPEED.aiArriveRadius) / SPEED.arriveFalloff));

  collideDogs(s);
  currentScene(s).update(s, dt);
  updateParticles(s, dt);

  s.timeLeft -= dt;
  if (s.timeLeft <= 0) endRound(s);
}

export { currentScene };
