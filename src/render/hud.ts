/**
 * render/hud.ts — in-world HUD: score pills (per dog), round timer bar, scene name.
 * Drawn in world units (camera transform already applied). The prototype used DOM nodes;
 * we render on-canvas so the whole game UI lives in one coordinate space (webview-friendly).
 */

import type { GameState } from '../state/gameState.js';
import { DOGS } from '../config/dogs.js';
import { WORLD } from '../config/balance.js';
import { rounded } from '../core/math.js';

type G = CanvasRenderingContext2D;
const W = WORLD.w;

export function drawHUD(g: G, s: GameState): void {
  // timer bar across the very top
  const pct = Math.max(0, s.timeLeft / s.sceneTime);
  g.fillStyle = 'rgba(0,0,0,.25)';
  g.fillRect(0, 0, W, 6);
  g.fillStyle = pct < 0.2 ? '#e07a5f' : '#f4d3a4';
  g.fillRect(0, 0, W * pct, 6);

  // scene name centred
  g.font = "700 18px Georgia, 'Times New Roman', serif";
  g.textAlign = 'center';
  g.fillStyle = 'rgba(20,14,10,.85)';
  g.fillText(s.sceneKey ? sceneLabel(s) : '', W / 2, 30);

  scorePill(g, 16, 16, s.dogs.cheddar.score, DOGS.cheddar.dry.body[1], 'CHEDDAR', 'left', s.playerId === 'cheddar');
  scorePill(g, W - 16, 16, s.dogs.cocoa.score, DOGS.cocoa.dry.body[1], 'COCOA', 'right', s.playerId === 'cocoa');
}

function sceneLabel(s: GameState): string {
  const left = Math.max(0, Math.ceil(s.timeLeft));
  return `${nameFor(s.sceneKey)}  ·  ${left}s`;
}

function nameFor(key: string): string {
  if (key === 'pool') return 'The Pool';
  if (key === 'house') return 'The House';
  return 'The Backyard';
}

function scorePill(
  g: G,
  x: number,
  y: number,
  score: number,
  swatch: string,
  name: string,
  align: 'left' | 'right',
  isPlayer: boolean,
): void {
  const w = 116;
  const h = 34;
  const px = align === 'left' ? x : x - w;
  g.save();
  g.fillStyle = 'rgba(255,250,242,.92)';
  rounded(g, px, y, w, h, 10);
  g.fill();
  if (isPlayer) {
    g.strokeStyle = '#f4d3a4';
    g.lineWidth = 2.5;
    rounded(g, px, y, w, h, 10);
    g.stroke();
  }
  // swatch
  g.fillStyle = swatch;
  g.beginPath();
  g.arc(px + 18, y + h / 2, 10, 0, 7);
  g.fill();
  // name + score
  g.fillStyle = '#3a2c20';
  g.textAlign = 'left';
  g.font = '800 11px -apple-system, sans-serif';
  g.fillText(name, px + 34, y + 14);
  g.font = '800 18px -apple-system, sans-serif';
  g.fillText(String(score), px + 34, y + 29);
  g.restore();
}
