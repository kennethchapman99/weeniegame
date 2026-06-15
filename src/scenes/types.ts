/**
 * scenes/types.ts — the contract a round implements so the scene manager can stay generic.
 * Each milestone that adds a round (M5 pool, M7 house) provides a SceneDef and registers it;
 * the flow/timer/interstitial logic in sceneManager never needs to know which round it is.
 */

import type { GameState } from '../state/gameState.js';
import type { Dog } from '../state/dog.js';
import type { ScenePainter } from '../render/backdrop.js';

export interface SceneConfig {
  key: string;
  name: string;
  sub: string;
  time: number;
}

export interface SceneDef {
  config: SceneConfig;
  /** backdrop cache key for the current frame (varies by visible room for the house) */
  bgKey(s: GameState): string;
  /** the painter for the current backdrop (the visible room's painter for the house) */
  paint(s: GameState): ScenePainter;
  /** per-entry (re)initialisation — the ONE explicit reset point per scene (CLAUDE.md) */
  enter(s: GameState): void;
  /** scene-specific simulation for one fixed step (toys, spot, round mechanics) */
  update(s: GameState, dt: number): void;
  /** scene-specific world-space drawing (props: spot/sunbeam/toys/events/couch/squish…) */
  drawWorld(g: CanvasRenderingContext2D, s: GameState): void;
  /** which dogs are currently visible (room-filtered for the house) */
  visibleDogs(s: GameState): Dog[];
  /**
   * Co-op solo fallback (M13): steer the AI partner to cooperate with THIS mission's objectives
   * (cover a pad, distract a guard, brace a boost, knock snacks…). Returns an un-normalised
   * intent vector; may trigger the dog's own actions (e.g. a jump) internally. Only called in
   * co-op mode when the sibling is AI-driven. Missions without it leave the partner idle.
   */
  coopAi?(s: GameState, d: Dog): [number, number];
}
