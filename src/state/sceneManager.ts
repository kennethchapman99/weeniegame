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
import type { Intent, DogCmd } from '../core/input.js';
import type { SceneDef } from '../scenes/types.js';
import { makeDog } from './dog.js';
import type { Dog } from './dog.js';
import { moveDog, collideDogs } from '../systems/movement.js';
import { updateParticles } from '../systems/particles.js';
import { doWrestle, maybeAiWrestle } from '../systems/wrestle.js';
import { updateTug, tugPull } from '../systems/tug.js';
import { tryJump } from '../systems/jump.js';
import { tickTransit, doorTriggers } from '../systems/house.js';
import { player, ai, other } from './gameState.js';
import { aiThink } from '../ai/sibling.js';
import { SPEED, PREDATOR, EVENTS } from '../config/balance.js';
import { yardScene } from '../scenes/yard.js';
import { poolScene } from '../scenes/pool.js';
import { houseScene } from '../scenes/house/index.js';
import { gateMission } from '../scenes/missions/gate.js';
import { sneakMission } from '../scenes/missions/sneak.js';
import { creekMission } from '../scenes/missions/creek.js';
import { kitchenMission } from '../scenes/missions/kitchen.js';
import { hawkMission } from '../scenes/missions/hawk.js';
import { tickMission } from '../systems/mission.js';

/** Versus rounds (M2–M8): Backyard → Pool → House. */
const VERSUS_REGISTRY: SceneDef[] = [yardScene, poolScene, houseScene];
/** Co-op mission campaign (M12+), played in order. */
const COOP_REGISTRY: SceneDef[] = [gateMission, sneakMission, creekMission, kitchenMission, hawkMission];

/** The active registry for the current game mode. */
function registry(s: GameState): SceneDef[] {
  return s.mode === 'coop' ? COOP_REGISTRY : VERSUS_REGISTRY;
}

export function sceneDefs(): readonly SceneDef[] {
  return VERSUS_REGISTRY;
}

function currentScene(s: GameState): SceneDef {
  const r = registry(s);
  return r[s.sceneIdx] ?? r[0]!;
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
  s.tug = null;
  s.sounds = [];
  s.spawnTimer = 1.2;
  s.toast = null;
  s.mission = null; // co-op scenes rebuild it in enter(); versus scenes leave it null
  // predators + ambient events reset per scene (predators are backyard-only)
  s.predator = null;
  s.predatorTimer = s.rng.range(PREDATOR.firstSpawn[0], PREDATOR.firstSpawn[1]);
  s.carriedDog = null;
  s.squirrel = null;
  s.treat = null;
  s.bellyRub = null;
  s.eventTimer = s.rng.range(EVENTS.schedule[0], EVENTS.schedule[1]);
  // house entities (the scene's enter() repopulates them when it's the house round)
  s.squishies = [];
  s.couch = null;
  for (const id of ['cheddar', 'cocoa'] as const) {
    const d = s.dogs[id];
    d.mode = 'free';
    d.zoom = 0;
    d.immune = 0;
    d.dryT = 0;
    d.jumpT = 0;
    d.barkT = 0;
    d.transit = null;
    d.room = '';
    d.trail = [];
    d.hist = [];
  }
  def.enter(s);
  s.phase = 'inter';
  s.interTimer = 1.6;
}

/** A no-op command (P2 idle, or two-player mode without a host-supplied P2 source). */
const IDLE_CMD: DogCmd = { intent: { ax: 0, ay: 0, arrive: 0 }, wrestle: false, jump: false };

/** Apply one dog's discrete actions: mash the tug if locked into one, else wrestle / jump. */
function applyAction(s: GameState, d: Dog, wrestle: boolean, jump: boolean): void {
  if (wrestle) {
    if (s.tug && d.mode === 'tug') tugPull(s, d);
    else doWrestle(s, d, other(s, d));
  }
  if (jump) tryJump(s, d);
}

export function endRound(s: GameState): void {
  s.sceneIdx++;
  if (s.sceneIdx < registry(s).length) {
    beginScene(s);
  } else {
    s.phase = 'end';
  }
}

