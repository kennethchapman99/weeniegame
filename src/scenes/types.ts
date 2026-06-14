/**
 * scenes/types.ts — the contract a round implements so the scene manager can stay generic.
 * Each milestone that adds a round (M5 pool, M7 house) provides a SceneDef and registers it;
 * the flow/timer/interstitial logic in sceneManager never needs to know which round it is.
 */

import type { GameState } from '../state/gameState.js';
import type { ScenePainter } from '../render/backdrop.js';

export interface SceneConfig {
  key: string;
  name: string;
  sub: string;
  time: number;
}

export interface SceneDef {
  config: SceneConfig;
  /** background painter (drawn once into the cached backdrop) */
  painter: ScenePainter;
  /** per-entry (re)initialisation — the ONE explicit reset point per scene (CLAUDE.md) */
  enter(s: GameState): void;
  /** scene-specific simulation for one fixed step (toys, spot, round mechanics) */
  update(s: GameState, dt: number): void;
  /** scene-specific world-space drawing (toys, spot, etc.) */
  drawWorld(g: CanvasRenderingContext2D, s: GameState): void;
}