/** Co-op campaign: is there a next mission after the current one? */
export function coopHasNext(s: GameState): boolean {
  return s.mode === 'coop' && s.sceneIdx < COOP_REGISTRY.length - 1;
}

/** Advance to the next co-op mission (after a success). */
export function advanceCoop(s: GameState): void {
  s.sceneIdx++;
  beginScene(s);
}

/** Restart the current scene (co-op mission retry after a fail). */
export function retryScene(s: GameState): void {
  beginScene(s);
}

/**
 * Advance the whole game one fixed step. Owns phase transitions and the ordered system
 * pipeline for the active round.
 *
 * P1 intent/actions come from the input layer. The sibling is driven by P2 in two-player
 * mode (`s.partner === 'human'`, host supplies `p2`) or by the AI brain otherwise (M10).
 * Both dogs run the identical movement/action systems — only the *intent source* differs.
 */
export function updateGame(
  s: GameState,
  intent: Intent,
  wrestle: boolean,
  jump: boolean,
  dt: number,
  p2?: DogCmd | null,
): void {
  s.elapsedMs += dt * 1000; // animation/streak clock advances in every phase

  if (s.phase === 'inter') {
    s.interTimer -= dt;
    if (s.interTimer <= 0) s.phase = 'play';
    return;
  }
  if (s.phase !== 'play') return;

  const sibling = ai(s);
  const siblingHuman = s.partner === 'human';
  const siblingCoopAi = !siblingHuman && s.mode === 'coop'; // AI partner in a co-op mission

  // house: advance any in-flight door/stair traversal first (a transiting dog can't steer)
  if (s.sceneKey === 'house') {
    tickTransit(player(s), dt);
    tickTransit(sibling, dt);
  }

  // P1 movement (moveDog no-ops while transiting)
  moveDog(s, player(s), intent.ax, intent.ay, dt, intent.arrive);

  // sibling movement: P2 (human) drives it; else an AI brain — the co-op partner in a mission,
  // or the competitive sibling in versus.
  if (siblingHuman) {
    const c = p2 ?? IDLE_CMD;
    moveDog(s, sibling, c.intent.ax, c.intent.ay, dt, c.intent.arrive);
  } else {
    const def = currentScene(s);
    // co-op partner cooperates with the mission (it may trigger its own jump inside coopAi);
    // versus AI runs the competitive brain. aiFactor 0.88 is inert (balance.ts / owner decision).
    const [aax, aay] = siblingCoopAi && def.coopAi ? def.coopAi(s, sibling) : siblingCoopAi ? [0, 0] : aiThink(s, sibling, dt);
    const aiTd = Math.hypot(aax, aay);
    moveDog(s, sibling, aax, aay, dt, Math.min(1, (aiTd - SPEED.aiArriveRadius) / SPEED.arriveFalloff));
  }

  if (s.sceneKey === 'house') doorTriggers(s); // walking into a doorway starts a transit

  // P1 action: mash the tug if locked into one, else wrestle the sibling
  applyAction(s, player(s), wrestle, jump);
  // sibling action: P2's edges in two-player; the co-op partner acts inside coopAi; the versus
  // AI starts its own wrestling trouble.
  if (siblingHuman) {
    if (p2) applyAction(s, sibling, p2.wrestle, p2.jump);
  } else if (!siblingCoopAi) {
    maybeAiWrestle(s, dt);
  }

  collideDogs(s);
  currentScene(s).update(s, dt); // toys may start a tug here / mission mechanics tick
  updateTug(s, dt);
  updateParticles(s, dt);

  if (s.mode === 'coop') {
    // missions end on objective success/fail, not a round timer
    tickMission(s, dt);
    const m = s.mission;
    if (m) {
      s.sceneTime = m.timeLimit;
      s.timeLeft = Math.max(0, m.timeLimit - m.elapsed);
      if (m.status !== 'active') s.phase = 'end';
    }
  } else {
    s.timeLeft -= dt;
    if (s.timeLeft <= 0) endRound(s);
  }
}

export { currentScene };
